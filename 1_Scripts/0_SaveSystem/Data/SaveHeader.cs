using System;

/// <summary>
/// 存档头信息
/// 用于快速预览存档信息，无需加载完整数据
/// </summary>
[Serializable]
public class SaveHeader
{
    /// <summary>存档版本号</summary>
    public string version;

    /// <summary>保存时间戳 (ISO 8601 格式)</summary>
    public string timestamp;

    /// <summary>游玩时间 (秒)</summary>
    public float playTime;

    /// <summary>当前场景名称</summary>
    public string sceneName;

    /// <summary>存档槽位索引</summary>
    public int slotIndex;

    /// <summary>
    /// 创建默认头信息
    /// </summary>
    public SaveHeader()
    {
        version = SaveUtility.SAVE_VERSION;
        timestamp = DateTime.Now.ToString("O");
        playTime = 0f;
        sceneName = string.Empty;
        slotIndex = -1;
    }

    /// <summary>
    /// 获取格式化的保存时间
    /// </summary>
    public string GetFormattedTime()
    {
        if (DateTime.TryParse(timestamp, out DateTime dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm");
        }
        return timestamp;
    }

    /// <summary>
    /// 获取格式化的游玩时间
    /// </summary>
    public string GetFormattedPlayTime()
    {
        TimeSpan ts = TimeSpan.FromSeconds(playTime);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
