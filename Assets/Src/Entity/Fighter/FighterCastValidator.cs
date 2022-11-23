using UnityEngine;
using static Globals;
using System;
using System.Linq;
using System.Collections.Generic;

internal class FighterCastValidator : BaseCastValidator
{
    private long timeOfLastAttackLeft_ = 0,
        timeOfLastAttackRight_ = 0,
        timeOfLastAttack_ = 0,
        lastAttackCooldown = 0;
    private long timeWhenLastAttackCooldownEnded_ = 0;
    private long timeOfLastSpin_ = 0;
    private long timeOfLastCharge_ = 0;
    private bool chargeHasStunned_ = false;
    private long timeOfLastDodge_ = 0;
    private CastCode[] comboCounter_ = { CastCode.None, CastCode.None, CastCode.None, CastCode.None, CastCode.None };

    internal const int kComboBreakTime = 500;
    internal const int kAttackLeftCooldown = 1000,
        kAttackLeftRange = 2,
        kAttackLeftDamage = 20;
    internal const int kAttackRightCooldown = 2000,
        kAttackRightRange = 2,
        kAttackRightDamage = 40;
    internal const int kSpinLength = 600,
        kSpinRadius = 4,
        kSpinDamage = 15;
    internal const int kChargeLength_s = 1,
        kChargeCooldown = 10000;
    internal const int kDodgeCooldown = 5000,
        kDodgeSpeed = 30;
    internal const float kDodgeLength_s = 0.15f;

    internal readonly static CastCode[] kComboSpin1 =
    {
        CastCode.FighterAttackLeft,
        CastCode.FighterAttackRight,
        CastCode.FighterAttackLeft,
        CastCode.FighterAttackRight,
        CastCode.FighterAttackLeft
    };
    internal readonly static CastCode[] kComboSpin2 =
    {
        CastCode.FighterAttackRight,
        CastCode.FighterAttackLeft,
        CastCode.FighterAttackRight,
        CastCode.FighterAttackLeft,
        CastCode.FighterAttackRight
    };
    internal readonly static Tuple<CastCode, CastCode[]>[] kCombos =
    {
        new Tuple<CastCode, CastCode[]>(CastCode.Spin, kComboSpin1),
        new Tuple<CastCode, CastCode[]>(CastCode.Spin, kComboSpin2)
    };

    private FighterCanvas fighterGui_;
    private GameObject cameraTarget_;

    /// <summary>
    /// Called by Validate() to determine validity of a class-specific CastRD (ability not in cooldown, valid targets, etc)
    /// </summary>
    /// <param name="rd"></param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificValidateCast(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.FighterAttackLeft:
                return CanAttackLeft();
            case CastCode.FighterAttackRight:
                return CanAttackRight();
            case CastCode.FighterCharge:
                return CanCharge();
            case CastCode.FighterChargeStun:
                return CanChargeStun();
            case CastCode.DodgeBack:
                return CanDodge();
            case CastCode.DodgeFront:
                return CanDodge();
            case CastCode.DodgeLeft:
                return CanDodge();
            case CastCode.DodgeRight:
                return CanDodge();
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
        fighterGui_ = (FighterCanvas)parent.Canvas;
        cameraTarget_ = parent.UnitTransform().Find("Root").Find("CameraTarget").gameObject;
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
            case CastCode.Spin:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterSpin);
                delayedEvents_.Add(currTime_ms + 300, rd);
                delayedEvents_.Add(currTime_ms + 700, rd);

                timeOfLastSpin_ = currTime_ms;
                timeWhenLastAttackCooldownEnded_ = currTime_ms + kAttackLeftCooldown;
                break;
            case CastCode.FighterAttackLeft:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterAttackLeft);
                delayedEvents_.Add(currTime_ms + 200, rd);

                timeOfLastAttackLeft_ = currTime_ms;
                timeOfLastAttack_ = timeOfLastAttackLeft_;
                lastAttackCooldown = kAttackLeftCooldown;
                timeWhenLastAttackCooldownEnded_ = currTime_ms + kAttackLeftCooldown;
                break;
            case CastCode.FighterAttackRight:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterAttackRight);
                delayedEvents_.Add(currTime_ms + 400, rd);

                timeOfLastAttackRight_ = currTime_ms;
                timeOfLastAttack_ = timeOfLastAttackRight_;
                lastAttackCooldown = kAttackRightCooldown;
                timeWhenLastAttackCooldownEnded_ = currTime_ms + kAttackRightCooldown;
                break;
            case CastCode.FighterCharge:
                timeOfLastCharge_ = currTime_ms;
                controller_.TransitionToState(CharacterState.FighterCharging);
                chargeHasStunned_ = false;
                break;
            case CastCode.FighterChargeStun:
#if !UNITY_SERVER
                //
#else
                ServerChargeStun(rd as TargetedCastRD);
