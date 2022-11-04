using UnityEngine;
using UnityEditor;
using System.Collections;

public class ParticleComponent : ExploderComponent
{
    public GameObject explosionEffectsContainer;
    public float scale = 1;
    public float playbackSpeed = 1;
    public override void onExplosionStarted(Exploder exploder)
    {
        GameObject container = (GameObject)GameObject.Instantiate(explosionEffectsContainer, transform.position, Quaternion.identity);
        ParticleSystem[] systems = container.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSpeedMultiplier = scale;
            main.startSizeMultiplier = scale;
            system.transform.localScale *= scale;
            main.simulationSpeed = playbackSpeed;
        }
    }
}
