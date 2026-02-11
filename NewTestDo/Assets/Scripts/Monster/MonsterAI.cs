using Mirror.BouncyCastle.Security;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public Animator animator; // 动画控制器引用
    public Collider CheckCollider; // 怪物用来检测附近玩家的碰撞器
    public float moveSpeed = 2f; // 移动速度
    public float rotationSpeed = 5f; // 旋转速度
    public float attackRange = 0.5f; // 攻击范围，小于此距离时只转向不移动
    public float minStopDistance = 2.5f; // 最小停止距离，小于此距离时完全停止

    private Vector3 initialPosition; // 怪物的初始位置
    private float stateTimer = 0f; // 状态计时器
    private float stateDuration = 0f; // 当前状态的持续时间
    private MonsterState currentState = MonsterState.Idle; // 当前状态
    private Vector3 currentMoveDirection; // 当前移动方向

    public float MoveWeight = 5.0f; // 怪物下一次状态切换为移动状态的权重
    private float idleDuration; // 记录站立状态的持续时间
    private float moveDuration; // 记录移动状态的持续时间

    private Transform playerTarget; // 追击的玩家目标
    private bool isChasing = false; // 是否正在追击玩家

    // 添加一个标志，用于标记是否被强制停止
    private bool isMovementStopped = false;

    // 添加一个可选的速度备份，用于恢复移动
    private float originalMoveSpeed = 0f;

    // 定义怪物状态枚举
    private enum MonsterState
    {
        Idle,   // 站立状态
        Move    // 移动状态
    }

    private void OnEnable()
    {
        // 获取怪物的初始位置
        initialPosition = transform.position;

        // 设置怪物动画状态机的初始参数
        animator.SetBool("Stand", true);
        animator.SetBool("Fly", false);

        // 初始化状态
        currentState = MonsterState.Idle;
        SetRandomStateDuration();

        // 记录原始移动速度
        originalMoveSpeed = moveSpeed;
    }

    private void Start()
    {
        // 确保检测碰撞器是触发器
        if (CheckCollider != null)
        {
            CheckCollider.isTrigger = true;
        }

        //注册监听怪物死亡事件
        this.RegisterEvent("MonsterDied", OnMonsterDie);
    }

    private void Update()
    {
        // 如果移动被强制停止，则直接返回
        if (isMovementStopped) return;

        // 如果正在追击玩家，优先处理追击逻辑
        if (isChasing && playerTarget != null)
        {
            ChasePlayer();
            return; // 追击状态下不执行原来的状态逻辑
        }

        // 更新状态计时器
        stateTimer += Time.deltaTime;

        // 如果状态持续时间结束，切换状态
        if (stateTimer >= stateDuration)
        {
            SwitchState();
        }

        // 根据当前状态执行相应的行为
        if (currentState == MonsterState.Move)
        {
            Move();
        }
    }

    /// <summary>
    /// 怪物死亡处理
    /// </summary>
    private void OnMonsterDie()
    {
        Debug.Log("收到Enemy脚本的怪物死亡通知，执行怪物死亡相关逻辑");
        StopMoving();
    }

    /// <summary>
    /// 立即停止怪物移动
    /// </summary>
    public void StopMoving() 
    {
        // 如果已经停止移动，则直接返回
        if (isMovementStopped) return;

        // 记录原始速度，以便恢复
        originalMoveSpeed = moveSpeed;

        // 立即停止移动
        moveSpeed = 0f;

        // 设置停止标志
        isMovementStopped = true;

        // 强制切换到站立状态
        if (currentState != MonsterState.Idle)
        {
            currentState = MonsterState.Idle;
            MoveToIdle();
        }

        // 重置计时器，防止立即切换状态
        stateTimer = 0f;

        Debug.Log("怪物移动已停止");
    }

    /// <summary>
    /// 恢复怪物移动
    /// </summary>
    /// <param name="restoreOriginalSpeed">是否恢复原始速度，默认为true</param>
    public void ResumeMoving(bool restoreOriginalSpeed = true)
    {
        // 如果已经在移动，则直接返回
        if (!isMovementStopped) return;

        // 清除停止标志
        isMovementStopped = false;

        // 恢复移动速度
        if (restoreOriginalSpeed)
        {
            moveSpeed = originalMoveSpeed;
        }
        else
        {
            // 如果不想恢复原始速度，可以设置一个新的速度
            // 或者保持当前为0的速度（但这样怪物不会移动）
        }

        Debug.Log($"怪物移动已恢复，当前速度: {moveSpeed}");
    }

    /// <summary>
    /// 停止怪物移动一段时间
    /// </summary>
    /// <param name="duration">停止时间（秒）</param>
    /// <param name="restoreOriginalSpeed">停止后是否恢复原始速度</param>
    public void StopMovingForDuration(float duration, bool restoreOriginalSpeed = true)
    {
        if (isMovementStopped) return;

        StartCoroutine(StopMovingCoroutine(duration, restoreOriginalSpeed));
    }

    /// <summary>
    /// 停止移动的协程
    /// </summary>
    private IEnumerator StopMovingCoroutine(float duration, bool restoreOriginalSpeed)
    {
        // 停止移动
        StopMoving();

        // 等待指定时间
        yield return new WaitForSeconds(duration);

        // 恢复移动
        ResumeMoving(restoreOriginalSpeed);
    }

    /// <summary>
    /// 设置随机状态持续时间
    /// </summary>
    private void SetRandomStateDuration()
    {
        // 如果移动被停止，不设置新的状态持续时间
        if (isMovementStopped) return;

        // 根据当前状态设置不同的持续时间
        if (currentState == MonsterState.Idle)
        {
            // 站立状态持续2.5-3秒
            stateDuration = Random.Range(1.5f, 2.5f);
            idleDuration = stateDuration; // 记录站立状态持续时间
        }
        else
        {
            // 移动状态持续1-1.5秒
            stateDuration = Random.Range(2.5f, 3f);
            moveDuration = stateDuration; // 记录移动状态持续时间
        }

        stateTimer = 0f; // 重置计时器
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    private void SwitchState()
    {
        // 如果移动被停止，不切换状态
        if (isMovementStopped) return;

        // 如果正在追击玩家，不切换状态
        if (isChasing) return;

        // 生成0-10的随机数
        float randomValue = Random.Range(0f, 10f);

        if (randomValue > MoveWeight)
        {
            // 切换到移动状态
            if (currentState != MonsterState.Move)
            {
                currentState = MonsterState.Move;
                IdleToMove();
            }
        }
        else
        {
            // 切换到站立状态
            if (currentState != MonsterState.Idle)
            {
                currentState = MonsterState.Idle;
                MoveToIdle();
            }
        }

        // 设置新的状态持续时间
        SetRandomStateDuration();
    }

    #region 状态切换逻辑
    private void IdleToMove()
    {
        // 如果移动被停止，不执行普通移动逻辑
        if (isMovementStopped) return;

        // 如果正在追击玩家，不执行普通移动逻辑
        if (isChasing) return;

        // 计算当前位置与初始位置的距离
        float distanceToInitial = Vector3.Distance(transform.position, initialPosition);

        // 如果距离超过20，朝初始位置移动
        if (distanceToInitial > 20f)
        {
            currentMoveDirection = (initialPosition - transform.position).normalized;
        }
        else
        {
            // 生成一个随机的移动方向
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            currentMoveDirection = new Vector3(Mathf.Sin(randomAngle), 0, Mathf.Cos(randomAngle)).normalized;
        }

        animator.SetBool("Stand", false);
        animator.SetBool("Fly", true);
    }

    private void MoveToIdle()
    {
        // 如果移动被停止，不切换到站立状态
        if (isMovementStopped) return;

        // 如果正在追击玩家，不切换到站立状态
        if (isChasing) return;

        animator.SetBool("Stand", true);
        animator.SetBool("Fly", false);
    }
    #endregion

    /// <summary>
    /// 普通移动函数
    /// </summary>
    private void Move()
    {
        // 如果移动被停止，不执行普通移动
        if (isMovementStopped) return;

        // 如果正在追击玩家，不执行普通移动
        if (isChasing) return;

        // 如果当前没有移动方向，则直接返回
        if (currentMoveDirection == Vector3.zero) return;

        // 平滑旋转到移动方向
        Quaternion targetRotation = Quaternion.LookRotation(currentMoveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 移动怪物
        transform.Translate(currentMoveDirection * moveSpeed * Time.deltaTime, Space.World);

        // 检查距离初始位置是否超过20，如果是则重新计算朝初始位置的方向
        float distanceToInitial = Vector3.Distance(transform.position, initialPosition);
        if (distanceToInitial > 20f)
        {
            currentMoveDirection = (initialPosition - transform.position).normalized;
        }
    }

    /// <summary>
    /// 追击玩家
    /// </summary>
    private void ChasePlayer()
    {
        // 如果移动被停止，不追击玩家
        if (isMovementStopped) return;

        if (playerTarget == null) return;

        // 计算与玩家的距离
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 计算朝向玩家的方向
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        directionToPlayer.y = 0; // 确保只在水平面上移动

        // 平滑旋转到玩家方向
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 如果距离小于最小停止距离，完全停止移动
        if (distanceToPlayer <= minStopDistance)
        {
            Debug.Log("已经到达停止范围，停止移动");
            // 在最小停止距离内，完全停止移动
            if (animator.GetBool("Fly"))
            {
                animator.SetBool("Stand", true);
                animator.SetBool("Fly", false);
            }
            animator.SetTrigger("Attack");
            // 完全停止，不执行移动代码
            return;
        }
        // 如果距离小于攻击范围但大于最小停止距离，只转向不移动
        else if (distanceToPlayer <= attackRange)
        {
            // 在攻击范围内，只转向不移动
            if (animator.GetBool("Fly"))
            {
                animator.SetBool("Stand", true);
                animator.SetBool("Fly", false);
            }
        }
        else
        {
            // 超出攻击范围，朝玩家移动
            transform.Translate(directionToPlayer * moveSpeed * Time.deltaTime, Space.World);

            // 确保动画状态为移动
            if (!animator.GetBool("Fly"))
            {
                animator.SetBool("Stand", false);
                animator.SetBool("Fly", true);
            }
        }
    }

    /// <summary>
    /// 当检测到玩家进入范围
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("进入碰撞器");
        if (other.CompareTag("Player"))
        {
            playerTarget = other.transform;
            isChasing = true;
            isMovementStopped = false;
            Debug.Log("开始追击玩家: " + other.name);
        }
    }

    /// <summary>
    /// 当玩家退出范围
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.transform == playerTarget)
        {
            playerTarget = null;
            isChasing = false;
            Debug.Log("停止追击玩家");

            // 停止追击后，切换到站立状态
            currentState = MonsterState.Idle;
            MoveToIdle();
            SetRandomStateDuration();
        }
    }
}