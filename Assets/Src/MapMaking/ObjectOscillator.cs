using System;
using UnityEngine;

/// <summary>
/// This script oscillates an object. Once it passes the [min,max] boundaries, object starts slowing down, until it reverses, oscillating around the points
/// </summary>
internal class ObjectOscillator : MonoBehaviour
{
    public float minX = 0,
        maxX = 0,
        minY = 0,
        maxY = 0,
        minZ = 0,
        maxZ = 0;
    public float maxSpeed = 10,
        forceX = 0,
        forceY = 0,
        forceZ = 0;

    private Rigidbody rb_;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        rb_ = gameObject.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update() { }

    /// <summary>
    /// FixedUpdate called with 60Hz frequency
    /// </summary>
    void FixedUpdate()
    {
#if UNITY_SERVER
        Oscillate();
#endif
    }

    /// <summary>
    /// Server-side call to make object move
    /// </summary>
    void Oscillate()
    {
        if (forceX != 0)
        {
            if (rb_.position.x > maxX)
            {
                forceX = -Math.Abs(forceX);
            }
            else if (rb_.position.x < minX)
            {
                forceX = Math.Abs(forceX);
            }
        }

        if (forceY != 0)
        {
            if (rb_.position.y > maxY)
            {
                forceY = -Math.Abs(forceY);
            }
            else if (rb_.position.y < minY)
            {
                forceY = Math.Abs(forceY);
            }
        }

        if (forceZ != 0)
        {
            if (rb_.position.z > maxZ)
            {
                forceZ = -Math.Abs(forceZ);
            }
            else if (rb_.position.z < minZ)
            {
                forceZ = Math.Abs(forceZ);
            }
        }
        rb_.AddForce(forceX, forceY, forceZ);

        if (rb_.velocity.magnitude > maxSpeed)
        {
            rb_.velocity = rb_.velocity.normalized * maxSpeed;
        }
    }
}
