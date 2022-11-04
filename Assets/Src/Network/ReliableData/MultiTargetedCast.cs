using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// MultiTargetCast RD; used to cast spell/effect targeted at multiple enemies simultaneously
/// </summary>
internal class MultiTargetedCastRD : CastRD
{
    internal int[] target_uids;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid_src">Unit's UID</param>
    /// <param name="uid_trg">Targets' UIDs</param>
    /// <param name="type">Spell type</param>
    internal MultiTargetedCastRD(int uid_src, int[] uid_trgs, Globals.CastCode type) : base(uid_src, type, TcpMessCode.MultiTargetedCast)
    {
        caster_uid = uid_src;
        target_uids = uid_trgs;
        this.type = type;

        // also sending targets count
        byteSize = 16 + 4 * uid_trgs.Length;
    }

    /// <summary>
    /// Converts RD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal override byte[] ToBytes()
    {
        byte[] vals = new byte[byteSize];

        byte[] mcBytes = BitConverter.GetBytes((int)mc);
        byte[] uidSrcBytes = BitConverter.GetBytes(caster_uid);
        byte[] targetCountBytes = BitConverter.GetBytes(target_uids.Length);
        List<byte[]> myList = new List<byte[]>();
        for (int target_index = 0; target_index < target_uids.Length; target_index++)
        {
            myList.Add(BitConverter.GetBytes(target_uids[target_index]));
        }
        byte[] typeBytes = BitConverter.GetBytes((int)type);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidSrcBytes);
            Array.Reverse(targetCountBytes);
            for (int target_index = 0; target_index < myList.Count; target_index++)
            {
                Array.Reverse(myList[target_index]);
            }
            Array.Reverse(typeBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidSrcBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 8, 4);
        Buffer.BlockCopy(targetCountBytes, 0, vals, 12, 4);
        for (int target_index = 0; target_index < myList.Count; target_index++)
        {
            Buffer.BlockCopy(myList[target_index], 0, vals, 16 + 4 * target_index, 4);
        }

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
        int targets_count;
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, start + 4, 4);
            Array.Reverse(bytes, start + 8, 4);
            Array.Reverse(bytes, start + 12, 4);
        }
        targets_count = BitConverter.ToInt32(bytes, start + 12);
        if (BitConverter.IsLittleEndian)
        {
            // Reverse each target, 4 bytes at a time
            for (int target_index = 0; target_index < targets_count; target_index++)
            {
                //GameDebug.Log(target_index + " -> " + (start + 4 + target_index * 4));
                Array.Reverse(bytes, start + 16 + target_index * 4, 4);
            }
        }

        int uid_src = BitConverter.ToInt32(bytes, start + 4);
        Globals.CastCode type = (Globals.CastCode)BitConverter.ToInt32(bytes, start + 8);
        int[] uid_trgs = new int[targets_count];
        string targets_str = "";
        for (int target_index = 0; target_index < targets_count; target_index++)
        {
            uid_trgs[target_index] = BitConverter.ToInt32(bytes, start + 16 + target_index * 4);
            targets_str += uid_trgs[target_index] + ", ";
        }

        GameDebug.Log("Got TCP Cast by " + uid_src + " targeted at " + uid_trgs.Length + " targets (" + targets_str + ") of " + type);

        return new MultiTargetedCastRD(uid_src, uid_trgs, type);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        string targets_str = "";
        for (int i = 0; i < target_uids.Length; i++)
        {
            targets_str += target_uids[i] + ", ";
        }
        return "Cast by " + caster_uid + " targeted at " + target_uids.Length + " targets (" + targets_str + ") of " + type;
    }
}
