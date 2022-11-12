﻿using System.Collections.Generic;
using UnityEngine;
using static Globals;

internal class SniperCastValidator : BaseCastValidator
{
    // TODO: use this instead of local variables for each weapon
    internal class WeaponInfo
    {
        internal long timeCooldownStarted,
            timeCooldownEnded;
        internal int currAmmo;
        internal bool reloading = false;
    }

    internal class WeaponConfig
    {
        internal WeaponConfig(int lCd, int lRn, int lRd, int lD, int rCd, int rRn, int rRd, int rD, int reload_ms, int max_ammo, int lA, int rA)
        {
            kLeftCooldown = lCd;
            kLeftRange = lRn;
            kLeftRadius = lRd;
            kLeftDamage = lD;

            kRightCooldown = rCd;
            kRightRange = rRn;
            kRightRadius = rRd;
            kRightDamage = rD;

            kReloadLength_ms = reload_ms;
            kMaxAmmo = max_ammo;
            kLeftAmmoConsumed = lA;
            kRightAmmoConsumed = rA;
        }

        internal readonly int kLeftCooldown,
            kLeftRange,
            kLeftRadius,
            kLeftDamage;
        internal readonly int kRightCooldown,
            kRightRange,
            kRightRadius,
            kRightDamage;
        internal readonly int kReloadLength_ms;
        internal readonly int kMaxAmmo,
            kLeftAmmoConsumed,
            kRightAmmoConsumed;
    }

    // TODO: eventually move this out of CastValidator and into some Sniper global file
    public enum Weapon
    {
        Rifle,
        Shotgun,
        Medigun
    };

    public readonly static Dictionary<Weapon, WeaponConfig> weapon_configs = new Dictionary<Weapon, WeaponConfig>
    {
        { Weapon.Rifle, new WeaponConfig(100, 50, 0, 5, 1000, 20, 6, 15, 1000, 30, 1, 10) },
        { Weapon.Shotgun, new WeaponConfig(500, 20, 0, 2, 5000, 20, 0, 2, 1500, 8, 1, 4) },
        { Weapon.Medigun, new WeaponConfig(100, 50, 0, 3, 100, 0, 0, 2, 1000, 20, 1, 1) }
    };

    // dictionary of Castcode-> Infos and Configs

    private long timeWeaponOneCooldownStarted_,
        timeWeaponOneCooldownEnded_,
        timeWeaponTwoCooldownStarted_,
        timeWeaponTwoCooldownEnded_,
        timeWeaponThreeCooldownStarted_,
        timeWeaponThreeCooldownEnded_;
    internal int currAmmoOne_,
        currAmmoTwo_,
        currAmmoThree_;
    private bool reloadingOne_ = false,
        reloadingTwo_ = false,
        reloadingThree_ = false;
    private int currentWeaponEquipped_ = 0;
    private int curr_weapon_effect_uid_ = 0;

    private SniperCanvas sniperGui_;
    private System.Random rng_;
    private SniperInputManager sniperInputManager_;

