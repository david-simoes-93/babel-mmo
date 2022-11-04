//#define UNITY_SERVER
//#define DEBUG_LOGGING

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Game Loop (executed before anything else) - Creates Client/Server game loops and calls Unity methods on them (update(), fixedupdate(), etc)
/// </summary>
[DefaultExecutionOrder(-1000)]
internal class Game : MonoBehaviour
{
    static private IGameLoop gameLoop;

    /// <summary>
    /// Starts Client / Server GameLoop
    /// </summary>
    internal void Awake()
    {
        DontDestroyOnLoad(this);
        //var commandLineArgs = new List<string>(Environment.GetCommandLineArgs());

        Application.targetFrameRate = (int)(1f / Time.fixedDeltaTime);
        //GameDebug.Log("Running at " + Application.targetFrameRate + " fps");

#if UNITY_SERVER
        GameDebug.Init(System.AppDomain.CurrentDomain.BaseDirectory+"/logs", "babel_server_" + ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());
        ServerGameLoop();
#else
#if UNITY_EDITOR
        GameDebug.Init(Application.dataPath.Replace("/Assets", "/logs"), "babel_client_" + ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());
#else
        GameDebug.Init(System.AppDomain.CurrentDomain.BaseDirectory + "/logs", "babel_client_" + ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds());
#endif
        ClientGameLoop();
#endif
        //ConfigVar.Init();
        gameLoop.Init(new string[0]);
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    internal void Update()
    {
        Globals.currTime_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        gameLoop.Update();
    }

    /// <summary>
    /// Called with fixed 60Hz
    /// </summary>
    internal void FixedUpdate()
    {
        Globals.currTime_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        gameLoop.FixedUpdate();
    }

    /// <summary>
    /// Called after frame update
    /// </summary>
    internal void LateUpdate()
    {
        gameLoop.LateUpdate();
    }

    /// <summary>
    /// Shutdown when process ends
    /// </summary>
    void OnApplicationQuit()
    {
        gameLoop.Shutdown();
    }

    /// <summary>
    /// Shutdown when process ends
    /// </summary>
    void OnDestroy()
    {
        gameLoop.Shutdown();
    }

    /// <summary>
    /// Returns existing game loop
    /// </summary>
    /// <returns></returns>
    internal static IGameLoop GetGameLoop()
    {
        return gameLoop;
    }

    /// <summary>
    /// Creates the Server's game loop
    /// </summary>
    private void ServerGameLoop()
    {
        //var consoleUI = new ConsoleTextLinux();
        ConsoleNullUI consoleUI = new ConsoleNullUI();
        string consoleTitle = Application.productName + " Console [" + System.Diagnostics.Process.GetCurrentProcess().Id + "]";
        //var consoleUI = new ConsoleTextWin(consoleTitle, false);
        Console.Init(consoleUI);

        gameLoop = new ServerGameLoop();
    }

    /// <summary>
    /// Creates the Client's game loop
    /// </summary>
    private void ClientGameLoop()
    {
        gameLoop = new ClientGameLoop();
    }
}
