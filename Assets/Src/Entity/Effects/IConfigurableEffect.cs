using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal interface IConfigurableEffect
{
    /// <summary>
    /// Configures effect with its corresponding EffectEntity parent
    /// </summary>
    /// <param name="parent"></param>
    void Config(EffectEntity parent);
}