    /// <summary>
    /// Called by Validate() to determine validity of a class-specific CastRD (ability not in cooldown, valid targets, etc)
    /// </summary>
    /// <param name="rd"></param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificValidateCast(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.SniperWeaponOneFire:
                return CanWeaponOneFire(currTime_ms);
            case CastCode.SniperWeaponOneAlternate:
                return CanWeaponOneAlternate(currTime_ms);
            case CastCode.SniperWeaponTwoFire:
                return CanWeaponTwoFire(currTime_ms);
            case CastCode.SniperWeaponTwoAlternate:
                return CanWeaponTwoAlternate(currTime_ms);
            case CastCode.SniperWeaponThreeFire:
                return CanWeaponThreeFire(currTime_ms);
            case CastCode.SniperWeaponThreeAlternate:
                return CanWeaponThreeAlternate(currTime_ms);
            case CastCode.SniperReload:
                return CanReload(currTime_ms);
            case CastCode.SniperChooseWeaponRifle:
            case CastCode.SniperChooseWeaponShotgun:
            case CastCode.SniperChooseWeaponMedigun:
                return CanWeaponScroll(currTime_ms);
            default:
                return false;
        }
    }

    /// <summary>
    /// Whether WeaponOneFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponOneFire(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponOneCooldownEnded_ && currAmmoOne_ > 0 && currentWeaponEquipped_ == 0;
    }

    /// <summary>
    /// Whether WeaponOneAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponOneAlternate(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponOneCooldownEnded_
            && currAmmoOne_ >= weapon_configs[Weapon.Rifle].kRightAmmoConsumed
            && currentWeaponEquipped_ == 0;
    }

    /// <summary>
    /// Whether WeaponTwoFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponTwoFire(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponTwoCooldownEnded_ && currAmmoTwo_ > 0 && currentWeaponEquipped_ == 1;
    }

    /// <summary>
    /// Whether WeaponTwoAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponTwoAlternate(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponTwoCooldownEnded_ && currAmmoTwo_ > 0 && currentWeaponEquipped_ == 1;
    }

    /// <summary>
    /// Whether WeaponThreeFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponThreeFire(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponThreeCooldownEnded_ && currAmmoThree_ > 0 && currentWeaponEquipped_ == 2;
    }

    /// <summary>
    /// Whether WeaponThreeAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponThreeAlternate(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponThreeCooldownEnded_ && currAmmoThree_ > 0 && currentWeaponEquipped_ == 2;
    }

    /// <summary>
    /// Whether Reload is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanReload(long currTime)
    {
        if (currentWeaponEquipped_ == 1)
        {
            return !parent_.IsDead && currTime > timeWeaponTwoCooldownEnded_ && currAmmoTwo_ < weapon_configs[Weapon.Shotgun].kMaxAmmo;
        }
        else if (currentWeaponEquipped_ == 2)
        {
            return !parent_.IsDead && currTime > timeWeaponThreeCooldownEnded_ && currAmmoThree_ < weapon_configs[Weapon.Medigun].kMaxAmmo;
        }
        else
        {
            return !parent_.IsDead && currTime > timeWeaponOneCooldownEnded_ && currAmmoOne_ < weapon_configs[Weapon.Rifle].kMaxAmmo;
        }
    }

    /// <summary>
    /// Whether Weapon Scrolling is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponScroll(long currTime)
    {
        return !parent_.IsDead;
    }

    internal bool HasAmmo()
    {
        if (currentWeaponEquipped_ == 1)
        {
            return currAmmoTwo_ != 0;
        }
        else if (currentWeaponEquipped_ == 2)
        {
            return currAmmoThree_ != 0;
        }
        else
        {
            return currAmmoOne_ != 0;
        }
    }

    internal int CurrentWeapon()
    {
        return currentWeaponEquipped_;
    }

    /// <summary>
    /// Calculates recoil for a WeaponOneFire
    /// </summary>
    /// <returns>tuple(horizontal recoil, vertical recoil)</returns>
    private (double, double) GetAttackLeftRecoil()
    {
        if (controller_.CurrentCharacterState == CharacterState.SniperCrouching)
            return (rng_.NextDouble() * 0.2 - 0.1, rng_.NextDouble() * 0.25 + 0.25); // [-0.1, 0.1], [0.25, 0.5]

        return (rng_.NextDouble() * 0.4 - 0.2, rng_.NextDouble() * 0.5 + 0.5); // [-0.2, 0.2], [0.5, 1]
    }

    /// <summary>
    /// Calculates recoil for an WeaponOneAlternate
    /// </summary>
    /// <returns>tuple(horizontal recoil, vertical recoil)</returns>
    private (double, double) GetAttackRightRecoil()
    {
        if (controller_.CurrentCharacterState == CharacterState.SniperCrouching)
            return (rng_.NextDouble() * 0.2 + 0.1, rng_.NextDouble() * 0.4 + 0.6); // [-0.1, 0.1], [0.6, 1]

        return (rng_.NextDouble() * 0.4 - 0.2, rng_.NextDouble() * 0.8 + 1.2); // [-0.2, 0.2], [1.2, 2]
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        sniperGui_ = (SniperCanvas)parent.Canvas;
        sniperInputManager_ = (SniperInputManager)parent.InputManager;
        currAmmoOne_ = weapon_configs[Weapon.Rifle].kMaxAmmo;
        currAmmoTwo_ = weapon_configs[Weapon.Shotgun].kMaxAmmo;
        currAmmoThree_ = weapon_configs[Weapon.Medigun].kMaxAmmo;
        rng_ = new System.Random();

        timeWeaponOneCooldownStarted_ = currTime_ms;
        timeWeaponOneCooldownEnded_ = currTime_ms + 1;
        timeWeaponTwoCooldownStarted_ = currTime_ms;
        timeWeaponTwoCooldownEnded_ = currTime_ms + 1;
        timeWeaponThreeCooldownStarted_ = currTime_ms;
        timeWeaponThreeCooldownEnded_ = currTime_ms + 1;

        currentWeaponEquipped_ = 0;
#if !UNITY_SERVER
        // TODO: when someone else spanws, they need info about the current weapon, or they'll just see w1
        ShowWeapon(CastCode.SniperChooseWeaponRifle);
#else
#endif
    }

    /// <summary>
    /// Called by ProcessServerAcks() to process class-specific CastRDs received from the server
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal override bool SpecificProcessAck(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.SniperWeaponOneFire:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV, recoilH);
                }
                ClientAttackLeftWeaponOne(rd as VectorCastRD);
