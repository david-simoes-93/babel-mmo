using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// A wrapper for a UDP socket
/// </summary>
internal class UDPSocketWrapper
{
    private readonly Socket socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private readonly SocketPair parent_;
    private readonly Queue<byte[]> dataToSend_ = new Queue<byte[]>();
    private readonly AutoResetEvent waitHandle_ = new AutoResetEvent(false);

    private bool isClosed_;
    private EntityManager em_ = null;
    private int sendTimestamp_ = 0,
        recvTimestamp_ = 0;

    /// <summary>
    /// Constructor for UDP socket wrapper, as a child of a SocketPair
    /// </summary>
    /// <param name="st">The SocketPair that owns this socket</param>
    /// <param name="ip">Local end-point to bind socket to</param>
    internal UDPSocketWrapper(SocketPair st, IPEndPoint ip)
    {
        parent_ = st;
        socket_.Bind(ip);
        isClosed_ = false;
    }

    /// <summary>
    /// Connects socket to specific remote address
    /// </summary>
    /// <param name="address">Remote IP</param>
    /// <param name="port">Remote port</param>
    internal void Connect(IPAddress address, int port)
    {
        socket_.Connect(address, port);
    }

    /// <summary>
    /// Configures the socket with necessary data
    /// </summary>
    /// <param name="em">the EntityManager which will process the data received by this socket</param>
    internal void Config(EntityManager em)
    {
        em_ = em;
    }

    /// <summary>
    /// Returns the local socket's port
    /// </summary>
    /// <returns>The port</returns>
    internal int GetPort()
    {
        return (socket_.LocalEndPoint as IPEndPoint).Port;
    }

    /// <summary>
    /// Non-blocking method that enqueues data to be asynchronously sent later
    /// </summary>
    /// <param name="data">The data to be sent</param>
    internal void SendDataAsync(byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if (data.Length > NetworkGlobals.kBufSize)
        {
            GameDebug.Log("Sending too large UDP = " + data.Length + "! Receiver would crash");
            return;
        }

        // queue data and awake sender thread
        dataToSend_.Enqueue(data);
        waitHandle_.Set();
    }

    /// <summary>
    /// Starts a thread that asynchronously sends enqueued data with SendDataLoop()
    /// </summary>
    internal void StartSendDataLoop()
    {
        Thread thread = new Thread(new ThreadStart(SendDataLoop));
        thread.Start();
    }

