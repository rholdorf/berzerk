using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace Berzerk.ContentPipeline;

/// <summary>
/// Lightweight FBX binary parser that extracts PreRotation Euler angles per bone.
/// Only parses enough of the FBX structure to find Objects → Model → Properties70 → P("PreRotation").
/// Supports FBX binary format version 7100–7700.
/// </summary>
public static class FbxPreRotationReader
{
    /// <summary>
    /// Reads PreRotation (XYZ Euler angles in degrees) for each bone from an FBX binary file.
    /// Returns a dictionary mapping bone name (e.g. "mixamorig:Hips") to its PreRotation vector.
    /// Only includes bones with non-zero PreRotation.
    /// </summary>
    public static Dictionary<string, Vector3> Read(string fbxPath)
    {
        var result = new Dictionary<string, Vector3>();

        using var stream = File.OpenRead(fbxPath);
        using var reader = new BinaryReader(stream, Encoding.ASCII);

        // Validate FBX binary header: "Kaydara FBX Binary  \0"
        byte[] header = reader.ReadBytes(21);
        string headerStr = Encoding.ASCII.GetString(header, 0, 20);
        if (!headerStr.StartsWith("Kaydara FBX Binary"))
            return result; // Not a binary FBX file

        // Skip 2 unknown bytes + read version
        reader.ReadBytes(2);
        uint version = reader.ReadUInt32();

        // Version >= 7500 uses 64-bit offsets; older uses 32-bit
        bool use64Bit = version >= 7500;

        // Parse top-level nodes to find "Objects"
        while (stream.Position < stream.Length)
        {
            var node = ReadNodeHeader(reader, use64Bit);
            if (node.EndOffset == 0) break; // Null node = end of siblings

            if (node.Name == "Objects")
            {
                ParseObjectsNode(reader, node.EndOffset, use64Bit, result);
                break; // Found what we need
            }

            // Skip to end of this node
            stream.Position = node.EndOffset;
        }

        return result;
    }

    /// <summary>
    /// Converts PreRotation Euler angles (degrees, XYZ order) to a rotation matrix.
    /// Uses the FBX convention: Rx * Ry * Rz applied in XYZ order.
    /// </summary>
    public static Matrix EulerToMatrix(Vector3 eulerDegrees)
    {
        float x = MathHelper.ToRadians(eulerDegrees.X);
        float y = MathHelper.ToRadians(eulerDegrees.Y);
        float z = MathHelper.ToRadians(eulerDegrees.Z);

        // FBX Euler XYZ: apply X first, then Y, then Z
        // In XNA row-major: Rx * Ry * Rz means apply Rx first
        return Matrix.CreateRotationX(x) * Matrix.CreateRotationY(y) * Matrix.CreateRotationZ(z);
    }

    private static void ParseObjectsNode(
        BinaryReader reader, long objectsEnd, bool use64Bit,
        Dictionary<string, Vector3> result)
    {
        var stream = reader.BaseStream;

        while (stream.Position < objectsEnd)
        {
            var node = ReadNodeHeader(reader, use64Bit);
            if (node.EndOffset == 0) break;

            if (node.Name == "Model")
            {
                ParseModelNode(reader, node, use64Bit, result);
            }

            stream.Position = node.EndOffset;
        }
    }

    private static void ParseModelNode(
        BinaryReader reader, NodeHeader modelNode, bool use64Bit,
        Dictionary<string, Vector3> result)
    {
        var stream = reader.BaseStream;

        // Read the Model node's properties to get the bone name.
        // First property is typically a Long (ID), second is a String (name like "Model::mixamorig:Hips")
        string? boneName = null;
        long propsStart = stream.Position;
        long propsEnd = propsStart + modelNode.PropertyListLen;

        for (int i = 0; i < Math.Min(modelNode.NumProperties, 3); i++)
        {
            if (stream.Position >= propsEnd) break;
            var (type, value) = ReadProperty(reader);

            if (i == 1 && type == 'S' && value is string nameStr)
            {
                // Name format: "Model::mixamorig:Hips" or just "mixamorig:Hips"
                // Remove the "Model::" prefix and any null-delimited suffix
                boneName = CleanBoneName(nameStr);
            }
        }

        if (boneName == null)
            return;

        // Skip remaining properties
        stream.Position = propsEnd;

        // Parse child nodes to find Properties70
        while (stream.Position < modelNode.EndOffset)
        {
            var child = ReadNodeHeader(reader, use64Bit);
            if (child.EndOffset == 0) break;

            if (child.Name == "Properties70")
            {
                var preRot = ParseProperties70ForPreRotation(reader, child.EndOffset, use64Bit);
                if (preRot.HasValue)
                {
                    var v = preRot.Value;
                    // Only store non-trivial PreRotation
                    if (Math.Abs(v.X) > 0.001f || Math.Abs(v.Y) > 0.001f || Math.Abs(v.Z) > 0.001f)
                    {
                        result[boneName] = v;
                    }
                }
                break;
            }

            stream.Position = child.EndOffset;
        }
    }

