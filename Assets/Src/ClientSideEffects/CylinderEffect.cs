using UnityEngine;
using UnityEditor;

/// <summary>
/// A Cylinder effect, vanishing over time
/// </summary>
internal class CylinderEffect : ILocalEffect
{
    private readonly float decay_;
    private GameObject myGameObject_;
    private Transform cylGameObject;

    /// <summary>
    /// Constructs a Cylinder starting at a given position with a given direction
    /// </summary>
    /// <param name="src">Starting position</param>
    /// <param name="ori">Target direction</param>
    /// <param name="length">Cylinder length</param>
    /// <param name="decay">How fast Cylinder decays (subtracts from kStartingWidth every frame)</param>
    internal CylinderEffect(Vector3 src, Quaternion ori, float length, float decay)
    {
        decay_ = decay;

        myGameObject_ = Object.Instantiate(Globals.kCylinderPrefab);
        myGameObject_.transform.SetPositionAndRotation(src, ori);
        cylGameObject = myGameObject_.transform.Find("Cylinder").gameObject.transform;
        cylGameObject.transform.localPosition = new Vector3(0, 0, length / 2);
        cylGameObject.localScale = new Vector3(cylGameObject.localScale.x, length / 2, cylGameObject.localScale.z);
    }

    /// <summary>
    /// Destroys the game object
    /// </summary>
    public void Destroy()
    {
        Object.Destroy(myGameObject_);
    }

    /// <summary>
    /// Decays Cylinder over time and destroys it when it's gone
    /// </summary>
    /// <returns></returns>
    public bool Update()
    {
        Vector3 scale = cylGameObject.localScale;
        cylGameObject.localScale = new Vector3(scale.x - decay_, scale.y, scale.z - decay_);

        if (cylGameObject.localScale.x <= 0)
        {
            return true;
        }

        return false;
    }
}
