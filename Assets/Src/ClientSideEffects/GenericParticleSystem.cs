using UnityEngine;
using UnityEditor;

/// <summary>
/// A GenericParticleSystem that stops particle emission after a while and dies when particles fade
/// </summary>
internal class GenericParticleSystem : ILocalEffect
{
    private GameObject myGameObject_;
    private readonly long time_to_stop_emitting_at_;
    ParticleSystem[] myParticleSystems;

    /// <summary>
    /// Constructs the GenericParticleSystem
    /// </summary>
    /// <param name="pos">Position</param>
    /// <param name="rotation">Rotation</param>
    /// <param name="scale">Scale</param>
    /// <param name="emition_duration_">How long until it vanishes (ms)</param>
    /// <param name="particleSystem">Prefab with PS</param>
    internal GenericParticleSystem(Vector3 pos, Quaternion rotation, Vector3 scale, long emition_duration_, GameObject particleSystem)
    {
        time_to_stop_emitting_at_ = Globals.currTime_ms + emition_duration_;
        myGameObject_ = Object.Instantiate(particleSystem, pos, rotation);
        myGameObject_.transform.localScale = scale;
        myParticleSystems = myGameObject_.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in myParticleSystems)
        {
            ps.Play();
        }
    }

    /// <summary>
    /// Stops GenericParticleSystem emission when appropriate and destroys it when all particles are gone
    /// </summary>
    /// <returns>Whether text was destroyed</returns>
    public bool Update()
    {
        if (time_to_stop_emitting_at_ < Globals.currTime_ms)
        {
            int particleCount = 0;
            foreach (var ps in myParticleSystems)
            {
                ps.Stop();
                particleCount += ps.particleCount;
            }

            if (particleCount == 0)
            {
                Object.Destroy(myGameObject_);
                return true;
            }
        }

        return false;
    }
}
