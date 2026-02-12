using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Custom content processor for Mixamo FBX models that produces SkinningData
/// following the canonical XNA SkinnedModelProcessor pattern.
///
/// The processor:
/// 1. Finds the skeleton via MeshHelper.FindSkeleton
/// 2. Flattens non-bone transforms into geometry
/// 3. Flattens the skeleton to establish canonical bone ordering
/// 4. Extracts bind pose, inverse bind pose, and skeleton hierarchy
/// 5. Extracts animation keyframes with correct bone indices
/// 6. Calls base.Process() for mesh compilation
/// 7. Attaches SkinningData to Model.Tag
///
/// For animation-only FBX files (no skeleton), animations are extracted
/// with incrementally assigned bone indices and empty skeleton arrays.
/// </summary>
[ContentProcessor(DisplayName = "Mixamo Model Processor")]
public class MixamoModelProcessor : ModelProcessor
{
    /// <summary>
    /// Forces SkinnedEffect for all materials processed by this processor.
    /// This is the canonical workaround from the XNA SkinnedModel sample.
    /// If this does not work in practice (MonoGame Issue #3057), the fallback
    /// is to override ConvertMaterial() to return SkinnedMaterialContent.
    /// </summary>
    [DefaultValue(MaterialProcessorDefaultEffect.SkinnedEffect)]
    public override MaterialProcessorDefaultEffect DefaultEffect
    {
        get => MaterialProcessorDefaultEffect.SkinnedEffect;
        set { }
    }

    /// <summary>
    /// Processes a Mixamo FBX model, producing SkinningData with canonical bone ordering.
    /// </summary>
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        context.Logger.LogImportantMessage("=== Processing Mixamo model: {0} ===", input.Name);

        // Step 1: Find skeleton
        BoneContent? skeleton = MeshHelper.FindSkeleton(input);

        // Step 2: Handle skeleton == null (animation-only or static model)
        if (skeleton == null)
        {
            context.Logger.LogWarning(null, null,
                "No skeleton found in '{0}'. Processing as animation-only/static model.", input.Name);
            return ProcessWithoutSkeleton(input, context);
        }

        // Step 3: Flatten non-bone transforms into geometry
        FlattenTransforms(input, skeleton);

        // Step 4: Flatten skeleton -- canonical bone ordering
        IList<BoneContent> bones = MeshHelper.FlattenSkeleton(skeleton);
        context.Logger.LogImportantMessage("Skeleton: {0} bones (max {1})", bones.Count, 72);

        // Validate bone count
        if (bones.Count > 72)
        {
            throw new InvalidContentException(
                $"Model '{input.Name}' skeleton has {bones.Count} bones, exceeding SkinnedEffect.MaxBones (72). " +
                "Reduce bone count in the model or use a custom shader that supports more bones.");
        }

        // Log bone names for debugging
        for (int i = 0; i < bones.Count; i++)
        {
            context.Logger.LogMessage("  Bone[{0}]: {1}", i, bones[i].Name);
        }

        // Step 5: Extract bind pose (bone.Transform for each bone in flattened list)
        var bindPose = new List<Matrix>();
        // Step 6: Extract inverse bind pose (Matrix.Invert(bone.AbsoluteTransform))
        var inverseBindPose = new List<Matrix>();
        // Step 7: Extract skeleton hierarchy (parent indices)
        var skeletonHierarchy = new List<int>();

        foreach (BoneContent bone in bones)
        {
            bindPose.Add(bone.Transform);
            inverseBindPose.Add(Matrix.Invert(bone.AbsoluteTransform));
            // bones.IndexOf(bone.Parent as BoneContent):
            // Root bone's parent is NodeContent (not BoneContent), so cast returns null,
            // and IndexOf(null) returns -1 -- which is the correct sentinel for "no parent".
            skeletonHierarchy.Add(bones.IndexOf((bone.Parent as BoneContent)!));
        }

        // Step 8: Extract animations
        var animationClips = ProcessAnimations(skeleton, bones, input, context);
        context.Logger.LogImportantMessage("Extracted {0} animation clip(s)", animationClips.Count);

        // Step 9: Standard ModelProcessor handles mesh compilation, vertex channels, materials
        ModelContent model = base.Process(input, context);

