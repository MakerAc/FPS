using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f; // 移动速度
    [SerializeField] private float rotationSpeed = 10f; // 旋转速度

    [Header("输入设置")]
    [SerializeField] private KeyCode forwardKey = KeyCode.W; // 前进键
    [SerializeField] private KeyCode backwardKey = KeyCode.S; // 后退键
    [SerializeField] private KeyCode leftKey = KeyCode.A; // 左移键
    [SerializeField] private KeyCode rightKey = KeyCode.D; // 右移键

    private Rigidbody rb; // 刚体组件
    private Vector3 moveDirection; // 移动方向
    private Vector3 rotationDirection; // 旋转方向

    private void Start()
    {
        // 获取或添加Rigidbody组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 设置Rigidbody属性
        SetupRigidbody();

        Debug.Log("玩家移动脚本初始化完成");
    }

    private void SetupRigidbody()
    {
        rb.useGravity = true; // 启用重力
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 冻结X和Z轴旋转
        rb.drag = 5f; // 设置阻力，使移动更平滑
        rb.angularDrag = 5f; // 设置角阻力
    }

    private void Update()
    {
        // 处理输入
        HandleInput();

        // 处理旋转
        HandleRotation();
    }

    private void FixedUpdate()
    {
        // 在FixedUpdate中处理物理移动
        MovePlayer();
    }

    private void HandleInput()
    {
        // 重置移动方向
        moveDirection = Vector3.zero;

        // 检测WASD按键
        if (Input.GetKey(forwardKey))
        {
            moveDirection += transform.forward; // 向前移动
        }
        if (Input.GetKey(backwardKey))
        {
            moveDirection -= transform.forward; // 向后移动
        }
        if (Input.GetKey(leftKey))
        {
            moveDirection -= transform.right; // 向左移动
        }
        if (Input.GetKey(rightKey))
        {
            moveDirection += transform.right; // 向右移动
        }

        // 归一化方向向量，防止斜向移动速度更快
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
    }

    private void HandleRotation()
    {
        // 获取鼠标X轴输入
        float mouseX = Input.GetAxis("Mouse X");

        // 计算旋转
        if (mouseX != 0)
        {
            rotationDirection = new Vector3(0, mouseX * rotationSpeed, 0);
            transform.Rotate(rotationDirection);
        }
    }

    private void MovePlayer()
    {
        // 计算移动速度
        Vector3 moveVelocity = moveDirection * moveSpeed;

        // 使用Rigidbody的MovePosition进行移动
        rb.MovePosition(rb.position + moveVelocity * Time.fixedDeltaTime);
    }

    // 返回当前移动方向（可用于动画或其他脚本）
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }

    // 返回当前移动速度（0-1之间，可用于动画混合树）
    public float GetMoveSpeed()
    {
        return moveDirection.magnitude;
    }

    // 设置移动速度
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0, newSpeed);
    }
}