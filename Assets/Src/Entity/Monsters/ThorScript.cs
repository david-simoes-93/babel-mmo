using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class ThorScript : MonoBehaviour, IConfigurableMonster
{
    private MonsterControllerKin controller_;
    private MonsterCastValidator validator_;
    private EntityManager em_;
    private int uid_;
    private Vector3 spawn_point_;
    private Vector3 cloud_spawn_point_;
    private UnitEntity parent_;

    private readonly System.Random random_ = new System.Random();

    private const float kAggroRange = 15;
    private const float kSpawnPointRadius = 1;
    private const float kMoveIfFurtherAwayThan = 3;

    private UnityEngine.AI.NavMeshAgent nav_agent_;

    private int lastSlamHealth_;
    private int cloudUid_;
    private long lastChainLightningTime_;

    private long aggroTime_;
    private bool isAggroed_;

    private long lastMagnetTime_;
    private List<int> magnet_uids_;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        spawn_point_ = transform.position;
        cloud_spawn_point_ = spawn_point_ + new Vector3(0, 5, 0);
        nav_agent_ = GetComponent<UnityEngine.AI.NavMeshAgent>();

        lastSlamHealth_ = parent_.MaxHealth;
        cloudUid_ = 0;
        lastChainLightningTime_ = Globals.currTime_ms;
        aggroTime_ = Globals.currTime_ms;
        magnet_uids_ = new List<int>();
        lastMagnetTime_ = Globals.currTime_ms;
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        List<int> entities_to_ignore = new List<int>(magnet_uids_);
        entities_to_ignore.Add(uid_);
        (UnitEntity closestEntity, float closestEntityDistance, Vector3 closestEntityDirection) = FindEntityUtils.FindClosestEntity(
            em_,
            transform.position,
            spawn_point_,
            kAggroRange,
            entities_to_ignore.ToArray()
        );
        closestEntityDirection.y = 0;

        if (parent_.IsDead)
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
            isAggroed_ = false;
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
                {
                    em_.AsyncCreateTempEvent(new CombatEffectRD(uid_, uid_, CastCode.Regen, parent_.MaxHealth));
                    lastSlamHealth_ = parent_.MaxHealth;
                }

                nav_agent_.SetDestination(spawn_point_);
                MoveToSpawn();
                isAggroed_ = false;
            }
            else
            {
                // Was aggroed this frame
                if (!isAggroed_)
                {
                    aggroTime_ = Globals.currTime_ms;
                }
                isAggroed_ = true;
                nav_agent_.SetDestination(closestEntity.UnitTransform().position);
                MoveToEnemy(closestEntityDistance, closestEntityDirection);

                // Attacks enemy
                ProcessAttacks(closestEntity, closestEntityDistance);
            }
        }

        // AoE
        if (closestEntity != null)
            ProcessSlam();

        // spawn adds that wont move
        ProcessMagnet();

        // chain lightning
        if (closestEntity != null)
            ProcessChainLightning(closestEntity);

        // cloud slowly rotating around the room
        ProcessCloud(closestEntity);
    }

    /// <summary>
    /// Spawns/despawns a magnet
    /// </summary>
    private void ProcessMagnet()
    {
        // at least 1s since last Chain Lightning
        // at least 10s since aggro
        // every 10s
        if (
            !parent_.IsDead
            && isAggroed_
            && lastMagnetTime_ + 10000 < Globals.currTime_ms
            && magnet_uids_.Count < 10
            && lastChainLightningTime_ + 1000 < Globals.currTime_ms
            && aggroTime_ + 10000 < Globals.currTime_ms
        )
        {
            magnet_uids_.Add(em_.GetValidNpcUid());
            em_.AsyncCreateTempEvent(
                new SpawnRD(
                    magnet_uids_[magnet_uids_.Count - 1],
                    "Magnet" + magnet_uids_.Count,
                    UnitEntityCode.kMagnet,
                    100,
                    100,
                    new Vector3(31 + random_.Next(-10, 10), 15, 102 + random_.Next(-10, 10)),
                    Quaternion.identity,
                    0
                )
            );
            lastMagnetTime_ = Globals.currTime_ms;
        }
        else if (!isAggroed_ && magnet_uids_.Count != 0)
        {
            foreach (int magnet_uid in magnet_uids_)
            {
                em_.AsyncCreateTempEvent(new DespawnRD(magnet_uid));
            }
            magnet_uids_.Clear();
        }
    }

    /// <summary>
    /// Casts Chain Lightning
    /// </summary>
    private void ProcessChainLightning(UnitEntity closestEntity)
    {
        if (!parent_.IsDead && lastChainLightningTime_ + 5000 < Globals.currTime_ms && aggroTime_ + 10000 < Globals.currTime_ms)
        {
            List<int> targets = new List<int>();
            targets.Add(closestEntity.Uid);
            UnitEntity last_target = closestEntity;
            // up to 5 targets
            while (targets.Count < 5)
            {
                List<UnitEntity> next_possible_targets = FindEntityUtils.FindEntitiesWithinRadius(em_, last_target.UnitTransform().position, 10, new int[] { uid_ });
                bool keep_adding = false;
                foreach (UnitEntity next_possible_target in next_possible_targets)
                {
                    // avoid repetitions
                    if (targets.Contains(next_possible_target.Uid))
                    {
                        continue;
                    }
                    // add target
                    targets.Add(next_possible_target.Uid);
                    last_target = next_possible_target;
                    keep_adding = true;
                    break;
                }
                // if no valid targets were found, then CL won't affect anyone else!
                if (!keep_adding)
                {
                    break;
                }
            }

            ReliableData rd = CastUtils.MakeThorChainLightning(uid_, targets.ToArray());
            em_.AsyncCreateTempEvent(rd);
            lastChainLightningTime_ = Globals.currTime_ms;
        }
    }

    /// <summary>
    /// Creates/destroys thunder cloud
    /// </summary>
    private void ProcessCloud(UnitEntity closestEntity)
    {
        if (!parent_.IsDead && closestEntity != null && cloudUid_ == 0)
        {
            int valid_uid = em_.GetValidNpcUid();
            em_.AsyncCreateTempEvent(new CreateRD(valid_uid, "ThunderCloud", EffectEntityCode.kThunderCloud, uid_, cloud_spawn_point_, Quaternion.identity));
            cloudUid_ = valid_uid;
        }
        else if ((closestEntity == null || parent_.IsDead) && cloudUid_ != 0)
        {
            em_.AsyncCreateTempEvent(new DestroyRD(cloudUid_));
            cloudUid_ = 0;
        }
    }

    /// <summary>
    /// Thor makes a Slam attack
    /// </summary>
    private void ProcessSlam()
    {
        if (!parent_.IsDead && parent_.Health < lastSlamHealth_ - parent_.MaxHealth * 0.1)
        {
            float heightModifier = parent_.UnitTransform().position.y + 0.2f;
            ReliableData rd = CastUtils.MakeThorSlam(uid_, new Vector3(transform.position.x, heightModifier, transform.position.z), transform.rotation);
            em_.AsyncCreateTempEvent(rd);
            lastSlamHealth_ = parent_.Health;

            AICharacterInputs characterInputs = new AICharacterInputs
            {
                MoveVector = Vector3.zero,
                LookVector = nav_agent_.desiredVelocity,
                Jump = true
            };
            controller_.SetInputs(ref characterInputs);
            controller_.AddVelocity(new Vector3(0, 10f, 0));
        }
    }

    /// <summary>
    /// Thor moves to spawn location
    /// </summary>
    private void MoveToSpawn()
    {
        Vector3 toSpawnPoint = spawn_point_ - transform.position;
        toSpawnPoint.y = 0;
        if (toSpawnPoint.magnitude > kSpawnPointRadius)
        {
            AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = nav_agent_.desiredVelocity.normalized, LookVector = nav_agent_.desiredVelocity };
            controller_.SetInputs(ref characterInputs);
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kIdle);
        }
    }

    /// <summary>
    /// Thor moves to some place and might attack
    /// </summary>
    /// <param name="closestEntityDistance">distance of closest enemy</param>
    /// <param name="closestEntityDirection">direction of closest enemy</param>
    private void MoveToEnemy(float closestEntityDistance, Vector3 closestEntityDirection)
    {
        if (closestEntityDistance >= kMoveIfFurtherAwayThan)
        {
            AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = nav_agent_.desiredVelocity.normalized, LookVector = nav_agent_.desiredVelocity };
            controller_.SetInputs(ref characterInputs);
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else
        {
            AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = Vector3.zero, LookVector = closestEntityDirection };
            controller_.SetInputs(ref characterInputs);
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kIdle);
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
                float heightModifier = closestEntity.UnitTransform().position.y + 1;
                ReliableData rd = CastUtils.MakeMonsterAttackLeft(uid_, closestEntity.Uid);
                em_.AsyncCreateTempEvent(rd);
            }
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
