using System;
using System.Collections.Generic;

/// <summary>
/// PCG 程序化生成数据
/// 存储用于重新生成关卡的种子和元数据
/// </summary>
[Serializable]
public class PCGSaveData
{
    /// <summary>主种子 - 用于生成其他种子</summary>
    public int masterSeed;

    /// <summary>各关卡的种子列表</summary>
    public List<int> levelSeeds;

    /// <summary>
    /// 元数据字典
    /// 用于存储额外的生成参数
    /// </summary>
    public SerializableDictionary<string, string> metadata;

    /// <summary>当前关卡在 levelSeeds 中的索引</summary>
    public int currentLevelIndex;

    /// <summary>
    /// 创建默认 PCG 数据
    /// </summary>
    public PCGSaveData()
    {
        masterSeed = 0;
        levelSeeds = new List<int>();
        metadata = new SerializableDictionary<string, string>();
        currentLevelIndex = 0;
    }

    /// <summary>
    /// 使用主种子初始化
    /// </summary>
    /// <param name="seed">主种子</param>
    /// <param name="levelCount">关卡数量</param>
    public void Initialize(int seed, int levelCount = 10)
    {
        masterSeed = seed;
        levelSeeds.Clear();

        // 使用主种子生成各关卡种子
        var random = new System.Random(masterSeed);
        for (int i = 0; i < levelCount; i++)
        {
            levelSeeds.Add(random.Next());
        }

        currentLevelIndex = 0;
    }

    /// <summary>
    /// 获取当前关卡的种子
    /// </summary>
    public int GetCurrentLevelSeed()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levelSeeds.Count)
        {
            return levelSeeds[currentLevelIndex];
        }
        return masterSeed;
    }

    /// <summary>
    /// 设置元数据
    /// </summary>
    public void SetMetadata(string key, string value)
    {
        metadata[key] = value;
    }

    /// <summary>
    /// 获取元数据
    /// </summary>
    public string GetMetadata(string key, string defaultValue = "")
    {
        if (metadata.TryGetValue(key, out string value))
        {
            return value;
        }
        return defaultValue;
    }
}
