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

        // For initial version, just copy default bone transforms
        // Full keyframe interpolation can be added later
        _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        // TODO: Implement keyframe interpolation and bone transform updates
        // For now, this is a placeholder that keeps the model in default pose
    }

    /// <summary>
    /// Draws the model with current bone transforms.
    /// </summary>
    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        if (_model == null)
            return;

        foreach (var mesh in _model.Meshes)
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
        Console.WriteLine($"AnimatedModel: Playing animation '{clipName}'");
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
}
