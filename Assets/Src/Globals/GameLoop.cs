using UnityEngine;
using UnityEditor;

/// <summary>
/// Interface that GameLoops must obey
/// </summary>
internal interface IGameLoop
{
    /// <summary>
    /// Initializes the GameLoop
    /// </summary>
    /// <param name="args">string list of arguments</param>
    /// <returns></returns>
    bool Init(string[] args);

    /// <summary>
    /// Shutsdown everything
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Called every frame
    /// </summary>
    void Update();

    /// <summary>
    /// Called with fixed 60Hz
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// Called after frame update
    /// </summary>
    void LateUpdate();
}
