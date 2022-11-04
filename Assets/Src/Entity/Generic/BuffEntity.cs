using UnityEngine;
using System;
using System.Collections.Generic;
using static Globals;

/// <summary>
/// The main class associated with any long-term Buff in the game (DOTs, HOTs, CCs, etc)
/// </summary>
internal class BuffEntity : Entity
{
    internal UnitEntity caster;
    internal UnitEntity target;
    internal BuffEntityCode Type { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">entity's UID (<0 for NPCs and >0 for players)</param>
    /// <param name="type">entity type (Fireflash DOT, Frostflash slow, etc)</param>
    internal BuffEntity(int uid, BuffEntityCode type)
    {
        Type = type;
        Uid = uid;
    }

    /// <summary>
    /// Client/Server spawns a buff based on given info, and entity is associated with EM entityManager
    /// </summary>
    /// <param name="entityName">buff's name</param>
    internal void Create(UnitEntity caster, UnitEntity target, string buffName)
    {
        Name = buffName;
        this.caster = caster;
        this.target = target;
        EntityManager = target.EntityManager;
#if !UNITY_SERVER
        ClientCreateGameObject(target);
        target.ClientAddBuffEntity(this);
#else
        ServerCreateGameObject(target);
        target.ServerAddBuffEntity(this);
#endif
    }

    /// <summary>
    /// Creates the Buff's GameObject
    /// </summary>
    /// <param name="target">parent Unit entity the buff is attached to</param>
    internal void ClientCreateGameObject(UnitEntity target)
    {
        GameObject = UnityEngine.Object.Instantiate(BuffEntityCodes[Type], target.TargetingTransform);
    }

    /// <summary>
    /// Creates an empty GameObject to act as the Buff's GameObject
    /// </summary>
    /// <param name="target">parent Unit entity the buff is attached to</param>
    internal void ServerCreateGameObject(UnitEntity target)
    {
        GameObject = UnityEngine.Object.Instantiate(Globals.kEmptyPrefab, target.TargetingTransform);
    }

    /// <summary>
    /// Called when object is destroyed
    /// </summary>
    internal void Destroy()
    {
        target.RemoveBuffEntity(this);
        UnityEngine.Object.Destroy(GameObject);
        GameObject = null;
    }

    /// <summary>
    /// Returns whether this BuffEntity exists in the world
    /// </summary>
    internal bool Exists()
    {
        return GameObject != null;
    }
}
