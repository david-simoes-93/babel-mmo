using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FindEntityUtils : ScriptableObject
{
    /// <summary>
    /// <summary>
    /// Finds the closest living unit entity to a given position, searching through entities within a given radius
    /// </summary>
    /// <param name="em">the entity manager with all entities</param>
    /// <param name="target_position">position to compare with</param>
    /// <param name="spawn_point">if max_distance_to_spawn_point!=0, this method only considers entities with some distance of a given spawn_point</param>
    /// <param name="max_distance_to_spawn_point">0 if there is no range limit, >0 if only entities within some area should be considered</param>
    /// <param name="ignore_these_uids">UIDs that will be ignored (usually self), or empty</param>
    /// <param name="affect_npcs">whether to affect NPCs (default: true)</param>
    /// <param name="affect_players">whether to affect Players (default: true)</param>
    /// <param name="only_attackable">whether to affect only attackable entities (default: true)</param>
    /// <returns>a tuple containing (Entity, distance, direction) of closest enemy, or (null, X, X) if no entities within range</returns>
    internal static (UnitEntity, float, Vector3) FindClosestEntity(
        EntityManager em,
        Vector3 target_position,
        Vector3 spawn_point,
        float max_distance_to_spawn_point,
        int[] ignore_these_uids,
        bool affect_npcs = false,
        bool affect_players = true,
        bool only_attackable = true
    )
    {
        UnitEntity closestEntity = null;
        float closestEntityDistance = Mathf.Infinity;
        Vector3 closestEntityDirection = new Vector3(0, 0, 0);

        List<UnitEntity> playersInSpawnZone = FindEntitiesWithinRadius(
            em,
            spawn_point,
            max_distance_to_spawn_point,
            ignore_these_uids,
            affect_npcs,
            affect_players,
            only_attackable
        );
        // find closest player
        foreach (UnitEntity unit in playersInSpawnZone)
        {
            // within distance of spawn
            if (max_distance_to_spawn_point > 0)
            {
                Vector3 entityToSpawn = spawn_point - unit.UnitTransform().position;
                if (entityToSpawn.magnitude > max_distance_to_spawn_point)
                    continue;
            }

            Vector3 thisDir = unit.UnitTransform().position - target_position;
            Vector2 horizontalDir = new Vector2(thisDir.x, thisDir.z);
            float distance = horizontalDir.magnitude;

            if (distance < closestEntityDistance)
            {
                closestEntityDistance = distance;
                closestEntity = unit;
                closestEntityDirection = thisDir;
            }
        }

        return (closestEntity, closestEntityDistance, closestEntityDirection);
    }

    /// <summary>
    /// <summary>
    /// Finds the closest living unit entity to a given position, searching through entities within a given radius
    /// </summary>
    /// <param name="em">the entity manager with all entities</param>
    /// <param name="target_position">position to compare with</param>
    /// <param name="ignore_these_uids">UIDs that will be ignored (usually self), or empty</param>
    /// <param name="affect_npcs">whether to affect NPCs (default: true)</param>
    /// <param name="affect_players">whether to affect Players (default: true)</param>
    /// <param name="only_attackable">whether to affect only attackable entities (default: true)</param>
    /// <returns>a tuple containing (Entity, distance, direction) of closest enemy, or (null, X, X) if no entities within range</returns>
    internal static (UnitEntity, float, Vector3) FindClosestEntity(
        EntityManager em,
        Vector3 target_position,
        int[] ignore_these_uids,
        bool affect_npcs = false,
        bool affect_players = true,
        bool only_attackable = true
    )
    {
        UnitEntity closestEntity = null;
        float closestEntityDistance = Mathf.Infinity;
        Vector3 closestEntityDirection = new Vector3(0, 0, 0);

        foreach (KeyValuePair<int, UnitEntity> kvp in em.tempUnitEntities)
        {
            UnitEntity unit = kvp.Value;

            // attackable
            if (!unit.IsAttackable && only_attackable)
                continue;

            // mobs
            if (unit.Uid < 0 && !affect_npcs)
                continue;

            // players
            if (unit.Uid > 0 && !affect_players)
                continue;

            // ignore th
            if (ignore_these_uids.Contains(unit.Uid))
                continue;

            Vector3 thisDir = unit.UnitTransform().position - target_position;
            Vector2 horizontalDir = new Vector2(thisDir.x, thisDir.z);
            float distance = horizontalDir.magnitude;

            if (distance < closestEntityDistance)
            {
                closestEntityDistance = distance;
                closestEntity = unit;
                closestEntityDirection = thisDir;
            }
        }

        return (closestEntity, closestEntityDistance, closestEntityDirection);
    }

    /// <summary>
    /// <summary>
    /// Finds the living unit entities within a given radius (iterates through ALL entities in world)
    /// </summary>
    /// <param name="em">the entity manager with all entities</param>
    /// <param name="target_position">position to compare with</param>
    /// <param name="radius">max distance</param>
    /// <param name="ignore_these_uids">UIDs that will be ignored (usually self), or empty</param>
    /// <param name="affect_npcs">whether to affect NPCs (default: true)</param>
    /// <param name="affect_players">whether to affect Players (default: true)</param>
    /// <param name="only_attackable">whether to affect only attackable entities (default: true)</param>
    /// <returns>a list of UnitEntities that match the criteria</returns>
    internal static List<UnitEntity> FindEntitiesWithinRadius(
        EntityManager em,
        Vector3 target_position,
        float radius,
        int[] ignore_these_uids,
        bool affect_npcs = true,
        bool affect_players = true,
        bool only_attackable = true
    )
    {
        List<UnitEntity> targets = new List<UnitEntity>();

        // find closest player
        foreach (KeyValuePair<int, UnitEntity> kvp in em.tempUnitEntities)
        {
            // attackable
            if (!kvp.Value.IsAttackable && only_attackable)
                continue;

            // mobs
            if (kvp.Value.Uid < 0 && !affect_npcs)
                continue;

            // players
            if (kvp.Value.Uid > 0 && !affect_players)
                continue;

            // ignore
            if (ignore_these_uids.Contains(kvp.Value.Uid))
                continue;

            Vector3 direction = kvp.Value.UnitTransform().position - target_position;
            float distance = direction.magnitude;

            if (distance < radius)
            {
                targets.Add(kvp.Value);
            }
        }

        return targets;
    }
}
