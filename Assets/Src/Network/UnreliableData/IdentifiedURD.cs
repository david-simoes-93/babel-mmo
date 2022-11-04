using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Identified URD; used to represent movement of temporary entities
/// </summary>
internal class IdentifiedURD : UnreliableData
{
    internal int uid,
        eventCounter;
    internal Globals.EntityAnimation state;
    internal Vector3 position,
        speed;
    internal Quaternion ori;

    internal readonly static int kSize = 56;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="pos">Entity position</param>
    /// <param name="vel">Entity velocity</param>
    /// <param name="ori">Entity orientation</param>
    /// <param name="state">Entity state of animation</param>
    /// <param name="eventCounter">ID of last event received in TCP</param>
    internal IdentifiedURD(int uid, Vector3 pos, Vector3 vel, Quaternion ori, Globals.EntityAnimation state, int eventCounter) : base(UdpMessCode.Identified, kSize)
    {
        this.uid = uid;
        position = pos;
        speed = vel;
        this.ori = ori;
        this.state = state;
        this.eventCounter = eventCounter;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="urd">previously unidentified URD</param>
    internal IdentifiedURD(int uid, UnidentifiedURD urd) : base(UdpMessCode.Identified, kSize)
    {
        this.uid = uid;
        position = urd.position;
        speed = urd.speed;
        ori = urd.ori;
        state = urd.state;
        eventCounter = urd.eventCounter;
    }

    /// <summary>
    /// Converts URD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal override byte[] ToBytes()
    {
        byte[] vals = new byte[byteSize];

        byte[] mcBytes = BitConverter.GetBytes((int)mc);
        byte[] uidBytes = BitConverter.GetBytes(uid);
        byte[] posXBytes = BitConverter.GetBytes(position.x);
        byte[] posYBytes = BitConverter.GetBytes(position.y);
        byte[] posZBytes = BitConverter.GetBytes(position.z);
        byte[] velXBytes = BitConverter.GetBytes(speed.x);
        byte[] velYBytes = BitConverter.GetBytes(speed.y);
        byte[] velZBytes = BitConverter.GetBytes(speed.z);
        byte[] oriXBytes = BitConverter.GetBytes(ori.x);
        byte[] oriYBytes = BitConverter.GetBytes(ori.y);
        byte[] oriZBytes = BitConverter.GetBytes(ori.z);
        byte[] oriWBytes = BitConverter.GetBytes(ori.w);
        byte[] stateBytes = BitConverter.GetBytes((int)state);
        byte[] eventCounterBytes = BitConverter.GetBytes(eventCounter);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
            Array.Reverse(posXBytes);
            Array.Reverse(posYBytes);
            Array.Reverse(posZBytes);
            Array.Reverse(velXBytes);
            Array.Reverse(velYBytes);
            Array.Reverse(velZBytes);
            Array.Reverse(oriXBytes);
            Array.Reverse(oriYBytes);
            Array.Reverse(oriZBytes);
            Array.Reverse(oriWBytes);
            Array.Reverse(stateBytes);
            Array.Reverse(eventCounterBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);
        Buffer.BlockCopy(posXBytes, 0, vals, 8, 4);
        Buffer.BlockCopy(posYBytes, 0, vals, 12, 4);
        Buffer.BlockCopy(posZBytes, 0, vals, 16, 4);
        Buffer.BlockCopy(velXBytes, 0, vals, 20, 4);
        Buffer.BlockCopy(velYBytes, 0, vals, 24, 4);
        Buffer.BlockCopy(velZBytes, 0, vals, 28, 4);
        Buffer.BlockCopy(oriXBytes, 0, vals, 32, 4);
        Buffer.BlockCopy(oriYBytes, 0, vals, 36, 4);
        Buffer.BlockCopy(oriZBytes, 0, vals, 40, 4);
        Buffer.BlockCopy(oriWBytes, 0, vals, 44, 4);
        Buffer.BlockCopy(stateBytes, 0, vals, 48, 4);
        Buffer.BlockCopy(eventCounterBytes, 0, vals, 52, 4);

        return vals;
    }

    /// <summary>
    /// Creates URD from a byte array
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <returns>The corresponding URD</returns>
    internal static IdentifiedURD FromBytes(ref byte[] bytes, int start)
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
            Array.Reverse(bytes, start + 52, 4);
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        Vector3 pos = new Vector3(BitConverter.ToSingle(bytes, start + 8), BitConverter.ToSingle(bytes, start + 12), BitConverter.ToSingle(bytes, start + 16));
        Vector3 vel = new Vector3(BitConverter.ToSingle(bytes, start + 20), BitConverter.ToSingle(bytes, start + 24), BitConverter.ToSingle(bytes, start + 28));
        Quaternion ori = new Quaternion(
            BitConverter.ToSingle(bytes, start + 32),
            BitConverter.ToSingle(bytes, start + 36),
            BitConverter.ToSingle(bytes, start + 40),
            BitConverter.ToSingle(bytes, start + 44)
        );
        Globals.EntityAnimation state = (Globals.EntityAnimation)BitConverter.ToInt32(bytes, start + 48);
        int eventCounter = BitConverter.ToInt32(bytes, start + 52);

        return new IdentifiedURD(uid, pos, vel, ori, state, eventCounter);
    }

    /// <summary>
    /// Converts URD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Update " + uid + " at {" + position + "," + ori + "} with speed " + speed + " on state " + state + " after event " + eventCounter;
    }
}
