using System;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The Client's GameLoop
/// </summary>
internal class ClientGameLoop : IGameLoop
{
    internal static ClientGameLoop CGL { get; private set; }

    internal GameWorld GameWorld { get; private set; }
    internal NetworkClient NetworkClient { get; private set; }
    internal LocalEntityManager LocalEntityManager { get; private set; }
    internal UnitEntity UnitEntity { get; private set; }

    private int myUid_;
    private ClientToWorldServerConnector connectionToWorldServer_;
    private ClientToMasterServerConnector connectionToMasterServer_;
    private SocketPair socketPair_;
    private bool returnToMainMenu_ = false;

    /// <summary>
    /// Constructor. Defines CGL for global access
    /// </summary>
    internal ClientGameLoop()
    {
        CGL = this;
    }

    /// <summary>
    /// Initializes the GameLoop
    /// </summary>
    /// <param name="args">CLI arguments</param>
    /// <returns>Whether initialization was successful</returns>
    public bool Init(string[] args)
    {
        LocalEntityManager = new LocalEntityManager();

        LoadMainMenu();
        GameDebug.Log("Client initialized");

        return true;
    }

    /// <summary>
    /// Loads the character selection menu. Should only be loaded after connection to master server is established
    /// </summary>
    internal static void LoadMainMenu()
    {
        GameDebug.Log("Loading menu");
        SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
    }

    /// <summary>
    /// Loads the main menu (player's initial scene)
    /// </summary>
    internal static void LoadCharacterSelection()
    {
        GameDebug.Log("Loading menu");
        SceneManager.LoadSceneAsync("CharSelect", LoadSceneMode.Single);
    }

    /// <summary>
    /// Connects Client to MasterServer, and then to WorldServer. Loads correct scene and will eventually call AfterLoadScene()
    /// Returns to main menu with error message if something goes wrong
    /// </summary>
    /// <param name="svIp">MasterServer IP</param>
    /// <param name="user">Username</param>
    /// <param name="pw">Password</param>
    internal void ConnectToMasterServer(IPAddress svIp, string user, string pw)
    {
        // Connect to master server, authenticate
        try
        {
            connectionToMasterServer_ = new ClientToMasterServerConnector(svIp, NetworkGlobals.kMasterServerPort);
        }
        catch (Exception ex)
        {
            MainMenuFunctions.SetErrorMessage("Couldn't connect to master server. " + ex.ToString());
            LoadMainMenu();
            return;
        }
        connectionToMasterServer_.InitialHandshake(user, pw);

        LoadCharacterSelection();
    }

    internal void ConnectToWorldServer(Globals.UnitEntityCode charType)
    {
        // Request world info
        ConnectToWorldInfo serverConnectInfo;
        try
        {
            serverConnectInfo = connectionToMasterServer_.AskGameWorldInformation();
        }
        catch (Exception ex)
        {
            MainMenuFunctions.SetErrorMessage("Couldn't retrieve world information. " + ex.ToString());
            LoadMainMenu();
            return;
        }

        // Connect to world
        myUid_ = serverConnectInfo.playerUid;

        connectionToWorldServer_ = new ClientToWorldServerConnector();
        try
        {
            socketPair_ = connectionToWorldServer_.Connect(serverConnectInfo.worldServerIp, serverConnectInfo.tcpPort, myUid_, charType);
        }
        catch (Exception ex)
        {
            MainMenuFunctions.SetErrorMessage("Couldn't connect to game world. " + ex.ToString());
            LoadMainMenu();
            return;
        }

        // Load world
        GameWorld = new GameWorld(serverConnectInfo.levelName, myUid_);
        SceneManager.sceneLoaded += GameWorld.ClientRegisterPermanentSceneEntities;
        SceneManager.LoadScene(serverConnectInfo.levelName, LoadSceneMode.Single);

        // LoadScene asynchronously calls GameWorld.RegisterSceneEntities(), which calls ClientGameLoop.afterLoadScene()
    }

    /// <summary>
    /// Called after ConnectToMasterServer(). Spawns player in world and finishes initialization
    /// </summary>
    internal static void AfterLoadScene()
    {
        // We only get spawn information after loading scene because it can take a really long time to open the scene
        // This way, the server is not waiting for a UDP to connect, it only has a TCP connection established, with
        // keepalives to show that client is still connected
        SpawnRD mySpawnInfo;
        try
        {
            mySpawnInfo = CGL.connectionToWorldServer_.GetSpawnInformation();
        }
        catch (Exception ex)
        {
            MainMenuFunctions.SetErrorMessage("Couldn't retrieve character information. " + ex.ToString());
            LoadMainMenu();
            return;
        }

        // Spawn myself
        CGL.UnitEntity = new UnitEntity(CGL.myUid_, mySpawnInfo.type);
        CGL.UnitEntity.Spawn(mySpawnInfo, CGL.GameWorld.EntityManager);

        // send OK to server so it awaits UDP connection
        CGL.connectionToWorldServer_.PrepareServerForUdpConnection();

        // Set-up the network client, which will finally ping the server
        CGL.NetworkClient = new NetworkClient(CGL.socketPair_);
        CGL.socketPair_.Config(CGL.GameWorld.EntityManager);
        CGL.GameWorld.Config(CGL.NetworkClient);

        GameDebug.Log("Network client initialized " + CGL.UnitEntity);
    }

    /// <summary>
    /// Updates local visuals (sfx, text, etc) based on FPS
    /// </summary>
    public void Update()
    {
        LocalEntityManager.UpdateLocalEffects();
    }

    /// <summary>
    /// Sends data to GameWorld and updates entities at a fixed rate.
    /// If server disconnects, returns to main menu
    /// </summary>
    public void FixedUpdate()
    {
        if (GameWorld != null)
        {
            NetworkClient.SendData();

            GameWorld.EntityManager.FixedUpdate();
        }

        if (returnToMainMenu_)
        {
            LoadMainMenu();
            returnToMainMenu_ = false;
        }
    }

    /// <summary>
    /// Called after frame update
    /// </summary>
    public void LateUpdate() { }

    /// <summary>
    /// Shuts down the client and closes the connection
    /// </summary>
    public void Shutdown()
    {
        GameDebug.Log("ClientGameLoop shutdown");
        Close(false);
    }

    /// <summary>
    /// Closes connection to server
    /// </summary>
    internal void Close()
    {
        GameDebug.Log("ClientGameLoop close");

        socketPair_ = null;
        if (NetworkClient != null)
            NetworkClient.Close();
        NetworkClient = null;
        if (GameWorld != null)
            GameWorld.Close();
        GameWorld = null;
        LocalEntityManager.ClearAll();
    }

    /// <summary>
    /// Set a UnitEntity as the player. Used by LocalPlayerWrapper
    /// </summary>
    /// <param name="myself"></param>
    internal static void SetLocalPlayer(UnitEntity myself)
    {
        CGL.UnitEntity = myself;
    }

    /// <summary>
    /// Disconnects the client and possibly returns to the main menu
    /// </summary>
    /// <param name="returnToMainMenu">Whether to return to the main menu after DCing client</param>
    internal static void Close(bool returnToMainMenu)
    {
        CGL.Close();
        CGL.returnToMainMenu_ = returnToMainMenu;
    }
}
