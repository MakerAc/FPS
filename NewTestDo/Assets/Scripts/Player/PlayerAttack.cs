using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("血量设置")]
    [SerializeField] private int maxHealth = 999; // 最大血量
    [SerializeField] private int currentHealth;   // 当前血量
    [SerializeField] private int damagePerHit = 3; // 每次受到的伤害

    [Header("受伤设置")]
    [SerializeField] private float damageCooldown = 0.2f; // 伤害冷却时间（秒）
    [SerializeField] private string[] damageTags = { "EnemyAttack", "Bullet" }; // 会造成伤害的标签
    [SerializeField] private float damageDelay = 0.3f; // 碰撞后延迟触发伤害的时间

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

    // 跟踪已触发延迟伤害的碰撞对象
    private HashSet<GameObject> delayedDamageSources = new HashSet<GameObject>();

    // 延迟伤害协程列表
    private List<Coroutine> delayedDamageCoroutines = new List<Coroutine>();

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
            // 启动延迟伤害协程
            Coroutine coroutine = StartCoroutine(DelayedDamage(other.gameObject, damageDelay));
            delayedDamageCoroutines.Add(coroutine);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 检查是否为伤害标签
        if (CheckDamageTag(collision.gameObject.tag))
        {
            // 启动延迟伤害协程
            Coroutine coroutine = StartCoroutine(DelayedDamage(collision.gameObject, damageDelay));
            delayedDamageCoroutines.Add(coroutine);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 处理持续触发
        if (CheckDamageTag(other.tag))
        {
            // 如果这个伤害源还没有在延迟处理中，则开始延迟处理
            if (!delayedDamageSources.Contains(other.gameObject))
            {
                Coroutine coroutine = StartCoroutine(DelayedDamage(other.gameObject, damageDelay));
                delayedDamageCoroutines.Add(coroutine);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 处理持续碰撞
        if (CheckDamageTag(collision.gameObject.tag))
        {
            // 如果这个伤害源还没有在延迟处理中，则开始延迟处理
            if (!delayedDamageSources.Contains(collision.gameObject))
            {
                Coroutine coroutine = StartCoroutine(DelayedDamage(collision.gameObject, damageDelay));
                delayedDamageCoroutines.Add(coroutine);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 离开时移除追踪
        delayedDamageSources.Remove(other.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        // 离开时移除追踪
        delayedDamageSources.Remove(collision.gameObject);
    }

    /// <summary>
    /// 延迟触发伤害
    /// </summary>
    private IEnumerator DelayedDamage(GameObject damageSource, float delay)
    {
        // 添加到已触发列表
        delayedDamageSources.Add(damageSource);

        // 等待延迟时间
        yield return new WaitForSeconds(delay);

        // 检查伤害源是否仍然有效
        if (damageSource == null || !delayedDamageSources.Contains(damageSource))
        {
            yield break;
        }

        // 处理伤害
        ProcessDamage(damageSource);

        // 从列表中移除
        delayedDamageSources.Remove(damageSource);
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
        if (damageSource != null && damageSource.CompareTag("Bullet"))
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

        // 停止所有延迟伤害协程
        StopAllDelayedDamageCoroutines();

        // 清空延迟伤害源列表
        delayedDamageSources.Clear();

        // 播放死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        // 销毁物体及其父物体
        DestroyWithParent();
    }

    /// <summary>
    /// 停止所有延迟伤害协程
    /// </summary>
    private void StopAllDelayedDamageCoroutines()
    {
        foreach (Coroutine coroutine in delayedDamageCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        delayedDamageCoroutines.Clear();
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