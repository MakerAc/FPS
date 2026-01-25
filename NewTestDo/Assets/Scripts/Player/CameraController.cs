using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("相机跟随设置")]
    [SerializeField] private Transform target;  // 跟随目标
    [SerializeField] private float followDuration = 0.3f; // 跟随动画持续时间
    [SerializeField] private Ease followEase = Ease.OutCubic; // 缓动函数

    private Vector3 initialOffset; // 初始偏移
    private Tweener followTweener;
    private Vector3 lastTargetPosition;
    private bool isInitialized = false;

    private void Start()
    {
        if (target == null)
        {
            // 自动查找玩家
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogError("CameraController: 没有设置目标!");
                return;
            }
        }

        // 计算相机与目标的初始偏移
        initialOffset = transform.position - target.position;
        lastTargetPosition = target.position;
        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (!isInitialized || target == null) return;

        // 如果目标位置发生变化，更新相机
        if (Vector3.Distance(lastTargetPosition, target.position) > 0.01f)
        {
            UpdateCameraPosition();
            lastTargetPosition = target.position;
        }
    }

    private void UpdateCameraPosition()
    {
        if (target == null) return;

        // 计算期望的相机位置
        Vector3 desiredPosition = target.position + initialOffset;

        // 停止之前的动画
        if (followTweener != null && followTweener.IsActive())
        {
            followTweener.Kill();
        }

        // 使用DOTween平滑移动相机
        followTweener = transform.DOMove(desiredPosition, followDuration)
            .SetEase(followEase);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        lastTargetPosition = newTarget.position;

        // 如果目标改变，重新计算偏移
        if (newTarget != null)
        {
            initialOffset = transform.position - newTarget.position;
        }
    }

    // 重置相机到初始位置
    public void ResetCamera()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + initialOffset;

        if (followTweener != null && followTweener.IsActive())
        {
            followTweener.Kill();
        }

        transform.position = desiredPosition;
    }

    private void OnDestroy()
    {
        // 清理DOTween
        if (followTweener != null && followTweener.IsActive())
        {
            followTweener.Kill();
        }
    }
}