    /// <summary>
    /// Loop called by thread to asynchronously send enqueued data
    /// </summary>
    private void SendDataLoop()
    {
        waitHandle_.WaitOne();
        while (!isClosed_)
        {
            byte[] data = dataToSend_.Dequeue();

            // build timestamp
            byte[] stampBytes = BitConverter.GetBytes(sendTimestamp_);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(stampBytes);
            }
            sendTimestamp_ = (sendTimestamp_ + 1) % NetworkGlobals.kUdpTimestampMax;
            byte[] timestampedData = new byte[4 + data.Length];
            Buffer.BlockCopy(stampBytes, 0, timestampedData, 0, 4);
            Buffer.BlockCopy(data, 0, timestampedData, 4, data.Length);

            try
            {
                socket_.Send(timestampedData);
            }
            catch (Exception ex)
            {
                if (!isClosed_)
                {
                    GameDebug.Log(ex.ToString());
                    if (parent_ != null)
                        parent_.Shutdown("UDP Send: " + ex.ToString());
                    else
                        Close();
                }
            }

            waitHandle_.WaitOne();
        }
    }

    /// <summary>
    /// Starts a thread that asynchronously receives data with ReceiveDataLoop()
    /// </summary>
    internal void StartReceiveDataLoop()
    {
        Thread thread = new Thread(new ThreadStart(ReceiveDataLoop));
        thread.Start();
    }

    /// <summary>
    /// Loop called by thread to asynchronously receive data and forward it to EntityManager
    /// </summary>
    private void ReceiveDataLoop()
    {
        while (!isClosed_)
        {
            int bytesRead;
            byte[] messageBytes;
            try
            {
                // read message
                messageBytes = new byte[NetworkGlobals.kBufSize];
                bytesRead = socket_.Receive(messageBytes, 0, NetworkGlobals.kBufSize, SocketFlags.None);
            }
            catch (Exception ex)
            {
                if (!isClosed_)
                {
                    GameDebug.Log(ex.ToString());
                    if (parent_ != null)
                        parent_.Shutdown("UDP Rec: " + ex.ToString());
                    else
                        Close();
                }
                break;
            }

            // process timestamp
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(messageBytes, 0, 4);
            }
            int timestamp = BitConverter.ToInt32(messageBytes, 0);

            // prepare timestamp for comparison, in case it cycles at 1000. we essentially compare a new value 0, which should rank higher than 999
            // to do so, we add 1000 to the new timestamp and compare like that. we have a window of 100 lost UDP messages (very large window)
            int compTimestamp = timestamp;
            if (timestamp < 50 && recvTimestamp_ > NetworkGlobals.kUdpTimestampMax - 50)
                compTimestamp = NetworkGlobals.kUdpTimestampMax + timestamp;

            // if we got a delayed UDP, we can safely ignore it
            if (compTimestamp < recvTimestamp_)
            {
                continue;
            }
            recvTimestamp_ = timestamp;

            // process data
#if UNITY_SERVER
            ServerProcessReceivedBytes(ref messageBytes, 4, bytesRead);
#else
            ClientProcessReceivedBytes(ref messageBytes, 4, bytesRead);
#endif
        }
    }

    /// <summary>
    /// Client-side. Converts received byte[] (sent by server) into a list of UnreliableData update entities, which is added to EM
    /// </summary>
    /// <param name="messageBytes">The received byte[]</param>
    /// <param name="startRead">Where to start reading</param>
    /// <param name="endRead">Where to end reading</param>
    private void ClientProcessReceivedBytes(ref byte[] messageBytes, int startRead, int endRead)
    {
        List<UnreliableData> updates = UnreliableData.ConvertMultipleFromBytes(ref messageBytes, startRead, endRead);
        em_.AsyncClientSetUnreliableUpdate(updates);
    }

    /// <summary>
    /// Server-side. Converts received byte[] (sent by a client) into a single IdentifiedURD update, which is added to EM
    /// </summary>
    /// <param name="messageBytes">The received byte[]</param>
    /// <param name="startRead">Where to start reading</param>
    /// <param name="endRead">Where to end reading</param>
    private void ServerProcessReceivedBytes(ref byte[] messageBytes, int startRead, int endRead)
    {
        UnidentifiedURD urd = UnidentifiedURD.FromBytes(ref messageBytes, startRead);
        em_.AsyncServerSetUnreliableUpdate(parent_.GetRemotePlayerUid(), new IdentifiedURD(parent_.GetRemotePlayerUid(), urd));
    }

    /// <summary>
    /// Waits for a UDP packet to arrive and connects socket to sender
    /// </summary>
    /// <returns>True if connected, false otherwise</returns>
    internal bool WaitAndConnectToRemote()
    {
        EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);

        byte[] buffer = new byte[NetworkGlobals.kBufSize];
        socket_.ReceiveTimeout = NetworkGlobals.kUdpConnectionTimeout_ms;
        try
        {
            socket_.ReceiveFrom(buffer, 0, NetworkGlobals.kBufSize, SocketFlags.None, ref epFrom);

            // Connect to client
            IPEndPoint sr = (IPEndPoint)epFrom;

            GameDebug.Log("Connecting to UDP client " + sr);
            socket_.Connect(sr);
        }
        catch (Exception e)
        {
            GameDebug.Log(e.ToString());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Closes the socket
    /// </summary>
    internal void Close()
    {
        GameDebug.Log("Closing UDP");
        isClosed_ = true;
        waitHandle_.Set();
        socket_.Close();
    }
}
