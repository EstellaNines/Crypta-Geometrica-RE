using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 消息监控器 - 运行时数据管理
/// 负责记录所有经过MessageManager的消息
/// </summary>
public static class MessageMonitor
{
    // 消息记录列表
    private static List<MessageRecord> records = new List<MessageRecord>();
    
    // 最大记录数量
    private static int maxRecords = 500;
    
    // 是否启用监控
    private static bool isEnabled = true;
    
    // 是否暂停记录
    private static bool isPaused = false;
    
    // 记录索引计数器
    private static int recordIndex = 0;

    // 新消息事件（用于Editor刷新）
    public static event Action OnNewRecord;

    #region 属性

    public static List<MessageRecord> Records => records;
    public static bool IsEnabled { get => isEnabled; set => isEnabled = value; }
    public static bool IsPaused { get => isPaused; set => isPaused = value; }
    public static int MaxRecords { get => maxRecords; set => maxRecords = Mathf.Max(10, value); }
    public static int RecordCount => records.Count;

    #endregion

    #region 记录方法

    /// <summary>
    /// 记录广播消息
    /// </summary>
    public static void RecordBroadcast(MessageType messageType, string remark = "")
    {
        AddRecord(messageType, "Broadcast", "-", "-", "-", remark);
    }

    /// <summary>
    /// 记录带1个参数的广播
    /// </summary>
    public static void RecordBroadcast<T>(MessageType messageType, T arg1, string remark = "")
    {
        AddRecord(messageType, "Broadcast", FormatArg(arg1), "-", "-", remark);
    }

    /// <summary>
    /// 记录带2个参数的广播
    /// </summary>
    public static void RecordBroadcast<T, U>(MessageType messageType, T arg1, U arg2, string remark = "")
    {
        AddRecord(messageType, "Broadcast", FormatArg(arg1), FormatArg(arg2), "-", remark);
    }

    /// <summary>
    /// 记录带3个参数的广播
    /// </summary>
    public static void RecordBroadcast<T, U, V>(MessageType messageType, T arg1, U arg2, V arg3, string remark = "")
    {
        AddRecord(messageType, "Broadcast", FormatArg(arg1), FormatArg(arg2), FormatArg(arg3), remark);
    }

    /// <summary>
    /// 记录添加监听
    /// </summary>
    public static void RecordAddListener(MessageType messageType, string handlerName = "")
    {
        AddRecord(messageType, "AddListener", handlerName, "-", "-", "注册监听");
    }

    /// <summary>
    /// 记录移除监听
    /// </summary>
    public static void RecordRemoveListener(MessageType messageType, string handlerName = "")
    {
        AddRecord(messageType, "RemoveListener", handlerName, "-", "-", "移除监听");
    }

    #endregion

    #region 内部方法

    private static void AddRecord(MessageType messageType, string actionType, 
        string param1, string param2, string param3, string remark)
    {
        if (!isEnabled || isPaused) return;

        var record = new MessageRecord(
            ++recordIndex,
            messageType,
            actionType,
            param1,
            param2,
            param3,
            remark
        );

        records.Add(record);

        // 超出最大数量时移除旧记录
        while (records.Count > maxRecords)
        {
            records.RemoveAt(0);
        }

        // 触发新记录事件
        OnNewRecord?.Invoke();
    }

    private static string FormatArg(object arg)
    {
        if (arg == null) return "null";
        
        // 限制字符串长度
        string str = arg.ToString();
        if (str.Length > 50)
        {
            str = str.Substring(0, 47) + "...";
        }
        return str;
    }

    #endregion

    #region 控制方法

    /// <summary>
    /// 清空所有记录
    /// </summary>
    public static void ClearRecords()
    {
        records.Clear();
        recordIndex = 0;
    }

    /// <summary>
    /// 暂停/恢复记录
    /// </summary>
    public static void TogglePause()
    {
        isPaused = !isPaused;
    }

    /// <summary>
    /// 启用/禁用监控
    /// </summary>
    public static void ToggleEnabled()
    {
        isEnabled = !isEnabled;
    }

    #endregion
}
