using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class DryadScript : MonoBehaviour, IConfigurableMonster
{
    private MonsterControllerKin controller_;
    private MonsterCastValidator validator_;
    private EntityManager em_;
    private int uid_;
    private Vector3 spawn_point_;
    private UnitEntity parent_;

    private const float kMoveIfFurtherAwayThan = 2;
    private const float kAttackRange = 3;

    private UnityEngine.AI.NavMeshAgent nav_agent_;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        spawn_point_ = transform.position;
        nav_agent_ = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        if (parent_.IsDead || parent_.IsStunned)
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
        }
        else
        {
            (UnitEntity closestEntity, float closestEntityDistance, Vector3 closestEntityDirection) = FindEntityUtils.FindClosestEntity(
                em_,
                transform.position,
                new int[] { uid_ }
            );
            closestEntityDirection.y = 0;

            // move there
            if (closestEntity != null)
            {
                nav_agent_.SetDestination(closestEntity.UnitTransform().position);
                MoveToEnemy(closestEntityDistance, closestEntityDirection);

                // Attacks enemy
                ProcessAttacks(closestEntity, closestEntityDistance);
            }
            else
            {
                controller_.SetInputs(ref MonsterControllerKin.kBeStill);
                parent_.SetAnimatorState(EntityAnimation.kIdle);
            }
        }
    }

    /// <summary>
    /// Dryad moves to some place and might attack
    /// </summary>
    /// <param name="closestEntityDistance">distance of closest enemy</param>
    /// <param name="closestEntityDirection">direction of closest enemy</param>
    private void MoveToEnemy(float closestEntityDistance, Vector3 closestEntityDirection)
    {
        if (closestEntityDistance >= kMoveIfFurtherAwayThan)
        {
            GameDebug.Log(nav_agent_.desiredVelocity);
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
        if (closestEntityDistance < kAttackRange)
        {
            if (validator_.CanSuicide(currTime_ms))
            {
                ReliableData rd = CastUtils.MakeSuicideBomb(uid_);
                em_.AsyncCreateTempEvent(rd);
            }
        }
    }

    /// <summary>
    /// Configures Monster with its corresponding unit entity
    /// </summary>
    /// <param name="parent"></param>
    public void Config(UnitEntity parent)
    {
        controller_ = (MonsterControllerKin)parent.Controller;
        parent_ = parent;
        validator_ = (MonsterCastValidator)parent.Validator;
        em_ = parent.EntityManager;
        uid_ = parent.Uid;
    }
}