#endif
                chargeHasStunned_ = true;

                break;
            case CastCode.DodgeBack:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterDodgeBack);
                timeOfLastDodge_ = currTime_ms;
                controller_.TransitionToState(CharacterState.FighterDodgingBack);
                break;
            case CastCode.DodgeFront:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterDodgeFront);
                timeOfLastDodge_ = currTime_ms;
                controller_.TransitionToState(CharacterState.FighterDodgingFront);
                break;
            case CastCode.DodgeLeft:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterDodgeLeft);
                timeOfLastDodge_ = currTime_ms;
                controller_.TransitionToState(CharacterState.FighterDodgingLeft);
                break;
            case CastCode.DodgeRight:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kFighterDodgeRight);
                timeOfLastDodge_ = currTime_ms;
                controller_.TransitionToState(CharacterState.FighterDodgingRight);
                break;
            default:
                GameDebug.Log("Unknown cast: " + cd);
                return false;
        }

#if !UNITY_SERVER
        // Actual client's controller, not the controller of other entities
        if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
        {
            ShiftCombo(cd);
        }
#endif
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
            case CastCode.Spin:
#if !UNITY_SERVER
                ClientSpin(rd);
#else
                ServerSpin(rd);
#endif
                break;
            case CastCode.FighterAttackLeft:
#if !UNITY_SERVER
                ClientAttackLeft(rd);
#else
                ServerAttackLeft(rd);
#endif
                break;
            case CastCode.FighterAttackRight:
#if !UNITY_SERVER
                ClientAttackRight(rd);
#else
                ServerAttackRight(rd);