        // Step 10: Attach SkinningData to Model.Tag
        model.Tag = new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);
        context.Logger.LogImportantMessage(
            "Attached SkinningData: {0} bones, {1} clips", bones.Count, animationClips.Count);

        context.Logger.LogImportantMessage("=== Mixamo model processing complete: {0} ===", input.Name);
        return model;
    }

    /// <summary>
    /// Bakes non-bone node transforms into geometry. Recurses through the node tree,
    /// skipping the skeleton (whose transforms are bone rest poses and must not be flattened).
    /// Source: XNA SkinningSample_4_0.
    /// </summary>
    private static void FlattenTransforms(NodeContent node, BoneContent skeleton)
    {
        foreach (NodeContent child in node.Children)
        {
            // Do NOT flatten the skeleton itself -- it stores bone rest transforms
            if (child == skeleton)
                continue;

            // Bake this node's transform into its geometry
            MeshHelper.TransformScene(child, child.Transform);

            // Reset the node's transform to identity (it's now in the geometry)
            child.Transform = Matrix.Identity;

            // Recurse into children
            FlattenTransforms(child, skeleton);
        }
    }

    /// <summary>
    /// Extracts animation clips from the model, using the flattened skeleton
    /// for correct bone index mapping. Applies PreRotation correction for bones
    /// where the Assimp importer didn't bake PreRotation into keyframes.
    ///
    /// Searches for animations in multiple locations (Mixamo-specific):
    /// 1. skeleton.Animations (standard location)
    /// 2. input.Animations (root node)
    /// 3. Child nodes recursively
    /// </summary>
    private Dictionary<string, SkinningDataClip> ProcessAnimations(
        BoneContent skeleton,
        IList<BoneContent> bones,
        NodeContent input,
        ContentProcessorContext context)
    {
        // Build bone-name-to-index mapping from flattened skeleton
        var boneMap = new Dictionary<string, int>();
        for (int i = 0; i < bones.Count; i++)
        {
            boneMap[bones[i].Name] = i;
        }

        // Read PreRotation per bone from FBX binary file
        var preRotations = ReadPreRotations(input, boneMap, context, out var preRotEulers);

        // Collect all animations from multiple locations
        var allAnimations = new Dictionary<string, AnimationContent>();

        // 1. Check skeleton.Animations first (standard location)
        foreach (var anim in skeleton.Animations)
        {
            allAnimations[anim.Key] = anim.Value;
            context.Logger.LogMessage("Found animation '{0}' on skeleton", anim.Key);
        }

        // 2. Check input.Animations (root node)
        foreach (var anim in input.Animations)
        {
            if (!allAnimations.ContainsKey(anim.Key))
            {
                allAnimations[anim.Key] = anim.Value;
                context.Logger.LogMessage("Found animation '{0}' on root node", anim.Key);
            }
        }

        // 3. Check child nodes recursively
        CollectAnimationsFromChildren(input, allAnimations, context);

        // Process each animation into a SkinningDataClip
        var clips = new Dictionary<string, SkinningDataClip>();

        foreach (var animation in allAnimations)
        {
            var keyframes = new List<SkinningDataKeyframe>();

            foreach (var channel in animation.Value.Channels)
            {
                string boneName = channel.Key;

                // Resolve bone name to flattened index
                if (!boneMap.TryGetValue(boneName, out int boneIndex))
                {
                    context.Logger.LogWarning(null, null,
                        "Animation '{0}' channel '{1}' not found in skeleton, skipping",
                        animation.Key, boneName);
                    continue;
                }

                // Convert each keyframe
                foreach (var kf in channel.Value)
                {
                    keyframes.Add(new SkinningDataKeyframe(boneIndex, kf.Time, kf.Transform));
                }
            }

            // Sort by Time then by Bone index (canonical ordering for cache-friendly playback)
            keyframes.Sort((a, b) =>
            {
                int cmp = a.Time.CompareTo(b.Time);
                return cmp != 0 ? cmp : a.Bone.CompareTo(b.Bone);
            });

            // Apply PreRotation correction to keyframes that are missing it
            keyframes = ApplyPreRotationCorrection(keyframes, bones, preRotations, preRotEulers, context, animation.Key);

            clips[animation.Key] = new SkinningDataClip(animation.Value.Duration, keyframes);
            context.Logger.LogImportantMessage(
                "  Clip '{0}': duration={1:F2}s, keyframes={2}",
                animation.Key, animation.Value.Duration.TotalSeconds, keyframes.Count);
        }

        return clips;
    }

    /// <summary>
    /// Reads FBX PreRotation data and maps it to flattened skeleton bone indices.
    /// Returns a dictionary of boneIndex → PreRotation rotation matrix, plus
    /// the Euler angles for magnitude checking.
    /// </summary>
    private static Dictionary<int, Matrix> ReadPreRotations(
        NodeContent input,
        Dictionary<string, int> boneMap,
        ContentProcessorContext context,
        out Dictionary<int, Vector3> eulerAngles)
    {
        var result = new Dictionary<int, Matrix>();
        eulerAngles = new Dictionary<int, Vector3>();

        string? fbxPath = input.Identity?.SourceFilename;
        if (string.IsNullOrEmpty(fbxPath))
        {
            context.Logger.LogWarning(null, null, "Cannot read PreRotation: no source file path available");
            return result;
        }

        Dictionary<string, Vector3> preRotEulers;
        try
        {
            preRotEulers = FbxPreRotationReader.Read(fbxPath);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(null, null,
                "Failed to read FBX PreRotation data: {0}", ex.Message);
            return result;
        }

        context.Logger.LogImportantMessage("FBX PreRotation: found {0} bones with PreRotation", preRotEulers.Count);

        foreach (var entry in preRotEulers)
        {
            if (boneMap.TryGetValue(entry.Key, out int boneIndex))
            {
                Matrix preRotMatrix = FbxPreRotationReader.EulerToMatrix(entry.Value);
                result[boneIndex] = preRotMatrix;
                eulerAngles[boneIndex] = entry.Value;
                context.Logger.LogMessage("  PreRot[{0}] '{1}': ({2:F1}, {3:F1}, {4:F1})°",
                    boneIndex, entry.Key, entry.Value.X, entry.Value.Y, entry.Value.Z);
            }
            else
            {
                context.Logger.LogMessage("  PreRot '{0}': not in skeleton (skipped)", entry.Key);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies PreRotation correction to animation keyframes.
    ///
    /// The Assimp FBX importer bakes PreRotation into bone.Transform (bind pose)
    /// but inconsistently bakes it into animation keyframes. For each bone with
    /// significant PreRotation (>15°), this method detects whether the keyframes
    /// already include it by comparing the first keyframe's rotation to two references:
    ///   - The bind pose rotation (which includes PreRotation)
    ///   - The bind pose rotation with PreRotation removed
    ///
    /// Injection only occurs when the evidence is clear: the quaternion dot product
    /// margin between "without PreRot" and "with PreRot" must exceed a threshold.
    /// This prevents false positives from animation pose differences on bones with
    /// moderate PreRotation.
    ///
    /// This mirrors the three.js FBXLoader approach of uniformly baking
    /// PreRotation into all keyframes.
    /// </summary>
    private static List<SkinningDataKeyframe> ApplyPreRotationCorrection(
        List<SkinningDataKeyframe> keyframes,
        IList<BoneContent> bones,
        Dictionary<int, Matrix> preRotations,
        Dictionary<int, Vector3> preRotEulers,
        ContentProcessorContext context,
        string clipName)
    {
        if (keyframes.Count == 0 || preRotations.Count == 0)
            return keyframes;

        // Minimum PreRotation magnitude to consider (degrees).
        // Bones with smaller PreRotation are skipped — the error is negligible
        // and detection becomes unreliable due to animation pose noise.
        const float MinPreRotDegrees = 15.0f;

        // Minimum margin between dotWithoutPre and dotWithBind to trigger injection.
        // Prevents false positives from bones where the animation pose difference
        // makes the two references similarly distant from the keyframe rotation.
        const float MinDetectionMargin = 0.05f;

        // Find first keyframe for each bone (sorted by time, so first occurrence is earliest)
        var firstKeyframe = new Dictionary<int, Matrix>();
        foreach (var kf in keyframes)
        {
            if (!firstKeyframe.ContainsKey(kf.Bone))
                firstKeyframe[kf.Bone] = kf.Transform;
        }

        // Determine which bones need PreRotation injected
        var bonesNeedingFix = new HashSet<int>();

        foreach (var entry in preRotations)
        {
            int boneIndex = entry.Key;
            Matrix preRotMatrix = entry.Value;

            if (!firstKeyframe.ContainsKey(boneIndex))
                continue;
            if (boneIndex >= bones.Count)
                continue;

            // Skip bones with small PreRotation — error is negligible and detection unreliable
            if (preRotEulers.TryGetValue(boneIndex, out Vector3 euler))
            {
                float maxComponent = Math.Max(Math.Abs(euler.X), Math.Max(Math.Abs(euler.Y), Math.Abs(euler.Z)));
                if (maxComponent < MinPreRotDegrees)
                {
                    context.Logger.LogMessage(
                        "  Clip '{0}' bone[{1}] '{2}': PreRot too small ({3:F1}°), skipping",
                        clipName, boneIndex, bones[boneIndex].Name, maxComponent);
                    continue;
                }
            }

            Matrix bindPose = bones[boneIndex].Transform;
            Matrix kf0 = firstKeyframe[boneIndex];

            // Decompose bind pose and first keyframe to get rotation quaternions
            if (!bindPose.Decompose(out _, out Quaternion qBind, out _))
                continue;
            if (!kf0.Decompose(out _, out Quaternion qKf, out _))
                continue;

            // Compute bind pose rotation WITHOUT PreRotation
            Quaternion qPreRot = Quaternion.CreateFromRotationMatrix(preRotMatrix);
            Quaternion qPreRotInv = Quaternion.Inverse(qPreRot);
            // qBindWithoutPre = the rotation that bind pose would have if PreRot were removed
            // In XNA: bind rotation = PreRot * RestRot, so RestRot = Inverse(PreRot) * BindRot
            // Quaternion multiply: q1 * q2 applies q2 first, then q1 (Hamilton convention)
            // To get Inverse(PreRot) * BindRot: Quaternion.Multiply(qPreRotInv, qBind)
            Quaternion qBindWithoutPre = Quaternion.Multiply(qPreRotInv, qBind);

            // Compare keyframe rotation to both references using quaternion dot product
            // |dot| close to 1 = same rotation, close to 0 = 90° apart
            float dotWithBind = Math.Abs(Quaternion.Dot(qKf, qBind));
            float dotWithoutPre = Math.Abs(Quaternion.Dot(qKf, qBindWithoutPre));
            float margin = dotWithoutPre - dotWithBind;

            if (margin > MinDetectionMargin)
            {
                // Clear evidence: keyframe is much closer to "without PreRot" → needs fix
                bonesNeedingFix.Add(boneIndex);
                context.Logger.LogImportantMessage(
                    "  Clip '{0}' bone[{1}] '{2}': PreRot NOT in keyframes (margin={3:F4}) → injecting",
                    clipName, boneIndex, bones[boneIndex].Name, margin);
            }
            else
            {
                context.Logger.LogMessage(
                    "  Clip '{0}' bone[{1}] '{2}': PreRot already in keyframes (margin={3:F4})",
                    clipName, boneIndex, bones[boneIndex].Name, margin);
            }
        }

        if (bonesNeedingFix.Count == 0)
        {
            context.Logger.LogImportantMessage("  Clip '{0}': all bones have PreRot baked, no correction needed", clipName);
            return keyframes;
        }

        context.Logger.LogImportantMessage(
            "  Clip '{0}': injecting PreRotation into {1} bones", clipName, bonesNeedingFix.Count);

        // Apply PreRotation to keyframes for bones that need it
        var result = new List<SkinningDataKeyframe>(keyframes.Count);

        foreach (var kf in keyframes)
        {
            if (bonesNeedingFix.Contains(kf.Bone) && preRotations.TryGetValue(kf.Bone, out Matrix preRotMatrix))
            {
                Matrix corrected = InjectPreRotation(kf.Transform, preRotMatrix);
                result.Add(new SkinningDataKeyframe(kf.Bone, kf.Time, corrected));
            }
            else
            {
                result.Add(kf);
            }
        }

        return result;
    }

    /// <summary>
    /// Injects PreRotation into a keyframe transform by decomposing it,
    /// prepending the PreRotation to the rotation component, and recomposing.
    ///
    /// Input:  kf = S * R * T  (scale, then rotate, then translate)
    /// Output: S * (PreRot * R) * T  (scale, then pre-rotate, then rotate, then translate)
    /// </summary>
    private static Matrix InjectPreRotation(Matrix keyframe, Matrix preRotMatrix)
    {
        if (!keyframe.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation))
        {
            // Decompose failed (e.g., degenerate matrix) — return as-is
            return keyframe;
        }

        // Combine PreRotation with keyframe rotation
        // In XNA row-major: M1 * M2 means apply M1 first
        // We want: PreRot applied first, then keyframe's rotation
        // So: newRotMatrix = preRotMatrix * rotMatrix
        Matrix rotMatrix = Matrix.CreateFromQuaternion(rotation);
        Matrix newRotMatrix = preRotMatrix * rotMatrix;
        Quaternion newRotation = Quaternion.CreateFromRotationMatrix(newRotMatrix);

        return Matrix.CreateScale(scale) *
               Matrix.CreateFromQuaternion(newRotation) *
               Matrix.CreateTranslation(translation);
    }

    /// <summary>
    /// Recursively collects animations from child nodes.
    /// Some Mixamo FBX files store animations on child nodes rather than the skeleton root.
    /// </summary>
    private void CollectAnimationsFromChildren(
        NodeContent node,
        Dictionary<string, AnimationContent> animations,
        ContentProcessorContext context)
    {
        foreach (NodeContent child in node.Children)
        {
            foreach (var anim in child.Animations)
            {
                if (!animations.ContainsKey(anim.Key))
                {
                    animations[anim.Key] = anim.Value;
                    context.Logger.LogMessage("Found animation '{0}' on child node '{1}'",
                        anim.Key, child.Name);
                }
            }

            CollectAnimationsFromChildren(child, animations, context);
        }
    }

    /// <summary>
    /// Processes a model without a recognized skeleton (animation-only or static model).
    /// For animation-only FBX files, extracts animations from the node hierarchy
    /// with incrementally assigned bone indices. The skeleton data (bind pose,
    /// inverse bind pose) is empty -- it will come from the base model at runtime.
    /// </summary>
    private ModelContent ProcessWithoutSkeleton(NodeContent input, ContentProcessorContext context)
    {
        // Extract animations from node hierarchy
        var allAnimations = new Dictionary<string, AnimationContent>();

        // Check root node
        foreach (var anim in input.Animations)
        {
            allAnimations[anim.Key] = anim.Value;
            context.Logger.LogMessage("Found animation '{0}' on root node (no skeleton)", anim.Key);
        }

        // Check child nodes recursively
        CollectAnimationsFromChildren(input, allAnimations, context);

        // Build bone indices from animation channel names (incrementing counter)
        var boneMap = new Dictionary<string, int>();
        var clips = new Dictionary<string, SkinningDataClip>();

        foreach (var animation in allAnimations)
        {
            var keyframes = new List<SkinningDataKeyframe>();

            foreach (var channel in animation.Value.Channels)
            {
                string boneName = channel.Key;

                // Assign bone index incrementally for animation-only files
                if (!boneMap.TryGetValue(boneName, out int boneIndex))
                {
                    boneIndex = boneMap.Count;
                    boneMap[boneName] = boneIndex;
                }

                foreach (var kf in channel.Value)
                {
                    keyframes.Add(new SkinningDataKeyframe(boneIndex, kf.Time, kf.Transform));
                }
            }

            // Sort by Time then by Bone index
            keyframes.Sort((a, b) =>
            {
                int cmp = a.Time.CompareTo(b.Time);
                return cmp != 0 ? cmp : a.Bone.CompareTo(b.Bone);
            });

            clips[animation.Key] = new SkinningDataClip(animation.Value.Duration, keyframes);
            context.Logger.LogImportantMessage(
                "  Clip '{0}' (no skeleton): duration={1:F2}s, keyframes={2}",
                animation.Key, animation.Value.Duration.TotalSeconds, keyframes.Count);
        }

        if (clips.Count > 0)
        {
            context.Logger.LogImportantMessage(
                "Extracted {0} animation clip(s) with {1} unique bone names (no skeleton data)",
                clips.Count, boneMap.Count);
        }

        // Call base.Process for standard mesh processing
        ModelContent model = base.Process(input, context);

        // Attach SkinningData with empty skeleton arrays (0 bones)
        // At runtime, animation clips from this file will be merged into
        // the base model's SkinningData which has the actual skeleton.
        var emptyBindPose = new List<Matrix>();
        var emptyInverseBindPose = new List<Matrix>();
        var emptyHierarchy = new List<int>();

        model.Tag = new SkinningData(clips, emptyBindPose, emptyInverseBindPose, emptyHierarchy);

        context.Logger.LogWarning(null, null,
            "Model '{0}' processed without skeleton data. " +
            "Animation clips are available but skeleton data (bind pose, inverse bind pose) " +
            "must be provided by the base model at runtime merge.", input.Name);

        context.Logger.LogImportantMessage("=== Mixamo model processing complete (no skeleton): {0} ===", input.Name);
        return model;
    }
}
