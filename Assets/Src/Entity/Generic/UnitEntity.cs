using UnityEngine;
using System;
using System.Collections.Generic;
using static Globals;

/// <summary>
/// The main class associated with any Unit in the game (players, NPCs, etc)
/// </summary>
internal class UnitEntity : Entity
{
    internal UnitEntityCode Type { get; private set; }
    internal BaseCamera Camera { get; private set; }
    internal BaseCanvas Canvas { get; private set; }
    internal BaseCastValidator Validator { get; private set; }
    internal BaseControllerKin Controller { get; private set; }
    internal BaseInputManager InputManager { get; private set; }
    internal BaseAnimator UnitAnimator { get; private set; }

    internal int Health { get; private set; }
    internal int MaxHealth { get; private set; }
    internal bool IsDead { get; private set; } // mostly used internally (IsDead implies !IsAttackable)
    internal bool IsAttackable { get; private set; } // whether a unit can be affected by other effects (like attacks)
    internal bool IsStunned { get; private set; } // whether a unit can move freely
    internal bool IsInvulnerable { get; private set; } // whether a unit takes damage / heal from effects (!IsAttackable implies IsInvulnerable)
    internal bool IsTargetable { get; private set; } // whether a unit can be targeted (!IsTargetable implies !IsAttackable)
    internal UnitEntity LeashedBy { get; private set; }
    internal Vector3 LeashedVector { get; private set; }
    internal int LastEventId { get; private set; }

    internal Transform TargetingTransform { get; private set; }

    private GameObject cameraGameObject_,
        guiGameObject_;

    private Dictionary<BuffEntityCode, int> current_buffs_;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uid">entity's UID (<0 for NPCs and >0 for players)</param>
    /// <param name="type">entity type (Fighter, Sniper, etc)</param>
    internal UnitEntity(int uid, UnitEntityCode type)
    {
        Type = type;
        Uid = uid;
        current_buffs_ = new Dictionary<BuffEntityCode, int>();
    }

    /// <summary>
    /// Deal damage to unit
    /// </summary>
    /// <param name="dmg">amount of damage</param>
    internal int Damage(int dmg)
    {
        if (dmg <= 0 || IsInvulnerable || IsDead)
        {
            return 0;
        }

        GameDebug.Log(Name + " (" + GameObject + ") got damaged with " + dmg);
        int dmg_dealt = Math.Min(Health, dmg);
        Health = Health - dmg_dealt;
        CheckHealth();
        return dmg_dealt;
    }

    /// <summary>
    /// Heal living unit with given amount
    /// </summary>
    /// <param name="heal"></param>
    internal int Heal(int heal)
    {
        if (heal < 0 || IsDead)
            return 0;

        GameDebug.Log(GameObject + " got healed with " + heal);
        int heal_dealt = Math.Min(MaxHealth - Health, heal);
        Health = Health + heal_dealt;
        CheckHealth();
        return heal_dealt;
    }

    /// <summary>
    /// Heal unit to it's maximum health
    /// </summary>
    internal void MaxHeal()
    {
        Health = MaxHealth;
        CheckHealth();
    }

    /// <summary>
    /// Check unit's current Health and update variables (dead status, GUI, etc)
    /// </summary>
    private void CheckHealth()
    {
        SetDead(Health <= 0);
    }

    /// <summary>
    /// Client/Server spawns a unit based on given info, and entity is associated with EM entityManager
    /// </summary>
    /// <param name="entityManager">the EM</param>
    /// <param name="pos">unit's position</param>
    /// <param name="ori">unit's rotation</param>
    /// <param name="curr_hp">unit's current HP</param>
    /// <param name="max_hp">unit's maximum HP</param>
    /// <param name="entityName">unit's name</param>
    /// <param name="last_id">id of unit's last event</param>
    internal void Spawn(EntityManager entityManager, Vector3 pos, Quaternion ori, int curr_hp, int max_hp, string entityName, int last_id)
    {
        EntityManager = entityManager;

        // spawn unit with LC, controller, and validator
        GameObject = UnityEngine.Object.Instantiate(UnitEntityCodes[Type], pos, ori);
        Controller = GameObject.GetComponent<BaseControllerKin>();

        // TODO map
        if (Type == UnitEntityCode.kFighter)
        {
            Validator = new FighterCastValidator();
            UnitAnimator = new FighterAnimator();
        }
        else if (Type == UnitEntityCode.kSniper)
        {
            Validator = new SniperCastValidator();
            UnitAnimator = new SniperAnimator();
        }
        else if (Type == UnitEntityCode.kMage)
        {
            Validator = new MageCastValidator();
            UnitAnimator = new MageAnimator();
        }
        else
        {
            // npc
            Validator = new MonsterCastValidator();
            UnitAnimator = new MonsterAnimator();
        }

        // config is last, after all objects have been defined
        Controller.Config(this);
        Validator.Config(this);
        UnitAnimator.Config(this);

        Name = entityName;
        IsStunned = false;
        IsInvulnerable = false;
        IsTargetable = true;
        LeashedBy = null;
        LeashedVector = Vector3.zero;
        IsAttackable = true;
        IsDead = false;
        Health = curr_hp;
        MaxHealth = max_hp;
        TargetingTransform = Controller.TargetingPoint.transform;

        CheckHealth();
    }

