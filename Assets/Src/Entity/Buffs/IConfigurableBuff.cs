using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal interface IConfigurableBuff
{
    /// <summary>
    /// Configures buff with its corresponding BuffEntity parent
    /// </summary>
    /// <param name="parent">the actual Buff entity</param>
    void Config(BuffEntity parent);
}
