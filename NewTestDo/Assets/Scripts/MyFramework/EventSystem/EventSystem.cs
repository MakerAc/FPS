using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 事件信息类，封装事件回调
/// </summary>
public class EventInfo
{
    /// <summary>
    /// 事件目标对象
    /// </summary>
    public object Target { get; private set; }

    /// <summary>
    /// 原始回调委托
    /// </summary>
    public Delegate Callback { get; private set; }

    /// <summary>
    /// 统一的调用委托
    /// </summary>
    public Action<object> InvokeAction { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="target">目标对象</param>
    /// <param name="callback">回调委托</param>
    /// <param name="invokeAction">调用委托</param>
    public EventInfo(object target, Delegate callback, Action<object> invokeAction)
    {
        Target = target;
        Callback = callback;
        InvokeAction = invokeAction;
    }

    /// <summary>
    /// 调用事件
    /// </summary>
    /// <param name="args">事件参数</param>
    public void Invoke(object args = null)
    {
        InvokeAction?.Invoke(args);
    }

    public override int GetHashCode()
    {
        return Callback?.GetHashCode() ?? 0;
    }

    public override bool Equals(object obj)
    {
        if (obj is EventInfo other)
        {
            return Callback?.Equals(other.Callback) ?? false;
        }
        return false;
    }
}

/// <summary>
/// 事件系统核心类
/// </summary>
public static class EventSystem
{
    private static readonly Dictionary<string, Type> EventTypes = new Dictionary<string, Type>();
    private static readonly Dictionary<string, Dictionary<object, HashSet<EventInfo>>> EventMap = new Dictionary<string, Dictionary<object, HashSet<EventInfo>>>();
    private static readonly object Lock = new object();
    private static readonly object GlobalObj = new object();

    /// <summary>
    /// 事件总数
    /// </summary>
    public static int EventCount
    {
        get
        {
            lock (Lock)
            {
                return EventMap.Sum(pair => pair.Value.Sum(kv => kv.Value.Count));
            }
        }
    }

    /// <summary>
    /// 事件名称列表
    /// </summary>
    public static List<string> EventNames
    {
        get
        {
            lock (Lock)
            {
                return EventMap.Keys.ToList();
            }
        }
    }

    #region 注册事件
    /// <summary>
    /// 监听事件
    /// </summary>
    /// <typeparam name="T">事件参数类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">事件回调</param>
    /// <param name="target">事件目标对象（用于分组管理）</param>
    public static void On<T>(string eventName, Action<T> callback, object target = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("EventSystem: 事件名称不能为空");
            return;
        }

        if (callback == null)
        {
            Debug.LogError($"EventSystem: 事件 {eventName} 的回调不能为null");
            return;
        }

