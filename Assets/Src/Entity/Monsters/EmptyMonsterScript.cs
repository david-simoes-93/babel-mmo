using UnityEngine;
using System.Collections.Generic;
using static Globals;

/// <summary>
/// This monster does nothing. It only checks if it's dead, and despawns if so
/// </summary>
internal class EmptyMonsterScript : MonoBehaviour, IConfigurableMonster
{
    UnitEntity parent_;

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
        // despawns when dead
        if (parent_.IsDead)
        {
            parent_.EntityManager.AsyncCreateTempEvent(new DespawnRD(parent_.Uid));
        }
    }

    /// <summary>
    /// Configures Monster with its corresponding unit entity
    /// </summary>
    /// <param name="parent">the parent entity</param>
    public void Config(UnitEntity parent)
    {
        parent_ = parent;
    }
}
