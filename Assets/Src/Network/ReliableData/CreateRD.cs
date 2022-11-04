using UnityEngine;
using UnityEditor;
using System;
using System.Text;

/// <summary>
/// Create RD; used to create an effect somewhere
/// </summary>
internal class CreateRD : ReliableData
{
    internal string name;
    internal Globals.EffectEntityCode type;
    internal int uid;
    internal int creator_uid;
    internal Vector3 pos;
    internal Quaternion ori;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Effect's UID</param>
    /// <param name="name">Effect Name</param>
    /// <param name="type">Effect type</param>
    /// <param name="creator_uid">Effect's Creator's UID</param>
    /// <param name="pos">Effect's position</param>
    /// <param name="ori">Effect's rotation</param>
    /// <returns></returns>
    internal CreateRD(int uid, string name, Globals.EffectEntityCode type, int creator_uid, Vector3 pos, Quaternion ori) : base(TcpMessCode.CreateEffect)
    {
        this.uid = uid;
        this.name = name;
        this.type = type;
        this.creator_uid = creator_uid;
        this.pos = pos;
        this.ori = ori;

        byteSize = 44 + name.Length + 1;
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
        byte[] typeBytes = BitConverter.GetBytes((int)type);
        byte[] creatorUidBytes = BitConverter.GetBytes(creator_uid);
        byte[] posXBytes = BitConverter.GetBytes(pos.x);
        byte[] posYBytes = BitConverter.GetBytes(pos.y);
        byte[] posZBytes = BitConverter.GetBytes(pos.z);
        byte[] oriXBytes = BitConverter.GetBytes(ori.x);
        byte[] oriYBytes = BitConverter.GetBytes(ori.y);
        byte[] oriZBytes = BitConverter.GetBytes(ori.z);
        byte[] oriWBytes = BitConverter.GetBytes(ori.w);
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
            Array.Reverse(typeBytes);
            Array.Reverse(creatorUidBytes);
            Array.Reverse(posXBytes);
            Array.Reverse(posYBytes);
            Array.Reverse(posZBytes);
            Array.Reverse(oriXBytes);
            Array.Reverse(oriYBytes);
            Array.Reverse(oriZBytes);
            Array.Reverse(oriWBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 8, 4);
        Buffer.BlockCopy(creatorUidBytes, 0, vals, 12, 4);
        Buffer.BlockCopy(posXBytes, 0, vals, 16, 4);
        Buffer.BlockCopy(posYBytes, 0, vals, 20, 4);
        Buffer.BlockCopy(posZBytes, 0, vals, 24, 4);
        Buffer.BlockCopy(oriXBytes, 0, vals, 28, 4);
        Buffer.BlockCopy(oriYBytes, 0, vals, 32, 4);
        Buffer.BlockCopy(oriZBytes, 0, vals, 36, 4);
        Buffer.BlockCopy(oriWBytes, 0, vals, 40, 4);

        Buffer.BlockCopy(nameBytes, 0, vals, 44, nameBytes.Length);

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
            Array.Reverse(bytes, start + 20, 4);
            Array.Reverse(bytes, start + 24, 4);
            Array.Reverse(bytes, start + 28, 4);
            Array.Reverse(bytes, start + 32, 4);
            Array.Reverse(bytes, start + 36, 4);
            Array.Reverse(bytes, start + 40, 4);
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        Globals.EffectEntityCode type = (Globals.EffectEntityCode)BitConverter.ToInt32(bytes, start + 8);
        int creator_uid = BitConverter.ToInt32(bytes, start + 12);
        Vector3 pos = new Vector3(BitConverter.ToSingle(bytes, start + 16), BitConverter.ToSingle(bytes, start + 20), BitConverter.ToSingle(bytes, start + 24));
        Quaternion ori = new Quaternion(
            BitConverter.ToSingle(bytes, start + 28),
            BitConverter.ToSingle(bytes, start + 32),
            BitConverter.ToSingle(bytes, start + 36),
            BitConverter.ToSingle(bytes, start + 40)
        );
        string name = Encoding.ASCII.GetString(bytes, start + 44, bytes.Length - 44 - start).Split((Char)0)[0];

        return new CreateRD(uid, name, type, creator_uid, pos, ori);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Created " + uid + " at {" + pos + "," + ori + "} called " + name + " of type=" + type;
    }
}
