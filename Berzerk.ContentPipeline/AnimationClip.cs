using System;
using System.Collections.Generic;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Represents a single named animation clip (e.g., "idle", "walk", "attack").
/// Contains keyframes organized by bone name.
/// </summary>
public class AnimationClip
{
    /// <summary>
    /// Name of the animation clip (e.g., "idle", "walk").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Total duration of the animation clip.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Keyframes organized by bone name.
    /// Each bone has a list of keyframes that define its animation over time.
    /// </summary>
    public Dictionary<string, List<Keyframe>> Keyframes { get; set; }

    public AnimationClip()
    {
        Name = string.Empty;
        Duration = TimeSpan.Zero;
        Keyframes = new Dictionary<string, List<Keyframe>>();
    }

    public AnimationClip(string name, TimeSpan duration)
    {
        Name = name;
        Duration = duration;
        Keyframes = new Dictionary<string, List<Keyframe>>();
    }
}
