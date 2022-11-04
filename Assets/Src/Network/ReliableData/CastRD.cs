using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Cast RD; used to cast spell/effect
/// </summary>
internal class CastRD : ReliableData
{
    internal Globals.CastCode type;
    internal int caster_uid;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Unit's UID</param>
    /// <param name="castCode">Spell type</param>
    /// <param name="castType">Type of Cast (normal, Vector, Targeted, ...)</param>

    internal CastRD(int uid, Globals.CastCode castCode, TcpMessCode castType) : base(castType)
    {
        caster_uid = uid;
        type = castCode;

        byteSize = 12;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Unit's UID</param>
    /// <param name="castCode">Spell type</param>
    internal CastRD(int uid, Globals.CastCode castCode) : this(uid, castCode, TcpMessCode.Cast) { }

    /// <summary>
    /// Converts RD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal override byte[] ToBytes()
    {
        byte[] vals = new byte[byteSize];

        byte[] mcBytes = BitConverter.GetBytes((int)mc);
        byte[] uidBytes = BitConverter.GetBytes(caster_uid);
        byte[] typeBytes = BitConverter.GetBytes((int)type);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
            Array.Reverse(typeBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 8, 4);

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
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        Globals.CastCode type = (Globals.CastCode)BitConverter.ToInt32(bytes, start + 8);

        GameDebug.Log("Got TCP Cast by " + uid + " of " + type);

        return new CastRD(uid, type);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Cast by " + caster_uid + " of " + type;
    }
}
