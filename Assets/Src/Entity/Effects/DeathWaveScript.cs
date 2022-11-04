using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class DeathWaveScript : MonoBehaviour, IConfigurableEffect
{
    // Server-side script that kills players that cross the wave
    private EffectEntity parent_;
    private int uid_,
        creator_uid_;
    private Vector3 position_delta_,
        half_size_;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        position_delta_ = new Vector3(0, 4.5f, 0);
        half_size_ = new Vector3(19f, 4.5f, 0.1f);
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        List<UnitEntity> collidedTargets = CollisionChecker.CheckBoxCollisionOnEnemies(
            parent_.EffectTransform().position + position_delta_,
            half_size_,
            Quaternion.identity,
            gameObject,
            casterImmune: true,
            npcsImmune: true
        );

        foreach (UnitEntity otherChar in collidedTargets)
        {
            // don't kill Loki
            if (otherChar.Uid == creator_uid_)
            {
                continue;
            }
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(uid_, otherChar.Uid, CastCode.LokiDeathWaveTick, 1000));
        }
    }

    /// <summary>
    /// Configures effect with its corresponding parent Entity
    /// </summary>
    /// <param name="parent">the parent</param>
    public void Config(EffectEntity parent)
    {
        uid_ = parent.Uid;
        parent_ = parent;
        creator_uid_ = parent.CreatorUid;
    }
}
