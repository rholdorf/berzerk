using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Berzerk.ContentPipeline;

/// <summary>
/// ContentTypeWriter for serializing SkinningData to XNB format at build time.
/// The binary format written here MUST be read in exactly the same order by
/// <see cref="Berzerk.Content.SkinningDataReader"/> at runtime.
/// </summary>
[ContentTypeWriter]
public class SkinningDataWriter : ContentTypeWriter<SkinningData>
{
    /// <summary>
    /// Writes the SkinningData to the XNB output stream.
    /// Binary format documented inline -- SkinningDataReader.Read() MUST match this exactly.
    /// </summary>
    protected override void Write(ContentWriter output, SkinningData value)
    {
        // --- Skeleton data ---
        // All three arrays share the same count (one entry per bone).
        // Write the count once; the reader uses it for all three arrays.
        int boneCount = value.BindPose.Count;
        output.Write(boneCount); // [Int32] boneCount

        // Bind pose: local-space bone rest transforms, boneCount matrices
        for (int i = 0; i < boneCount; i++)
            output.Write(value.BindPose[i]); // [Matrix] bindPose[i]

        // Inverse bind pose: vertex-to-bonespace transforms, boneCount matrices
        for (int i = 0; i < boneCount; i++)
            output.Write(value.InverseBindPose[i]); // [Matrix] inverseBindPose[i]

        // Skeleton hierarchy: parent bone index per bone, boneCount ints
        for (int i = 0; i < boneCount; i++)
            output.Write(value.SkeletonHierarchy[i]); // [Int32] skeletonHierarchy[i]

        // --- Animation clips ---
        output.Write(value.AnimationClips.Count); // [Int32] clipCount

        foreach (var kvp in value.AnimationClips)
        {
            // Clip name (dictionary key)
            output.Write(kvp.Key); // [String] clipName

            // Clip duration as ticks (lossless TimeSpan serialization)
            output.Write(kvp.Value.Duration.Ticks); // [Int64] durationTicks

            // Keyframes: flat list for all bones in this clip
            output.Write(kvp.Value.Keyframes.Count); // [Int32] keyframeCount

            foreach (var keyframe in kvp.Value.Keyframes)
            {
                output.Write(keyframe.Bone);           // [Int32]  bone index
                output.Write(keyframe.Time.Ticks);     // [Int64]  time ticks
                output.Write(keyframe.Transform);      // [Matrix] local-space bone transform
            }
        }
    }

    /// <summary>
    /// Gets the runtime type name for the corresponding ContentTypeReader.
    /// This tells MonoGame which reader to use at runtime to deserialize this data.
    /// CRITICAL: Must match the exact namespace, class name, and assembly name.
    /// </summary>
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        // Format: Namespace.ClassName, AssemblyName
        // Namespace: Berzerk.Content (runtime namespace)
        // Class: SkinningDataReader (reader class)
        // Assembly: Berzerk (game project assembly name)
        return "Berzerk.Content.SkinningDataReader, Berzerk";
    }

    /// <summary>
    /// Gets the runtime type that this writer serializes.
    /// CRITICAL: Must match the exact namespace, class name, and assembly name.
    /// </summary>
    public override string GetRuntimeType(TargetPlatform targetPlatform)
    {
        // Format: Namespace.ClassName, AssemblyName
        // Namespace: Berzerk.Content (runtime namespace)
        // Class: SkinningData (runtime data type)
        // Assembly: Berzerk (game project assembly name)
        return "Berzerk.Content.SkinningData, Berzerk";
    }
}
