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
        /// 使用 Bottom-Up 模拟攀爬算法 + 包围盒预检查
        /// </summary>
        private int ProcessRoom(DungeonContext context, RoomNode room, int safeHeight)
        {
            BoundsInt bounds = room.WorldBounds;
            int startX = bounds.xMin;
            int startY = bounds.yMin;
            int width = bounds.size.x;
            int height = bounds.size.y;

            // 【关键改动】使用 List<BoundsInt> 记录平台的实际占地面积
            List<BoundsInt> placedPlatformBounds = new List<BoundsInt>();
            int platformCount = 0;

            // 平台间隔：安全高度 - 安全余量
            int platformInterval = Mathf.Max(5, safeHeight - _safetyMargin);

            // 垂直扫描每一列（Bottom-Up：从下往上）
            for (int x = startX + 2; x < startX + width - 2; x++)
            {
                int continuousAirCount = 0;

                // 从底部向上扫描
                for (int y = startY; y < startY + height; y++)
                {
                    int tile = context.GetTile(TilemapLayer.Ground, x, y);
                    bool isSolid = (tile == 1);

                    if (!isSolid)
                    {
                        continuousAirCount++;

                        // 核心触发逻辑：当距离上一个地面/平台达到安全高度时生成平台
                        if (continuousAirCount >= platformInterval)
                        {
                            // 【关键改动】先计算平台包围盒，再检查是否碰撞
                            BoundsInt? predictedBounds = CalculatePlatformBounds(context, x, y, bounds);
                            
                            if (predictedBounds.HasValue)
                            {
                                // 检查与已放置平台的包围盒是否碰撞
                                if (!CheckBoundsCollision(predictedBounds.Value, placedPlatformBounds, _minHorizontalSpacing, platformInterval / 2))
                                {
                                    // 实际放置平台
                                    PlacePlatformFromBounds(context, predictedBounds.Value);
                                    placedPlatformBounds.Add(predictedBounds.Value);
                                    platformCount++;

                                    // 【关键】重置计数器，将当前平台视为新的"地面"
                                    continuousAirCount = 0;

                                    if (_debugLog)
                                        LogInfo($"Bottom-Up平台: x={predictedBounds.Value.xMin}, y={y}, 宽度={predictedBounds.Value.size.x}");
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

            // 填充大空洞（按层扫描）
            platformCount += FillBigGaps(context, bounds, safeHeight, placedPlatformBounds);

            return platformCount;
        }

        /// <summary>
        /// 填充大空洞（按层扫描）+ 包围盒预检查
        /// 解决房间中间大面积空白区域的问题
        /// </summary>
        private int FillBigGaps(DungeonContext context, BoundsInt bounds, int safeHeight, List<BoundsInt> placedPlatformBounds)
        {
            int startX = bounds.xMin;
            int startY = bounds.yMin;
            int width = bounds.size.x;
            int height = bounds.size.y;
            int platformCount = 0;

            // 平台间隔
            int platformInterval = Mathf.Max(5, safeHeight - _safetyMargin);

            // 按层扫描（从下往上，每隔 platformInterval 一层）
            for (int layerY = startY + platformInterval; layerY < startY + height - 3; layerY += platformInterval)
            {
                // 扫描这一层的水平连续空气区域
                int airRunStart = -1;
                int airRunLength = 0;

                for (int x = startX + 2; x < startX + width - 2; x++)
                {
                    int tile = context.GetTile(TilemapLayer.Ground, x, layerY);
                    
                    // 检查该位置是否在已有平台包围盒内
                    bool hasExistingPlatform = IsPointInAnyBounds(x, layerY, placedPlatformBounds, _minHorizontalSpacing, platformInterval / 2);

                    bool isAir = (tile == 0) && !hasExistingPlatform;

                    if (isAir)
                    {
                        if (airRunStart < 0)
                            airRunStart = x;
                        airRunLength++;
                    }
                    else
                    {
                        // 空气区域结束，检查是否需要填充平台
                        if (airRunLength >= _minHorizontalSpacing * 2)
                        {
                            int platformX = airRunStart + airRunLength / 2;
                            BoundsInt? predictedBounds = CalculatePlatformBounds(context, platformX, layerY, bounds);
                            
                            if (predictedBounds.HasValue && 
                                !CheckBoundsCollision(predictedBounds.Value, placedPlatformBounds, _minHorizontalSpacing, platformInterval / 2))
                            {
                                PlacePlatformFromBounds(context, predictedBounds.Value);
                                placedPlatformBounds.Add(predictedBounds.Value);
                                platformCount++;

                                if (_debugLog)
                                    LogInfo($"大空洞填充平台: x={predictedBounds.Value.xMin}, y={layerY}, 宽度={predictedBounds.Value.size.x}");
                            }
                        }

                        airRunStart = -1;
                        airRunLength = 0;
                    }
                }

                // 处理行末尾的空气区域
                if (airRunLength >= _minHorizontalSpacing * 2)
                {
                    int platformX = airRunStart + airRunLength / 2;
                    BoundsInt? predictedBounds = CalculatePlatformBounds(context, platformX, layerY, bounds);
                    
                    if (predictedBounds.HasValue && 
                        !CheckBoundsCollision(predictedBounds.Value, placedPlatformBounds, _minHorizontalSpacing, platformInterval / 2))
                    {
                        PlacePlatformFromBounds(context, predictedBounds.Value);
                        placedPlatformBounds.Add(predictedBounds.Value);
                        platformCount++;
                    }
                }
            }

            return platformCount;
        }

        /// <summary>
        /// 计算平台的预期包围盒（不实际放置）
        /// </summary>
        private BoundsInt? CalculatePlatformBounds(DungeonContext context, int x, int y, BoundsInt roomBounds)
        {
            // 边界检查
            if (y < roomBounds.yMin + 2 || y > roomBounds.yMax - 3)
                return null;

            // 计算左右可用空间
            int leftSpace = 0;
            int rightSpace = 0;

            // 向左探测
            for (int dx = 0; dx < _maxPlatformWidth; dx++)
            {
                int checkX = x - dx;
                int leftEdgeX = checkX - 1;
                
                if (checkX < roomBounds.xMin + 2) break;
                
                bool currentClear = context.GetTile(TilemapLayer.Ground, checkX, y) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 1) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 2) == 0;
                
                bool leftEdgeIsWall = context.GetTile(TilemapLayer.Ground, leftEdgeX, y) == 1;
                
                if (!currentClear) break;
                if (leftEdgeIsWall && dx > 0) break;
                    
                leftSpace++;
            }

            // 向右探测
            for (int dx = 1; dx < _maxPlatformWidth; dx++)
            {
                int checkX = x + dx;
                int rightEdgeX = checkX + 1;
                
                if (checkX > roomBounds.xMax - 3) break;
                
                bool currentClear = context.GetTile(TilemapLayer.Ground, checkX, y) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 1) == 0 &&
                                    context.GetTile(TilemapLayer.Ground, checkX, y + 2) == 0;
                
                bool rightEdgeIsWall = context.GetTile(TilemapLayer.Ground, rightEdgeX, y) == 1;
                
                if (!currentClear) break;
                if (rightEdgeIsWall) break;
                    
                rightSpace++;
            }

            int totalSpace = leftSpace + rightSpace;
            if (totalSpace < _minPlatformWidth)
                return null;

            // 计算实际平台宽度和起始位置
            int platformWidth = Mathf.Min(totalSpace, _maxPlatformWidth);
            int platformStartX = x - Mathf.Min(leftSpace - 1, platformWidth / 2);
            
            // 最终验证：确保平台两端与墙壁有1格间距
            int platformLeftEdge = platformStartX - 1;
            int platformRightEdge = platformStartX + platformWidth;
            
            if (context.GetTile(TilemapLayer.Ground, platformLeftEdge, y) == 1 ||
                context.GetTile(TilemapLayer.Ground, platformRightEdge, y) == 1)
            {
                return null;
            }

            // 返回包围盒（包含安全边距）
            return new BoundsInt(platformStartX, y - _platformThickness + 1, 0, platformWidth, _platformThickness, 1);
        }

        /// <summary>
        /// 检查新平台包围盒是否与已有平台碰撞
        /// </summary>
        private bool CheckBoundsCollision(BoundsInt newBounds, List<BoundsInt> existingBounds, int horizontalMargin, int verticalMargin)
        {
            // 扩展新包围盒以包含安全边距
            BoundsInt expandedBounds = new BoundsInt(
                newBounds.xMin - horizontalMargin,
                newBounds.yMin - verticalMargin,
                0,
                newBounds.size.x + horizontalMargin * 2,
                newBounds.size.y + verticalMargin * 2,
                1
            );

            foreach (var existing in existingBounds)
            {
                // 矩形碰撞检测 (AABB)
                if (expandedBounds.xMin < existing.xMax &&
                    expandedBounds.xMax > existing.xMin &&
                    expandedBounds.yMin < existing.yMax &&
                    expandedBounds.yMax > existing.yMin)
                {
                    return true; // 碰撞
                }
            }

            return false; // 无碰撞
        }

        /// <summary>
        /// 检查点是否在任何平台包围盒附近
        /// </summary>
        private bool IsPointInAnyBounds(int x, int y, List<BoundsInt> boundsList, int horizontalMargin, int verticalMargin)
        {
            foreach (var b in boundsList)
            {
                if (x >= b.xMin - horizontalMargin && x < b.xMax + horizontalMargin &&
                    y >= b.yMin - verticalMargin && y < b.yMax + verticalMargin)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 根据包围盒实际放置平台
        /// </summary>
        private void PlacePlatformFromBounds(DungeonContext context, BoundsInt platformBounds)
        {
            for (int dx = 0; dx < platformBounds.size.x; dx++)
            {
                for (int dy = 0; dy < platformBounds.size.y; dy++)
                {
                    context.SetTile(TilemapLayer.Platform, platformBounds.xMin + dx, platformBounds.yMin + dy, 1);
                }
            }
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
