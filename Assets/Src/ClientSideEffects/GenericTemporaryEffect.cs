using UnityEngine;
using UnityEditor;

/// <summary>
/// A 3D visual explosion!
/// </summary>
internal class GenericTemporaryEffect : ILocalEffect
{
    private readonly long destroyTime_;
    private GameObject myGameObject_;

    /// <summary>
    /// Places a game object at some position for some time
    /// </summary>
    /// <param name="src">position</param>
    /// <param name="ori">rotation</param>
    /// <param name="scaleModifier">scale multiplier</param>
    /// <param name="obj">prefab object</param>
    /// <param name="time_ms">how long to keep it for (in ms)</param>
    internal GenericTemporaryEffect(Vector3 src, Quaternion ori, float scaleModifier, GameObject obj, long time_ms)
    {
        myGameObject_ = Object.Instantiate(obj);
        myGameObject_.transform.position = src;
        myGameObject_.transform.rotation = ori;
        myGameObject_.transform.localScale *= scaleModifier;
        destroyTime_ = Globals.currTime_ms + time_ms;
    }

    /// <summary>
    /// Destroys the game object
    /// </summary>
    public void Destroy()
    {
        Object.Destroy(myGameObject_);
    }

    /// <summary>
    /// Destroys the game object when enough time has elapsed
    /// </summary>
    /// <returns></returns>
    public bool Update()
    {
        if (Globals.currTime_ms > destroyTime_)
        {
            return true;
        }
        return false;
    }
}
