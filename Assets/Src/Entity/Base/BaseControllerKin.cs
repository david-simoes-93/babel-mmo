using UnityEngine;
using KinematicCharacterController;
using System.Collections.Generic;
using static Globals;
using System;

internal enum CharacterState
{
    Default,
    FighterCharging,
    FighterDodgingBack,
    FighterDodgingFront,
    FighterDodgingLeft,
    FighterDodgingRight,
    SniperCrouching,
    Dead,
    Stunned,
    Leashed,
    MageChanneling
}

internal struct AICharacterInputs
{
    internal Vector3 MoveVector;
    internal Vector3 LookVector;
    internal bool Jump;
}

internal struct PlayerCharacterInputs
{
    internal float MoveAxisForward;
    internal float MoveAxisRight;
    internal Quaternion CameraRotation;
    internal bool JumpDown;
    internal bool CrouchDown;
    internal bool CrouchUp;
    //internal bool ChargingDown;
    //internal bool AttackL, AttackR, Spin;
    //internal bool DodgeL, DodgeR, DodgeF, DodgeB;
}

/// <summary>
/// An abstract component to control the body for a UnitEntity
/// </summary>
internal abstract class BaseControllerKin : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor = null;
    internal CharacterState CurrentCharacterState { get; private set; }

    [Header("Stable Movement")]
    public float MaxStableMoveSpeed = 10f;
    public float StableMovementSharpness = 15f;
    public float OrientationSharpness = 10f; // TODO if we increase this (in editor), char turns faster, but also jitters

    [Header("Air Movement")]
    public float MaxAirMoveSpeed = 100f;
    public float AirAccelerationSpeed = 15f;
    public float Drag = 0.1f;
    public bool Flyer = false;

    [Header("Jumping")]
    public bool AllowJumpingWhenSliding = false;
    public float JumpUpSpeed = 10f;
    public float JumpScalableForwardSpeed = 1f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;
    public bool DoubleJumpEnabled = false;

    [Header("Misc")]
    public List<Collider> IgnoredColliders = new List<Collider>();
    public bool OrientTowardsGravity = false;
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public Transform MeshRoot = null;
    public Transform CameraFollowPoint = null;
    public Transform TargetingPoint = null;

    internal Collider[] _probedColliders = new Collider[8];
    internal Vector3 _moveInputVector;
    internal Vector3 _lookInputVector;
    internal bool _jumpRequested = false;
    internal bool _doubleJumpConsumed = false;
    internal bool _jumpConsumed = false;
    internal bool _jumpedThisFrame = false;
    internal float _timeSinceJumpRequested = Mathf.Infinity;
    internal float _timeSinceLastAbleToJump = 0f;
    internal Vector3 _internalVelocityAdd = Vector3.zero;

    protected Vector3 lastInnerNormal = Vector3.zero;
    protected Vector3 lastOuterNormal = Vector3.zero;

    protected UnitEntity parent_;
    protected Vector3 defaultCapsuleDimentions_;
    protected BaseCastValidator baseValidator_;
    protected EntityManager em_;
    protected float _moveSpeedModifier = 0.0f; // [-1, +inf]

    /// <summary>
    /// Called by SetInputs() for specific class
    /// </summary>
    /// <param name="inputs">the player's inputs</param>
    /// <param name="moveInputVector">entity's movement based on input</param>
    /// <param name="cameraPlanarDirection">entity's camera direction</param>
    /// <param name="cameraPlanarRotation">entity's camera rotation, derived from its direction</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificSetInputsBasedOnState(
        ref PlayerCharacterInputs inputs,
        ref Vector3 moveInputVector,
        ref Vector3 cameraPlanarDirection,
        ref Quaternion cameraPlanarRotation
    );

    /// <summary>
    /// Called by BeforeCharacterUpdate() for specific class
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificBeforeUpdate(float deltaTime);

    /// <summary>
    /// Called by UpdateRotation() for specific class
    /// </summary>
    /// <param name="currentRotation">target rotation</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificUpdateRotation(ref Quaternion currentRotation, float deltaTime);

    /// <summary>
    /// Called by UpdateVelocity() for specific class
    /// </summary>
    /// <param name="currentVelocity">target velocity</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificUpdateVelocity(ref Vector3 currentVelocity, float deltaTime);

    /// <summary>
    /// Called by AfterCharacterUpdate() for specific class
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificAfterUpdate(float deltaTime);

    /// <summary>
    /// Called by OnMovementHit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificOnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);

    /// <summary>
    /// Called by Config() for specific class
    /// </summary>
    /// <param name="parent">the controller's parent</param>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract void SpecificConfig(UnitEntity parent);

    /// <summary>
    /// Called by OnStateEnter() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificOnStateEnter(CharacterState state, CharacterState fromState);

    /// <summary>
    /// Called by OnStateExit() for specific class
    /// </summary>
    /// <returns>true if specific class handled state, false if general behavior is required</returns>
    internal abstract bool SpecificOnStateExit(CharacterState state, CharacterState toState);

    /// <summary>
    /// Configures the local variables the controller will use
    /// </summary>
    /// <param name="parent">the controller's parent</param>
    internal void Config(UnitEntity parent)
    {
        parent_ = parent;
        baseValidator_ = parent.Validator;
        em_ = parent.EntityManager;

        SpecificConfig(parent);
    }

    /// <summary>
    /// This is called every frame by MyPlayer in order to tell the character what its inputs are
    /// </summary>
    /// <param name="inputs">the player's inputs</param>
    internal void SetInputs(ref PlayerCharacterInputs inputs)
    {
        // Clamp input
        Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

        if (SpecificSetInputsBasedOnState(ref inputs, ref moveInputVector, ref cameraPlanarDirection, ref cameraPlanarRotation))
        {
            return;
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            default:
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
                break;
            }
            case CharacterState.Dead:
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            {
                _moveInputVector = Vector3.zero;
                _lookInputVector = cameraPlanarDirection;
                break;
            }
        }
    }

    /// <summary>
    /// This is called every frame by the AI script in order to tell the character what its inputs are
    /// </summary>
    /// <param name="inputs">the NPC's inputs</param>
    internal void SetInputs(ref AICharacterInputs inputs)
    {
        _moveInputVector = inputs.MoveVector;
        _lookInputVector = inputs.LookVector;

        _lookInputVector.y = 0;

        // Jumping input
        if (inputs.Jump)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the motor does anything
    /// </summary>
    /// <param name="deltaTime">time elapsed</param>
    public void BeforeCharacterUpdate(float deltaTime)
    {
        if (baseValidator_ != null)
        {
            baseValidator_.ProcessServerAcks();

            // If unit fallen into infinity, kill it and teleport it
#if !UNITY_SERVER
            // player only updates himself
            ClientOutOfBoundsIfSelf();
#else
            // server only updates NPCs
            ServerOutOfBoundsIfNpc();
#endif
        }

        if (SpecificBeforeUpdate(deltaTime))
        {
            return;
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            case CharacterState.Dead:
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            default:
            {
                break;
            }
        }
    }

    /// <summary>
    /// Client-side. Checks if Entity is the player and casts OutOfBoundsTeleport if so
    /// </summary>
    private void ClientOutOfBoundsIfSelf()
    {
        if (parent_.Uid == ClientGameLoop.CGL.UnitEntity.Uid && baseValidator_.CanOutOfBoundsTeleport())
        {
            CastRD oobTeleport = CastUtils.MakeOutOfBoundsTeleport(parent_.Uid, Motor.TransientPosition, Motor.TransientRotation);
            ClientGameLoop.CGL.NetworkClient.AddEvent(oobTeleport);
        }
    }

    /// <summary>
    /// Server-side. Checks if Entity is NPC and casts OutOfBoundsTeleport if so
    /// </summary>
    private void ServerOutOfBoundsIfNpc()
    {
        if (parent_.Uid < 0 && baseValidator_.CanOutOfBoundsTeleport())
        {
            CastRD oobTeleport = CastUtils.MakeOutOfBoundsTeleport(parent_.Uid, Motor.TransientPosition, Motor.TransientRotation);
            em_.AsyncCreateTempEvent(oobTeleport);
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now.
    /// This is the ONLY place where you should set the character's rotation
    /// This is called when the motor wants to know what its rotation should be right now
    /// </summary>
    /// <param name="currentRotation">target rotation</param>
    /// <param name="deltaTime">time elapsed</param>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (SpecificUpdateRotation(ref currentRotation, deltaTime))
        {
            return;
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            default:
            {
                if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                {
                    // Smoothly interpolate from current to target look direction
                    Vector3 smoothedLookInputDirection = Vector3
                        .Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime))
                        .normalized;

                    // Set the current rotation (which will be used by the KinematicCharacterMotor)
                    currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                }
                if (OrientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
                }
                break;
            }
            case CharacterState.Dead:
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            {
                if (OrientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
                }
                break;
            }
        }
    }

    /// <summary>
    /// Sets entity's velocity based on being grounded or airborne
    /// </summary>
    /// <param name="currentVelocity">entity's current velocity</param>
    /// <param name="deltaTime">elapsed time</param>
    /// <param name="MoveSpeed">entity's movement speed</param>
    protected void ConductGroundAirMovement(ref Vector3 currentVelocity, float deltaTime, float MoveSpeed)
    {
        // Ground movement
        if (Flyer)
        {
            Vector3 targetVelocity = Vector3.zero;

            // Add move input
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                targetVelocity = _moveInputVector * MoveSpeed;
            }

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        else if (Motor.GroundingStatus.IsStableOnGround)
        {
            float currentVelocityMagnitude = currentVelocity.magnitude;

            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
            if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
            {
                // Take the normal from where we're coming from
                Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
                if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
                {
                    effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
                }
                else
                {
                    effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
                }
            }

            // Reorient velocity on slope
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            Vector3 targetMovementVelocity = reorientedInput * MoveSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        // Air movement
        else
        {
            // Add move input
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

                // Prevent air movement from making you move up steep sloped walls
                if (Motor.GroundingStatus.FoundAnyGround)
                {
                    Vector3 perpenticularObstructionNormal = Vector3
                        .Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp)
                        .normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                }

                // Limit air movement from inputs to a certain maximum, without limiting the total air move speed from momentum, gravity or other forces
                Vector3 resultingVelOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp);
                if (resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed && Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= 0f)
                {
                    addedVelocity = Vector3.zero;
                }
                else
                {
                    Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                    Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed);
                    addedVelocity = clampedResultingVelOnInputsPlane - velOnInputsPlane;
                }

                currentVelocity += addedVelocity;
            }

            // Gravity
            currentVelocity += Gravity * deltaTime;

            // Drag
            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now.
    /// This is the ONLY place where you can set the character's velocity
    /// This is called when the motor wants to know what its velocity should be right now
    /// </summary>
    /// <param name="currentVelocity">target velocity</param>
    /// <param name="deltaTime">elapsed time</param>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (SpecificUpdateVelocity(ref currentVelocity, deltaTime))
        {
            return;
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            default:
            {
                // speed modifier is a value [0, +inf] multiplied by character's regular speed
                float speedModifier = 1 + (float)Math.Max(-1.0, _moveSpeedModifier);
                ConductGroundAirMovement(ref currentVelocity, deltaTime, MaxStableMoveSpeed * speedModifier);

                // Handle jumping
                _jumpedThisFrame = false;
                _timeSinceJumpRequested += deltaTime;
                if (_jumpRequested)
                {
                    // See if we actually are allowed to jump
                    if (
                        !_jumpConsumed
                        && (
                            (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime
                        )
                    )
                    {
                        // Calculate jump direction before ungrounding
                        Vector3 jumpDirection = Motor.CharacterUp;
                        if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                        {
                            jumpDirection = Motor.GroundingStatus.GroundNormal;
                        }

                        // Makes the character skip ground probing/snapping on its next update.
                        // If this line weren't here, the character would remain snapped to the ground when trying to jump
                        Motor.ForceUnground();

                        // Add to the return velocity and reset jump state
                        currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);

                        // prevent people from moving faster when jumping
                        //currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                        _jumpRequested = false;
                        _jumpConsumed = true;
                        _jumpedThisFrame = true;
                    }
                    // See if we actually are allowed to jump
                    else if (DoubleJumpEnabled && !_doubleJumpConsumed && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        // Calculate jump direction before ungrounding
                        Vector3 jumpDirection = Motor.CharacterUp;

                        // Add to the return velocity and reset jump state
                        currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);

                        _jumpRequested = false;
                        _doubleJumpConsumed = true;
                        _jumpedThisFrame = true;
                    }
                }

                // Take into account additive velocity
                if (_internalVelocityAdd.sqrMagnitude > 0f)
                {
                    currentVelocity += _internalVelocityAdd;
                    Motor.ForceUnground();
                    _internalVelocityAdd = Vector3.zero;
                }
                break;
            }
            case CharacterState.Dead:
            case CharacterState.Stunned:
            {
                ConductGroundAirMovement(ref currentVelocity, deltaTime, 0);

                // unable to jump

                // Take into account additive velocity
                if (_internalVelocityAdd.sqrMagnitude > 0f)
                {
                    currentVelocity += _internalVelocityAdd;
                    _internalVelocityAdd = Vector3.zero;
                }
                break;
            }
            case CharacterState.Leashed:
            {
                currentVelocity = Vector3.zero;
                _internalVelocityAdd = Vector3.zero;
                break;
            }
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the motor has finished everything in its update
    /// </summary>
    /// <param name="deltaTime">elapsed time</param>
    public void AfterCharacterUpdate(float deltaTime)
    {
        if (SpecificAfterUpdate(deltaTime))
        {
            return;
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            default:
            {
                // Handle jump-related values
                {
                    // Handle jumping pre-ground grace period
                    if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                    {
                        _jumpRequested = false;
                    }

                    if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                    {
                        // If we're on a ground surface, reset jumping values
                        if (!_jumpedThisFrame)
                        {
                            _jumpConsumed = false;
                            _doubleJumpConsumed = false;
                        }
                        _timeSinceLastAbleToJump = 0f;
                    }
                    else
                    {
                        // Keep track of time since we were last able to jump (for grace period)
                        _timeSinceLastAbleToJump += deltaTime;
                    }
                }
                break;
            }
            case CharacterState.Dead:
            case CharacterState.Stunned:
            {
                break;
            }
            case CharacterState.Leashed:
            {
                if (parent_.LeashedBy.GameObject != null)
                    SetMotorPose(parent_.LeashedBy.UnitTransform().position + parent_.LeashedVector, Vector3.zero, parent_.UnitTransform().rotation);
                break;
            }
        }
    }

    /// <summary>
    /// This is called after when the motor wants to know if the collider can be collided with (or if we just go through it)
    /// </summary>
    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (IgnoredColliders.Count == 0)
        {
            return true;
        }

        if (IgnoredColliders.Contains(coll))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Called when character stops being airborne and touches ground
    /// </summary>
    protected void OnLanded() { }

    /// <summary>
    /// Called when character leaves ground and becomes airborne
    /// </summary>
    protected void OnLeaveStableGround() { }

    /// <summary>
    /// This is called when the character detects discrete collisions (collisions that don't result from the motor's capsuleCasts when moving)
    /// </summary>
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    /// <summary>
    /// This is called when the motor's ground probing detects a ground hit
    /// </summary>
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    /// <summary>
    /// This is called when the motor's movement logic detects a hit
    /// </summary>
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        if (SpecificOnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport))
        {
            return;
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            default:
            {
                // We can wall jump only if we are not stable on ground and are moving against an obstruction
                /*if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
                {
                    _canWallJump = true;
                    _wallJumpNormal = hitNormal;
                }*/
                break;
            }
            case CharacterState.Dead:
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            {
                break;
            }
        }
    }

    /// <summary>
    /// This is called after the motor has finished its ground probing, but before PhysicsMover/Velocity/etc.... handling
    /// </summary>
    public void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    /// <summary>
    /// This is called after every move hit, to give you an opportunity to modify the HitStabilityReport to your liking
    /// </summary>
    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport
    ) { }

    /// <summary>
    /// Called when script is loaded
    /// </summary>
    void Awake()
    {
        defaultCapsuleDimentions_ = new Vector3(Motor.Capsule.radius, Motor.Capsule.height, Motor.Capsule.height / 2);

        // Handle initial state
        TransitionToState(CharacterState.Default);

        // Assign the characterController to the motor
        Motor.CharacterController = this;
    }

    /// <summary>
    /// Returns the parent UnitEntity of this controller
    /// </summary>
    /// <returns>the entity</returns>
    internal UnitEntity Parent()
    {
        return parent_;
    }

    /// <summary>
    /// Called before first frame update
    /// </summary>
    void Start() { }

    /// <summary>
    /// Sets the unit's current state (dead, alive, crouching, ..)
    /// Handles movement state transitions and enter/exit callbacks
    /// </summary>
    /// <param name="state">target state</param>
    internal void TransitionToState(CharacterState newState)
    {
        if (CurrentCharacterState == newState)
        {
            return;
        }
        CharacterState tmpInitialState = CurrentCharacterState;
        OnStateExit(tmpInitialState, newState);
        CurrentCharacterState = newState;
        OnStateEnter(newState, tmpInitialState);
    }

    /// <summary>
    /// Event when entering a state, called by TransitionToState()
    /// </summary>
    /// <param name="state">to state</param>
    /// <param name="fromState">from state</param>
    private void OnStateEnter(CharacterState state, CharacterState fromState)
    {
        if (SpecificOnStateEnter(state, fromState))
        {
            return;
        }

        switch (state)
        {
            case CharacterState.Default:
            default:
            {
                Motor.SetCapsuleDimensions(defaultCapsuleDimentions_.x, defaultCapsuleDimentions_.y, defaultCapsuleDimentions_.z);
                break;
            }
            case CharacterState.Dead:
            {
                Motor.SetCapsuleDimensions(0.1f, 0.1f, 0.1f);
                break;
            }
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            {
                break;
            }
        }
    }

    /// <summary>
    /// Event when exiting a state, called by TransitionToState()
    /// </summary>
    /// <param name="state">to state</param>
    /// <param name="fromState">from state</param>
    private void OnStateExit(CharacterState state, CharacterState toState)
    {
        if (SpecificOnStateExit(state, toState))
        {
            return;
        }

        switch (state)
        {
            case CharacterState.Default:
            case CharacterState.Dead:
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            default:
            {
                break;
            }
        }
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update() { }

    /// <summary>
    /// Overrides and defines the unit's current pose
    /// </summary>
    /// <param name="position">target position</param>
    /// <param name="speed">target velocity</param>
    /// <param name="orientation">target rotation</param>
    internal void SetMotorPose(Vector3 position, Vector3 speed, Quaternion orientation)
    {
        Motor.SetPositionAndRotation(position, orientation);
        Motor.BaseVelocity = speed;
    }

    /// <summary>
    /// Returns the unit's current speed
    /// </summary>
    /// <returns>entity's velocity</returns>
    internal Vector3 GetMotorSpeed()
    {
        return Motor.BaseVelocity;
    }

    /// <summary>
    /// Called when something happens that pushes unit
    /// </summary>
    /// <param name="velocity">added velocity</param>
    internal void AddVelocity(Vector3 velocity)
    {
        // todo call specific method

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            case CharacterState.Dead:
            case CharacterState.Stunned:
            case CharacterState.Leashed:
            default:
            {
                _internalVelocityAdd += velocity;
                break;
            }
        }
    }

    /// <summary>
    /// Adds / removes a slow/haste buff from target. Effect currently additive (2 slows of 50% make target have a speed of 0%).
    ///  TODO: in the future, effect should be multiplicative for slows (2 slows of 50% make target have a speed of 25%, diminishing returns),
    ///  and additive for hastes (2 hastes of 25% makes target have speed of 150%)
    /// </summary>
    /// <param name="speed_mod">[-1, +inf] float. negative means target gets slowed by some percentage (-0.5 is a 50% slow)</param>
    internal void setSpeedModifier(float speed_mod)
    {
        _moveSpeedModifier += speed_mod;
    }
}
