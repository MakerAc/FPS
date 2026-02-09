using UnityEngine;
using System.Collections;  // 添加这个命名空间以便使用协程

[System.Serializable]
public class EnemyEventData
{
    public int enemyId;  // 怪物唯一标识（使用InstanceID）
    public int damage;    // 伤害值（可选）
}

public class Enemy : MonoBehaviour
{
    [SerializeField] private float health = 50f;
    [SerializeField] private float maxHealth = 50f;
    public Animator MonsterAnimator;

    private bool isDead = false;  // 添加死亡状态标记

    private void Start()
    {
        health = maxHealth;

        //注册怪物死亡事件
        this.RegisterEvent("MonsterDied", MonsterOut);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;  // 如果已经死亡，不再处理伤害

        health -= damage;
        Debug.Log($"{gameObject.name} 受到 {damage} 点伤害，剩余生命: {health}");
        MonsterAnimator.SetTrigger("Hurt");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;  // 防止重复执行
        isDead = true;

        // 播放死亡动画、特效等
        Debug.Log($"{gameObject.name} 被击败！");
        MonsterAnimator.SetTrigger("Dead");

        //这里需要写一个事件来通知怪物状态机脚本停止移动
        this.TriggerEvent("MonsterDied");

        // 禁用碰撞体，防止玩家继续与死亡怪物交互
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // 可选：禁用脚本，防止继续接收伤害
        // enabled = false;

        // 延迟销毁
        StartCoroutine(DelayedDestroy(2f));
    }

    private IEnumerator DelayedDestroy(float delay)
    {
        // 等待指定时间
        yield return new WaitForSeconds(delay);

        // 销毁父物体（如果存在），否则销毁自身
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //暂时只当作占位函数来用，没有实际意义，后续有时间可以优化一下事件系统，就可以不用这个占位函数了
    private void MonsterOut()
    {
        Debug.Log("怪物死亡");
    }
}