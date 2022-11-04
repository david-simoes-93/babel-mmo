using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class ThunderCloudScript : MonoBehaviour, IConfigurableEffect
{
    // Server-side script that does damage to players underneath cloud
    private Vector3 spawn_point_;
    private EffectEntity parent_;
    private int uid_;

    private long last_tick_time_ms_;

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
        if (Globals.currTime_ms < last_tick_time_ms_ + 100)
            return;
        last_tick_time_ms_ = Globals.currTime_ms;

        // cloud center is at 7.75 away from the cloud spawnpoint, at a 45º angle, (so a vector(5.48, 5.48)), and radius is 12 to cover from center of platform to its edge
        Vector3 cloud_center = spawn_point_ + parent_.EffectTransform().rotation * new Vector3(5.48f, 0, 5.48f);
        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(cloud_center, 12, gameObject, casterImmune: true, npcsImmune: true);

        foreach (UnitEntity otherChar in collidedTargets)
        {
            otherChar.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(uid_, otherChar.Uid, CastCode.ThorThunderCloudTick, 2));
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
        parent_ = parent;
        last_tick_time_ms_ = Globals.currTime_ms;
    }
}
