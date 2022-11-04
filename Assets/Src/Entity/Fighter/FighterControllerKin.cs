using UnityEngine;
using KinematicCharacterController;

// https://www.youtube.com/watch?v=XgPumFn34xA
// https://www.youtube.com/watch?v=oJft0yKweZ8 list of combos
// https://gamefaqs.gamespot.com/boards/946346-bayonetta/57542976
// https://www.youtube.com/watch?v=nBHA0PnPA4k

internal class FighterControllerKin : BaseControllerKin
{
    [Header("Charging")]
    internal float ChargeSpeed = 25f;
    internal float DodgeSpeed = 50f;
    internal float StoppedTime = 0f;

    private Vector3 currentChargeVelocity_;
    private bool isStopped_;
    private bool mustStopVelocity_ = false;
    private float timeSinceStartedCharge_ = 0;
    private float timeSinceStopped_ = 0;
    private FighterCastValidator fighterValidator_;

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
            case CharacterState.FighterCharging:
            {
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
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
            case CharacterState.FighterCharging:
            {
                // Update times
                timeSinceStartedCharge_ += deltaTime;
                if (isStopped_)
                {
                    timeSinceStopped_ += deltaTime;
                }
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
                // Update times
                timeSinceStartedCharge_ += deltaTime;
                if (isStopped_)
                {
                    timeSinceStopped_ += deltaTime;
                }
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
            case CharacterState.FighterCharging:
            {
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
                break;
            }
            default:
                return false;
        }

        return true;
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
            case CharacterState.FighterCharging:
            {
                // If we have stopped and need to cancel velocity, do it here
                if (mustStopVelocity_)
                {
                    Vector3 velWithFall = new Vector3(0, currentVelocity.y, 0);
                    currentVelocity = velWithFall;
                    mustStopVelocity_ = false;
                }

                if (isStopped_)
                {
                    // When stopped, do no velocity handling except gravity
                    currentVelocity += Gravity * deltaTime;
                }
                else
                {
                    // When charging, velocity is always constant
                    currentVelocity = currentChargeVelocity_ + Gravity * deltaTime;
                }
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
                // If we have stopped and need to cancel velocity, do it here
                if (mustStopVelocity_)
                {
                    currentVelocity = Vector3.zero;
                    mustStopVelocity_ = false;
                }

                if (isStopped_)
                {
                    // When stopped, do no velocity handling except gravity
                    currentVelocity += Gravity * deltaTime;
                }
                else
                {
                    // When charging, velocity is always constant
                    currentVelocity = currentChargeVelocity_;
                }
                break;
            }
            default:
                return false;
        }

        return true;
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
            case CharacterState.FighterCharging:
            {
                // Detect being stopped by elapsed time
                if (!isStopped_ && timeSinceStartedCharge_ > FighterCastValidator.kChargeLength_s)
                {
                    mustStopVelocity_ = true;
                    isStopped_ = true;
                }

                // Detect end of stopping phase and transition back to default movement state
                if (timeSinceStopped_ > StoppedTime)
                {
                    TransitionToState(CharacterState.Default);
                }
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
                // Detect being stopped by elapsed time
                if (!isStopped_ && timeSinceStartedCharge_ > FighterCastValidator.kDodgeLength_s)
                {
                    mustStopVelocity_ = true;
                    isStopped_ = true;
                }

                // Detect end of stopping phase and transition back to default movement state
                if (timeSinceStopped_ > StoppedTime)
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
    /// Called by OnMovementHit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.FighterCharging:
            {
                // Detect being stopped by obstructions
                if (!isStopped_ && !hitStabilityReport.IsStable && Vector3.Dot(-hitNormal, currentChargeVelocity_.normalized) > 0.5f)
                {
                    mustStopVelocity_ = true;
                    isStopped_ = true;
                }
#if !UNITY_SERVER
                ClientChargeStun(hitCollider);
#else

#endif
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
                // Detect being stopped by obstructions
                if (!isStopped_ && !hitStabilityReport.IsStable && Vector3.Dot(-hitNormal, currentChargeVelocity_.normalized) > 0.5f)
                {
                    mustStopVelocity_ = true;
                    isStopped_ = true;
                }
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
        fighterValidator_ = (FighterCastValidator)parent.Validator;
    }

    /// <summary>
    /// Called by OnStateEnter() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateEnter(CharacterState state, CharacterState fromState)
    {
        switch (state)
        {
            case CharacterState.FighterCharging:
            {
                currentChargeVelocity_ = Motor.CharacterForward * ChargeSpeed;
                isStopped_ = false;
                timeSinceStartedCharge_ = 0f;
                timeSinceStopped_ = 0f;
                break;
            }
            case CharacterState.FighterDodgingBack:
            {
                parent_.SetInvulnerable(true);
                currentChargeVelocity_ = -Motor.CharacterForward * DodgeSpeed;
                isStopped_ = false;
                timeSinceStartedCharge_ = 0f;
                timeSinceStopped_ = 0f;
                break;
            }
            case CharacterState.FighterDodgingFront:
            {
                parent_.SetInvulnerable(true);
                currentChargeVelocity_ = Motor.CharacterForward * DodgeSpeed;
                isStopped_ = false;
                timeSinceStartedCharge_ = 0f;
                timeSinceStopped_ = 0f;
                break;
            }
            case CharacterState.FighterDodgingLeft:
            {
                parent_.SetInvulnerable(true);
                currentChargeVelocity_ = -Motor.CharacterRight * DodgeSpeed;
                isStopped_ = false;
                timeSinceStartedCharge_ = 0f;
                timeSinceStopped_ = 0f;
                break;
            }

            case CharacterState.FighterDodgingRight:
            {
                parent_.SetInvulnerable(true);
                currentChargeVelocity_ = Motor.CharacterRight * DodgeSpeed;
                isStopped_ = false;
                timeSinceStartedCharge_ = 0f;
                timeSinceStopped_ = 0f;
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
            case CharacterState.FighterCharging:
            {
                break;
            }
            case CharacterState.FighterDodgingBack:
            case CharacterState.FighterDodgingFront:
            case CharacterState.FighterDodgingLeft:
            case CharacterState.FighterDodgingRight:
            {
                parent_.SetInvulnerable(false);
                break;
            }
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// When the Fighter Charge collides with something, cast a ChargeStun if it was a unit
    /// </summary>
    /// <param name="hitCollider">charge collision</param>
    private void ClientChargeStun(Collider hitCollider)
    {
        if (parent_.Uid != ClientGameLoop.CGL.UnitEntity.Uid)
            return;

        // if fighter hit an attackable unit, stun it
        BaseControllerKin controller = hitCollider.gameObject.GetComponent<BaseControllerKin>();
        if (controller != null)
        {
            UnitEntity target = controller.Parent();
            if (target.IsAttackable)
            {
                parent_.InputManager.Cast(CastUtils.MakeChargeStun(parent_.Uid, target.Uid));
            }
        }
    }
}
