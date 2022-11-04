using UnityEngine;
using UnityEngine.UI;

internal abstract class BaseCanvas : MonoBehaviour
{
    protected int currentHp_;
    protected GameObject deathPanel_,
        AlertAnchor_;
    protected UnitEntity parent_;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    internal abstract void Awake();

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal abstract void SpecificConfig(UnitEntity parent);

    /// <summary>
    /// Configures the local variables the GUI will use
    /// </summary>
    /// <param name="parent">the UnitEntity this canvas belongs to</param>
    internal void Config(UnitEntity parent)
    {
        parent_ = parent;
        currentHp_ = parent.Health;

        deathPanel_ = null;
        SetHP(currentHp_);

        SpecificConfig(parent);
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    internal abstract void Update();

    /// <summary>
    /// updates player HP on GUI
    /// </summary>
    /// <param name="hp">target HP</param>
    internal abstract void SetHP(int currentHp);

    /// <summary>
    /// Sets unit alive
    /// </summary>
    internal abstract void SetAlive();

    /// <summary>
    /// Sets unit dead
    /// </summary>
    internal abstract void SetDead();

    /// <summary>
    /// adds local graphical fx (like combat text or explosions)
    /// </summary>
    /// <param name="rd">the cause of the FX</param>
    /// <param name="target">the target's UnitEntity</param>
    internal abstract void AddCombatEffect(CombatEffectRD rd, UnitEntity target);
}
