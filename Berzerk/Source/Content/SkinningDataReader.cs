using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Berzerk.Content;

/// <summary>
/// ContentTypeReader for deserializing SkinningData from XNB format at runtime.
/// The binary format read here MUST match the order written by SkinningDataWriter exactly.
/// </summary>
public class SkinningDataReader : ContentTypeReader<SkinningData>
{
    /// <summary>
    /// Reads the SkinningData from the XNB input stream.
    /// Format must match SkinningDataWriter.Write() exactly.
    /// </summary>
    protected override SkinningData Read(ContentReader input, SkinningData existingInstance)
    {
        // --- Skeleton data ---
        // All three arrays share the same count (one entry per bone).
        int boneCount = input.ReadInt32(); // [Int32] boneCount

        // Bind pose: local-space bone rest transforms, boneCount matrices
        var bindPose = new List<Matrix>(boneCount);
        for (int i = 0; i < boneCount; i++)
            bindPose.Add(input.ReadMatrix()); // [Matrix] bindPose[i]

        // Inverse bind pose: vertex-to-bonespace transforms, boneCount matrices
        var inverseBindPose = new List<Matrix>(boneCount);
        for (int i = 0; i < boneCount; i++)
            inverseBindPose.Add(input.ReadMatrix()); // [Matrix] inverseBindPose[i]

        // Skeleton hierarchy: parent bone index per bone, boneCount ints
        var skeletonHierarchy = new List<int>(boneCount);
        for (int i = 0; i < boneCount; i++)
            skeletonHierarchy.Add(input.ReadInt32()); // [Int32] skeletonHierarchy[i]

        // --- Animation clips ---
        int clipCount = input.ReadInt32(); // [Int32] clipCount
        var animationClips = new Dictionary<string, SkinningDataClip>(clipCount);

        for (int i = 0; i < clipCount; i++)
        {
            // Clip name (dictionary key)
            string clipName = input.ReadString(); // [String] clipName

            // Clip duration from ticks (lossless TimeSpan deserialization)
            var duration = TimeSpan.FromTicks(input.ReadInt64()); // [Int64] durationTicks

            // Keyframes: flat list for all bones in this clip
            int keyframeCount = input.ReadInt32(); // [Int32] keyframeCount
            var keyframes = new List<SkinningDataKeyframe>(keyframeCount);

            for (int j = 0; j < keyframeCount; j++)
            {
                int bone = input.ReadInt32();              // [Int32]  bone index
                var time = TimeSpan.FromTicks(input.ReadInt64()); // [Int64]  time ticks
                var transform = input.ReadMatrix();        // [Matrix] local-space bone transform

                keyframes.Add(new SkinningDataKeyframe(bone, time, transform));
            }

            animationClips[clipName] = new SkinningDataClip(duration, keyframes);
        }

        return new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);
    }
}
