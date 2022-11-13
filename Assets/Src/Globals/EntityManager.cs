using KinematicCharacterController;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// handler for all objects in world that are replicated
/// </summary>
internal class EntityManager
{
    readonly private int playerUid_;

    // pose updates on entities
    private readonly ConcurrentDictionary<int, UnidentifiedURD> permanentEntitiesUpdates_;
    private readonly ConcurrentDictionary<int, IdentifiedURD> tempUnitEntitiesUpdates_;

    // lists of entities
    internal List<GameObject> permanentEntities;
    internal Dictionary<int, UnitEntity> tempUnitEntities;
    internal Dictionary<int, EffectEntity> tempEffectEntities;
    internal Dictionary<int, BuffEntity> tempBuffEntities;
    private int npcUidCounter,
        pcUidCounter;

    // new events
    internal ConcurrentQueue<ReliableData> eventsReceived;
    internal ReliableData[] eventsSent;

    /// <summary>
    /// Server-side constructor
    /// </summary>
    internal EntityManager()
    {
        permanentEntities = new List<GameObject>();
        tempUnitEntities = new Dictionary<int, UnitEntity>();
        tempEffectEntities = new Dictionary<int, EffectEntity>();
        tempBuffEntities = new Dictionary<int, BuffEntity>();

        permanentEntitiesUpdates_ = new ConcurrentDictionary<int, UnidentifiedURD>();
        tempUnitEntitiesUpdates_ = new ConcurrentDictionary<int, IdentifiedURD>();
        //tempEffectEntitiesUpdates_ = new ConcurrentDictionary<int, IdentifiedURD>();

        eventsReceived = new ConcurrentQueue<ReliableData>();
        eventsSent = new ReliableData[0];

        npcUidCounter = 0;
        pcUidCounter = 0;
    }

    /// <summary>
    /// Client-side constructor
    /// </summary>
    /// <param name="playerUid">Client's UID</param>
    internal EntityManager(int playerUid)
    {
        playerUid_ = playerUid;

        permanentEntities = new List<GameObject>();
        tempUnitEntities = new Dictionary<int, UnitEntity>();
        tempEffectEntities = new Dictionary<int, EffectEntity>();
        tempBuffEntities = new Dictionary<int, BuffEntity>();

        permanentEntitiesUpdates_ = new ConcurrentDictionary<int, UnidentifiedURD>();
        tempUnitEntitiesUpdates_ = new ConcurrentDictionary<int, IdentifiedURD>();

        eventsReceived = new ConcurrentQueue<ReliableData>();
        eventsSent = null;

        npcUidCounter = 0;
        pcUidCounter = 0;
    }

    /// <summary>
    /// Adds a permanent entity (like a moving elevator) to the EM
    /// </summary>
    /// <param name="entity">The GameObject of the entity being added</param>
    internal void AddPermanentEntity(GameObject entity)
    {
        Transform rb = entity.transform;
        permanentEntitiesUpdates_[permanentEntities.Count] = new UnidentifiedURD(rb.position, Vector3.zero, rb.rotation, 0, 0);
        permanentEntities.Add(entity);
    }

    /// <summary>
    /// Async call. Adds a RD to the TempEvents queue. If server, also validates it
    /// </summary>
    /// <param name="rd">the RD to be added</param>
    internal void AsyncCreateTempEvent(ReliableData rd)
    {
#if UNITY_SERVER
        // preprocesses an event, like a cast that should be turned into a combo or an invalid cast
        rd = PreprocessReliableData(rd);
#endif
        eventsReceived.Enqueue(rd);
    }

    /// <summary>
    /// Client-side async call. Client received multiple UnreliableData updates from server, and client applies them
    /// </summary>
    /// <param name="updates">List of pose updates</param>
    internal void AsyncClientSetUnreliableUpdate(List<UnreliableData> updates)
    {
        for (int index = 0; index < permanentEntities.Count; index++)
        {
            permanentEntitiesUpdates_[index] = updates[index] as UnidentifiedURD;
        }

        for (int index = permanentEntities.Count; index < updates.Count; index++)
        {
            IdentifiedURD urd = updates[index] as IdentifiedURD;

            // dont update self
            if (urd.uid == playerUid_)
                continue;

            // only update things that exist
            if (!tempUnitEntitiesUpdates_.ContainsKey(urd.uid))
                continue;

            tempUnitEntitiesUpdates_[urd.uid] = urd;
        }
    }

