using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Despawn RD; used to despawn a unit
/// </summary>
internal class DespawnRD : ReliableData
{
    internal int uid;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Unit's UID</param>
    /// <returns></returns>
    internal DespawnRD(int uid) : base(TcpMessCode.DespawnUnit)
    {
        this.uid = uid;
        byteSize = 8;
    }

    /// <summary>
    /// Converts RD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal override byte[] ToBytes()
    {
        byte[] vals = new byte[byteSize];

        byte[] mcBytes = BitConverter.GetBytes((int)mc);
        byte[] uidBytes = BitConverter.GetBytes(uid);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, mcBytes.Length);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);

        return vals;
    }

    /// <summary>
    /// Creates RD from a byte array
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <returns>The corresponding RD</returns>
    internal static ReliableData FromBytes(ref byte[] bytes, int start)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, start + 4, 4);
        }
        int uid = BitConverter.ToInt32(bytes, start + 4);

        return new DespawnRD(uid);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Despawn " + uid;
    }
}
