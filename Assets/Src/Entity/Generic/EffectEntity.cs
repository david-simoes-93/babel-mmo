using UnityEngine;
using System;
using System.Collections.Generic;
using static Globals;

/// <summary>
/// The main class associated with any long-term Effect in the game (danger zones, projectiles, etc)
/// </summary>
internal class EffectEntity : Entity
{
    internal EffectEntityCode Type { get; private set; }
    internal int CreatorUid { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">entity's UID (<0)</param>
    /// <param name="type">entity type (PoisonZone, DeathWave, etc)</param>
    /// <param name="creator_uid">entity's Creator's UID (<0)</param>
    internal EffectEntity(int uid, EffectEntityCode type, int creator_uid)
    {
        Type = type;
        Uid = uid;
        CreatorUid = creator_uid;
    }

    /// <summary>
    /// Client/Server spawns an effect based on given info, and entity is associated with EM entityManager
    /// </summary>
    /// <param name="entityManager">the EM</param>
    /// <param name="pos">unit's position</param>
    /// <param name="ori">unit's rotation</param>
    /// <param name="entityName">unit's name</param>
    internal void Create(EntityManager entityManager, Vector3 pos, Quaternion ori, string entityName)
    {
        EntityManager = entityManager;
        GameObject = UnityEngine.Object.Instantiate(EffectEntityCodes[Type], pos, ori);
        Name = entityName;
    }

    /// <summary>
    /// Called when object is destroyed
    /// </summary>
    internal void Destroy()
    {
        UnityEngine.Object.Destroy(GameObject);
        GameObject = null;
    }

    /// <summary>
    /// Returns the effect's transform
    /// </summary>
    internal Transform EffectTransform()
    {
        return GameObject.transform;
    }

    /// <summary>
    /// Returns whether this EffectEntity exists in the world
    /// </summary>
    internal bool Exists()
    {
        return GameObject != null;
    }
}
