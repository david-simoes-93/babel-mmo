using System.Collections.Generic;
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

    private long timeWeaponRifleCooldownStarted_,
        timeWeaponRifleCooldownEnded_,
        timeWeaponShotgunCooldownStarted_,
        timeWeaponShotgunCooldownEnded_,
        timeWeaponMedigunCooldownStarted_,
        timeWeaponMedigunCooldownEnded_;
    internal int currAmmoRifle_,
        currAmmoShotgun_,
        currAmmoMedigun_;
    private bool reloadingRifle_ = false,
        reloadingShotgun_ = false,
        reloadingMedigun_ = false;
    private Weapon currentWeaponEquipped_ = Weapon.Rifle;
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
            case CastCode.SniperWeaponRifleFire:
                return CanWeaponRifleFire(currTime_ms);
            case CastCode.SniperWeaponRifleAlternate:
                return CanWeaponRifleAlternate(currTime_ms);
            case CastCode.SniperWeaponShotgunFire:
                return CanWeaponShotgunFire(currTime_ms);
            case CastCode.SniperWeaponShotgunAlternate:
                return CanWeaponShotgunAlternate(currTime_ms);
            case CastCode.SniperWeaponMedigunFire:
                return CanWeaponMedigunFire(currTime_ms);
            case CastCode.SniperWeaponMedigunAlternate:
                return CanWeaponMedigunAlternate(currTime_ms);
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
    /// Whether WeaponRifleFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponRifleFire(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponRifleCooldownEnded_ && currAmmoRifle_ > 0 && currentWeaponEquipped_ == Weapon.Rifle;
    }

    /// <summary>
    /// Whether WeaponRifleAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponRifleAlternate(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponRifleCooldownEnded_
            && currAmmoRifle_ >= weapon_configs[Weapon.Rifle].kRightAmmoConsumed
            && currentWeaponEquipped_ == Weapon.Rifle;
    }

    /// <summary>
    /// Whether WeaponShotgunFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponShotgunFire(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponShotgunCooldownEnded_ && currAmmoShotgun_ > 0 && currentWeaponEquipped_ == Weapon.Shotgun;
    }

    /// <summary>
    /// Whether WeaponShotgunAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponShotgunAlternate(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponShotgunCooldownEnded_ && currAmmoShotgun_ > 0 && currentWeaponEquipped_ == Weapon.Shotgun;
    }

    /// <summary>
    /// Whether WeaponMedigunFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponMedigunFire(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponMedigunCooldownEnded_ && currAmmoMedigun_ > 0 && currentWeaponEquipped_ == Weapon.Medigun;
    }

    /// <summary>
    /// Whether WeaponMedigunAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponMedigunAlternate(long currTime)
    {
        return !parent_.IsDead && currTime > timeWeaponMedigunCooldownEnded_ && currAmmoMedigun_ > 0 && currentWeaponEquipped_ == Weapon.Medigun;
    }

    /// <summary>
    /// Whether Reload is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanReload(long currTime)
    {
        if (currentWeaponEquipped_ == Weapon.Shotgun)
        {
            return !parent_.IsDead && currTime > timeWeaponShotgunCooldownEnded_ && currAmmoShotgun_ < weapon_configs[Weapon.Shotgun].kMaxAmmo;
        }
        else if (currentWeaponEquipped_ == Weapon.Medigun)
        {
            return !parent_.IsDead && currTime > timeWeaponMedigunCooldownEnded_ && currAmmoMedigun_ < weapon_configs[Weapon.Medigun].kMaxAmmo;
        }
        else
        {
            return !parent_.IsDead && currTime > timeWeaponRifleCooldownEnded_ && currAmmoRifle_ < weapon_configs[Weapon.Rifle].kMaxAmmo;
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
        if (currentWeaponEquipped_ == Weapon.Shotgun)
        {
            return currAmmoShotgun_ != 0;
        }
        else if (currentWeaponEquipped_ == Weapon.Medigun)
        {
            return currAmmoMedigun_ != 0;
        }
        else
        {
            return currAmmoRifle_ != 0;
        }
    }

    internal Weapon CurrentWeapon()
    {
        return currentWeaponEquipped_;
    }

    /// <summary>
    /// Calculates recoil for a WeaponRifleFire
    /// </summary>
    /// <returns>tuple(horizontal recoil, vertical recoil)</returns>
    private (double, double) GetAttackLeftRecoil()
    {
        if (controller_.CurrentCharacterState == CharacterState.SniperCrouching)
            return (rng_.NextDouble() * 0.2 - 0.1, rng_.NextDouble() * 0.25 + 0.25); // [-0.1, 0.1], [0.25, 0.5]

        return (rng_.NextDouble() * 0.4 - 0.2, rng_.NextDouble() * 0.5 + 0.5); // [-0.2, 0.2], [0.5, 1]
    }

    /// <summary>
    /// Calculates recoil for an WeaponRifleAlternate
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
        currAmmoRifle_ = weapon_configs[Weapon.Rifle].kMaxAmmo;
        currAmmoShotgun_ = weapon_configs[Weapon.Shotgun].kMaxAmmo;
        currAmmoMedigun_ = weapon_configs[Weapon.Medigun].kMaxAmmo;
        rng_ = new System.Random();

        timeWeaponRifleCooldownStarted_ = currTime_ms;
        timeWeaponRifleCooldownEnded_ = currTime_ms + 1;
        timeWeaponShotgunCooldownStarted_ = currTime_ms;
        timeWeaponShotgunCooldownEnded_ = currTime_ms + 1;
        timeWeaponMedigunCooldownStarted_ = currTime_ms;
        timeWeaponMedigunCooldownEnded_ = currTime_ms + 1;

        currentWeaponEquipped_ = Weapon.Rifle;
#if !UNITY_SERVER
        // TODO: when someone else spanws, they need info about the current weapon, or they'll just see Rifle
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
            case CastCode.SniperWeaponRifleFire:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV, recoilH);
                }
                ClientAttackLeftWeaponRifle(rd as VectorCastRD);
