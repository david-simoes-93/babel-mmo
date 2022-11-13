using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal abstract class BaseAnimator
{
    protected UnitEntity parent_;
    private Animator AnimatorObj;

    protected Dictionary<EntityAnimation, string> localAnimationStrings_;

    internal protected EntityAnimation CurrentAnimatorState { get; protected set; }

    internal BaseAnimator() { }

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
    /// iterates over animations[], setting everything to false except given 'state'.
    /// this also check controller states (which have priority over movement animations) and uses those as adequate
    /// this sets currentAnimatorState, which will be propagated through the network
    /// </summary>
    /// <param name="state"></param>
    internal void SetAnimatorState(EntityAnimation state)
    {
        if (SpecificSetAnimatorState(state))
        {
            return;
        }

        if (state != CurrentAnimatorState)
        {
            GameDebug.Log("setting state " + state);
            CurrentAnimatorState = state;
            UpdateAnimator();
        }
    }

    /// <summary>
    /// Called by SetAnimatorState() to set unit-specific states
    /// </summary>
    /// <param name="state"></param>
    /// <returns>true if EntityAnimation was set correctly</returns>
    internal abstract bool SpecificSetAnimatorState(EntityAnimation state);

    /// <summary>
    /// Triggers animator with given animation
    /// </summary>
    /// <param name="trig"></param>
    internal void SetAnimatorTrigger(EntityAnimationTrigger trig)
    {
#if !UNITY_SERVER
        GameDebug.Log("setting trigger " + trig);
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
}
