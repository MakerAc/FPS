using System;
using UnityEngine;

/// <summary>
/// 事件系统扩展方法
/// </summary>
public static class EventSystemExtensions
{
    #region 快捷方法
    /// <summary>
    /// 注册事件（快捷方法）
    /// </summary>
    public static void RegisterEvent<T>(this object target, string eventName, Action<T> callback)
    {
        EventSystem.On(eventName, callback, target);
    }

    /// <summary>
    /// 注册无参事件（快捷方法）
    /// </summary>
    public static void RegisterEvent(this object target, string eventName, Action callback)
    {
        EventSystem.On(eventName, callback, target);
    }

    /// <summary>
    /// 注销事件（快捷方法）
    /// </summary>
    public static void UnregisterEvent<T>(this object target, string eventName, Action<T> callback)
    {
        EventSystem.Off(eventName, callback, target);
    }

    /// <summary>
    /// 注销无参事件（快捷方法）
    /// </summary>
    public static void UnregisterEvent(this object target, string eventName, Action callback)
    {
        EventSystem.Off(eventName, callback, target);
    }

    /// <summary>
    /// 注销该对象的所有事件
    /// </summary>
    public static void UnregisterAllEvents(this object target)
    {
        EventSystem.RemoveAllInTarget(target);
    }

    /// <summary>
    /// 触发事件（快捷方法）
    /// </summary>
    public static void TriggerEvent<T>(this object target, string eventName, T args = default)
    {
        EventSystem.Emit(eventName, args);
    }

    /// <summary>
    /// 触发无参事件（快捷方法）
    /// </summary>
    public static void TriggerEvent(this object target, string eventName)
    {
        EventSystem.Emit(eventName);
    }
    #endregion

    #region Unity组件扩展
    /// <summary>
    /// 为GameObject注册事件
    /// </summary>
    public static void RegisterEvent<T>(this GameObject gameObject, string eventName, Action<T> callback)
    {
        var handler = gameObject.GetComponent<EventHandler>();
        if (handler == null)
        {
            handler = gameObject.AddComponent<EventHandler>();
        }
        EventSystem.On(eventName, callback, handler);
    }

    /// <summary>
    /// 为GameObject注册无参事件
    /// </summary>
    public static void RegisterEvent(this GameObject gameObject, string eventName, Action callback)
    {
        var handler = gameObject.GetComponent<EventHandler>();
        if (handler == null)
        {
            handler = gameObject.AddComponent<EventHandler>();
        }
        EventSystem.On(eventName, callback, handler);
    }

    /// <summary>
    /// 为MonoBehaviour注册事件
    /// </summary>
    public static void RegisterEvent<T>(this MonoBehaviour monoBehaviour, string eventName, Action<T> callback)
    {
        EventSystem.On(eventName, callback, monoBehaviour);
    }

    /// <summary>
    /// 为MonoBehaviour注册无参事件
    /// </summary>
    public static void RegisterEvent(this MonoBehaviour monoBehaviour, string eventName, Action callback)
    {
        EventSystem.On(eventName, callback, monoBehaviour);
    }
    #endregion

    #region 一次性事件
    /// <summary>
    /// 注册一次性事件（触发后自动移除）
    /// </summary>
    public static void Once<T>(string eventName, Action<T> callback, object target = null)
    {
        Action<T> wrapper = null;
        wrapper = args =>
        {
            callback?.Invoke(args);
            EventSystem.Off(eventName, wrapper, target);
        };
        EventSystem.On(eventName, wrapper, target);
    }

    /// <summary>
    /// 注册一次性无参事件（触发后自动移除）
    /// </summary>
    public static void Once(string eventName, Action callback, object target = null)
    {
        Action wrapper = null;
        wrapper = () =>
        {
            callback?.Invoke();
            EventSystem.Off(eventName, wrapper, target);
        };
        EventSystem.On(eventName, wrapper, target);
    }

    /// <summary>
    /// 注册带延迟触发的事件
    /// </summary>
    public static void DelayedEmit(string eventName, float delay, object args = null, MonoBehaviour coroutineRunner = null)
    {
        if (coroutineRunner == null)
        {
            var go = new GameObject("EventSystem_CoroutineRunner");
            coroutineRunner = go.AddComponent<EventSystemCoroutineRunner>();
        }

        coroutineRunner.StartCoroutine(DelayedEmitCoroutine(eventName, delay, args));
    }

    private static System.Collections.IEnumerator DelayedEmitCoroutine(string eventName, float delay, object args)
    {
        yield return new WaitForSeconds(delay);
        EventSystem.Emit(eventName, args);
    }
    #endregion
}

/// <summary>
/// 协程运行器
/// </summary>
public class EventSystemCoroutineRunner : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.hideFlags = HideFlags.HideInHierarchy;
    }
}