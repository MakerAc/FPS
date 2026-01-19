using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飞龙状态机管理器
/// </summary>
public class DragonStateMachine : MonoBehaviour
{
    [System.Serializable]
    public enum DragonStateType
    {
        Idle,
        Battle,
        Hurt,
        Dead
    }

    [Header("状态配置")]
    [SerializeField] private DragonStateType currentStateType = DragonStateType.Idle;
    [SerializeField] private DragonStateType previousStateType = DragonStateType.Idle;

    // 状态字典
    private Dictionary<DragonStateType, DragonState> states = new Dictionary<DragonStateType, DragonState>();
    private DragonState currentState;

    // 组件引用
    private FlyingDragonController dragonController;

    private void Awake()
    {
        dragonController = GetComponent<FlyingDragonController>();

        // 初始化所有状态
        InitializeStates();
    }

    private void InitializeStates()
    {
        states.Clear();

        // 创建并注册所有状态
        states.Add(DragonStateType.Idle, new DragonIdleState(this, dragonController));
        states.Add(DragonStateType.Battle, new DragonBattleState(this, dragonController));
        states.Add(DragonStateType.Hurt, new DragonHurtState(this, dragonController));
        states.Add(DragonStateType.Dead, new DragonDeadState(this, dragonController));

        // 设置初始状态
        SwitchState(currentStateType);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void SwitchState(DragonStateType newStateType)
    {
        if (currentStateType == newStateType) return;

        // 记录之前的状态
        if (currentStateType != DragonStateType.Hurt)
        {
            previousStateType = currentStateType;
        }

        // 退出当前状态
        currentState?.Exit();

        // 切换状态
        currentStateType = newStateType;
        currentState = states[newStateType];

        // 进入新状态
        currentState.Enter();

        Debug.Log($"Dragon State Changed: {previousStateType} -> {currentStateType}");
    }

    /// <summary>
    /// 返回到之前的状态
    /// </summary>
    public void ReturnToPreviousState()
    {
        SwitchState(previousStateType);
    }

    /// <summary>
    /// 获取当前状态类型
    /// </summary>
    public DragonStateType GetCurrentStateType() => currentStateType;

    /// <summary>
    /// 处理碰撞事件
    /// </summary>
    public void HandleTriggerEnter(Collider other)
    {
        currentState?.OnTriggerEnter(other);
    }
}