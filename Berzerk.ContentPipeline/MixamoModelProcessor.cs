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
            var animationData = BuildAnimationData(skeleton, animations, context);
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
        BoneContent? skeleton,
        System.Collections.Generic.Dictionary<string, TimeSpan> animations,
        ContentProcessorContext context)
    {
        var animationData = new AnimationData();

        // Build bone index mapping
        if (skeleton != null)
        {
            BuildBoneIndices(skeleton, animationData.BoneIndices, 0);
            context.Logger.LogMessage("Built bone index mapping with {0} bones", animationData.BoneIndices.Count);
        }

        // For now, just store clip names and durations
        // Actual keyframe extraction will be implemented when needed
        foreach (var anim in animations)
        {
            var clip = new AnimationClip(anim.Key, anim.Value);
            animationData.Clips[anim.Key] = clip;
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
}
