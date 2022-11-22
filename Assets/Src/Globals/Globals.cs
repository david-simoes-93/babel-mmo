using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class Globals
{
    internal enum CastCode
    {
        None,
        FighterCharge,
        FighterChargeStun,
        DodgeLeft,
        DodgeRight,
        DodgeFront,
        DodgeBack,
        FighterAttackLeft,
        FighterAttackRight,
        Spin,
        SniperWeaponRifleFire,
        SniperWeaponRifleAlternate,
        SniperWeaponShotgunFire,
        SniperWeaponShotgunAlternate,
        SniperWeaponMedigunFire,
        SniperWeaponMedigunAlternate,
        SniperReload,
        SniperChooseWeaponRifle,
        SniperChooseWeaponShotgun,
        SniperChooseWeaponMedigun,
        MageFireflash,
        MageFireflashTick,
        MageFrostflash,
        MageArcaneflash,
        MagePyroblast,
        MagePyroblastEnd,
        MageCastStop,
        MageRenew,
        MageRenewEnd,
        Regen,
        Heal,
        Respawn,
        FallDamage,
        OutOfBoundsTeleport,
        MonsterAttackLeft,
        MonsterAttackRight,
        MonsterRangedAttack,
        DryadSuicideBomb,
        ValkyrieStunCarry,
        ValkyrieDropCarried,
        FreyjaPoisonZone,
        FreyjaPoisonZoneTick,
        ThorSlam,
        ThorThunderCloudTick,
        ThorChainLightning,
        LokiDeathWaveTick,
        LokiSplitStart,
        LokiSplitEnd
    };

    // TODO move to hashmap
    internal static bool IsHealingSpell(CastCode val)
    {
        // TODO use a dictionary instead of if conditions to map from rd.type into bool
        return val == CastCode.Heal
            || val == CastCode.Regen
            || val == CastCode.SniperWeaponMedigunFire
            || val == CastCode.SniperWeaponMedigunAlternate
            || val == CastCode.MageRenew
            || val == CastCode.MageRenewEnd;
    }

    internal static long currTime_ms;

    // Misc
    internal static GameObject kEmptyPrefab = Resources.Load("Prefabs/EmptyGameObject") as GameObject;

    // Fighter Prefabs
    internal static GameObject kFighterPrefab = Resources.Load("Prefabs/KinFighter") as GameObject;
    internal static GameObject kFighterUI = Resources.Load("Prefabs/UI_Fighter") as GameObject;
    internal static GameObject kFighterCam = Resources.Load("Prefabs/Fighter_Camera") as GameObject;

    // Sniper Prefabs
    internal static GameObject kSniperUI = Resources.Load("Prefabs/UI_Sniper") as GameObject;
    internal static GameObject kSniperPrefab = Resources.Load("Prefabs/KinSniper") as GameObject;
    internal static GameObject kSniperCam = Resources.Load("Prefabs/Sniper_Camera") as GameObject;

    // Mage Prefabs
    internal static GameObject kMageUI = Resources.Load("Prefabs/UI_Mage") as GameObject;
    internal static GameObject kMagePrefab = Resources.Load("Prefabs/KinMage") as GameObject;
    internal static GameObject kMageCam = Resources.Load("Prefabs/Mage_Camera") as GameObject;

    // General UI prefabs
    internal static GameObject kDeathPanelPrefab = Resources.Load("Prefabs/DeathPanel") as GameObject;

    // Client Side Effects
    internal static GameObject kCombatTextPrefab = Resources.Load("Prefabs/CombatText") as GameObject;
    internal static GameObject kLaserPrefab = Resources.Load("Prefabs/Laser") as GameObject;
    internal static GameObject kExplosionPrefab = Resources.Load("Prefabs/Volumetric") as GameObject;
    internal static GameObject kFireflashPrefab = Resources.Load("Prefabs/Fireflash") as GameObject;
    internal static GameObject kLightningStrikePrefab = Resources.Load("Prefabs/LightningStrike") as GameObject;
    internal static GameObject kSniperWeaponRifle = Resources.Load("Prefabs/SniperWeaponRifle") as GameObject;
    internal static GameObject kSniperWeaponShotgun = Resources.Load("Prefabs/SniperWeaponShotgun") as GameObject;
    internal static GameObject kSniperWeaponMedigun = Resources.Load("Prefabs/SniperWeaponMedigun") as GameObject;
    internal static GameObject kFighterHitPrefab = Resources.Load("Prefabs/FighterHit") as GameObject;

    // Effects
    internal static GameObject kPoisonZonePrefab = Resources.Load("Prefabs/PoisonZone") as GameObject;
    internal static GameObject kThunderCloudPrefab = Resources.Load("Prefabs/ThunderCloud") as GameObject;
    internal static GameObject kDeathWavePrefab = Resources.Load("Prefabs/DeathWave") as GameObject;

    // Buffs
    internal static GameObject kChargeStunPrefab = Resources.Load("Prefabs/ChargeStun") as GameObject;
    internal static GameObject kFireflashDotPrefab = Resources.Load("Prefabs/FireflashDot") as GameObject;
    internal static GameObject kFrostflashSlowPrefab = Resources.Load("Prefabs/FrostflashSlow") as GameObject;

    // Monsters
    internal static GameObject kFreyjaPrefab = Resources.Load("Prefabs/Freyja") as GameObject;
    internal static GameObject kValkyriePrefab = Resources.Load("Prefabs/Valkyrie") as GameObject;
    internal static GameObject kDryadPrefab = Resources.Load("Prefabs/Dryad") as GameObject;
    internal static GameObject kThorPrefab = Resources.Load("Prefabs/Thor") as GameObject;
    internal static GameObject kLokiPrefab = Resources.Load("Prefabs/Loki") as GameObject;
    internal static GameObject kMagnetPrefab = Resources.Load("Prefabs/Magnet") as GameObject;

    //internal static GameObject kCapsulePrefab = Resources.Load("Prefabs/PlayerCapsule") as GameObject;

    internal enum UnitEntityCode
    {
        kEmpty,
        kFighter,
        kFreyja,
        kSniper,
        kValkyrie,
        kDryad,
        kThor,
        kLoki,
        kMagnet,
        kMage,
        kLokiClone
    }; // kCapsule

    internal enum EffectEntityCode
    {
        kPoisonZone,
        kThunderCloud,
        kDeathWave
    };

    internal enum BuffEntityCode
    {
        kChargeDebuff,
        kFireflashDebuff,
        kFrostflashDebuff
    };

    internal static Dictionary<UnitEntityCode, GameObject> UnitEntityCodes = new Dictionary<UnitEntityCode, GameObject>
    {
        { UnitEntityCode.kEmpty, kEmptyPrefab },
        { UnitEntityCode.kFighter, kFighterPrefab },
        { UnitEntityCode.kFreyja, kFreyjaPrefab },
        { UnitEntityCode.kSniper, kSniperPrefab },
        { UnitEntityCode.kValkyrie, kValkyriePrefab },
        { UnitEntityCode.kDryad, kDryadPrefab },
        { UnitEntityCode.kThor, kThorPrefab },
        { UnitEntityCode.kLoki, kLokiPrefab },
        { UnitEntityCode.kMagnet, kMagnetPrefab },
        { UnitEntityCode.kMage, kMagePrefab },
        { UnitEntityCode.kLokiClone, kLokiPrefab },
    };

    internal static Dictionary<EffectEntityCode, GameObject> EffectEntityCodes = new Dictionary<EffectEntityCode, GameObject>
    {
        { EffectEntityCode.kPoisonZone, kPoisonZonePrefab },
        { EffectEntityCode.kThunderCloud, kThunderCloudPrefab },
        { EffectEntityCode.kDeathWave, kDeathWavePrefab },
    };

    internal static Dictionary<BuffEntityCode, GameObject> BuffEntityCodes = new Dictionary<BuffEntityCode, GameObject>
    {
        { BuffEntityCode.kChargeDebuff, kChargeStunPrefab },
        { BuffEntityCode.kFireflashDebuff, kFireflashDotPrefab },
        { BuffEntityCode.kFrostflashDebuff, kFrostflashSlowPrefab }
    };

    internal static Dictionary<UnitEntityCode, Action<UnitEntity>> UnitEntityScripts = new Dictionary<UnitEntityCode, Action<UnitEntity>>
    {
        { UnitEntityCode.kFreyja, AddComponent<FreyjaScript> },
        { UnitEntityCode.kValkyrie, AddComponent<ValkyrieScript> },
        { UnitEntityCode.kDryad, AddComponent<DryadScript> },
        { UnitEntityCode.kThor, AddComponent<ThorScript> },
        { UnitEntityCode.kLoki, AddComponent<LokiScript> },
        { UnitEntityCode.kMagnet, AddComponent<EmptyMonsterScript> },
        { UnitEntityCode.kLokiClone, AddComponent<LokiCloneScript> }
    };

    internal static Dictionary<EffectEntityCode, Action<EffectEntity>> EffectEntityScripts = new Dictionary<EffectEntityCode, Action<EffectEntity>>
    {
        { EffectEntityCode.kPoisonZone, AddComponent<PoisonZoneScript> },
        { EffectEntityCode.kThunderCloud, AddComponent<ThunderCloudScript> },
        { EffectEntityCode.kDeathWave, AddComponent<DeathWaveScript> },
    };

    internal static Dictionary<BuffEntityCode, Action<BuffEntity>> BuffEntityScripts = new Dictionary<BuffEntityCode, Action<BuffEntity>>
    {
        { BuffEntityCode.kChargeDebuff, AddComponent<ChargeDebuffScript> },
        { BuffEntityCode.kFireflashDebuff, AddComponent<FireflashDebuffScript> },
        { BuffEntityCode.kFrostflashDebuff, AddComponent<FrostflashDebuffScript> }
    };

    internal enum EntityAnimation
    {
        kIdle = 0,
        kWalkForward,
        kWalkRight,
        kWalkLeft,
        kWalkBack,
        kRun,
        kCrouch,
        kMageChanneling
    };

    internal static Dictionary<EntityAnimation, string> AnimationStrings = new Dictionary<EntityAnimation, string>
    {
        { EntityAnimation.kIdle, "Idle" },
        { EntityAnimation.kWalkForward, "Walk Forward" },
        { EntityAnimation.kWalkRight, "WalkRight" },
        { EntityAnimation.kWalkLeft, "WalkLeft" },
        { EntityAnimation.kWalkBack, "Walk Backward" },
        { EntityAnimation.kRun, "Run" },
        { EntityAnimation.kCrouch, "Crouch" }
    };

    internal static Dictionary<EntityAnimation, string> MageAnimationStrings = new Dictionary<EntityAnimation, string>
    {
        { EntityAnimation.kIdle, "Idle" },
        { EntityAnimation.kWalkForward, "Moving" },
        { EntityAnimation.kWalkRight, "Moving" },
        { EntityAnimation.kWalkLeft, "Moving" },
        { EntityAnimation.kWalkBack, "Moving" },
        { EntityAnimation.kMageChanneling, "Channeling" }
    };

    internal enum EntityAnimationTrigger
    {
        kDeath = 0,
        kRevive,
        kFighterSpin,
        kFighterAttackLeft,
        kFighterAttackRight,
        kFighterDodgeFront,
        kFighterDodgeBack,
        kFighterDodgeLeft,
        kFighterDodgeRight,
        kNpcAttackLeft,
        kNpcAttackRight,
        kSniperAttackLeft,
        kSniperAttackRight,
        kSniperReload,
        kMageFireflash,
        kMageFrostflash,
        kMageArcaneflash,
        kMageChannelEnd,
        kMageChannelFailed,
        kThorSlam,
        kThorChainLightning,
        kLokiSplitStart
    };

    internal static Dictionary<EntityAnimationTrigger, string> AnimationTriggerStrings = new Dictionary<EntityAnimationTrigger, string>
    {
        { EntityAnimationTrigger.kDeath, "DeathTrigger" },
        { EntityAnimationTrigger.kRevive, "ReviveTrigger" },
        { EntityAnimationTrigger.kFighterSpin, "MoveAttack2Trigger" },
        { EntityAnimationTrigger.kFighterAttackLeft, "PunchTrigger" },
        { EntityAnimationTrigger.kFighterAttackRight, "KickTrigger" },
        { EntityAnimationTrigger.kFighterDodgeBack, "RollBackwardTrigger" },
        { EntityAnimationTrigger.kFighterDodgeFront, "RollForwardTrigger" },
        { EntityAnimationTrigger.kFighterDodgeLeft, "DashLeftTrigger" },
        { EntityAnimationTrigger.kFighterDodgeRight, "DashRightTrigger" },
        { EntityAnimationTrigger.kNpcAttackLeft, "PunchTrigger" },
        { EntityAnimationTrigger.kNpcAttackRight, "KickTrigger" },
        { EntityAnimationTrigger.kSniperAttackLeft, "JabTrigger" },
        { EntityAnimationTrigger.kSniperAttackRight, "RangeAttack1Trigger" },
        { EntityAnimationTrigger.kSniperReload, "RangeAttack2Trigger" },
        { EntityAnimationTrigger.kMageFireflash, "TriggerQuick" },
        { EntityAnimationTrigger.kMageFrostflash, "TriggerQuick" },
        { EntityAnimationTrigger.kMageArcaneflash, "TriggerQuick" },
        { EntityAnimationTrigger.kMageChannelEnd, "TriggerChannelEnd" },
        { EntityAnimationTrigger.kMageChannelFailed, "TriggerChannelFail" },
        { EntityAnimationTrigger.kThorSlam, "JumpTrigger" },
        { EntityAnimationTrigger.kThorChainLightning, "AxeKickTrigger" },
        { EntityAnimationTrigger.kLokiSplitStart, "Victory2Trigger" }
    };

    // Utils methods called by server to add scripts to Units, Effects, and Buffs
    internal static void AddComponent<T>(UnitEntity unit) where T : MonoBehaviour, IConfigurableMonster
    {
        IConfigurableMonster monster = unit.GameObject.AddComponent<T>();
        monster.Config(unit);
    }

    internal static void AddComponent<T>(EffectEntity effect) where T : MonoBehaviour, IConfigurableEffect
    {
        IConfigurableEffect script = effect.GameObject.AddComponent<T>();
        script.Config(effect);
    }

    internal static void AddComponent<T>(BuffEntity buff) where T : MonoBehaviour, IConfigurableBuff
    {
        IConfigurableBuff script = buff.GameObject.AddComponent<T>();
        script.Config(buff);
    }
}
