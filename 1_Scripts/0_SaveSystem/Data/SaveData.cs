using System;
using System.Collections.Generic;

/// <summary>
/// 存档数据 - 完整结构
/// 包含存档头信息、全局数据、PCG数据和实体数据
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>存档头信息</summary>
    public SaveHeader header;

    /// <summary>全局游戏数据</summary>
    public GlobalSaveData globalData;

    /// <summary>PCG 程序化生成数据</summary>
    public PCGSaveData pcgData;

    /// <summary>
    /// 实体数据字典
    /// Key: SaveID (GUID)
    /// Value: 序列化后的 JSON 字符串
    /// </summary>
    public SerializableDictionary<string, string> entityData;

    /// <summary>动态生成的物体列表</summary>
    public List<DynamicObjectData> dynamicObjects;

    /// <summary>
    /// 创建默认存档数据
    /// </summary>
    public SaveData()
    {
        header = new SaveHeader();
        globalData = new GlobalSaveData();
        pcgData = new PCGSaveData();
        entityData = new SerializableDictionary<string, string>();
        dynamicObjects = new List<DynamicObjectData>();
    }
}

/// <summary>
/// 可序列化的字典包装类
/// 用于解决 Unity JsonUtility 不支持 Dictionary 的问题
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();

    public void Add(TKey key, TValue value)
    {
        keys.Add(key);
        values.Add(value);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        int index = keys.IndexOf(key);
        if (index >= 0)
        {
            value = values[index];
            return true;
        }
        value = default;
        return false;
    }

    public bool ContainsKey(TKey key)
    {
        return keys.Contains(key);
    }

    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    public int Count => keys.Count;

    public TValue this[TKey key]
    {
        get
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
                return values[index];
            throw new KeyNotFoundException($"Key not found: {key}");
        }
        set
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
                values[index] = value;
            else
                Add(key, value);
        }
    }
}
