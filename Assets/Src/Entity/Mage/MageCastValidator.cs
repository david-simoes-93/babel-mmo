using UnityEngine;
using static Globals;
using System;
using System.Linq;
using System.Collections.Generic;

internal class MageCastValidator : BaseCastValidator
{
    private long timeOfLastAttack_ = 0;
    private long timeOfLastPyroblast_ = 0,
        timeOfLastRenew_ = 0;

    internal const int kFireflashRange = 30,
        kFireflashDamage = 5;
    internal const int kFrostflashRange = 30,
        kFrostflashDamage = 10;
    internal const int kArcaneflashRange = 30,
        kArcaneflashDamage = 20;
    internal const int kPyroblastCooldown = 10000,
        kPyroblastRange = 60,
        kPyroblastDamage = 100,
        kPyroblastChannelingTime = 2000; // Assumption: CD > Channeling
    internal const int kRenewCooldown = 2000,
        kRenewRange = 60,
        kRenewHeal = 20,
        kRenewChannelingTime = 1200; // Assumption: CD > Channeling
    internal const int kGlobalCooldown = 1000;
    private GameObject cameraTarget_;
    private Transform myTransform_;
    private int myUid_;

    internal CastCode channelingSpell { get; private set; }

    /// <summary>
    /// Called by Validate() to determine validity of a class-specific CastRD (ability not in cooldown, valid targets, etc)
    /// </summary>
    /// <param name="rd"></param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificValidateCast(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.MageFireflash:
                return CanFireflash((rd as TargetedCastRD).target_uid);
            case CastCode.MageFrostflash:
                return CanFrostflash((rd as TargetedCastRD).target_uid);
            case CastCode.MageArcaneflash:
                return CanArcaneflash((rd as TargetedCastRD).target_uid);
            case CastCode.MagePyroblast:
                return CanPyroblast((rd as TargetedCastRD).target_uid);
            case CastCode.MagePyroblastEnd:
                return CanPyroblastEnd((rd as TargetedCastRD).target_uid);
            case CastCode.MageRenew:
                return CanRenew((rd as TargetedCastRD).target_uid);
            case CastCode.MageRenewEnd:
                return CanRenewEnd((rd as TargetedCastRD).target_uid);
            case CastCode.MageCastStop:
                return IsChannelingSpell();
            default:
                return false;
        }
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        cameraTarget_ = parent.UnitTransform().Find("Root").Find("CameraTarget").gameObject;
        myTransform_ = parent.UnitTransform();
        channelingSpell = CastCode.None;
        myUid_ = parent.Uid;
    }

    /// <summary>
    /// Called by ProcessServerAcks() to process class-specific CastRDs received from the server
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificProcessAck(CastRD rd)
    {
        CastCode cd = rd.type;

        switch (cd)
        {
            case CastCode.MageFireflash:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kMageFireflash);
                delayedEvents_.Add(currTime_ms + 0, rd);

                timeOfLastAttack_ = currTime_ms;
                break;
            case CastCode.MageFrostflash:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kMageFrostflash);
                delayedEvents_.Add(currTime_ms + 0, rd);

                timeOfLastAttack_ = currTime_ms;
                break;
            case CastCode.MageArcaneflash:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kMageArcaneflash);
                delayedEvents_.Add(currTime_ms + 0, rd);

                timeOfLastAttack_ = currTime_ms;
                break;
            case CastCode.MagePyroblast:
                controller_.TransitionToState(CharacterState.MageChanneling);
                delayedEvents_.Add(currTime_ms + kPyroblastChannelingTime, rd);

                timeOfLastAttack_ = currTime_ms;
                timeOfLastPyroblast_ = currTime_ms;
                channelingSpell = CastCode.MagePyroblast;
                break;
            case CastCode.MageRenew:
                controller_.TransitionToState(CharacterState.MageChanneling);
                delayedEvents_.Add(currTime_ms + kRenewChannelingTime, rd);

                timeOfLastAttack_ = currTime_ms;
                timeOfLastRenew_ = currTime_ms;
                channelingSpell = CastCode.MageRenew;
                break;
            case CastCode.MagePyroblastEnd:
            // fallthrough
            case CastCode.MageRenewEnd:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kMageChannelEnd);
                channelingSpell = CastCode.None;
                controller_.TransitionToState(CharacterState.Default);
                break;
            case CastCode.MageCastStop:
                channelingSpell = CastCode.None;
                if (controller_.CurrentCharacterState == CharacterState.MageChanneling)
                {
                    controller_.TransitionToState(CharacterState.Default);
                }
                break;
            default:
                GameDebug.Log("Unknown cast: " + cd);
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called by ProcessServerAcks() to process class-specific CastRDs received from the server and that act with a delay
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificProcessDelayedCast(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.MageFireflash:
#if !UNITY_SERVER
                ClientFireflash(rd as TargetedCastRD);
#else
                ServerFireflash(rd as TargetedCastRD);
#endif
                break;
            case CastCode.MageFrostflash:
#if !UNITY_SERVER
                ClientFrostflash(rd as TargetedCastRD);
#else
                ServerFrostflash(rd as TargetedCastRD);
#endif
                break;
            case CastCode.MageArcaneflash:
#if !UNITY_SERVER
                ClientArcaneflash(rd as TargetedCastRD);
#else
                ServerArcaneflash(rd as TargetedCastRD);
#endif
                break;
            case CastCode.MagePyroblast:
#if !UNITY_SERVER
                ClientPyroblastEnd(rd as TargetedCastRD);
#else
                ServerPyroblastEnd(rd as TargetedCastRD);
#endif
                break;
            case CastCode.MageRenew:
#if !UNITY_SERVER
                ClientRenewEnd(rd as TargetedCastRD);
#else
                ServerRenewEnd(rd as TargetedCastRD);
#endif
                break;
            case CastCode.MagePyroblastEnd:
            case CastCode.MageRenewEnd:
                break;
            case CastCode.MageCastStop:
                break;
            default:
                GameDebug.Log("Unknown cast: " + rd.type);
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called by ServersideCheck(), processes class-specific event with server-side info, before being broadcast to clients
    /// </summary>
    /// <param name="rd">the CastRD event</param>
    internal override void SpecificServersideCheck(CastRD rd) { }

    /// <summary>
    /// Whether Fireflash is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanFireflash(int target_uid)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        return CanFireflash(target);
    }

    /// <summary>
    /// Whether Fireflash is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanFireflash(UnitEntity target)
    {
        if (!CanCastSpell(target, kFireflashRange))
            return false;

        return target.Uid != myUid_;
    }

    /// <summary>
    /// Whether Frostflash is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanFrostflash(int target_uid)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        return CanFrostflash(target);
    }

    /// <summary>
    /// Whether Frostflash is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanFrostflash(UnitEntity target)
    {
        if (!CanCastSpell(target, kFrostflashRange))
            return false;

        return target.Uid != myUid_;
    }

    /// <summary>
    /// Whether Arcaneflash is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanArcaneflash(int target_uid)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        return CanArcaneflash(target);
    }

    /// <summary>
    /// Whether Arcaneflash is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanArcaneflash(UnitEntity target)
    {
        if (!CanCastSpell(target, kArcaneflashRange))
            return false;

        return target.Uid != myUid_;
    }

    /// <summary>
    /// Whether Pyroblast is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanPyroblast(int target_uid)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        return CanPyroblast(target);
    }

    /// <summary>
    /// Whether Pyroblast is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanPyroblast(UnitEntity target)
    {
        if (!CanCastSpell(target, kPyroblastRange))
            return false;

        return target.Uid != myUid_ && currTime_ms - timeOfLastPyroblast_ > kPyroblastCooldown && motor_.GroundingStatus.IsStableOnGround;
    }

    /// <summary>
    /// Whether Pyroblast can finish casting
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanPyroblastEnd(int target_uid)
    {
        // assuming cooldown > cast time; if that's not the case, someone can cast pyro, stop casting, cast pyro again, and this check won't work
        if (channelingSpell != CastCode.MagePyroblast || controller_.CurrentCharacterState != CharacterState.MageChanneling)
        {
            return false;
        }

        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        if (target == null)
        {
            return false;
        }

        return target.IsAttackable; // TODO: check LoS, maybe no need to check if dead
    }

    /// <summary>
    /// Whether Renew is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanRenew(int target_uid)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        return CanRenew(target);
    }

    /// <summary>
    /// Whether Renew is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanRenew(UnitEntity target)
    {
        if (!CanCastSpell(target, kRenewRange))
            return false;

        return currTime_ms - timeOfLastRenew_ > kRenewCooldown && motor_.GroundingStatus.IsStableOnGround;
    }

    /// <summary>
    /// Whether Pyroblast can finish casting
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanRenewEnd(int target_uid)
    {
        // assuming cooldown > cast time; if that's not the case, someone can cast pyro, stop casting, cast pyro again, and this check won't work
        if (channelingSpell != CastCode.MageRenew || controller_.CurrentCharacterState != CharacterState.MageChanneling)
            return false;

        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(target_uid);
        if (target == null)
        {
            return false;
        }

        return target.IsAttackable; // TODO: check LoS, maybe no need to check if dead
    }

    /// <summary>
    /// Checks if caster is alive, target exists and is attackable, caster is not on global cooldown, and target is within range
    /// </summary>
    /// <param name="target"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    private bool CanCastSpell(UnitEntity target, int range)
    {
        if (target == null)
        {
            return false;
        }

        return !parent_.IsDead
            && target.IsAttackable
            && currTime_ms > timeOfLastAttack_ + 1000
            && (myTransform_.position - target.UnitTransform().position).magnitude < range; // TODO: check LoS
    }

    /// <summary>
    /// Returns whether Mage is channeling a spell
    /// </summary>
    /// <returns></returns>
    internal bool IsChannelingSpell()
    {
        return channelingSpell != CastCode.None;
    }

    /// <summary>
    /// Returns the progress [0, 1] of the spell being channeled (0 means channel just begun, 1 means channel is complete)
    /// </summary>
    /// <returns></returns>
    internal float ChannelingProgress()
    {
        if (!IsChannelingSpell())
        {
            return 1;
        }

        float progress;
        switch (channelingSpell)
        {
            case CastCode.MagePyroblast:
                progress = (currTime_ms - timeOfLastAttack_) / (float)kPyroblastChannelingTime;
                break;
            case CastCode.MageRenew:
                progress = (currTime_ms - timeOfLastAttack_) / (float)kRenewChannelingTime;
                break;
            default:
                GameDebug.Log("Unknown channelingSpell to calculate progress!");
                return 1;
        }

        return Math.Max(Math.Min(progress, 1), 0);
    }

    /// <summary>
    /// Client-side call. Mage casts a Fireflash
    /// </summary>
    /// <param name="rd">the Fireflash cast</param>
    private void ClientFireflash(TargetedCastRD rd)
    {
        // TODO make proper fire flash on target
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (source == null || target == null)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(source.TargetingTransform.position, target.TargetingTransform.position, 0.01f, Color.red));
    }

    /// <summary>
    /// Server-side call. Mage casts a Fireflash
    /// </summary>
    /// <param name="rd">the Fireflash cast</param>
    private void ServerFireflash(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kFireflashDamage));
        target.EntityManager.AsyncCreateTempEvent(
            new BuffRD(parent_.EntityManager.GetValidNpcUid(), parent_.Uid, rd.target_uid, Globals.BuffEntityCode.kFireflashDebuff)
        );
    }

    /// <summary>
    /// Client-side call. Mage casts a Frosbolt
    /// </summary>
    /// <param name="rd">the Frosbolt cast</param>
    private void ClientFrostflash(TargetedCastRD rd)
    {
        // TODO make proper frost flash on target
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (source == null || target == null)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(source.TargetingTransform.position, target.TargetingTransform.position, 0.01f, Color.blue));
    }

    /// <summary>
    /// Server-side call. Mage casts a Frostflash
    /// </summary>
    /// <param name="rd">the Frostflash cast</param>
    private void ServerFrostflash(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kFrostflashDamage));
        target.EntityManager.AsyncCreateTempEvent(
            new BuffRD(parent_.EntityManager.GetValidNpcUid(), parent_.Uid, rd.target_uid, Globals.BuffEntityCode.kFrostflashDebuff)
        );
    }

    /// <summary>
    /// Client-side call. Mage casts an Arcaneflash
    /// </summary>
    /// <param name="rd">the Arcaneflash cast</param>
    private void ClientArcaneflash(TargetedCastRD rd)
    {
        // TODO make proper arcane flash on target
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (source == null || target == null)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(
            new LaserEffect(source.TargetingTransform.position, target.TargetingTransform.position, 0.01f, Color.magenta)
        );
    }

    /// <summary>
    /// Server-side call. Mage casts an Arcaneflash
    /// </summary>
    /// <param name="rd">the Arcaneflash cast</param>
    private void ServerArcaneflash(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kArcaneflashDamage));
    }

    /// <summary>
    /// Client-side call. Mage finishes casting a Pyroblast
    /// </summary>
    /// <param name="rd">the Pyroblast cast</param>
    private void ClientPyroblastEnd(TargetedCastRD rd)
    {
        // TODO make proper pyroblast chasing target
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (source == null || target == null)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(source.TargetingTransform.position, target.TargetingTransform.position, 0.005f, Color.red));
    }

    /// <summary>
    /// Server-side call. Mage finishes casting a Pyroblast
    /// </summary>
    /// <param name="rd">the Pyroblast cast</param>
    private void ServerPyroblastEnd(TargetedCastRD rd)
    {
        if (!CanPyroblastEnd(rd.target_uid))
        {
            return;
        }

        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(CastUtils.MakeMagePyroblastEnd(rd));
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kPyroblastDamage));
    }

    /// <summary>
    /// Server-side call. Mage finishes casting  a Renew
    /// </summary>
    /// <param name="rd">the Renew cast</param>
    private void ClientRenewEnd(TargetedCastRD rd)
    {
        // TODO make proper pyroblast chasing target
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (source == null || target == null)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(
            new LaserEffect(source.TargetingTransform.position, target.TargetingTransform.position, 0.005f, Color.green)
        );
    }

    /// <summary>
    /// Server-side call. Mage finishes casting  a Renew
    /// </summary>
    /// <param name="rd">the Renew cast</param>
    private void ServerRenewEnd(TargetedCastRD rd)
    {
        if (!CanRenewEnd(rd.target_uid))
        {
            return;
        }

        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(CastUtils.MakeMageRenewEnd(rd));
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kRenewHeal));
    }

    /// <summary>
    /// Return cooldown fraction of Fireflash
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownFireflash()
    {
        if (currTime_ms >= timeOfLastAttack_ + kGlobalCooldown)
            return 0;
        return (timeOfLastAttack_ + kGlobalCooldown - currTime_ms) * 1.0f / kGlobalCooldown;
    }

    /// <summary>
    /// Return cooldown fraction of Frostflash
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownFrostflash()
    {
        if (currTime_ms >= timeOfLastAttack_ + kGlobalCooldown)
            return 0;
        return (timeOfLastAttack_ + kGlobalCooldown - currTime_ms) * 1.0f / kGlobalCooldown;
    }

    /// <summary>
    /// Return cooldown fraction of Arcaneflash
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownArcaneflash()
    {
        if (currTime_ms >= timeOfLastAttack_ + kGlobalCooldown)
            return 0;
        return (timeOfLastAttack_ + kGlobalCooldown - currTime_ms) * 1.0f / kGlobalCooldown;
    }

    /// <summary>
    /// Return cooldown fraction of Pyroblast
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownPyroblast()
    {
        return CooldownSpell(timeOfLastPyroblast_, kPyroblastCooldown);
    }

    /// <summary>
    /// Return cooldown fraction of Renew
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownRenew()
    {
        return CooldownSpell(timeOfLastRenew_, kRenewCooldown);
    }

    /// <summary>
    /// Return cooldown fraction of a specific spell
    /// </summary>
    /// <param name="specificSpellLastTime">timeOfLast*_ value</param>
    /// <param name="specificSpellCooldown">k*Cooldown value</param>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    private float CooldownSpell(long specificSpellLastTime, int specificSpellCooldown)
    {
        long cooldown_over_time;
        int cooldown_time;
        // sets values depending on which ends later: GCD or Pyroblast CD
        if (specificSpellLastTime + specificSpellCooldown > timeOfLastAttack_ + kGlobalCooldown)
        {
            cooldown_over_time = specificSpellLastTime + specificSpellCooldown;
            cooldown_time = specificSpellCooldown;
        }
        else
        {
            cooldown_over_time = timeOfLastAttack_ + kGlobalCooldown;
            cooldown_time = kGlobalCooldown;
        }
        if (currTime_ms >= cooldown_over_time)
            return 0;
        return (cooldown_over_time - currTime_ms) * 1.0f / cooldown_time;
    }
}
