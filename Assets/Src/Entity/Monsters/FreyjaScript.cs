using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class FreyjaScript : MonoBehaviour, IConfigurableMonster
{
    private MonsterControllerKin controller_;
    private MonsterCastValidator validator_;
    private EntityManager em_;
    private int uid_;
    private Vector3 spawn_point_;
    private UnitEntity parent_;

    private int valkyrie_uid_;
    private int[] valkyrie_uids_25_;
    private int[] dryads_uids_;
    private int dryad_counter_;
    private List<int> poison_zones_;

    private readonly System.Random random_ = new System.Random();

    private const float kAggroRange = 15;
    private const float kSpawnPointRadius = 1;
    private const float kMoveIfFurtherAwayThan = 3;

    private UnityEngine.AI.NavMeshAgent nav_agent_;

    private int last_hp_when_poison_zone_was_cast_;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        spawn_point_ = transform.position;
        nav_agent_ = GetComponent<UnityEngine.AI.NavMeshAgent>();

        valkyrie_uid_ = 0;
        dryads_uids_ = new int[10];
        dryad_counter_ = 0;
        valkyrie_uids_25_ = null;
        last_hp_when_poison_zone_was_cast_ = parent_.MaxHealth;
        poison_zones_ = new List<int>();
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        (UnitEntity closestEntity, float closestEntityDistance, Vector3 closestEntityDirection) = FindEntityUtils.FindClosestEntity(
            em_,
            transform.position,
            spawn_point_,
            kAggroRange,
            new int[] { uid_ }
        );
        closestEntityDirection.y = 0;

        if (parent_.IsDead)
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
            // isAggroed_ = false;
        }
        else if (parent_.IsStunned)
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
        }
        else
        {
            // move there
            if (closestEntity == null)
            {
                if (parent_.Health != parent_.MaxHealth)
                    em_.AsyncCreateTempEvent(new CombatEffectRD(uid_, uid_, CastCode.Regen, parent_.MaxHealth));

                last_hp_when_poison_zone_was_cast_ = parent_.MaxHealth;

                nav_agent_.SetDestination(spawn_point_);
                MoveToSpawn();
            }
            else
            {
                nav_agent_.SetDestination(closestEntity.UnitTransform().position);
                MoveToEnemy(closestEntityDistance, closestEntityDirection);

                // Attacks enemy
                ProcessAttacks(closestEntity, closestEntityDistance);
            }
        }

        // Spawn Valkyrie at 75%
        ProcessValkyrie1();

        // Spawn Dryads at 50%
        ProcessDryads();

        // Spawn 2 Valkyrie at 25%
        ProcessValkyries2();

        // Spawn PoisonZone at every 5%
        ProcessPoisonZones();
    }

    /// <summary>
    /// Freyja moves to spawn location
    /// </summary>
    private void MoveToSpawn()
    {
        Vector3 toSpawnPoint = spawn_point_ - transform.position;
        toSpawnPoint.y = 0;
        if (toSpawnPoint.magnitude > kSpawnPointRadius)
        {
            AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = nav_agent_.desiredVelocity.normalized, LookVector = nav_agent_.desiredVelocity };
            controller_.SetInputs(ref characterInputs);
            parent_.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
            parent_.SetAnimatorState(EntityAnimation.kIdle);
        }
    }

    /// <summary>
    /// Freyja moves to some place and might attack
    /// </summary>
    /// <param name="closestEntityDistance">distance of closest enemy</param>
    /// <param name="closestEntityDirection">direction of closest enemy</param>
    private void MoveToEnemy(float closestEntityDistance, Vector3 closestEntityDirection)
    {
        if (closestEntityDistance >= kMoveIfFurtherAwayThan)
        {
            AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = nav_agent_.desiredVelocity.normalized, LookVector = nav_agent_.desiredVelocity };
            controller_.SetInputs(ref characterInputs);
            parent_.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else
        {
            AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = Vector3.zero, LookVector = closestEntityDirection };
            controller_.SetInputs(ref characterInputs);
            parent_.SetAnimatorState(EntityAnimation.kIdle);
        }
    }

    /// <summary>
    /// Attacks an enemy if close enough
    /// </summary>
    /// <param name="closestEntity">closest enemy</param>
    /// <param name="closestEntityDistance">distance of closest enemy</param>
    private void ProcessAttacks(UnitEntity closestEntity, float closestEntityDistance)
    {
        // if within range, damage it
        if (closestEntityDistance < MonsterCastValidator.kAttackLeftRange)
        {
            if (validator_.CanAttackLeft(currTime_ms))
            {
                ReliableData rd = CastUtils.MakeMonsterAttackLeft(uid_, closestEntity.Uid);
                em_.AsyncCreateTempEvent(rd);
            }
        }
    }

    /// <summary>
    /// Spawns/despawns a valkyrie
    /// </summary>
    private void ProcessValkyrie1()
    {
        if (!parent_.IsDead && parent_.Health < parent_.MaxHealth * 0.75 && valkyrie_uid_ == 0)
        {
            valkyrie_uid_ = em_.GetValidNpcUid();
            em_.AsyncCreateTempEvent(new SpawnRD(valkyrie_uid_, "Valkyrie", UnitEntityCode.kValkyrie, 100, 100, new Vector3(-20, 11, 100), Quaternion.identity, 0));
        }
        else if (
            (parent_.Health == parent_.MaxHealth || parent_.IsDead)
            && valkyrie_uid_ != 0
            && em_.tempUnitEntities[valkyrie_uid_].GameObject.GetComponent<ValkyrieScript>().carryingTarget_ == null
        )
        {
            em_.AsyncCreateTempEvent(new DespawnRD(valkyrie_uid_));
            valkyrie_uid_ = 0;
        }
    }

    /// <summary>
    /// Spawns/despawns a group of dryads
    /// </summary>
    private void ProcessDryads()
    {
        if (!parent_.IsDead && parent_.Health < parent_.MaxHealth * 0.5 && dryad_counter_ < dryads_uids_.Length)
        {
            if (random_.NextDouble() < 0.05)
            {
                dryads_uids_[dryad_counter_] = em_.GetValidNpcUid();
                em_.AsyncCreateTempEvent(
                    new SpawnRD(
                        dryads_uids_[dryad_counter_],
                        "Dryad",
                        UnitEntityCode.kDryad,
                        10,
                        10,
                        new Vector3(-20 + random_.Next(-3, 3), 11, 95 + dryad_counter_),
                        Quaternion.identity,
                        0
                    )
                );
                dryad_counter_++;
            }
        }
        else if ((parent_.Health == parent_.MaxHealth || parent_.IsDead) && dryad_counter_ != 0)
        {
            for (int i = 0; i < dryad_counter_; i++)
            {
                em_.AsyncCreateTempEvent(new DespawnRD(dryads_uids_[i]));
            }
            dryad_counter_ = 0;
        }
    }

    /// <summary>
    /// Spawns/despawns a pair of valkyries
    /// </summary>
    private void ProcessValkyries2()
    {
        if (!parent_.IsDead && parent_.Health < parent_.MaxHealth * 0.25 && valkyrie_uids_25_ == null)
        {
            valkyrie_uids_25_ = new int[2];
            for (int i = 0; i < valkyrie_uids_25_.Length; i++)
            {
                valkyrie_uids_25_[i] = em_.GetValidNpcUid();
                em_.AsyncCreateTempEvent(
                    new SpawnRD(valkyrie_uids_25_[i], "Valkyrie", UnitEntityCode.kValkyrie, 100, 100, new Vector3(-20, 11 + i, 75), Quaternion.identity, 0)
                );
            }
        }
        else if ((parent_.Health == parent_.MaxHealth || parent_.IsDead) && valkyrie_uids_25_ != null)
        {
            bool allReset = true;
            for (int i = 0; i < valkyrie_uids_25_.Length; i++)
            {
                if (valkyrie_uids_25_[i] != 0)
                {
                    if (em_.tempUnitEntities[valkyrie_uids_25_[i]].GameObject.GetComponent<ValkyrieScript>().carryingTarget_ == null)
                    {
                        em_.AsyncCreateTempEvent(new DespawnRD(valkyrie_uids_25_[i]));
                        valkyrie_uids_25_[i] = 0;
                    }
                    else
                    {
                        // a valkyrie is stil carrying someone, so we cant despawn it
                        allReset = false;
                    }
                }
            }
            if (allReset)
                valkyrie_uids_25_ = null;
        }
    }

    /// <summary>
    /// Creates/destroys PoisonZones
    /// </summary>
    private void ProcessPoisonZones()
    {
        if (!parent_.IsDead && parent_.Health <= last_hp_when_poison_zone_was_cast_ - parent_.MaxHealth * 0.05)
        {
            // pick a random valid target (if one exists)
            List<UnitEntity> players = FindEntityUtils.FindEntitiesWithinRadius(em_, spawn_point_, kAggroRange, new int[] { uid_ });
            if (players.Count == 0)
            {
                return;
            }
            UnitEntity target = players[random_.Next(players.Count)];

            bool spawnZone = true;
            foreach (var zone_uid in poison_zones_)
            {
                if ((em_.tempEffectEntities[zone_uid].GameObject.transform.position - target.UnitTransform().position).magnitude < 1)
                {
                    spawnZone = false;
                    break;
                }
            }
            if (spawnZone)
            {
                last_hp_when_poison_zone_was_cast_ = parent_.Health;
                int valid_uid = em_.GetValidNpcUid();
                em_.AsyncCreateTempEvent(CastUtils.MakePoisonZone(uid_));

                em_.AsyncCreateTempEvent(new CreateRD(valid_uid, "PoisonZone", EffectEntityCode.kPoisonZone, uid_, target.UnitTransform().position, Quaternion.identity));
                poison_zones_.Add(valid_uid);
            }
        }
        else if ((parent_.Health == parent_.MaxHealth || parent_.IsDead) && poison_zones_.Count != 0)
        {
            foreach (var zone_uid in poison_zones_)
            {
                em_.AsyncCreateTempEvent(new DestroyRD(zone_uid));
            }
            poison_zones_.Clear();
            last_hp_when_poison_zone_was_cast_ = parent_.MaxHealth;
        }
    }

    /// <summary>
    /// Configures Monster with its corresponding unit entity
    /// </summary>
    /// <param name="parent">the parent entity</param>
    public void Config(UnitEntity parent)
    {
        controller_ = (MonsterControllerKin)parent.Controller;
        parent_ = parent;
        validator_ = (MonsterCastValidator)parent.Validator;
        em_ = parent.EntityManager;
        uid_ = parent.Uid;
    }
}
