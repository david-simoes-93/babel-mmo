using UnityEngine;
using static Globals;

/// <summary>
/// An abstract component to control the commands/inputs for a client UnitEntity
/// </summary>
internal abstract class BaseInputManager : MonoBehaviour
{
    protected const int kLeftMouseButton = 0,
        kRightMouseButton = 1;
    protected const string kMouseXInput = "Mouse X";
    protected const string kMouseYInput = "Mouse Y";
    protected const string kMouseScrollInput = "Mouse ScrollWheel";
    protected const string kHorizontalInput = "Horizontal";
    protected const string kVerticalInput = "Vertical";
    protected PlayerCharacterInputs kNoopCharacterInputs = new PlayerCharacterInputs { };

    protected UnitEntity parent_;
    protected int uid_;
    protected bool wasAlive_ = false;
    protected BaseCastValidator baseValidator_;
    protected BaseControllerKin baseController_;

    /// <summary>
    /// Called when user is on some Menu and not directly controlling character
    /// </summary>
    protected void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Configures the local variables the InputManager will use
    /// </summary>
    /// <param name="parent">the UnitEntity this manager belongs to</param>
    internal abstract void Config(UnitEntity parent);

    /// <summary>
    /// Forwards an RD to server with a given cast
    /// </summary>
    /// <param name="rd">the unit's cast</param>
    internal abstract void Cast(CastRD rd);

    /// <summary>
    /// Called on the first frame
    /// </summary>
    protected abstract void Start();

    /// <summary>
    /// Called every frame
    /// </summary>
    protected abstract void Update();

    /// <summary>
    /// Called by Update, deals with camera control
    /// </summary>
    protected abstract void HandleCameraInput();

    /// <summary>
    /// Called by Update, deals with character control
    /// </summary>
    protected abstract void HandleCharacterInput();

    /// <summary>
    /// Validates pressed keys, ensuring the HandleInput methods won't have to worry about non-sensical combinations
    /// </summary>
    protected abstract void ResolveConflictingKeys();
}
