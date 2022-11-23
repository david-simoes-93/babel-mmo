using System.Collections.Generic;
using UnityEngine;
using static Globals;

internal class MonsterCastValidator : BaseCastValidator
{
    internal long timeWhenLastAttackCooldownEnded_;

    internal const int kAttackLeftCooldown = 1000,
        kAttackLeftRange = 4,
        kAttackLeftDamage = 10;
    internal const int kAttackRightCooldown = 2000,
        kAttackRightRange = 2,
        kAttackRightDamage = 20;
    internal const int kRangedAttackCooldown = 1000,
        kRangedAttackRange = 9,
        kRangedAttackDamage = 5;

    /// <summary>
    /// Called by Validate() to determine validity of a class-specific CastRD (ability not in cooldown, valid targets, etc)
    /// </summary>
    /// <param name="rd"></param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificValidateCast(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.MonsterAttackLeft:
                return CanAttackLeft(currTime_ms);
            case CastCode.MonsterAttackRight:
                return CanAttackRight(currTime_ms);
            case CastCode.MonsterRangedAttack:
                return CanRangedAttack(currTime_ms);
            case CastCode.DryadSuicideBomb:
                return CanSuicide(currTime_ms);
            default:
                return false;
        }
    }

    /// <summary>
    /// Whether MonsterAttackLeft is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanAttackLeft(long currTime)
    {
        return !parent_.IsDead && currTime > timeWhenLastAttackCooldownEnded_;
    }

    /// <summary>
    /// Whether MonsterAttackRight is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanAttackRight(long currTime)
    {
        return !parent_.IsDead && currTime > timeWhenLastAttackCooldownEnded_;
    }

    /// <summary>
    /// Whether MonsterRangedAttack is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanRangedAttack(long currTime)
    {
        return !parent_.IsDead && currTime > timeWhenLastAttackCooldownEnded_;
    }

    /// <summary>
    /// Whether CanSuicide is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanSuicide(long currTime)
    {
        return !parent_.IsDead;
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent) { }

    /// <summary>
    /// Called by ProcessServerAcks() to process class-specific CastRDs received from the server
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificProcessAck(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.MonsterAttackLeft:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kNpcAttackLeft);
                delayedEvents_.Add(currTime_ms + 200, rd);
                timeWhenLastAttackCooldownEnded_ = currTime_ms + kAttackLeftCooldown;
                break;
            case CastCode.MonsterAttackRight:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kNpcAttackRight);
                delayedEvents_.Add(currTime_ms + 200, rd);
                timeWhenLastAttackCooldownEnded_ = currTime_ms + kAttackRightCooldown;
                break;
            case CastCode.MonsterRangedAttack:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kNpcAttackLeft);
                delayedEvents_.Add(currTime_ms + 200, rd);
                timeWhenLastAttackCooldownEnded_ = currTime_ms + kRangedAttackCooldown;
                break;
            case CastCode.DryadSuicideBomb:
                delayedEvents_.Add(currTime_ms, rd);
                break;
            case CastCode.ValkyrieStunCarry:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kNpcAttackRight);
                delayedEvents_.Add(currTime_ms, rd);
                break;
            case CastCode.ValkyrieDropCarried:
                delayedEvents_.Add(currTime_ms, rd);
                break;
            case CastCode.FreyjaPoisonZone:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kNpcAttackRight);
                break;
            case CastCode.ThorSlam:
                // Animations don't always start IMMEDIATELY, so we instead have the character jump
                //parent_.SetAnimatorTrigger(EntityAnimationTrigger.kThorSlam);
                delayedEvents_.Add(currTime_ms + 1300, rd);
                break;
            case CastCode.ThorChainLightning:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kThorChainLightning);
                delayedEvents_.Add(currTime_ms + 400, rd);
                break;
            case CastCode.LokiSplitStart:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kLokiSplitStart);
                parent_.SetTargetable(false);
                parent_.SetInvulnerable(true);
                // TODO clear all debuffs
                break;
            case CastCode.LokiSplitEnd:
                parent_.SetTargetable(true);
                parent_.SetInvulnerable(false);
                delayedEvents_.Add(currTime_ms, rd);
                break;
            default:
                GameDebug.Log("Unknown cast: " + rd.type);
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
            case CastCode.MonsterAttackLeft:
#if !UNITY_SERVER
                ClientAttackLeft(rd as TargetedCastRD);
#else
                ServerAttackLeft(rd as TargetedCastRD);
#endif
                break;
            case CastCode.MonsterAttackRight:
#if !UNITY_SERVER
                //
#else
                ServerAttackRight(rd as VectorCastRD);
#endif
                break;
            case CastCode.MonsterRangedAttack:
#if !UNITY_SERVER
                ClientRangedAttack(rd as TargetedCastRD);
#else
                ServerRangedAttack(rd as TargetedCastRD);