    /// <summary>
    /// Client spawns his own unit, based on info mySpawnInfo, and entity is associated with EM entityManager
    /// </summary>
    /// <param name="mySpawnInfo">spawn information</param>
    /// <param name="entityManager">the EM</param>
    internal void Spawn(SpawnRD mySpawnInfo, EntityManager entityManager)
    {
        // player camera, input, and GUI
        if (Type == UnitEntityCode.kFighter)
        {
            cameraGameObject_ = UnityEngine.Object.Instantiate(kFighterCam, Vector3.zero, Quaternion.identity);
            guiGameObject_ = UnityEngine.Object.Instantiate(kFighterUI, Vector3.zero, Quaternion.identity);
        }
        else if (Type == UnitEntityCode.kSniper)
        {
            cameraGameObject_ = UnityEngine.Object.Instantiate(kSniperCam, Vector3.zero, Quaternion.identity);
            guiGameObject_ = UnityEngine.Object.Instantiate(kSniperUI, Vector3.zero, Quaternion.identity);
        }
        else if (Type == UnitEntityCode.kMage)
        {
            cameraGameObject_ = UnityEngine.Object.Instantiate(kMageCam, Vector3.zero, Quaternion.identity);
            guiGameObject_ = UnityEngine.Object.Instantiate(kMageUI, Vector3.zero, Quaternion.identity);
        }
        else
        {
            GameDebug.Log("Unrecognized TYPE: " + Type);
            return;
        }
        Camera = cameraGameObject_.GetComponent<BaseCamera>();
        InputManager = cameraGameObject_.GetComponent<BaseInputManager>();
        Canvas = guiGameObject_.GetComponent<BaseCanvas>();

        // spawn unit
        Spawn(entityManager, mySpawnInfo.pos, mySpawnInfo.ori, mySpawnInfo.current_hp, mySpawnInfo.max_hp, mySpawnInfo.name, 0);

        // configures everything
        Camera.Config(this);
        InputManager.Config(this);
        Canvas.Config(this);
    }

    /// <summary>
    /// Called when object is destroyed
    /// </summary>
    internal void Destroy()
    {
        UnityEngine.Object.Destroy(GameObject);
        GameObject = null;
        if (cameraGameObject_ != null)
        {
            UnityEngine.Object.Destroy(cameraGameObject_);
            cameraGameObject_ = null;
        }
        if (guiGameObject_ != null)
        {
            UnityEngine.Object.Destroy(guiGameObject_);
            guiGameObject_ = null;
        }

        // When a UnitEntity is despawned, it might have Buffs on it, which must be cleared from EntityManager
        var buffUids = new List<int>(current_buffs_.Values);
        foreach (int buffUid in buffUids)
        {
            EntityManager.DestroyBuff(buffUid);
        }
    }

    /// <summary>
    /// Returns the camera's transform
    /// </summary>
    internal Transform CameraTransform()
    {
        return cameraGameObject_.transform;
    }

    /// <summary>
    /// Returns the player's transform
    /// </summary>
    internal Transform UnitTransform()
    {
        return GameObject.transform;
    }

    /// <summary>
    /// Returns whether this UnitEntity exists in the world
    /// </summary>
    internal bool Exists()
    {
        return GameObject != null;
    }

    /// <summary>
    /// Sets the entity's dead status
    /// </summary>
    /// <param name="dead">dead status</param>
    internal void SetDead(bool dead)
    {
        if (IsDead == dead)
        {
            return;
        }

        IsDead = dead;
        IsAttackable = !dead;

        if (IsDead)
        {
            UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kDeath);
            SetLeash(null, Vector3.zero);
            SetInvulnerable(false);
            SetTargetable(true);
#if !UNITY_SERVER
            if (Type == UnitEntityCode.kSniper)
            {
                (Validator as SniperCastValidator).ClientHideCurrentWeapon();
            }
#else
#endif
        }
        else
        {
#if !UNITY_SERVER
            if (Type == UnitEntityCode.kSniper)
            {
                SniperCastValidator sVal = Validator as SniperCastValidator;
                sVal.ClientShowWeapon(sVal.CurrentWeapon());
            }
#else
#endif
        }

