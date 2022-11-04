using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Data that is transmitted using UDP. Used for repeated updates that CAN be lost by connection
/// </summary>
internal abstract class UnreliableData
{
    // Types
    internal enum UdpMessCode
    {
        Noop,
        Identified,
        Unidentified
    };

    internal UdpMessCode mc;
    protected int byteSize;

    /// <summary>
    /// Empty URD, contains only type
    /// </summary>
    /// <param name="mc"></param>
    internal UnreliableData(UdpMessCode mc, int size)
    {
        this.mc = mc;
        byteSize = size;
    }

    /// <summary>
    /// Converts URD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal abstract byte[] ToBytes();

    /// <summary>
    /// Creates URD from a byte array, starting and stopping to read at a given location
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <param name="maxSize">Where to stop reading from</param>
    /// <returns>The corresponding URD</returns>
    internal static UnreliableData ConvertFromBytes(ref byte[] bytes, int start, int maxSize)
    {
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes, start, 4);
        UdpMessCode mc = (UdpMessCode)BitConverter.ToInt32(bytes, start);

        switch (mc)
        {
            case UdpMessCode.Identified:
                return IdentifiedURD.FromBytes(ref bytes, start);
            case UdpMessCode.Unidentified:
                return UnidentifiedURD.FromBytes(ref bytes, start);
            default:
                GameDebug.Log("converting from bytes something unknown: " + mc);
                break;
        }

        return null;
    }

    /// <summary>
    /// Creates multiple URD from a byte array, starting and stopping to read at a given location
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <param name="maxSize">Where to stop reading from</param>
    /// <returns>The corresponding URD</returns>
    internal static List<UnreliableData> ConvertMultipleFromBytes(ref byte[] bytes, int start, int maxSize)
    {
        List<UnreliableData> rds = new List<UnreliableData>();

        while (start < maxSize)
        {
            //GameDebug.Log("Reading at " + currentIndex);
            UnreliableData rd = ConvertFromBytes(ref bytes, start, maxSize);
            if (rd == null)
                break;

            rds.Add(rd);
            start += rd.byteSize;
        }

        return rds;
    }
}
