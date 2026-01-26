using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float health = 50f;
    [SerializeField] private float maxHealth = 50f;

    private void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"{gameObject.name} 受到 {damage} 点伤害，剩余生命: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 播放死亡动画、特效等
        Debug.Log($"{gameObject.name} 被击败！");

        // 销毁敌人
        Destroy(gameObject);
    }
}