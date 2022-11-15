using UnityEngine;
using System.Collections.Generic;
using static Globals;

internal class ValkyrieScript : MonoBehaviour, IConfigurableMonster
{
    private MonsterControllerKin controller_;
    private MonsterCastValidator validator_;
    private EntityManager em_;
    private int uid_;
    private UnitEntity parent_;

    internal UnitEntity carryingTarget_ { get; private set; }

    private Vector3 spawn_point_ = new Vector3(-20, 5, 45);

    private const float kAttackRange = 3;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        carryingTarget_ = null;
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        // reset target
        if (carryingTarget_ != null && carryingTarget_.GameObject == null)
            carryingTarget_ = null;

        if (parent_.IsDead || parent_.IsStunned)
        {
            controller_.SetInputs(ref MonsterControllerKin.kBeStill);
        }
        else if (carryingTarget_ == null)
        {
            (UnitEntity closestEntity, float closestEntityDistance, Vector3 closestEntityDirection) = FindEntityUtils.FindClosestEntity(
                em_,
                transform.position,
                spawn_point_,
                20,
                new int[] { uid_ }
            );

            // move there
            if (closestEntity != null && closestEntity.IsAttackable && closestEntity.LeashedBy == null)
            {
                MoveToEnemy(closestEntityDistance, closestEntityDirection);
                ProcessAttack(closestEntity, closestEntityDistance);
            }
            else
            {
                MoveToSpawn();
            }
        }

        CarryEnemy();
    }

    /// <summary>
    /// Valkyries moves to spawn location
    /// </summary>
    private void MoveToSpawn()
    {
        controller_.SetInputs(ref MonsterControllerKin.kBeStill);
        parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kIdle);
    }

    /// <summary>
    /// Attacks an enemy if close enough
    /// </summary>
    /// <param name="closestEntity">closest enemy</param>
    /// <param name="closestEntityDistance">distance of closest enemy</param>
    private void ProcessAttack(UnitEntity closestEntity, float closestEntityDistance)
    {
        // if within range, damage it
        if (closestEntityDistance < kAttackRange)
        {
            ReliableData rd = CastUtils.MakeStunCarry(uid_, closestEntity.Uid);
            em_.AsyncCreateTempEvent(rd);
            carryingTarget_ = closestEntity;
        }
    }

    /// <summary>
    /// Casts StunCarry or DropCarried
    /// </summary>
    private void CarryEnemy()
    {
        if (carryingTarget_ == null)
            return;

        if (
            !parent_.IsDead
            && !carryingTarget_.IsDead
            && carryingTarget_.IsAttackable
            && (Mathf.Abs(parent_.UnitTransform().position.x + 20) < 20 || parent_.UnitTransform().position.y < 50)
        )
        {
            // move up!
            Vector3 lookVector = carryingTarget_.UnitTransform().position - transform.position;
            lookVector.y = 0;
            AICharacterInputs characterInputs = new AICharacterInputs
            {
                MoveVector = new Vector3(parent_.UnitTransform().position.x < 0 ? -1 : 1, 1, 0),
                LookVector = lookVector
            };
            controller_.SetInputs(ref characterInputs);
            parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkForward);
        }
        else
        {
            ReliableData rd = CastUtils.MakeDropCarried(uid_, carryingTarget_.Uid);
            em_.AsyncCreateTempEvent(rd);
            carryingTarget_ = null;
        }
    }

    /// <summary>
    /// Valkyrie moves to some place and might attack
    /// </summary>
    /// <param name="closestEntityDistance">distance of closest enemy</param>
    /// <param name="closestEntityDirection">direction of closest enemy</param>
    private void MoveToEnemy(float closestEntityDistance, Vector3 closestEntityDirection)
    {
        Vector3 lookVector = new Vector3(closestEntityDirection.x, 0, closestEntityDirection.z);
        AICharacterInputs characterInputs = new AICharacterInputs { MoveVector = closestEntityDirection.normalized, LookVector = lookVector };
        controller_.SetInputs(ref characterInputs);
        parent_.UnitAnimator.SetAnimatorState(EntityAnimation.kWalkForward);
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
