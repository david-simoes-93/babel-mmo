using System.Collections.Generic;
using UnityEngine;
using static Globals;
using static Globals.CastCode;

internal class SniperCastValidator : BaseCastValidator
{
    // TODO: use this instead of local variables for each weapon
    internal class WeaponInfo
    {
        internal long timeCooldownStarted,
            timeCooldownEnded;
        internal int currAmmo;
        internal bool reloading = false;

        internal readonly WeaponConfig config;
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

    public readonly static Dictionary<CastCode, WeaponConfig> weapon_configs = new Dictionary<CastCode, WeaponConfig>
    {
        { SniperChooseWeaponRifle, new WeaponConfig(100, 50, 0, 5, 1000, 20, 6, 15, 1000, 30, 1, 10) },
        { SniperChooseWeaponShotgun, new WeaponConfig(500, 20, 0, 2, 5000, 20, 0, 2, 1500, 8, 1, 4) },
        { SniperChooseWeaponMedigun, new WeaponConfig(100, 50, 0, 3, 100, 0, 0, 2, 1000, 20, 1, 1) }
    };

    // TODO: dictionary of Castcode-> Infos

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
    private CastCode currentWeaponEquipped_ = SniperChooseWeaponRifle;
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
            case SniperWeaponRifleFire:
                return CanWeaponRifleFire(currTime_ms);
            case SniperWeaponRifleAlternate:
                return CanWeaponRifleAlternate(currTime_ms);
            case SniperWeaponShotgunFire:
                return CanWeaponShotgunFire(currTime_ms);
            case SniperWeaponShotgunAlternate:
                return CanWeaponShotgunAlternate(currTime_ms);
            case SniperWeaponMedigunFire:
                return CanWeaponMedigunFire(currTime_ms);
            case SniperWeaponMedigunAlternate:
                return CanWeaponMedigunAlternate(currTime_ms);
            case SniperReload:
                return CanReload(currTime_ms);
            case SniperChooseWeaponRifle:
            case SniperChooseWeaponShotgun:
            case SniperChooseWeaponMedigun:
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
        return !parent_.IsDead
            && currTime > timeWeaponRifleCooldownEnded_
            && currAmmoRifle_ >= weapon_configs[SniperChooseWeaponRifle].kLeftAmmoConsumed
            && currentWeaponEquipped_ == SniperChooseWeaponRifle;
    }

