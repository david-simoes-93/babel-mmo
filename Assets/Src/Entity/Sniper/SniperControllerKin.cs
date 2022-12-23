using UnityEngine;
using KinematicCharacterController;
using System;

internal class SniperControllerKin : BaseControllerKin
{
    private GameObject cameraTarget_;
    private float defaultCameraHeight_,
        crouchingCameraHeight_;
    private bool shouldBeCrouching_ = false;

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
            case CharacterState.Default:
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

                // Crouching input
                if (inputs.CrouchDown)
                {
                    shouldBeCrouching_ = true;
                    TransitionToState(CharacterState.SniperCrouching);
                }

                break;
            }
            case CharacterState.SniperCrouching:
            {
                // Move and look inputs
                _moveInputVector = Vector3.zero;
                _lookInputVector = cameraPlanarDirection;

                if (inputs.CrouchUp)
                {
                    shouldBeCrouching_ = false;
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
            case CharacterState.Default:
            {
                if (cameraTarget_.transform.localPosition.y < defaultCameraHeight_)
                    cameraTarget_.transform.localPosition = new Vector3(
                        cameraTarget_.transform.localPosition.x,
                        cameraTarget_.transform.localPosition.y + 0.1f,
                        cameraTarget_.transform.localPosition.z
                    );
                break;
            }
            case CharacterState.SniperCrouching:
            {
                if (cameraTarget_.transform.localPosition.y > crouchingCameraHeight_)
                    cameraTarget_.transform.localPosition = new Vector3(
                        cameraTarget_.transform.localPosition.x,
                        cameraTarget_.transform.localPosition.y - 0.1f,
                        cameraTarget_.transform.localPosition.z
                    );
                break;
            }
            case CharacterState.Dead:
            {
                if (cameraTarget_.transform.localPosition.y != defaultCameraHeight_)
                    cameraTarget_.transform.localPosition = new Vector3(
                        cameraTarget_.transform.localPosition.x,
                        defaultCameraHeight_,
                        cameraTarget_.transform.localPosition.z
                    );
                break;
            }
            default:
                return false;
        }

        return true;
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
            case CharacterState.SniperCrouching:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        // return true;
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
            case CharacterState.SniperCrouching:
            {
                // act as default
                return false;
            }
            default:
                return false;
        }

        // return true;
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
            case CharacterState.SniperCrouching:
            {
                // Handle uncrouching
                if (!shouldBeCrouching_)
                {
                    // Do an overlap test with the character's standing height to see if there are any obstructions
                    Motor.SetCapsuleDimensions(defaultCapsuleDimentions_.x, defaultCapsuleDimentions_.y, defaultCapsuleDimentions_.z);
                    //Motor.Capsule.radius
                    if (
                        Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, Motor.CollidableLayers, QueryTriggerInteraction.Ignore)
                        > 0
                    )
                    {
                        // If obstructions, just stick to crouching dimensions
                        Motor.SetCapsuleDimensions(defaultCapsuleDimentions_.x, defaultCapsuleDimentions_.y / 2, defaultCapsuleDimentions_.z / 2);
                    }
                    else
                    {
                        // If no obstructions, uncrouch
                        //MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                        TransitionToState(CharacterState.Default);
                    }
                }
                break;
            }
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called by OnMovementHit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.SniperCrouching:
            {
                break;
            }
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called by Config() for specific class
    /// </summary>
    /// <param name="parent">the controller's parent</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override void SpecificConfig(UnitEntity parent)
    {
        cameraTarget_ = transform.Find("Root").Find("CameraTarget").gameObject;
        //TODO this lets players crouch, log-off, login and now theyre uncrouched somewhere they should be crouched
        defaultCameraHeight_ = cameraTarget_.transform.localPosition.y;
        crouchingCameraHeight_ = cameraTarget_.transform.localPosition.y - 1;
    }

    /// <summary>
    /// Called by OnStateEnter() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateEnter(CharacterState state, CharacterState fromState)
    {
        switch (state)
        {
            case CharacterState.SniperCrouching:
            {
                Motor.SetCapsuleDimensions(defaultCapsuleDimentions_.x, defaultCapsuleDimentions_.y / 2, defaultCapsuleDimentions_.z / 2);
                break;
            }
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called by OnStateExit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateExit(CharacterState state, CharacterState toState)
    {
        switch (state)
        {
            case CharacterState.SniperCrouching:
            {
                break;
            }
            default:
                return false;
        }

        return true;
    }
}
