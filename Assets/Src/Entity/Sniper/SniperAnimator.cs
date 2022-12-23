using static Globals;

internal class SniperAnimator : BaseAnimator
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
        EntityAnimation newAnimatorState;

        CharacterState cState = parent_.Controller.CurrentCharacterState;
        if (cState == CharacterState.SniperCrouching)
        {
            newAnimatorState = EntityAnimation.kCrouch;
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
