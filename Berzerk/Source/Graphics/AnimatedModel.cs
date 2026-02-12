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
/// Uses SkinningData + AnimationPlayer for the canonical XNA three-stage skinning pipeline.
/// Draw uses SkinnedEffect.SetBoneTransforms() for GPU-skinned rendering.
/// </summary>
public class AnimatedModel
{
    private Model? _model;
    private SkinningData? _skinningData;
    private AnimationPlayer? _animationPlayer;
    private bool _effectsChecked;

    private string? _currentClipName;

    /// <summary>
    /// Loads a model and its skinning data from content.
    /// </summary>
    /// <param name="content">ContentManager to load from</param>
    /// <param name="modelPath">Asset path (without extension)</param>
    public void LoadContent(ContentManager content, string modelPath)
    {
        // Load model
        _model = content.Load<Model>(modelPath);

        // Extract skinning data from Model.Tag
        _skinningData = _model.Tag as SkinningData;

        if (_skinningData != null && _skinningData.BindPose.Count > 0)
        {
            // Full skinning data with skeleton -- create animation player
            _animationPlayer = new AnimationPlayer(_skinningData);
            Console.WriteLine($"AnimatedModel: Loaded '{modelPath}' with {_skinningData.AnimationClips.Count} animations, {_skinningData.BindPose.Count} bones:");
            foreach (var clipName in _skinningData.AnimationClips.Keys)
            {
                var clip = _skinningData.AnimationClips[clipName];
                Console.WriteLine($"  - {clipName} (duration: {clip.Duration.TotalSeconds:F2}s, {clip.Keyframes.Count} keyframes)");
            }
        }
        else if (_skinningData != null)
        {
            // Animation-only file with 0 bones -- no player needed
            Console.WriteLine($"AnimatedModel: Loaded '{modelPath}' (animation-only, {_skinningData.AnimationClips.Count} clips, no skeleton)");
        }
        else
        {
            Console.WriteLine($"AnimatedModel: Loaded '{modelPath}' (static model, no animations)");
        }
    }

    /// <summary>
    /// Updates animation playback via the AnimationPlayer three-stage pipeline.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_animationPlayer == null || _animationPlayer.CurrentClip == null)
            return;

