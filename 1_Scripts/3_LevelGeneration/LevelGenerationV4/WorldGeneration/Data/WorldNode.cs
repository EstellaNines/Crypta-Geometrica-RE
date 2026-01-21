using System;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 世界节点数据类
    /// 表示世界网格中的一个房间位置
    /// </summary>
    [Serializable]
    public class WorldNode
    {
        #region 网格位置

        /// <summary>
        /// 网格坐标 (0~GridSize-1, 0~GridSize-1)
        /// </summary>
        [Tooltip("世界网格中的坐标位置")]
        public Vector2Int GridPosition;

        #endregion

        #region 世界坐标

        /// <summary>
        /// 世界像素偏移量
        /// 计算方式: GridPosition × RoomPixelSize
        /// </summary>
        [Tooltip("世界像素坐标偏移")]
        public Vector2Int WorldPixelOffset;

        #endregion

        #region 房间生成

        /// <summary>
        /// 房间生成种子
        /// 用于DungeonGenerator生成房间时的随机种子
        /// </summary>
        [Tooltip("房间生成使用的随机种子")]
        public int RoomSeed;

        /// <summary>
        /// 是否已生成房间
        /// </summary>
        [Tooltip("标记该节点的房间是否已生成")]
        public bool IsGenerated;

        #endregion

        #region 出入口位置

        /// <summary>
        /// 入口像素位置（相对于世界原点）
        /// </summary>
        [Tooltip("房间入口的世界像素坐标")]
        public Vector2Int EntrancePosition;

        /// <summary>
        /// 出口像素位置（相对于世界原点）
        /// </summary>
        [Tooltip("房间出口的世界像素坐标")]
        public Vector2Int ExitPosition;

        /// <summary>
        /// 是否有有效的出入口数据
        /// </summary>
        [Tooltip("标记是否已设置出入口位置")]
        public bool HasEntranceExitData;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WorldNode()
        {
            GridPosition = Vector2Int.zero;
            WorldPixelOffset = Vector2Int.zero;
            RoomSeed = 0;
            IsGenerated = false;
        }

        /// <summary>
        /// 带网格位置的构造函数
        /// </summary>
        /// <param name="gridPosition">网格坐标</param>
        /// <param name="roomSeed">房间生成种子</param>
        public WorldNode(Vector2Int gridPosition, int roomSeed)
        {
            GridPosition = gridPosition;
            WorldPixelOffset = Vector2Int.zero;
            RoomSeed = roomSeed;
            IsGenerated = false;
        }

        /// <summary>
        /// 完整构造函数
        /// </summary>
        /// <param name="gridPosition">网格坐标</param>
        /// <param name="worldPixelOffset">世界像素偏移</param>
        /// <param name="roomSeed">房间生成种子</param>
        public WorldNode(Vector2Int gridPosition, Vector2Int worldPixelOffset, int roomSeed)
        {
            GridPosition = gridPosition;
            WorldPixelOffset = worldPixelOffset;
            RoomSeed = roomSeed;
            IsGenerated = false;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算世界像素偏移
        /// </summary>
        /// <param name="roomPixelSize">单房间像素尺寸</param>
        public void CalculateWorldOffset(Vector2Int roomPixelSize)
        {
            WorldPixelOffset = new Vector2Int(
                GridPosition.x * roomPixelSize.x,
                GridPosition.y * roomPixelSize.y
            );
        }

        /// <summary>
        /// 重置生成状态
        /// </summary>
        public void Reset()
        {
            IsGenerated = false;
        }

        /// <summary>
        /// 调试信息输出
        /// </summary>
        /// <returns>节点信息字符串</returns>
        public override string ToString()
        {
            return $"WorldNode[Grid:{GridPosition}, World:{WorldPixelOffset}, Seed:{RoomSeed}, Generated:{IsGenerated}]";
        }

        #endregion
    }
}
