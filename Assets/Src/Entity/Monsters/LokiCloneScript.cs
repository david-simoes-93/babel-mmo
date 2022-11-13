using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class LokiCloneScript : MonoBehaviour, IConfigurableMonster
{
    private MonsterControllerKin controller_;
    private MonsterCastValidator validator_;
    private EntityManager em_;
    private int uid_;
    private Vector3 spawn_point_;
    private UnitEntity parent_;

    private const float kAggroRange = 20;
    private const float kSpawnPointRadius = 1;
    private const float kMoveIfFurtherAwayThan = 10;

    private UnityEngine.AI.NavMeshAgent nav_agent_;

    private long aggroTime_;
    private bool isAggroed_;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        // TODO spawn point is now whatever random position this clone was spawned at; should be Loki's spawn point
        // kAggroRange has been decreased by 10 to compensate
        spawn_point_ = transform.position;
        nav_agent_ = GetComponent<UnityEngine.AI.NavMeshAgent>();
        aggroTime_ = Globals.currTime_ms;
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
