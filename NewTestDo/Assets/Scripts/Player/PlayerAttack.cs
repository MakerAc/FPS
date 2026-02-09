using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("血量设置")]
    [SerializeField] private int maxHealth = 100; // 最大血量
    [SerializeField] private int currentHealth;   // 当前血量
    [SerializeField] private int damagePerHit = 10; // 每次受到的伤害

    [Header("受伤设置")]
    [SerializeField] private float damageCooldown = 1f; // 伤害冷却时间（秒）
    [SerializeField] private string[] damageTags = { "EnemyAttack", "Bullet" }; // 会造成伤害的标签

    [Header("组件引用")]
    [SerializeField] public Collider playerCollider; // 玩家碰撞器
    [SerializeField] public Renderer playerRenderer; // 玩家渲染器（用于闪烁效果）

    [Header("效果设置")]
    [SerializeField] private Color damageColor = Color.red; // 受伤时的颜色
    [SerializeField] private float flashDuration = 0.1f; // 闪烁持续时间
    [SerializeField] private GameObject deathEffect; // 死亡特效（可选）

    private float lastDamageTime = 0f; // 上次受到伤害的时间
    private Color originalColor; // 原始颜色
    private bool isDead = false; // 是否死亡

    private void Awake()
    {
        // 初始化血量
        currentHealth = maxHealth;

        // 自动获取组件
        if (playerCollider == null)
            playerCollider = GetComponent<Collider>();

        if (playerRenderer == null)
            playerRenderer = GetComponent<Renderer>();

        // 保存原始颜色
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
        }
    }

    private void Start()
    {
        Debug.Log($"玩家血量初始化: {currentHealth}/{maxHealth}");
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检查是否为伤害标签
        if (CheckDamageTag(other.tag))
        {
            ProcessDamage(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 检查是否为伤害标签
        if (CheckDamageTag(collision.gameObject.tag))
        {
            ProcessDamage(collision.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 处理持续触发
        if (CheckDamageTag(other.tag))
        {
            ProcessDamage(other.gameObject);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 处理持续碰撞
        if (CheckDamageTag(collision.gameObject.tag))
        {
            ProcessDamage(collision.gameObject);
        }
    }

    /// <summary>
    /// 检查标签是否为伤害标签
    /// </summary>
    private bool CheckDamageTag(string tag)
    {
        foreach (string damageTag in damageTags)
        {
            if (tag == damageTag)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 处理伤害逻辑
    /// </summary>
    private void ProcessDamage(GameObject damageSource)
    {
        if (isDead) return; // 如果已死亡，不再处理伤害

        // 检查冷却时间
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return; // 冷却时间内不处理伤害
        }

        // 更新最后受伤时间
        lastDamageTime = Time.time;

        // 扣除血量
        TakeDamage(damagePerHit);

        // 显示受伤效果
        StartCoroutine(FlashDamageEffect());

        // 销毁子弹（如果攻击源是子弹）
        if (damageSource.CompareTag("Bullet"))
        {
            Bullet bullet = damageSource.GetComponent<Bullet>();
            if (bullet != null)
            {
                // 这里可以根据需要调用子弹的销毁方法
                // 如果子弹有自己的销毁逻辑，可以不用这里处理
            }
        }

        Debug.Log($"玩家受到伤害，当前血量: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 扣除血量
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        // 触发受伤事件（可以扩展）
        OnDamageTaken(damage);
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("玩家死亡");

        // 播放死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        // 销毁物体及其父物体
        DestroyWithParent();
    }

    /// <summary>
    /// 销毁当前物体及其父物体
    /// </summary>
    private void DestroyWithParent()
    {
        if (transform.parent != null)
        {
            // 如果有父物体，先销毁父物体
            Destroy(transform.parent.gameObject);
        }
        else
        {
            // 如果没有父物体，只销毁当前物体
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 受伤闪烁效果
    /// </summary>
    private IEnumerator FlashDamageEffect()
    {
        if (playerRenderer != null)
        {
            // 变为受伤颜色
            playerRenderer.material.color = damageColor;

            // 等待一段时间
            yield return new WaitForSeconds(flashDuration);

            // 恢复原始颜色
            if (!isDead) // 如果没死才恢复颜色
            {
                playerRenderer.material.color = originalColor;
            }
        }
    }

    /// <summary>
    /// 受伤事件（可扩展）
    /// </summary>
    private void OnDamageTaken(int damage)
    {
        // 这里可以添加更多受伤时的逻辑
        // 例如：触发UI更新、播放受伤音效等
    }

   
 
    
}