    private static Vector3? ParseProperties70ForPreRotation(
        BinaryReader reader, long propsEnd, bool use64Bit)
    {
        var stream = reader.BaseStream;

        while (stream.Position < propsEnd)
        {
            var node = ReadNodeHeader(reader, use64Bit);
            if (node.EndOffset == 0) break;

            if (node.Name == "P" && node.NumProperties >= 7)
            {
                // P node properties: name(S), type1(S), type2(S), flags(S), val1, val2, val3, ...
                long pPropsEnd = stream.Position + node.PropertyListLen;
                var (t0, v0) = ReadProperty(reader);

                if (t0 == 'S' && v0 is string propName && propName == "PreRotation")
                {
                    // Skip type1, type2, flags (3 string properties)
                    for (int i = 0; i < 3; i++)
                        ReadProperty(reader);

                    // Read X, Y, Z (doubles)
                    var (tx, vx) = ReadProperty(reader);
                    var (ty, vy) = ReadProperty(reader);
                    var (tz, vz) = ReadProperty(reader);

                    double x = Convert.ToDouble(vx);
                    double y = Convert.ToDouble(vy);
                    double z = Convert.ToDouble(vz);

                    return new Vector3((float)x, (float)y, (float)z);
                }
            }

            stream.Position = node.EndOffset;
        }

        return null;
    }

    private static string CleanBoneName(string rawName)
    {
        // FBX stores names with null-byte separator: "mixamorig:Hips\x00\x01Model"
        // The bone name is BEFORE the null byte; the type ("Model") is after.
        int nullIdx = rawName.IndexOf('\0');
        if (nullIdx > 0)
        {
            return rawName.Substring(0, nullIdx);
        }

        // Fallback: remove "Model::" prefix (ASCII FBX format)
        if (rawName.StartsWith("Model::"))
            return rawName.Substring(7);

        return rawName;
    }

    #region FBX Binary Parsing Primitives

    private struct NodeHeader
    {
        public long EndOffset;
        public long NumProperties;
        public long PropertyListLen;
        public string Name;
    }

    private static NodeHeader ReadNodeHeader(BinaryReader reader, bool use64Bit)
    {
        var header = new NodeHeader();

        if (use64Bit)
        {
            header.EndOffset = (long)reader.ReadUInt64();
            header.NumProperties = (long)reader.ReadUInt64();
            header.PropertyListLen = (long)reader.ReadUInt64();
        }
        else
        {
            header.EndOffset = reader.ReadUInt32();
            header.NumProperties = reader.ReadUInt32();
            header.PropertyListLen = reader.ReadUInt32();
        }

        byte nameLen = reader.ReadByte();
        header.Name = nameLen > 0
            ? Encoding.ASCII.GetString(reader.ReadBytes(nameLen))
            : "";

        return header;
    }

    private static (char type, object? value) ReadProperty(BinaryReader reader)
    {
        char type = (char)reader.ReadByte();

        switch (type)
        {
            case 'Y': return (type, reader.ReadInt16());
            case 'C': return (type, reader.ReadByte() != 0);
            case 'I': return (type, reader.ReadInt32());
            case 'F': return (type, reader.ReadSingle());
            case 'D': return (type, reader.ReadDouble());
            case 'L': return (type, reader.ReadInt64());

            case 'S':
            case 'R':
            {
                uint len = reader.ReadUInt32();
                byte[] data = reader.ReadBytes((int)len);
                if (type == 'S')
                    return (type, Encoding.ASCII.GetString(data));
                return (type, data);
            }

            // Array types (not needed for PreRotation, but handle to avoid corruption)
            case 'f': case 'd': case 'l': case 'i': case 'b':
            {
                uint arrayLen = reader.ReadUInt32();
                uint encoding = reader.ReadUInt32();
                uint compLen = reader.ReadUInt32();
                reader.ReadBytes((int)compLen);
                return (type, null);
            }

            default:
                throw new InvalidDataException($"Unknown FBX property type: '{type}' (0x{(int)type:X2}) at position {reader.BaseStream.Position - 1}");
        }
    }

    #endregion
}