#else
                ServerAttackLeftWeaponOne(rd as VectorCastRD);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponOneCooldownStarted_ = currTime_ms;
                timeWeaponOneCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Rifle].kLeftCooldown;
                currAmmoOne_ -= weapon_configs[Weapon.Rifle].kLeftAmmoConsumed;
                break;
            case CastCode.SniperWeaponTwoFire:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV * 3, recoilH * 3);
                }
                ClientAttackLeftWeaponTwo(rd as VectorCastRD);
#else
                ServerAttackLeftWeaponTwo(rd as VectorCastRD);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponTwoCooldownStarted_ = currTime_ms;
                timeWeaponTwoCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Shotgun].kLeftCooldown;
                currAmmoTwo_ -= weapon_configs[Weapon.Shotgun].kLeftAmmoConsumed;
                break;
            case CastCode.SniperWeaponThreeFire:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV, recoilH);
                }
                ClientAttackLeftWeaponThree(rd as VectorCastRD);
#else
                ServerAttackLeftWeaponThree(rd as VectorCastRD);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponThreeCooldownStarted_ = currTime_ms;
                timeWeaponThreeCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Medigun].kLeftCooldown;
                currAmmoThree_ -= weapon_configs[Weapon.Medigun].kLeftAmmoConsumed;
                break;
            case CastCode.SniperWeaponOneAlternate:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackRightRecoil();
                    sniperInputManager_.SetRecoil(recoilV, recoilH);
                }
#else
                    
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackRight);
                // TODO if the attack is delayed, it will still use the original position and orientation when it was cast.
                // to fix, we need to recheck those values when the attack is actually executed
                // should add some delay, like 500ms (charging)
                delayedEvents_.Add(currTime_ms + 0, rd);

                timeWeaponOneCooldownStarted_ = currTime_ms;
                timeWeaponOneCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Rifle].kRightCooldown;
                currAmmoOne_ -= weapon_configs[Weapon.Rifle].kRightAmmoConsumed;
                break;
            case CastCode.SniperWeaponTwoAlternate:
                // not implemented
                // TODO move sniper backwards, spend 4 ammo, do single shot worth of dmg
                break;
            case CastCode.SniperWeaponThreeAlternate:
#if !UNITY_SERVER

#else
                ServerAttackRightWeaponThree(rd);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackRight);
                timeWeaponThreeCooldownStarted_ = currTime_ms;
                timeWeaponThreeCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Medigun].kRightCooldown;
                currAmmoThree_ -= weapon_configs[Weapon.Medigun].kRightAmmoConsumed;
                break;
            case CastCode.SniperReload:
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperReload);
#if !UNITY_SERVER
                HideCurrentWeapon();