    /// <summary>
    /// Whether WeaponRifleAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponRifleAlternate(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponRifleCooldownEnded_
            && currAmmoRifle_ >= weapon_configs[SniperChooseWeaponRifle].kRightAmmoConsumed
            && currentWeaponEquipped_ == SniperChooseWeaponRifle;
    }

    /// <summary>
    /// Whether WeaponShotgunFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponShotgunFire(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponShotgunCooldownEnded_
            && currAmmoShotgun_ >= weapon_configs[SniperChooseWeaponShotgun].kLeftAmmoConsumed
            && currentWeaponEquipped_ == SniperChooseWeaponShotgun;
    }

    /// <summary>
    /// Whether WeaponShotgunAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponShotgunAlternate(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponShotgunCooldownEnded_
            && currAmmoShotgun_ >= weapon_configs[SniperChooseWeaponShotgun].kRightAmmoConsumed
            && currentWeaponEquipped_ == SniperChooseWeaponShotgun;
    }

    /// <summary>
    /// Whether WeaponMedigunFire is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponMedigunFire(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponMedigunCooldownEnded_
            && currAmmoMedigun_ >= weapon_configs[SniperChooseWeaponMedigun].kLeftAmmoConsumed
            && currentWeaponEquipped_ == SniperChooseWeaponMedigun;
    }

    /// <summary>
    /// Whether WeaponMedigunAlternate is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanWeaponMedigunAlternate(long currTime)
    {
        return !parent_.IsDead
            && currTime > timeWeaponMedigunCooldownEnded_
            && currAmmoMedigun_ >= weapon_configs[SniperChooseWeaponMedigun].kRightAmmoConsumed
            && currentWeaponEquipped_ == SniperChooseWeaponMedigun;
    }

    /// <summary>
    /// Whether Reload is valid
    /// </summary>
    /// <returns>true if valid</returns>
    internal bool CanReload(long currTime)
    {
        if (currentWeaponEquipped_ == SniperChooseWeaponShotgun)
        {
            return !parent_.IsDead && currTime > timeWeaponShotgunCooldownEnded_ && currAmmoShotgun_ < weapon_configs[SniperChooseWeaponShotgun].kMaxAmmo;
        }
        else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun)
        {
            return !parent_.IsDead && currTime > timeWeaponMedigunCooldownEnded_ && currAmmoMedigun_ < weapon_configs[SniperChooseWeaponMedigun].kMaxAmmo;
        }
        else
        {
            return !parent_.IsDead && currTime > timeWeaponRifleCooldownEnded_ && currAmmoRifle_ < weapon_configs[SniperChooseWeaponRifle].kMaxAmmo;
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

    internal bool HasAmmoLeft()
    {
        if (currentWeaponEquipped_ == SniperChooseWeaponShotgun)
        {
            return currAmmoShotgun_ >= weapon_configs[SniperChooseWeaponShotgun].kLeftAmmoConsumed;
        }
        else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun)
        {
            return currAmmoMedigun_ >= weapon_configs[SniperChooseWeaponMedigun].kLeftAmmoConsumed;
        }
        else
        {
            return currAmmoRifle_ >= weapon_configs[SniperChooseWeaponRifle].kLeftAmmoConsumed;
        }
    }

    internal bool HasAmmoRight()
    {
        if (currentWeaponEquipped_ == SniperChooseWeaponShotgun)
        {
            return currAmmoShotgun_ >= weapon_configs[SniperChooseWeaponShotgun].kRightAmmoConsumed;
        }
        else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun)
        {
            return currAmmoMedigun_ >= weapon_configs[SniperChooseWeaponMedigun].kRightAmmoConsumed;
        }
        else
        {
            return currAmmoRifle_ >= weapon_configs[SniperChooseWeaponRifle].kRightAmmoConsumed;
        }
    }

    internal CastCode CurrentWeapon()
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
        currAmmoRifle_ = weapon_configs[SniperChooseWeaponRifle].kMaxAmmo;
        currAmmoShotgun_ = weapon_configs[SniperChooseWeaponShotgun].kMaxAmmo;
        currAmmoMedigun_ = weapon_configs[SniperChooseWeaponMedigun].kMaxAmmo;
        rng_ = new System.Random();

        timeWeaponRifleCooldownStarted_ = currTime_ms;
        timeWeaponRifleCooldownEnded_ = currTime_ms + 1;
        timeWeaponShotgunCooldownStarted_ = currTime_ms;
        timeWeaponShotgunCooldownEnded_ = currTime_ms + 1;
        timeWeaponMedigunCooldownStarted_ = currTime_ms;
        timeWeaponMedigunCooldownEnded_ = currTime_ms + 1;

        currentWeaponEquipped_ = SniperChooseWeaponRifle;
#if !UNITY_SERVER
        // TODO: when someone else spawns, they need info about the current weapon, or they'll just see Rifle
        ClientShowWeapon(SniperChooseWeaponRifle);
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
            case SniperWeaponRifleFire:
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
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponRifleCooldownStarted_ = currTime_ms;
                timeWeaponRifleCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponRifle].kLeftCooldown;
                currAmmoRifle_ -= weapon_configs[SniperChooseWeaponRifle].kLeftAmmoConsumed;
                break;
            case SniperWeaponShotgunFire:
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
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponShotgunCooldownStarted_ = currTime_ms;
                timeWeaponShotgunCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponShotgun].kLeftCooldown;
                currAmmoShotgun_ -= weapon_configs[SniperChooseWeaponShotgun].kLeftAmmoConsumed;
                break;
            case SniperWeaponMedigunFire:
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
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackLeft);
                timeWeaponMedigunCooldownStarted_ = currTime_ms;
                timeWeaponMedigunCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponMedigun].kLeftCooldown;
                currAmmoMedigun_ -= weapon_configs[SniperChooseWeaponMedigun].kLeftAmmoConsumed;
                break;
            case SniperWeaponRifleAlternate:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackRightRecoil();
                    sniperInputManager_.SetRecoil(recoilV, recoilH);
                }
