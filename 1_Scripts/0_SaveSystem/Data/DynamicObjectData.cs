using System;

/// <summary>
/// 动态生成物体数据
/// 用于保存运行时动态创建的物体（如掉落物）
/// </summary>
[Serializable]
public class DynamicObjectData
{
    /// <summary>
    /// 预制体路径
    /// 用于从 Resources 重新实例化
    /// </summary>
    public string prefabPath;

    /// <summary>
    /// 运行时唯一标识
    /// 用于区分同一预制体的不同实例
    /// </summary>
    public string runtimeId;

    /// <summary>
    /// 序列化后的状态数据 (JSON 字符串)
    /// </summary>
    public string serializedState;

    /// <summary>
    /// 位置 X
    /// </summary>
    public float posX;

    /// <summary>
    /// 位置 Y
    /// </summary>
    public float posY;

    /// <summary>
    /// 位置 Z
    /// </summary>
    public float posZ;

    /// <summary>
    /// 创建默认动态物体数据
    /// </summary>
    public DynamicObjectData()
    {
        prefabPath = string.Empty;
        runtimeId = string.Empty;
        serializedState = string.Empty;
        posX = 0f;
        posY = 0f;
        posZ = 0f;
    }

    /// <summary>
    /// 创建动态物体数据
    /// </summary>
    /// <param name="path">预制体路径</param>
    /// <param name="id">运行时ID</param>
    /// <param name="state">状态JSON</param>
    /// <param name="position">位置</param>
    public DynamicObjectData(string path, string id, string state, UnityEngine.Vector3 position)
    {
        prefabPath = path;
        runtimeId = id;
        serializedState = state;
        posX = position.x;
        posY = position.y;
        posZ = position.z;
    }

    /// <summary>
    /// 获取位置向量
    /// </summary>
    public UnityEngine.Vector3 GetPosition()
    {
        return new UnityEngine.Vector3(posX, posY, posZ);
    }
}
