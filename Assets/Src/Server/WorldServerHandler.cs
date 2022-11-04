using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// WorldServer: accepts a connection from a client, asynchronously handles log-in, and places client in NetworkServer list of clients
/// </summary>
internal class WorldServerHandler
{
    readonly Socket clientSocket_;
    readonly NetworkStream networkStream_;
    readonly GameWorld gameWorld_;
    readonly SocketPair socketPair_;

    private int uid_;
    private Globals.UnitEntityCode unit_type_;

    internal WorldServerHandler(Socket clientSocket, GameWorld gw)
    {
        clientSocket_ = clientSocket;
        clientSocket_.ReceiveTimeout = NetworkGlobals.kClientTimeoutTime_ms * 1000;

        networkStream_ = new NetworkStream(clientSocket);
        gameWorld_ = gw;
        socketPair_ = new SocketPair(clientSocket, gw.NetworkServer);
    }

    /// <summary>
    /// Authenticates client, finishes connections, and places client in NetworkServer.
    /// Called asynchronously, paired with ClientToWorldServerConnector::Connect() and GetSpawnInformation()
    /// </summary>
    internal void AsyncAcceptGWConnection()
    {
        try
        {
            Authenticate();

            SendUdp();

            SendSpawnInformation();

            SetupClient();
        }
        catch (Exception ex)
        {
            GameDebug.Log("Exception: " + ex.ToString());
        }
    }

    /// <summary>
    /// Authenticates client (paired with ClientToWorldServerConnector::Authenticate() )
    /// </summary>
    internal void Authenticate()
    {
        // eventually playerUID identifies a specific character available for this account, so list of UIDs is given to client, and client picks from there
        // a specific spawn location will also be chosen by server, not client. TODO

        // server receives the playerUID
        uid_ = NetworkStreamUtils.readInt(networkStream_);
        if (uid_ <= 0 || gameWorld_.EntityManager.FindUnitEntityByUid(uid_) != null)
        {
            NetworkStreamUtils.sendString(networkStream_, "NOK");
            throw new Exception("GW auth Error: Client sent uid " + uid_);
        }
        socketPair_.SetRemotePlayerUid(uid_);
        NetworkStreamUtils.sendString(networkStream_, "OK");

        // server receives the unit type
        unit_type_ = (Globals.UnitEntityCode)BitConverter.ToInt32(NetworkStreamUtils.readBytes(networkStream_), 0);
        if (unit_type_ != Globals.UnitEntityCode.kFighter && unit_type_ != Globals.UnitEntityCode.kSniper && unit_type_ != Globals.UnitEntityCode.kMage)
        {
            NetworkStreamUtils.sendString(networkStream_, "NOK");
            throw new Exception("GW auth Error: Client sent unit entity type " + unit_type_);
        }
        NetworkStreamUtils.sendString(networkStream_, "OK");
    }

    /// <summary>
    /// Gives client UDP socket info and waits for him to connect (paired with ClientToWorldServerConnector::GetUdp() )
    /// </summary>
    internal void SendUdp()
    {
        // server tells client which UDP port to connect to
        NetworkStreamUtils.sendInt(networkStream_, socketPair_.GetUdpPort());
        string ok = NetworkStreamUtils.readString(networkStream_);
        if (ok != "OK")
        {
            throw new Exception("GW Accept Error: Client sent " + ok);
        }

        socketPair_.Config(gameWorld_.EntityManager);
    }

    /// <summary>
    /// Gives client his spawn information (paired with ClientToWorldServerConnector::GetSpawnInformation() )
    /// </summary>
    internal void SendSpawnInformation()
    {
        // TODO DB
        Vector3 pos = new Vector3(-20, 2, -7);
        Quaternion ori = Quaternion.identity;
        string playerName = "pName" + uid_.ToString();
        SpawnRD newPlayerEvent = new SpawnRD(uid_, playerName, unit_type_, 10, 100, pos, ori, 0);
        gameWorld_.EntityManager.AsyncCreateTempEvent(newPlayerEvent);
        NetworkStreamUtils.sendBytes(networkStream_, newPlayerEvent.ToBytes());
    }

    /// <summary>
    /// Waits for client to connect, adds client to NetworkServer
    /// </summary>
    internal void SetupClient()
    {
        // wait for client to inform that it's about to connect UDP
        string ok = NetworkStreamUtils.readString(networkStream_);
        if (ok != "OK")
        {
            throw new Exception("GW Accept Error: Client sent " + ok);
        }

        bool udpConnected = false;
        int failureCounter = 0;
        while (!udpConnected && failureCounter < 3)
        {
            udpConnected = socketPair_.WaitForClientUdp();
            failureCounter++;
        }
        if (!udpConnected)
        {
            throw new Exception("LoadLevel Error: Client timed-out during connection");
        }

        socketPair_.StartAsyncLoops();
        gameWorld_.NetworkServer.AsyncAddNewClientConnection(socketPair_);
        GameDebug.Log("New client in world");
    }
}
