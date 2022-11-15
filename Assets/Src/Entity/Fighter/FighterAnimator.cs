﻿using static Globals;

internal class FighterAnimator : BaseAnimator
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
    /// Called by SetAnimatorState() to set unit-specific states
    /// </summary>
    /// <param name="state"></param>
    /// <returns>true if EntityAnimation was set correctly</returns>
    internal override bool SpecificSetAnimatorState(EntityAnimation state)
    {
        EntityAnimation newAnimatorState;

        CharacterState cState = parent_.Controller.CurrentCharacterState;
        if (cState == CharacterState.FighterCharging)
        {
            newAnimatorState = EntityAnimation.kRun;
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
