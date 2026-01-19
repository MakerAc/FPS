using UnityEngine;

/// <summary>
/// 闲置状态
/// </summary>
public class DragonIdleState : DragonState
{
    private float idleTimer;
    private float currentWaitTime;
    private Vector3 randomDestination;
    private bool isMoving = false;
    private bool isReturningHome = false;
    private bool isCorrectingPosition = false; // 新增：位置矫正标志

    public DragonIdleState(DragonStateMachine stateMachine, FlyingDragonController dragonController)
        : base(stateMachine, dragonController) { }

    public override void Enter()
    {
        isMoving = false;
        isReturningHome = false;
        isCorrectingPosition = false; // 重置位置矫正标志

        // 设置动画
        dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, false);
        dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, true);

        // 设置随机等待时间
        currentWaitTime = dragonController.GetRandomIdleTime();
        idleTimer = currentWaitTime;
    }

    public override void Update()
    {
        // 优先检查玩家是否在检测范围内
        if (dragonController.IsPlayerInDetectionRange())
        {
            stateMachine.SwitchState(DragonStateMachine.DragonStateType.Battle);
            return;
        }

        // 新增：位置矫正检查 - 优先于其他逻辑
        if (ShouldCorrectPosition())
        {
            CorrectPosition();
            return;
        }

        // 原有逻辑：检查是否超出闲置范围
        if (dragonController.IsOutOfIdleRange())
        {
            ReturnToHome();
        }
        else
        {
            UpdateIdleBehavior();
        }
    }

    /// <summary>
    /// 新增：检查是否需要位置矫正
    /// </summary>
    private bool ShouldCorrectPosition()
    {
        float distanceToInitial = Vector3.Distance(dragonController.transform.position, dragonController.InitialPosition);
        return distanceToInitial > 20f && !isCorrectingPosition;
    }

    /// <summary>
    /// 新增：立即矫正位置
    /// </summary>
    private void CorrectPosition()
    {
        isCorrectingPosition = true;
        isMoving = true;

        // 设置飞行动画
        dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, true);
        dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, false);

        // 向初始位置移动
        dragonController.MoveTowards(dragonController.InitialPosition, dragonController.MoveSpeed * 1.5f); // 增加移动速度

        // 检查是否到达初始位置
        if (Vector3.Distance(dragonController.transform.position, dragonController.InitialPosition) < 1f)
        {
            // 位置矫正完成，重置状态
            isCorrectingPosition = false;
            isMoving = false;
            ResetIdleTimer();

            // 恢复站立动画
            dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, false);
            dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, true);

            Debug.Log("位置矫正完成，返回初始位置");
        }
    }

    private void ReturnToHome()
    {
        if (!isReturningHome)
        {
            isReturningHome = true;
            isMoving = true;
            dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, true);
            dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, false);
        }

        // 向初始位置移动
        dragonController.MoveTowards(dragonController.InitialPosition, dragonController.MoveSpeed);

        // 检查是否到达初始位置
        if (Vector3.Distance(dragonController.transform.position, dragonController.InitialPosition) < 1f)
        {
            isReturningHome = false;
            isMoving = false;
            ResetIdleTimer();
        }
    }

    private void UpdateIdleBehavior()
    {
        idleTimer -= Time.deltaTime;
        // 添加调试信息
        Debug.Log($"idleTimer: {idleTimer}, isMoving: {isMoving}, isReturningHome: {isReturningHome}");

        if (isMoving)
        {
            // 向目标位置移动
            dragonController.MoveTowards(randomDestination, dragonController.MoveSpeed);

            // 检查是否到达目标
            if (Vector3.Distance(dragonController.transform.position, randomDestination) < 1f)
            {
                isMoving = false;
                idleTimer = dragonController.GetRandomStayTime();
                dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, false);
                dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, true);
            }
        }
        else
        {
            // 等待时间结束后，开始新的移动
            if (idleTimer <= 0)
            {
                SetRandomDestination();
                isMoving = true;
                dragonController.SetAnimationBool(FlyingDragonController.ANIM_STAND, false);
                dragonController.SetAnimationBool(FlyingDragonController.ANIM_FLY, true);
            }
        }
    }

    private void SetRandomDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * dragonController.IdleRange;
        randomDestination = dragonController.InitialPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    private void ResetIdleTimer()
    {
        idleTimer = dragonController.GetRandomIdleTime();
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            dragonController.TakeDamage(10);
        }
    }
}