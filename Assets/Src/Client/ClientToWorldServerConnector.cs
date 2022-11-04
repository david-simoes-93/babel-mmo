using System.Net;
using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Client-side connector to World Server
/// </summary>
internal class ClientToWorldServerConnector
{
    private readonly SocketPair pair_;
    private NetworkStream networkStream_;

    /// <summary>
    /// Empty constructor, prepares a pair_ to Connect()
    /// </summary>
    internal ClientToWorldServerConnector()
    {
        pair_ = new SocketPair();
        networkStream_ = null;
    }

    /// <summary>
    /// Connects TCP socket to specific remote address and conducts logic to authenticate, send PlayerUID to server, and get remote UDP port
    /// </summary>
    /// <param name="ip">remote server IP</param>
    /// <param name="port">remote TCP port</param>
    /// <param name="playerUid">current client UID</param>
    /// <returns>The connected SocketPair</returns>
    internal SocketPair Connect(IPAddress ip, int port, int playerUid, Globals.UnitEntityCode unit_type)
    {
        networkStream_ = pair_.ConnectTcpSocket(ip, port);

        Authenticate(playerUid, unit_type);

        GetUdp(ip);

        return pair_;
    }

    /// <summary>
    /// Conducts initial authentication (paired with WorldServerHandler::Authenticate() )
    /// </summary>
    internal void Authenticate(int playerUid, Globals.UnitEntityCode unit_type)
    {
        // server receives the playerUID
        NetworkStreamUtils.sendInt(networkStream_, playerUid);
        string ok = NetworkStreamUtils.readString(networkStream_);
        if (ok != NetworkGlobals.kOk)
        {
            GameDebug.Log("ERROR: Client sent " + ok);
            throw new Exception("Client auth Error: Client sent uid " + playerUid);
        }

        // server receives the unit type
        NetworkStreamUtils.sendBytes(networkStream_, BitConverter.GetBytes((int)unit_type));
        ok = NetworkStreamUtils.readString(networkStream_);
        if (ok != NetworkGlobals.kOk)
        {
            GameDebug.Log("ERROR: Client sent " + ok);
            throw new Exception("Client auth Error: Client sent unit entity type " + unit_type);
        }
    }

    /// <summary>
    /// Gets UDP socket information (paired with WorldServerHandler::SendUdp() )
    /// </summary>
    /// <param name="ip"></param>
    internal void GetUdp(IPAddress ip)
    {
        // server tells client which UDP port to connect to
        int udpPort = NetworkStreamUtils.readInt(networkStream_);
        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kOk);

        GameDebug.Log("Client UDP will connect to port " + udpPort);
        pair_.ConnectUdpSocket(ip, udpPort);
    }

    /// <summary>
    /// Gets spawn information (paired with WorldServerHandler::SendSpawnInformation() )
    /// </summary>
    /// <returns></returns>
    internal SpawnRD GetSpawnInformation()
    {
        byte[] spawn_info = NetworkStreamUtils.readBytes(networkStream_);
        SpawnRD myself = ReliableData.ConvertFromBytes(ref spawn_info) as SpawnRD;
        return myself;
    }

    /// <summary>
    /// Informs server that everything has been loaded properly, and client will now start its NetworkClient and start sending TCP and UDP packets
    /// This means server can now wait for UDP packets and accept client's UDP connection
    /// </summary>
    internal void PrepareServerForUdpConnection()
    {
        NetworkStreamUtils.sendString(networkStream_, NetworkGlobals.kOk);
    }
}
