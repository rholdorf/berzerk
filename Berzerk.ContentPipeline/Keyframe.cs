using Microsoft.Xna.Framework;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Represents a single keyframe for a single bone in an animation.
/// </summary>
public class Keyframe
{
    /// <summary>
    /// Time position of this keyframe within the animation clip.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Index of the bone this keyframe affects.
    /// </summary>
    public int BoneIndex { get; set; }

    /// <summary>
    /// Transformation matrix for the bone at this keyframe.
    /// </summary>
    public Matrix Transform { get; set; }

    public Keyframe()
    {
        Time = TimeSpan.Zero;
        BoneIndex = 0;
        Transform = Matrix.Identity;
    }

    public Keyframe(TimeSpan time, int boneIndex, Matrix transform)
    {
        Time = time;
        BoneIndex = boneIndex;
        Transform = transform;
    }
}
