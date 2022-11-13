using NetworkCompression;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking.Match;
using System.Linq;
using System.Collections.Concurrent;

/// <summary>
/// A handler for the world servers's connection to all the clients
/// </summary>
internal class NetworkServer
{
    private readonly EntityManager em_;
    private readonly List<SocketPair> clients_;
    private readonly ConcurrentQueue<SocketPair> clientsConnecting_;
    private readonly GameWorld gw_;
    private int entityPoseCyclicIndex_ = 0;

    internal int worldTcpPort;

    /// <summary>
    /// Constructor. Also starts a ServerAcceptor thread to accept incoming connections
    /// </summary>
    /// <param name="gw">associated GameWorld</param>
    internal NetworkServer(GameWorld gw)
    {
        em_ = gw.EntityManager;
        gw_ = gw;
        clients_ = new List<SocketPair>();
        clientsConnecting_ = new ConcurrentQueue<SocketPair>();

        // server loop
        ServerAcceptor serverLogin = new ServerAcceptor(gw);
        worldTcpPort = serverLogin.GetSocketPort();
        Thread thread = new Thread(new ThreadStart(serverLogin.WorldAcceptLoop));
        thread.Start();

        GameDebug.Log("World " + gw.WorldName + " awaiting connections on port " + worldTcpPort);
    }

    /// <summary>
    /// Called asynchronously. Addes a new client to list; client will be synced in the next SendData()
    /// </summary>
    /// <param name="st"></param>
    internal void AsyncAddNewClientConnection(SocketPair st)
    {
        clientsConnecting_.Enqueue(st);
    }

    /// <summary>
    /// Called when a client's SocketPair is shutdown (due to Client DCing)
    /// Despawns a player and removes his SocketPair from the clients' list
    /// </summary>
    /// <param name="st"></param>
    internal void RemoveAndDespawnClientConnection(SocketPair st)
    {
        //delete player entity
        // TODO DB
        DespawnRD despawnPlayer = new DespawnRD(st.GetRemotePlayerUid());
        em_.AsyncCreateTempEvent(despawnPlayer);
        clients_.Remove(st);
    }

    /// <summary>
    /// Called on fixedUpdate. It sends the poses of ALL entities (unless they dont fit in a single message)
    /// Also sends any new events in the EM. Must be called before EM update, which wipes events
    /// </summary>
    internal void SendData()
    {
        //GameDebug.Log("NS SendData " + em_.permanentEntities.Count+"  "+ em_.tempUnitEntities.Count+"   "+em_.tempEffectEntities.Count);
        // TODO we assume all permanent entities always fit in buffer. this should be changed, and we should cycle through them as well. however, that means they will need IDs

        byte[] unreliableDataBytes = SyncUdpUpdates();

        // send all current tempEntities to clients connecting
        SyncConnectingClients();

        // send all new events to connected clients
        byte[][] reliableDataBytes = SyncTcpUpdates();

        // send all data to connected clients
        for (int clientIndex = 0; clientIndex < clients_.Count; clientIndex++)
        {
            clients_[clientIndex].SendData(unreliableDataBytes, reliableDataBytes[clientIndex]);
        }
    }

    /// <summary>
    /// Close NS and all associated client connections
    /// </summary>
    internal void Close()
    {
        foreach (SocketPair nt in clients_)
        {
            nt.Close();
        }
    }

