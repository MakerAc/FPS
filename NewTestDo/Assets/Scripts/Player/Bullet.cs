using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    [Header("子弹设置")]
    [SerializeField] private float speed = 20f; // 子弹速度
    [SerializeField] private float damage = 10f; // 子弹伤害
    [SerializeField] private float lifeTime = 3f; // 生存时间（秒）
    [SerializeField] private float explosionDuration = 1f; // 爆炸效果持续时间

    [Header("组件引用")]
    [SerializeField] private GameObject capsuleObject; // 胶囊体对象
    [SerializeField] private ParticleSystem explosionEffect; // 爆炸粒子特效
    [SerializeField] private Collider bulletCollider; // 子弹碰撞器

    [Header("效果设置")]
    [SerializeField] private float impactForce = 10f; // 冲击力

    private Rigidbody rb;
    private bool hasHit = false;
    private Coroutine autoDestroyCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 自动获取组件引用
        if (bulletCollider == null)
            bulletCollider = GetComponent<Collider>();

        if (capsuleObject == null && transform.childCount > 0)
        {
            // 尝试查找胶囊体子对象
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Capsule") || child.GetComponent<CapsuleCollider>() != null)
                {
                    capsuleObject = child.gameObject;
                    break;
                }
            }
        }

        if (explosionEffect == null)
        {
            // 尝试查找粒子系统
            explosionEffect = GetComponentInChildren<ParticleSystem>();
        }
    }

    private void Start()
    {
        // 设置自动销毁
        autoDestroyCoroutine = StartCoroutine(AutoDestroyAfterTime());

        // 如果粒子特效存在，确保它开始时是关闭的
        if (explosionEffect != null)
        {
            explosionEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void Initialize(Vector3 direction, float bulletSpeed = 20f, float bulletDamage = 10f)
    {
        speed = bulletSpeed;
        damage = bulletDamage;

        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }
        else
        {
            // 如果没有Rigidbody，使用Transform移动
            StartCoroutine(MoveWithoutRigidbody(direction));
        }

        // 让子弹始终面向移动方向
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private IEnumerator MoveWithoutRigidbody(Vector3 direction)
    {
        while (!hasHit && gameObject != null)
        {
            transform.position += direction.normalized * speed * Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator AutoDestroyAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);

        if (!hasHit && gameObject != null)
        {
            // 播放爆炸效果
            PlayExplosionEffect();

            // 销毁子弹
            Destroy(gameObject, explosionDuration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // 检查碰撞对象
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 对敌人造成伤害
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 应用冲击力
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                Vector3 forceDirection = (collision.transform.position - transform.position).normalized;
                enemyRb.AddForce(forceDirection * impactForce, ForceMode.Impulse);
            }
        }

        // 处理击中效果
        HandleHit();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // 检查触发器对象
        if (other.CompareTag("Enemy"))
        {
            // 对敌人造成伤害
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 处理击中效果
            HandleHit();
        }
    }

    private void HandleHit()
    {
        hasHit = true;

        // 停止自动销毁协程
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
        }

        // 禁用胶囊体
        if (capsuleObject != null)
        {
            capsuleObject.SetActive(false);
        }

        // 禁用碰撞器
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }

        // 停止物理运动
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 播放爆炸效果
        PlayExplosionEffect();

        // 延迟销毁子弹
        StartCoroutine(DestroyAfterExplosion());
    }

    private void PlayExplosionEffect()
    {
        if (explosionEffect != null)
        {
            explosionEffect.Play();

            // 确保粒子系统不会被销毁
            var main = explosionEffect.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
    }

    private IEnumerator DestroyAfterExplosion()
    {
        yield return new WaitForSeconds(explosionDuration);

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    // 调试用：显示子弹轨迹
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}