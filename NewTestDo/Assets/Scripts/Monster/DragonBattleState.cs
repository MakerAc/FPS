using UnityEngine;

/// <summary>
/// Õ½¶·×´Ì¬
/// </summary>
public class DragonBattleState : DragonState
{
    public DragonBattleState(DragonStateMachine stateMachine, FlyingDragonController dragonController)
        : base(stateMachine, dragonController) { }

    public override void Enter()
    {
        dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, true);
        dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, false);
    }

    public override void Update()
    {
        if (dragonController.PlayerTarget == null)
        {
            stateMachine.SwitchState(DragonStateMachine.DragonStateType.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(dragonController.transform.position,
                                                dragonController.PlayerTarget.position);

        if (dragonController.IsPlayerInAttackRange())
        {
            // ¹¥»÷Íæ¼Ò
            if (dragonController.AttackTimer <= 0)
            {
                dragonController.TriggerAnimation(FlyingDragonController.ANIM_ATTACK);
                dragonController.AttackTimer = dragonController.AttackCooldown;
                dragonController.Attack();
            }
        }
        else
        {
            // ×·»÷Íæ¼Ò
            dragonController.MoveTowards(dragonController.PlayerTarget.position, dragonController.ChaseSpeed);
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            dragonController.TakeDamage(10);
        }
    }
}