    /// <summary>
    /// Cycles through existing entities and prepares their poses to be sent through UDP
    /// </summary>
    /// <returns>The UDP updates.</returns>
    private byte[] SyncUdpUpdates()
    {
        int spaceOccupiedByPermEntities = em_.GetPermEntitiesCount() * UnidentifiedURD.kSize;
        int tempEntitiesThatFitInBuf = Math.Min(em_.GetTempUnitEntitiesCount(), (NetworkGlobals.kBufSize - spaceOccupiedByPermEntities - 4) / IdentifiedURD.kSize); //subtract permEntities and timestamp
        int entitiesThatFitInBuf = em_.GetPermEntitiesCount() + tempEntitiesThatFitInBuf;
        int lenUnreliableDataBytes = 0;
        byte[][] bytesToSendUdp = new byte[entitiesThatFitInBuf][];

        // first, get pose of all permanent entities. convert to byte to get forwarded, and add to list to be processed
        int currEntityindex = 0;
        for (int counter = 0; counter < em_.GetPermEntitiesCount(); counter++, currEntityindex++)
        {
            Transform transf = em_.permanentEntities[counter].GetComponent(typeof(Transform)) as Transform;
            // TODO sending zero'd information that permanent entities don't have
            UnreliableData urd = new UnidentifiedURD(transf.position, Vector3.zero, transf.rotation, 0, 0);
            byte[] urdBytes = urd.ToBytes();
            bytesToSendUdp[currEntityindex] = urdBytes;
            lenUnreliableDataBytes += urdBytes.Length;
        }

        // second, get pose of <however many still fit> temp entities. convert to byte to get forwarded, and add to list to be processed
        int index = Math.Max(0, Math.Min(entityPoseCyclicIndex_, em_.GetTempUnitEntitiesCount() - 1));
        List<int> myKeys = em_.tempUnitEntities.Keys.ToList();
        // sends up to tempEntitiesThatFitInBuf poses, or the amount of entities in tempEntities
        for (int counter = 0; counter < tempEntitiesThatFitInBuf; counter++, index = (index + 1) % em_.GetTempUnitEntitiesCount(), currEntityindex++)
        {
            UnitEntity unit = em_.tempUnitEntities[myKeys[index]];
            //GameDebug.Log("index=" + index+",   counter="+counter);
            //GameDebug.Log("key=" + myKeys[index]);
            Transform transf = unit.UnitTransform();
            BaseControllerKin controller = unit.Controller;
            IdentifiedURD urd = new IdentifiedURD(
                myKeys[index],
                transf.position,
                controller.GetMotorSpeed(),
                transf.rotation,
                unit.UnitAnimator.CurrentAnimatorState,
                unit.LastEventId
            );
            //GameDebug.Log("urd=" + urd);
            byte[] urdBytes = urd.ToBytes();
            bytesToSendUdp[currEntityindex] = urdBytes;
            lenUnreliableDataBytes += urdBytes.Length;
        }
        entityPoseCyclicIndex_ = index;

        // dont send anything if no new events
        byte[] unreliableDataBytes = null;
        if (lenUnreliableDataBytes != 0)
        {
            // copy all event bytes to single array
            unreliableDataBytes = new byte[lenUnreliableDataBytes];
            int currentOffset = 0;
            foreach (byte[] bytes in bytesToSendUdp)
            {
                Buffer.BlockCopy(bytes, 0, unreliableDataBytes, currentOffset, bytes.Length);
                currentOffset += bytes.Length;
            }
        }
        else
        {
            unreliableDataBytes = new byte[0];
        }

        return unreliableDataBytes;
    }

    /// <summary>
    /// Iterates over all clients that have connected since the last SendData() and syncs them
    /// All necessary events/temp entities are grouped up and sent to them. Clients are moved to "clientsAdded" list
    /// </summary>
    private void SyncConnectingClients()
    {
        while (!clientsConnecting_.IsEmpty)
        {
            int lenReliableDataBytesCC = 0;
            byte[][] bytesToSendCC = new byte[em_.GetTempUnitEntitiesCount() + em_.GetTempEffectEntitiesCount() + em_.GetTempBuffEntitiesCount()][];
            clientsConnecting_.TryDequeue(out SocketPair st);

            // convert all entities (except player) to byte[]
            for (int indexCC = 0; indexCC < em_.GetTempUnitEntitiesCount(); indexCC++)
            {
                KeyValuePair<int, UnitEntity> kvp = em_.tempUnitEntities.ElementAt(indexCC);

                // dont send player to himself
                if (kvp.Key == st.GetRemotePlayerUid())
                    continue;

                UnitEntity entity = kvp.Value;

                // send each entity to player
                SpawnRD entityRd = new SpawnRD(
                    kvp.Key,
                    entity.Name,
                    entity.Type,
                    entity.Health,
                    entity.MaxHealth,
                    entity.UnitTransform().position,
                    entity.UnitTransform().rotation,
                    entity.LastEventId
                );
                byte[] rdBytes = entityRd.ToBytes();
                bytesToSendCC[indexCC] = rdBytes;
                lenReliableDataBytesCC += rdBytes.Length;
                GameDebug.Log("Sending to connecting player: " + entityRd.ToString());
            }

            // convert all effects to byte[]
            int prevCount = em_.GetTempUnitEntitiesCount();
            for (int indexCC = 0; indexCC < em_.GetTempEffectEntitiesCount(); indexCC++)
            {
                KeyValuePair<int, EffectEntity> kvp = em_.tempEffectEntities.ElementAt(indexCC);
                // send each entity to player
                CreateRD entityRd = new CreateRD(
                    kvp.Key,
                    kvp.Value.Name,
                    kvp.Value.Type,
                    kvp.Value.CreatorUid,
                    kvp.Value.EffectTransform().position,
                    kvp.Value.EffectTransform().rotation
                );
                byte[] rdBytes = entityRd.ToBytes();
                bytesToSendCC[indexCC + prevCount] = rdBytes;
                lenReliableDataBytesCC += rdBytes.Length;
                GameDebug.Log("Sending to connecting player: " + entityRd.ToString());
            }

            // convert all buffs to byte[]
            prevCount += em_.GetTempEffectEntitiesCount();
            for (int indexCC = 0; indexCC < em_.GetTempBuffEntitiesCount(); indexCC++)
            {
                KeyValuePair<int, BuffEntity> kvp = em_.tempBuffEntities.ElementAt(indexCC);
                // send each entity to player
                BuffRD entityRd = new BuffRD(kvp.Key, kvp.Value.caster.Uid, kvp.Value.target.Uid, kvp.Value.Type);
                byte[] rdBytes = entityRd.ToBytes();
                bytesToSendCC[indexCC + prevCount] = rdBytes;
                lenReliableDataBytesCC += rdBytes.Length;
                GameDebug.Log("Sending to connecting player: " + entityRd.ToString());
            }

            // join everything and send
            byte[] reliableDataBytesCC = new byte[lenReliableDataBytesCC];
            int currentOffset = 0;
            foreach (byte[] bytes in bytesToSendCC)
            {
                if (bytes == null)
                    continue;

                Buffer.BlockCopy(bytes, 0, reliableDataBytesCC, currentOffset, bytes.Length);
                currentOffset += bytes.Length;
            }
            //GameDebug.Log("Sending to connecting player " + lenReliableDataBytesCC+" bytes");
            st.SendData(null, reliableDataBytesCC);

            // add client to list of clients. it is now fully up-to-date
            clients_.Add(st);
        }
    }

