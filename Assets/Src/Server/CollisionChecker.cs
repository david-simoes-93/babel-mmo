using UnityEngine;
using UnityEditor;
using static Globals;
using KinematicCharacterController;
using System.Collections.Generic;

/// <summary>
/// Utils to check collisions between objects
/// </summary>
internal class CollisionChecker : ScriptableObject
{
    /// <summary>
    /// Circular collision check around a point, hitting all UnitEntity within range
    /// </summary>
    /// <param name="attackSource">Explosion center</param>
    /// <param name="attackRange">Explosion radius</param>
    /// <param name="gameObject">Caster GameObject</param>
    /// <param name="casterImmune">Whether caster should be immune to explosion (default: True)</param>
    /// <param name="npcsImmune">Whether other NPCs should be immune (default: False)</param>
    /// <returns>list of UnitEntity that explosion has affected</returns>
    internal static List<UnitEntity> CheckExplosionRadius(Vector3 attackSource, int attackRange, GameObject gameObject, bool casterImmune = true, bool npcsImmune = false)
    {
        //Debug.DrawLine(attackSource, attackSource + new Vector3(0, 10, 0), Color.white, 10);
        //Debug.DrawLine(gameObject.transform.position + new Vector3(0, 1, -2), gameObject.transform.position + new Vector3(0, 1, 2), Color.white, 10);
        //GameDebug.Log(gameObject+" is spinning");
        List<UnitEntity> affectedUnits = new List<UnitEntity>();

        Collider[] hitColliders = Physics.OverlapSphere(attackSource, attackRange);

        CapsuleCollider myCollider = gameObject.GetComponent<CapsuleCollider>();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i] == myCollider && casterImmune)
                continue;

            // if not a unit, skip
            BaseControllerKin controller = hitColliders[i].gameObject.GetComponent<BaseControllerKin>();
            if (controller == null)
                continue;

            // if not attackable, skip
            UnitEntity otherChar = controller.Parent();
            if (!otherChar.IsAttackable)
                continue;

            // other NPC
            if (npcsImmune && otherChar.Uid < 0 && hitColliders[i] != myCollider)
                continue;

            affectedUnits.Add(otherChar);
        }

        return affectedUnits;
    }

    /// <summary>
    /// Line collision from a point into a given direction, hitting first UnitEntity
    /// </summary>
    /// <param name="attackSource">Laser source</param>
    /// <param name="attackDirection">Laser direction</param>
    /// <param name="attackRange">Laser length</param>
    /// <param name="gameObject">Caster GameObject</param>
    /// <param name="raycastThickness">Laser thickness (default: 0.3)</param>
    /// <returns>tuple with first hit UnitEntity and point in space where it got hit</returns>
    internal static (UnitEntity, Vector3) CheckCollisionOnEnemies(
        Vector3 attackSource,
        Vector3 attackDirection,
        int attackRange,
        GameObject gameObject,
        float raycastThickness = 0.3f
    )
    {
        //GameDebug.Log(cd+"  "+gameObject);
        RaycastHit closestHit = new RaycastHit { distance = Mathf.Infinity };

        RaycastHit[] _obstructions = new RaycastHit[32];
        int _obstructionCount = Physics.SphereCastNonAlloc(
            attackSource,
            raycastThickness,
            attackDirection,
            _obstructions,
            attackRange,
            -1,
            QueryTriggerInteraction.Ignore
        );

        //only shows in scene mode -.-
        //Debug.DrawLine(transform.position+new Vector3(0, 1, 0), transform.position+ new Vector3(0, 1, 0) + transform.forward.normalized*2, Color.white, 10);
        //Debug.DrawRay(transform.position+ new Vector3(0, 1, 0), transform.forward, Color.white, 10);

        CapsuleCollider myCollider = gameObject.GetComponent<CapsuleCollider>();
        for (int i = 0; i < _obstructionCount; i++)
        {
            if (_obstructions[i].collider == myCollider)
                continue;

            // if not a unit, skip
            BaseControllerKin controller = _obstructions[i].collider.gameObject.GetComponent<BaseControllerKin>();
            if (controller == null)
                continue;

            // if not attackable, skip
            UnitEntity otherChar = controller.Parent();
            if (!otherChar.IsAttackable)
                continue;

            return (otherChar, _obstructions[i].point);
        }

        return (null, Vector3.zero);
    }

    /// <summary>
    /// Line collision from a point into the direction that the caster is facing, hitting first UnitEntity
    /// </summary>
    /// <param name="attackSource">Laser source</param>
    /// <param name="attackRange">Laser length</param>
    /// <param name="gameObject">Caster GameObject</param>
    /// <returns>tuple with first hit UnitEntity and point in space where it got hit</returns>
    internal static (UnitEntity, Vector3) CheckCollisionForwardOnEnemies(Vector3 attackSource, int attackRange, GameObject gameObject)
    {
        return CheckCollisionOnEnemies(attackSource, gameObject.transform.forward, attackRange, gameObject);
    }

    /// <summary>
    /// Line collision from a point into a given direction, hitting first object
    /// </summary>
    /// <param name="attackSource">Laser source</param>
    /// <param name="attackDirection">Laser direction</param>
    /// <param name="attackRange">Laser length</param>
    /// <param name="gameObject">Caster GameObject</param>
    /// <param name="raycastThickness">Laser thickness (default: 0.3)</param>
    /// <returns>tuple with first hit Object and point in space where it got hit</returns>
    internal static (GameObject, Vector3) CheckCollisionOnAnyObstacle(
        Vector3 attackSource,
        Vector3 attackDirection,
        int attackRange,
        GameObject gameObject,
        float raycastThickness = 0.3f
    )
    {
        //GameDebug.Log(cd+"  "+gameObject);
        RaycastHit closestHit = new RaycastHit();
        closestHit.distance = Mathf.Infinity;

        RaycastHit[] _obstructions = new RaycastHit[32];
        int _obstructionCount = Physics.SphereCastNonAlloc(
            attackSource,
            raycastThickness,
            attackDirection,
            _obstructions,
            attackRange,
            -1,
            QueryTriggerInteraction.Ignore
        );

        //only shows in scene mode -.-
        //Debug.DrawLine(transform.position+new Vector3(0, 1, 0), transform.position+ new Vector3(0, 1, 0) + transform.forward.normalized*2, Color.white, 10);
        //Debug.DrawRay(transform.position+ new Vector3(0, 1, 0), transform.forward, Color.white, 10);

        CapsuleCollider myCollider = gameObject.GetComponent<CapsuleCollider>();
        for (int i = 0; i < _obstructionCount; i++)
        {
            if (_obstructions[i].collider == myCollider)
                continue;

            //CheckExplosionRadius(cd, _obstructions[i].point, explosionRange, gameObject, casterImmune:false);

            return (_obstructions[i].transform.gameObject, _obstructions[i].point);
        }

        return (null, Vector3.zero);
    }

    /// <summary>
    /// Line collision from a point into the direction that the caster is facing, hitting first Object
    /// </summary>
    /// <param name="attackSource">Laser source</param>
    /// <param name="attackRange">Laser length</param>
    /// <param name="gameObject">Caster GameObject</param>
    /// <returns>tuple with first hit object and point in space where it got hit</returns>
    internal static (GameObject, Vector3) CheckCollisionForwardOnAnyObstacle(Vector3 attackSource, int attackRange, GameObject gameObject)
    {
        return CheckCollisionOnAnyObstacle(attackSource, gameObject.transform.forward, attackRange, gameObject);
    }

    internal static List<UnitEntity> CheckBoxCollisionOnEnemies(
        Vector3 attackSource,
        Vector3 halfExtents,
        Quaternion orientation,
        GameObject gameObject,
        bool casterImmune = true,
        bool npcsImmune = false
    )
    {
        List<UnitEntity> affectedUnits = new List<UnitEntity>();

        Collider[] hitColliders = Physics.OverlapBox(attackSource, halfExtents, orientation);

        CapsuleCollider myCollider = gameObject.GetComponent<CapsuleCollider>();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i] == myCollider && casterImmune)
                continue;

            // if not a unit, skip
            BaseControllerKin controller = hitColliders[i].gameObject.GetComponent<BaseControllerKin>();
            if (controller == null)
                continue;

            // if not attackable, skip
            UnitEntity otherChar = controller.Parent();
            if (!otherChar.IsAttackable)
                continue;

            // other NPC
            if (npcsImmune && otherChar.Uid < 0 && hitColliders[i] != myCollider)
                continue;

            affectedUnits.Add(otherChar);
        }

        return affectedUnits;
    }
}
