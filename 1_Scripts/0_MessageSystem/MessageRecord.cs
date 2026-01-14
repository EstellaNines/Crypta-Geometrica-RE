using System;
using UnityEngine;

/// <summary>
/// 消息记录数据结构
/// 用于监控器显示
/// </summary>
[Serializable]
public class MessageRecord
{
    public int Index;
    public string Timestamp;
    public MessageType MessageType;
    public string MessageName;
    public string ActionType; // Broadcast, AddListener, RemoveListener
    public string Param1;
    public string Param2;
    public string Param3;
    public string Remark;

    public MessageRecord(int index, MessageType messageType, string actionType, 
        string param1 = "-", string param2 = "-", string param3 = "-", string remark = "")
    {
        Index = index;
        Timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        MessageType = messageType;
        MessageName = messageType.ToString();
        ActionType = actionType;
        Param1 = param1;
        Param2 = param2;
        Param3 = param3;
        Remark = remark;
    }

    /// <summary>
    /// 根据消息类型获取颜色
    /// </summary>
    public Color GetTypeColor()
    {
        string typeName = MessageType.ToString();

        if (typeName.StartsWith("GAME_"))
            return new Color(0.2f, 0.8f, 0.2f); // 绿色 - 系统消息
        if (typeName.StartsWith("PLAYER_"))
            return new Color(0.3f, 0.6f, 1f);   // 蓝色 - 玩家消息
        if (typeName.StartsWith("UI_"))
            return new Color(1f, 0.8f, 0.2f);   // 黄色 - UI消息
        if (typeName.StartsWith("ENEMY_"))
            return new Color(1f, 0.3f, 0.3f);   // 红色 - 敌人消息
        if (typeName.StartsWith("ITEM_"))
            return new Color(0.8f, 0.5f, 1f);   // 紫色 - 道具消息
        if (typeName.StartsWith("AUDIO_"))
            return new Color(1f, 0.6f, 0.4f);   // 橙色 - 音频消息
        if (typeName.StartsWith("DAMAGE_"))
            return new Color(1f, 0.2f, 0.5f);   // 粉红 - 伤害消息
        if (typeName.StartsWith("SCENE_"))
            return new Color(0.5f, 0.9f, 0.9f); // 青色 - 场景消息

        return Color.white; // 默认白色
    }

    /// <summary>
    /// 根据操作类型获取颜色
    /// </summary>
    public Color GetActionColor()
    {
        switch (ActionType)
        {
            case "Broadcast":
                return new Color(0.4f, 1f, 0.4f);   // 亮绿
            case "AddListener":
                return new Color(0.4f, 0.8f, 1f);   // 亮蓝
            case "RemoveListener":
                return new Color(1f, 0.6f, 0.4f);   // 橙色
            default:
                return Color.white;
        }
    }
}
