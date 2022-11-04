using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class PoisonZoneScript : MonoBehaviour, IConfigurableEffect
{
    private Vector3 spawn_point_;
    private int uid_;

    private long last_tick_time_ms;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start() { }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        // tick at 10Hz
        if (Globals.currTime_ms < last_tick_time_ms + 100)
            return;

        last_tick_time_ms = Globals.currTime_ms;
        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(spawn_point_, 2, gameObject, casterImmune: true, npcsImmune: true);

        foreach (UnitEntity otherChar in collidedTargets)
        {
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(uid_, otherChar.Uid, CastCode.FreyjaPoisonZoneTick, 5));
        }
    }

    /// <summary>
    /// Configures effect with its corresponding parent Entity
    /// </summary>
    /// <param name="parent">the parent</param>
    public void Config(EffectEntity parent)
    {
        uid_ = parent.Uid;
        spawn_point_ = parent.EffectTransform().position;
        last_tick_time_ms = Globals.currTime_ms;
    }
}
