using UnityEngine;
using KinematicCharacterController;

internal class MageControllerKin : BaseControllerKin
{
    private MageCastValidator mageValidator_;

    /// <summary>
    /// Called by SetInputs() for specific class
    /// </summary>
    /// <param name="inputs">the player's inputs</param>
    /// <param name="moveInputVector">entity's movement based on input</param>
    /// <param name="cameraPlanarDirection">entity's camera direction</param>
    /// <param name="cameraPlanarRotation">entity's camera rotation, derived from its direction</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificSetInputsBasedOnState(
        ref PlayerCharacterInputs inputs,
        ref Vector3 moveInputVector,
        ref Vector3 cameraPlanarDirection,
        ref Quaternion cameraPlanarRotation
    )
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // Move and look inputs
                _moveInputVector = cameraPlanarRotation * moveInputVector;
                _lookInputVector = cameraPlanarDirection;

                // Jumping input
                if (inputs.JumpDown)
                {
                    _timeSinceJumpRequested = 0f;
                    _jumpRequested = true;
                }

                if (_moveInputVector.sqrMagnitude != 0 || _jumpRequested)
                {
                    TransitionToState(CharacterState.Default);
                }
                break;
            }
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called by BeforeCharacterUpdate() for specific class
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificBeforeUpdate(float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        //return true;
    }

    /// <summary>
    /// Called by UpdateRotation() for specific class
    /// </summary>
    /// <param name="currentRotation">target rotation</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificUpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        //return true;
    }

    /// <summary>
    /// Called by UpdateVelocity() for specific class
    /// </summary>
    /// <param name="currentVelocity">target velocity</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificUpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        //return true;
    }

    /// <summary>
    /// Called by AfterCharacterUpdate() for specific class
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificAfterUpdate(float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        //return true;
    }

    /// <summary>
    /// Called by OnMovementHit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        //return true;
    }

    /// <summary>
    /// Called by Config() for specific class
    /// </summary>
    /// <param name="parent">the controller's parent</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override void SpecificConfig(UnitEntity parent)
    {
        mageValidator_ = (MageCastValidator)parent.Validator;
    }

    /// <summary>
    /// Called by OnStateEnter() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateEnter(CharacterState state, CharacterState fromState)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }
        //return true;
    }

    /// <summary>
    /// Called by OnStateExit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateExit(CharacterState state, CharacterState toState)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.MageChanneling:
            {
                if (mageValidator_.IsChannelingSpell())
                {
#if !UNITY_SERVER
                    ClientStopCast(toState);
#else
                        ServerStopCast(toState);
#endif
                    parent_.SetAnimatorTrigger(Globals.EntityAnimationTrigger.kMageChannelFailed);
                }
                break;
            }
            default:
                return false;
        }
        return true;
    }

    /// <summary>
    /// When the Mage stops channeling something. Client needs to cast this when it moves, which breaks casting (because we trust clients in regards to movement).
    /// </summary>
    /// <param name="hitCollider">charge collision</param>
    private void ClientStopCast(CharacterState toState)
    {
        if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
        {
            return;
        }

        // We need this check here under the assumption that client only needs to CastStop when moving (transiting to Default), while server will only CastStop when
        //  transiting to other states (Stunned, Leashed, Dead). Without this check, we have repeated CastStops! Example:
        //  - When anything (e.g., death) breaks channeling, server publishes its own CastStop.
        //  - Client then gets the cause (Death) and publishes its own CastStop.
        //  - Server's CastStop comes after, everything is fine so far.
        //  - Server then receives the client's CastStop, which by then is invalid.
        if (toState != CharacterState.Default)
        {
            return;
        }

        parent_.InputManager.Cast(CastUtils.MakeMageCastStop(parent_.Uid));
    }

    /// <summary>
    /// When the Mage stops channeling something
    /// </summary>
    /// <param name="hitCollider">charge collision</param>
    private void ServerStopCast(CharacterState toState)
    {
        // see ClientStopCast
        if (toState == CharacterState.Default)
        {
            GameDebug.Log("Publishing a CastStop while transiting to Default. Client will probably publish its own invalid CastStop.");
            // If this does happen, should be investigated and a better way to avoid CastStops should be found
        }

        parent_.EntityManager.AsyncCreateTempEvent(CastUtils.MakeMageCastStop(parent_.Uid));
    }
}
