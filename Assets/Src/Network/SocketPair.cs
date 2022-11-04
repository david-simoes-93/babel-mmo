using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// A pair of sockets (UDP and TCP) to send and receive data from a remote host
/// </summary>
internal class SocketPair
{
    // Server-side, contains parent NetworkServer and playerUid of remote host
    private readonly NetworkServer networkServer_;
    private int playerUid_;

    private readonly UDPSocketWrapper udpSocket_;
    private readonly TCPSocketWrapper tcpSocket_;

    private long lastTCPSentAt_;

    /// <summary>
    /// Client-side constructor, opens sockets on any available ports
    /// </summary>
    internal SocketPair()
    {
        udpSocket_ = new UDPSocketWrapper(this, new IPEndPoint(IPAddress.Any, 0));
        tcpSocket_ = new TCPSocketWrapper(this);
    }

    /// <summary>
    /// Server-side constructor, takes ownership of a given TCP socket and establishes the parent NetworkServer
    /// </summary>
    /// <param name="tcpSock">Already connected TCP socket</param>
    /// <param name="ns">parent</param>
    internal SocketPair(Socket tcpSock, NetworkServer ns)
    {
        this.networkServer_ = ns;
        udpSocket_ = new UDPSocketWrapper(this, new IPEndPoint(IPAddress.Any, 0));
        tcpSocket_ = new TCPSocketWrapper(this, tcpSock);
    }

    /// <summary>
    /// Configures internal variables
    /// </summary>
    /// <param name="em"></param>
    internal void Config(EntityManager em)
    {
        udpSocket_.Config(em);
        tcpSocket_.Config(em);
    }

    /// <summary>
    /// Returns port used by UDP socket
    /// </summary>
    /// <returns></returns>
    internal int GetUdpPort()
    {
        return udpSocket_.GetPort();
    }

    /// <summary>
    /// Returns port used by TCP socket
    /// </summary>
    /// <returns></returns>
    internal int GetTcpPort()
    {
        return tcpSocket_.GetPort();
    }

    /// <summary>
    /// Server-side, returns UID of remote player
    /// </summary>
    /// <returns>Player UID</returns>
    internal int GetRemotePlayerUid()
    {
        return playerUid_;
    }

    /// <summary>
    /// Server-side, sets UID of remote player
    /// </summary>
    /// <returns>Player UID</returns>
    internal void SetRemotePlayerUid(int uid)
    {
        playerUid_ = uid;
    }

    /// <summary>
    /// Server-side, UDP Socket awaits for client to connect
    /// </summary>
    /// <returns>True if connected</returns>
    internal bool WaitForClientUdp()
    {
        return udpSocket_.WaitAndConnectToRemote();
    }

    /// <summary>
    /// Connects TCP socket to given remote
    /// </summary>
    /// <param name="address">remote IP</param>
    /// <param name="tcpPort">remote port</param>
    /// <returns></returns>
    internal NetworkStream ConnectTcpSocket(IPAddress address, int tcpPort)
    {
        tcpSocket_.Connect(address, tcpPort);
        return tcpSocket_.GetNetworkStream();
    }

    /// <summary>
    /// Connects UDP socket to given remote
    /// </summary>
    /// <param name="address">remote IP</param>
    /// <param name="udpPort">remote port</param>
    internal void ConnectUdpSocket(IPAddress address, int udpPort)
    {
        udpSocket_.Connect(address, udpPort);
    }

    /// <summary>
    /// Start threads for both sockets, to receive and send information
    /// </summary>
    internal void StartAsyncLoops()
    {
        tcpSocket_.StartReceiveDataLoop();
        tcpSocket_.StartSendDataLoop();

        udpSocket_.StartReceiveDataLoop();
        udpSocket_.StartSendDataLoop();
    }

    /// <summary>
    /// Called in FixedUpdate, sends current data to connected client/server.
    /// If reliableData is null and a packet was sent within the kClientKeepAliveTime, no TCP data are sent
    /// </summary>
    /// <param name="unreliableData">UDP data</param>
    /// <param name="reliableData">TCP data</param>
    internal void SendData(byte[] unreliableData, byte[] reliableData)
    {
        if (reliableData != null)
        {
            lastTCPSentAt_ = Globals.currTime_ms;
        }
        // keepalive
        else if (Globals.currTime_ms - lastTCPSentAt_ > NetworkGlobals.kClientTCPKeepAlivePeriod_ms)
        {
            reliableData = new byte[0];
            lastTCPSentAt_ = Globals.currTime_ms;
        }

        udpSocket_.SendDataAsync(unreliableData);
        tcpSocket_.SendDataAsync(reliableData);
    }

    /// <summary>
    /// Shuts down SocketPair when an error is thrown by the UDP or TCP socket. Cleanly closes the remaining socket
    /// </summary>
    /// <param name="errorMessage"></param>
    internal void Shutdown(string errorMessage = "")
    {
#if UNITY_SERVER
        Close();
#else
        MainMenuFunctions.SetErrorMessage(errorMessage);
        ClientGameLoop.Close(true);
#endif
    }

    /// <summary>
    /// Cleanly closes connection and sockets
    /// </summary>
    internal void Close()
    {
        GameDebug.Log("Closing SocketPair");
#if UNITY_SERVER
        if (networkServer_ != null)
        {
            networkServer_.RemoveAndDespawnClientConnection(this);
        }
#endif
        udpSocket_.Close();
        tcpSocket_.Close();
    }
}