    /// <summary>
    /// Server-side async call. Server received UnreliableData update from client, and server applies it
    /// </summary>
    /// <param name="uid">Client's UID</param>
    /// <param name="update">The pose update</param>
    internal void AsyncServerSetUnreliableUpdate(int uid, IdentifiedURD update)
    {
        // only update things that exist
        if (!tempUnitEntitiesUpdates_.ContainsKey(uid))
            return;

        //GameDebug.Log("setting "+uid+" at "+update.ToString());
        tempUnitEntitiesUpdates_[uid] = update;
    }

    /// <summary>
    /// Client-side. Updates permanent entities in GW with Unreliable updates from server (server updates them in their own scripts)
    /// </summary>
    private void UpdatePermanentEntities()
    {
        for (int index = 0; index < permanentEntities.Count; index++)
        {
            Transform transform = permanentEntities[index].transform;
            UnidentifiedURD urd = permanentEntitiesUpdates_[index];

            transform.position = urd.position;
            transform.rotation = urd.ori;
        }
    }

    /// <summary>
    /// Takes updates and events received asynchronously, and commits/processes them
    /// </summary>
    internal void FixedUpdate()
    {
#if UNITY_SERVER
        // server processes all events received from clients and that have since been forwarded
        ServerProcessEvents();
#else
        // client updates permanent entities with updates from server. client then processes all events received from server
        UpdatePermanentEntities();
        ClientProcessEvents();
#endif

        // process pose updates
        ProcessUnreliableUpdates();
    }

    /// <summary>
    /// Update all TempEntities with unrealiable pose updates
    /// </summary>
    private void ProcessUnreliableUpdates()
    {
        // TODO we are updating all entities at all times, this is stupid. we should only update when new info exists
        foreach (KeyValuePair<int, IdentifiedURD> kvp in tempUnitEntitiesUpdates_)
        {
            // TODO DB K/V

            // to handle delays in UDP/TCP
            if (!tempUnitEntities.ContainsKey(kvp.Key))
            {
                continue;
            }

            // local already processed some event, but remote hasn't received it yet
            // and remote is sending UDP which may be outdated (like movement after dying, or wrong positions after teleport)
            // this check ensures that UDPs are processed only if they were sent after the event was processed
            if (tempUnitEntities[kvp.Key].LastEventId > kvp.Value.eventCounter)
            {
                GameDebug.Log(
                    tempUnitEntities[kvp.Key].Name + " has last event id " + tempUnitEntities[kvp.Key].LastEventId + " and update has id " + kvp.Value.eventCounter
                );
                continue;
            }

            BaseControllerKin controller = tempUnitEntities[kvp.Key].Controller;
            controller.SetMotorPose(kvp.Value.position, kvp.Value.speed, kvp.Value.ori);
            tempUnitEntities[kvp.Key].UnitAnimator.SetAnimatorState(kvp.Value.state);
        }
    }

    /// <summary>
    /// Iterates over all RD received from server and processes them
    /// </summary>
    private void ClientProcessEvents()
    {
        while (!eventsReceived.IsEmpty)
        {
            eventsReceived.TryDequeue(out ReliableData rd);

            switch (rd.mc)
            {
                case ReliableData.TcpMessCode.SpawnUnit:
                    ClientProcessSpawn(rd as SpawnRD);
                    break;
                case ReliableData.TcpMessCode.DespawnUnit:
                    ProcessDespawn(rd as DespawnRD);
                    break;
                case ReliableData.TcpMessCode.Cast:
                case ReliableData.TcpMessCode.MultiTargetedCast:
                case ReliableData.TcpMessCode.TargetedCast:
                case ReliableData.TcpMessCode.VectorCast:
                    ClientProcessCast(rd as CastRD);
                    break;
                case ReliableData.TcpMessCode.CombatEffect:
                    ClientProcessCombatEffect(rd as CombatEffectRD);
                    break;
                case ReliableData.TcpMessCode.Noop:
                    ClientProcessNoop(rd as NoopRD);
                    break;
                case ReliableData.TcpMessCode.CreateEffect:
                    ClientProcessCreateEffect(rd as CreateRD);
                    break;
                case ReliableData.TcpMessCode.DestroyEffect:
                    ProcessDestroyEffect(rd as DestroyRD);
                    break;
                case ReliableData.TcpMessCode.Buff:
                    ClientProcessCreateBuff(rd as BuffRD);
                    break;
                case ReliableData.TcpMessCode.Debuff:
                    ProcessDestroyBuff(rd as DebuffRD);
                    break;
                default:
                    GameDebug.Log("Unknown event " + rd.mc);
                    break;
            }
        }
    }

