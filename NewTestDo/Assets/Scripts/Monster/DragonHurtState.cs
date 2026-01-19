using UnityEngine;

/// <summary>
/// ÊÜ»÷×´Ì¬
/// </summary>
public class DragonHurtState : DragonState
{
    public DragonHurtState(DragonStateMachine stateMachine, FlyingDragonController dragonController)
        : base(stateMachine, dragonController) { }

    public override void Enter()
    {
        dragonController.TriggerAnimation(FlyingDragonController.ANIM_HURT);
        dragonController.HurtTimer = dragonController.HurtDuration;
    }

    public override void Update()
    {
        dragonController.HurtTimer -= Time.deltaTime;

        if (dragonController.HurtTimer <= 0)
        {
            stateMachine.ReturnToPreviousState();
        }
    }
}