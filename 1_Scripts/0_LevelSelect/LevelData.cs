using System;
using UnityEngine;

/// <summary>
/// 单个关卡数据
/// </summary>
[Serializable]
public class LevelData
{
    [Tooltip("关卡显示名称 (如 1-1, 1-2)")]
    public string displayName;

    [Tooltip("关卡场景名称")]
    public string sceneName;

    [Tooltip("是否已解锁")]
    public bool isUnlocked;

    [Tooltip("是否已通关")]
    public bool isCompleted;

    [Tooltip("最佳完成时间 (秒)")]
    public float bestTime;

    [Tooltip("关卡图标 (可选)")]
    public Sprite icon;
}

/// <summary>
/// 章节数据 (包含多个关卡)
/// </summary>
[Serializable]
public class ChapterData
{
    [Tooltip("章节名称 (如 Chapter 1)")]
    public string chapterName;

    [Tooltip("章节内的关卡列表")]
    public LevelData[] levels;
}

/// <summary>
/// 关卡配置 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Crypta Geometrica:RE/关卡配置/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("章节列表")]
    public ChapterData[] chapters;

    /// <summary>
    /// 获取关卡总数
    /// </summary>
    public int GetTotalLevelCount()
    {
        int count = 0;
        foreach (var chapter in chapters)
        {
            count += chapter.levels.Length;
        }
        return count;
    }

    /// <summary>
    /// 根据索引获取关卡数据
    /// </summary>
    public LevelData GetLevel(int chapterIndex, int levelIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Length) return null;
        if (levelIndex < 0 || levelIndex >= chapters[chapterIndex].levels.Length) return null;
        return chapters[chapterIndex].levels[levelIndex];
    }

    /// <summary>
    /// 获取关卡的全局索引
    /// </summary>
    public int GetGlobalIndex(int chapterIndex, int levelIndex)
    {
        int index = 0;
        for (int i = 0; i < chapterIndex && i < chapters.Length; i++)
        {
            index += chapters[i].levels.Length;
        }
        return index + levelIndex;
    }
}
