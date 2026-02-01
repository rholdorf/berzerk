using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Berzerk.Content;

/// <summary>
/// ContentTypeReader for deserializing AnimationData from XNB format.
/// This reader is used at runtime to load animation data from compiled content.
/// MUST match the format written by AnimationDataWriter exactly.
/// </summary>
public class AnimationDataReader : ContentTypeReader<AnimationData>
{
    /// <summary>
    /// Reads the AnimationData from the XNB input stream.
    /// Format must match AnimationDataWriter.Write() exactly.
    /// </summary>
    protected override AnimationData Read(ContentReader input, AnimationData existingInstance)
    {
        var animationData = existingInstance ?? new AnimationData();

        // Read bone indices mapping
        int boneCount = input.ReadInt32();
        animationData.BoneIndices = new Dictionary<string, int>(boneCount);
        for (int i = 0; i < boneCount; i++)
        {
            string boneName = input.ReadString();
            int boneIndex = input.ReadInt32();
            animationData.BoneIndices[boneName] = boneIndex;
        }

        // Read number of animation clips
        int clipCount = input.ReadInt32();
        animationData.Clips = new Dictionary<string, AnimationClip>(clipCount);

        // Read each animation clip
        for (int i = 0; i < clipCount; i++)
        {
            // Read clip name
            string clipName = input.ReadString();

            // Read clip duration
            long durationTicks = input.ReadInt64();
            TimeSpan duration = TimeSpan.FromTicks(durationTicks);

            var clip = new AnimationClip(clipName, duration);

            // Read number of bone keyframe lists
            int boneKeyframeCount = input.ReadInt32();
            clip.Keyframes = new Dictionary<string, List<Keyframe>>(boneKeyframeCount);

            // Read keyframes for each bone
            for (int j = 0; j < boneKeyframeCount; j++)
            {
                // Read bone name
                string boneName = input.ReadString();

                // Read number of keyframes for this bone
                int keyframeCount = input.ReadInt32();
                var keyframes = new List<Keyframe>(keyframeCount);

                // Read each keyframe
                for (int k = 0; k < keyframeCount; k++)
                {
                    long timeTicks = input.ReadInt64();
                    TimeSpan time = TimeSpan.FromTicks(timeTicks);
                    int boneIndex = input.ReadInt32();
                    Matrix transform = input.ReadMatrix();

                    keyframes.Add(new Keyframe(time, boneIndex, transform));
                }

                clip.Keyframes[boneName] = keyframes;
            }

            animationData.Clips[clipName] = clip;
        }

        return animationData;
    }
}
