using UnityEngine;
using static Globals;

internal class FighterInputManager : BaseInputManager
{
    private FighterControllerKin controller_;
    private FighterCamera camera_;
    private FighterCastValidator validator_;

    // Movement WASD Space
    private bool moveLeft_,
        moveRight_,
        moveFront_,
        moveBack_;

    // Charge Mouse3
    private bool charge_;

    // dodge double tap
    private long lastLeftPress_,
        lastRightPress_,
        lastFrontPress_,
        lastBackPress_;
    private bool wasMovingLeft_,
        wasMovingRight_,
        wasMovingFront_,
        wasMovingBack_;
    private bool dodgeLeft_,
        dodgeRight_,
        dodgeFront_,
        dodgeBack_;
    private readonly int kDodgeDoubleTapMaxTime = 300;

    // attack left
    private bool attackLeft_;

    // attack right
    private bool attackRight_;

    /// <summary>
    /// Configures the local variables the InputManager will use
    /// </summary>
    /// <param name="parent">the UnitEntity this manager belongs to</param>
    internal override void Config(UnitEntity parent)
    {
        parent_ = parent;
        controller_ = (FighterControllerKin)parent.Controller;
        baseController_ = controller_;
        camera_ = (FighterCamera)parent.Camera;
        validator_ = (FighterCastValidator)parent.Validator;
        baseValidator_ = validator_;
        uid_ = parent.Uid;
        animator_ = parent.Animator;
    }

    /// <summary>
    /// Creates an RD which is sent to server with a given cast
    /// </summary>
    /// <param name="code">the unit's cast</param>
    internal override void Cast(CastRD rd)
    {
        // ensure only a single cast is sent
        if (baseValidator_.IsCastcodeClear())
        {
            baseValidator_.SetSentCastcode(rd.type);
            ClientGameLoop.CGL.NetworkClient.AddEvent(rd);
        }
    }

    /// <summary>
    /// Called on the first frame
    /// </summary>
    protected override void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Tell camera to follow transform
        camera_.SetFollowTransform(controller_.CameraFollowPoint);

        // Ignore the character's collider(s) for camera obstruction checks
        camera_.IgnoredColliders.Clear();
        camera_.IgnoredColliders.AddRange(controller_.GetComponentsInChildren<Collider>());
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    protected override void Update()
    {
        // if nothing has been cast, we check if we need to reset combo
        if (validator_.IsCastcodeClear())
            validator_.CheckAndResetCombo();

        if (UpdateIfDead())
            return;
        if (UpdateIfStunned())
            return;

        // else
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (Input.GetAxisRaw("Cancel") > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        HandleCameraInput();
        HandleCharacterInput();
    }

    /// <summary>
    /// Called every frame, processes input if player is dead
    /// </summary>
    /// <returns>true if player is dead</returns>
    private bool UpdateIfDead()
    {
        if (!parent_.IsDead)
        {
            if (!wasAlive_)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            wasAlive_ = true;
            return false;
        }

        wasAlive_ = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        controller_.SetInputs(ref kNoopCharacterInputs);

        Vector3 lookInputVector = Vector3.zero;

        // Move camera when right clicked
        if (Input.GetMouseButton(kRightMouseButton))
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = Input.GetAxisRaw(kMouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(kMouseXInput);
            lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);
        }
        float scrollInput = -Input.GetAxis(kMouseScrollInput);
        camera_.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

        return true;
    }

    /// <summary>
    /// Called every frame, processes input if player is dead
    /// </summary>
    /// <returns>true if player is dead</returns>
    private bool UpdateIfStunned()
    {
        if (!parent_.IsStunned)
        {
            return false;
        }

        controller_.SetInputs(ref kNoopCharacterInputs);
        // Create the look input vector for the camera
        float mouseLookAxisUp = Input.GetAxisRaw(kMouseYInput);
        float mouseLookAxisRight = Input.GetAxisRaw(kMouseXInput);
        Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);
        camera_.UpdateWithInput(Time.deltaTime, 0, lookInputVector);