#endif
                break;
            case CastCode.DryadSuicideBomb:
#if !UNITY_SERVER
                ClientSuicideBomb(rd);
#else
                ServerSuicideBomb(rd);
#endif
                break;
            case CastCode.ValkyrieStunCarry:
#if !UNITY_SERVER
                ClientStunCarry(rd as TargetedCastRD);
#else
                ServerStunCarry(rd as TargetedCastRD);
#endif
                break;
            case CastCode.ValkyrieDropCarried:
#if !UNITY_SERVER
                ClientDropCarried(rd as TargetedCastRD);
#else
                ServerDropCarried(rd as TargetedCastRD);
#endif
                break;
            case CastCode.FreyjaPoisonZone:
#if !UNITY_SERVER

#else
                
#endif
                break;
            case CastCode.ThorSlam:
#if !UNITY_SERVER
                ClientThorSlam(rd as VectorCastRD);
#else
                ServerThorSlam(rd as VectorCastRD);
#endif
                break;
            case CastCode.ThorChainLightning:
#if !UNITY_SERVER
                ClientThorChainLightning(rd as MultiTargetedCastRD);
#else
                ServerThorChainLightning(rd as MultiTargetedCastRD);
#endif
                break;
            case CastCode.LokiSplitStart:
#if !UNITY_SERVER

#else

#endif
                break;
            case CastCode.LokiSplitEnd:
#if !UNITY_SERVER
                //
#else
                ServerLokiSplitEnd(rd as VectorCastRD);
