using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Xunit;

// Alias pipeline-side and runtime-side types to avoid ambiguity.
// Both assemblies define SkinningData, SkinningDataClip, SkinningDataKeyframe
// in different namespaces (Berzerk.ContentPipeline vs Berzerk.Content).
using PipelineSkinningData = Berzerk.ContentPipeline.SkinningData;
using PipelineClip = Berzerk.ContentPipeline.SkinningDataClip;
using PipelineKeyframe = Berzerk.ContentPipeline.SkinningDataKeyframe;
using RuntimeSkinningData = Berzerk.Content.SkinningData;
using RuntimeClip = Berzerk.Content.SkinningDataClip;
using RuntimeKeyframe = Berzerk.Content.SkinningDataKeyframe;

namespace Berzerk.Tests;

/// <summary>
/// Round-trip serialization tests that prove the SkinningData binary format contract
/// is correct: data serialized in the writer's format is faithfully reconstructed
/// when read in the reader's format.
///
/// Since MonoGame's ContentWriter/ContentReader require internal pipeline infrastructure,
/// these tests use BinaryWriter/BinaryReader following the EXACT same binary format
/// as SkinningDataWriter and SkinningDataReader.
/// </summary>
public class SkinningDataRoundTripTests
{
    private const float Epsilon = 1e-6f;

    #region Helper Methods

    /// <summary>
    /// Writes a Matrix as 16 floats in row-major order (M11..M44).
    /// This matches what MonoGame's ContentWriter.Write(Matrix) does internally.
    /// </summary>
    private static void WriteMatrix(BinaryWriter writer, Matrix m)
    {
        writer.Write(m.M11); writer.Write(m.M12); writer.Write(m.M13); writer.Write(m.M14);
        writer.Write(m.M21); writer.Write(m.M22); writer.Write(m.M23); writer.Write(m.M24);
        writer.Write(m.M31); writer.Write(m.M32); writer.Write(m.M33); writer.Write(m.M34);
        writer.Write(m.M41); writer.Write(m.M42); writer.Write(m.M43); writer.Write(m.M44);
    }

    /// <summary>
    /// Reads 16 floats and constructs a Matrix in row-major order (M11..M44).
    /// This matches what MonoGame's ContentReader.ReadMatrix() does internally.
    /// </summary>
    private static Matrix ReadMatrix(BinaryReader reader)
    {
        return new Matrix(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
        );
    }

    /// <summary>
    /// Serializes a pipeline-side SkinningData to binary, mirroring SkinningDataWriter.Write() exactly.
    /// Format: boneCount, bindPose[], inverseBindPose[], skeletonHierarchy[], clipCount,
    ///         then for each clip: name, durationTicks, keyframeCount, keyframes[].
    /// </summary>
    private static void WriteSkinningData(BinaryWriter writer, PipelineSkinningData data)
    {
        // --- Skeleton data ---
        int boneCount = data.BindPose.Count;
        writer.Write(boneCount);

        for (int i = 0; i < boneCount; i++)
            WriteMatrix(writer, data.BindPose[i]);

        for (int i = 0; i < boneCount; i++)
            WriteMatrix(writer, data.InverseBindPose[i]);

        for (int i = 0; i < boneCount; i++)
            writer.Write(data.SkeletonHierarchy[i]);

        // --- Animation clips ---
        writer.Write(data.AnimationClips.Count);

        foreach (var kvp in data.AnimationClips)
        {
            writer.Write(kvp.Key);                    // clip name (7-bit length-prefixed string)
            writer.Write(kvp.Value.Duration.Ticks);    // duration as ticks
            writer.Write(kvp.Value.Keyframes.Count);   // keyframe count

            foreach (var kf in kvp.Value.Keyframes)
            {
                writer.Write(kf.Bone);
                writer.Write(kf.Time.Ticks);
                WriteMatrix(writer, kf.Transform);
            }
        }
    }

