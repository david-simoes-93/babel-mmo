using System;
using UnityEngine;

/// <summary>
/// Describes a temporary entity to be spawned by server on level load
/// </summary>
internal class MapEntityDescriptor : MonoBehaviour
{
    public Globals.UnitEntityCode type = Globals.UnitEntityCode.kEmpty;
    public int health = 0;
    public string unitName = null;
}
