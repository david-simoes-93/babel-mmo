using UnityEngine;
using UnityEditor;

/// <summary>
/// A WeaponEquip effect, only vanishes when deleted
/// </summary>
internal class WeaponEquip : ILocalEffect
{
    private GameObject myGameObject_;

    /// <summary>
    /// Constructs a weapon attached to a kSniper entity
    /// </summary>
    /// <param name="parent">Parent entity</param>
    /// <param name="weapon">CastCode cast code</param>
    internal WeaponEquip(UnitEntity parent, Globals.CastCode weapon)
    {
        Transform parent_transform;
        if (parent.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
        {
            parent_transform = parent.CameraTransform();
        }
        else
        {
            parent_transform = parent.UnitTransform();
        }

        switch (weapon)
        {
            case Globals.CastCode.SniperChooseWeaponRifle:
                myGameObject_ = Object.Instantiate(Globals.kSniperWeaponRifle, parent_transform);
                if (parent.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    myGameObject_.transform.localPosition = new Vector3(0.1f, -0.4f, 0.1f);
                }
                break;
            case Globals.CastCode.SniperChooseWeaponShotgun:
                myGameObject_ = Object.Instantiate(Globals.kSniperWeaponShotgun, parent_transform);
                if (parent.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    myGameObject_.transform.localPosition = new Vector3(0.2f, -0.4f, 0.4f);
                }
                break;
            case Globals.CastCode.SniperChooseWeaponMedigun:
                myGameObject_ = Object.Instantiate(Globals.kSniperWeaponMedigun, parent_transform);
                if (parent.Uid == ClientGameLoop.CGL.UnitEntity.Uid)
                {
                    myGameObject_.transform.localPosition = new Vector3(0.1f, -0.3f, 0.7f);
                }
                break;
            default:
                GameDebug.Log("Unknown weapon selected!");
                myGameObject_ = Object.Instantiate(Globals.kEmptyPrefab, parent_transform);
                break;
        }
    }

    /// <summary>
    /// Destroys the game object
    /// </summary>
    public void Destroy()
    {
        Object.Destroy(myGameObject_);
    }

    /// <summary>
    /// Effect always returns false, doesn't expire
    /// </summary>
    /// <returns>false</returns>
    public bool Update()
    {
        return false;
    }
}
