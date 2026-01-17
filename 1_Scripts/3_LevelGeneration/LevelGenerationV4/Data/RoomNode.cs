using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 房间节点数据
    /// 包含拓扑信息、出入口约束和连接关系
    /// </summary>
    [Serializable]
    public struct RoomNode
    {
        /// <summary>
        /// 网格坐标
        /// </summary>
        public Vector2Int GridPosition;

        /// <summary>
        /// 房间类型（Normal/Start/End）
        /// </summary>
        public RoomType Type;

        /// <summary>
        /// 关卡门类型（用于标记关卡入口/出口）
        /// </summary>
        public LevelDoorType DoorType;

        /// <summary>
        /// 限定的门方向（仅限Left或Right，用于侧向出入口约束）
        /// </summary>
        public WallDirection RestrictedDoorSide;

        /// <summary>
        /// 是否在关键路径上
        /// </summary>
        public bool IsCritical;

        /// <summary>
        /// 世界坐标边界
        /// </summary>
        public BoundsInt WorldBounds;

        /// <summary>
        /// 连接的邻居房间坐标列表
        /// </summary>
        public List<Vector2Int> ConnectedNeighbors;

        /// <summary>
        /// 创建房间节点
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <param name="type">房间类型</param>
        /// <param name="worldBounds">世界坐标边界</param>
        public RoomNode(Vector2Int gridPos, RoomType type, BoundsInt worldBounds)
        {
            GridPosition = gridPos;
            Type = type;
            DoorType = LevelDoorType.None;
            RestrictedDoorSide = WallDirection.None;
            IsCritical = false;
            WorldBounds = worldBounds;
            ConnectedNeighbors = new List<Vector2Int>();
        }

        /// <summary>
        /// 获取网格索引（用于邻接矩阵）
        /// </summary>
        /// <param name="gridColumns">网格列数</param>
        /// <returns>一维索引</returns>
        public int GetIndex(int gridColumns)
        {
            return GridPosition.y * gridColumns + GridPosition.x;
        }

        /// <summary>
        /// 设置为起始房间（带侧向入口约束）
        /// </summary>
        /// <param name="doorSide">入口方向（Left或Right）</param>
        public void SetAsStart(WallDirection doorSide)
        {
            Type = RoomType.Start;
            DoorType = LevelDoorType.LevelEntrance;
            RestrictedDoorSide = doorSide;
            IsCritical = true;
        }

        /// <summary>
        /// 设置为终点房间（带侧向出口约束）
        /// </summary>
        /// <param name="doorSide">出口方向（Left或Right）</param>
        public void SetAsEnd(WallDirection doorSide)
        {
            Type = RoomType.End;
            DoorType = LevelDoorType.LevelExit;
            RestrictedDoorSide = doorSide;
            IsCritical = true;
        }

        /// <summary>
        /// 添加邻居连接
        /// </summary>
        /// <param name="neighborPos">邻居网格坐标</param>
        public void AddNeighbor(Vector2Int neighborPos)
        {
            ConnectedNeighbors ??= new List<Vector2Int>();
            if (!ConnectedNeighbors.Contains(neighborPos))
            {
                ConnectedNeighbors.Add(neighborPos);
            }
        }
    }
}
