using System;
using System.Linq;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Custom content processor for Mixamo FBX models that adds verbose logging
/// and validates skeleton structure during import.
/// </summary>
[ContentProcessor(DisplayName = "Mixamo Model Processor")]
public class MixamoModelProcessor : ModelProcessor
{
    /// <summary>
    /// Processes a Mixamo FBX model with verbose logging and validation.
    /// </summary>
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        context.Logger.LogImportantMessage("=== Processing Mixamo model: {0} ===", input.Name);

        // Validate skeleton structure
        BoneContent? skeleton = MeshHelper.FindSkeleton(input);

        if (skeleton == null)
        {
            context.Logger.LogWarning(null, null,
                "No skeleton found in model '{0}'. This may be a static model, or FBX import failed to detect bones.",
                input.Name);
        }
        else
        {
            int boneCount = CountBones(skeleton);
            context.Logger.LogImportantMessage("Found skeleton: '{0}' with {1} bones", skeleton.Name, boneCount);

            // Log bone hierarchy for debugging
            context.Logger.LogMessage("Bone hierarchy:");
            LogBoneHierarchy(skeleton, 0, context);
        }

        // Check for animations
        var animations = ExtractAnimations(input, context);
        if (animations.Count > 0)
        {
            context.Logger.LogImportantMessage("Found {0} animation(s):", animations.Count);
            foreach (var anim in animations)
            {
                context.Logger.LogMessage("  - '{0}' (duration: {1:F2}s)", anim.Key, anim.Value.TotalSeconds);
            }
        }
        else
        {
            context.Logger.LogMessage("No animations found (expected for static models)");
        }

        // Call base ModelProcessor for standard processing
        context.Logger.LogMessage("Starting standard ModelProcessor...");
        ModelContent model;

        try
        {
            model = base.Process(input, context);
            context.Logger.LogImportantMessage("Standard ModelProcessor completed successfully");
        }
        catch (Exception ex)
        {
            // Log detailed error and rethrow
            context.Logger.LogMessage("ModelProcessor failed with error: {0}", ex.Message);
            throw new InvalidContentException(
                $"Failed to process Mixamo model '{input.Name}'. This may indicate FBX compatibility issues. " +
                $"Error: {ex.Message}", ex);
        }

        // Extract and attach animation data to Model.Tag
        if (animations.Count > 0)
        {
            var animationData = BuildAnimationData(input, skeleton, animations, context);
            model.Tag = animationData;
            context.Logger.LogMessage("Animation data attached to Model.Tag ({0} clips)", animationData.Clips.Count);
        }

