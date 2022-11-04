using System.Net;
using System.Threading;
using UnityEngine;

/// <summary>
/// Used by both the MasterServer and the WorldServers. Creates server socket that asynchronously processes connections
/// </summary>
internal class ServerAcceptor
{
    private readonly TCPSocketWrapper serverSocket;
    private readonly GameWorld[] gw;

    /// <summary>
    /// WorldServer constructor, accepts a single gameworld (itself), and binds TCP socket to any available port
    /// </summary>
    /// <param name="gw">Corresponding GameWorld</param>
    internal ServerAcceptor(GameWorld gw)
    {
        serverSocket = new TCPSocketWrapper(new IPEndPoint(IPAddress.Any, 0));
        this.gw = new GameWorld[] { gw };

        GameDebug.Log("WorldServer expecting TCP connection at " + IPAddress.Any.ToString() + ":" + serverSocket.GetPort());
    }

    /// <summary>
    /// MasterServer constructor, accepts multiple gameworlds, and binds TCP socket to given port
    /// </summary>
    /// <param name="gw">Available GameWorlds</param>
    /// <param name="port">Local port on which to accept connections</param>
    internal ServerAcceptor(GameWorld[] gw, int port)
    {
        serverSocket = new TCPSocketWrapper(new IPEndPoint(IPAddress.Any, port));
        this.gw = gw;

        GameDebug.Log("MasterServer expecting TCP connection at " + IPAddress.Any.ToString() + ":" + serverSocket.GetPort());
    }

    /// <summary>
    /// Return port on which connections are being listened for
    /// </summary>
    /// <returns></returns>
    internal int GetSocketPort()
    {
        return serverSocket.GetPort();
    }

    /// <summary>
    /// MasterServer loop, called asynchronously
    /// </summary>
    internal void ServerAcceptLoop()
    {
        for (; ; )
        {
            GameDebug.Log("Master waiting for a connection");
            MasterServerHandler clientHandler = new MasterServerHandler(serverSocket.Accept(), gw);

            Thread thread = new Thread(new ThreadStart(clientHandler.MainLoop));
            thread.Start();
        }
    }

    /// <summary>
    /// WorldServer loop, called asynchronously
    /// </summary>
    internal void WorldAcceptLoop()
    {
        for (; ; )
        {
            GameDebug.Log("World waiting for a connection");
            WorldServerHandler clientHandler = new WorldServerHandler(serverSocket.Accept(), gw[0]);

            Thread thread = new Thread(new ThreadStart(clientHandler.AsyncAcceptGWConnection));
            thread.Start();
        }
    }
}
