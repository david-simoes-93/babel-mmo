using static Globals;

internal class MageAnimator : BaseAnimator
{
    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal override void SpecificConfig(UnitEntity parent)
    {
        localAnimationStrings_ = Globals.MageAnimationStrings;
    }

    /// <summary>
    /// Called by SetAnimatorState() to set unit-specific states
    /// </summary>
    /// <param name="state"></param>
    /// <returns>true if EntityAnimation was set correctly</returns>
    internal override bool SpecificSetAnimatorState(EntityAnimation state)
    {
        EntityAnimation newAnimatorState;

        CharacterState cState = parent_.Controller.CurrentCharacterState;
        if (cState == CharacterState.MageChanneling)
        {
            newAnimatorState = EntityAnimation.kMageChanneling;
        }
        else
        {
            return false;
        }

        if (newAnimatorState != CurrentAnimatorState)
        {
            CurrentAnimatorState = newAnimatorState;
            UpdateAnimator();
        }
        return true;
    }
}
