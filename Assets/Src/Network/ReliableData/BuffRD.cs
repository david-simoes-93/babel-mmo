using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Buff RD; used to add a buff on a single target
/// </summary>
internal class BuffRD : ReliableData
{
    internal int uid;
    internal int target_uid;
    internal Globals.BuffEntityCode type;
    internal int caster_uid;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid_src">Unit's UID</param>
    /// <param name="uid_trg">Target's UID</param>
    /// <param name="type">Buff type</param>
    internal BuffRD(int uid, int uid_src, int uid_trg, Globals.BuffEntityCode type) : base(TcpMessCode.Buff)
    {
        this.uid = uid;
        caster_uid = uid_src;
        target_uid = uid_trg;
        this.type = type;

        byteSize = 20;
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
        byte[] uidSrcBytes = BitConverter.GetBytes(caster_uid);
        byte[] uidTrgBytes = BitConverter.GetBytes(target_uid);
        byte[] typeBytes = BitConverter.GetBytes((int)type);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(uidBytes);
            Array.Reverse(mcBytes);
            Array.Reverse(uidSrcBytes);
            Array.Reverse(uidTrgBytes);
            Array.Reverse(typeBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(uidSrcBytes, 0, vals, 8, 4);
        Buffer.BlockCopy(uidTrgBytes, 0, vals, 12, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 16, 4);

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
            Array.Reverse(bytes, start + 8, 4);
            Array.Reverse(bytes, start + 12, 4);
            Array.Reverse(bytes, start + 16, 4);
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        int uid_src = BitConverter.ToInt32(bytes, start + 8);
        int uid_trg = BitConverter.ToInt32(bytes, start + 12);
        Globals.BuffEntityCode type = (Globals.BuffEntityCode)BitConverter.ToInt32(bytes, start + 16);

        GameDebug.Log("Got TCP Buff " + uid + " by " + uid_src + " targeted at " + uid_trg + " of " + type);

        return new BuffRD(uid, uid_src, uid_trg, type);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Buff " + uid + " by " + caster_uid + " targeted at " + target_uid + " of " + type;
    }
}