    /// <summary>
    /// Iterates over all RD received from clients and already prepared to be forwarded to clients, and processes them
    /// </summary>
    private void ServerProcessEvents()
    {
        foreach (ReliableData rd in eventsSent)
        {
            switch (rd.mc)
            {
                case ReliableData.TcpMessCode.SpawnUnit:
                    ServerProcessSpawn(rd as SpawnRD);
                    break;
                case ReliableData.TcpMessCode.DespawnUnit:
                    ProcessDespawn(rd as DespawnRD);
                    break;
                case ReliableData.TcpMessCode.Cast:
                case ReliableData.TcpMessCode.MultiTargetedCast:
                case ReliableData.TcpMessCode.TargetedCast:
                case ReliableData.TcpMessCode.VectorCast:
                    ServerProcessCast(rd as CastRD);
                    break;
                case ReliableData.TcpMessCode.CombatEffect:
                    ServerProcessCombatEffect(rd as CombatEffectRD);
                    break;
                case ReliableData.TcpMessCode.Noop:
                    // do nothing
                    break;
                case ReliableData.TcpMessCode.CreateEffect:
                    ServerProcessCreateEffect(rd as CreateRD);
                    break;
                case ReliableData.TcpMessCode.DestroyEffect:
                    ProcessDestroyEffect(rd as DestroyRD);
                    break;
                case ReliableData.TcpMessCode.Buff:
                    ServerProcessCreateBuff(rd as BuffRD);
                    break;
                case ReliableData.TcpMessCode.Debuff:
                    ProcessDestroyBuff(rd as DebuffRD);
                    break;
                default:
                    GameDebug.Log("Unknown event " + rd.mc);
                    break;
            }
        }
    }

    /// <summary>
    /// Processes a ReliableData of type Spawn
    /// </summary>
    /// <param name="rd">the update</param>
    private void ClientProcessSpawn(SpawnRD rd)
    {
        GameDebug.Log("Spawning unit " + rd.name + " at " + rd.pos);

        // spawn entity of type rd.type
        UnitEntity entity = new UnitEntity(rd.uid, rd.type);
        entity.Spawn(this, rd.pos, rd.ori, rd.current_hp, rd.max_hp, rd.name, rd.last_event_id);

        // add entity to list of URD updates; expecting to get updates from the server
        tempUnitEntities.Add(rd.uid, entity);
        IdentifiedURD urd = new IdentifiedURD(
            rd.uid,
            entity.UnitTransform().transform.position,
            Vector3.zero,
            entity.UnitTransform().transform.rotation,
            Globals.EntityAnimation.kIdle,
            0
        );
        tempUnitEntitiesUpdates_.TryAdd(rd.uid, urd);
    }

    /// <summary>
    /// Processes a ReliableData of type Spawn
    /// </summary>
    /// <param name="rd">the update</param>
    private void ServerProcessSpawn(SpawnRD rd)
    {
        GameDebug.Log("Spawning unit " + rd.name + " at " + rd.pos);

        // spawn entity of type rd.type
        UnitEntity entity = new UnitEntity(rd.uid, rd.type);
        entity.Spawn(this, rd.pos, rd.ori, rd.current_hp, rd.max_hp, rd.name, rd.last_event_id);
        tempUnitEntities.Add(rd.uid, entity);

        // If NPC with script, create script
        if (Globals.UnitEntityScripts.ContainsKey(rd.type))
        {
            Globals.UnitEntityScripts[rd.type](entity);
        }
        // Temporary dummy NPC list!
        else if (ServerGameLoop.dummyNPCs.Contains(rd.uid))
        {
            // do nothing
        }
        // Else, add entity to list of URD updates; expecting to get updates from a client
        else
        {
            IdentifiedURD urd = new IdentifiedURD(
                rd.uid,
                entity.UnitTransform().transform.position,
                Vector3.zero,
                entity.UnitTransform().transform.rotation,
                Globals.EntityAnimation.kIdle,
                0
            );
            tempUnitEntitiesUpdates_.TryAdd(rd.uid, urd);
        }
    }