    /// <summary>
    /// Checks all new events in AsyncQueue, converts them to bytes, and returns them
    /// </summary>
    /// <returns>The TCP updates (a custom byte[] for each connected client).</returns>
    private byte[][] SyncTcpUpdates()
    {
        // Move events out of async queue and into eventsSent list, and convert them to byte[] to broadcast to clients
        int eventsToProcess = em_.eventsReceived.Count;
        em_.eventsSent = new ReliableData[eventsToProcess];
        byte[][] bytesToSendTcp = new byte[eventsToProcess][];
        for (int i = 0; i < eventsToProcess; i++)
        {
            // no need to check if return true, since no one else dequeues
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1.trydequeue?view=netframework-4.8
            em_.eventsReceived.TryDequeue(out ReliableData rd);

            em_.eventsSent[i] = rd;
            //GameDebug.Log("Broadcasting " + rd.ToString());

            byte[] rdBytes = rd.ToBytes();
            bytesToSendTcp[i] = rdBytes;
        }

        byte[][] reliableDataBytes = new byte[clients_.Count][];

        // send reliable events for each client
        // these may differ for each client, based on they see or NoOps being received for invalid actions
        for (int clientIndex = 0; clientIndex < clients_.Count; clientIndex++)
        {
            // first count how many bytes to send
            int bytesToSendToThisClient = 0;
            for (int eventIndex = 0; eventIndex < eventsToProcess; eventIndex++)
            {
                // a NoOp is a response to an invalid cast
                // send NoOp to clients that asked for invalid action. this way, they know their action was invalid, and they can request another
                if (
                    em_.eventsSent[eventIndex].mc == ReliableData.TcpMessCode.Noop
                    && (em_.eventsSent[eventIndex] as NoopRD).uid_src != clients_[clientIndex].GetRemotePlayerUid()
                )
                    continue;
                bytesToSendToThisClient += bytesToSendTcp[eventIndex].Length;
            }
            byte[] reliableDataBytesPerClient = new byte[bytesToSendToThisClient];

            // then copy bytes for each client
            int currentOffset = 0;
            for (int eventIndex = 0; eventIndex < eventsToProcess; eventIndex++)
            {
                // send NoOp to clients that asked for invalid action. this way, they know their action was not valid, and they can request another
                if (
                    em_.eventsSent[eventIndex].mc == ReliableData.TcpMessCode.Noop
                    && (em_.eventsSent[eventIndex] as NoopRD).uid_src != clients_[clientIndex].GetRemotePlayerUid()
                )
                    continue;

                Buffer.BlockCopy(bytesToSendTcp[eventIndex], 0, reliableDataBytesPerClient, currentOffset, bytesToSendTcp[eventIndex].Length);
                currentOffset += bytesToSendTcp[eventIndex].Length;
            }

            reliableDataBytes[clientIndex] = reliableDataBytesPerClient;
        }

        return reliableDataBytes;
    }
}