        SetProperCharacterState();
    }

    /// <summary>
    /// Sets the entity's leash status
    /// </summary>
    /// <param name="leashedBy">the unit leashing this one (null if nothing is leashing)</param>
    /// <param name="leashVector">the vector at which this unit will be relative to the other's position</param>
    internal void SetLeash(UnitEntity leashedBy, Vector3 leashVector)
    {
        LeashedBy = leashedBy;
        LeashedVector = leashVector;
        SetStunned(leashedBy != null);
        // SetProperCharacterState() called by SetStunned
    }

    /// <summary>
    /// Sets the entity's stun status
    /// </summary>
    /// <param name="stunned">stun status</param>
    internal void SetStunned(bool stunned)
    {
        // STUN is superseeded by LEASHED
        IsStunned = LeashedBy != null || stunned;
        SetProperCharacterState();
    }

    /// <summary>
    /// Sets the entity's invulnerability status
    /// </summary>
    /// <param name="stunned">invulnerability status</param>
    internal void SetInvulnerable(bool invulnerable)
    {
        IsInvulnerable = invulnerable;
    }

    /// <summary>
    /// Sets the entity's targetable status
    /// </summary>
    /// <param name="stunned">targetable status</param>
    internal void SetTargetable(bool targetable)
    {
        IsTargetable = targetable;
    }

    /// <summary>
    /// Sets the entity's correct CharacterState. Priority is, in order: Dead, Leashed, Stunned, specific states (SniperCrouching, MageChanneling, etc), Default
    /// </summary>
    private void SetProperCharacterState()
    {
        GameDebug.Log("SetProperCharacterState");
        if (IsDead)
        {
            Controller.TransitionToState(CharacterState.Dead);
        }
        else if (LeashedBy != null)
        {
            Controller.TransitionToState(CharacterState.Leashed);
        }
        else if (IsStunned)
        {
            Controller.TransitionToState(CharacterState.Stunned);
        }
        else
        {
            // Only go default if leaving Dead, Leashed, or Stunned states
            if (
                Controller.CurrentCharacterState == CharacterState.Dead
                || Controller.CurrentCharacterState == CharacterState.Leashed
                || Controller.CurrentCharacterState == CharacterState.Stunned
            )
                Controller.TransitionToState(CharacterState.Default);
        }
    }

    /// <summary>
    /// Sets the entity's LastEventId
    /// </summary>
    internal void UpdateLastEvent(int last_id)
    {
        //GameDebug.Log("UpdateLastEvent "+Uid+ " with event id "+last_id+ " at "+System.Environment.StackTrace);
        LastEventId = last_id;
    }

    /// <summary>
    /// Adds a new Buff entity to Unit. If another of the same type exists, it gets destroyed!
    /// </summary>
    /// <param name="buff">new BuffEntity</param>
    internal void ClientAddBuffEntity(BuffEntity buff)
    {
        // if buff of given type already exists on target, remove it
        RemoveBuffEntity(buff.Type);

        // keep track of list of buffs on target
        current_buffs_.Add(buff.Type, buff.Uid);
    }

    /// <summary>
    /// Adds a new Buff entity to Unit. If another of the same type exists, it gets destroyed!
    /// </summary>
    /// <param name="buff">new BuffEntity</param>
    internal void ServerAddBuffEntity(BuffEntity buff)
    {
        // if buff of given type already exists on target, remove it
        if (current_buffs_.ContainsKey(buff.Type))
        {
            EntityManager.AsyncCreateTempEvent(new DebuffRD(current_buffs_[buff.Type]));
            RemoveBuffEntity(buff.Type);
        }

        // keep track of list of buffs on target
        current_buffs_.Add(buff.Type, buff.Uid);
    }

    internal void RemoveBuffEntity(BuffEntity buff)
    {
        if (current_buffs_.ContainsKey(buff.Type) && current_buffs_[buff.Type] == buff.Uid)
        {
            current_buffs_.Remove(buff.Type);
        }
    }

    internal void RemoveBuffEntity(BuffEntityCode buff_type)
    {
        if (current_buffs_.ContainsKey(buff_type))
        {
            current_buffs_.Remove(buff_type);
        }
    }

    internal bool HasBuffType(BuffEntityCode buff_type)
    {
        return current_buffs_.ContainsKey(buff_type);
    }
}
