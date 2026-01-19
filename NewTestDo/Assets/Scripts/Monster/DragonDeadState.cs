using UnityEngine;

/// <summary>
/// 死亡状态
/// </summary>
public class DragonDeadState : DragonState
{
    public DragonDeadState(DragonStateMachine stateMachine, FlyingDragonController dragonController)
        : base(stateMachine, dragonController) { }

    public override void Enter()
    {
        dragonController.TriggerAnimation(FlyingDragonController.ANIM_DEAD);

        if (dragonController.Rb != null)
        {
            dragonController.Rb.isKinematic = true;
        }

        // 可以在这里添加死亡效果，比如粒子特效、声音等
        Debug.Log("Dragon is dead!");
    }

    public override void Update()
    {
        // 死亡状态不执行任何操作
        // 可以添加尸体消失的计时器
    }
}