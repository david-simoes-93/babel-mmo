using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The GameWorld, containing an actual level, an entity manager, and corresponding entities. Eventually is it configured with a network Client/Server to exchange information
/// </summary>
internal class GameWorld
{
    internal readonly string WorldName;
    internal NetworkServer NetworkServer { get; private set; }
    internal NetworkClient NetworkClient { get; private set; }
    internal EntityManager EntityManager { get; private set; }

    /// <summary>
    /// Server-side constructor
    /// </summary>
    /// <param name="name">World's name</param>
    internal GameWorld(string name)
    {
        WorldName = name;
        EntityManager = new EntityManager();
    }

    /// <summary>
    /// Client-side constructor
    /// </summary>
    /// <param name="name">World's name</param>
    /// <param name="playerUid">Client's UID</param>
    internal GameWorld(string name, int playerUid)
    {
        WorldName = name;
        EntityManager = new EntityManager(playerUid);
    }

    /// <summary>
    /// Client-side callback, when a world is loaded, to fill the EM. Method loads PERMANENT entities in map, and then calls AfterLoadScene()
    /// </summary>
    /// <param name="scene">Loaded scene</param>
    /// <param name="mode">Scene loading mode</param>
    internal void ClientRegisterPermanentSceneEntities(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ClientRegisterPermanentSceneEntities;
        AddPermEntities(GameObject.Find("PermanentReplicatedEntities"));
        ClientGameLoop.AfterLoadScene();
    }

    /// <summary>
    /// Server-side callback, when a world is loaded, to fill the EM with permanent entities in World, and to spawn temporary entities and add them to EM
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    internal void ServerRegisterAllSceneEntities(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject o in scene.GetRootGameObjects())
        {
            if (o.name == "PermanentReplicatedEntities")
                AddPermEntities(o);
            if (o.name == "TempReplicatedEntities")
                AddTempEntities(o);
        }
    }

    /// <summary>
    /// Adds all permanent entities to EM
    /// </summary>
    /// <param name="PermanentReplicatedEntities">The root PermanentReplicatedEntities object</param>
    private void AddPermEntities(GameObject PermanentReplicatedEntities)
    {
        // add permanent entities controlled by server to EntityManager
        foreach (Transform child in PermanentReplicatedEntities.transform)
        {
            EntityManager.AddPermanentEntity(child.gameObject);
        }
    }

    /// <summary>
    /// Server-side call. Adds all temporary entities to EM
    /// </summary>
    /// <param name="PermanentReplicatedEntities">The root TempReplicatedEntities object</param>
    private void AddTempEntities(GameObject TempReplicatedEntities)
    {
        // spawn / add temporary entities to EM
        foreach (Transform child in TempReplicatedEntities.transform)
        {
            MapEntityDescriptor entityInfo = child.gameObject.GetComponent<MapEntityDescriptor>();

            // spawn entity now
            ReliableData dummySpawn = new SpawnRD(
                EntityManager.GetValidNpcUid(),
                entityInfo.unitName,
                entityInfo.type,
                entityInfo.health,
                entityInfo.health,
                child.position,
                child.rotation,
                0
            );
            EntityManager.AsyncCreateTempEvent(dummySpawn);
        }
    }

    /// <summary>
    /// Closes the game world
    /// </summary>
    internal void Close() { }

    /// <summary>
    /// Client-side config call
    /// </summary>
    /// <param name="nc">the client's NetworkClient</param>
    internal void Config(NetworkClient nc)
    {
        NetworkClient = nc;
    }

    /// <summary>
    /// Server-side config call
    /// </summary>
    /// <param name="ns">the server's NetworkServer</param>
    internal void Config(NetworkServer ns)
    {
        NetworkServer = ns;
    }

    //internal static List<Collider> allColliders = new List<Collider>();
    //internal double nextTickTime;
    //internal float frameDuration;
    //internal int lastServerTick;

    // Makes it so that collider coll will not collide with the first amountOfCollidersFromOtherScenes registered colliders in the list allColliders
    // The idea is that, when loading multiple scenes, you want collisions between each object in the same scene, but not between scenes
    // So the first scene loads 10 colliders with amountOfCollidersFromOtherScenes=0, and the second loads 5 colliders with amountOfCollidersFromOtherScenes=10
    /*private void ignoreCollisionsWithOtherScenes(Collider coll, int amountOfCollidersFromOtherScenes)
    {
        // ignore collisions of this object with objects from other scenes
        if (coll != null)
        {
            for (int index = 0; index < amountOfCollidersFromOtherScenes; index++)
            {
                Physics.IgnoreCollision(coll, allColliders[index]);
            }

            // add this collider to the list
            allColliders.Add(coll);
        }
    }*/

    // iterate over all entities and disable collisions of them with other worlds
    /*int amountOfCollidersAlreadyRegistered = allColliders.Count;
    GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
    foreach (GameObject go in allObjects)
        if (go.activeInHierarchy)
            ignoreCollisionsWithOtherScenes(go.GetComponent<Collider>(), amountOfCollidersAlreadyRegistered);
            */
}
