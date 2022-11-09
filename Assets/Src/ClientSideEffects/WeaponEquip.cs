using UnityEngine;
using UnityEditor;

/// <summary>
/// A Laser effect, vanishing over time
/// </summary>
internal class WeaponEquip : ILocalEffect
{
    private GameObject myGameObject_;

    /// <summary>
    /// Constructs a weapon attached to a kSniper entity
    /// </summary>
    /// <param name="parent">Parent entity</param>
    /// <param name="weapon">Weapon cast code</param>
    internal WeaponEquip(UnitEntity parent, Globals.CastCode weapon)
    {
        switch (weapon)
        {
            case Globals.CastCode.SniperChooseWeaponOne:
                myGameObject_ = Object.Instantiate(Globals.kSniperWeaponOne, parent.UnitTransform());
                break;
            case Globals.CastCode.SniperChooseWeaponTwo:
                myGameObject_ = Object.Instantiate(Globals.kSniperWeaponTwo, parent.UnitTransform());
                break;
            case Globals.CastCode.SniperChooseWeaponThree:
                myGameObject_ = Object.Instantiate(Globals.kSniperWeaponThree, parent.UnitTransform());
                break;
            default:
                GameDebug.Log("Unknown weapon selected!");
                myGameObject_ = Object.Instantiate(Globals.kEmptyPrefab, parent.UnitTransform());
                break;
        }
        //myGameObject_.transform.position = src;
        //myGameObject_.transform.localScale *= explosionRadius / 2;
    }

    /// <summary>
    /// Destroys the game object
    /// </summary>
    public void Destroy()
    {
        GameDebug.Log("dtor!");
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
