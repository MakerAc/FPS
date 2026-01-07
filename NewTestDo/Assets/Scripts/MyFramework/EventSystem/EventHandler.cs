using System;
using UnityEngine;

/// <summary>
/// 自动事件管理组件，挂载在GameObject上自动管理事件
/// </summary>
[DisallowMultipleComponent]
public class EventHandler : MonoBehaviour
{
    [Tooltip("是否在Awake时清理该对象上之前注册的事件")]
    [SerializeField] private bool clearOnAwake = true;

    [Tooltip("是否在OnDestroy时自动移除该对象注册的所有事件")]
    [SerializeField] private bool autoRemoveOnDestroy = true;

    [Tooltip("是否在OnDisable时自动移除该对象注册的所有事件")]
    [SerializeField] private bool removeOnDisable = false;

    [Header("事件列表")]
    [SerializeField] private EventBinding[] eventBindings = Array.Empty<EventBinding>();

    private void Awake()
    {
        if (clearOnAwake)
        {
            EventSystem.RemoveAllInTarget(this);
        }

        // 注册序列化的事件
        RegisterEventBindings();
    }

    private void OnEnable()
    {
        if (removeOnDisable)
        {
            RegisterEventBindings();
        }
    }

    private void OnDisable()
    {
        if (removeOnDisable)
        {
            UnregisterEventBindings();
        }
    }

    private void OnDestroy()
    {
        if (autoRemoveOnDestroy)
        {
            EventSystem.RemoveAllInTarget(this);
        }

        if (!removeOnDisable)
        {
            UnregisterEventBindings();
        }
    }

    private void RegisterEventBindings()
    {
        foreach (var binding in eventBindings)
        {
            if (binding != null && binding.IsValid())
            {
                binding.Register(this);
            }
        }
    }

    private void UnregisterEventBindings()
    {
        foreach (var binding in eventBindings)
        {
            if (binding != null && binding.IsValid())
            {
                binding.Unregister(this);
            }
        }
    }
}

/// <summary>
/// 事件绑定数据类
/// </summary>
[Serializable]
public class EventBinding
{
    [Tooltip("事件名称")]
    public string eventName = "";

    [Tooltip("是否包含参数")]
    public bool hasParameter = false;

    [Tooltip("事件参数类型（如果有参数）")]
    [ConditionalHide("hasParameter", true)]
    public string parameterType = "";

    [Tooltip("回调方法名称")]
    public string callbackMethod = "";

    [Tooltip("回调目标组件")]
    public MonoBehaviour targetComponent = null;

    [Tooltip("事件描述")]
    [TextArea]
    public string description = "";

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(eventName) &&
               !string.IsNullOrEmpty(callbackMethod) &&
               targetComponent != null;
    }

    public void Register(object handler)
    {
        if (!IsValid()) return;

        try
        {
            if (hasParameter && !string.IsNullOrEmpty(parameterType))
            {
                var type = Type.GetType(parameterType);
                if (type != null)
                {
                    var methodInfo = targetComponent.GetType().GetMethod(callbackMethod,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (methodInfo != null)
                    {
                        var actionType = typeof(Action<>).MakeGenericType(type);
                        var action = Delegate.CreateDelegate(actionType, targetComponent, methodInfo);

                        var onMethod = typeof(EventSystem).GetMethod("On")
                            .MakeGenericMethod(type);

                        onMethod.Invoke(null, new object[] { eventName, action, handler });
                    }
                }
            }
            else
            {
                var methodInfo = targetComponent.GetType().GetMethod(callbackMethod,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (methodInfo != null)
                {
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), targetComponent, methodInfo);
                    EventSystem.On(eventName, action, handler);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"EventBinding: 注册事件失败 - {eventName}: {e.Message}");
        }
    }

    public void Unregister(object handler)
    {
        if (!IsValid()) return;

        try
        {
            if (hasParameter && !string.IsNullOrEmpty(parameterType))
            {
                var type = Type.GetType(parameterType);
                if (type != null)
                {
                    var methodInfo = targetComponent.GetType().GetMethod(callbackMethod,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (methodInfo != null)
                    {
                        var actionType = typeof(Action<>).MakeGenericType(type);
                        var action = Delegate.CreateDelegate(actionType, targetComponent, methodInfo);

                        var offMethod = typeof(EventSystem).GetMethod("Off")
                            .MakeGenericMethod(type);

                        offMethod.Invoke(null, new object[] { eventName, action, handler });
                    }
                }
            }
            else
            {
                var methodInfo = targetComponent.GetType().GetMethod(callbackMethod,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (methodInfo != null)
                {
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), targetComponent, methodInfo);
                    EventSystem.Off(eventName, action, handler);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"EventBinding: 注销事件失败 - {eventName}: {e.Message}");
        }
    }
}

/// <summary>
/// 条件隐藏属性
/// </summary>
public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalSourceField = "";
    public bool HideInInspector = false;

    public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector = false)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = hideInInspector;
    }
}