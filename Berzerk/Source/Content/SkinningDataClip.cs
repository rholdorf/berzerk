using System;
using System.Collections.Generic;

namespace Berzerk.Content;

/// <summary>
/// Runtime representation of a single named animation clip (e.g., "idle", "walk", "attack")
/// for skinned animation. Contains all keyframes for all bones in a flat list sorted by time.
/// The animation player scans through this list sequentially during playback.
/// </summary>
public class SkinningDataClip
{
    /// <summary>
    /// Total duration of the animation clip.
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// All keyframes for all bones in this clip, sorted by time.
    /// Each keyframe contains a bone index, time, and local-space transform.
    /// The flat list structure is cache-friendly and simple for sequential playback.
    /// </summary>
    public List<SkinningDataKeyframe> Keyframes { get; private set; }

    /// <summary>
    /// Creates a new animation clip.
    /// </summary>
    /// <param name="duration">Total clip duration.</param>
    /// <param name="keyframes">Flat list of keyframes for all bones, sorted by time.</param>
    public SkinningDataClip(TimeSpan duration, List<SkinningDataKeyframe> keyframes)
    {
        Duration = duration;
        Keyframes = keyframes;
    }
}