#else
                ServerAttackLeftWeaponRifle(rd as VectorCastRD);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponRifleCooldownStarted_ = currTime_ms;
                timeWeaponRifleCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Rifle].kLeftCooldown;
                currAmmoRifle_ -= weapon_configs[Weapon.Rifle].kLeftAmmoConsumed;
                break;
            case CastCode.SniperWeaponShotgunFire:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV * 3, recoilH * 3);
                }
                ClientAttackLeftWeaponShotgun(rd as VectorCastRD);
#else
                ServerAttackLeftWeaponShotgun(rd as VectorCastRD);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponShotgunCooldownStarted_ = currTime_ms;
                timeWeaponShotgunCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Shotgun].kLeftCooldown;
                currAmmoShotgun_ -= weapon_configs[Weapon.Shotgun].kLeftAmmoConsumed;
                break;
            case CastCode.SniperWeaponMedigunFire:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV, recoilH);
                }
                ClientAttackLeftWeaponMedigun(rd as VectorCastRD);
#else
                ServerAttackLeftWeaponMedigun(rd as VectorCastRD);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponMedigunCooldownStarted_ = currTime_ms;
                timeWeaponMedigunCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Medigun].kLeftCooldown;
                currAmmoMedigun_ -= weapon_configs[Weapon.Medigun].kLeftAmmoConsumed;
                break;
            case CastCode.SniperWeaponRifleAlternate:
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

                timeWeaponRifleCooldownStarted_ = currTime_ms;
                timeWeaponRifleCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Rifle].kRightCooldown;
                currAmmoRifle_ -= weapon_configs[Weapon.Rifle].kRightAmmoConsumed;
                break;
            case CastCode.SniperWeaponShotgunAlternate:
                // not implemented
                // TODO move sniper backwards, spend 4 ammo, do single shot worth of dmg
                break;
            case CastCode.SniperWeaponMedigunAlternate:
#if !UNITY_SERVER

#else
                ServerAttackRightWeaponMedigun(rd);
