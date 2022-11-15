using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// A handler for the client's connection to a world server
/// </summary>
internal class NetworkClient
{
    private readonly SocketPair sockets_;
    private readonly Queue<ReliableData> events_;
    private byte[] lastUnreliableDataBytes_;
    private long lastUDPSentAt_;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="transport">The connected SocketPair to the WorldServer</param>
    internal NetworkClient(SocketPair transport)
    {
        sockets_ = transport;
        sockets_.StartAsyncLoops();
        events_ = new Queue<ReliableData>();
        lastUnreliableDataBytes_ = new byte[0];
        lastUDPSentAt_ = 0;
    }

    /// <summary>
    /// Closes the NC
    /// </summary>
    internal void Close()
    {
        sockets_.Close();
    }

    /// <summary>
    /// Adds an event to be forwarded to the WorldServer in SendData()
    /// </summary>
    /// <param name="rd"></param>
    virtual internal void AddEvent(ReliableData rd)
    {
        events_.Enqueue(rd);
    }

    /// <summary>
    /// Sends client's data asynchronously. Pose (unreliable) and events (reliable)
    /// </summary>
    internal void SendData()
    {
        UnitEntity player = ClientGameLoop.CGL.UnitEntity;
        // pose of player
        Transform rb = player.UnitTransform();
        BaseControllerKin controller = player.Controller;
        UnidentifiedURD urd = new UnidentifiedURD(rb.position, controller.GetMotorSpeed(), rb.rotation, player.UnitAnimator.CurrentAnimatorState, player.LastEventId);
        byte[] unreliableDataBytes = urd.ToBytes();

        // only send pose if about to timeout or if current pose != from last sent pose
        // TODO create crash here to check how server handles it
        if (Globals.currTime_ms - lastUDPSentAt_ <= NetworkGlobals.kClientUDPKeepAlivePeriod_ms && unreliableDataBytes.SequenceEqual(lastUnreliableDataBytes_))
        {
            unreliableDataBytes = null;
        }
        else
        {
            lastUDPSentAt_ = Globals.currTime_ms;
            lastUnreliableDataBytes_ = unreliableDataBytes;
        }

        // send 1 event at a time
        byte[] reliableDataBytes = null;
        if (events_.Count != 0)
        {
            reliableDataBytes = events_.Dequeue().ToBytes();
        }

        sockets_.SendData(unreliableDataBytes, reliableDataBytes);
    }
}
