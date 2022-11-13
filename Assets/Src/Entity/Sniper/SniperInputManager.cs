using UnityEngine;
using static Globals;

internal class SniperInputManager : BaseInputManager
{
    private SniperControllerKin controller_;
    private SniperCamera camera_;
    private SniperCastValidator validator_;

    private long unfreezeTime_ms_; // ? probably to prevent shooting from camera when camera is outside of unit? could be done with distance instead
    private float recoilV_ = 0,
        recoilH_ = 0;

    // Movement WASD Space
    private bool moveLeft_,
        moveRight_,
        moveFront_,
        moveBack_;

    // attack left
    private bool attackLeft_;

    // attack right
    private bool attackRight_;

    // reload
    private bool reload_;

    // attack left
    private bool scrollUp_;

    // attack right
    private bool scrollDown_;

    /// <summary>
    /// Configures the local variables the InputManager will use
    /// </summary>
    /// <param name="parent">the UnitEntity this manager belongs to</param>
    internal override void Config(UnitEntity parent)
    {
        parent_ = parent;
        controller_ = (SniperControllerKin)parent.Controller;
        baseController_ = controller_;
        camera_ = (SniperCamera)parent.Camera;
        validator_ = (SniperCastValidator)parent.Validator;
        baseValidator_ = validator_;
        uid_ = parent.Uid;
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

        unfreezeTime_ms_ = currTime_ms + 1000;
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    protected override void Update()
    {
        if (UpdateIfDead())
            return;

        if (UpdateIfStunned())
            return;

        // switch between mouse and crosshair
        if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.visible)
                unfreezeTime_ms_ = currTime_ms + 500;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (Input.GetAxisRaw("Cancel") > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        HandleCameraInput();

        if (currTime_ms < unfreezeTime_ms_)
            return;

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
            // if just became alive
            if (!wasAlive_)
            {
                camera_.SetTargetDistance(0);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            wasAlive_ = true;
            return false;
        }

        if (wasAlive_)
        {
            camera_.SetTargetDistance(6);
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
    /// Called every frame, processes input if player is Stunned
    /// </summary>
    /// <returns>true if player is Stunned</returns>
    private bool UpdateIfStunned()
    {
        if (!parent_.IsStunned)
        {
            //camera_.SetTargetDistance(0);
            return false;
        }

        //camera_.SetTargetDistance(5); // se calhar fazemos só no leash

        HandleCameraInput();
        // inputs should have camera_.Transform.rotation
        controller_.SetInputs(ref kNoopCharacterInputs);

        return true;
    }

    /// <summary>
    /// Sets the Sniper's camera with some recoil, applied later on HandleCameraInput()
    /// </summary>
    /// <param name="v">vertical recoil</param>
    /// <param name="h">horizontal recoil</param>
    internal void SetRecoil(double v, double h)
    {
        recoilV_ = (float)v;
        recoilH_ = (float)h;
    }

    /// <summary>
    /// Called by Update, deals with camera control
    /// </summary>
    protected override void HandleCameraInput()
    {
        // Create the look input vector for the camera
        float mouseLookAxisUp = Input.GetAxisRaw(kMouseYInput) + recoilV_;
        float mouseLookAxisRight = Input.GetAxisRaw(kMouseXInput) + recoilH_;
        recoilV_ = 0;
        recoilH_ = 0;
        Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

        // Prevent moving the camera while the cursor isn't locked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            lookInputVector = Vector3.zero;
        }

        // Apply inputs to the camera
        camera_.UpdateWithInput(Time.deltaTime, 0, lookInputVector);
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

        // left attack
        attackLeft_ = Input.GetMouseButton(kLeftMouseButton);

        // right attack
        attackRight_ = Input.GetMouseButton(kRightMouseButton);

        // reload
        reload_ = Input.GetAxis("Reload") > 0;

        // Scroll weapons
        float scrollInput = Input.GetAxis(kMouseScrollInput);
        scrollDown_ = scrollInput < 0;
        scrollUp_ = scrollInput > 0;

        ResolveConflictingKeys();

        if (moveFront_)
        {
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else if (moveLeft_)
        {
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkLeft);
        }
        else if (moveRight_)
        {
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkRight);
        }
        else if (moveBack_)
        {
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkBack);
        }
        else
        {
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kIdle);
        }