    /// <summary>
    /// Processes a ReliableData of type Despawn
    /// </summary>
    /// <param name="rd">the update</param>
    private void ProcessDespawn(DespawnRD rd)
    {
        GameDebug.Log("Despawning " + rd.uid);
        if (tempUnitEntities.ContainsKey(rd.uid))
        {
            tempUnitEntities[rd.uid].Destroy();
            tempUnitEntitiesUpdates_.TryRemove(rd.uid, out IdentifiedURD goi);
            tempUnitEntities.Remove(rd.uid);
        }
        else
        {
            // Happens if client DCd and both UDP and TCP threads crashed simultaneously; both will try to despawn him; TODO this should be covered in thread error handling
            GameDebug.Log("ERROR: Tried to despawn unit that didnt exist: " + rd.uid);
        }
    }

    /// <summary>
    /// Client-side. Processes a ReliableData of type Cast and adds it to corresponding controller
    /// </summary>
    /// <param name="rd">the update</param>
    private void ClientProcessCast(CastRD rd)
    {
        if (rd.caster_uid == playerUid_)
        {
            ClientGameLoop.CGL.UnitEntity.Validator.AddEvent(rd);
        }
        else
        {
            tempUnitEntities[rd.caster_uid].Validator.AddEvent(rd);
        }
    }

    /// <summary>
    /// Server-side. Processes a ReliableData of type Cast and adds it to corresponding controller
    /// </summary>
    /// <param name="rd">the update</param>
    private void ServerProcessCast(CastRD rd)
    {
        tempUnitEntities[rd.caster_uid].Validator.AddEvent(rd);
    }

    /// <summary>
    /// Client-side. Processes a ReliableData of type CombatEffect
    /// </summary>
    /// <param name="rd">the update</param>
    private void ClientProcessCombatEffect(CombatEffectRD rd)
    {
        Globals.CastCode spellCast = rd.effect_source_type;
        UnitEntity target;

        if (rd.target_uid == playerUid_)
        {
            // player got targeted
            target = ClientGameLoop.CGL.UnitEntity;
        }
        else if (tempUnitEntities.ContainsKey(rd.target_uid))
        {
            // someone targeted another entity
            target = tempUnitEntities[rd.target_uid];
        }
        else
        {
            // whatever got damaged just Despawned
            return;
        }

        // update RD with actual damage/heal dealt
        // TODO: if we don't actually want to do this (and want to show original value), then UI must instead check for shit like invulnerability
        if (Globals.IsHealingSpell(spellCast))
        {
            rd.value = target.Heal(rd.value);
        }
        else
        {
            rd.value = target.Damage(rd.value);
        }

        // Update GUI
        if (rd.target_uid == playerUid_)
        {
            // player got targeted
            ClientGameLoop.CGL.UnitEntity.Canvas.AddCombatEffect(rd, ClientGameLoop.CGL.UnitEntity);
        }
        else if (rd.effect_source_uid == playerUid_)
        {
            // player targeted other entity
            ClientGameLoop.CGL.UnitEntity.Canvas.AddCombatEffect(rd, tempUnitEntities[rd.target_uid]);
        }
    }

    /// <summary>
    /// Server-side. Processes a ReliableData of type CombatEffect
    /// </summary>
    /// <param name="rd">the update</param>
    private void ServerProcessCombatEffect(CombatEffectRD rd)
    {
        if (FindEntityByUid(rd.target_uid) == null)
            return;

        Globals.CastCode spellCast = rd.effect_source_type;
        if (Globals.IsHealingSpell(spellCast))
        {
            tempUnitEntities[rd.target_uid].Heal(rd.value);
        }
        else
        {
            if (tempUnitEntities[rd.target_uid].IsAttackable)
                tempUnitEntities[rd.target_uid].Damage(rd.value);
        }
    }

