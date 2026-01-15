using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration
{
    /// <summary>
    /// 房间节点数据结构 - 承载宏观层元数据
    /// 每个RoomNode代表4×4网格中的一个单元格
    /// </summary>
    [System.Serializable]
    public class RoomNode
    {
        // ========== 位置标识 ==========
        
        /// <summary>
        /// 宏观网格坐标 (0-3, 0-3)
        /// </summary>
        public Vector2Int GridCoordinates;
        
        // ========== 类型与状态 ==========
        
        /// <summary>
        /// 房间类型
        /// </summary>
        public RoomType Type = RoomType.None;
        
        /// <summary>
        /// 是否在关键路径上
        /// </summary>
        public bool IsCriticalPath;
        
        /// <summary>
        /// 房间是否已生成内部结构
        /// </summary>
        public bool IsGenerated;
        
        // ========== 连通性 ==========
        
        /// <summary>
        /// 4-bit连通性掩码
        /// N=1, E=2, S=4, W=8
        /// 示例: 0b0101 = 5 表示北南连通
        /// </summary>
        public int ConnectionMask;
        
        // ========== 软边界核心 ==========
        
        /// <summary>
        /// 活跃区域 - 实现"软边界"的关键
        /// 在GridCell内部随机偏移的实际房间区域
        /// </summary>
        public RectInt ActiveZone;
        
        // ========== 游戏性数据 ==========
        
        /// <summary>
        /// 难度系数 0.0-1.0
        /// </summary>
        public float DifficultyRating;
        
        /// <summary>
        /// 房间内敌人数量
        /// </summary>
        public int EnemyCount;
        
        /// <summary>
        /// 敌人生成点列表 (相对于房间原点的坐标)
        /// </summary>
        public List<Vector2Int> EnemySpawnPoints = new List<Vector2Int>();
        
        // ========== 生成数据 (运行时) ==========
        
        /// <summary>
        /// 房间内部瓦片数据 (运行时生成)
        /// </summary>
        [System.NonSerialized]
        public MicroTileState[,] TileData;
        
        /// <summary>
        /// 物理验证用平台列表 (运行时生成)
        /// </summary>
        [System.NonSerialized]
        public List<PlatformSurface> Platforms;
        
        // ========== 构造函数 ==========
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public RoomNode() { }
        
        /// <summary>
        /// 带坐标的构造函数
        /// </summary>
        /// <param name="x">网格X坐标</param>
        /// <param name="y">网格Y坐标</param>
        public RoomNode(int x, int y)
        {
            GridCoordinates = new Vector2Int(x, y);
        }
        
        // ========== 连通性方法 ==========
        
        /// <summary>
        /// 检查是否与指定方向连通
        /// </summary>
        /// <param name="direction">检查的方向</param>
        /// <returns>是否连通</returns>
        public bool HasConnection(Direction direction)
        {
            int mask = direction.ToMask();
            return (ConnectionMask & mask) != 0;
        }
        
        /// <summary>
        /// 添加指定方向的连通性
        /// </summary>
        /// <param name="direction">要添加的方向</param>
        public void AddConnection(Direction direction)
        {
            ConnectionMask |= direction.ToMask();
        }
        
        /// <summary>
        /// 移除指定方向的连通性
        /// </summary>
        /// <param name="direction">要移除的方向</param>
        public void RemoveConnection(Direction direction)
        {
            ConnectionMask &= ~direction.ToMask();
        }
        
        /// <summary>
        /// 获取所有连通方向
        /// </summary>
        /// <returns>连通方向列表</returns>
        public List<Direction> GetConnections()
        {
            var connections = new List<Direction>();
            
            if (HasConnection(Direction.North)) connections.Add(Direction.North);
            if (HasConnection(Direction.East)) connections.Add(Direction.East);
            if (HasConnection(Direction.South)) connections.Add(Direction.South);
            if (HasConnection(Direction.West)) connections.Add(Direction.West);
            
            return connections;
        }
        
        /// <summary>
        /// 获取连通方向数量
        /// </summary>
        /// <returns>连通方向数量</returns>
        public int GetConnectionCount()
        {
            int count = 0;
            if (HasConnection(Direction.North)) count++;
            if (HasConnection(Direction.East)) count++;
            if (HasConnection(Direction.South)) count++;
            if (HasConnection(Direction.West)) count++;
            return count;
        }
        
        // ========== 入口/出口点计算 ==========
        
        /// <summary>
        /// 获取指定方向的入口点 (相对于ActiveZone)
        /// </summary>
        /// <param name="direction">入口方向</param>
        /// <returns>入口点坐标</returns>
        public Vector2Int GetEntryPoint(Direction direction)
        {
            int centerX = ActiveZone.x + ActiveZone.width / 2;
            int centerY = ActiveZone.y + ActiveZone.height / 2;
            
            return direction switch
            {
                Direction.North => new Vector2Int(centerX, ActiveZone.yMax - 1),
                Direction.South => new Vector2Int(centerX, ActiveZone.y),
                Direction.East => new Vector2Int(ActiveZone.xMax - 1, centerY),
                Direction.West => new Vector2Int(ActiveZone.x, centerY),
                _ => new Vector2Int(centerX, centerY)
            };
        }
        
        /// <summary>
        /// 获取指定方向的出口点 (相对于ActiveZone)
        /// </summary>
        /// <param name="direction">出口方向</param>
        /// <returns>出口点坐标</returns>
        public Vector2Int GetExitPoint(Direction direction)
        {
            return GetEntryPoint(direction);
        }
        
        // ========== 辅助方法 ==========
        
        /// <summary>
        /// 检查房间是否有效 (非None类型)
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return Type != RoomType.None;
        }
        
        /// <summary>
        /// 重置房间数据
        /// </summary>
        public void Reset()
        {
            Type = RoomType.None;
            IsCriticalPath = false;
            IsGenerated = false;
            ConnectionMask = 0;
            ActiveZone = default;
            DifficultyRating = 0f;
            EnemyCount = 0;
            EnemySpawnPoints.Clear();
            TileData = null;
            Platforms = null;
        }
        
        public override string ToString()
        {
            return $"RoomNode[{GridCoordinates.x},{GridCoordinates.y}] Type={Type} Mask={ConnectionMask:X}";
        }
    }
    
    /// <summary>
    /// 微观瓦片状态枚举 - WFC使用
    /// </summary>
    public enum MicroTileState
    {
        /// <summary>
        /// 空气 - 可通行
        /// </summary>
        Air = 0,
        
        /// <summary>
        /// 地面 - 实心墙壁/地板
        /// </summary>
        Ground = 1,
        
        /// <summary>
        /// 平台 - 可站立可穿越
        /// </summary>
        Platform = 2,
        
        /// <summary>
        /// 尖刺 - 伤害陷阱
        /// </summary>
        Spike = 3,
        
        /// <summary>
        /// 水 - 特殊区域
        /// </summary>
        Water = 4
    }
    
    /// <summary>
    /// 平台表面数据 - 物理验证用
    /// </summary>
    [System.Serializable]
    public class PlatformSurface
    {
        /// <summary>
        /// 平台左端点位置
        /// </summary>
        public Vector2 Position;
        
        /// <summary>
        /// 平台宽度
        /// </summary>
        public float Width;
        
        /// <summary>
        /// 平台高度 (Y坐标)
        /// </summary>
        public float Height => Position.y;
        
        /// <summary>
        /// 平台中心点
        /// </summary>
        public Vector2 Center => new Vector2(Position.x + Width / 2f, Position.y);
        
        /// <summary>
        /// 平台左边缘
        /// </summary>
        public float LeftEdge => Position.x;
        
        /// <summary>
        /// 平台右边缘
        /// </summary>
        public float RightEdge => Position.x + Width;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="position">左端点位置</param>
        /// <param name="width">宽度</param>
        public PlatformSurface(Vector2 position, float width)
        {
            Position = position;
            Width = width;
        }
    }
}
