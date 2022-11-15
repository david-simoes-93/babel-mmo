using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class LokiScript : MonoBehaviour, IConfigurableMonster
{
    private MonsterControllerKin controller_;
    private MonsterCastValidator validator_;
    private EntityManager em_;
    private int uid_;
    private Vector3 spawn_point_;
    private Vector3 death_wave_spawn_point_right_,
        death_wave_spawn_point_left_;
    private Quaternion death_wave_facing_left_,
        death_wave_facing_right_;
    private UnitEntity parent_;

    private readonly System.Random random_ = new System.Random();

    private const float kAggroRange = 20;
    private const float kSpawnPointRadius = 1;
    private const float kMoveIfFurtherAwayThan = MonsterCastValidator.kRangedAttackRange - 1;

    private UnityEngine.AI.NavMeshAgent nav_agent_;

    private long aggroTime_;
    private bool isAggroed_;

    private long lastWaveTime_;
    private int waveUid_;

    private List<int> cloneUids_;
    private int lastSplitHealth_;
    private long lastSplitTime_;
    private const int kClonesSpawned = 3;
    private int splitPhase = 0;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        spawn_point_ = transform.position;
        death_wave_spawn_point_right_ = spawn_point_ + new Vector3(0, -3.5f, 20);
        death_wave_facing_left_ = Quaternion.Euler(0, 90, 0);
        death_wave_spawn_point_left_ = spawn_point_ + new Vector3(0, -3.5f, -20);
        death_wave_facing_right_ = Quaternion.Euler(0, -90, 0);
        nav_agent_ = GetComponent<UnityEngine.AI.NavMeshAgent>();

        waveUid_ = 0;
        aggroTime_ = Globals.currTime_ms;
        lastWaveTime_ = Globals.currTime_ms;
        lastSplitHealth_ = parent_.MaxHealth;
        lastSplitTime_ = Globals.currTime_ms;

        cloneUids_ = new List<int>();
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        List<int> entities_to_ignore = new List<int>(cloneUids_);
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
        else if (parent_.IsStunned || splitPhase == 1)
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

        // spawn Wave that kills everyone it touches
        ProcessDeathWave();

        // every 20%, spawn 3 clones in random places and also tp randomly
        ProcessSplit();
    }

    /// <summary>
    /// Spawns/despawns a magnet
    /// </summary>
    private void ProcessDeathWave()
    {
        // at least 10s since aggro
        // every 10s
        if (!parent_.IsDead && lastWaveTime_ + 10000 < Globals.currTime_ms && aggroTime_ + 10000 < Globals.currTime_ms && isAggroed_)
        {
            waveUid_ = em_.GetValidNpcUid();
            if (random_.NextDouble() < 0.5)
            {
                em_.AsyncCreateTempEvent(new CreateRD(waveUid_, "DeathWave", EffectEntityCode.kDeathWave, uid_, death_wave_spawn_point_right_, death_wave_facing_left_));
            }
            else
            {
                em_.AsyncCreateTempEvent(new CreateRD(waveUid_, "DeathWave", EffectEntityCode.kDeathWave, uid_, death_wave_spawn_point_left_, death_wave_facing_right_));
            }
            lastWaveTime_ = Globals.currTime_ms;
        }
        else if ((!isAggroed_ || lastWaveTime_ + 6000 < Globals.currTime_ms) && waveUid_ != 0)
        {
            em_.AsyncCreateTempEvent(new DestroyRD(waveUid_));
            waveUid_ = 0;
        }
    }

    private void ProcessSplit()
    {
        // every 20% hp
        if (!parent_.IsDead && splitPhase == 0 && parent_.Health <= lastSplitHealth_ - 0.2 * parent_.MaxHealth)
        {
            lastSplitHealth_ = parent_.Health;
            lastSplitTime_ = Globals.currTime_ms;

            ReliableData rd = CastUtils.MakeLokiSplitStart(uid_);
            em_.AsyncCreateTempEvent(rd);
            splitPhase = 1;
            return;
        }

        // 1s after Split
        if (!parent_.IsDead && splitPhase == 1 && Globals.currTime_ms >= lastSplitTime_ + 5000)
        {
            ReliableData rd = CastUtils.MakeLokiSplitEnd(uid_, spawn_point_ + new Vector3(random_.Next(-10, 10), 0, random_.Next(-10, 10)), transform.rotation);
            em_.AsyncCreateTempEvent(rd);

            for (int i = 0; i < kClonesSpawned; i++)
            {
                int cloneUid = em_.GetValidNpcUid();
                em_.AsyncCreateTempEvent(
                    new SpawnRD(
                        cloneUid,
                        "Loki (Clone)",
                        UnitEntityCode.kLokiClone,
                        (int)(0.1f * parent_.MaxHealth),
                        (int)(0.1f * parent_.MaxHealth),
                        spawn_point_ + new Vector3(random_.Next(-10, 10), 0, random_.Next(-10, 10)),
                        transform.rotation,
                        0
                    )
                );
                cloneUids_.Add(cloneUid);
            }
            splitPhase = 0;
            return;
        }

        if (!isAggroed_ && cloneUids_.Count != 0)
        {
            foreach (int clone_uid in cloneUids_)
            {
                em_.AsyncCreateTempEvent(new DespawnRD(clone_uid));
            }
            cloneUids_.Clear();
            splitPhase = 0;
            lastSplitHealth_ = parent_.MaxHealth;
        }
    }

    /// <summary>
    /// Loki moves to spawn location
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
    /// Loki moves to some place and might attack
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
        if (closestEntityDistance < MonsterCastValidator.kRangedAttackRange)
        {
            if (validator_.CanRangedAttack(currTime_ms))
            {
                ReliableData rd = CastUtils.MakeMonsterRangedAttack(uid_, closestEntity.Uid);
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