#endif
                break;
            case CastCode.FighterCharge:
                //
                break;
            case CastCode.FighterChargeStun:
                //
                break;
            case CastCode.DodgeBack:
                //
                break;
            case CastCode.DodgeFront:
                //
                break;
            case CastCode.DodgeLeft:
                //
                break;
            case CastCode.DodgeRight:
                //
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
    internal override void SpecificServersideCheck(CastRD rd)
    {
        if (rd.type != CastCode.FighterAttackLeft && rd.type != CastCode.FighterAttackRight)
            return;

        // break combo if adequate
        CheckAndResetCombo();

        // shift and add new cast
        ShiftCombo(rd.type);
        //GameDebug.Log(comboCounter[0]+" "+ comboCounter[1] + " " + comboCounter[2] + " " + comboCounter[3] + " " + comboCounter[4]);

        //if combo
        foreach (Tuple<CastCode, CastCode[]> combo in kCombos)
        {
            if (Enumerable.SequenceEqual(comboCounter_, combo.Item2))
            {
                comboCounter_ = new CastCode[] { CastCode.None, CastCode.None, CastCode.None, CastCode.None, CastCode.None };
                timeWhenLastAttackCooldownEnded_ = 0;
                rd.type = combo.Item1;
                return;
            }
        }
    }

    /// <summary>
    /// Whether FighterAttackLeft is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanAttackLeft()
    {
        return !parent_.IsDead && currTime_ms > timeWhenLastAttackCooldownEnded_;
    }

    /// <summary>
    /// Whether FighterAttackRight is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanAttackRight()
    {
        return !parent_.IsDead && currTime_ms > timeWhenLastAttackCooldownEnded_;
    }

    /// <summary>
    /// Whether Charge is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanCharge()
    {
        return !parent_.IsDead && currTime_ms - timeOfLastCharge_ > kChargeCooldown && motor_.GroundingStatus.IsStableOnGround;
    }

    /// <summary>
    /// Whether ChargeStun is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanChargeStun()
    {
        return !parent_.IsDead && !chargeHasStunned_ && currTime_ms - timeOfLastCharge_ < kChargeLength_s * 1000;
        // ideally, we'd check controller_.CurrentCharacterState == CharacterState.FighterCharging instead of currTime_ms - timeOfLastCharge_ < kChargeLength_s * 1000,
        //  but the fighter changes states to Default in the time that the ChargeStun takes to reach the server, which would make this invalid.
    }

    /// <summary>
    /// Whether Dodge is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanDodge()
    {
        return !parent_.IsDead && currTime_ms - timeOfLastDodge_ > kDodgeCooldown && motor_.GroundingStatus.IsStableOnGround;
    }

    /// <summary>
    /// Checks if Fighter combo should reset, resets it if adequate
    /// </summary>
    internal void CheckAndResetCombo()
    {
        // combo broke
        if (currTime_ms - kComboBreakTime > timeWhenLastAttackCooldownEnded_ && comboCounter_[4] != CastCode.None)
        {
            comboCounter_ = new CastCode[] { CastCode.None, CastCode.None, CastCode.None, CastCode.None, CastCode.None };
#if !UNITY_SERVER
            fighterGui_.ResetCombo();
#endif
        }
    }

    /// <summary>
    /// Inserts a new cast into the Fighter combo, shifting it to lose the oldest Cast
    /// </summary>
    /// <param name="cd">the new cast</param>
    private void ShiftCombo(CastCode cd)
    {
        if (cd != CastCode.FighterAttackLeft && cd != CastCode.FighterAttackRight && cd != CastCode.Spin)
            return;

        int lastIndex = comboCounter_.Length - 1;
        // shift and add new cast
        for (int i = 0; i < comboCounter_.Length - 1; i++)
        {
            comboCounter_[i] = comboCounter_[i + 1];
        }
        comboCounter_[lastIndex] = cd;

#if !UNITY_SERVER
        for (int i = 0; i < comboCounter_.Length; i++)
        {
            fighterGui_.SetAttackCombo(i, comboCounter_[i]);
        }
#endif
    }

    /// <summary>
    /// Client-side call. Fighter casts a Spin
    /// </summary>
    /// <param name="rd">the Spin cast</param>
    private void ClientSpin(CastRD rd)
    {
        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(cameraTarget_.transform.position, kSpinRadius, gameObject_);

        foreach (UnitEntity otherChar in collidedTargets)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(
                new GenericTemporaryEffect(otherChar.TargetingTransform.position, Quaternion.identity, 0.5f, Globals.kFighterHitPrefab, 1000)
            );
        }
    }

    /// <summary>
    /// Server-side call. Fighter casts a Spin
    /// </summary>
    /// <param name="rd">the Spin cast</param>
    private void ServerSpin(CastRD rd)
    {
        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(cameraTarget_.transform.position, kSpinRadius, gameObject_);

        foreach (UnitEntity otherChar in collidedTargets)
        {
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, otherChar.Uid, rd.type, kSpinDamage));
            GameDebug.Log("Spin: " + otherChar.Name + " @ " + otherChar.Health);
        }
    }

    /// <summary>
    /// Client-side call. Fighter casts an FighterAttackLeft
    /// </summary>
    /// <param name="rd">the FighterAttackLeft cast</param>
    private void ClientAttackLeft(CastRD rd)
    {
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionForwardOnEnemies(
            cameraTarget_.transform.position,
            kAttackLeftRange,
            gameObject_
        );

        if (collidedTarget != null)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new GenericTemporaryEffect(collisionPoint, Quaternion.identity, 0.3f, Globals.kFighterHitPrefab, 1000));
        }
    }

    /// <summary>
    /// Server-side call. Fighter casts an FighterAttackLeft
    /// </summary>
    /// <param name="rd">the FighterAttackLeft cast</param>
    private void ServerAttackLeft(CastRD rd)
    {
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionForwardOnEnemies(
            cameraTarget_.transform.position,
            kAttackLeftRange,
            gameObject_
        );

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, kAttackLeftDamage));
            GameDebug.Log("FighterAttackLeft: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Client-side call. Fighter casts an FighterAttackRight
    /// </summary>
    /// <param name="rd">the FighterAttackRight cast</param>
    private void ClientAttackRight(CastRD rd)
    {
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionForwardOnEnemies(
            cameraTarget_.transform.position,
            kAttackRightRange,
            gameObject_
        );

        if (collidedTarget != null)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new GenericTemporaryEffect(collisionPoint, Quaternion.identity, 0.5f, Globals.kFighterHitPrefab, 1000));
        }
    }

    /// <summary>
    /// Server-side call. Fighter casts an FighterAttackRight
    /// </summary>
    /// <param name="rd">the FighterAttackRight cast</param>
    private void ServerAttackRight(CastRD rd)
    {
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionForwardOnEnemies(
            cameraTarget_.transform.position,
            kAttackRightRange,
            gameObject_
        );

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, kAttackRightDamage));
            GameDebug.Log("FighterAttackRight: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Server-side call. Fighter casts an FighterAttackRight
    /// </summary>
    /// <param name="rd">the FighterAttackRight cast</param>
    private void ServerChargeStun(TargetedCastRD rd)
    {
        parent_.EntityManager.AsyncCreateTempEvent(
            new BuffRD(parent_.EntityManager.GetValidNpcUid(), rd.caster_uid, rd.target_uid, Globals.BuffEntityCode.kChargeDebuff)
        );
        GameDebug.Log("FighterChargeStun: " + rd.target_uid);
    }

    /// <summary>
    /// Return cooldown fraction of FighterAttackLeft
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownAttackLeft()
    {
        if (currTime_ms >= timeOfLastAttack_ + lastAttackCooldown)
            return 0;
        return (timeOfLastAttack_ + lastAttackCooldown - currTime_ms) * 1.0f / lastAttackCooldown;
    }

    /// <summary>
    /// Return cooldown fraction of FighterAttackRight
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownAttackRight()
    {
        if (currTime_ms >= timeOfLastAttack_ + lastAttackCooldown)
            return 0;
        return (timeOfLastAttack_ + lastAttackCooldown - currTime_ms) * 1.0f / lastAttackCooldown;
    }

    /// <summary>
    /// Return cooldown fraction of Charge
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownCharge()
    {
        if (currTime_ms >= timeOfLastCharge_ + kChargeCooldown)
            return 0;
        return (timeOfLastCharge_ + kChargeCooldown - currTime_ms) * 1.0f / kChargeCooldown;
    }

    /// <summary>
    /// Return cooldown fraction of Dodge
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownDodge()
    {
        if (currTime_ms >= timeOfLastDodge_ + kDodgeCooldown)
            return 0;
        return (timeOfLastDodge_ + kDodgeCooldown - currTime_ms) * 1.0f / kDodgeCooldown;
    }
}
