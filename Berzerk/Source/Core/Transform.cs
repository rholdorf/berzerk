using Microsoft.Xna.Framework;

namespace Berzerk.Source.Core;

/// <summary>
/// Represents position and rotation of an entity in 3D space.
/// Uses Quaternion for rotation to avoid gimbal lock.
/// </summary>
public class Transform
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>
    /// Forward direction vector based on current rotation.
    /// </summary>
    public Vector3 Forward => Vector3.Transform(Vector3.Forward, Rotation);

    /// <summary>
    /// Right direction vector based on current rotation.
    /// </summary>
    public Vector3 Right => Vector3.Transform(Vector3.Right, Rotation);

    /// <summary>
    /// Up direction vector based on current rotation.
    /// </summary>
    public Vector3 Up => Vector3.Transform(Vector3.Up, Rotation);

    /// <summary>
    /// Creates world matrix from position and rotation.
    /// </summary>
    public Matrix WorldMatrix => Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);
}
