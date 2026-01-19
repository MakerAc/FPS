using UnityEngine;

/// <summary>
/// 飞龙主控制器，负责管理组件和公共数据
/// </summary>
public class FlyingDragonController : MonoBehaviour, IDamageable
{
    [Header("怪物属性")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("闲置状态设置")]
    [SerializeField] private float idleRange = 20f;
    [SerializeField] private float idleMoveIntervalMin = 2f;
    [SerializeField] private float idleMoveIntervalMax = 5f;
    [SerializeField] private float idleStayDurationMin = 1f;
    [SerializeField] private float idleStayDurationMax = 3f;

    [Header("战斗状态设置")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float chaseSpeed = 8f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("受击设置")]
    [SerializeField] private float hurtDuration = 0.5f;

    [Header("组件引用")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Rigidbody rb;

    [Header("调试")]
    [SerializeField] private bool showGizmos = true;

    // 公开属性
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float IdleRange => idleRange;
    public float DetectionRange => detectionRange;
    public float AttackRange => attackRange;
    public float ChaseSpeed => chaseSpeed;
    public float AttackCooldown => attackCooldown;
    public float HurtDuration => hurtDuration;
    public Vector3 InitialPosition { get; private set; }
    public Animator Animator => animator;
    public Rigidbody Rb => rb;
    public Transform PlayerTarget => playerTarget;
    public float AttackTimer { get; set; }
    public float HurtTimer { get; set; }

    // 状态机引用
    private DragonStateMachine stateMachine;

    // 动画参数常量
    public const string ANIM_STAND = "Stand";
    public const string ANIM_FLY = "Fly";
    public const string ANIM_ATTACK = "Attack";
    public const string ANIM_HURT = "Hurt";
    public const string ANIM_DEAD = "Dead";

    private void Awake()
    {
        stateMachine = GetComponent<DragonStateMachine>();

        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        InitialPosition = transform.position;
        currentHealth = maxHealth;

        // 查找玩家
        FindPlayer();
    }

    private void Update()
    {
        if (AttackTimer > 0) AttackTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 将碰撞事件传递给状态机
        stateMachine.HandleTriggerEnter(other);
    }

    /// <summary>
    /// 查找玩家
    /// </summary>
    public void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;
    }

    /// <summary>
    /// 设置玩家目标
    /// </summary>
    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    /// <summary>
    /// 检查是否有玩家在检测范围内
    /// </summary>
    public bool IsPlayerInDetectionRange()
    {
        if (playerTarget == null) return false;
        return Vector3.Distance(transform.position, playerTarget.position) <= detectionRange;
    }

    /// <summary>
    /// 检查是否在攻击范围内
    /// </summary>
    public bool IsPlayerInAttackRange()
    {
        if (playerTarget == null) return false;
        return Vector3.Distance(transform.position, playerTarget.position) <= attackRange;
    }

    /// <summary>
    /// 检查是否超出闲置范围
    /// </summary>
    public bool IsOutOfIdleRange()
    {
        return Vector3.Distance(transform.position, InitialPosition) > idleRange;
    }

    /// <summary>
    /// 移动到目标位置
    /// </summary>
    public void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // 保持水平移动

        // 移动
        transform.position += direction * speed * Time.deltaTime;

        // 旋转
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 设置动画布尔值
    /// </summary>
    public void SetAnimationBool(string param, bool value)
    {
        if (animator != null) animator.SetBool(param, value);
    }

    /// <summary>
    /// 触发动画
    /// </summary>
    public void TriggerAnimation(string param)
    {
        if (animator != null) animator.SetTrigger(param);
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"Dragon took {damage} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            stateMachine.SwitchState(DragonStateMachine.DragonStateType.Dead);
        }
        else
        {
            stateMachine.SwitchState(DragonStateMachine.DragonStateType.Hurt);
        }
    }

    /// <summary>
    /// 播放攻击效果
    /// </summary>
    public void Attack()
    {
        // 这里可以添加攻击逻辑，比如伤害玩家
        Debug.Log("Dragon attacks!");
    }

    /// <summary>
    /// 获取闲置行为的随机时间
    /// </summary>
    public float GetRandomIdleTime()
    {
        return Random.Range(idleMoveIntervalMin, idleMoveIntervalMax);
    }

    /// <summary>
    /// 获取随机站立时间
    /// </summary>
    public float GetRandomStayTime()
    {
        return Random.Range(idleStayDurationMin, idleStayDurationMax);
    }

    #region 调试绘制
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(InitialPosition, idleRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion
}