        // AnimationPlayer handles looping, keyframe scanning, hierarchy, everything.
        // NOTE: skinTransforms are computed but not sent to GPU yet (Phase 4 switches to SkinnedEffect).
        _animationPlayer.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);
    }

    /// <summary>
    /// Draws the model using SkinnedEffect with GPU-skinned bone transforms.
    /// On first invocation, ensures all mesh parts use SkinnedEffect (handles MonoGame Issue #3057).
    /// </summary>
    public void Draw(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
    {
        if (_model == null)
            return;

        // One-time effect replacement on first draw (handles MonoGame Issue #3057)
        if (!_effectsChecked)
        {
            EnsureSkinnedEffects(graphicsDevice);
            _effectsChecked = true;
        }

        // Enable backface culling and depth testing
        RasterizerState rasterizerState = new RasterizerState
        {
            CullMode = CullMode.CullCounterClockwiseFace
        };
        graphicsDevice.RasterizerState = rasterizerState;
        graphicsDevice.DepthStencilState = DepthStencilState.Default;

        // Get skin transforms from animation player
        Matrix[]? skinTransforms = _animationPlayer?.GetSkinTransforms();

        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (Effect effect in mesh.Effects)
            {
                if (effect is SkinnedEffect skinnedEffect)
                {
                    if (skinTransforms != null)
                        skinnedEffect.SetBoneTransforms(skinTransforms);
                    skinnedEffect.World = world;
                    skinnedEffect.View = view;
                    skinnedEffect.Projection = projection;
                    skinnedEffect.EnableDefaultLighting();
                    skinnedEffect.PreferPerPixelLighting = true;
                }
                else if (effect is BasicEffect basicEffect)
                {
                    // Fallback for static models without skinning data
                    basicEffect.World = world;
                    basicEffect.View = view;
                    basicEffect.Projection = projection;
                    basicEffect.EnableDefaultLighting();
                    basicEffect.PreferPerPixelLighting = true;
                }
            }
            mesh.Draw();
        }
    }

    /// <summary>
    /// Ensures all mesh parts use SkinnedEffect instead of BasicEffect.
    /// Handles MonoGame Issue #3057 where DefaultEffect = SkinnedEffect may not propagate
    /// through the content pipeline. Runs once on first Draw call.
    /// </summary>
    private void EnsureSkinnedEffects(GraphicsDevice graphicsDevice)
    {
        if (_model == null) return;

        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                Console.WriteLine($"AnimatedModel: Mesh '{mesh.Name}' part effect type: {part.Effect.GetType().Name}");

                if (part.Effect is BasicEffect basic)
                {
                    // Replace BasicEffect with SkinnedEffect, copying material properties
                    var skinned = new SkinnedEffect(graphicsDevice)
                    {
                        DiffuseColor = basic.DiffuseColor,
                        SpecularColor = basic.SpecularColor,
                        SpecularPower = basic.SpecularPower,
                        EmissiveColor = basic.EmissiveColor,
                        Alpha = basic.Alpha,
                        Texture = basic.Texture,
                        WeightsPerVertex = 4,
                        PreferPerPixelLighting = true
                    };

                    part.Effect = skinned;
                    Console.WriteLine($"AnimatedModel: Replaced BasicEffect with SkinnedEffect for mesh '{mesh.Name}'");
                }
            }
        }
    }

    /// <summary>
    /// Starts playing a specific animation clip via AnimationPlayer.StartClip.
    /// </summary>
    public void PlayAnimation(string clipName)
    {
        if (_skinningData == null)
        {
            Console.WriteLine($"AnimatedModel: Cannot play animation '{clipName}' - no skinning data");
            return;
        }

        if (_skinningData.AnimationClips.TryGetValue(clipName, out var clip))
        {
            _animationPlayer?.StartClip(clip);
            _currentClipName = clipName;
            Console.WriteLine($"AnimatedModel: Playing animation '{clipName}' ({clip.Keyframes.Count} keyframes)");
        }
        else
        {
            Console.WriteLine($"AnimatedModel: Animation '{clipName}' not found");
        }
    }

    /// <summary>
    /// Gets list of available animation clip names.
    /// </summary>
    public List<string> GetAnimationNames()
    {
        if (_skinningData == null)
            return new List<string>();

        return _skinningData.AnimationClips.Keys.ToList();
    }

    /// <summary>
    /// Merges animation clips from another model's SkinningData into this one.
    /// Used to combine a base character model with separate animation files.
    /// </summary>
    /// <param name="content">ContentManager to load from</param>
    /// <param name="animationPath">Path to animation-only model</param>
    /// <param name="animationName">Optional custom name for the animation (uses original name if null)</param>
    public void AddAnimationsFrom(ContentManager content, string animationPath, string? animationName = null)
    {
        if (_skinningData == null)
        {
            Console.WriteLine($"AnimatedModel: Cannot add animations - base model has no SkinningData");
            return;
        }

        // Load the animation model
        var animModel = content.Load<Model>(animationPath);
        var animSkinningData = animModel.Tag as SkinningData;

        if (animSkinningData == null)
        {
            Console.WriteLine($"AnimatedModel: No SkinningData found in '{animationPath}'");
            return;
        }

        // Merge clips from the loaded model into our skinning data
        foreach (var clipEntry in animSkinningData.AnimationClips)
        {
            // Use custom name if provided, otherwise use original name
            string targetName = animationName ?? clipEntry.Key;

            if (_skinningData.AnimationClips.ContainsKey(targetName))
            {
                Console.WriteLine($"AnimatedModel: Warning - animation '{targetName}' already exists, skipping");
                continue;
            }

            _skinningData.AnimationClips[targetName] = clipEntry.Value;
            Console.WriteLine($"AnimatedModel: Added animation '{targetName}' from '{animationPath}' (duration: {clipEntry.Value.Duration.TotalSeconds:F2}s, {clipEntry.Value.Keyframes.Count} keyframes)");
        }
    }
}
