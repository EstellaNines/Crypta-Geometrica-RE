using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 地牢生成上下文（黑板模式）
    /// 所有规则通过此对象共享数据
    /// </summary>
    public class DungeonContext : IDisposable
    {
        #region 配置数据

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

        /// <summary>
        /// 网格尺寸（列数）
        /// </summary>
        public int GridColumns { get; set; }

        /// <summary>
        /// 网格尺寸（行数）
        /// </summary>
        public int GridRows { get; set; }

        /// <summary>
        /// 单个房间像素尺寸
        /// </summary>
        public Vector2Int RoomSize { get; set; }

        /// <summary>
        /// 世界坐标偏移（用于多房间渲染时的位置偏移）
        /// 渲染规则会将此偏移添加到所有瓦片坐标上
        /// </summary>
        public Vector2Int WorldOffset { get; set; }

        #endregion

        #region 宏观层数据

        /// <summary>
        /// 房间节点列表
        /// </summary>
        public List<RoomNode> RoomNodes { get; set; }

        /// <summary>
        /// 邻接矩阵 [fromIndex, toIndex] = 1表示连通
        /// </summary>
        public int[,] AdjacencyMatrix { get; set; }

        /// <summary>
        /// 起始房间网格坐标
        /// </summary>
        public Vector2Int StartRoom { get; set; }

        /// <summary>
        /// 终点房间网格坐标
        /// </summary>
        public Vector2Int EndRoom { get; set; }

        /// <summary>
        /// 关键路径房间集合
        /// </summary>
        public HashSet<Vector2Int> CriticalPath { get; set; }

        #endregion

        #region 微观层数据

        /// <summary>
        /// 背景层地形数据（一维扁平化存储）
        /// 索引公式: index = y * MapWidth + x
        /// </summary>
        public int[] BackgroundTileData { get; set; }

        /// <summary>
        /// 地面层地形数据（一维扁平化存储）
        /// 索引公式: index = y * MapWidth + x
        /// </summary>
        public int[] GroundTileData { get; set; }

        /// <summary>
        /// 平台层地形数据（一维扁平化存储）
        /// 索引公式: index = y * MapWidth + x
        /// </summary>
        public int[] PlatformTileData { get; set; }

        /// <summary>
        /// 地图总宽度（像素）
        /// </summary>
        public int MapWidth { get; set; }

        /// <summary>
        /// 地图总高度（像素）
        /// </summary>
        public int MapHeight { get; set; }

        /// <summary>
        /// 获取指定层级和坐标的瓦片值
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>瓦片值，边界外返回-1</returns>
        public int GetTile(TilemapLayer layer, int x, int y)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                return -1;
            
            int index = y * MapWidth + x;
            return layer switch
            {
                TilemapLayer.Background => BackgroundTileData[index],
                TilemapLayer.Ground => GroundTileData[index],
                TilemapLayer.Platform => PlatformTileData[index],
                _ => -1
            };
        }

        /// <summary>
        /// 设置指定层级和坐标的瓦片值
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="value">瓦片值</param>
        public void SetTile(TilemapLayer layer, int x, int y, int value)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                return;
            
            int index = y * MapWidth + x;
            switch (layer)
            {
                case TilemapLayer.Background:
                    BackgroundTileData[index] = value;
                    break;
                case TilemapLayer.Ground:
                    GroundTileData[index] = value;
                    break;
                case TilemapLayer.Platform:
                    PlatformTileData[index] = value;
                    break;
            }
        }

        /// <summary>
        /// 将网格坐标转换为世界坐标边界
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <returns>世界坐标边界</returns>
        public BoundsInt GridToWorldBounds(Vector2Int gridPos)
        {
            return new BoundsInt(
                gridPos.x * RoomSize.x,
                gridPos.y * RoomSize.y,
                0,
                RoomSize.x,
                RoomSize.y,
                1
            );
        }

        #endregion

        #region 内容层数据

        /// <summary>
        /// 待生成的实体指令列表
        /// </summary>
        public List<SpawnCommand> PendingSpawns { get; set; }

        #endregion

        #region 渲染层数据

        /// <summary>
        /// 当前主题（随机生成）
        /// </summary>
        public TileTheme Theme { get; set; }

        /// <summary>
        /// 需要重新渲染的区块集合
        /// </summary>
        public HashSet<BoundsInt> DirtyChunks { get; set; }

        #endregion

        #region 生命周期

        /// <summary>
        /// 可用主题列表（用于随机选择）
        /// </summary>
        private static readonly TileTheme[] AvailableThemes = 
        {
            TileTheme.Blue,
            TileTheme.Red,
            TileTheme.Yellow
        };

        /// <summary>
        /// 创建地牢上下文
        /// </summary>
        /// <param name="seed">随机种子</param>
        public DungeonContext(int seed)
        {
            Seed = seed;
            RNG = new System.Random(seed);
            RoomNodes = new List<RoomNode>();
            CriticalPath = new HashSet<Vector2Int>();
            PendingSpawns = new List<SpawnCommand>();
            DirtyChunks = new HashSet<BoundsInt>();
            
            // 随机选择主题
            Theme = AvailableThemes[RNG.Next(AvailableThemes.Length)];
        }

        /// <summary>
        /// 重置上下文（保留配置，清空生成数据）
        /// </summary>
        public void Reset()
        {
            RNG = new System.Random(Seed);
            RoomNodes.Clear();
            CriticalPath.Clear();
            PendingSpawns.Clear();
            DirtyChunks.Clear();
            AdjacencyMatrix = null;
            BackgroundTileData = null;
            GroundTileData = null;
            PlatformTileData = null;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            RoomNodes?.Clear();
            CriticalPath?.Clear();
            PendingSpawns?.Clear();
            DirtyChunks?.Clear();
            RoomNodes = null;
            CriticalPath = null;
            PendingSpawns = null;
            DirtyChunks = null;
            AdjacencyMatrix = null;
            BackgroundTileData = null;
            GroundTileData = null;
            PlatformTileData = null;
        }

        #endregion
    }
}
