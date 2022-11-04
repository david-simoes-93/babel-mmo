using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Master Server: accepts a connection from a client, asynchronously handles log-in, and distributes client over corresponding world
/// </summary>
internal class MasterServerHandler
{
    readonly Socket serverToClientSocket_;
    readonly NetworkStream networkStream_;
    readonly GameWorld[] gameWorlds_;

    internal MasterServerHandler(Socket ServerToClientSocket, GameWorld[] GameWorlds)
    {
        serverToClientSocket_ = ServerToClientSocket;

        // 3m in login screen
        ServerToClientSocket.ReceiveTimeout = NetworkGlobals.kLoginScreenTimeout_ms;

        networkStream_ = new NetworkStream(ServerToClientSocket);
        gameWorlds_ = GameWorlds;
    }

    /// <summary>
    /// Authenticates client and accepts requests (like WorldServer information)
    /// </summary>
    internal void MainLoop()
    {
        try
        {
            InitialHandshake();
            //GameDebug.Log("Handshake done");
        }
        catch (Exception ex)
        {
            GameDebug.Log("Exception: " + ex.ToString());
        }

        while (true)
        {
            //GameDebug.Log("MasterServerHandler MainLoop");

            string request = NetworkStreamUtils.readString(networkStream_);
            if (request == NetworkGlobals.kLoadLevelProtocol)
            {
                //TODO find which world the client is in
                // TODO DB
                int worldIndex = 0;

                try
                {
                    ProvideGameWorldInformation(gameWorlds_[worldIndex]);
                    GameDebug.Log("Loaded client into new world");
                }
                catch (Exception ex)
                {
                    GameDebug.Log("Exception: " + ex.ToString());
                }
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Conducts initial handshake protocol with client (paired with ClientToMasterServerConnector::InitialHandshake() )
    /// </summary>
    private void InitialHandshake()
    {
        // diffie hellman / certificate auth

        // initial login protocol
        string ok = NetworkStreamUtils.readString(networkStream_);
        if (ok != NetworkGlobals.kOk)
        {
            throw new Exception("Handshake Error: Client sent " + ok);
        }
    }

    /// <summary>
    /// Client asked server for GameWorld information to connect to (paired with ClientToMasterServerConnector::AskGameWorldInformation() )
    /// </summary>
    /// <param name="clientGw">The GW where unit is</param>
    private void ProvideGameWorldInformation(GameWorld clientGw)
    {
        // server tells client which GameWorld to load
        NetworkStreamUtils.sendString(networkStream_, clientGw.WorldName);
        string response = NetworkStreamUtils.readString(networkStream_);
        if (response != NetworkGlobals.kOk)
        {
            throw new Exception("LoadLevel Error: Client sent " + response);
        }

        // server tells client which TCP port to connect to for WorldServer
        NetworkStreamUtils.sendInt(networkStream_, clientGw.NetworkServer.worldTcpPort);
        response = NetworkStreamUtils.readString(networkStream_);
        if (response != NetworkGlobals.kOk)
        {
            throw new Exception("LoadLevel Error: Client sent " + response);
        }

        // server tells client what is its UID (intead of random nmr, should be fetching from DB)
        int playerUid = clientGw.EntityManager.GetValidPlayerUid();
        NetworkStreamUtils.sendInt(networkStream_, playerUid);
        response = NetworkStreamUtils.readString(networkStream_);
        if (response != NetworkGlobals.kOk)
        {
            throw new Exception("LoadLevel Error: Client sent " + response);
        }

        GameDebug.Log("ProvideGameWorldInformation request complete");
    }
}
