using UnityEngine;
using static Globals;

internal class MageInputManager : BaseInputManager
{
    private MageControllerKin controller_;
    private MageCamera camera_;
    private MageCastValidator validator_;
    private MageCanvas canvas_;

    // Movement WASD Space
    private bool moveLeft_,
        moveRight_,
        moveFront_,
        moveBack_;

    // attack fire + dot
    private bool attackFireflash_;

    // attack frost + slow
    private bool attackFrostflash_;

    // attack arcane + dmg
    private bool attackArcaneflash_;

    // attack fire cd
    private bool attackPyroblast_;

    // heal cd
    private bool attackRenew_;
    public float CameraAdjustmentSpeed = 5f;

    /// <summary>
    /// Configures the local variables the InputManager will use
    /// </summary>
    /// <param name="parent">the UnitEntity this manager belongs to</param>
    internal override void Config(UnitEntity parent)
    {
        parent_ = parent;
        controller_ = (MageControllerKin)parent.Controller;
        baseController_ = controller_;
        camera_ = (MageCamera)parent.Camera;
        validator_ = (MageCastValidator)parent.Validator;
        canvas_ = (MageCanvas)parent.Canvas;
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
        HandleUiInput();

        if (UpdateIfDead())
            return;
        if (UpdateIfStunned())
            return;

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
            wasAlive_ = true;
            return false;
        }
        wasAlive_ = false;
        controller_.SetInputs(ref kNoopCharacterInputs);

        Vector3 lookInputVector = MoveCameraOnMouseDrag();
        float scrollInput = -Input.GetAxis(kMouseScrollInput);
        camera_.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

        return true;
    }

    private Vector3 MoveCameraOnMouseDrag()
    {
        Vector3 lookInputVector = Vector3.zero;
        // Move camera when left or right clicked
        if (Input.GetMouseButton(kLeftMouseButton) || Input.GetMouseButton(kRightMouseButton))
        {
            // Hide mouse when right-dragging
            if (Input.GetMouseButton(kRightMouseButton))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Create the look input vector for the camera
            float mouseLookAxisUp = Input.GetAxisRaw(kMouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(kMouseXInput);
            lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // rotate camera in same direction as character if moving and not dragging it
            if (moveFront_ || moveBack_ || moveLeft_ || moveRight_)
            {
                Vector3 camera_dir = camera_.transform.rotation * Vector3.forward;
                camera_dir.y = 0;
                float angle_delta =
                    Vector3.Angle(camera_dir, controller_.Motor.CharacterForward) * Mathf.Sign(Vector3.Cross(camera_dir, controller_.Motor.CharacterForward).y);
                lookInputVector = new Vector3(Mathf.Min((angle_delta / 180) * CameraAdjustmentSpeed, CameraAdjustmentSpeed), 0, 0f);
            }
        }
        return lookInputVector;
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
        Vector3 lookInputVector = MoveCameraOnMouseDrag();

        float scrollInput = -Input.GetAxis(kMouseScrollInput);
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

        attackFireflash_ = Input.GetAxis("Fireflash") > 0;
        attackFrostflash_ = Input.GetAxis("Frostflash") > 0;
        attackArcaneflash_ = Input.GetAxis("Arcaneflash") > 0;
        attackPyroblast_ = Input.GetAxis("Pyroblast") > 0;
        attackRenew_ = Input.GetAxis("Renew") > 0;

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

        // attack
        if (attackFireflash_ && validator_.CanFireflash(canvas_.GetTarget()))
        {
            Cast(CastUtils.MakeMageFireflash(uid_, canvas_.GetTarget().Uid));
        }
        else if (attackFrostflash_ && validator_.CanFrostflash(canvas_.GetTarget()))
        {
            Cast(CastUtils.MakeMageFrostflash(uid_, canvas_.GetTarget().Uid));
        }
        else if (attackArcaneflash_ && validator_.CanArcaneflash(canvas_.GetTarget()))
        {
            Cast(CastUtils.MakeMageArcaneflash(uid_, canvas_.GetTarget().Uid));
        }
        else if (attackPyroblast_ && validator_.CanPyroblast(canvas_.GetTarget()))
        {
            Cast(CastUtils.MakeMagePyroblast(uid_, canvas_.GetTarget().Uid));
        }
        else if (attackRenew_ && validator_.CanRenew(canvas_.GetTarget()))
        {
            Cast(CastUtils.MakeMageRenew(uid_, canvas_.GetTarget().Uid));
        }

        bool targetShouldFollowCameraOrientation = Input.GetMouseButton(kRightMouseButton);
        // Build the CharacterInputs struct
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs
        {
            MoveAxisForward = Input.GetAxisRaw(kVerticalInput),
            MoveAxisRight = Input.GetAxisRaw(kHorizontalInput),
            CameraRotation = targetShouldFollowCameraOrientation ? camera_.Transform.rotation : controller_.transform.rotation,
            JumpDown = Input.GetKeyDown(KeyCode.Space),
        };

        // Apply inputs to character
        controller_.SetInputs(ref characterInputs);
    }

    private void HandleUiInput()
    {
        if (!Input.GetMouseButtonDown(kLeftMouseButton))
        {
            return;
        }

        //ray shooting out of the camera from where the mouse is
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            BaseControllerKin controller = hit.collider.gameObject.GetComponent<BaseControllerKin>();
            if (controller != null)
            {
                canvas_.SetTarget(controller.Parent());
            }
            else
            {
                canvas_.ClearTarget();
            }
        }
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

        int attack_buttons_pressed = 0;
        if (attackFireflash_)
            attack_buttons_pressed++;
        if (attackFrostflash_)
            attack_buttons_pressed++;
        if (attackArcaneflash_)
            attack_buttons_pressed++;
        if (attackPyroblast_)
            attack_buttons_pressed++;
        if (attackRenew_)
            attack_buttons_pressed++;
        if (attack_buttons_pressed > 1)
        {
            attackFireflash_ = false;
            attackFrostflash_ = false;
            attackArcaneflash_ = false;
            attackPyroblast_ = false;
            attackRenew_ = false;
        }
    }
}