#else
                    
#endif
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackRight);
                // TODO if the attack is delayed, it will still use the original position and orientation when it was cast.
                // to fix, we need to recheck those values when the attack is actually executed
                // should add some delay, like 500ms (charging)
                delayedEvents_.Add(currTime_ms + 0, rd);

                timeWeaponRifleCooldownStarted_ = currTime_ms;
                timeWeaponRifleCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponRifle].kRightCooldown;
                currAmmoRifle_ -= weapon_configs[SniperChooseWeaponRifle].kRightAmmoConsumed;
                break;
            case SniperWeaponShotgunAlternate:
#if !UNITY_SERVER
                if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    (double recoilH, double recoilV) = GetAttackLeftRecoil();
                    sniperInputManager_.SetRecoil(recoilV * 10, recoilH * 10);
                }
                ClientAttackRightWeaponShotgun(rd as VectorCastRD);
#else
                ServerAttackRightWeaponShotgun(rd as VectorCastRD);
#endif
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackRight);
                timeWeaponShotgunCooldownStarted_ = currTime_ms;
                timeWeaponShotgunCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponShotgun].kRightCooldown;
                currAmmoShotgun_ -= weapon_configs[SniperChooseWeaponShotgun].kRightAmmoConsumed;
                break;
            case SniperWeaponMedigunAlternate:
#if !UNITY_SERVER

#else
                ServerAttackRightWeaponMedigun(rd);
#endif
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperAttackRight);
                timeWeaponMedigunCooldownStarted_ = currTime_ms;
                timeWeaponMedigunCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponMedigun].kRightCooldown;
                currAmmoMedigun_ -= weapon_configs[SniperChooseWeaponMedigun].kRightAmmoConsumed;
                break;
            case SniperReload:
                parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kSniperReload);
#if !UNITY_SERVER
                ClientHideCurrentWeapon();
