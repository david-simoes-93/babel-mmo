using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// CombatEffect RD; used to represent damage or healing done
/// </summary>
internal class CombatEffectRD : ReliableData
{
    internal Globals.CastCode effect_source_type;
    internal int effect_source_uid,
        target_uid,
        value;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid_source">Source's UID</param>
    /// <param name="uid_target">Target's UID</param>
    /// <param name="action">Cast type that caused this</param>
    /// <param name="damage">Regen/health caused</param>
    /// <returns></returns>
    internal CombatEffectRD(int uid_source, int uid_target, Globals.CastCode action, int damage) : base(TcpMessCode.CombatEffect)
    {
        effect_source_uid = uid_source;
        effect_source_type = action;
        target_uid = uid_target;
        value = damage;

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
        byte[] uidBytes = BitConverter.GetBytes(effect_source_uid);
        byte[] uid2Bytes = BitConverter.GetBytes(target_uid);
        byte[] typeBytes = BitConverter.GetBytes((int)effect_source_type);
        byte[] damageBytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
            Array.Reverse(uid2Bytes);
            Array.Reverse(typeBytes);
            Array.Reverse(damageBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(uid2Bytes, 0, vals, 8, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 12, 4);
        Buffer.BlockCopy(damageBytes, 0, vals, 16, 4);

        return vals;
    }

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
        int uid2 = BitConverter.ToInt32(bytes, start + 8);
        Globals.CastCode type = (Globals.CastCode)BitConverter.ToInt32(bytes, start + 12);
        int damage = BitConverter.ToInt32(bytes, start + 16);

        GameDebug.Log("Got TCP CombatEffect by Entity#" + uid + " to Entity#" + uid2 + " with Attack#" + type + " of SpellCastValue=" + damage);

        return new CombatEffectRD(uid, uid2, type, damage);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Entity" + effect_source_uid + " cast " + effect_source_type + " at " + target_uid + " and dealt " + value + " damage";
    }
}
