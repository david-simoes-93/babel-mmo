using System;

/// <summary>
/// NoOp RD; used when client casts something which is invalid; server replies with NoOp
/// </summary>
internal class NoopRD : ReliableData
{
    internal readonly int uid_src;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">Source's UID</param>
    internal NoopRD(int uid) : base(TcpMessCode.Noop)
    {
        uid_src = uid;
        byteSize = 8;
    }

    /// <summary>
    /// Converts RD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal override byte[] ToBytes()
    {
        byte[] vals = new byte[byteSize];

        byte[] mcBytes = BitConverter.GetBytes((int)TcpMessCode.Noop);
        byte[] uidBytes = BitConverter.GetBytes(uid_src);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(mcBytes);
            Array.Reverse(uidBytes);
        }

        Buffer.BlockCopy(mcBytes, 0, vals, 0, 4);
        Buffer.BlockCopy(uidBytes, 0, vals, 4, 4);

        return vals;
    }

    /// <summary>
    /// Creates RD from a byte array
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <returns>The corresponding RD</returns>
    internal static NoopRD FromBytes(ref byte[] bytes, int start)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, start + 4, 4);
        }

        int uid = BitConverter.ToInt32(bytes, start + 4);
        return new NoopRD(uid);
    }

    /// <summary>
    /// Converts RD to string
    /// </summary>
    /// <returns>Human-readable string representation</returns>
    public override string ToString()
    {
        return "Noop";
    }
}
