using UnityEngine;
using UnityEditor;
using System;
using System.Text;

/// <summary>
/// Spawn RD; used to spawn a unit somewhere
/// </summary>
internal class SpawnRD : ReliableData
{
    internal string name;
    internal Globals.UnitEntityCode type;
    internal int uid,
        current_hp,
        max_hp,
        last_event_id;
    internal Vector3 pos;
    internal Quaternion ori;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Unit's UID</param>
    /// <param name="name">Unit Name</param>
    /// <param name="type">Unit type</param>
    /// <param name="curr_hp">Unit's current HP</param>
    /// <param name="max_hp">Unit's max HP</param>
    /// <param name="pos">Unit's position</param>
    /// <param name="ori">Unit's rotation</param>
    /// <param name="lastEvent">Unit's last event ID</param>
    /// <returns></returns>
    internal SpawnRD(int uid, string name, Globals.UnitEntityCode type, int curr_hp, int max_hp, Vector3 pos, Quaternion ori, int lastEvent) : base(TcpMessCode.SpawnUnit)
    {
        this.uid = uid;
        current_hp = curr_hp;
        this.max_hp = max_hp;
        this.name = name;
        this.type = type;
        this.pos = pos;
        this.ori = ori;
        this.last_event_id = lastEvent;

        byteSize = 52 + name.Length + 1;
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
        byte[] hp1Bytes = BitConverter.GetBytes(current_hp);
        byte[] hp2Bytes = BitConverter.GetBytes(max_hp);
        byte[] posXBytes = BitConverter.GetBytes(pos.x);
        byte[] posYBytes = BitConverter.GetBytes(pos.y);
        byte[] posZBytes = BitConverter.GetBytes(pos.z);
        byte[] oriXBytes = BitConverter.GetBytes(ori.x);
        byte[] oriYBytes = BitConverter.GetBytes(ori.y);
        byte[] oriZBytes = BitConverter.GetBytes(ori.z);
        byte[] oriWBytes = BitConverter.GetBytes(ori.w);
        byte[] lastEventBytes = BitConverter.GetBytes(last_event_id);
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
            Array.Reverse(typeBytes);
            Array.Reverse(hp1Bytes);
            Array.Reverse(hp2Bytes);
            Array.Reverse(posXBytes);
            Array.Reverse(posYBytes);
            Array.Reverse(posZBytes);
            Array.Reverse(oriXBytes);
            Array.Reverse(oriYBytes);
            Array.Reverse(oriZBytes);
            Array.Reverse(oriWBytes);
            Array.Reverse(lastEventBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(typeBytes, 0, vals, 8, 4);
        Buffer.BlockCopy(hp1Bytes, 0, vals, 12, 4);
        Buffer.BlockCopy(hp2Bytes, 0, vals, 16, 4);
        Buffer.BlockCopy(posXBytes, 0, vals, 20, 4);
        Buffer.BlockCopy(posYBytes, 0, vals, 24, 4);
        Buffer.BlockCopy(posZBytes, 0, vals, 28, 4);
        Buffer.BlockCopy(oriXBytes, 0, vals, 32, 4);
        Buffer.BlockCopy(oriYBytes, 0, vals, 36, 4);
        Buffer.BlockCopy(oriZBytes, 0, vals, 40, 4);
        Buffer.BlockCopy(oriWBytes, 0, vals, 44, 4);
        Buffer.BlockCopy(lastEventBytes, 0, vals, 48, 4);

        Buffer.BlockCopy(nameBytes, 0, vals, 52, nameBytes.Length);

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
            Array.Reverse(bytes, start + 44, 4);
            Array.Reverse(bytes, start + 48, 4);
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        Globals.UnitEntityCode type = (Globals.UnitEntityCode)BitConverter.ToInt32(bytes, start + 8);
        int curr_hp = BitConverter.ToInt32(bytes, start + 12);
        int max_hp = BitConverter.ToInt32(bytes, start + 16);
        Vector3 pos = new Vector3(BitConverter.ToSingle(bytes, start + 20), BitConverter.ToSingle(bytes, start + 24), BitConverter.ToSingle(bytes, start + 28));
        Quaternion ori = new Quaternion(
            BitConverter.ToSingle(bytes, start + 32),
            BitConverter.ToSingle(bytes, start + 36),
            BitConverter.ToSingle(bytes, start + 40),
            BitConverter.ToSingle(bytes, start + 44)
        );
        int lastEvent = BitConverter.ToInt32(bytes, start + 48);
        string name = Encoding.ASCII.GetString(bytes, start + 52, bytes.Length - 52 - start).Split((Char)0)[0];

        return new SpawnRD(uid, name, type, curr_hp, max_hp, pos, ori, lastEvent);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Spawn "
            + uid
            + " at {"
            + pos
            + ","
            + ori
            + "} called "
            + name
            + " of type="
            + type
            + " with hp="
            + current_hp
            + "/"
            + max_hp
            + " and last event="
            + last_event_id;
    }
}
