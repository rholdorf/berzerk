using System.Collections.Generic;

namespace Berzerk.Content;

/// <summary>
/// Runtime representation of all animation data in a model.
/// This gets deserialized from Model.Tag when loaded from XNB.
/// </summary>
public class AnimationData
{
    /// <summary>
    /// Dictionary of animation clips, keyed by clip name.
    /// </summary>
    public Dictionary<string, AnimationClip> Clips { get; set; }

    /// <summary>
    /// Mapping from bone name to bone index for runtime lookup.
    /// This allows keyframes to reference bones by index for efficiency.
    /// </summary>
    public Dictionary<string, int> BoneIndices { get; set; }

    public AnimationData()
    {
        Clips = new Dictionary<string, AnimationClip>();
        BoneIndices = new Dictionary<string, int>();
    }

    /// <summary>
    /// Gets an animation clip by name, or null if not found.
    /// </summary>
    public AnimationClip? GetClip(string name)
    {
        return Clips.TryGetValue(name, out var clip) ? clip : null;
    }

    /// <summary>
    /// Gets the bone index for a bone name, or -1 if not found.
    /// </summary>
    public int GetBoneIndex(string boneName)
    {
        return BoneIndices.TryGetValue(boneName, out var index) ? index : -1;
    }
}