#else
#endif
                if (currentWeaponEquipped_ == 1)
                {
                    reloadingTwo_ = true;
                    timeWeaponTwoCooldownStarted_ = currTime_ms;
                    timeWeaponTwoCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Shotgun].kReloadLength_ms;
                    currAmmoTwo_ = weapon_configs[Weapon.Shotgun].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponTwoCooldownEnded_, rd);
                }
                else if (currentWeaponEquipped_ == 2)
                {
                    reloadingThree_ = true;
                    timeWeaponThreeCooldownStarted_ = currTime_ms;
                    timeWeaponThreeCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Medigun].kReloadLength_ms;
                    currAmmoThree_ = weapon_configs[Weapon.Medigun].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponThreeCooldownEnded_, rd);
                }
                else
                {
                    reloadingOne_ = true;
                    timeWeaponOneCooldownStarted_ = currTime_ms;
                    timeWeaponOneCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Rifle].kReloadLength_ms;
                    currAmmoOne_ = weapon_configs[Weapon.Rifle].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponOneCooldownEnded_, rd);
                }
                break;
            case CastCode.SniperChooseWeaponRifle:
                HalveWeaponOneCooldown();
                if (currentWeaponEquipped_ == 1)
                {
                    DoubleWeaponTwoCooldown();
                }
                else if (currentWeaponEquipped_ == 2)
                {
                    DoubleWeaponThreeCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Rifle, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = 0;
                if (reloadingOne_ && currTime_ms >= timeWeaponOneCooldownEnded_)
                {
                    reloadingOne_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            case CastCode.SniperChooseWeaponShotgun:
                HalveWeaponTwoCooldown();
                if (currentWeaponEquipped_ == 0)
                {
                    DoubleWeaponOneCooldown();
                }
                else if (currentWeaponEquipped_ == 2)
                {
                    DoubleWeaponThreeCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Shotgun, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = 1;
                if (reloadingTwo_ && currTime_ms >= timeWeaponTwoCooldownEnded_)
                {
                    reloadingTwo_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            case CastCode.SniperChooseWeaponMedigun:
                HalveWeaponThreeCooldown();
                if (currentWeaponEquipped_ == 0)
                {
                    DoubleWeaponOneCooldown();
                }
                else if (currentWeaponEquipped_ == 1)
                {
                    DoubleWeaponTwoCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Medigun, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = 2;
                if (reloadingThree_ && currTime_ms >= timeWeaponThreeCooldownEnded_)
                {
                    reloadingThree_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            default:
                GameDebug.Log("Unknown cast: " + rd.type);
                return false;
        }

        return true;
    }

    /// <summary>
    /// Double the CD of WeaponOne (called when reloading and switching into another weapon)
    /// </summary>
    private void DoubleWeaponOneCooldown()
    {
        timeWeaponOneCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponOneCooldownStarted_) * 2;
        timeWeaponOneCooldownEnded_ = currTime_ms + (timeWeaponOneCooldownEnded_ - currTime_ms) * 2;
    }

    /// <summary>
    /// Double the CD of WeaponTwo (called when reloading and switching into another weapon)
    /// </summary>
    private void DoubleWeaponTwoCooldown()
    {
        timeWeaponTwoCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponTwoCooldownStarted_) * 2;
        timeWeaponTwoCooldownEnded_ = currTime_ms + (timeWeaponTwoCooldownEnded_ - currTime_ms) * 2;
    }

    /// <summary>
    /// Double the CD of WeaponThree (called when reloading and switching into another weapon)
    /// </summary>
    private void DoubleWeaponThreeCooldown()
    {
        timeWeaponThreeCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponThreeCooldownStarted_) * 2;
        timeWeaponThreeCooldownEnded_ = currTime_ms + (timeWeaponThreeCooldownEnded_ - currTime_ms) * 2;
    }

    /// <summary>
    /// Halve the CD of WeaponOne (called when reloading and switching into WeaponOne)
    /// </summary>
    private void HalveWeaponOneCooldown()
    {
        timeWeaponOneCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponOneCooldownStarted_) / 2;
        timeWeaponOneCooldownEnded_ = currTime_ms + (timeWeaponOneCooldownEnded_ - currTime_ms) / 2;
    }

    /// <summary>
    /// Halve the CD of WeaponTwo (called when reloading and switching into WeaponTwo)
    /// </summary>
    private void HalveWeaponTwoCooldown()
    {
        timeWeaponTwoCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponTwoCooldownStarted_) / 2;
        timeWeaponTwoCooldownEnded_ = currTime_ms + (timeWeaponTwoCooldownEnded_ - currTime_ms) / 2;
    }

    /// <summary>
    /// Halve the CD of WeaponThree (called when reloading and switching into WeaponThree)
    /// </summary>
    private void HalveWeaponThreeCooldown()
    {
        timeWeaponThreeCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponThreeCooldownStarted_) / 2;
        timeWeaponThreeCooldownEnded_ = currTime_ms + (timeWeaponThreeCooldownEnded_ - currTime_ms) / 2;
    }

    /// <summary>
    /// Return cooldown fraction of WeaponOne
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownWeaponOne()
    {
        if (currTime_ms >= timeWeaponOneCooldownEnded_)
            return 0;

        return (timeWeaponOneCooldownEnded_ - currTime_ms) * 1.0f / (timeWeaponOneCooldownEnded_ - timeWeaponOneCooldownStarted_);
    }

    /// <summary>
    /// Return cooldown fraction of WeaponTwo
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownWeaponTwo()
    {
        if (currTime_ms >= timeWeaponTwoCooldownEnded_)
            return 0;
        return (timeWeaponTwoCooldownEnded_ - currTime_ms) * 1.0f / (timeWeaponTwoCooldownEnded_ - timeWeaponTwoCooldownStarted_);
    }

    /// <summary>
    /// Return cooldown fraction of WeaponThree
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownWeaponThree()
    {
        if (currTime_ms >= timeWeaponThreeCooldownEnded_)
            return 0;
        return (timeWeaponThreeCooldownEnded_ - currTime_ms) * 1.0f / (timeWeaponThreeCooldownEnded_ - timeWeaponThreeCooldownStarted_);
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
            case CastCode.SniperWeaponOneAlternate:
#if !UNITY_SERVER
                ClientAttackRightWeaponOne(rd as VectorCastRD);
#else
                ServerAttackRightWeaponOne(rd as VectorCastRD);
#endif
                break;
            case CastCode.SniperReload:
                // delayed SniperReload acts as a ping when a reload finishes. However, because reload times can slow down
                //  when you switch to different weapons, we check the time again and re-issue another delayed SniperReload
                //  until it's actually correct
                if (currentWeaponEquipped_ == 0 && reloadingOne_)
                {
                    if (currTime_ms < timeWeaponOneCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponOneCooldownEnded_, rd);
                        break;
                    }
                    reloadingOne_ = false;
#if !UNITY_SERVER
                    ShowWeapon(CastCode.SniperChooseWeaponRifle);
#else
#endif
                }
                else if (currentWeaponEquipped_ == 1 && reloadingTwo_)
                {
                    if (currTime_ms < timeWeaponTwoCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponTwoCooldownEnded_, rd);
                        break;
                    }
                    reloadingTwo_ = false;
