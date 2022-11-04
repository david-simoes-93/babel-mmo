using UnityEngine;
using System.Collections;

internal interface IConfigurableMonster
{
    /// <summary>
    /// Configures Monster with its corresponding unit entity
    /// </summary>
    /// <param name="parent"></param>
    void Config(UnitEntity parent);
}
