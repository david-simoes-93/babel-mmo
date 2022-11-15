using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstract component to control the camera for a client UnitEntity
/// </summary>
internal abstract class BaseCamera : MonoBehaviour
{
    [Header("Framing")]
    public Camera Camera = null;
    public Vector2 FollowPointFraming = new Vector2(0f, 0f);
    public float FollowingSharpness = 10000f;

    [Header("Distance")]
    public float DefaultDistance = 0f;
    public float MinDistance = 0f;
    public float MaxDistance = 10f;
    public float DistanceMovementSpeed = 5f;
    public float DistanceMovementSharpness = 10f;

    [Header("Rotation")]
    public bool InvertX = false;
    public bool InvertY = false;

    [Range(-90f, 90f)]
    public float DefaultVerticalAngle = 20f;

    [Range(-90f, 90f)]
    public float MinVerticalAngle = -90f;

    [Range(-90f, 90f)]
    public float MaxVerticalAngle = 90f;
    public float RotationSpeed = 1f;
    public float RotationSharpness = 10000f;

    [Header("Obstruction")]
    public float ObstructionCheckRadius = 0.2f;
    public LayerMask ObstructionLayers = -1;
    public float ObstructionSharpness = 10000f;
    public List<Collider> IgnoredColliders = new List<Collider>();

    internal Transform Transform { get; private set; }
    internal Vector3 PlanarDirection { get; private set; }
    internal Transform FollowTransform { get; private set; }
    internal float TargetDistance { get; set; }

    protected bool _distanceIsObstructed;
    protected float _currentDistance;
    protected float _targetVerticalAngle;

    //protected RaycastHit _obstructionHit;
    protected int _obstructionCount;
    protected RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];

    //protected float _obstructionTime;
    protected Vector3 _currentFollowPosition;

    protected const int MaxObstructions = 32;

    /// <summary>
    /// Validates (and clamps) camera's position
    /// </summary>
    void OnValidate()
    {
        DefaultDistance = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
        DefaultVerticalAngle = Mathf.Clamp(DefaultVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
    }

    /// <summary>
    /// Called on construct
    /// </summary>
    void Awake()
    {
        Transform = this.transform;

        _currentDistance = DefaultDistance;
        TargetDistance = _currentDistance;

        _targetVerticalAngle = 0f;

        PlanarDirection = Vector3.forward;
    }

    /// <summary>
    /// Configures the local variables the camera will use
    /// </summary>
    /// <param name="parent">the UnitEntity this camera belongs to</param>
    internal void Config(UnitEntity parent) { }

    /// <summary>
    /// Set the transform that the camera will orbit around
    /// </summary>
    /// <param name="t">UnitEntity's transform</param>
    internal void SetFollowTransform(Transform t)
    {
        FollowTransform = t;
        PlanarDirection = FollowTransform.forward;
        _currentFollowPosition = FollowTransform.position;
    }

    /// <summary>
    /// Sets the camera's target distance
    /// </summary>
    /// <param name="dist">target distance</param>
    internal void SetTargetDistance(float dist)
    {
        TargetDistance = dist;
    }

    /// <summary>
    /// Updates Camera's position
    /// </summary>
    /// <param name="deltaTime">time since last frame</param>
    /// <param name="zoomInput">camera zoom input</param>
    /// <param name="rotationInput">camera rotation input</param>
    internal void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput)
    {
        if (FollowTransform)
        {
            if (InvertX)
            {
                rotationInput.x *= -1f;
            }
            if (InvertY)
            {
                rotationInput.y *= -1f;
            }

            // Process rotation input
            Quaternion rotationFromInput = Quaternion.Euler(FollowTransform.up * (rotationInput.x * RotationSpeed));
            PlanarDirection = rotationFromInput * PlanarDirection;
            PlanarDirection = Vector3.Cross(FollowTransform.up, Vector3.Cross(PlanarDirection, FollowTransform.up));
            _targetVerticalAngle -= (rotationInput.y * RotationSpeed);
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);

            // Process distance input
            if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f)
            {
                TargetDistance = _currentDistance;
            }
            TargetDistance += zoomInput * DistanceMovementSpeed;
            TargetDistance = Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);

            // Find the smoothed follow position
            _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1f - Mathf.Exp(-FollowingSharpness * deltaTime)); //

            // Calculate smoothed rotation
            Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, FollowTransform.up);
            Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
            Quaternion targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot, 1f - Mathf.Exp(-RotationSharpness * deltaTime)); //

            // Apply rotation
            Transform.rotation = targetRotation;

            // Handle obstructions
            {
                RaycastHit closestHit = new RaycastHit();
                closestHit.distance = Mathf.Infinity;
                _obstructionCount = Physics.SphereCastNonAlloc(
                    _currentFollowPosition,
                    ObstructionCheckRadius,
                    -Transform.forward,
                    _obstructions,
                    TargetDistance,
                    ObstructionLayers,
                    QueryTriggerInteraction.Ignore
                );
                for (int i = 0; i < _obstructionCount; i++)
                {
                    bool isIgnored = false;
                    for (int j = 0; j < IgnoredColliders.Count; j++)
                    {
                        if (IgnoredColliders[j] == _obstructions[i].collider)
                        {
                            isIgnored = true;
                            break;
                        }
                    }
                    for (int j = 0; j < IgnoredColliders.Count; j++)
                    {
                        if (IgnoredColliders[j] == _obstructions[i].collider)
                        {
                            isIgnored = true;
                            break;
                        }
                    }

                    if (!isIgnored && _obstructions[i].distance < closestHit.distance && _obstructions[i].distance > 0)
                    {
                        closestHit = _obstructions[i];
                    }
                }

                // If obstructions detected
                if (closestHit.distance < Mathf.Infinity)
                {
                    _distanceIsObstructed = true;
                    _currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance, 1 - Mathf.Exp(-ObstructionSharpness * deltaTime));
                }
                // If no obstruction
                else
                {
                    _distanceIsObstructed = false;
                    _currentDistance = Mathf.Lerp(_currentDistance, TargetDistance, 1 - Mathf.Exp(-DistanceMovementSharpness * deltaTime));
                }
            }

            // Find the smoothed camera orbit position
            Vector3 targetPosition = _currentFollowPosition - ((targetRotation * Vector3.forward) * _currentDistance);

            // Handle framing
            targetPosition += Transform.right * FollowPointFraming.x;
            targetPosition += Transform.up * FollowPointFraming.y;

            // Apply position
            Transform.position = targetPosition;
        }
    }
}