#if !UNITY_SERVER
                    ShowWeapon(CastCode.SniperChooseWeaponShotgun);
#else
#endif
                }
                else if (currentWeaponEquipped_ == 2 && reloadingThree_)
                {
                    if (currTime_ms < timeWeaponThreeCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponThreeCooldownEnded_, rd);
                        break;
                    }
                    reloadingThree_ = false;
#if !UNITY_SERVER
                    ShowWeapon(CastCode.SniperChooseWeaponMedigun);
#else
#endif
                }
                break;
            default:
                GameDebug.LogWarning("Unknown cast: " + rd.type);
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
    /// Client-side call. Sniper casts an WeaponOneFire with WeaponOne
    /// </summary>
    /// <param name="rd">the WeaponOneFire cast</param>
    private void ClientAttackLeftWeaponOne(VectorCastRD rd)
    {
        (GameObject collidedObject, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnAnyObstacle(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[Weapon.Rifle].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[Weapon.Rifle].kLeftRange;
        }

        if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(rd.pos, collisionPoint, 0.01f, Color.yellow));
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponOneFire with WeaponOne
    /// </summary>
    /// <param name="rd">the WeaponOneFire cast</param>
    private void ServerAttackLeftWeaponOne(VectorCastRD rd)
    {
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnEnemies(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[Weapon.Rifle].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, weapon_configs[Weapon.Rifle].kLeftDamage));
            GameDebug.Log("WeaponOneFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponOneAlternate with WeaponOne
    /// </summary>
    /// <param name="rd">the WeaponOneAlternate cast</param>
    private void ClientAttackRightWeaponOne(VectorCastRD rd)
    {
        (GameObject collidedObject, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnAnyObstacle(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[Weapon.Rifle].kRightRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[Weapon.Rifle].kRightRange;
        }

        if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(rd.pos, collisionPoint, 0.01f, Color.red));
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new ExplosionEffect(collisionPoint, weapon_configs[Weapon.Rifle].kRightRadius));
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponOneAlternate with WeaponOne
    /// </summary>
    /// <param name="rd">the WeaponOneAlternate cast</param>
    private void ServerAttackRightWeaponOne(VectorCastRD rd)
    {
        (GameObject collidedObject, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnAnyObstacle(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[Weapon.Rifle].kRightRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[Weapon.Rifle].kRightRange;
        }

        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(
            collisionPoint,
            weapon_configs[Weapon.Rifle].kRightRadius,
            gameObject_,
            casterImmune: false
        );
        foreach (UnitEntity otherChar in collidedTargets)
        {
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, otherChar.Uid, rd.type, weapon_configs[Weapon.Rifle].kRightDamage));
            GameDebug.Log("WeaponOneAlternate: " + otherChar.Name + " @ " + otherChar.Health);
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponTwoFire with WeaponTwo
    /// </summary>
    /// <param name="rd">the WeaponTwoFire cast</param>
    private void ClientAttackLeftWeaponTwo(VectorCastRD rd)
    {
        // buckshot
        for (int i = 0; i < 12; i++)
        {
            Vector3 randomSpread = new Vector3((float)(rng_.NextDouble() * 0.4 - 0.2), (float)(rng_.NextDouble() * 0.4 - 0.2), 1).normalized;
            (GameObject collidedObject, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnAnyObstacle(
                rd.pos,
                rd.ori * randomSpread,
                weapon_configs[Weapon.Shotgun].kLeftRange,
                gameObject_,
                raycastThickness: 0.01f
            );
            if (collidedObject == null)
            {
                collisionPoint = rd.pos + rd.ori * randomSpread * weapon_configs[Weapon.Shotgun].kLeftRange;
            }

            if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
            {
                ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(rd.pos, collisionPoint, 0.01f, Color.red));
            }
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponTwoFire with WeaponTwo
    /// </summary>
    /// <param name="rd">the WeaponTwoFire cast</param>
    private void ServerAttackLeftWeaponTwo(VectorCastRD rd)
    {
        // buckshot
        for (int i = 0; i < 12; i++)
        {
            Vector3 randomSpread = new Vector3((float)(rng_.NextDouble() * 0.4 - 0.2), (float)(rng_.NextDouble() * 0.4 - 0.2), 1).normalized;
            (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnEnemies(
                rd.pos,
                rd.ori * randomSpread,
                weapon_configs[Weapon.Shotgun].kLeftRange,
                gameObject_,
                raycastThickness: 0.01f
            );

            if (collidedTarget != null)
            {
                collidedTarget.EntityManager.AsyncCreateTempEvent(
                    new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, weapon_configs[Weapon.Shotgun].kLeftDamage)
                );
                GameDebug.Log("WeaponTwoFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
            }
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponTwoAlternate with WeaponTwo
    /// </summary>
    /// <param name="rd">the WeaponTwoAlternate cast</param>
    private void ClientAttackRightWeaponTwo(VectorCastRD rd) { }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponTwoAlternate with WeaponTwo
    /// </summary>
    /// <param name="rd">the WeaponTwoAlternate cast</param>
    private void ServerAttackRightWeaponTwo(VectorCastRD rd) { }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponThreeFire with WeaponThree
    /// </summary>
    /// <param name="rd">the WeaponThreeFire cast</param>
    private void ClientAttackLeftWeaponThree(VectorCastRD rd)
    {
        (GameObject collidedObject, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnAnyObstacle(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[Weapon.Medigun].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[Weapon.Medigun].kLeftRange;
        }

        if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(rd.pos, collisionPoint, 0.01f, Color.green));
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponThreeFire with WeaponThree
    /// </summary>
    /// <param name="rd">the WeaponThreeFire cast</param>
    private void ServerAttackLeftWeaponThree(VectorCastRD rd)
    {
        (UnitEntity collidedTarget, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnEnemies(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[Weapon.Medigun].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, weapon_configs[Weapon.Medigun].kLeftDamage)); // its Heal, not damage
            GameDebug.Log("WeaponThreeFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponThreeAlternate with WeaponTwo
    /// </summary>
    /// <param name="rd">the WeaponThreeAlternate cast</param>
    private void ServerAttackRightWeaponThree(CastRD rd)
    {
        parent_.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, parent_.Uid, rd.type, weapon_configs[Weapon.Medigun].kRightDamage)); // its Heal, not damage
    }

    /// <summary>
    /// Client-side call. Sniper switches weapon
    /// </summary>
    /// <param name="rd">the WeaponThreeFire cast</param>
    private void ClientWeaponEquip(CastRD rd)
    {
        HideCurrentWeapon();

        if (
            (rd.type == Globals.CastCode.SniperChooseWeaponRifle && !reloadingOne_)
            || (rd.type == Globals.CastCode.SniperChooseWeaponShotgun && !reloadingTwo_)
            || (rd.type == Globals.CastCode.SniperChooseWeaponMedigun && !reloadingThree_)
        )
        {
            ShowWeapon(rd.type);
        }
    }

    private void HideCurrentWeapon()
    {
        if (curr_weapon_effect_uid_ == 0)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.Remove(curr_weapon_effect_uid_);
        curr_weapon_effect_uid_ = 0;
    }

    private void ShowWeapon(CastCode type)
    {
        curr_weapon_effect_uid_ = ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new WeaponEquip(parent_, type));
    }
}
