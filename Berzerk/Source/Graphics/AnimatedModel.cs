using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Berzerk.Content;

namespace Berzerk.Graphics;

/// <summary>
/// Handles loading and rendering of 3D models with animation support.
/// Extracts animation data from Model.Tag and provides animation playback.
/// </summary>
public class AnimatedModel
{
    private Model? _model;
    private Matrix[] _boneTransforms = Array.Empty<Matrix>();
    private AnimationData? _animationData;

    private string? _currentClipName;
    private TimeSpan _currentTime = TimeSpan.Zero;
    private int _debugFrameCount = 0;

    /// <summary>
    /// Loads a model and its animation data from content.
    /// </summary>
    /// <param name="content">ContentManager to load from</param>
    /// <param name="modelPath">Asset path (without extension)</param>
    public void LoadContent(ContentManager content, string modelPath)
    {
        // Load model
        _model = content.Load<Model>(modelPath);

        // Initialize bone transforms array
        _boneTransforms = new Matrix[_model.Bones.Count];
        _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        // Extract animation data from Model.Tag
        _animationData = _model.Tag as AnimationData;

        if (_animationData != null)
        {
            Console.WriteLine($"AnimatedModel: Loaded '{modelPath}' with {_animationData.Clips.Count} animations:");
            foreach (var clipName in _animationData.Clips.Keys)
            {
                var clip = _animationData.Clips[clipName];
                Console.WriteLine($"  - {clipName} (duration: {clip.Duration.TotalSeconds:F2}s)");
            }
        }
        else
        {
            Console.WriteLine($"AnimatedModel: Loaded '{modelPath}' (static model, no animations)");
        }
    }

    /// <summary>
    /// Updates animation playback.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_animationData == null || _currentClipName == null || _model == null)
            return;

        var clip = _animationData.GetClip(_currentClipName);
        if (clip == null)
            return;

        // Advance animation time
        _currentTime += gameTime.ElapsedGameTime;

        // Loop animation when reaching end
        if (_currentTime >= clip.Duration)
        {
            _currentTime = TimeSpan.Zero;
        }

