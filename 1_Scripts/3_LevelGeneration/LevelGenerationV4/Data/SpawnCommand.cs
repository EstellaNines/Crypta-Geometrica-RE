using System;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 实体生成指令
    /// </summary>
    [Serializable]
    public struct SpawnCommand
    {
        /// <summary>
        /// 世界坐标位置
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 预制体引用
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// 生成延迟（秒）
        /// </summary>
        public float Delay;

        /// <summary>
        /// 附加数据（JSON格式）
        /// </summary>
        public string ExtraData;

        /// <summary>
        /// 创建生成指令
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <param name="prefab">预制体</param>
        /// <param name="delay">延迟时间</param>
        public SpawnCommand(Vector3 position, GameObject prefab, float delay = 0f)
        {
            Position = position;
            Prefab = prefab;
            Delay = delay;
            ExtraData = string.Empty;
        }
    }
}
