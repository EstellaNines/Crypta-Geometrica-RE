using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 世界生成上下文（黑板模式）
    /// 在规则管线执行过程中共享数据
    /// </summary>
    public class WorldContext : IDisposable
    {
        #region 配置参数

        /// <summary>
        /// 网格尺寸（X×X）
        /// </summary>
        public int GridSize { get; set; }

        /// <summary>
        /// 目标房间数量
        /// </summary>
        public int RoomCount { get; set; }

        /// <summary>
        /// 单房间像素尺寸
        /// </summary>
        public Vector2Int RoomPixelSize { get; set; }

        #endregion

        #region 网格数据

        /// <summary>
        /// 占用网格（true=有房间，false=空）
        /// </summary>
        public bool[,] OccupancyGrid { get; set; }

        /// <summary>
        /// 世界节点列表
        /// </summary>
        public List<WorldNode> Nodes { get; set; }

        #endregion

        #region 随机与控制

        /// <summary>
        /// 随机数生成器
        /// </summary>
        public System.Random RNG { get; private set; }

        /// <summary>
        /// 随机种子
        /// </summary>
        public int Seed { get; private set; }

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken Token { get; set; }

        #endregion

        #region 房间生成器引用

        /// <summary>
        /// 房间生成器引用
        /// 用于RoomGenerationRule调用
        /// </summary>
        public DungeonGenerator DungeonGenerator { get; set; }

        #endregion

        #region 状态标记

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建世界上下文
        /// </summary>
        /// <param name="roomCount">目标房间数量</param>
        /// <param name="roomPixelSize">单房间像素尺寸</param>
        /// <param name="seed">随机种子（-1使用系统时间）</param>
        public WorldContext(int roomCount, Vector2Int roomPixelSize, int seed = -1)
        {
            RoomCount = roomCount;
            GridSize = roomCount - 1; // 网格大小 = 房间数量 - 1
            RoomPixelSize = roomPixelSize;

            // 初始化随机数生成器
            Seed = seed == -1 ? Environment.TickCount : seed;
            RNG = new System.Random(Seed);

            // 初始化网格和节点列表
            OccupancyGrid = new bool[GridSize, GridSize];
            Nodes = new List<WorldNode>(roomCount);

            IsInitialized = true;
            IsDisposed = false;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 重置上下文状态
        /// </summary>
        /// <param name="newSeed">新种子（-1保持原种子）</param>
        public void Reset(int newSeed = -1)
        {
            if (IsDisposed)
            {
                Debug.LogWarning("[WorldContext] Cannot reset disposed context.");
                return;
            }

            // 更新种子
            if (newSeed != -1)
            {
                Seed = newSeed;
            }
            RNG = new System.Random(Seed);

            // 清空网格
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    OccupancyGrid[x, y] = false;
                }
            }

            // 清空节点
            Nodes.Clear();
        }

        /// <summary>
        /// 检查网格位置是否在边界内
        /// </summary>
        /// <param name="position">网格坐标</param>
        /// <returns>是否在边界内</returns>
        public bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < GridSize &&
                   position.y >= 0 && position.y < GridSize;
        }

        /// <summary>
        /// 检查网格位置是否被占用
        /// </summary>
        /// <param name="position">网格坐标</param>
        /// <returns>是否被占用</returns>
        public bool IsOccupied(Vector2Int position)
        {
            if (!IsInBounds(position)) return false;
            return OccupancyGrid[position.x, position.y];
        }

        /// <summary>
        /// 设置网格位置占用状态
        /// </summary>
        /// <param name="position">网格坐标</param>
        /// <param name="occupied">占用状态</param>
        public void SetOccupied(Vector2Int position, bool occupied)
        {
            if (!IsInBounds(position))
            {
                Debug.LogWarning($"[WorldContext] Position {position} out of bounds.");
                return;
            }
            OccupancyGrid[position.x, position.y] = occupied;
        }

        /// <summary>
        /// 添加世界节点
        /// </summary>
        /// <param name="node">世界节点</param>
        public void AddNode(WorldNode node)
        {
            Nodes.Add(node);
            SetOccupied(node.GridPosition, true);
        }

        /// <summary>
        /// 获取下一个随机数 [0, 1)
        /// </summary>
        /// <returns>随机浮点数</returns>
        public float NextRandomFloat()
        {
            return (float)RNG.NextDouble();
        }

        /// <summary>
        /// 获取下一个随机整数
        /// </summary>
        /// <param name="maxExclusive">最大值（不包含）</param>
        /// <returns>随机整数</returns>
        public int NextRandomInt(int maxExclusive)
        {
            return RNG.Next(maxExclusive);
        }

        /// <summary>
        /// 获取下一个随机整数
        /// </summary>
        /// <param name="minInclusive">最小值（包含）</param>
        /// <param name="maxExclusive">最大值（不包含）</param>
        /// <returns>随机整数</returns>
        public int NextRandomInt(int minInclusive, int maxExclusive)
        {
            return RNG.Next(minInclusive, maxExclusive);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            Nodes?.Clear();
            Nodes = null;
            OccupancyGrid = null;
            RNG = null;
            DungeonGenerator = null;

            IsInitialized = false;
            IsDisposed = true;
        }

        #endregion

        #region 调试

        /// <summary>
        /// 获取上下文状态信息
        /// </summary>
        /// <returns>状态字符串</returns>
        public override string ToString()
        {
            return $"WorldContext[GridSize:{GridSize}, RoomCount:{RoomCount}, " +
                   $"PlacedNodes:{Nodes?.Count ?? 0}, Seed:{Seed}]";
        }

        #endregion
    }
}