#endif
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
    /// Client-side call. NPC casts a MonsterAttackLeft
    /// </summary>
    /// <param name="rd">the MonsterAttackLeft cast</param>
    private void ClientAttackLeft(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(
            new GenericTemporaryEffect(target.TargetingTransform.position, Quaternion.identity, 0.5f, Globals.kMonsterHitPrefab, 1000)
        );
    }

    /// <summary>
    /// Server-side call. NPC casts a MonsterAttackLeft
    /// </summary>
    /// <param name="rd">the MonsterAttackLeft cast</param>
    private void ServerAttackLeft(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kAttackLeftDamage));
        GameDebug.Log(target.Name + " @ " + target.Health);
    }

    /// <summary>
    /// Server-side call. NPC casts an MonsterAttackRight
    /// </summary>
    /// <param name="rd">the MonsterAttackRight cast</param>
    private void ServerAttackRight(VectorCastRD rd)
    {
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionForwardOnEnemies(rd.pos, kAttackRightRange, gameObject_);

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, kAttackRightDamage));
            GameDebug.Log(collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Client-side call. NPC casts a MonsterRangedAttack
    /// </summary>
    /// <param name="rd">the MonsterRangedAttack cast</param>
    private void ClientRangedAttack(TargetedCastRD rd)
    {
        UnitEntity source = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (source == null || target == null)
        {
            return;
        }
        Vector3 attack_source = new Vector3(source.TargetingTransform.position.x, source.TargetingTransform.position.y, source.TargetingTransform.position.z);
        Vector3 attack_target = new Vector3(target.TargetingTransform.position.x, target.TargetingTransform.position.y, target.TargetingTransform.position.z);
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(attack_source, attack_target, 0.01f, Color.blue));
    }

    /// <summary>
    /// Server-side call. NPC casts a MonsterRangedAttack
    /// </summary>
    /// <param name="rd">the MonsterRangedAttack cast</param>
    private void ServerRangedAttack(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, rd.target_uid, rd.type, kRangedAttackDamage));
        GameDebug.Log(target.Name + " @ " + target.Health);
    }

    /// <summary>
    /// Client-side call. NPC casts a SuicideBomb
    /// </summary>
    /// <param name="rd">the SuicideBomb cast</param>
    private void ClientSuicideBomb(CastRD rd)
    {
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new ExplosionEffect(gameObject_.transform.position, 10));
    }

    /// <summary>
    /// Server-side call. NPC casts a SuicideBomb
    /// </summary>
    /// <param name="rd">the SuicideBomb cast</param>
    private void ServerSuicideBomb(CastRD rd)
    {
        // kill unit immediately, or it is able to cast Suicide twice
        parent_.Damage(parent_.MaxHealth);

        parent_.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, parent_.Uid, rd.type, parent_.MaxHealth));

        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(gameObject_.transform.position, 10, gameObject_, casterImmune: true, npcsImmune: true);
        foreach (UnitEntity otherChar in collidedTargets)
        {
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, otherChar.Uid, rd.type, 50));
            GameDebug.Log("SuicideBomb: " + otherChar.Name + " @ " + otherChar.Health);
        }
    }

    /// <summary>
    /// Client-side call. NPC casts a StunCarry
    /// </summary>
    /// <param name="rd">the StunCarry cast</param>
    private void ClientStunCarry(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        Vector3 leashDistance = target.UnitTransform().position - parent_.UnitTransform().position;
        target.SetLeash(parent_, leashDistance);
    }

    /// <summary>
    /// Server-side call. NPC casts a StunCarry
    /// </summary>
    /// <param name="rd">the StunCarry cast</param>
    private void ServerStunCarry(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        Vector3 leashDistance = target.UnitTransform().position - parent_.UnitTransform().position;
        target.SetLeash(parent_, leashDistance);
    }

    /// <summary>
    /// Client-side call. NPC casts a DropCarried
    /// </summary>
    /// <param name="rd">the DropCarried cast</param>
    private void ClientDropCarried(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.SetLeash(null, Vector3.zero);
    }

    /// <summary>
    /// Server-side call. NPC casts a DropCarried
    /// </summary>
    /// <param name="rd">the DropCarried cast</param>
    private void ServerDropCarried(TargetedCastRD rd)
    {
        UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uid);
        if (target == null)
        {
            return;
        }
        target.SetLeash(null, Vector3.zero);
    }

    /// <summary>
    /// Client-side call. NPC casts a ThorSlam
    /// </summary>
    /// <param name="rd">the ThorSlam cast</param>
    private void ClientThorSlam(VectorCastRD rd)
    {
        // make prefab
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(
            new GenericParticleSystem(new Vector3(30.64f, 14.11f, 102.2f), Quaternion.identity, new Vector3(30, 1, 30), 1000, Globals.kLightningStrikePrefab)
        );
    }

    /// <summary>
    /// Server-side call. NPC casts a ThorSlam
    /// </summary>
    /// <param name="rd">the ThorSlam cast</param>
    private void ServerThorSlam(VectorCastRD rd)
    {
        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(gameObject_.transform.position, 10, gameObject_, casterImmune: true, npcsImmune: true);
        foreach (UnitEntity otherChar in collidedTargets)
        {
            //GameDebug.Log("ThorSlam: " + otherChar.UnitTransform().position.y + " VS " + rd.pos.y);
            if (otherChar.UnitTransform().position.y > rd.pos.y)
                continue;

            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, otherChar.Uid, rd.type, 80));
            //GameDebug.Log("ThorSlam: " + otherChar.Name + " @ " + otherChar.Health);
        }
    }

    /// <summary>
    /// Client-side call. NPC casts a ThorChainLightning
    /// </summary>
    /// <param name="rd">the ThorChainLightning cast</param>
    private void ClientThorChainLightning(MultiTargetedCastRD rd)
    {
        // TODO make proper chain lightning prefab; currently drawing lasers and making a shitty lightning prefab at each target
        // start at caster
        UnitEntity caster = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        if (caster == null)
        {
            return;
        }
        Vector3 previous_pos = caster.TargetingTransform.position;
        // iterate through all targets
        for (int targetIndex = 0; targetIndex < rd.target_uids.Length; targetIndex++)
        {
            UnitEntity target = parent_.EntityManager.FindUnitEntityByUid(rd.target_uids[targetIndex]);
            if (target == null)
            {
                return;
            }
            Vector3 target_pos = target.UnitTransform().position;
            // draw a laser between last and current target
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(previous_pos, target_pos, 0.001f, Color.white));
            // create lightning effect
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(
                new GenericParticleSystem(target_pos, Quaternion.identity, Vector3.one, 1000, Globals.kLightningStrikePrefab)
            );
            previous_pos = target_pos;
        }
    }

    /// <summary>
    /// /// Server-side call. NPC casts a ThorChainLightning
    /// </summary>
    /// <param name="rd">the ThorChainLightning cast</param>
    private void ServerThorChainLightning(MultiTargetedCastRD rd)
    {
        for (int targetIndex = 0; targetIndex < rd.target_uids.Length; targetIndex++)
        {
            UnitEntity otherChar = parent_.EntityManager.FindUnitEntityByUid(rd.target_uids[targetIndex]);
            if (otherChar == null)
            {
                continue;
            }
            // ignore magnets
            if (otherChar.Type == UnitEntityCode.kMagnet)
                continue;
            // damage is 40 for first target, 60 for second, 80 for third, etc
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, otherChar.Uid, rd.type, 40 + 20 * targetIndex));
            GameDebug.Log("ThorChainLightning: " + otherChar.Name + " @ " + otherChar.Health);
        }
    }

    /// <summary>
    /// /// Server-side call. NPC casts a LokiSplitEnd
    /// </summary>
    /// <param name="rd">the LokiSplitEnd cast</param>
    private void ServerLokiSplitEnd(VectorCastRD rd)
    {
        UnitEntity otherChar = parent_.EntityManager.FindUnitEntityByUid(rd.caster_uid);
        if (otherChar == null)
        {
            return;
        }

        otherChar.Controller.SetMotorPose(rd.pos, Vector3.zero, rd.ori);
    }
}