    /// <summary>
    /// Deserializes a runtime-side SkinningData from binary, mirroring SkinningDataReader.Read() exactly.
    /// </summary>
    private static RuntimeSkinningData ReadSkinningData(BinaryReader reader)
    {
        // --- Skeleton data ---
        int boneCount = reader.ReadInt32();

        var bindPose = new List<Matrix>(boneCount);
        for (int i = 0; i < boneCount; i++)
            bindPose.Add(ReadMatrix(reader));

        var inverseBindPose = new List<Matrix>(boneCount);
        for (int i = 0; i < boneCount; i++)
            inverseBindPose.Add(ReadMatrix(reader));

        var skeletonHierarchy = new List<int>(boneCount);
        for (int i = 0; i < boneCount; i++)
            skeletonHierarchy.Add(reader.ReadInt32());

        // --- Animation clips ---
        int clipCount = reader.ReadInt32();
        var animationClips = new Dictionary<string, RuntimeClip>(clipCount);

        for (int i = 0; i < clipCount; i++)
        {
            string clipName = reader.ReadString();
            var duration = TimeSpan.FromTicks(reader.ReadInt64());

            int keyframeCount = reader.ReadInt32();
            var keyframes = new List<RuntimeKeyframe>(keyframeCount);

            for (int j = 0; j < keyframeCount; j++)
            {
                int bone = reader.ReadInt32();
                var time = TimeSpan.FromTicks(reader.ReadInt64());
                var transform = ReadMatrix(reader);

                keyframes.Add(new RuntimeKeyframe(bone, time, transform));
            }

            animationClips[clipName] = new RuntimeClip(duration, keyframes);
        }

        return new RuntimeSkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);
    }

    /// <summary>
    /// Compares two matrices element-by-element within a float tolerance.
    /// </summary>
    private static bool MatricesEqual(Matrix a, Matrix b, float tolerance)
    {
        return Math.Abs(a.M11 - b.M11) < tolerance && Math.Abs(a.M12 - b.M12) < tolerance
            && Math.Abs(a.M13 - b.M13) < tolerance && Math.Abs(a.M14 - b.M14) < tolerance
            && Math.Abs(a.M21 - b.M21) < tolerance && Math.Abs(a.M22 - b.M22) < tolerance
            && Math.Abs(a.M23 - b.M23) < tolerance && Math.Abs(a.M24 - b.M24) < tolerance
            && Math.Abs(a.M31 - b.M31) < tolerance && Math.Abs(a.M32 - b.M32) < tolerance
            && Math.Abs(a.M33 - b.M33) < tolerance && Math.Abs(a.M34 - b.M34) < tolerance
            && Math.Abs(a.M41 - b.M41) < tolerance && Math.Abs(a.M42 - b.M42) < tolerance
            && Math.Abs(a.M43 - b.M43) < tolerance && Math.Abs(a.M44 - b.M44) < tolerance;
    }

    /// <summary>
    /// Performs a write-then-read round trip on pipeline-side SkinningData,
    /// returning the deserialized runtime-side SkinningData.
    /// </summary>
    private static RuntimeSkinningData RoundTrip(PipelineSkinningData original)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            WriteSkinningData(writer, original);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        return ReadSkinningData(reader);
    }

    /// <summary>
    /// Asserts that a runtime-side SkinningData matches the original pipeline-side data.
    /// </summary>
    private static void AssertSkinningDataEqual(PipelineSkinningData original, RuntimeSkinningData result)
    {
        // Bone count consistency
        Assert.Equal(original.BindPose.Count, result.BindPose.Count);
        Assert.Equal(original.InverseBindPose.Count, result.InverseBindPose.Count);
        Assert.Equal(original.SkeletonHierarchy.Count, result.SkeletonHierarchy.Count);

        int boneCount = original.BindPose.Count;

        // Bind pose matrices
        for (int i = 0; i < boneCount; i++)
        {
            Assert.True(
                MatricesEqual(original.BindPose[i], result.BindPose[i], Epsilon),
                $"BindPose[{i}] mismatch");
        }

        // Inverse bind pose matrices
        for (int i = 0; i < boneCount; i++)
        {
            Assert.True(
                MatricesEqual(original.InverseBindPose[i], result.InverseBindPose[i], Epsilon),
                $"InverseBindPose[{i}] mismatch");
        }

        // Skeleton hierarchy
        for (int i = 0; i < boneCount; i++)
        {
            Assert.Equal(original.SkeletonHierarchy[i], result.SkeletonHierarchy[i]);
        }

        // Animation clips
        Assert.Equal(original.AnimationClips.Count, result.AnimationClips.Count);

        foreach (var kvp in original.AnimationClips)
        {
            Assert.True(result.AnimationClips.ContainsKey(kvp.Key), $"Missing clip: {kvp.Key}");
            var originalClip = kvp.Value;
            var resultClip = result.AnimationClips[kvp.Key];

            Assert.Equal(originalClip.Duration.Ticks, resultClip.Duration.Ticks);
            Assert.Equal(originalClip.Keyframes.Count, resultClip.Keyframes.Count);

            for (int j = 0; j < originalClip.Keyframes.Count; j++)
            {
                var origKf = originalClip.Keyframes[j];
                var resKf = resultClip.Keyframes[j];

                Assert.Equal(origKf.Bone, resKf.Bone);
                Assert.Equal(origKf.Time.Ticks, resKf.Time.Ticks);
                Assert.True(
                    MatricesEqual(origKf.Transform, resKf.Transform, Epsilon),
                    $"Clip '{kvp.Key}' Keyframe[{j}] transform mismatch");
            }
        }
    }

    #endregion

    #region Test Cases

    [Fact]
    public void RoundTrip_SingleBone_SingleClip_SingleKeyframe()
    {
        // Simplest possible case: 1 bone, 1 clip, 1 keyframe.
        var bindPose = new List<Matrix> { Matrix.CreateTranslation(0, 1, 0) };
        var inverseBindPose = new List<Matrix> { Matrix.CreateTranslation(0, -1, 0) };
        var skeletonHierarchy = new List<int> { -1 }; // root bone

        var keyframes = new List<PipelineKeyframe>
        {
            new PipelineKeyframe(0, TimeSpan.Zero, Matrix.Identity)
        };
        var clip = new PipelineClip(TimeSpan.FromSeconds(1.0), keyframes);
        var clips = new Dictionary<string, PipelineClip> { { "idle", clip } };

        var original = new PipelineSkinningData(clips, bindPose, inverseBindPose, skeletonHierarchy);
        var result = RoundTrip(original);

        AssertSkinningDataEqual(original, result);
    }

    [Fact]
    public void RoundTrip_MultipleBones_MultipleClips()
    {
        // Realistic case: 3 bones with parent hierarchy, 2 clips with multiple keyframes.
        var bindPose = new List<Matrix>
        {
            Matrix.CreateTranslation(0, 0, 0),       // root
            Matrix.CreateTranslation(0, 1.5f, 0),    // bone1 -> root
            Matrix.CreateTranslation(0, 1.0f, 0),    // bone2 -> bone1
        };

        var inverseBindPose = new List<Matrix>
        {
            Matrix.CreateTranslation(0, 0, 0),
            Matrix.CreateTranslation(0, -1.5f, 0),
            Matrix.CreateTranslation(0, -2.5f, 0),
        };

        var skeletonHierarchy = new List<int> { -1, 0, 1 }; // root, bone1->root, bone2->bone1

        // "idle" clip: 4 keyframes across 3 bones
        var idleKeyframes = new List<PipelineKeyframe>
        {
            new PipelineKeyframe(0, TimeSpan.Zero, Matrix.Identity),
            new PipelineKeyframe(1, TimeSpan.Zero, Matrix.CreateRotationY(0.1f)),
            new PipelineKeyframe(0, TimeSpan.FromMilliseconds(500), Matrix.CreateRotationY(0.05f)),
            new PipelineKeyframe(2, TimeSpan.FromMilliseconds(500), Matrix.CreateTranslation(0, 0.1f, 0)),
        };
        var idleClip = new PipelineClip(TimeSpan.FromSeconds(1.0), idleKeyframes);

        // "walk" clip: 6 keyframes across 3 bones
        var walkKeyframes = new List<PipelineKeyframe>
        {
            new PipelineKeyframe(0, TimeSpan.Zero, Matrix.Identity),
            new PipelineKeyframe(1, TimeSpan.Zero, Matrix.CreateRotationY(0.2f)),
            new PipelineKeyframe(2, TimeSpan.Zero, Matrix.CreateTranslation(0.5f, 0, 0)),
            new PipelineKeyframe(0, TimeSpan.FromMilliseconds(333), Matrix.CreateRotationY(0.3f)),
            new PipelineKeyframe(1, TimeSpan.FromMilliseconds(333), Matrix.CreateTranslation(0, 0.2f, 0)),
            new PipelineKeyframe(2, TimeSpan.FromMilliseconds(667), Matrix.CreateRotationY(-0.1f)),
        };
        var walkClip = new PipelineClip(TimeSpan.FromSeconds(2.0), walkKeyframes);

        var clips = new Dictionary<string, PipelineClip>
        {
            { "idle", idleClip },
            { "walk", walkClip },
        };

        var original = new PipelineSkinningData(clips, bindPose, inverseBindPose, skeletonHierarchy);
        var result = RoundTrip(original);

        AssertSkinningDataEqual(original, result);
    }

    [Fact]
    public void RoundTrip_65Bones_RealisticSize()
    {
        // Scale test: 65 bones (typical Mixamo skeleton), 1 clip with 65*30=1950 keyframes.
        const int boneCount = 65;
        const int keyframesPerBone = 30;

        var bindPose = new List<Matrix>(boneCount);
        var inverseBindPose = new List<Matrix>(boneCount);
        var skeletonHierarchy = new List<int>(boneCount);

        for (int i = 0; i < boneCount; i++)
        {
            bindPose.Add(Matrix.CreateTranslation(i * 0.1f, i * 0.05f, i * 0.02f));
            inverseBindPose.Add(Matrix.CreateTranslation(-i * 0.1f, -i * 0.05f, -i * 0.02f));
            skeletonHierarchy.Add(i == 0 ? -1 : i - 1); // linear chain for simplicity
        }

        // 1950 keyframes: 30 keyframes per bone, sorted by time
        var keyframes = new List<PipelineKeyframe>(boneCount * keyframesPerBone);
        var clipDuration = TimeSpan.FromSeconds(2.0);
        var timeStep = clipDuration.Ticks / keyframesPerBone;

        for (int frame = 0; frame < keyframesPerBone; frame++)
        {
            var time = TimeSpan.FromTicks(timeStep * frame);
            for (int bone = 0; bone < boneCount; bone++)
            {
                float angle = (float)(frame * 0.1 + bone * 0.05);
                keyframes.Add(new PipelineKeyframe(bone, time, Matrix.CreateRotationY(angle)));
            }
        }

        var clip = new PipelineClip(clipDuration, keyframes);
        var clips = new Dictionary<string, PipelineClip> { { "mixamo_anim", clip } };

        var original = new PipelineSkinningData(clips, bindPose, inverseBindPose, skeletonHierarchy);
        var result = RoundTrip(original);

        AssertSkinningDataEqual(original, result);

        // Verify specific counts to confirm scale
        Assert.Equal(boneCount, result.BindPose.Count);
        Assert.Equal(boneCount * keyframesPerBone, result.AnimationClips["mixamo_anim"].Keyframes.Count);
    }

    [Fact]
    public void Constructor_MismatchedArrayLengths_Throws()
    {
        // Verifies that SkinningData constructor rejects mismatched array lengths.
        // Test both pipeline-side and runtime-side constructors.
        var clips = new Dictionary<string, PipelineClip>();
        var bindPose3 = new List<Matrix> { Matrix.Identity, Matrix.Identity, Matrix.Identity };
        var inverseBindPose2 = new List<Matrix> { Matrix.Identity, Matrix.Identity };
        var hierarchy3 = new List<int> { -1, 0, 1 };

        // Pipeline-side: 3 bind pose, 2 inverse bind pose, 3 hierarchy -> mismatch
        Assert.Throws<ArgumentException>(() =>
            new PipelineSkinningData(clips, bindPose3, inverseBindPose2, hierarchy3));

        // Runtime-side: same mismatch
        var runtimeClips = new Dictionary<string, RuntimeClip>();
        Assert.Throws<ArgumentException>(() =>
            new RuntimeSkinningData(runtimeClips, bindPose3, inverseBindPose2, hierarchy3));
    }

    #endregion
}
