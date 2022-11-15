using UnityEngine;
using UnityEditor;

/// <summary>
/// A Fireflash that moves in Z; to be improved
/// </summary>
internal class Fireflash : ILocalEffect
{
    private GameObject myGameObject_;
    private readonly long time_to_vanish_at_;
    ParticleSystem[] myParticleSystems;
    Vector3 source2;

    /// <summary>
    /// Constructs the Fireflash
    /// </summary>
    /// <param name="pos">Starting position</param>
    /// <param name="text">The written text</param>
    /// <param name="time_to_vanish_ms">How long until it vanishes (ms)</param>
    /// <param name="color">Text color</param>
    internal Fireflash()
    {
        time_to_vanish_at_ = Globals.currTime_ms + 10000;

        myGameObject_ = Object.Instantiate(Globals.kFireflashPrefab);
        myParticleSystems = myGameObject_.GetComponentsInChildren<ParticleSystem>();
        source2 = Vector3.zero;
    }

    /// <summary>
    /// Destroys the game object
    /// </summary>
    public void Destroy()
    {
        Object.Destroy(myGameObject_);
    }

    /// <summary>
    /// Moves the Fireflash and destroys it after enough time
    /// </summary>
    /// <returns>Whether text was destroyed</returns>
    public bool Update()
    {
        source2 = new Vector3(source2.x, source2.y, source2.z - 0.5f);
        if (source2.z < -100)
            source2 = Vector3.zero;

        foreach (var ps in myParticleSystems)
        {
            var sh = ps.shape;
            sh.position = source2;
        }

        if (time_to_vanish_at_ < Globals.currTime_ms)
        {
            return true;
        }

        return false;
    }
}