    /// <summary>
    /// Client-side. Processes a ReliableData of type NoOp
    /// </summary>
    /// <param name="rd">the update</param>
    private void ClientProcessNoop(NoopRD rd)
    {
        if (rd.uid_src == playerUid_)
        {
            ClientGameLoop.CGL.UnitEntity.Validator.ResetSentCastcode();
        }
        else
        {
            GameDebug.Log("Got NOOP from another client. Should never happen.");
        }
    }

    /// <summary>
    /// Processes a ReliableData of type Create
    /// </summary>
    /// <param name="rd">the update</param>
    private void ClientProcessCreateEffect(CreateRD rd)
    {
        GameDebug.Log("Creating " + rd.name + " at " + rd.pos);

        // spawn entity of type rd.type
        EffectEntity entity = new EffectEntity(rd.uid, rd.type, rd.creator_uid);
        entity.Create(this, rd.pos, rd.ori, rd.name);

        // add entity to list of URD updates; expecting to get updates from the server
        tempEffectEntities.Add(rd.uid, entity);
    }

    /// <summary>
    /// Processes a ReliableData of type Create
    /// </summary>
    /// <param name="rd">the update</param>
    private void ServerProcessCreateEffect(CreateRD rd)
    {
        GameDebug.Log("Creating " + rd.name + " at " + rd.pos);

        // spawn entity of type rd.type
        EffectEntity entity = new EffectEntity(rd.uid, rd.type, rd.creator_uid);
        entity.Create(this, rd.pos, rd.ori, rd.name);
        tempEffectEntities.Add(rd.uid, entity);

        // If Effect with script, create script
        if (Globals.EffectEntityScripts.ContainsKey(rd.type))
        {
            Globals.EffectEntityScripts[rd.type](entity);
        }
    }

    /// <summary>
    /// Processes a ReliableData of type Destroy
    /// </summary>
    /// <param name="rd">the update</param>
    private void ProcessDestroyEffect(DestroyRD rd)
    {
        GameDebug.Log("Destroying " + rd.uid);
        if (tempEffectEntities.ContainsKey(rd.uid))
        {
            tempEffectEntities[rd.uid].Destroy();
            tempEffectEntities.Remove(rd.uid);
        }
        else
        {
            GameDebug.Log("ERROR: Tried to destroy effect that didnt exist: " + rd.uid);
        }
    }

    /// <summary>
    /// Processes a ReliableData of type Buff
    /// </summary>
    /// <param name="rd">the update</param>
    private void ClientProcessCreateBuff(BuffRD rd)
    {
        GameDebug.Log("Buffing " + rd.type + " (" + rd.uid + ") on " + rd.target_uid);

        // spawn entity of type rd.type
        BuffEntity entity = new BuffEntity(rd.uid, rd.type);
        entity.Create(
            rd.caster_uid == playerUid_ ? ClientGameLoop.CGL.UnitEntity : tempUnitEntities[rd.caster_uid],
            rd.target_uid == playerUid_ ? ClientGameLoop.CGL.UnitEntity : tempUnitEntities[rd.target_uid],
            "some_buff"
        );
        tempBuffEntities.Add(rd.uid, entity);
    }

    /// <summary>
    /// Processes a ReliableData of type Buff
    /// </summary>
    /// <param name="rd">the update</param>
    private void ServerProcessCreateBuff(BuffRD rd)
    {
        GameDebug.Log("Buffing " + rd.type + " (" + rd.uid + ") on " + rd.target_uid);

        // spawn entity of type rd.type
        BuffEntity entity = new BuffEntity(rd.uid, rd.type);
        entity.Create(tempUnitEntities[rd.caster_uid], tempUnitEntities[rd.target_uid], "some_buff");
        tempBuffEntities.Add(rd.uid, entity);

        // If Buff with script, create script
        if (Globals.BuffEntityScripts.ContainsKey(rd.type))
        {
            Globals.BuffEntityScripts[rd.type](entity);
        }
    }

    /// <summary>
    /// Processes a ReliableData of type Debuff
    /// </summary>
    /// <param name="rd">the update</param>
    private void ProcessDestroyBuff(DebuffRD rd)
    {
        GameDebug.Log("Debuffing " + rd.uid);
        if (tempBuffEntities.ContainsKey(rd.uid))
        {
            tempBuffEntities[rd.uid].Destroy();
            tempBuffEntities.Remove(rd.uid);
        }
        else
        {
            GameDebug.Log("ERROR: Tried to destroy buff that didnt exist: " + rd.uid);
        }
    }

