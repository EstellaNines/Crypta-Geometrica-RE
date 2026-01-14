using System;

/// <summary>
/// 全局游戏数据
/// 存储与场景无关的全局游戏状态
/// </summary>
[Serializable]
public class GlobalSaveData
{
    /// <summary>游戏难度 (0=Easy, 1=Normal, 2=Hard)</summary>
    public int difficulty;

    /// <summary>当前关卡索引</summary>
    public int currentLevel;

    /// <summary>当前生命值（最大3）</summary>
    public int currentHearts;

    /// <summary>玩家金币数量</summary>
    public int playerGold;

    /// <summary>玩家总击杀数</summary>
    public int totalKills;

    /// <summary>玩家总死亡次数</summary>
    public int totalDeaths;

    /// <summary>是否已完成教程</summary>
    public bool tutorialCompleted;

    /// <summary>
    /// 创建默认全局数据
    /// </summary>
    public GlobalSaveData()
    {
        difficulty = 1; // Normal
        currentLevel = 0;
        currentHearts = 3; // 默认最大生命值
        playerGold = 0;
        totalKills = 0;
        totalDeaths = 0;
        tutorialCompleted = false;
    }
}
