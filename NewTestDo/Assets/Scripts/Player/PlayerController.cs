using UnityEngine;
using System.Collections;
using DG.Tweening; // 导入DOTween

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float fastMoveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f; // 增加旋转速度
    [SerializeField] private float keyboardRotationDuration = 0.2f; // 键盘转向持续时间

    [Header("生命值")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("组件引用")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera playerCamera;

    [Header("射击设置")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float shootDelay = 0.1f; // 新增：子弹延迟发射时间
    private float nextFireTime = 0f;
    private Coroutine delayedShootCoroutine; // 延迟射击协程
    private bool isShooting = false; // 是否正在射击

    // 输入变量
    private Vector3 moveDirection = Vector3.zero;
    private bool isFastFlying = false;
    private bool isDead = false;
    private float targetRotationY = 0f; // 目标旋转角度
    private Vector3 lastInputDirection = Vector3.zero; // 上次输入方向

    // DOTween旋转补间
    private Tweener rotationTweener;

    // 动画参数名称
    private readonly string ANIM_SPEED = "Speed";
    private readonly string ANIM_IS_FAST_FLY = "IsFastFlying";
    private readonly string ANIM_SHOOT = "Shoot";
    private readonly string ANIM_DIE = "Die";

    private void Start()
    {
        currentHealth = maxHealth;
        targetRotationY = transform.eulerAngles.y;

        // 获取组件引用
        if (animator == null)
            animator = GetComponent<Animator>();

        // 获取相机引用
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("没有找到相机引用！角色转向功能将不可用。");
        }
    }

    private void Update()
    {
        if (isDead) return;

        HandleMovement();
        HandleFastFlight();
        HandleShooting();
        ApplyRotation(); // 应用旋转
    }

    private void HandleMovement()
    {
        // 重置移动方向
        moveDirection = Vector3.zero;
        Vector3 keyboardInput = Vector3.zero;

        // 检测WASD输入并设置移动方向
        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += Vector3.forward;
            keyboardInput += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection += Vector3.back;
            keyboardInput += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection += Vector3.left;
            keyboardInput += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection += Vector3.right;
            keyboardInput += Vector3.right;
        }

        // 如果有键盘输入
        if (keyboardInput != Vector3.zero)
        {
            // 标准化键盘输入
            keyboardInput.Normalize();

            // 如果键盘输入方向改变，更新目标旋转角度
            if (keyboardInput != lastInputDirection)
            {
                lastInputDirection = keyboardInput;

                // 计算新的目标旋转角度
                targetRotationY = GetDirectionAngle(keyboardInput);

                // 平滑旋转到新方向
                SmoothRotateTo(targetRotationY);
            }

            // 计算移动速度
            float currentSpeed = isFastFlying ? fastMoveSpeed : moveSpeed;

            // 标准化移动方向
            moveDirection.Normalize();

            // 移动角色
            transform.position += moveDirection * currentSpeed * Time.deltaTime;

            // 设置动画参数
            animator.SetFloat(ANIM_SPEED, 1.0f);
        }
        else
        {
            // 没有键盘输入时
            lastInputDirection = Vector3.zero;

            // 设置动画参数
            animator.SetFloat(ANIM_SPEED, 0f);
        }
    }

    private float GetDirectionAngle(Vector3 direction)
    {
        // 计算方向对应的角度
        if (direction == Vector3.forward) return 0f;       // 北
        if (direction == Vector3.back) return 180f;        // 南
        if (direction == Vector3.left) return 270f;        // 西
        if (direction == Vector3.right) return 90f;        // 东

        // 计算复合方向的角度
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // 将角度映射到合适的数值
        if (angle < 0) angle += 360f;

        // 处理对角线方向
        if (Mathf.Abs(direction.x) > 0 && Mathf.Abs(direction.z) > 0)
        {
            if (direction.x > 0 && direction.z > 0) return 45f;    // 东北
            if (direction.x > 0 && direction.z < 0) return 135f;  // 东南
            if (direction.x < 0 && direction.z < 0) return 225f;  // 西南
            if (direction.x < 0 && direction.z > 0) return 315f;  // 西北
        }

        return angle;
    }

    private void SmoothRotateTo(float targetY)
    {
        // 如果已经有旋转动画在进行，先停止
        if (rotationTweener != null && rotationTweener.IsActive())
        {
            rotationTweener.Kill();
        }

        // 使用DOTween平滑旋转
        rotationTweener = transform.DORotate(
            new Vector3(0, targetY, 0),
            keyboardRotationDuration
        ).SetEase(Ease.OutCubic);
    }

    private void ApplyRotation()
    {
        // 如果没有旋转动画，应用当前旋转
        if (rotationTweener == null || !rotationTweener.IsActive())
        {
            transform.rotation = Quaternion.Euler(0, targetRotationY, 0);
        }
    }

    private void HandleFastFlight()
    {
        isFastFlying = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        animator.SetBool(ANIM_IS_FAST_FLY, isFastFlying);
    }

    private void HandleShooting()
    {
        // 如果已经按下了射击键并且正在射击过程中，不重复触发
        if (isShooting) return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            // 触发射击
            nextFireTime = Time.time + fireRate;

            // 立即播放射击动画
            animator.SetTrigger(ANIM_SHOOT);

            // 开始延迟射击协程
            delayedShootCoroutine = StartCoroutine(DelayedShoot());
        }
    }

    private IEnumerator DelayedShoot()
    {
        isShooting = true;

        // 等待指定的延迟时间
        yield return new WaitForSeconds(shootDelay);

        // 创建子弹
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // 获取子弹脚本并初始化
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                // 从firePoint的前方发射子弹
                Vector3 shootDirection = firePoint.forward;
                bulletScript.Initialize(shootDirection);
            }
        }

        isShooting = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        Debug.Log($"受到伤害: {damage}, 剩余生命: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // 停止延迟射击协程
        if (delayedShootCoroutine != null)
        {
            StopCoroutine(delayedShootCoroutine);
        }

        // 播放死亡动画
        animator.SetTrigger(ANIM_DIE);

        // 设置速度为0
        animator.SetFloat(ANIM_SPEED, 0f);
        animator.SetBool(ANIM_IS_FAST_FLY, false);

        // 停止所有动画
        if (rotationTweener != null && rotationTweener.IsActive())
        {
            rotationTweener.Kill();
        }

        // 禁用移动和射击
        enabled = false;

        // 延迟销毁角色
        StartCoroutine(DestroyAfterDeath());
    }

    private IEnumerator DestroyAfterDeath()
    {
        // 等待死亡动画播放完成
        yield return new WaitForSeconds(2f);

        // 销毁角色
        Destroy(gameObject);
    }

    // 添加一个方法来停止所有射击协程（如果需要的话）
    public void StopAllShooting()
    {
        if (delayedShootCoroutine != null)
        {
            StopCoroutine(delayedShootCoroutine);
            isShooting = false;
        }
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    private void OnDestroy()
    {
        // 清理DOTween
        if (rotationTweener != null && rotationTweener.IsActive())
        {
            rotationTweener.Kill();
        }

        // 清理射击协程
        StopAllShooting();
    }
}