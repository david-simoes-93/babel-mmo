using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Client-side connector to Master Server
/// </summary>
internal class ClientToMasterServerConnector
{
    private readonly TcpClient clientSocket_;
    private readonly NetworkStream networkStream_;

    /// <summary>
    /// Initializes clientSocket and networkStream by connecting them to given ip and port
    /// </summary>
    /// <param name="ip">Remote server IP</param>
    /// <param name="port">Remote server port</param>
    internal ClientToMasterServerConnector(IPAddress ip, int port)
    {
        clientSocket_ = new TcpClient();
        clientSocket_.Connect(ip, port);
        networkStream_ = clientSocket_.GetStream();
    }

    /// <summary>
    /// Conducts initial handshake protocol with server (paired with MasterServerHandler::InitialHandshake() )
    /// </summary>
    /// <param name="user">username</param>
    /// <param name="pw">password</param>
    internal void InitialHandshake(string user, string pw)
    {
        // diffie hellman / certificate auth

        // initial login protocol

        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kOk);
    }

    /// <summary>
    /// Asks server for GameWorld information to connect to (paired with MasterServerHandler::ProvideGameWorldInformation() )
    /// </summary>
    /// <returns>a ConnectToWorldInfo object with necessary info</returns>
    internal ConnectToWorldInfo AskGameWorldInformation()
    {
        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kLoadLevelProtocol);

        // server tells client which level to load
        string levelName = NetworkStreamUtils.readString(networkStream_);
        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kOk);

        // server tells client what is its IP. For now, just use the same IP
        IPEndPoint ipep = (IPEndPoint)clientSocket_.Client.RemoteEndPoint;
        IPAddress worldServerIp = ipep.Address;

        // server tells client which TCP port to connect to
        int tcpPort = NetworkStreamUtils.readInt(networkStream_);
        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kOk);

        // server tells client what is client's UID
        int playerUid = NetworkStreamUtils.readInt(networkStream_);
        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kOk);

        GameDebug.Log(levelName + " " + tcpPort + " " + playerUid);
        return new ConnectToWorldInfo(tcpPort, levelName, playerUid, worldServerIp);
    }
}

/// <summary>
/// Describes necessary info for a client to connect to a world server
/// </summary>
internal class ConnectToWorldInfo
{
    internal int tcpPort,
        playerUid;
    internal string levelName;
    internal IPAddress worldServerIp;

    internal ConnectToWorldInfo(int tcpPort, string levelName, int playerUid, IPAddress ip)
    {
        this.tcpPort = tcpPort;
        this.levelName = levelName;
        this.playerUid = playerUid;
        this.worldServerIp = ip;
    }
}
