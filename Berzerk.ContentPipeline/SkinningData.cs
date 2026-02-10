using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Top-level container for all skinning data in a model.
/// This gets attached to Model.Tag by the content processor and serialized to XNB format.
/// Contains the skeleton definition (bind pose, inverse bind pose, hierarchy) and all animation clips.
/// </summary>
public class SkinningData
{
    /// <summary>
    /// Animation clips keyed by name (e.g., "idle", "walk", "run", "bash").
    /// Each clip contains a flat list of keyframes for all bones.
    /// </summary>
    public Dictionary<string, SkinningDataClip> AnimationClips { get; private set; }

    /// <summary>
    /// Bind pose matrices for each bone, in local space relative to parent.
    /// Index matches skeleton hierarchy order from MeshHelper.FlattenSkeleton().
    /// These are the rest-pose transforms used as the starting point for animation.
    /// </summary>
    public List<Matrix> BindPose { get; private set; }

    /// <summary>
    /// Inverse bind pose matrices: Matrix.Invert(bone.AbsoluteTransform).
    /// Transforms vertices from model space to bone-local space.
    /// Used in Stage 3 of the three-stage skinning transform pipeline.
    /// </summary>
    public List<Matrix> InverseBindPose { get; private set; }

    /// <summary>
    /// For each bone, the index of its parent bone. Root bone has parent index -1.
    /// Index order matches <see cref="BindPose"/> and <see cref="InverseBindPose"/> arrays.
    /// Used by the animation player to traverse the bone hierarchy and compute world transforms.
    /// </summary>
    public List<int> SkeletonHierarchy { get; private set; }

    /// <summary>
    /// Creates a new SkinningData container.
    /// </summary>
    /// <param name="animationClips">Animation clips keyed by name.</param>
    /// <param name="bindPose">Local-space bone rest transforms, one per bone.</param>
    /// <param name="inverseBindPose">Vertex-to-bonespace transforms, one per bone.</param>
    /// <param name="skeletonHierarchy">Parent index per bone (root bone is -1).</param>
    /// <exception cref="ArgumentException">
    /// Thrown if bindPose, inverseBindPose, and skeletonHierarchy have different counts.
    /// All three arrays must have exactly one entry per bone.
    /// </exception>
    public SkinningData(
        Dictionary<string, SkinningDataClip> animationClips,
        List<Matrix> bindPose,
        List<Matrix> inverseBindPose,
        List<int> skeletonHierarchy)
    {
        if (bindPose.Count != inverseBindPose.Count || bindPose.Count != skeletonHierarchy.Count)
        {
            throw new ArgumentException(
                $"Bone array length mismatch: BindPose has {bindPose.Count}, " +
                $"InverseBindPose has {inverseBindPose.Count}, " +
                $"SkeletonHierarchy has {skeletonHierarchy.Count}. " +
                "All three arrays must have the same length (one entry per bone).");
        }

        AnimationClips = animationClips;
        BindPose = bindPose;
        InverseBindPose = inverseBindPose;
        SkeletonHierarchy = skeletonHierarchy;
    }
}
