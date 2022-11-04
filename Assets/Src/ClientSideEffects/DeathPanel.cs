using UnityEngine;
using UnityEditor;

/// <summary>
/// A Panel that insults player for dying and let's him respawn
/// </summary>
internal class DeathPanel : MonoBehaviour
{
    /// <summary>
    /// Linked to a button in DeathPanel prefab. When pressed, client casts Respawn
    /// </summary>
    internal void CastRespawn()
    {
        ClientGameLoop.CGL.UnitEntity.InputManager.Cast(CastUtils.MakeRespawn(ClientGameLoop.CGL.UnitEntity.Uid, Vector3.zero, Quaternion.identity));
    }
}
