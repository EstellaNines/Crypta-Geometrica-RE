using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 智能平台生成规则
    /// 根据玩家跳跃能力在必要位置生成平台
    /// </summary>
    [Serializable]
    public class PlatformRule : GeneratorRuleBase
    {
        #region 跳跃参数

        [TitleGroup("跳跃参数")]
        [LabelText("单跳高度")]
        [Tooltip("玩家单次跳跃可达到的高度（格）")]
        [Range(4, 16)]
        [SerializeField]
        private int _jumpHeight = 8;

        [TitleGroup("跳跃参数")]
        [LabelText("支持二段跳")]
        [SerializeField]
        private bool _doubleJump = true;

        [TitleGroup("跳跃参数")]
        [LabelText("安全余量")]
        [Tooltip("跳跃高度的安全余量（格）")]
        [Range(1, 4)]
        [SerializeField]
        private int _safetyMargin = 2;

        #endregion

        #region 平台参数

        [TitleGroup("平台参数")]
        [LabelText("最小宽度")]
        [Range(2, 4)]
        [SerializeField]
        private int _minPlatformWidth = 2;

        [TitleGroup("平台参数")]
        [LabelText("最大宽度")]
        [Range(3, 8)]
        [SerializeField]
        private int _maxPlatformWidth = 5;

        [TitleGroup("平台参数")]
        [LabelText("平台厚度")]
        [Range(1, 2)]
        [SerializeField]
        private int _platformThickness = 1;

        [TitleGroup("平台参数")]
        [LabelText("最小水平间距")]
        [Tooltip("平台之间的最小水平距离（格）")]
        [Range(3, 8)]
        [SerializeField]
        private int _minHorizontalSpacing = 4;

        [TitleGroup("调试")]
        [LabelText("显示调试日志")]
        [SerializeField]
        private bool _debugLog = false;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public PlatformRule()
        {
            _ruleName = "PlatformRule";
            _executionOrder = 40; // 在PathValidationRule之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始智能平台生成...");

            // 计算有效跳跃高度
            int effectiveJumpHeight = _doubleJump ? _jumpHeight * 2 : _jumpHeight;
            int safeHeight = effectiveJumpHeight - _safetyMargin;

            LogInfo($"有效跳跃高度: {effectiveJumpHeight}, 安全高度阈值: {safeHeight}");

            int platformCount = 0;

            // 遍历每个房间
            foreach (var room in context.RoomNodes)
            {
                int count = ProcessRoom(context, room, safeHeight);
                platformCount += count;

                // 每处理几个房间让出一帧
                await UniTask.Yield(token);
            }

            LogInfo($"平台生成完成，共生成 {platformCount} 个平台");
            return true;
        }

        /// <summary>
        /// 处理单个房间的平台生成
        /// 使用空气柱步进采样算法，在连续空旷区域生成平台
        /// </summary>
        private int ProcessRoom(DungeonContext context, RoomNode room, int safeHeight)
        {
            BoundsInt bounds = room.WorldBounds;
            int startX = bounds.xMin;
            int startY = bounds.yMin;
            int width = bounds.size.x;
            int height = bounds.size.y;

            // 记录已放置平台的位置（避免重叠）
            HashSet<Vector2Int> placedPlatforms = new HashSet<Vector2Int>();
            int platformCount = 0;

            // 平台间隔：安全高度 - 容错
            int platformInterval = Mathf.Max(6, safeHeight - 4);

            // 垂直扫描每一列（从上往下）
            for (int x = startX + 2; x < startX + width - 2; x++)
            {
                int continuousAirCount = 0;

                // 从顶部向下扫描
                for (int y = startY + height - 1; y >= startY; y--)
                {
                    int tile = context.GetTile(TilemapLayer.Ground, x, y);
                    bool isSolid = (tile == 1);

                    if (!isSolid)
                    {
                        continuousAirCount++;

                        // 核心触发逻辑：当空位高度达到阈值，且满足间隔条件
                        if (continuousAirCount >= safeHeight && 
                            (continuousAirCount % platformInterval == 0))
                        {
                            // 检查是否与已放置平台太近
                            bool tooClose = false;
                            foreach (var placed in placedPlatforms)
                            {
                                if (Mathf.Abs(placed.x - x) < _minHorizontalSpacing &&
                                    Mathf.Abs(placed.y - y) < platformInterval / 2)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }

                            if (!tooClose)
                            {
                                // 尝试放置自适应平台
                                if (TryPlaceAdaptivePlatform(context, x, y, bounds))
                                {
                                    placedPlatforms.Add(new Vector2Int(x, y));
                                    platformCount++;

                                    if (_debugLog)
                                        LogInfo($"空气柱平台: x={x}, y={y}, 连续空气={continuousAirCount}");
                                }
                            }
                        }
                    }
                    else
                    {
                        // 遇到实心格，重置计数器
                        continuousAirCount = 0;
                    }
                }
            }

            return platformCount;
        }

        /// <summary>
        /// 尝试放置自适应宽度的平台
        /// 平台与墙壁必须左右间隔1个瓦片
        /// </summary>
        private bool TryPlaceAdaptivePlatform(DungeonContext context, int x, int y, BoundsInt bounds)
        {
            // 边界检查
            if (y < bounds.yMin + 2 || y > bounds.yMax - 3)
                return false;

            // 计算左右可用空间（保持与墙壁1格间距）
            int leftSpace = 0;
            int rightSpace = 0;

            // 向左探测（需要检测更左边1格是否为空，确保间距）
            for (int dx = 0; dx < _maxPlatformWidth; dx++)
            {
                int checkX = x - dx;
                int leftEdgeX = checkX - 1; // 左边缘的左边1格
                
                if (checkX < bounds.xMin + 2) break; // 边界保护+1
                
                // 检查当前位置和上方2格是否为空
                bool currentClear = context.GetTile(TilemapLayer.Ground, checkX, y) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 1) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 2) == 0;
                
                // 检查左边缘的左边1格是否为墙（如果是墙则停止，保持间距）
                bool leftEdgeIsWall = context.GetTile(TilemapLayer.Ground, leftEdgeX, y) == 1;
                
                if (!currentClear)
                    break;
                    
                if (leftEdgeIsWall && dx > 0)
                    break; // 遇到墙壁，停止扩展但保留当前格
                    
                leftSpace++;
            }

            // 向右探测（需要检测更右边1格是否为空，确保间距）
            for (int dx = 1; dx < _maxPlatformWidth; dx++)
            {
                int checkX = x + dx;
                int rightEdgeX = checkX + 1; // 右边缘的右边1格
                
                if (checkX > bounds.xMax - 3) break; // 边界保护+1
                
                // 检查当前位置和上方2格是否为空
                bool currentClear = context.GetTile(TilemapLayer.Ground, checkX, y) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 1) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 2) == 0;
                
                // 检查右边缘的右边1格是否为墙（如果是墙则停止，保持间距）
                bool rightEdgeIsWall = context.GetTile(TilemapLayer.Ground, rightEdgeX, y) == 1;
                
                if (!currentClear)
                    break;
                    
                if (rightEdgeIsWall)
                    break; // 遇到墙壁，停止扩展
                    
                rightSpace++;
            }

            int totalSpace = leftSpace + rightSpace;
            if (totalSpace < _minPlatformWidth)
                return false;

            // 计算实际平台宽度和起始位置
            int platformWidth = Mathf.Min(totalSpace, _maxPlatformWidth);
            int platformStartX = x - Mathf.Min(leftSpace - 1, platformWidth / 2);
            
            // 最终验证：确保平台两端与墙壁有1格间距
            int platformLeftEdge = platformStartX - 1;
            int platformRightEdge = platformStartX + platformWidth;
            
            if (context.GetTile(TilemapLayer.Ground, platformLeftEdge, y) == 1 ||
                context.GetTile(TilemapLayer.Ground, platformRightEdge, y) == 1)
            {
                return false; // 平台边缘与墙壁相邻，不放置
            }

            // 放置平台
            for (int dx = 0; dx < platformWidth; dx++)
            {
                for (int dy = 0; dy < _platformThickness; dy++)
                {
                    context.SetTile(TilemapLayer.Platform, platformStartX + dx, y - dy, 1);
                }
            }

            return true;
        }

        /// <summary>
        /// 找到指定列的最高地面高度（从上往下扫描）
        /// </summary>
        private int FindHighestGround(DungeonContext context, int x, int startY, int height)
        {
            for (int y = startY + height - 2; y >= startY; y--)
            {
                int current = context.GetTile(TilemapLayer.Ground, x, y);
                int above = context.GetTile(TilemapLayer.Ground, x, y + 1);

                // 地面点：当前是实心，上方是空
                if (current == 1 && above == 0)
                {
                    return y + 1; // 返回可站立的Y位置
                }
            }
            return -1; // 未找到地面
        }

        /// <summary>
        /// 找到指定列的最低地面高度（从下往上扫描）
        /// </summary>
        private int FindLowestGround(DungeonContext context, int x, int startY, int height)
        {
            for (int y = startY; y < startY + height - 1; y++)
            {
                int current = context.GetTile(TilemapLayer.Ground, x, y);
                int above = context.GetTile(TilemapLayer.Ground, x, y + 1);

                // 地面点：当前是实心，上方是空
                if (current == 1 && above == 0)
                {
                    return y + 1; // 返回可站立的Y位置
                }
            }
            return -1; // 未找到地面
        }

        /// <summary>
        /// 在悬崖壁上生成阶梯式平台
        /// </summary>
        private int GenerateCliffPlatforms(DungeonContext context, int cliffX, int lowerY, int upperY,
            int safeHeight, BoundsInt bounds, HashSet<Vector2Int> placedPlatforms, bool isLeftCliff)
        {
            int heightDiff = upperY - lowerY;
            int numPlatforms = Mathf.Max(1, (heightDiff - 1) / safeHeight);
            int spacing = heightDiff / (numPlatforms + 1);
            int count = 0;

            for (int p = 1; p <= numPlatforms; p++)
            {
                int platformY = lowerY + spacing * p;
                
                // 平台X位置：贴近悬崖壁
                int platformX = isLeftCliff ? cliffX - _maxPlatformWidth + 1 : cliffX;

                // 检查是否与已放置平台太近
                bool tooClose = false;
                foreach (var placed in placedPlatforms)
                {
                    if (Mathf.Abs(placed.x - platformX) < _minHorizontalSpacing &&
                        Mathf.Abs(placed.y - platformY) < safeHeight / 2)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // 验证平台位置是否合适
                if (CanPlacePlatform(context, platformX, platformY, bounds))
                {
                    PlacePlatform(context, platformX, platformY);
                    placedPlatforms.Add(new Vector2Int(platformX, platformY));
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 处理同一列内的垂直高度差（原有逻辑）
        /// </summary>
        private int ProcessVerticalGaps(DungeonContext context, BoundsInt bounds, int safeHeight, 
            HashSet<Vector2Int> placedPlatforms)
        {
            int startX = bounds.xMin;
            int startY = bounds.yMin;
            int width = bounds.size.x;
            int height = bounds.size.y;
            int platformCount = 0;

            for (int x = startX + 2; x < startX + width - 2; x++)
            {
                List<int> groundLevels = FindGroundLevels(context, x, startY, height);
                
                if (groundLevels.Count < 2)
                    continue;

                for (int i = 0; i < groundLevels.Count - 1; i++)
                {
                    int lowerGround = groundLevels[i];
                    int upperGround = groundLevels[i + 1];
                    int heightDiff = upperGround - lowerGround;

                    if (heightDiff > safeHeight)
                    {
                        int numPlatforms = (heightDiff / safeHeight);
                        int spacing = heightDiff / (numPlatforms + 1);

                        for (int p = 1; p <= numPlatforms; p++)
                        {
                            int platformY = lowerGround + spacing * p;
                            
                            // 检查是否已有平台
                            bool exists = false;
                            foreach (var placed in placedPlatforms)
                            {
                                if (Mathf.Abs(placed.x - x) < _minHorizontalSpacing &&
                                    Mathf.Abs(placed.y - platformY) < safeHeight / 2)
                                {
                                    exists = true;
                                    break;
                                }
                            }

                            if (exists) continue;

                            if (CanPlacePlatform(context, x, platformY, bounds))
                            {
                                PlacePlatform(context, x, platformY);
                                placedPlatforms.Add(new Vector2Int(x, platformY));
                                platformCount++;
                            }
                        }
                    }
                }
            }

            return platformCount;
        }

        /// <summary>
        /// 找到指定列的所有地面高度
        /// </summary>
        private List<int> FindGroundLevels(DungeonContext context, int x, int startY, int height)
        {
            List<int> levels = new List<int>();

            for (int y = startY; y < startY + height - 1; y++)
            {
                int current = context.GetTile(TilemapLayer.Ground, x, y);
                int above = context.GetTile(TilemapLayer.Ground, x, y + 1);

                // 地面点：当前是实心，上方是空
                if (current == 1 && above == 0)
                {
                    levels.Add(y + 1); // 返回可站立的Y位置
                }
            }

            return levels;
        }

        /// <summary>
        /// 检查是否可以在指定位置放置平台
        /// </summary>
        private bool CanPlacePlatform(DungeonContext context, int x, int y, BoundsInt roomBounds)
        {
            // 边界检查
            if (x < roomBounds.xMin + 2 || x > roomBounds.xMax - _maxPlatformWidth - 2)
                return false;

            if (y < roomBounds.yMin + 2 || y > roomBounds.yMax - 3)
                return false;

            // 检查平台位置是否是空的（不阻挡现有地形）
            for (int dx = 0; dx < _minPlatformWidth; dx++)
            {
                int tile = context.GetTile(TilemapLayer.Ground, x + dx, y);
                if (tile == 1) // 已有实心
                    return false;
            }

            // 检查平台上方是否有足够空间
            for (int dx = 0; dx < _minPlatformWidth; dx++)
            {
                for (int dy = 1; dy <= 3; dy++)
                {
                    int tile = context.GetTile(TilemapLayer.Ground, x + dx, y + dy);
                    if (tile == 1)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 在指定位置放置平台
        /// </summary>
        private void PlacePlatform(DungeonContext context, int x, int y)
        {
            // 随机平台宽度
            int width = UnityEngine.Random.Range(_minPlatformWidth, _maxPlatformWidth + 1);

            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < _platformThickness; dy++)
                {
                    context.SetTile(TilemapLayer.Platform, x + dx, y - dy, 1);
                }
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_minPlatformWidth > _maxPlatformWidth)
            {
                errorMessage = "最小平台宽度不能大于最大宽度";
                return false;
            }

            if (_jumpHeight < 4)
            {
                errorMessage = "跳跃高度太小";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
