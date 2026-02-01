using System;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Berzerk.ContentPipeline;

/// <summary>
/// ContentTypeWriter for serializing AnimationData to XNB format.
/// This writer is used at build-time to convert AnimationData into binary format.
/// </summary>
[ContentTypeWriter]
public class AnimationDataWriter : ContentTypeWriter<AnimationData>
{
    /// <summary>
    /// Writes the AnimationData to the XNB output stream.
    /// </summary>
    protected override void Write(ContentWriter output, AnimationData value)
    {
        // Write bone indices mapping
        output.Write(value.BoneIndices.Count);
        foreach (var kvp in value.BoneIndices)
        {
            output.Write(kvp.Key);   // bone name
            output.Write(kvp.Value); // bone index
        }

        // Write number of animation clips
        output.Write(value.Clips.Count);

        // Write each animation clip
        foreach (var clipKvp in value.Clips)
        {
            var clip = clipKvp.Value;

            // Write clip name
            output.Write(clip.Name);

            // Write clip duration
            output.Write((long)clip.Duration.Ticks);

            // Write number of bone keyframe lists
            output.Write(clip.Keyframes.Count);

            // Write keyframes for each bone
            foreach (var keyframeKvp in clip.Keyframes)
            {
                // Write bone name
                output.Write(keyframeKvp.Key);

                // Write number of keyframes for this bone
                var keyframes = keyframeKvp.Value;
                output.Write(keyframes.Count);

                // Write each keyframe
                foreach (var keyframe in keyframes)
                {
                    output.Write((long)keyframe.Time.Ticks);
                    output.Write(keyframe.BoneIndex);
                    output.Write(keyframe.Transform);
                }
            }
        }
    }

    /// <summary>
    /// Gets the runtime type name for the corresponding ContentTypeReader.
    /// This tells MonoGame which reader to use at runtime to deserialize this data.
    /// </summary>
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        // The runtime reader will be in the game project (Berzerk assembly)
        // Format: Namespace.ClassName, AssemblyName
        return "Berzerk.Content.AnimationDataReader, Berzerk";
    }

    /// <summary>
    /// Gets the runtime type that this writer serializes.
    /// </summary>
    public override string GetRuntimeType(TargetPlatform targetPlatform)
    {
        // The runtime AnimationData type will be in the game project
        return "Berzerk.Content.AnimationData, Berzerk";
    }
}
