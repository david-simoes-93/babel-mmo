using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The Server's Game Loop
/// </summary>
internal class ServerGameLoop : IGameLoop
{
    private readonly string[] scenesToLoad_ = { "NorseMap" }; //"Map_Arena"
    private readonly GameWorld[] gameWorlds_ = new GameWorld[1];
    private readonly NetworkServer[] networkServers_ = new NetworkServer[1];
    private readonly LocalEntityManager[] localEntityManagers_ = new LocalEntityManager[1];

    /// <summary>
    /// Initializes the GameLoop
    /// </summary>
    /// <param name="args">CLI arguments</param>
    /// <returns>Whether initialization was successful</returns>
    public bool Init(string[] args)
    {
        // load each world
        foreach (string sceneName in scenesToLoad_)
        {
            // prepare world, register entities for each world, and disable collisions between them
            // TODO: temp entities will collide! must find a fix
            GameWorld gw = new GameWorld(sceneName);
            gameWorlds_[0] = gw;
            SceneManager.sceneLoaded += gw.ServerRegisterAllSceneEntities;

            // load world
            //https://answers.unity.com/questions/8148/how-to-have-multiple-scenes-concurrently-runnning.html
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

            // prepare network server for each world, each on their own thread
            networkServers_[0] = new NetworkServer(gw);
            gw.Config(networkServers_[0]);

            localEntityManagers_[0] = new LocalEntityManager();
        }

        // prepare master server thread
        ServerAcceptor serverLogin = new ServerAcceptor(gameWorlds_, NetworkGlobals.kMasterServerPort);
        Thread thread = new Thread(new ThreadStart(serverLogin.ServerAcceptLoop));
        thread.Start();

        GameDebug.Log("Server initialized");
        Console.SetOpen(false);

        return true;
    }

    /// <summary>
    /// Shuts down the server and closes all WorldServers
    /// </summary>
    public void Shutdown()
    {
        GameDebug.Log("ServerGameState shutdown");
        Close();
    }

    /// <summary>
    /// Closes all WorldServers (NetworkServers and GameWorlds)
    /// </summary>
    internal void Close()
    {
        // close all network servers
        foreach (NetworkServer ns in networkServers_)
        {
            ns.Close();
        }

        // close all worlds
        foreach (GameWorld gw in gameWorlds_)
        {
            gw.Close();
        }
    }

    /// <summary>
    /// Updates local visuals (sfx, text, etc) based on FPS; should NOT be used
    /// </summary>
    public void Update() { }

    /// <summary>
    /// Called after frame update; should NOT be used
    /// </summary>
    public void LateUpdate() { }

    /// DEBUG: spawns a random NPC
    internal static List<int> dummyNPCs = new List<int>();
    System.Random rand = new System.Random();

    void DebugSpawnRandomNPC()
    {
        // if an npc exists, chance to remove it
        if (rand.NextDouble() >= 0.4 && dummyNPCs.Count >= 3)
        {
            DespawnRD dummySpawn = new DespawnRD(dummyNPCs[0]);
            gameWorlds_[0].EntityManager.AsyncCreateTempEvent(dummySpawn);
            dummyNPCs.RemoveAt(0);
        }
        // otherwise spawn new npc
        else
        {
            int playerUid = gameWorlds_[0].EntityManager.GetValidPlayerUid();
            string playerName = "npcName" + playerUid.ToString();
            Globals.UnitEntityCode type = (rand.NextDouble() > 0.5 ? Globals.UnitEntityCode.kFighter : Globals.UnitEntityCode.kSniper);
            //Vector3 pos = new Vector3(-25 + rand.Next(10), 11, 45 + rand.Next(10)); // Freyja
            //Vector3 pos = new Vector3(22 + rand.Next(20), 17, 92 + rand.Next(20));  // Thor
            Vector3 pos = new Vector3(-80 + rand.Next(20), 17, 92 + rand.Next(20)); // Loki
            Quaternion ori = Quaternion.identity;

            dummyNPCs.Add(playerUid);
            SpawnRD dummySpawn = new SpawnRD(playerUid, playerName, type, 30, 100, pos, ori, 0);
            gameWorlds_[0].EntityManager.AsyncCreateTempEvent(dummySpawn);
        }
    }

    /// <summary>
    /// Sends data to clients and updates entities in worlds at a fixed rate.
    /// </summary>
    public void FixedUpdate()
    {
        // update network information for each world
        foreach (NetworkServer ns in networkServers_)
        {
            // asynchronously send events
            ns.SendData();
        }

        // update each world with information received from network
        foreach (GameWorld gw in gameWorlds_)
        {
            gw.EntityManager.FixedUpdate();
        }

        //
        foreach (LocalEntityManager lem in localEntityManagers_)
        {
            lem.UpdateLocalEffects();
        }

        // DEBUG - random event occasionally, for debug purposes, spawning an npc
        if (rand.NextDouble() < 0.0)
        {
            DebugSpawnRandomNPC();
        }
    }
}
