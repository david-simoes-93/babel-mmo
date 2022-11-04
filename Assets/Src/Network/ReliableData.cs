using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Data that is transmitted using TCP. Used for single-time events that CANNOT be lost by connection
/// </summary>
internal abstract class ReliableData
{
    // Types
    internal enum TcpMessCode
    {
        Noop,
        SpawnUnit,
        DespawnUnit,
        SpawnItem,
        Cast,
        CombatEffect,
        VectorCast,
        TargetedCast,
        MultiTargetedCast,
        CreateEffect,
        DestroyEffect,
        Buff,
        Debuff
    };

    internal TcpMessCode mc;
    protected int byteSize;

    /// <summary>
    /// Empty RD, contains only type
    /// </summary>
    /// <param name="mc"></param>
    internal ReliableData(TcpMessCode mc)
    {
        this.mc = mc;
    }

    /// <summary>
    /// Converts RD to byte array
    /// </summary>
    /// <returns>The corresponding byte []</returns>
    internal abstract byte[] ToBytes();

    /// <summary>
    /// Creates RD from a byte array
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <returns>The corresponding RD</returns>
    internal static ReliableData ConvertFromBytes(ref byte[] bytes)
    {
        return ConvertFromBytes(ref bytes, 0);
    }

    /// <summary>
    /// Creates RD from a byte array, starting to read at a given location
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// /// <param name="start">Where to start reading from</param>
    /// <returns>The corresponding RD</returns>
    internal static ReliableData ConvertFromBytes(ref byte[] bytes, int start)
    {
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes, start, 4);
        TcpMessCode mc = (TcpMessCode)BitConverter.ToInt32(bytes, start);

        switch (mc)
        {
            case TcpMessCode.SpawnUnit:
                return SpawnRD.FromBytes(ref bytes, start);
            case TcpMessCode.CreateEffect:
                return CreateRD.FromBytes(ref bytes, start);
            case TcpMessCode.Buff:
                return BuffRD.FromBytes(ref bytes, start);
            case TcpMessCode.DespawnUnit:
                return DespawnRD.FromBytes(ref bytes, start);
            case TcpMessCode.DestroyEffect:
                return DestroyRD.FromBytes(ref bytes, start);
            case TcpMessCode.Debuff:
                return DebuffRD.FromBytes(ref bytes, start);
            case TcpMessCode.Cast:
                return CastRD.FromBytes(ref bytes, start);
            case TcpMessCode.VectorCast:
                return VectorCastRD.FromBytes(ref bytes, start);
            case TcpMessCode.TargetedCast:
                return TargetedCastRD.FromBytes(ref bytes, start);
            case TcpMessCode.MultiTargetedCast:
                return MultiTargetedCastRD.FromBytes(ref bytes, start);
            case TcpMessCode.CombatEffect:
                return CombatEffectRD.FromBytes(ref bytes, start);
            case TcpMessCode.Noop:
                return NoopRD.FromBytes(ref bytes, start);
            default:
                GameDebug.Log("converting from bytes something unknown: " + mc);
                break;
        }

        return null;
    }

    /// <summary>
    /// Creates multiple RD from a byte array, starting and stopping to read at a given location
    /// </summary>
    /// <param name="bytes">the byte array</param>
    /// <param name="start">Where to start reading from</param>
    /// <param name="maxSize">Where to stop reading from</param>
    /// <returns>The corresponding RD</returns>
    internal static List<ReliableData> ConvertMultipleFromBytes(ref byte[] bytes, int currentIndex, int maxSize)
    {
        List<ReliableData> rds = new List<ReliableData>();

        while (currentIndex < maxSize)
        {
            //GameDebug.Log("Reading at " + currentIndex);
            ReliableData rd = ConvertFromBytes(ref bytes, currentIndex);
            if (rd == null)
                break;

            rds.Add(rd);
            currentIndex += rd.byteSize;
        }

        return rds;
    }
}
