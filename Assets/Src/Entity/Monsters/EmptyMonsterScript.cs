using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class EmptyMonsterScript : MonoBehaviour, IConfigurableMonster
{
    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        // Does nothing
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        // Does nothing
    }

    /// <summary>
    /// Configures Monster with its corresponding unit entity
    /// </summary>
    /// <param name="parent">the parent entity</param>
    public void Config(UnitEntity parent)
    {
        // Does nothing
    }
}
