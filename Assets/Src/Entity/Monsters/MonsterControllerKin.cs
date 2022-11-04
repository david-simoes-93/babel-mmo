using UnityEngine;
using KinematicCharacterController;

internal class MonsterControllerKin : BaseControllerKin
{
    /// <summary>
    /// Called by AfterCharacterUpdate() for specific class
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificAfterUpdate(float deltaTime)
    {
        return false;
    }

    /// <summary>
    /// Called by BeforeCharacterUpdate() for specific class
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificBeforeUpdate(float deltaTime)
    {
        return false;
    }

    /// <summary>
    /// Called by Config() for specific class
    /// </summary>
    /// <param name="parent">the controller's parent</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override void SpecificConfig(UnitEntity parent) { }

    /// <summary>
    /// Called by OnMovementHit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        return false;
    }

    /// <summary>
    /// Called by OnStateEnter() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateEnter(CharacterState state, CharacterState fromState)
    {
        return false;
    }

    /// <summary>
    /// Called by OnStateExit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificOnStateExit(CharacterState state, CharacterState toState)
    {
        return false;
    }

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
        return false;
    }

    /// <summary>
    /// Called by UpdateRotation() for specific class
    /// </summary>
    /// <param name="currentRotation">target rotation</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificUpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        return false;
    }

    /// <summary>
    /// Called by UpdateVelocity() for specific class
    /// </summary>
    /// <param name="currentVelocity">target velocity</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal override bool SpecificUpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        return false;
    }

    public static AICharacterInputs kBeStill = new AICharacterInputs { MoveVector = new Vector3(0, 0, 0), LookVector = new Vector3(0, 0, -1) };
}