        // check ammo and attack/reload
        if ((attackLeft_ || attackRight_) && !validator_.HasAmmo())
        {
            attackLeft_ = false;
            attackRight_ = false;
            reload_ = true;
        }
        else if (attackLeft_)
        {
            if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponRifle && validator_.CanWeaponRifleFire(currTime_ms))
                Cast(CastUtils.MakeSniperWeaponRifleAttack(uid_, transform.position, transform.rotation));
            else if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponShotgun && validator_.CanWeaponShotgunFire(currTime_ms))
                Cast(CastUtils.MakeSniperWeaponShotgunAttack(uid_, transform.position, transform.rotation));
            else if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponMedigun && validator_.CanWeaponMedigunFire(currTime_ms))
                Cast(CastUtils.MakeSniperWeaponMedigunAttack(uid_, transform.position, transform.rotation));
        }
        else if (attackRight_)
        {
            if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponRifle && validator_.CanWeaponRifleAlternate(currTime_ms))
                Cast(CastUtils.MakeSniperWeaponRifleleAlternateAttack(uid_, transform.position, transform.rotation));
            else if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponShotgun && validator_.CanWeaponShotgunAlternate(currTime_ms))
            {
                Cast(CastUtils.MakeSniperWeaponShotgunAlternateAttack(uid_, transform.position, transform.rotation));
            }
            else if (validator_.CurrentWeapon() == CastCode.SniperChooseWeaponMedigun && validator_.CanWeaponMedigunAlternate(currTime_ms))
                Cast(CastUtils.MakeSniperWeaponMedigunAlternateAttack(uid_));
        }

        // reload
        if (reload_ && validator_.CanReload(currTime_ms))
        {
            Cast(CastUtils.MakeReload(uid_));
        }

        // scroll
        if (scrollUp_ && validator_.CanWeaponScroll(currTime_ms))
        {
            switch (validator_.CurrentWeapon())
            {
                case CastCode.SniperChooseWeaponRifle:
                    Cast(CastUtils.MakeChooseWeaponMedigun(uid_));
                    break;
                case CastCode.SniperChooseWeaponShotgun:
                    Cast(CastUtils.MakeChooseWeaponRifle(uid_));
                    break;
                case CastCode.SniperChooseWeaponMedigun:
                    Cast(CastUtils.MakeChooseWeaponShotgun(uid_));
                    break;
            }
        }
        else if (scrollDown_ && validator_.CanWeaponScroll(currTime_ms))
        {
            switch (validator_.CurrentWeapon())
            {
                case CastCode.SniperChooseWeaponRifle:
                    Cast(CastUtils.MakeChooseWeaponShotgun(uid_));
                    break;
                case CastCode.SniperChooseWeaponShotgun:
                    Cast(CastUtils.MakeChooseWeaponMedigun(uid_));
                    break;
                case CastCode.SniperChooseWeaponMedigun:
                    Cast(CastUtils.MakeChooseWeaponRifle(uid_));
                    break;
            }
        }

        // Build the CharacterInputs struct
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs
        {
            MoveAxisForward = Input.GetAxisRaw(kVerticalInput),
            MoveAxisRight = Input.GetAxisRaw(kHorizontalInput),
            CameraRotation = camera_.Transform.rotation,
            JumpDown = Input.GetKeyDown(KeyCode.Space),
            CrouchDown = Input.GetKeyDown(KeyCode.LeftControl),
            CrouchUp = Input.GetKeyUp(KeyCode.LeftControl)
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

        if (reload_)
        {
            attackLeft_ = false;
            attackRight_ = false;
        }

        if (scrollUp_ && scrollDown_)
        {
            scrollUp_ = false;
            scrollDown_ = false;
        }

        if (scrollUp_ || scrollDown_)
        {
            attackLeft_ = false;
            attackRight_ = false;
            reload_ = false;
        }
    }
}