        // Apply keyframe animation to bones
        ApplyKeyframes(clip);
    }

    /// <summary>
    /// Draws the model with current bone transforms.
    /// </summary>
    public void Draw(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
    {
        if (_model == null)
            return;

        // Enable backface culling and depth testing
        RasterizerState rasterizerState = new RasterizerState
        {
            CullMode = CullMode.CullCounterClockwiseFace
        };
        graphicsDevice.RasterizerState = rasterizerState;

        // Ensure depth testing is enabled for proper occlusion
        graphicsDevice.DepthStencilState = DepthStencilState.Default;

        // Render spheres first, then other meshes (for proper depth sorting)
        // This ensures armor pieces occlude the joint spheres correctly
        var sphereMeshes = new List<ModelMesh>();
        var otherMeshes = new List<ModelMesh>();

        foreach (var mesh in _model.Meshes)
        {
            string meshName = mesh.Name.ToLower();
            if (meshName.Contains("sphere") || meshName.Contains("joint"))
            {
                sphereMeshes.Add(mesh);
            }
            else
            {
                otherMeshes.Add(mesh);
            }
        }

        // Draw spheres first
        foreach (var mesh in sphereMeshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.World = _boneTransforms[mesh.ParentBone.Index] * world;
                effect.View = view;
                effect.Projection = projection;

                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
            }

            mesh.Draw();
        }

        // Draw other meshes on top (will occlude spheres where appropriate)
        foreach (var mesh in otherMeshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.World = _boneTransforms[mesh.ParentBone.Index] * world;
                effect.View = view;
                effect.Projection = projection;

                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
            }

            mesh.Draw();
        }
    }

    /// <summary>
    /// Starts playing a specific animation clip.
    /// </summary>
    public void PlayAnimation(string clipName)
    {
        if (_animationData == null)
        {
            Console.WriteLine($"AnimatedModel: Cannot play animation '{clipName}' - no animation data");
            return;
        }

        var clip = _animationData.GetClip(clipName);
        if (clip == null)
        {
            Console.WriteLine($"AnimatedModel: Animation '{clipName}' not found");
            return;
        }

        _currentClipName = clipName;
        _currentTime = TimeSpan.Zero;
        Console.WriteLine($"AnimatedModel: Playing animation '{clipName}' with {clip.Keyframes.Count} bone tracks");
    }

    /// <summary>
    /// Gets list of available animation clip names.
    /// </summary>
    public List<string> GetAnimationNames()
    {
        if (_animationData == null)
            return new List<string>();

        return _animationData.Clips.Keys.ToList();
    }

    /// <summary>
    /// Merges animation data from another model into this one.
    /// Used to combine a base character model with separate animation files.
    /// </summary>
    /// <param name="content">ContentManager to load from</param>
    /// <param name="animationPath">Path to animation-only model</param>
    /// <param name="animationName">Optional custom name for the animation (uses original name if null)</param>
    public void AddAnimationsFrom(ContentManager content, string animationPath, string? animationName = null)
    {
        if (_animationData == null)
        {
            _animationData = new AnimationData();
        }

        // Load the animation-only model
        var animModel = content.Load<Model>(animationPath);
        var animData = animModel.Tag as AnimationData;

        if (animData == null)
        {
            Console.WriteLine($"AnimatedModel: No animation data found in '{animationPath}'");
            return;
        }

        // Merge animations from the loaded model into our animation data
        foreach (var clip in animData.Clips)
        {
            // Use custom name if provided, otherwise use original name
            string targetName = animationName ?? clip.Key;

            if (_animationData.Clips.ContainsKey(targetName))
            {
                Console.WriteLine($"AnimatedModel: Warning - animation '{targetName}' already exists, skipping");
                continue;
            }

            _animationData.Clips[targetName] = clip.Value;
            Console.WriteLine($"AnimatedModel: Added animation '{targetName}' from '{animationPath}' (duration: {clip.Value.Duration.TotalSeconds:F2}s)");
        }

        // Merge bone indices (should be compatible if from same skeleton)
        foreach (var bone in animData.BoneIndices)
        {
            if (!_animationData.BoneIndices.ContainsKey(bone.Key))
            {
                _animationData.BoneIndices[bone.Key] = bone.Value;
            }
        }
    }

    /// <summary>
    /// Applies keyframe animation to bone transforms.
    /// Interpolates between keyframes based on current time.
    /// </summary>
    private void ApplyKeyframes(AnimationClip clip)
    {
        if (_model == null)
            return;

        // Start with bind pose (local transforms for each bone)
        Matrix[] localTransforms = new Matrix[_model.Bones.Count];
        for (int i = 0; i < _model.Bones.Count; i++)
        {
            localTransforms[i] = _model.Bones[i].Transform;
        }

        // Debug: Log bone count once per second
        if (_debugFrameCount++ % 60 == 0 && clip.Keyframes.Count == 0)
        {
            Console.WriteLine($"AnimatedModel: WARNING - Clip '{clip.Name}' has NO keyframes!");
        }

        // Override with animated transforms from keyframes
        foreach (var boneName in clip.Keyframes.Keys)
        {
            var keyframes = clip.Keyframes[boneName];
            if (keyframes.Count == 0)
                continue;

            // Find bone by name
            int boneIndex = -1;
            for (int i = 0; i < _model.Bones.Count; i++)
            {
                if (_model.Bones[i].Name == boneName)
                {
                    boneIndex = i;
                    break;
                }
            }

            if (boneIndex == -1)
                continue;

            // Find the two keyframes to interpolate between
            Keyframe? currentFrame = null;
            Keyframe? nextFrame = null;

            for (int i = 0; i < keyframes.Count; i++)
            {
                if (keyframes[i].Time <= _currentTime)
                {
                    currentFrame = keyframes[i];
                    // Get next frame (wrap around to first frame if at end)
                    nextFrame = keyframes[(i + 1) % keyframes.Count];
                }
                else
                {
                    break;
                }
            }

            // If we found keyframes, apply interpolation
            if (currentFrame != null && nextFrame != null)
            {
                Matrix transform;

                // Calculate interpolation factor
                TimeSpan frameDuration = nextFrame.Time - currentFrame.Time;
                if (frameDuration.TotalSeconds > 0)
                {
                    float t = (float)((_currentTime - currentFrame.Time).TotalSeconds / frameDuration.TotalSeconds);
                    t = MathHelper.Clamp(t, 0f, 1f);

                    // Interpolate between keyframes
                    transform = InterpolateTransform(currentFrame.Transform, nextFrame.Transform, t);
                }
                else
                {
                    transform = currentFrame.Transform;
                }

                // Apply animated local transform
                localTransforms[boneIndex] = transform;
            }
        }

        // Build absolute transforms by composing local transforms up the hierarchy
        for (int i = 0; i < _model.Bones.Count; i++)
        {
            if (_model.Bones[i].Parent == null)
            {
                // Root bone
                _boneTransforms[i] = localTransforms[i];
            }
            else
            {
                // Child bone: multiply local transform by parent's absolute transform
                int parentIndex = _model.Bones.IndexOf(_model.Bones[i].Parent);
                _boneTransforms[i] = localTransforms[i] * _boneTransforms[parentIndex];
            }
        }
    }

    /// <summary>
    /// Interpolates between two transformation matrices.
    /// Decomposes matrices, interpolates components, and recomposes.
    /// </summary>
    private Matrix InterpolateTransform(Matrix from, Matrix to, float t)
    {
        // Decompose matrices
        from.Decompose(out Vector3 fromScale, out Quaternion fromRotation, out Vector3 fromTranslation);
        to.Decompose(out Vector3 toScale, out Quaternion toRotation, out Vector3 toTranslation);

        // Interpolate components
        Vector3 scale = Vector3.Lerp(fromScale, toScale, t);
        Quaternion rotation = Quaternion.Slerp(fromRotation, toRotation, t);
        Vector3 translation = Vector3.Lerp(fromTranslation, toTranslation, t);

        // Recompose matrix
        return Matrix.CreateScale(scale) *
               Matrix.CreateFromQuaternion(rotation) *
               Matrix.CreateTranslation(translation);
    }
}