        return true;
    }

    /// <summary>
    /// Called by Update, deals with camera control
    /// </summary>
    protected override void HandleCameraInput()
    {
        // Create the look input vector for the camera
        float mouseLookAxisUp = Input.GetAxisRaw(kMouseYInput);

        float mouseLookAxisRight;
        if (
            controller_.CurrentCharacterState == CharacterState.FighterDodgingBack
            || controller_.CurrentCharacterState == CharacterState.FighterDodgingFront
            || controller_.CurrentCharacterState == CharacterState.FighterDodgingLeft
            || controller_.CurrentCharacterState == CharacterState.FighterDodgingRight
        )
        {
            mouseLookAxisRight = 0;
        }
        else if (controller_.CurrentCharacterState == CharacterState.FighterCharging)
        {
            // TODO not able to turn at all during charge!
            mouseLookAxisRight = Input.GetAxisRaw(kMouseXInput) / 4;
        }
        else
        {
            mouseLookAxisRight = Input.GetAxisRaw(kMouseXInput);
        }
        Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

        // Prevent moving the camera while the cursor isn't locked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            lookInputVector = Vector3.zero;
        }

        // Input for zooming the camera
        float scrollInput = -Input.GetAxis(kMouseScrollInput);

        // Apply inputs to the camera
        camera_.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);
    }

    /// <summary>
    /// Called by Update, deals with character control
    /// </summary>
    protected override void HandleCharacterInput()
    {
        moveFront_ = Input.GetAxisRaw(kVerticalInput) > 0;
        moveBack_ = Input.GetAxisRaw(kVerticalInput) < 0;
        moveRight_ = Input.GetAxisRaw(kHorizontalInput) > 0;
        moveLeft_ = Input.GetAxisRaw(kHorizontalInput) < 0;

        // Charge
        charge_ = Input.GetAxis("Fighter Charge") > 0;

        // left attack
        attackLeft_ = Input.GetMouseButton(kLeftMouseButton);

        // right attack
        attackRight_ = Input.GetMouseButton(kRightMouseButton);

        ResolveConflictingKeys();

        if (moveFront_)
        {
            parent_.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else if (moveLeft_)
        {
            parent_.SetAnimatorState(EntityAnimation.kWalkLeft);
        }
        else if (moveRight_)
        {
            parent_.SetAnimatorState(EntityAnimation.kWalkRight);
        }
        else if (moveBack_)
        {
            parent_.SetAnimatorState(EntityAnimation.kWalkBack);
        }
        else
        {
            parent_.SetAnimatorState(EntityAnimation.kIdle);
        }

        // dodge
        dodgeRight_ = false;
        dodgeLeft_ = false;
        dodgeFront_ = false;
        dodgeBack_ = false;
        if (moveRight_ && !wasMovingRight_)
        {
            dodgeRight_ = currTime_ms - lastRightPress_ < kDodgeDoubleTapMaxTime;
            lastRightPress_ = currTime_ms;
        }
        else if (moveLeft_ && !wasMovingLeft_)
        {
            dodgeLeft_ = currTime_ms - lastLeftPress_ < kDodgeDoubleTapMaxTime;
            lastLeftPress_ = currTime_ms;
        }
        else if (moveFront_ && !wasMovingFront_)
        {
            dodgeFront_ = currTime_ms - lastFrontPress_ < kDodgeDoubleTapMaxTime;
            lastFrontPress_ = currTime_ms;
        }
        else if (moveBack_ && !wasMovingBack_)
        {
            dodgeBack_ = currTime_ms - lastBackPress_ < kDodgeDoubleTapMaxTime;
            lastBackPress_ = currTime_ms;
        }
        wasMovingBack_ = moveBack_;
        wasMovingFront_ = moveFront_;
        wasMovingLeft_ = moveLeft_;
        wasMovingRight_ = moveRight_;

        // attack
        if (attackLeft_ && validator_.CanAttackLeft())
        {
            Cast(CastUtils.MakeFighterAttackLeft(uid_, transform.position, transform.rotation));
        }
        else if (attackRight_ && validator_.CanAttackRight())
        {
            Cast(CastUtils.MakeFighterAttackRight(uid_, transform.position, transform.rotation));
        }

        // charge
        if (charge_ && validator_.CanCharge())
        {
            Cast(CastUtils.MakeCharge(uid_));
        }

        // dodge
        if (dodgeFront_ && validator_.CanDodge())
        {
            Cast(CastUtils.MakeDodge(uid_, CastCode.DodgeFront));
        }
        else if (dodgeBack_ && validator_.CanDodge())
        {
            Cast(CastUtils.MakeDodge(uid_, CastCode.DodgeBack));
        }
        else if (dodgeLeft_ && validator_.CanDodge())
        {
            Cast(CastUtils.MakeDodge(uid_, CastCode.DodgeLeft));
        }
        else if (dodgeRight_ && validator_.CanDodge())
        {
            Cast(CastUtils.MakeDodge(uid_, CastCode.DodgeRight));
        }

        // Build the CharacterInputs struct
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs
        {
            MoveAxisForward = Input.GetAxisRaw(kVerticalInput),
            MoveAxisRight = Input.GetAxisRaw(kHorizontalInput),
            CameraRotation = camera_.Transform.rotation,
            JumpDown = Input.GetKeyDown(KeyCode.Space),
        };

        // Apply inputs to character
        controller_.SetInputs(ref characterInputs);
    }

    /// <summary>
    /// Validates pressed keys, ensuring the HandleInput methods won't have to worry about non-sensical combinations
    /// </summary>
    protected override void ResolveConflictingKeys()
    {
        if (moveRight_ && moveLeft_)
        {
            moveRight_ = false;
            moveLeft_ = false;
        }
        if (moveFront_ && moveBack_)
        {
            moveFront_ = false;
            moveBack_ = false;
        }

        if (attackLeft_ && attackRight_)
        {
            attackLeft_ = false;
            attackRight_ = false;
        }
    }
}
