using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
internal class LocalPlayerWrapper : MonoBehaviour
{
    private void Awake()
    {
        // Create CGL and player
        new ClientGameLoop();
        UnitEntity myself = new UnitEntity(1, Globals.UnitEntityCode.kFighter);
        ClientGameLoop.SetLocalPlayer(myself);

        // Create EM and event
        EntityManager em = new EntityManager();
        SpawnRD rd = new SpawnRD(1, "LocalPlayer", Globals.UnitEntityCode.kFighter, 100, 100, gameObject.transform.position, gameObject.transform.rotation, 0);

        // Spawn
        myself.Spawn(rd, em);
    }

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    void FixedUpdate() { }
}