    /// <summary>
    /// Server-side pre-processing for a RD
    /// </summary>
    /// <param name="rd">The RD sent by some UnitEntity</param>
    /// <returns>The pre-processed RD</returns>
    private ReliableData PreprocessReliableData(ReliableData rd)
    {
        if (
            rd.mc == ReliableData.TcpMessCode.Cast
            || rd.mc == ReliableData.TcpMessCode.VectorCast
            || rd.mc == ReliableData.TcpMessCode.TargetedCast
            || rd.mc == ReliableData.TcpMessCode.MultiTargetedCast
        )
            return PreprocessCastRD(rd as CastRD);

        return rd;
    }

    /// <summary>
    /// Server-side pre-processing for a Cast (validation and combo)
    /// </summary>
    /// <param name="rd">The CastRD sent by some UnitEntity</param>
    /// <returns>The valid RD, or NoOp if invalid</returns>
    private ReliableData PreprocessCastRD(CastRD rd)
    {
        BaseCastValidator validator = tempUnitEntities[rd.caster_uid].Validator;

        // validate client cast (no need to validate server-controlled casts)
        if (rd.caster_uid > 0 && (validator == null || !validator.Validate(rd)))
        {
            GameDebug.Log("FOUND AN INVALID CAST! " + rd + " by " + tempUnitEntities[rd.caster_uid].Name);
            return new NoopRD(rd.caster_uid);
        }

        // preprocess the cast
        validator.ServersideCheck(rd);

        return rd;
    }

    /// <summary>
    /// Returns current amount of permanent entities
    /// </summary>
    /// <returns>count</returns>
    internal int GetPermEntitiesCount()
    {
        return permanentEntities.Count;
    }

    /// <summary>
    /// Returns current amount of temporary unit entities
    /// </summary>
    /// <returns>count</returns>
    internal int GetTempUnitEntitiesCount()
    {
        return tempUnitEntities.Count;
    }

    /// <summary>
    /// Returns current amount of temporary effect entities
    /// </summary>
    /// <returns>count</returns>
    internal int GetTempEffectEntitiesCount()
    {
        return tempEffectEntities.Count;
    }

    /// <summary>
    /// Returns current amount of temporary buff entities
    /// </summary>
    /// <returns>count</returns>
    internal int GetTempBuffEntitiesCount()
    {
        return tempBuffEntities.Count;
    }

    /// <summary>
    /// Calculates an available NPC UID for an entity to be spawned
    /// </summary>
    /// <returns>the valid UID</returns>
    internal int GetValidNpcUid()
    {
        npcUidCounter--;

        while (FindEntityByUid(npcUidCounter) != null)
            npcUidCounter--;

        return npcUidCounter;
    }

    /// <summary>
    /// Calculates an available Player UID for an entity to be spawned
    /// Temporary method: used while players have random UIDs, instead of fetching them from DB
    /// Also used to spawn debug NPCs that act as players
    /// </summary>
    /// <returns>the valid UID</returns>
    internal int GetValidPlayerUid()
    {
        pcUidCounter++;

        while (FindEntityByUid(pcUidCounter) != null)
            pcUidCounter++;

        return pcUidCounter;
    }

    /// <summary>
    /// Finds an entity (unit or effect) by its UID
    /// </summary>
    /// <returns>the entity (or null if not found)</returns>
    internal Entity FindEntityByUid(int uid)
    {
        if (tempUnitEntities.ContainsKey(uid))
        {
            return tempUnitEntities[uid];
        }
        if (tempEffectEntities.ContainsKey(uid))
        {
            return tempEffectEntities[uid];
        }
        return null;
    }

    /// <summary>
    /// Finds a Unit Entity by its UID
    /// </summary>
    /// <returns>the entity (or null if not found)</returns>
    internal UnitEntity FindUnitEntityByUid(int uid)
    {
#if !UNITY_SERVER
        if (uid == ClientGameLoop.CGL.UnitEntity.Uid)
        {
            return ClientGameLoop.CGL.UnitEntity;
        }
#else
        //
#endif
        if (tempUnitEntities.ContainsKey(uid))
        {
            return tempUnitEntities[uid];
        }
        return null;
    }
}