#endif
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackRight);
                timeWeaponMedigunCooldownStarted_ = currTime_ms;
                timeWeaponMedigunCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Medigun].kRightCooldown;
                currAmmoMedigun_ -= weapon_configs[Weapon.Medigun].kRightAmmoConsumed;
                break;
            case CastCode.SniperReload:
                parent_.SetAnimatorTrigger(EntityAnimationTrigger.kSniperReload);
#if !UNITY_SERVER
                HideCurrentWeapon();
#else
#endif
                if (currentWeaponEquipped_ == Weapon.Shotgun)
                {
                    reloadingShotgun_ = true;
                    timeWeaponShotgunCooldownStarted_ = currTime_ms;
                    timeWeaponShotgunCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Shotgun].kReloadLength_ms;
                    currAmmoShotgun_ = weapon_configs[Weapon.Shotgun].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponShotgunCooldownEnded_, rd);
                }
                else if (currentWeaponEquipped_ == Weapon.Medigun)
                {
                    reloadingMedigun_ = true;
                    timeWeaponMedigunCooldownStarted_ = currTime_ms;
                    timeWeaponMedigunCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Medigun].kReloadLength_ms;
                    currAmmoMedigun_ = weapon_configs[Weapon.Medigun].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponMedigunCooldownEnded_, rd);
                }
                else
                {
                    reloadingRifle_ = true;
                    timeWeaponRifleCooldownStarted_ = currTime_ms;
                    timeWeaponRifleCooldownEnded_ = currTime_ms + weapon_configs[Weapon.Rifle].kReloadLength_ms;
                    currAmmoRifle_ = weapon_configs[Weapon.Rifle].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponRifleCooldownEnded_, rd);
                }
                break;
            case CastCode.SniperChooseWeaponRifle:
                HalveWeaponRifleCooldown();
                if (currentWeaponEquipped_ == Weapon.Shotgun)
                {
                    DoubleWeaponShotgunCooldown();
                }
                else if (currentWeaponEquipped_ == Weapon.Medigun)
                {
                    DoubleWeaponMedigunCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Rifle, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = Weapon.Rifle;
                if (reloadingRifle_ && currTime_ms >= timeWeaponRifleCooldownEnded_)
                {
                    reloadingRifle_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            case CastCode.SniperChooseWeaponShotgun:
                HalveWeaponShotgunCooldown();
                if (currentWeaponEquipped_ == Weapon.Rifle)
                {
                    DoubleWeaponRifleCooldown();
                }
                else if (currentWeaponEquipped_ == Weapon.Medigun)
                {
                    DoubleWeaponMedigunCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Shotgun, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = Weapon.Shotgun;
                if (reloadingShotgun_ && currTime_ms >= timeWeaponShotgunCooldownEnded_)
                {
                    reloadingShotgun_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            case CastCode.SniperChooseWeaponMedigun:
                HalveWeaponMedigunCooldown();
                if (currentWeaponEquipped_ == Weapon.Rifle)
                {
                    DoubleWeaponRifleCooldown();
                }
                else if (currentWeaponEquipped_ == Weapon.Shotgun)
                {
                    DoubleWeaponShotgunCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Medigun, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = Weapon.Medigun;
                if (reloadingMedigun_ && currTime_ms >= timeWeaponMedigunCooldownEnded_)
                {
                    reloadingMedigun_ = false;
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
    /// Double the CD of WeaponRifle (called when reloading and switching into another weapon)
    /// </summary>
    private void DoubleWeaponRifleCooldown()
    {
        timeWeaponRifleCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponRifleCooldownStarted_) * 2;
        timeWeaponRifleCooldownEnded_ = currTime_ms + (timeWeaponRifleCooldownEnded_ - currTime_ms) * 2;
    }

    /// <summary>
    /// Double the CD of WeaponShotgun (called when reloading and switching into another weapon)
    /// </summary>
    private void DoubleWeaponShotgunCooldown()
    {
        timeWeaponShotgunCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponShotgunCooldownStarted_) * 2;
        timeWeaponShotgunCooldownEnded_ = currTime_ms + (timeWeaponShotgunCooldownEnded_ - currTime_ms) * 2;
    }

    /// <summary>
    /// Double the CD of WeaponMedigun (called when reloading and switching into another weapon)
    /// </summary>
    private void DoubleWeaponMedigunCooldown()
    {
        timeWeaponMedigunCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponMedigunCooldownStarted_) * 2;
        timeWeaponMedigunCooldownEnded_ = currTime_ms + (timeWeaponMedigunCooldownEnded_ - currTime_ms) * 2;
    }

    /// <summary>
    /// Halve the CD of WeaponRifle (called when reloading and switching into WeaponRifle)
    /// </summary>
    private void HalveWeaponRifleCooldown()
    {
        timeWeaponRifleCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponRifleCooldownStarted_) / 2;
        timeWeaponRifleCooldownEnded_ = currTime_ms + (timeWeaponRifleCooldownEnded_ - currTime_ms) / 2;
    }

    /// <summary>
    /// Halve the CD of WeaponShotgun (called when reloading and switching into WeaponShotgun)
    /// </summary>
    private void HalveWeaponShotgunCooldown()
    {
        timeWeaponShotgunCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponShotgunCooldownStarted_) / 2;
        timeWeaponShotgunCooldownEnded_ = currTime_ms + (timeWeaponShotgunCooldownEnded_ - currTime_ms) / 2;
    }

    /// <summary>
    /// Halve the CD of WeaponMedigun (called when reloading and switching into WeaponMedigun)
    /// </summary>
    private void HalveWeaponMedigunCooldown()
    {
        timeWeaponMedigunCooldownStarted_ = currTime_ms - (currTime_ms - timeWeaponMedigunCooldownStarted_) / 2;
        timeWeaponMedigunCooldownEnded_ = currTime_ms + (timeWeaponMedigunCooldownEnded_ - currTime_ms) / 2;
    }

    /// <summary>
    /// Return cooldown fraction of WeaponRifle
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownWeaponRifle()
    {
        if (currTime_ms >= timeWeaponRifleCooldownEnded_)
            return 0;

        return (timeWeaponRifleCooldownEnded_ - currTime_ms) * 1.0f / (timeWeaponRifleCooldownEnded_ - timeWeaponRifleCooldownStarted_);
    }

    /// <summary>
    /// Return cooldown fraction of WeaponShotgun
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownWeaponShotgun()
    {
        if (currTime_ms >= timeWeaponShotgunCooldownEnded_)
            return 0;
        return (timeWeaponShotgunCooldownEnded_ - currTime_ms) * 1.0f / (timeWeaponShotgunCooldownEnded_ - timeWeaponShotgunCooldownStarted_);
    }

    /// <summary>
    /// Return cooldown fraction of WeaponMedigun
    /// </summary>
    /// <returns>[0,1] meaning [not on CD, full CD]</returns>
    internal float CooldownWeaponMedigun()
    {
        if (currTime_ms >= timeWeaponMedigunCooldownEnded_)
            return 0;
        return (timeWeaponMedigunCooldownEnded_ - currTime_ms) * 1.0f / (timeWeaponMedigunCooldownEnded_ - timeWeaponMedigunCooldownStarted_);
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
            case CastCode.SniperWeaponRifleAlternate:
#if !UNITY_SERVER
                ClientAttackRightWeaponRifle(rd as VectorCastRD);
#else
                ServerAttackRightWeaponRifle(rd as VectorCastRD);
#endif
                break;
            case CastCode.SniperReload:
                // delayed SniperReload acts as a ping when a reload finishes. However, because reload times can slow down
                //  when you switch to different weapons, we check the time again and re-issue another delayed SniperReload
                //  until it's actually correct
                if (currentWeaponEquipped_ == Weapon.Rifle && reloadingRifle_)
                {
                    if (currTime_ms < timeWeaponRifleCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponRifleCooldownEnded_, rd);
                        break;
                    }
                    reloadingRifle_ = false;
#if !UNITY_SERVER
                    ShowWeapon(CastCode.SniperChooseWeaponRifle);
#else
#endif
                }
                else if (currentWeaponEquipped_ == Weapon.Shotgun && reloadingShotgun_)
                {
                    if (currTime_ms < timeWeaponShotgunCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponShotgunCooldownEnded_, rd);
                        break;
                    }
                    reloadingShotgun_ = false;
#if !UNITY_SERVER
                    ShowWeapon(CastCode.SniperChooseWeaponShotgun);
#else
#endif
                }
                else if (currentWeaponEquipped_ == Weapon.Medigun && reloadingMedigun_)
                {
                    if (currTime_ms < timeWeaponMedigunCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponMedigunCooldownEnded_, rd);
                        break;
                    }
                    reloadingMedigun_ = false;
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
    /// Client-side call. Sniper casts an WeaponRifleFire with WeaponRifle
    /// </summary>
    /// <param name="rd">the WeaponRifleFire cast</param>
    private void ClientAttackLeftWeaponRifle(VectorCastRD rd)
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
    /// Server-side call. Sniper casts an WeaponRifleFire with WeaponRifle
    /// </summary>
    /// <param name="rd">the WeaponRifleFire cast</param>
    private void ServerAttackLeftWeaponRifle(VectorCastRD rd)
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
            GameDebug.Log("WeaponRifleFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponRifleAlternate with WeaponRifle
    /// </summary>
    /// <param name="rd">the WeaponRifleAlternate cast</param>
    private void ClientAttackRightWeaponRifle(VectorCastRD rd)
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
    /// Server-side call. Sniper casts an WeaponRifleAlternate with WeaponRifle
    /// </summary>
    /// <param name="rd">the WeaponRifleAlternate cast</param>
    private void ServerAttackRightWeaponRifle(VectorCastRD rd)
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
            GameDebug.Log("WeaponRifleAlternate: " + otherChar.Name + " @ " + otherChar.Health);
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponShotgunFire with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponShotgunFire cast</param>
    private void ClientAttackLeftWeaponShotgun(VectorCastRD rd)
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
    /// Server-side call. Sniper casts an WeaponShotgunFire with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponShotgunFire cast</param>
    private void ServerAttackLeftWeaponShotgun(VectorCastRD rd)
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
                GameDebug.Log("WeaponShotgunFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
            }
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponShotgunAlternate with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponShotgunAlternate cast</param>
    private void ClientAttackRightWeaponShotgun(VectorCastRD rd) { }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponShotgunAlternate with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponShotgunAlternate cast</param>
    private void ServerAttackRightWeaponShotgun(VectorCastRD rd) { }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponMedigunFire with WeaponMedigun
    /// </summary>
    /// <param name="rd">the WeaponMedigunFire cast</param>
    private void ClientAttackLeftWeaponMedigun(VectorCastRD rd)
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
    /// Server-side call. Sniper casts an WeaponMedigunFire with WeaponMedigun
    /// </summary>
    /// <param name="rd">the WeaponMedigunFire cast</param>
    private void ServerAttackLeftWeaponMedigun(VectorCastRD rd)
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
            GameDebug.Log("WeaponMedigunFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponMedigunAlternate with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponMedigunAlternate cast</param>
    private void ServerAttackRightWeaponMedigun(CastRD rd)
    {
        parent_.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, parent_.Uid, rd.type, weapon_configs[Weapon.Medigun].kRightDamage)); // its Heal, not damage
    }

    /// <summary>
    /// Client-side call. Sniper switches weapon
    /// </summary>
    /// <param name="rd">the WeaponMedigunFire cast</param>
    private void ClientWeaponEquip(CastRD rd)
    {
        HideCurrentWeapon();

        if (
            (rd.type == Globals.CastCode.SniperChooseWeaponRifle && !reloadingRifle_)
            || (rd.type == Globals.CastCode.SniperChooseWeaponShotgun && !reloadingShotgun_)
            || (rd.type == Globals.CastCode.SniperChooseWeaponMedigun && !reloadingMedigun_)
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
