using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderCloudBase : MonoBehaviour
{
    private long last_tick_time_ms_;
    private readonly System.Random random_ = new System.Random();

    // Start is called before the first frame update
    void Start() { }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        // TODO not synced with client! movement should be server-side, and position sent to client
        // Thundercloud slowly rotates upon its spawnpoint, which is at one of its corners
        gameObject.transform.Rotate(0, 0.1f, 0);
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: some client-side rain? who knows
#if !UNITY_SERVER
        ShowLightning();
#else
        // do nothing
#endif
    }

    void ShowLightning()
    {
        // tick at 10Hz
        if (Globals.currTime_ms < last_tick_time_ms_ + 100)
            return;
        last_tick_time_ms_ = Globals.currTime_ms;

        // cloud center is at 7.75 away from the cloud spawnpoint, at a 45º angle, (so a vector(5.48, 5.48)), and radius is 12 to cover from center of platform to its edge
        Vector3 cloud_center = transform.position + transform.rotation * new Vector3(5.48f, 0, 5.48f);
        List<UnitEntity> collidedTargets = CollisionChecker.CheckExplosionRadius(cloud_center, 12, gameObject, casterImmune: true, npcsImmune: true);

        // laser coming from random point above entity in cloud, and into entity
        foreach (UnitEntity otherChar in collidedTargets)
        {
            Vector3 lightning_source = new Vector3(
                otherChar.TargetingTransform.position.x + (float)(random_.NextDouble() * 3 - 1.5),
                gameObject.transform.position.y,
                otherChar.TargetingTransform.position.z + (float)(random_.NextDouble() * 3 - 1.5)
            );
            Vector3 lightning_target = new Vector3(
                otherChar.TargetingTransform.position.x + (float)(random_.NextDouble() - 0.5),
                otherChar.TargetingTransform.position.y,
                otherChar.TargetingTransform.position.z + (float)(random_.NextDouble() - 0.5)
            );
            ClientGameLoop.CGL.LocalEntityManager.AddLocalEffect(new LaserEffect(lightning_source, lightning_target, 0.01f, Color.white));
        }
    }
}
