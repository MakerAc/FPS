using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    [SerializeField] private int initialHealth = 100; // 初始生命值
    [SerializeField] private string enemyTag = "Enemy"; // 敌人标签（不是层级！）

    [Header("伤害设置")]
    [SerializeField] private int damagePerHit = 10; // 每次受到的伤害
    [SerializeField] private float damageInterval = 1f; // 持续触发时的伤害间隔

    private int health; // 当前生命值
    private float lastDamageTime; // 上次受到伤害的时间
    private bool isEnemyInTrigger = false; // 敌人是否在触发器内

    private void Start()
    {
        // 初始化生命值
        health = initialHealth;

        // 确保游戏对象有BoxCollider
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError("此游戏对象没有BoxCollider组件！");
        }
        else
        {
            // 确保BoxCollider是触发器
            if (!boxCollider.isTrigger)
            {
                Debug.LogWarning("BoxCollider不是触发器，已自动设置为触发器");
                boxCollider.isTrigger = true;
            }
        }

        Debug.Log("玩家初始化，生命值: " + health);
    }

    private void OnEnable()
    {
        // 当物体被激活时，重置生命值
        health = initialHealth;
        isEnemyInTrigger = false;

        Debug.Log("玩家激活，生命值重置为: " + health);
    }

    private void Update()
    {
        // 如果敌人在触发器内，检查是否可以造成伤害
        if (isEnemyInTrigger && Time.time >= lastDamageTime + damageInterval)
        {
            TakeDamage(damagePerHit);
            lastDamageTime = Time.time;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("11111111111");
        // 修改这里：使用Tag检测而不是Layer检测
        if (other.CompareTag(enemyTag))
        {
            // 立即造成一次伤害
            TakeDamage(damagePerHit);
            lastDamageTime = Time.time;

            // 标记有敌人在触发器内
            isEnemyInTrigger = true;

            Debug.Log("敌人进入触发器，生命值减少。当前生命值: " + health);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 修改这里：使用Tag检测而不是Layer检测
        if (other.CompareTag(enemyTag))
        {
            // 标记敌人已离开触发器
            isEnemyInTrigger = false;

            Debug.Log("敌人离开触发器");
        }
    }

    // 处理持续触发伤害
    private void OnTriggerStay(Collider other)
    {
        // 修改这里：使用Tag检测而不是Layer检测
        if (other.CompareTag(enemyTag))
        {
            isEnemyInTrigger = true;
        }
    }

    // 受到伤害的方法
    public void TakeDamage(int damage)
    {
        if (health <= 0) return; // 如果已经死亡，不再处理伤害

        health -= damage;

        Debug.Log("受到伤害: " + damage + "，当前生命值: " + health);

        // 确保生命值不会低于0
        if (health <= 0)
        {
            health = 0;
            Debug.Log("玩家生命值为0！玩家死亡。");

            // 禁用整个游戏对象
            gameObject.SetActive(false);
        }
    }

    // 获取当前生命值
    public int GetHealth()
    {
        return health;
    }

    // 设置生命值
    public void SetHealth(int newHealth)
    {
        health = Mathf.Max(0, newHealth);
    }

    // 恢复生命值
    public void Heal(int healAmount)
    {
        if (health <= 0) return; // 如果已经死亡，不能恢复生命值

        health += healAmount;
        Debug.Log("恢复生命值: " + healAmount + "，当前生命值: " + health);
    }
}