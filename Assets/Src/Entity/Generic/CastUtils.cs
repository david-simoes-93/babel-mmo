using UnityEngine;
using UnityEditor;

public class CastUtils : ScriptableObject
{
    static internal VectorCastRD MakeFighterAttackLeft(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.FighterAttackLeft, position, rotation);
    }

    static internal VectorCastRD MakeFighterAttackRight(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.FighterAttackRight, position, rotation);
    }

    static internal TargetedCastRD MakeMonsterAttackLeft(int uid, int target_uid)
    {
        return new TargetedCastRD(uid, target_uid, Globals.CastCode.MonsterAttackLeft);
    }

    static internal TargetedCastRD MakeMonsterRangedAttack(int uid, int target_uid)
    {
        return new TargetedCastRD(uid, target_uid, Globals.CastCode.MonsterRangedAttack);
    }

    static internal VectorCastRD MakeSniperWeaponOneAttack(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.SniperWeaponOneFire, position, rotation);
    }

    static internal VectorCastRD MakeSniperWeaponOneAlternateAttack(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.SniperWeaponOneAlternate, position, rotation);
    }

    static internal VectorCastRD MakeSniperWeaponTwoAttack(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.SniperWeaponTwoFire, position, rotation);
    }

    static internal VectorCastRD MakeSniperWeaponTwoAlternateAttack(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.SniperWeaponTwoAlternate, position, rotation);
    }

    static internal VectorCastRD MakeSniperWeaponThreeAttack(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.SniperWeaponThreeFire, position, rotation);
    }

    static internal CastRD MakeSniperWeaponThreeAlternateAttack(int uid)
    {
        return new CastRD(uid, Globals.CastCode.SniperWeaponThreeAlternate);
    }

    static internal CastRD MakeReload(int uid)
    {
        return new CastRD(uid, Globals.CastCode.SniperReload);
    }

    static internal CastRD MakeChooseWeaponOne(int uid)
    {
        return new CastRD(uid, Globals.CastCode.SniperChooseWeaponRifle);
    }

    static internal CastRD MakeChooseWeaponTwo(int uid)
    {
        return new CastRD(uid, Globals.CastCode.SniperChooseWeaponShotgun);
    }

    static internal CastRD MakeChooseWeaponThree(int uid)
    {
        return new CastRD(uid, Globals.CastCode.SniperChooseWeaponMedigun);
    }

    static internal CastRD MakeSuicideBomb(int uid)
    {
        return new CastRD(uid, Globals.CastCode.DryadSuicideBomb);
    }

    static internal CastRD MakeCharge(int uid)
    {
        return new CastRD(uid, Globals.CastCode.FighterCharge);
    }

    static internal TargetedCastRD MakeChargeStun(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.FighterChargeStun);
    }

    static internal CastRD MakeDodge(int uid, Globals.CastCode dodge)
    {
        return new CastRD(uid, dodge);
    }

    static internal VectorCastRD MakeRespawn(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.Respawn, position, rotation);
    }

    static internal VectorCastRD MakeOutOfBoundsTeleport(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.OutOfBoundsTeleport, position, rotation);
    }

    static internal TargetedCastRD MakeStunCarry(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.ValkyrieStunCarry);
    }

    static internal TargetedCastRD MakeDropCarried(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.ValkyrieDropCarried);
    }

    static internal CastRD MakePoisonZone(int uid)
    {
        return new CastRD(uid, Globals.CastCode.FreyjaPoisonZone);
    }

    static internal VectorCastRD MakeThorSlam(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.ThorSlam, position, rotation);
    }

    static internal MultiTargetedCastRD MakeThorChainLightning(int uid, int[] targets)
    {
        return new MultiTargetedCastRD(uid, targets, Globals.CastCode.ThorChainLightning);
    }

    static internal TargetedCastRD MakeMageFireflash(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.MageFireflash);
    }

    static internal TargetedCastRD MakeMageFrostflash(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.MageFrostflash);
    }

    static internal TargetedCastRD MakeMageArcaneflash(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.MageArcaneflash);
    }

    static internal TargetedCastRD MakeMagePyroblast(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.MagePyroblast);
    }

    static internal TargetedCastRD MakeMagePyroblastEnd(TargetedCastRD pyro)
    {
        return new TargetedCastRD(pyro.caster_uid, pyro.target_uid, Globals.CastCode.MagePyroblastEnd);
    }

    static internal TargetedCastRD MakeMageRenew(int uid, int target)
    {
        return new TargetedCastRD(uid, target, Globals.CastCode.MageRenew);
    }

    static internal TargetedCastRD MakeMageRenewEnd(TargetedCastRD renew)
    {
        return new TargetedCastRD(renew.caster_uid, renew.target_uid, Globals.CastCode.MageRenewEnd);
    }

    static internal CastRD MakeMageCastStop(int uid)
    {
        return new CastRD(uid, Globals.CastCode.MageCastStop);
    }

    static internal CastRD MakeLokiSplitStart(int uid)
    {
        return new CastRD(uid, Globals.CastCode.LokiSplitStart);
    }

    static internal VectorCastRD MakeLokiSplitEnd(int uid, Vector3 position, Quaternion rotation)
    {
        return new VectorCastRD(uid, Globals.CastCode.LokiSplitEnd, position, rotation);
    }
}
