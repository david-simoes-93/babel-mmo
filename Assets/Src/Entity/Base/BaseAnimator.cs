using UnityEngine;
using System.Collections.Generic;
using static Globals;

/// <summary>
/// An abstract component to control animations for a UnitEntity
/// </summary>
internal abstract class BaseAnimator
{
    protected UnitEntity parent_;
    private Animator AnimatorObj;

    protected Dictionary<EntityAnimation, string> localAnimationStrings_;

    internal protected EntityAnimation CurrentAnimatorState { get; protected set; }

    internal BaseAnimator() { }

    /// <summary>
    /// Configures the local variables the animator will use
    /// </summary>
    /// <param name="parent">the UnitEntity this animator belongs to</param>
    internal void Config(UnitEntity parent)
    {
        parent_ = parent;
        AnimatorObj = parent.GameObject.GetComponentInChildren<Animator>();
        CurrentAnimatorState = EntityAnimation.kIdle;

        SpecificConfig(parent);
    }

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal abstract void SpecificConfig(UnitEntity parent);

    /// <summary>
    /// checks unit-specific controller states (which have priority over movement animations) and uses those as adequate.
    /// otherwise, sets currentAnimatorState, which will be propagated through the network
    /// </summary>
    /// <param name="state"></param>
    internal void SetAnimatorState(EntityAnimation state)
    {
        if (SpecificSetAnimatorState())
        {
            return;
        }
        if (state != CurrentAnimatorState)
        {
            CurrentAnimatorState = state;
            UpdateAnimator();
        }
    }

    /// <summary>
    /// Called by SetAnimatorState() to set unit-specific states, based on controller state
    /// </summary>
    /// <returns>true if EntityAnimation was set correctly</returns>
    internal abstract bool SpecificSetAnimatorState();

    /// <summary>
    /// Triggers animator with given animation
    /// </summary>
    /// <param name="trig"></param>
    internal void SetAnimatorTrigger(EntityAnimationTrigger trig)
    {
#if !UNITY_SERVER
        AnimatorObj.SetTrigger(AnimationTriggerStrings[trig]);
#endif
    }

    /// <summary>
    /// Disables all Animator states except for CurrentAnimatorState
    /// </summary>
    protected void UpdateAnimator()
    {
#if !UNITY_SERVER
        foreach (KeyValuePair<EntityAnimation, string> entry in localAnimationStrings_)
        {
            AnimatorObj.SetBool(entry.Value, false);
        }
        AnimatorObj.SetBool(localAnimationStrings_[CurrentAnimatorState], true);
#endif
    }

    internal void setBasicMovementAnimationState(bool moveFront, bool moveLeft, bool moveRight, bool moveBack)
    {
        if (moveFront)
        {
            SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else if (moveLeft)
        {
            SetAnimatorState(EntityAnimation.kWalkLeft);
        }
        else if (moveRight)
        {
            SetAnimatorState(EntityAnimation.kWalkRight);
        }
        else if (moveBack)
        {
            SetAnimatorState(EntityAnimation.kWalkBack);
        }
        else
        {
            SetAnimatorState(EntityAnimation.kIdle);
        }
    }
}
