using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Berzerk.Content;

namespace Berzerk.Graphics;

/// <summary>
/// Plays back skinned animations using the canonical XNA three-stage transform pipeline:
///   Stage 1 (UpdateBoneTransforms): Decode keyframes into local-space bone transforms
///   Stage 2 (UpdateWorldTransforms): Compose hierarchy to produce model-space transforms
///   Stage 3 (UpdateSkinTransforms): Multiply by inverse bind pose for GPU skinning
///
/// Direct port of the XNA SkinningSample_4_0 AnimationPlayer, adapted to use
/// SkinningData/SkinningDataClip/SkinningDataKeyframe types.
/// </summary>
public class AnimationPlayer
{
    private readonly SkinningData _skinningData;

    // Stage 1: Local-space bone transforms (from keyframes)
    private readonly Matrix[] _boneTransforms;

    // Stage 2: Model-space transforms (after hierarchy composition)
    private readonly Matrix[] _worldTransforms;

    // Stage 3: Final skinning transforms (after inverse bind pose)
    private readonly Matrix[] _skinTransforms;

    // Playback state
    private SkinningDataClip? _currentClip;
    private TimeSpan _currentTime;
    private int _currentKeyframe;

    /// <summary>
    /// Creates a new AnimationPlayer for the given skinning data.
    /// Allocates transform arrays sized to the number of bones.
    /// </summary>
    public AnimationPlayer(SkinningData skinningData)
    {
        _skinningData = skinningData ?? throw new ArgumentNullException(nameof(skinningData));

        int boneCount = skinningData.BindPose.Count;
        _boneTransforms = new Matrix[boneCount];
        _worldTransforms = new Matrix[boneCount];
        _skinTransforms = new Matrix[boneCount];
    }

    /// <summary>
    /// Starts playing a new animation clip from the beginning.
    /// Resets playback position and copies bind pose as initial bone transforms.
    /// </summary>
    public void StartClip(SkinningDataClip clip)
    {
        _currentClip = clip ?? throw new ArgumentNullException(nameof(clip));
        _currentTime = TimeSpan.Zero;
        _currentKeyframe = 0;

        // Initialize bone transforms to bind pose (rest position)
        _skinningData.BindPose.CopyTo(_boneTransforms, 0);
    }

    /// <summary>
    /// Convenience method that calls all three pipeline stages in order.
    /// </summary>
    public void Update(TimeSpan time, bool relativeToCurrentTime, Matrix rootTransform)
    {
        UpdateBoneTransforms(time, relativeToCurrentTime);
        UpdateWorldTransforms(rootTransform);
        UpdateSkinTransforms();
    }

    /// <summary>
    /// Stage 1: Decode keyframes into local-space bone transforms.
    /// Scans the flat keyframe list forward, overwriting boneTransforms directly.
    /// Handles looping via while-subtract and backwards-time detection.
    /// </summary>
    public void UpdateBoneTransforms(TimeSpan time, bool relativeToCurrentTime)
    {
        if (_currentClip == null)
            throw new InvalidOperationException("StartClip must be called before UpdateBoneTransforms.");

        // If relative, add elapsed time to current position
        if (relativeToCurrentTime)
        {
            time += _currentTime;
        }

        // Loop handling: wrap time within clip duration
        while (time >= _currentClip.Duration)
            time -= _currentClip.Duration;

        // If time went backwards (loop wrap or seek), reset keyframe scan
        if (time < _currentTime)
        {
            _currentKeyframe = 0;
            _skinningData.BindPose.CopyTo(_boneTransforms, 0);
        }

        _currentTime = time;

        // Scan forward through flat keyframe list
        IList<SkinningDataKeyframe> keyframes = _currentClip.Keyframes;

        while (_currentKeyframe < keyframes.Count)
        {
            SkinningDataKeyframe keyframe = keyframes[_currentKeyframe];

            // Stop if we've reached keyframes beyond current time
            if (keyframe.Time > _currentTime)
                break;

            // Apply keyframe: overwrite bone's local transform
            _boneTransforms[keyframe.Bone] = keyframe.Transform;
            _currentKeyframe++;
        }
    }

    /// <summary>
    /// Stage 2: Compose bone hierarchy to produce model-space transforms.
    /// Root bone is multiplied by rootTransform; children multiply by parent's world transform.
    /// </summary>
    public void UpdateWorldTransforms(Matrix rootTransform)
    {
        // Root bone
        _worldTransforms[0] = _boneTransforms[0] * rootTransform;

        // Child bones: local * parent world
        for (int bone = 1; bone < _worldTransforms.Length; bone++)
        {
            int parentBone = _skinningData.SkeletonHierarchy[bone];
            _worldTransforms[bone] = _boneTransforms[bone] * _worldTransforms[parentBone];
        }
    }

    /// <summary>
    /// Stage 3: Multiply by inverse bind pose to produce final GPU skinning transforms.
    /// skinTransform = inverseBindPose * worldTransform
    /// </summary>
    public void UpdateSkinTransforms()
    {
        for (int bone = 0; bone < _skinTransforms.Length; bone++)
        {
            _skinTransforms[bone] = _skinningData.InverseBindPose[bone] * _worldTransforms[bone];
        }
    }

    /// <summary>
    /// Gets the final skinning transform matrices ready for GPU upload.
    /// One matrix per bone, combining inverse bind pose with animated world transform.
    /// </summary>
    public Matrix[] GetSkinTransforms()
    {
        return _skinTransforms;
    }

    /// <summary>
    /// The currently playing animation clip, or null if no clip has been started.
    /// </summary>
    public SkinningDataClip? CurrentClip => _currentClip;
}
