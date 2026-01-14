using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局消息管理器
/// 基于发布-订阅模式实现模块间低耦合通信
/// </summary>
public static class MessageManager
{
    // 事件表：存储所有注册的事件和对应的委托
    private static Dictionary<MessageType, Delegate> messageTable = new Dictionary<MessageType, Delegate>();

    // 永久消息列表：场景切换时不会被清理
    private static List<MessageType> permanentMessages = new List<MessageType>();

    // 是否启用日志
    private static bool enableLog = false;

    #region 配置方法

    /// <summary>
    /// 设置是否启用日志
    /// </summary>
    public static void SetLogEnabled(bool enabled)
    {
        enableLog = enabled;
    }

    /// <summary>
    /// 标记为永久消息（场景切换时不清理）
    /// </summary>
    public static void MarkAsPermanent(MessageType messageType)
    {
        if (!permanentMessages.Contains(messageType))
        {
            permanentMessages.Add(messageType);
            Log($"[MessageManager] 标记永久消息: {messageType}");
        }
    }

    /// <summary>
    /// 清理非永久消息（场景切换时调用）
    /// </summary>
    public static void Cleanup()
    {
        List<MessageType> toRemove = new List<MessageType>();

        foreach (var pair in messageTable)
        {
            if (!permanentMessages.Contains(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (var messageType in toRemove)
        {
            messageTable.Remove(messageType);
        }

        Log($"[MessageManager] 清理完成，移除 {toRemove.Count} 个消息");
    }

    #endregion

    #region 添加监听

    public static void AddListener(MessageType messageType, Callback handler)
    {
        OnListenerAdding(messageType, handler);
        messageTable[messageType] = (Callback)messageTable[messageType] + handler;
        Log($"[MessageManager] 添加监听: {messageType}");
    }

    public static void AddListener<T>(MessageType messageType, Callback<T> handler)
    {
        OnListenerAdding(messageType, handler);
        messageTable[messageType] = (Callback<T>)messageTable[messageType] + handler;
        Log($"[MessageManager] 添加监听: {messageType}");
    }

    public static void AddListener<T, U>(MessageType messageType, Callback<T, U> handler)
    {
        OnListenerAdding(messageType, handler);
        messageTable[messageType] = (Callback<T, U>)messageTable[messageType] + handler;
        Log($"[MessageManager] 添加监听: {messageType}");
    }

    public static void AddListener<T, U, V>(MessageType messageType, Callback<T, U, V> handler)
    {
        OnListenerAdding(messageType, handler);
        messageTable[messageType] = (Callback<T, U, V>)messageTable[messageType] + handler;
        Log($"[MessageManager] 添加监听: {messageType}");
    }

    #endregion

    #region 移除监听

    public static void RemoveListener(MessageType messageType, Callback handler)
    {
        if (OnListenerRemoving(messageType, handler))
        {
            messageTable[messageType] = (Callback)messageTable[messageType] - handler;
            OnListenerRemoved(messageType);
            Log($"[MessageManager] 移除监听: {messageType}");
        }
    }

    public static void RemoveListener<T>(MessageType messageType, Callback<T> handler)
    {
        if (OnListenerRemoving(messageType, handler))
        {
            messageTable[messageType] = (Callback<T>)messageTable[messageType] - handler;
            OnListenerRemoved(messageType);
            Log($"[MessageManager] 移除监听: {messageType}");
        }
    }

    public static void RemoveListener<T, U>(MessageType messageType, Callback<T, U> handler)
    {
        if (OnListenerRemoving(messageType, handler))
        {
            messageTable[messageType] = (Callback<T, U>)messageTable[messageType] - handler;
            OnListenerRemoved(messageType);
            Log($"[MessageManager] 移除监听: {messageType}");
        }
    }

    public static void RemoveListener<T, U, V>(MessageType messageType, Callback<T, U, V> handler)
    {
        if (OnListenerRemoving(messageType, handler))
        {
            messageTable[messageType] = (Callback<T, U, V>)messageTable[messageType] - handler;
            OnListenerRemoved(messageType);
            Log($"[MessageManager] 移除监听: {messageType}");
        }
    }

    #endregion

    #region 广播消息

    public static void Broadcast(MessageType messageType)
    {
        Log($"[MessageManager] 广播: {messageType}");
        MessageMonitor.RecordBroadcast(messageType);

        if (messageTable.TryGetValue(messageType, out Delegate d))
        {
            if (d is Callback callback)
            {
                callback();
            }
            else
            {
                ThrowBroadcastException(messageType);
            }
        }
    }

    public static void Broadcast<T>(MessageType messageType, T arg1)
    {
        Log($"[MessageManager] 广播: {messageType}, 参数: {arg1}");
        MessageMonitor.RecordBroadcast(messageType, arg1);

        if (messageTable.TryGetValue(messageType, out Delegate d))
        {
            if (d is Callback<T> callback)
            {
                callback(arg1);
            }
            else
            {
                ThrowBroadcastException(messageType);
            }
        }
    }

    public static void Broadcast<T, U>(MessageType messageType, T arg1, U arg2)
    {
        Log($"[MessageManager] 广播: {messageType}");
        MessageMonitor.RecordBroadcast(messageType, arg1, arg2);

        if (messageTable.TryGetValue(messageType, out Delegate d))
        {
            if (d is Callback<T, U> callback)
            {
                callback(arg1, arg2);
            }
            else
            {
                ThrowBroadcastException(messageType);
            }
        }
    }

    public static void Broadcast<T, U, V>(MessageType messageType, T arg1, U arg2, V arg3)
    {
        Log($"[MessageManager] 广播: {messageType}");
        MessageMonitor.RecordBroadcast(messageType, arg1, arg2, arg3);

        if (messageTable.TryGetValue(messageType, out Delegate d))
        {
            if (d is Callback<T, U, V> callback)
            {
                callback(arg1, arg2, arg3);
            }
            else
            {
                ThrowBroadcastException(messageType);
            }
        }
    }

    #endregion

    #region 内部方法

    private static void OnListenerAdding(MessageType messageType, Delegate handler)
    {
        if (!messageTable.ContainsKey(messageType))
        {
            messageTable.Add(messageType, null);
        }

        Delegate d = messageTable[messageType];
        if (d != null && d.GetType() != handler.GetType())
        {
            throw new MessageException($"添加监听失败: 消息 {messageType} 的签名不一致。" +
                $"当前类型: {d.GetType().Name}, 尝试添加: {handler.GetType().Name}");
        }
    }

    private static bool OnListenerRemoving(MessageType messageType, Delegate handler)
    {
        if (!messageTable.ContainsKey(messageType))
        {
            Debug.LogWarning($"[MessageManager] 移除监听失败: 消息 {messageType} 不存在");
            return false;
        }

        Delegate d = messageTable[messageType];
        if (d == null)
        {
            Debug.LogWarning($"[MessageManager] 移除监听失败: 消息 {messageType} 的监听器为空");
            return false;
        }

        if (d.GetType() != handler.GetType())
        {
            throw new MessageException($"移除监听失败: 消息 {messageType} 的签名不一致");
        }

        return true;
    }

    private static void OnListenerRemoved(MessageType messageType)
    {
        if (messageTable[messageType] == null)
        {
            messageTable.Remove(messageType);
        }
    }

    private static void ThrowBroadcastException(MessageType messageType)
    {
        throw new MessageException($"广播失败: 消息 {messageType} 的监听器签名与广播参数不匹配");
    }

    private static void Log(string message)
    {
        if (enableLog)
        {
            Debug.Log(message);
        }
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印当前所有注册的消息
    /// </summary>
    public static void PrintMessageTable()
    {
        Debug.Log("========== MessageManager 消息表 ==========");
        foreach (var pair in messageTable)
        {
            Debug.Log($"  {pair.Key}: {pair.Value?.GetType().Name ?? "null"}");
        }
        Debug.Log($"永久消息数: {permanentMessages.Count}");
        Debug.Log("============================================");
    }

    /// <summary>
    /// 检查是否有监听器
    /// </summary>
    public static bool HasListener(MessageType messageType)
    {
        return messageTable.ContainsKey(messageType) && messageTable[messageType] != null;
    }

    /// <summary>
    /// 获取事件总数
    /// </summary>
    public static int GetEventCount()
    {
        return messageTable.Count;
    }

    /// <summary>
    /// 获取永久事件列表
    /// </summary>
    public static List<string> GetPermanentEvents()
    {
        List<string> result = new List<string>();
        foreach (var messageType in permanentMessages)
        {
            result.Add(messageType.ToString());
        }
        return result;
    }

    /// <summary>
    /// 获取事件表（只读）
    /// </summary>
    public static Dictionary<MessageType, Delegate> GetEventTable()
    {
        return new Dictionary<MessageType, Delegate>(messageTable);
    }

    /// <summary>
    /// 获取指定事件的监听器数量
    /// </summary>
    public static int GetListenerCount(MessageType messageType)
    {
        if (!messageTable.ContainsKey(messageType))
            return 0;

        Delegate d = messageTable[messageType];
        return d?.GetInvocationList().Length ?? 0;
    }

    /// <summary>
    /// 检查是否为永久消息
    /// </summary>
    public static bool IsPermanent(MessageType messageType)
    {
        return permanentMessages.Contains(messageType);
    }

    #endregion
}

/// <summary>
/// 消息系统异常
/// </summary>
public class MessageException : Exception
{
    public MessageException(string message) : base(message) { }
}
