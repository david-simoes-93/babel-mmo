using static Globals;

internal class MonsterAnimator : BaseAnimator
{
    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        localAnimationStrings_ = Globals.AnimationStrings;
    }

    /// <summary>
    /// Called by SetAnimatorState() to set unit-specific states, based on controller state
    /// </summary>
    /// <returns>true if EntityAnimation was set correctly</returns>
    internal override bool SpecificSetAnimatorState()
    {
        return false;
    }
}
