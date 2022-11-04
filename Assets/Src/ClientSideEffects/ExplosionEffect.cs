using UnityEngine;
using UnityEditor;

/// <summary>
/// A 3D visual explosion!
/// </summary>
internal class ExplosionEffect : ILocalEffect
{
    private readonly long destroyTime_;
    private GameObject myGameObject_;

    /// <summary>
    /// Constructs an explosion at a given location with a given size
    /// </summary>
    /// <param name="src">Explosion position</param>
    /// <param name="explosionRadius">Explosion radius</param>
    internal ExplosionEffect(Vector3 src, int explosionRadius)
    {
        myGameObject_ = Object.Instantiate(Globals.kExplosionPrefab);
        myGameObject_.transform.position = src;
        myGameObject_.transform.localScale *= explosionRadius / 2;
        destroyTime_ = Globals.currTime_ms + (int)(myGameObject_.GetComponent<PseudoVolumetricExplosion>().timeScale * 1000);
    }

    /// <summary>
    /// Destroys the explosion when it has extinguished
    /// </summary>
    /// <returns></returns>
    public bool Update()
    {
        if (Globals.currTime_ms > destroyTime_)
        {
            Object.Destroy(myGameObject_);
            return true;
        }
        return false;
    }
}
