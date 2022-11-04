using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// A wrapper for a TCP Socket
/// </summary>
internal class TCPSocketWrapper
{
    private readonly Socket socket_;
    private readonly SocketPair parent_;
    private readonly Queue<byte[]> dataToSend_ = new Queue<byte[]>();
    private readonly AutoResetEvent waitHandle_ = new AutoResetEvent(false);

    private bool isClosed_ = false;
    private EntityManager em_ = null;

    /// <summary>
    /// Constructor for TCP socket wrapper, binding on a specific endpoint. Used to open main sockets for MasterServer and WorldServer
    /// </summary>
    /// <param name="bindPoint">Local IP to bind socket at</param>
    internal TCPSocketWrapper(IPEndPoint bindPoint)
    {
        parent_ = null;

        socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket_.Bind(bindPoint);
        socket_.Listen(25);
    }

    /// <summary>
    /// Constructor for TCP socket wrapper, binding on any available port, as a child of a SocketPair
    /// </summary>
    /// <param name="st">Parent</param>
    internal TCPSocketWrapper(SocketPair st)
    {
        parent_ = st;
        socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    /// <summary>
    /// Constructor for TCP socket wrapper, with an already existing TCP socket, as a child of a SocketPair
    /// </summary>
    /// <param name="sock">The socket to wrap around</param>
    internal TCPSocketWrapper(SocketPair st, Socket sock)
    {
        parent_ = st;
        socket_ = sock;
    }

    /// <summary>
    /// Blocking call that waits for a remote socket to connect
    /// </summary>
    /// <returns></returns>
    internal Socket Accept()
    {
        return socket_.Accept();
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
    /// Connects socket to specific remote address
    /// </summary>
    /// <param name="address">Remote IP</param>
    /// <param name="port">Remote port</param>
    internal void Connect(IPAddress address, int port)
    {
        GameDebug.Log("Client TCP connecting to " + address + ":" + port);
        socket_.Connect(address, port);
    }

    /// <summary>
    /// Creates a NetworkStream for this socket
    /// </summary>
    /// <returns>The NetworkStream associated with the socket</returns>
    internal NetworkStream GetNetworkStream()
    {
        return new NetworkStream(socket_);
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
        if (data == null)
            return;

        // prepare data
        byte[] intToBytes = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intToBytes);
        byte[] rv = new byte[4 + data.Length];
        Buffer.BlockCopy(intToBytes, 0, rv, 0, 4);
        Buffer.BlockCopy(data, 0, rv, 4, data.Length);

        // queue data and awake sender thread
        dataToSend_.Enqueue(rv);
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
            try
            {
                socket_.Send(data);
            }
            catch (Exception ex)
            {
                if (!isClosed_)
                {
                    GameDebug.Log(ex.ToString());
                    if (parent_ != null)
                        parent_.Shutdown("TCP Send: " + ex.ToString());
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
        Thread thread = new Thread(new ThreadStart(ReceiveDataThreadedLoop));
        thread.Start();
    }

    /// <summary>
    /// Loop called by thread to asynchronously receive data and forward it to EntityManager
    /// </summary>
    private void ReceiveDataThreadedLoop()
    {
        byte[] messageSizeBytes = new byte[4];
        while (!isClosed_)
        {
            int bytesRead;
            byte[] messageBytes;
            try
            {
                // read 4 bytes with message size
                bytesRead = socket_.Receive(messageSizeBytes, 0, 4, SocketFlags.None);
                while (bytesRead < 4)
                {
                    bytesRead += socket_.Receive(messageSizeBytes, bytesRead, 4 - bytesRead, SocketFlags.None);
                }
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(messageSizeBytes);
                int messageSize = BitConverter.ToInt32(messageSizeBytes, 0);

                // read actual message
                messageBytes = new byte[messageSize];
                bytesRead = socket_.Receive(messageBytes, 0, messageSize, SocketFlags.None);
                while (bytesRead < messageSize)
                {
                    bytesRead += socket_.Receive(messageBytes, bytesRead, messageSize - bytesRead, SocketFlags.None);
                }
            }
            catch (Exception ex)
            {
                if (!isClosed_)
                {
                    GameDebug.Log(ex.ToString());
                    if (parent_ != null)
                        parent_.Shutdown("TCP Rec: " + ex.ToString());
                    else
                        Close();
                }
                break;
            }
            // process data
            List<ReliableData> rds = ReliableData.ConvertMultipleFromBytes(ref messageBytes, 0, bytesRead);
            //GameDebug.Log("Client received TCP " + bytesRead + " bytes, " + rds.Count+" RDs");
            foreach (ReliableData rd in rds)
            {
                //GameDebug.Log("\t " + rd.ToString());
                em_.AsyncCreateTempEvent(rd);
            }
        }
    }

    /// <summary>
    /// Closes the socket
    /// </summary>
    internal void Close()
    {
        GameDebug.Log("Closing TCP");
        isClosed_ = true;
        waitHandle_.Set();
        socket_.Close();
    }
}
