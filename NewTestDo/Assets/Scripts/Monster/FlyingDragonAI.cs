using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class FlyingDragonAI : MonoBehaviour
{
    // 状态枚举
    public enum DragonState
    {
        Idle,       // 闲置状态
        Battle,     // 战斗状态
        Hurt,       // 受击状态
        Dead        // 死亡状态
    }

    [Header("状态设置")]
    public DragonState currentState = DragonState.Idle;
    [SerializeField] private DragonState previousState; // 记录之前的状态，用于受击后返回

    [Header("怪物属性")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    [Header("闲置状态设置")]
    public float idleRange = 20f; // 闲置移动范围
    public Vector3 initialPosition; // 初始坐标
    public float idleMoveIntervalMin = 2f; // 随机移动最小间隔
    public float idleMoveIntervalMax = 5f; // 随机移动最大间隔
    public float idleStayDurationMin = 1f; // 站立最小时间
    public float idleStayDurationMax = 3f; // 站立最大时间

    [Header("战斗状态设置")]
    public float detectionRange = 15f; // 检测玩家范围
    public float attackRange = 3f; // 攻击范围
    public float chaseSpeed = 8f; // 追击速度
    public float attackCooldown = 2f; // 攻击冷却时间

    [Header("受击设置")]
    public float hurtDuration = 0.5f; // 受击动画持续时间

    [Header("组件引用")]
    public Animator animator;
    public Transform playerTarget;
    public Rigidbody rb;

    [Header("调试")]
    public bool showGizmos = true;

    // 私有变量
    private Vector3 randomDestination;
    private float idleTimer;
    private float attackTimer;
    private float hurtTimer;
    private bool isMoving = false;
    private bool isReturningHome = false;

    // 动画参数
    private const string ANIM_STAND = "Stand";
    private const string ANIM_FLY = "Fly";
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_HURT = "Hurt";
    private const string ANIM_DEAD = "Dead";

    void Start()
    {
        // 初始化
        currentHealth = maxHealth;
        initialPosition = transform.position;

        // 获取组件
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        // 开始闲置状态
        SwitchState(DragonState.Idle);

        // 查找玩家（标签为"Player"）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;
    }

    void Update()
    {
        // 根据当前状态执行不同的逻辑
        switch (currentState)
        {
            case DragonState.Idle:
                IdleStateUpdate();
                CheckForPlayer();
                break;

            case DragonState.Battle:
                BattleStateUpdate();
                break;

            case DragonState.Hurt:
                HurtStateUpdate();
                break;

            case DragonState.Dead:
                DeadStateUpdate();
                break;
        }

        // 更新计时器
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    #region 状态更新函数

    void IdleStateUpdate()
    {
        // 如果超出范围，返回初始位置
        float distanceFromHome = Vector3.Distance(transform.position, initialPosition);
        if (distanceFromHome > idleRange)
        {
            if (!isReturningHome)
            {
                isReturningHome = true;
                isMoving = true;
                animator.SetBool(ANIM_FLY, true);
                animator.SetBool(ANIM_STAND, false);
            }

            // 朝初始位置移动
            MoveTowards(initialPosition, moveSpeed);

            // 到达初始位置附近
            if (distanceFromHome < 1f)
            {
                isReturningHome = false;
                isMoving = false;
                StartIdleBehavior();
            }
        }
        else
        {
            // 闲置行为
            idleTimer -= Time.deltaTime;

            if (isMoving)
            {
                // 朝随机目标移动
                MoveTowards(randomDestination, moveSpeed);

                // 到达目标
                if (Vector3.Distance(transform.position, randomDestination) < 1f)
                {
                    isMoving = false;
                    idleTimer = Random.Range(idleStayDurationMin, idleStayDurationMax);
                    animator.SetBool(ANIM_FLY, false);
                    animator.SetBool(ANIM_STAND, true);
                }
            }
            else
            {
                // 站立一段时间后，开始新的移动
                if (idleTimer <= 0)
                {
                    SetRandomDestination();
                    isMoving = true;
                    animator.SetBool(ANIM_STAND, false);
                    animator.SetBool(ANIM_FLY, true);
                }
            }
        }
    }

    void BattleStateUpdate()
    {
        if (playerTarget == null)
        {
            SwitchState(DragonState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 检查是否在攻击范围内
        if (distanceToPlayer <= attackRange)
        {
            // 攻击玩家
            if (attackTimer <= 0)
            {
                AttackPlayer();
            }

            // 停止移动，播放攻击动画
            isMoving = false;
            animator.SetBool(ANIM_FLY, false);
        }
        else
        {
            // 追击玩家
            isMoving = true;
            MoveTowards(playerTarget.position, chaseSpeed);
            animator.SetBool(ANIM_FLY, true);

            // 播放飞行动画
            if (!animator.GetBool(ANIM_FLY))
            {
                animator.SetBool(ANIM_FLY, true);
            }
        }
    }

    void HurtStateUpdate()
    {
        // 受击计时
        hurtTimer -= Time.deltaTime;

        if (hurtTimer <= 0)
        {
            // 受击结束，返回之前的状态
            SwitchState(previousState);
        }
    }

    void DeadStateUpdate()
    {
        // 死亡状态，不执行任何操作
        // 可以在这里添加尸体消失等逻辑
    }

    #endregion

    #region 状态切换函数

    public void SwitchState(DragonState newState)
    {
        // 退出当前状态
        ExitState(currentState);

        // 记录之前的状态（如果是受击状态，不覆盖之前的记录）
        if (currentState != DragonState.Hurt && newState == DragonState.Hurt)
        {
            previousState = currentState;
        }

        // 切换状态
        currentState = newState;

        // 进入新状态
        EnterState(newState);
    }

    void EnterState(DragonState state)
    {
        switch (state)
        {
            case DragonState.Idle:
                StartIdleBehavior();
                animator.SetBool(ANIM_FLY, false);
                animator.SetBool(ANIM_STAND, true);
                break;

            case DragonState.Battle:
                animator.SetBool(ANIM_FLY, true);
                animator.SetBool(ANIM_STAND, false);
                break;

            case DragonState.Hurt:
                // 停止移动
                isMoving = false;
                // 播放受击动画
                animator.SetTrigger(ANIM_HURT);
                hurtTimer = hurtDuration;
                break;

            case DragonState.Dead:
                // 播放死亡动画
                animator.SetTrigger(ANIM_DEAD);
                // 禁用碰撞和移动
                if (rb != null) rb.isKinematic = true;
                // 可以在这里添加掉落物品、经验等逻辑
                break;
        }
    }

    void ExitState(DragonState state)
    {
        // 退出状态的清理工作
        switch (state)
        {
            case DragonState.Idle:
                animator.SetBool(ANIM_STAND, false);
                break;

            case DragonState.Battle:
                animator.SetBool(ANIM_FLY, false);
                break;
        }
    }

    #endregion

    #region 功能函数

    void StartIdleBehavior()
    {
        SetRandomDestination();
        idleTimer = Random.Range(idleMoveIntervalMin, idleMoveIntervalMax);
    }

    void SetRandomDestination()
    {
        // 在闲置范围内随机生成目标点
        Vector2 randomCircle = Random.insideUnitCircle * idleRange;
        randomDestination = initialPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    void MoveTowards(Vector3 target, float speed)
    {
        // 计算方向
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // 保持水平移动

        // 移动
        transform.position += direction * speed * Time.deltaTime;

        // 旋转朝向移动方向
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void CheckForPlayer()
    {
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance <= detectionRange)
        {
            SwitchState(DragonState.Battle);
        }
    }

    void AttackPlayer()
    {
        // 播放攻击动画
        animator.SetTrigger(ANIM_ATTACK);

        // 重置攻击计时器
        attackTimer = attackCooldown;

        // 这里可以添加对玩家造成伤害的逻辑
        // 例如：playerTarget.GetComponent<PlayerHealth>().TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        // 如果已经死亡，不再受到伤害
        if (currentState == DragonState.Dead) return;

        // 扣除生命值
        currentHealth -= damage;

        // 切换到受击状态
        SwitchState(DragonState.Hurt);

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            SwitchState(DragonState.Dead);
        }
    }

    #endregion

    #region 碰撞检测

    void OnTriggerEnter(Collider other)
    {
        // 检测子弹
        if (other.CompareTag("Bullet"))
        {
            // 受到伤害
            TakeDamage(10);

            // 销毁子弹（可选）
            Destroy(other.gameObject);
        }
    }

    #endregion

    #region 调试

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // 绘制闲置范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(initialPosition, idleRange);

        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 绘制当前目标
        if (currentState == DragonState.Idle && isMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(randomDestination, 0.5f);
            Gizmos.DrawLine(transform.position, randomDestination);
        }
    }

    #endregion
}