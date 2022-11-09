using UnityEngine;
using UnityEditor;

/// <summary>
/// A Laser effect, vanishing over time
/// </summary>
internal class LaserEffect : ILocalEffect
{
    private readonly float decay_;
    private GameObject myGameObject_;
    private LineRenderer line_;

    private const float kStartingWidth = 0.1f;

    /// <summary>
    /// Constructs a laser starting at a given position with a given direction
    /// </summary>
    /// <param name="src">Starting position</param>
    /// <param name="ori">Target direction</param>
    /// <param name="length">Laser length</param>
    /// <param name="decay">How fast laser decays (subtracts from kStartingWidth every frame)</param>
    internal LaserEffect(Vector3 src, Quaternion ori, float length, float decay, Color color)
    {
        decay_ = decay;

        myGameObject_ = Object.Instantiate(Globals.kLaserPrefab);
        myGameObject_.transform.SetPositionAndRotation(src, ori);
        line_ = myGameObject_.GetComponent<LineRenderer>();
        line_.startWidth = kStartingWidth;
        line_.endWidth = kStartingWidth;
        line_.startColor = color;
        line_.endColor = color;
    }

    /// <summary>
    /// Constructs a laser between two points
    /// </summary>
    /// /// <param name="src">Starting position</param>
    /// <param name="dst">Final position</param>
    /// <param name="decay">How fast laser decays (subtracts from kStartingWidth every frame)</param>
    internal LaserEffect(Vector3 src, Vector3 dst, float decay, Color color)
    {
        decay_ = decay;

        myGameObject_ = Object.Instantiate(Globals.kLaserPrefab);
        line_ = myGameObject_.GetComponent<LineRenderer>();
        line_.SetPosition(0, src);
        line_.SetPosition(1, dst);
        line_.startWidth = kStartingWidth;
        line_.endWidth = kStartingWidth;
        line_.startColor = color;
        line_.endColor = color;
    }

    /// <summary>
    /// Destroys the game object
    /// </summary>
    public void Destroy()
    {
        Object.Destroy(myGameObject_);
    }

    /// <summary>
    /// Decays laser over time and destroys it when it's gone
    /// </summary>
    /// <returns></returns>
    public bool Update()
    {
        line_.startWidth -= decay_;
        line_.endWidth -= decay_;

        if (line_.startWidth <= 0)
        {
            return true;
        }

        return false;
    }
}
