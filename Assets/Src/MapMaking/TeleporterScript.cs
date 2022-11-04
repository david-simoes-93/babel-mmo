using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterScript : MonoBehaviour
{
    public GameObject teleport_target;

    float MinX,
        MaxX,
        MinZ,
        MaxZ;
    Vector3 teleportation_delta;

    // Start is called before the first frame update
    void Start()
    {
        MinX = transform.position.x - transform.lossyScale.x / 2f;
        MaxX = transform.position.x + transform.lossyScale.x / 2f;
        MinZ = transform.position.z - transform.lossyScale.z / 2f;
        MaxZ = transform.position.z + transform.lossyScale.z / 2f;
        teleportation_delta = teleport_target.transform.position - transform.position;
    }

    // Update is called once per frame
    void Update()
    {
#if !UNITY_SERVER
        Vector3 player_pos = ClientGameLoop.CGL.UnitEntity.UnitTransform().position;

        if (player_pos.x >= MinX && player_pos.x <= MaxX && player_pos.z >= MinZ && player_pos.z <= MaxZ && player_pos.y >= 10 && player_pos.y <= 18)
        {
            BaseControllerKin controller = ClientGameLoop.CGL.UnitEntity.Controller;
            controller.SetMotorPose(player_pos + teleportation_delta, controller.GetMotorSpeed(), ClientGameLoop.CGL.UnitEntity.UnitTransform().rotation);
        }
#else
    // do nothing
#endif
    }
}
