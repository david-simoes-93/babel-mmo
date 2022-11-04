using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// VectorCast RD; used to cast spell/effect with location and direction
/// </summary>
internal class VectorCastRD : CastRD
{
    internal Vector3 pos;
    internal Quaternion ori;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Unit's UID</param>
    /// <param name="type">Spell type</param>
    /// <param name="pos">Spell position</param>
    /// <param name="ori">Spell orientation</param>
    internal VectorCastRD(int uid, Globals.CastCode type, Vector3 pos, Quaternion ori) : base(uid, type, TcpMessCode.VectorCast)
    {
        this.pos = pos;
        this.ori = ori;

        byteSize = 40;
    }

    /// <summary>
    /// Converts RD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal override byte[] ToBytes()
    {
        byte[] vals = new byte[byteSize];

        byte[] mcBytes = BitConverter.GetBytes((int)mc);
        byte[] uidBytes = BitConverter.GetBytes(caster_uid);
        byte[] posXBytes = BitConverter.GetBytes(pos.x);
        byte[] posYBytes = BitConverter.GetBytes(pos.y);
        byte[] posZBytes = BitConverter.GetBytes(pos.z);
        byte[] oriXBytes = BitConverter.GetBytes(ori.x);
        byte[] oriYBytes = BitConverter.GetBytes(ori.y);
        byte[] oriZBytes = BitConverter.GetBytes(ori.z);
        byte[] oriWBytes = BitConverter.GetBytes(ori.w);
        byte[] typeBytes = BitConverter.GetBytes((int)type);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
            Array.Reverse(posXBytes);
            Array.Reverse(posYBytes);
            Array.Reverse(posZBytes);
            Array.Reverse(oriXBytes);
            Array.Reverse(oriYBytes);
            Array.Reverse(oriZBytes);
            Array.Reverse(oriWBytes);
            Array.Reverse(typeBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(posXBytes, 0, vals, 8, 4);
        Buffer.BlockCopy(posYBytes, 0, vals, 12, 4);
        Buffer.BlockCopy(posZBytes, 0, vals, 16, 4);
        Buffer.BlockCopy(oriXBytes, 0, vals, 20, 4);
        Buffer.BlockCopy(oriYBytes, 0, vals, 24, 4);
        Buffer.BlockCopy(oriZBytes, 0, vals, 28, 4);
        Buffer.BlockCopy(oriWBytes, 0, vals, 32, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 36, 4);

        return vals;
    }

    /// <summary>
    /// Creates RD from a byte array
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <returns>The corresponding RD</returns>
    new internal static ReliableData FromBytes(ref byte[] bytes, int start)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, start + 4, 4);
            Array.Reverse(bytes, start + 8, 4);
            Array.Reverse(bytes, start + 12, 4);
            Array.Reverse(bytes, start + 16, 4);
            Array.Reverse(bytes, start + 20, 4);
            Array.Reverse(bytes, start + 24, 4);
            Array.Reverse(bytes, start + 28, 4);
            Array.Reverse(bytes, start + 32, 4);
            Array.Reverse(bytes, start + 36, 4);
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        Vector3 pos = new Vector3(BitConverter.ToSingle(bytes, start + 8), BitConverter.ToSingle(bytes, start + 12), BitConverter.ToSingle(bytes, start + 16));
        Quaternion ori = new Quaternion(
            BitConverter.ToSingle(bytes, start + 20),
            BitConverter.ToSingle(bytes, start + 24),
            BitConverter.ToSingle(bytes, start + 28),
            BitConverter.ToSingle(bytes, start + 32)
        );
        Globals.CastCode type = (Globals.CastCode)BitConverter.ToInt32(bytes, start + 36);

        GameDebug.Log("Got TCP Cast by " + uid + "::" + pos + "::" + ori + " of " + type);

        return new VectorCastRD(uid, type, pos, ori);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Cast by " + caster_uid + " at " + pos + " " + ori + " of " + type;
    }
}
