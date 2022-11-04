using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathWaveBase : MonoBehaviour
{
    Vector3 move_delta_;

    // Start is called before the first frame update
    void Start()
    {
        move_delta_ = new Vector3(0.15f, 0, 0);
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        // TODO not synced with client! movement should be server-side, and position sent to client
        gameObject.transform.Translate(move_delta_);
    }

    // Update is called once per frame
    void Update() { }
}
