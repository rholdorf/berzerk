using System;
using Microsoft.Xna.Framework;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Represents a single keyframe for a single bone in a skinned animation.
/// Keyframes are stored in a flat list sorted by time within <see cref="SkinningDataClip"/>.
/// Each keyframe records the local-space transform of one bone at one point in time.
/// </summary>
public class SkinningDataKeyframe
{
    /// <summary>
    /// Index of the bone this keyframe affects.
    /// Matches the index in <see cref="SkinningData.BindPose"/>,
    /// <see cref="SkinningData.InverseBindPose"/>, and
    /// <see cref="SkinningData.SkeletonHierarchy"/>.
    /// </summary>
    public int Bone { get; private set; }

    /// <summary>
    /// Time offset from the start of the animation clip.
    /// </summary>
    public TimeSpan Time { get; private set; }

    /// <summary>
    /// Local-space bone transform at this point in time.
    /// Relative to the bone's parent in the skeleton hierarchy, not absolute/world space.
    /// </summary>
    public Matrix Transform { get; private set; }

    /// <summary>
    /// Creates a new keyframe.
    /// </summary>
    /// <param name="bone">Bone index (matches BindPose/InverseBindPose/SkeletonHierarchy index).</param>
    /// <param name="time">Time offset from clip start.</param>
    /// <param name="transform">Local-space bone transform at this time.</param>
    public SkinningDataKeyframe(int bone, TimeSpan time, Matrix transform)
    {
        Bone = bone;
        Time = time;
        Transform = transform;
    }
}