        context.Logger.LogImportantMessage("=== Mixamo model processing complete ===");
        return model;
    }

    /// <summary>
    /// Recursively counts all bones in the skeleton hierarchy.
    /// </summary>
    private int CountBones(BoneContent bone)
    {
        int count = 1;
        foreach (BoneContent child in bone.Children.OfType<BoneContent>())
        {
            count += CountBones(child);
        }
        return count;
    }

    /// <summary>
    /// Logs the bone hierarchy structure for debugging.
    /// </summary>
    private void LogBoneHierarchy(BoneContent bone, int depth, ContentProcessorContext context)
    {
        string indent = new string(' ', depth * 2);
        context.Logger.LogMessage("{0}> {1}", indent, bone.Name);

        foreach (BoneContent child in bone.Children.OfType<BoneContent>())
        {
            LogBoneHierarchy(child, depth + 1, context);
        }
    }

    /// <summary>
    /// Extracts animation information from the node content.
    /// </summary>
    private System.Collections.Generic.Dictionary<string, TimeSpan> ExtractAnimations(
        NodeContent input,
        ContentProcessorContext context)
    {
        var animations = new System.Collections.Generic.Dictionary<string, TimeSpan>();

        // Check for animations in the node hierarchy
        foreach (var animation in input.Animations)
        {
            TimeSpan duration = animation.Value.Duration;
            animations[animation.Key] = duration;
        }

        // Also check child nodes (some FBX files store animations differently)
        foreach (NodeContent child in input.Children)
        {
            foreach (var animation in child.Animations)
            {
                if (!animations.ContainsKey(animation.Key))
                {
                    animations[animation.Key] = animation.Value.Duration;
                }
            }
        }

        return animations;
    }

    /// <summary>
    /// Builds the AnimationData structure from extracted animations.
    /// </summary>
    private AnimationData BuildAnimationData(
        NodeContent input,
        BoneContent? skeleton,
        System.Collections.Generic.Dictionary<string, TimeSpan> animations,
        ContentProcessorContext context)
    {
        var animationData = new AnimationData();

        // Build bone index mapping from skeleton if available
        if (skeleton != null)
        {
            BuildBoneIndices(skeleton, animationData.BoneIndices, 0);
            context.Logger.LogMessage("Built bone index mapping with {0} bones", animationData.BoneIndices.Count);
        }

        // Extract keyframes from animations
        foreach (var anim in animations)
        {
            var clip = new AnimationClip(anim.Key, anim.Value);

            // Extract keyframes from NodeContent animation data
            ExtractKeyframes(input, skeleton, anim.Key, clip, animationData.BoneIndices, context);

            animationData.Clips[anim.Key] = clip;
            context.Logger.LogMessage("Extracted animation '{0}' with {1} bone tracks",
                anim.Key, clip.Keyframes.Count);
        }

        return animationData;
    }

    /// <summary>
    /// Recursively builds the bone name to index mapping.
    /// </summary>
    private void BuildBoneIndices(BoneContent bone, System.Collections.Generic.Dictionary<string, int> indices, int currentIndex = 0)
    {
        indices[bone.Name] = currentIndex;

        int childIndex = currentIndex + 1;
        foreach (BoneContent child in bone.Children.OfType<BoneContent>())
        {
            BuildBoneIndices(child, indices, childIndex);
            childIndex++;
        }
    }

    /// <summary>
    /// Extracts keyframes from bone animations and populates the animation clip.
    /// In Mixamo FBX files, all animation channels are stored on the root bone,
    /// with each channel named after the bone it affects.
    /// For animation-only files without a skeleton, we extract from the root node itself.
    /// </summary>
    private void ExtractKeyframes(
        NodeContent input,
        BoneContent? skeleton,
        string animationName,
        AnimationClip clip,
        System.Collections.Generic.Dictionary<string, int> boneIndices,
        ContentProcessorContext context)
    {
        // Try to find animation on skeleton first
        AnimationContent? animation = null;
        bool isAnimationOnly = false;

        if (skeleton != null && skeleton.Animations.TryGetValue(animationName, out animation))
        {
            // Found on skeleton bone - process normally
            context.Logger.LogMessage("Extracting {0} channels from animation '{1}'",
                animation.Channels.Count, animationName);
        }
        else
        {
            // Animation-only file: collect channels from all child nodes
            context.Logger.LogMessage("Animation-only file detected - collecting channels from node hierarchy");
            isAnimationOnly = true;
        }

        // If boneIndices is empty (animation-only file), build it from animation channels
        bool buildIndices = boneIndices.Count == 0;
        int nextBoneIndex = 0;

        // Collect all nodes with animations
        var nodesToProcess = new System.Collections.Generic.List<(NodeContent node, AnimationContent anim)>();

        if (isAnimationOnly)
        {
            // For animation-only files, collect animations from all nodes in hierarchy
            void CollectAnimations(NodeContent node)
            {
                if (node.Animations.TryGetValue(animationName, out AnimationContent? anim))
                {
                    nodesToProcess.Add((node, anim));
                }
                foreach (NodeContent child in node.Children)
                {
                    CollectAnimations(child);
                }
            }
            CollectAnimations(input);
            context.Logger.LogMessage("Found animation on {0} nodes in hierarchy", nodesToProcess.Count);
        }
        else if (animation != null)
        {
            // For normal files, just process the one animation
            nodesToProcess.Add((input, animation));
        }

        // Process all collected animations
        foreach (var (node, anim) in nodesToProcess)
        {
            // Each channel is named after the bone it animates
            foreach (var channel in anim.Channels)
            {
                string boneName = channel.Key;
                var animKeyframes = channel.Value;

                // Get or create bone index for this channel
                int boneIndex;
                if (!boneIndices.TryGetValue(boneName, out boneIndex))
                {
                    if (buildIndices)
                    {
                        // For animation-only files, create bone indices on-the-fly
                        boneIndex = nextBoneIndex++;
                        boneIndices[boneName] = boneIndex;
                    }
                    else
                    {
                        continue; // Skip bones not in our skeleton
                    }
                }

                // Convert animation keyframes to our Keyframe format
                var keyframes = new System.Collections.Generic.List<Keyframe>();
                foreach (var animKeyframe in animKeyframes)
                {
                    keyframes.Add(new Keyframe(
                        animKeyframe.Time,
                        boneIndex,
                        animKeyframe.Transform));
                }

                // Sort by time and store
                keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
                clip.Keyframes[boneName] = keyframes;

                // Debug: log first bone with keyframes
                if (clip.Keyframes.Count == 1)
                {
                    context.Logger.LogMessage("First bone '{0}' has {1} keyframes", boneName, keyframes.Count);
                }
            }
        }

        if (buildIndices && boneIndices.Count > 0)
        {
            context.Logger.LogMessage("Built {0} bone indices from animation channels", boneIndices.Count);
        }

        context.Logger.LogMessage("Extracted {0} bone tracks", clip.Keyframes.Count);
    }
}