#else
#endif
                if (currentWeaponEquipped_ == SniperChooseWeaponShotgun)
                {
                    reloadingShotgun_ = true;
                    timeWeaponShotgunCooldownStarted_ = currTime_ms;
                    timeWeaponShotgunCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponShotgun].kReloadLength_ms;
                    currAmmoShotgun_ = weapon_configs[SniperChooseWeaponShotgun].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponShotgunCooldownEnded_, rd);
                }
                else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun)
                {
                    reloadingMedigun_ = true;
                    timeWeaponMedigunCooldownStarted_ = currTime_ms;
                    timeWeaponMedigunCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponMedigun].kReloadLength_ms;
                    currAmmoMedigun_ = weapon_configs[SniperChooseWeaponMedigun].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponMedigunCooldownEnded_, rd);
                }
                else
                {
                    reloadingRifle_ = true;
                    timeWeaponRifleCooldownStarted_ = currTime_ms;
                    timeWeaponRifleCooldownEnded_ = currTime_ms + weapon_configs[SniperChooseWeaponRifle].kReloadLength_ms;
                    currAmmoRifle_ = weapon_configs[SniperChooseWeaponRifle].kMaxAmmo;
                    delayedEvents_.Add(timeWeaponRifleCooldownEnded_, rd);
                }
                break;
            case SniperChooseWeaponRifle:
                HalveWeaponRifleCooldown();
                if (currentWeaponEquipped_ == SniperChooseWeaponShotgun)
                {
                    DoubleWeaponShotgunCooldown();
                }
                else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun)
                {
                    DoubleWeaponMedigunCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Rifle, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = SniperChooseWeaponRifle;
                if (reloadingRifle_ && currTime_ms >= timeWeaponRifleCooldownEnded_)
                {
                    reloadingRifle_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            case SniperChooseWeaponShotgun:
                HalveWeaponShotgunCooldown();
                if (currentWeaponEquipped_ == SniperChooseWeaponRifle)
                {
                    DoubleWeaponRifleCooldown();
                }
                else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun)
                {
                    DoubleWeaponMedigunCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Shotgun, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = SniperChooseWeaponShotgun;
                if (reloadingShotgun_ && currTime_ms >= timeWeaponShotgunCooldownEnded_)
                {
                    reloadingShotgun_ = false;
                }
#if !UNITY_SERVER
                ClientWeaponEquip(rd as CastRD);
#else
#endif
                break;
            case SniperChooseWeaponMedigun:
                HalveWeaponMedigunCooldown();
                if (currentWeaponEquipped_ == SniperChooseWeaponRifle)
                {
                    DoubleWeaponRifleCooldown();
                }
                else if (currentWeaponEquipped_ == SniperChooseWeaponShotgun)
                {
                    DoubleWeaponShotgunCooldown();
                }
                else
                {
                    GameDebug.LogWarning("Choosing Medigun, but current weapon is: " + currentWeaponEquipped_);
                }
                currentWeaponEquipped_ = SniperChooseWeaponMedigun;
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
            case SniperWeaponRifleAlternate:
#if !UNITY_SERVER
                ClientAttackRightWeaponRifle(rd as VectorCastRD);
#else
                ServerAttackRightWeaponRifle(rd as VectorCastRD);
#endif
                break;
            case SniperReload:
                // delayed SniperReload acts as a ping when a reload finishes. However, because reload times can slow down
                //  when you switch to different weapons, we check the time again and re-issue another delayed SniperReload
                //  until it's actually correct
                if (currentWeaponEquipped_ == SniperChooseWeaponRifle && reloadingRifle_)
                {
                    if (currTime_ms < timeWeaponRifleCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponRifleCooldownEnded_, rd);
                        break;
                    }
                    reloadingRifle_ = false;
#if !UNITY_SERVER
                    ClientShowWeapon(SniperChooseWeaponRifle);
#else
#endif
                }
                else if (currentWeaponEquipped_ == SniperChooseWeaponShotgun && reloadingShotgun_)
                {
                    if (currTime_ms < timeWeaponShotgunCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponShotgunCooldownEnded_, rd);
                        break;
                    }
                    reloadingShotgun_ = false;
#if !UNITY_SERVER
                    ClientShowWeapon(SniperChooseWeaponShotgun);
#else
#endif
                }
                else if (currentWeaponEquipped_ == SniperChooseWeaponMedigun && reloadingMedigun_)
                {
                    if (currTime_ms < timeWeaponMedigunCooldownEnded_)
                    {
                        delayedEvents_.Add(timeWeaponMedigunCooldownEnded_, rd);
                        break;
                    }
                    reloadingMedigun_ = false;
#if !UNITY_SERVER
                    ClientShowWeapon(SniperChooseWeaponMedigun);
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
            weapon_configs[SniperChooseWeaponRifle].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[SniperChooseWeaponRifle].kLeftRange;
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
            weapon_configs[SniperChooseWeaponRifle].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(
                new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, weapon_configs[SniperChooseWeaponRifle].kLeftDamage)
            );
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
            weapon_configs[SniperChooseWeaponRifle].kRightRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[SniperChooseWeaponRifle].kRightRange;
        }

        if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
        {
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(rd.pos, collisionPoint, 0.01f, Color.red));
        }
        ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new ExplosionEffect(collisionPoint, weapon_configs[SniperChooseWeaponRifle].kRightRadius));
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
            weapon_configs[SniperChooseWeaponRifle].kRightRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[SniperChooseWeaponRifle].kRightRange;
        }

        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(
            collisionPoint,
            weapon_configs[SniperChooseWeaponRifle].kRightRadius,
            gameObject_,
            casterImmune: false
        );
        foreach (UnitEntity otherChar in collidedTargets)
        {
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, otherChar.Uid, rd.type, weapon_configs[SniperChooseWeaponRifle].kRightDamage));
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
                weapon_configs[SniperChooseWeaponShotgun].kLeftRange,
                gameObject_,
                raycastThickness: 0.01f
            );
            if (collidedObject == null)
            {
                collisionPoint = rd.pos + rd.ori * randomSpread * weapon_configs[SniperChooseWeaponShotgun].kLeftRange;
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
                weapon_configs[SniperChooseWeaponShotgun].kLeftRange,
                gameObject_,
                raycastThickness: 0.01f
            );

            if (collidedTarget != null)
            {
                collidedTarget.EntityManager.AsyncCreateTempEvent(
                    new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, weapon_configs[SniperChooseWeaponShotgun].kLeftDamage)
                );
                GameDebug.Log("WeaponShotgunFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
            }
        }
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponShotgunAlternate with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponShotgunAlternate cast</param>
    private void ClientAttackRightWeaponShotgun(VectorCastRD rd)
    {
        ClientAttackLeftWeaponShotgun(rd);
        if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
        {
            Vector3 backwards_force = parent_.CameraTransform().rotation * Vector3.forward * -20;
            parent_.Controller.AddVelocity(backwards_force);
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponShotgunAlternate with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponShotgunAlternate cast</param>
    private void ServerAttackRightWeaponShotgun(VectorCastRD rd)
    {
        ServerAttackLeftWeaponShotgun(rd);
    }

    /// <summary>
    /// Client-side call. Sniper casts an WeaponMedigunFire with WeaponMedigun
    /// </summary>
    /// <param name="rd">the WeaponMedigunFire cast</param>
    private void ClientAttackLeftWeaponMedigun(VectorCastRD rd)
    {
        (GameObject collidedObject, Vector3 collisionPoint) = CollisionChecker.CheckCollisionOnAnyObstacle(
            rd.pos,
            rd.ori * Vector3.forward,
            weapon_configs[SniperChooseWeaponMedigun].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );
        if (collidedObject == null)
        {
            collisionPoint = rd.pos + rd.ori * Vector3.forward * weapon_configs[SniperChooseWeaponMedigun].kLeftRange;
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
            weapon_configs[SniperChooseWeaponMedigun].kLeftRange,
            gameObject_,
            raycastThickness: 0.01f
        );

        if (collidedTarget != null)
        {
            collidedTarget.EntityManager.AsyncCreateTempEvent(
                new CombatEffectRD(parent_.Uid, collidedTarget.Uid, rd.type, weapon_configs[SniperChooseWeaponMedigun].kLeftDamage)
            ); // its Heal, not damage
            GameDebug.Log("WeaponMedigunFire: " + collidedTarget.Name + " @ " + collidedTarget.Health);
        }
    }

    /// <summary>
    /// Server-side call. Sniper casts an WeaponMedigunAlternate with WeaponShotgun
    /// </summary>
    /// <param name="rd">the WeaponMedigunAlternate cast</param>
    private void ServerAttackRightWeaponMedigun(CastRD rd)
    {
        parent_.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(parent_.Uid, parent_.Uid, rd.type, weapon_configs[SniperChooseWeaponMedigun].kRightDamage)); // its Heal, not damage
    }

    /// <summary>
    /// Client-side call. Sniper switches weapon
    /// </summary>
    /// <param name="rd">the WeaponMedigunFire cast</param>
    private void ClientWeaponEquip(CastRD rd)
    {
        ClientHideCurrentWeapon();

        if (
            (rd.type == SniperChooseWeaponRifle && !reloadingRifle_)
            || (rd.type == SniperChooseWeaponShotgun && !reloadingShotgun_)
            || (rd.type == SniperChooseWeaponMedigun && !reloadingMedigun_)
        )
        {
            ClientShowWeapon(rd.type);
        }
    }

    /// <summary>
    /// Client-side call. Sniper hides current weapon's LocalEffect
    /// </summary>
    internal void ClientHideCurrentWeapon()
    {
        if (curr_weapon_effect_uid_ == 0)
        {
            return;
        }
        ClientGameLoop.CGL.LocalEntityManager.Remove(curr_weapon_effect_uid_);
        curr_weapon_effect_uid_ = 0;
    }

    /// <summary>
    /// Client-side call. Sniper creates a given weapon's LocalEffect
    /// </summary>
    /// <param name="rd">the weapon</param>
    internal void ClientShowWeapon(CastCode type)
    {
        if (parent_.IsDead)
        {
            return;
        }
        curr_weapon_effect_uid_ = ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new WeaponEquip(parent_, type));
    }
}
