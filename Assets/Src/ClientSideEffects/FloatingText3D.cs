using UnityEngine;
using UnityEditor;

/// <summary>
/// A 3D floating text, always facing the player's camera, floating upwards
/// </summary>
internal class FloatingText3D : ILocalEffect
{
    private GameObject myGameObject_;
    private readonly long time_to_vanish_at_;

    /// <summary>
    /// Constructs the Text
    /// </summary>
    /// <param name="pos">Starting position</param>
    /// <param name="text">The written text</param>
    /// <param name="time_to_vanish_ms">How long until it vanishes (ms)</param>
    /// <param name="color">Text color</param>
    internal FloatingText3D(Vector3 pos, string text, long time_to_vanish_ms, Color color)
    {
        time_to_vanish_at_ = Globals.currTime_ms + time_to_vanish_ms;

        Quaternion ori = Quaternion.LookRotation(pos - ClientGameLoop.CGL.UnitEntity.CameraTransform().position);

        myGameObject_ = Object.Instantiate(Globals.kCombatTextPrefab);

        myGameObject_.transform.position = pos;
        myGameObject_.transform.rotation = ori;

        Vector3 rotationVector = ClientGameLoop.CGL.UnitEntity.CameraTransform().position - pos;
        float scale = rotationVector.magnitude / 100;
        myGameObject_.transform.localScale = new Vector3(scale, scale, scale);

        TextMesh txt = myGameObject_.GetComponent<TextMesh>();
        txt.text = text;
        txt.color = color;
    }

    /// <summary>
    /// Floats the text upwards, faces it to camera, and destroys it when it times out
    /// </summary>
    /// <returns>Whether text was destroyed</returns>
    public bool Update()
    {
        Vector3 newPos = myGameObject_.transform.position + new Vector3(0, 0.01f, 0);
        Quaternion newRotation = Quaternion.LookRotation(newPos - ClientGameLoop.CGL.UnitEntity.CameraTransform().position);
        myGameObject_.transform.position = newPos;
        myGameObject_.transform.rotation = newRotation;

        if (time_to_vanish_at_ < Globals.currTime_ms)
        {
            Object.Destroy(myGameObject_);
            return true;
        }

        return false;
    }
}