        lock (Lock)
        {
            if (!EventMap.ContainsKey(eventName))
            {
                EventMap[eventName] = new Dictionary<object, HashSet<EventInfo>>();
            }

            target ??= callback.Target ?? GlobalObj;

            if (!EventMap[eventName].ContainsKey(target))
            {
                EventMap[eventName][target] = new HashSet<EventInfo>();
            }

            // 检查事件类型是否匹配
            if (EventTypes.TryGetValue(eventName, out var existingType) && existingType != typeof(T))
            {
                Debug.LogError($"EventSystem: 事件 {eventName} 的类型不匹配。已注册类型: {existingType.Name}，尝试注册类型: {typeof(T).Name}");
                return;
            }

            EventTypes[eventName] = typeof(T);

            var eventInfo = new EventInfo(target, callback, obj =>
            {
                try
                {
                    if (obj is T typedArg)
                    {
                        callback(typedArg);
                    }
                    else if (obj == null && default(T) == null)
                    {
                        callback(default);
                    }
                    else
                    {
                        Debug.LogError($"EventSystem: 事件 {eventName} 参数类型不匹配。期望: {typeof(T).Name}，实际: {obj?.GetType().Name ?? "null"}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"EventSystem: 调用事件 {eventName} 时发生错误: {e.Message}");
                }
            });

            EventMap[eventName][target].Add(eventInfo);

            Debug.Log($"EventSystem: 注册事件 {eventName}, 目标: {target}, 类型: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 监听无参事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">事件回调</param>
    /// <param name="target">事件目标对象</param>
    public static void On(string eventName, Action callback, object target = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("EventSystem: 事件名称不能为空");
            return;
        }

        if (callback == null)
        {
            Debug.LogError($"EventSystem: 事件 {eventName} 的回调不能为null");
            return;
        }

        lock (Lock)
        {
            if (!EventMap.ContainsKey(eventName))
            {
                EventMap[eventName] = new Dictionary<object, HashSet<EventInfo>>();
            }

            target ??= callback.Target ?? GlobalObj;

            if (!EventMap[eventName].ContainsKey(target))
            {
                EventMap[eventName][target] = new HashSet<EventInfo>();
            }

            // 检查事件类型是否匹配
            if (EventTypes.TryGetValue(eventName, out var existingType) && existingType != typeof(object))
            {
                Debug.LogError($"EventSystem: 事件 {eventName} 的类型不匹配。已注册类型: {existingType.Name}，尝试注册无参事件");
                return;
            }

            EventTypes[eventName] = typeof(object);

            var eventInfo = new EventInfo(target, callback, _ =>
            {
                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    Debug.LogError($"EventSystem: 调用无参事件 {eventName} 时发生错误: {e.Message}");
                }
            });

            EventMap[eventName][target].Add(eventInfo);

            Debug.Log($"EventSystem: 注册无参事件 {eventName}, 目标: {target}");
        }
    }
    #endregion

    #region 移除事件
    /// <summary>
    /// 移除事件监听
    /// </summary>
    /// <typeparam name="T">事件参数类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">事件回调</param>
    /// <param name="target">事件目标对象</param>
    public static void Off<T>(string eventName, Action<T> callback, object target = null)
    {
        lock (Lock)
        {
            if (!EventMap.ContainsKey(eventName))
            {
                Debug.LogWarning($"EventSystem: 尝试移除不存在的事件 {eventName}");
                return;
            }

            target ??= callback?.Target ?? GlobalObj;

            if (!EventMap[eventName].ContainsKey(target))
            {
                Debug.LogWarning($"EventSystem: 事件 {eventName} 在目标 {target} 上不存在");
                return;
            }

            var eventInfos = EventMap[eventName][target];
            var eventInfo = eventInfos.FirstOrDefault(ei => ei.Callback.Equals(callback));

            if (eventInfo != null)
            {
                eventInfos.Remove(eventInfo);
                Debug.Log($"EventSystem: 移除事件 {eventName}, 目标: {target}, 类型: {typeof(T).Name}");

                // 清理空目标
                if (eventInfos.Count == 0)
                {
                    EventMap[eventName].Remove(target);
                }

                // 清理空事件
                if (EventMap[eventName].Count == 0)
                {
                    EventMap.Remove(eventName);
                    EventTypes.Remove(eventName);
                }
            }
            else
            {
                Debug.LogWarning($"EventSystem: 未找到要移除的事件 {eventName} 回调");
            }
        }
    }

    /// <summary>
    /// 移除无参事件监听
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">事件回调</param>
    /// <param name="target">事件目标对象</param>
    public static void Off(string eventName, Action callback, object target = null)
    {
        lock (Lock)
        {
            if (!EventMap.TryGetValue(eventName, out var targetMap))
            {
                Debug.LogWarning($"EventSystem: 尝试移除不存在的事件 {eventName}");
                return;
            }

            target ??= callback?.Target ?? GlobalObj;

            if (!targetMap.ContainsKey(target))
            {
                Debug.LogWarning($"EventSystem: 事件 {eventName} 在目标 {target} 上不存在");
                return;
            }

            var eventInfos = targetMap[target];
            var eventInfo = eventInfos.FirstOrDefault(ei => ei.Callback.Equals(callback));

            if (eventInfo != null)
            {
                eventInfos.Remove(eventInfo);
                Debug.Log($"EventSystem: 移除无参事件 {eventName}, 目标: {target}");

                // 清理空目标
                if (eventInfos.Count == 0)
                {
                    targetMap.Remove(target);
                }

                // 清理空事件
                if (targetMap.Count == 0)
                {
                    EventMap.Remove(eventName);
                    EventTypes.Remove(eventName);
                }
            }
            else
            {
                Debug.LogWarning($"EventSystem: 未找到要移除的无参事件 {eventName} 回调");
            }
        }
    }

    /// <summary>
    /// 移除指定目标的所有事件
    /// </summary>
    /// <param name="target">目标对象</param>
    public static void RemoveAllInTarget(object target)
    {
        if (target == null) return;

        lock (Lock)
        {
            var eventsToRemove = new List<string>();

            foreach (var eventPair in EventMap)
            {
                var eventName = eventPair.Key;
                var targetMap = eventPair.Value;

                if (targetMap.ContainsKey(target))
                {
                    targetMap.Remove(target);
                    Debug.Log($"EventSystem: 移除目标 {target} 上的所有 {eventName} 事件监听");

                    if (targetMap.Count == 0)
                    {
                        eventsToRemove.Add(eventName);
                    }
                }
            }

            // 清理空事件
            foreach (var eventName in eventsToRemove)
            {
                EventMap.Remove(eventName);
                EventTypes.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// 移除指定事件的所有监听
    /// </summary>
    /// <param name="eventName">事件名称</param>
    public static void RemoveAllEvent(string eventName)
    {
        lock (Lock)
        {
            if (EventMap.Remove(eventName))
            {
                EventTypes.Remove(eventName);
                Debug.Log($"EventSystem: 移除事件 {eventName} 的所有监听");
            }
        }
    }

    /// <summary>
    /// 清空所有事件监听
    /// </summary>
    public static void Clear()
    {
        lock (Lock)
        {
            EventMap.Clear();
            EventTypes.Clear();
            Debug.Log("EventSystem: 已清空所有事件监听");
        }
    }
    #endregion

    #region 触发事件
    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="args">事件参数</param>
    public static void Emit(string eventName, object args = null)
    {
        if (!EventMap.ContainsKey(eventName))
        {
            Debug.LogWarning($"EventSystem: 尝试触发不存在的事件 {eventName}");
            return;
        }

        lock (Lock)
        {
            if (!EventMap.TryGetValue(eventName, out var targetMap)) return;

            // 收集所有回调，避免在迭代过程中修改集合
            var allCallbacks = new List<Action<object>>();

            foreach (var targetPair in targetMap)
            {
                foreach (var eventInfo in targetPair.Value)
                {
                    allCallbacks.Add(eventInfo.InvokeAction);
                }
            }

            // 执行所有回调
            foreach (var callback in allCallbacks)
            {
                try
                {
                    callback?.Invoke(args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"EventSystem: 执行事件 {eventName} 回调时发生错误: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 触发泛型事件
    /// </summary>
    /// <typeparam name="T">事件参数类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="args">事件参数</param>
    public static void Emit<T>(string eventName, T args = default)
    {
        Emit(eventName, args);
    }
    #endregion

    #region 查询功能
    /// <summary>
    /// 获取事件参数类型
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>事件参数类型，如果不存在返回null</returns>
    public static Type GetEventType(string eventName)
    {
        lock (Lock)
        {
            return EventTypes.GetValueOrDefault(eventName, null);
        }
    }

    /// <summary>
    /// 检查事件是否已注册
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>是否存在</returns>
    public static bool HasEvent(string eventName)
    {
        lock (Lock)
        {
            return EventMap.ContainsKey(eventName);
        }
    }

    /// <summary>
    /// 获取事件的监听者数量
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>监听者数量</returns>
    public static int GetListenerCount(string eventName)
    {
        lock (Lock)
        {
            if (EventMap.TryGetValue(eventName, out var targetMap))
            {
                return targetMap.Sum(kv => kv.Value.Count);
            }
            return 0;
        }
    }

    /// <summary>
    /// 获取所有事件信息（用于调试）
    /// </summary>
    /// <returns>事件信息字符串</returns>
    public static string GetEventSystemInfo()
    {
        lock (Lock)
        {
            var info = "Event System Info:\n";
            info += $"Total Events: {EventMap.Count}\n";
            info += $"Total Listeners: {EventCount}\n\n";

            foreach (var eventPair in EventMap)
            {
                var eventName = eventPair.Key;
                var targetMap = eventPair.Value;
                var listenerCount = targetMap.Sum(kv => kv.Value.Count);
                var eventType = GetEventType(eventName)?.Name ?? "Unknown";

                info += $"Event: {eventName} (Type: {eventType}, Listeners: {listenerCount})\n";

                foreach (var targetPair in targetMap)
                {
                    var target = targetPair.Key;
                    var eventInfos = targetPair.Value;

                    info += $"  Target: {target} ({target?.GetType().Name ?? "null"})\n";
                    info += $"    Callbacks: {eventInfos.Count}\n";
                }

                info += "\n";
            }

            return info;
        }
    }
    #endregion
}