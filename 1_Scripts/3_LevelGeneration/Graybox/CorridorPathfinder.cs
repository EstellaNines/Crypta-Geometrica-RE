using UnityEngine;
using System.Collections.Generic;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 走廊寻路器
    /// 使用A*算法生成避开障碍物的走廊路径
    /// </summary>
    public class CorridorPathfinder
    {
        // ==================== 寻路节点 ====================
        
        private class PathNode
        {
            public Vector2Int Position;
            public PathNode Parent;
            public float GCost; // 从起点到当前节点的实际代价
            public float HCost; // 从当前节点到终点的估计代价
            public float FCost => GCost + HCost;
            
            public PathNode(Vector2Int pos)
            {
                Position = pos;
                Parent = null;
                GCost = float.MaxValue;
                HCost = 0;
            }
        }
        
        // ==================== 配置参数 ====================
        
        private List<Rect> _obstacles;
        private Rect _layoutBounds;
        private int _gridResolution;
        private float _obstacleMargin;
        private System.Random _rng;
        
        // 扩展后的障碍物列表（用于寻路）
        private List<Rect> _expandedObstacles;
        
        // ==================== 预烘焙网格系统 ====================
        
        /// <summary>
        /// 预烘焙的可通行网格（true = 可通行）
        /// </summary>
        private bool[,] _walkableGrid;
        
        /// <summary>
        /// 网格宽度（格子数）
        /// </summary>
        private int _bakedGridWidth;
        
        /// <summary>
        /// 网格高度（格子数）
        /// </summary>
        private int _bakedGridHeight;
        
        /// <summary>
        /// 网格原点（世界坐标）
        /// </summary>
        private Vector2 _gridOrigin;
        
        /// <summary>
        /// 初始化寻路器
        /// </summary>
        /// <param name="obstacles">障碍物列表（房间边界）</param>
        /// <param name="layoutBounds">布局区域边界</param>
        /// <param name="gridResolution">寻路网格分辨率（瓦片单位）</param>
        /// <param name="obstacleMargin">障碍物安全边距</param>
        /// <param name="seed">随机种子</param>
        public CorridorPathfinder(List<Rect> obstacles, Rect layoutBounds, int gridResolution = 4, float obstacleMargin = 2f, int seed = 0)
        {
            _obstacles = obstacles ?? new List<Rect>();
            _layoutBounds = layoutBounds;
            _gridResolution = Mathf.Max(1, gridResolution);
            _obstacleMargin = obstacleMargin;
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            
            // 预计算扩展后的障碍物边界（走廊必须在这些边界外部）
            _expandedObstacles = new List<Rect>();
            foreach (Rect obs in _obstacles)
            {
                _expandedObstacles.Add(new Rect(
                    obs.x - _obstacleMargin,
                    obs.y - _obstacleMargin,
                    obs.width + _obstacleMargin * 2,
                    obs.height + _obstacleMargin * 2
                ));
            }
            
            // 预烘焙可通行网格
            BakeWalkableGrid();
        }
        
        /// <summary>
        /// 预烘焙可通行网格
        /// 将障碍物检测结果缓存到布尔数组中，实现 O(1) 查询
        /// </summary>
        private void BakeWalkableGrid()
        {
            // 计算网格尺寸（添加边界缓冲）
            _gridOrigin = new Vector2(_layoutBounds.xMin, _layoutBounds.yMin);
            _bakedGridWidth = Mathf.CeilToInt(_layoutBounds.width / _gridResolution) + 2;
            _bakedGridHeight = Mathf.CeilToInt(_layoutBounds.height / _gridResolution) + 2;
            
            _walkableGrid = new bool[_bakedGridWidth, _bakedGridHeight];
            
            // 预计算每个格子是否可通行
            for (int gx = 0; gx < _bakedGridWidth; gx++)
            {
                for (int gy = 0; gy < _bakedGridHeight; gy++)
                {
                    Vector2 worldPos = new Vector2(
                        _gridOrigin.x + gx * _gridResolution,
                        _gridOrigin.y + gy * _gridResolution
                    );
                    
                    // 检查是否在布局区域内
                    bool inBounds = _layoutBounds.Contains(worldPos);
                    
                    // 检查是否与任何扩展障碍物重叠
                    bool blocked = false;
                    if (inBounds)
                    {
                        foreach (Rect expanded in _expandedObstacles)
                        {
                            if (expanded.Contains(worldPos))
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }
                    
                    _walkableGrid[gx, gy] = inBounds && !blocked;
                }
            }
            
            UnityEngine.Debug.Log($"[CorridorPathfinder] 网格烘焙完成: {_bakedGridWidth}x{_bakedGridHeight} = {_bakedGridWidth * _bakedGridHeight} 格子");
        }
        
        /// <summary>
        /// 查找从起点到终点的路径
        /// </summary>
        /// <param name="start">起点（世界坐标）</param>
        /// <param name="end">终点（世界坐标）</param>
        /// <returns>路径点列表，如果找不到路径则返回空列表</returns>
        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            // 1. 获取安全点（确保在障碍物外）
            Vector2 safeStart = FindSafePointOutsideObstacles(start);
            Vector2 safeEnd = FindSafePointOutsideObstacles(end);
            
            // 2. 严格对齐到网格（使用 RoundToInt 确保对齐到最近格子）
            Vector2Int gridStart = WorldToGrid(safeStart);
            Vector2Int gridEnd = WorldToGrid(safeEnd);
            
            // 3. 执行纯粹的 4 方向 A*
            List<Vector2Int> gridPath = AStarManhattanSearch(gridStart, gridEnd);
            
            if (gridPath.Count == 0)
            {
                gridPath = FindPathWithWaypoints(gridStart, gridEnd);
            }
            
            if (gridPath.Count == 0)
            {
                UnityEngine.Debug.LogError($"[CorridorPathfinder] 无法生成路径: {gridStart} -> {gridEnd}");
                return new List<Vector2>();
            }
            
            // 4. 转换回世界坐标（纯网格路径，锯齿状）
            List<Vector2> worldPath = new List<Vector2>();
            foreach (var p in gridPath)
            {
                worldPath.Add(GridToWorld(p));
            }
            
            // 5. 【关键】仅合并共线点，绝不使用射线简化
            worldPath = MergeCollinearPointsStrict(worldPath);
            
            // 6. 处理首尾连接
            List<Vector2> finalPath = new List<Vector2>();
            float threshold = 0.05f;
            
            // 处理头部
            if (Vector2.Distance(start, worldPath[0]) > threshold)
            {
                finalPath.Add(start);
                // 如果形成斜线，不插入拐点（水平接入段已在 Manager 中处理）
            }
            else
            {
                worldPath[0] = start;
            }
            
            finalPath.AddRange(worldPath);
            
            // 处理尾部
            Vector2 lastPoint = finalPath[finalPath.Count - 1];
            if (Vector2.Distance(lastPoint, end) > threshold)
            {
                finalPath.Add(end);
            }
            else
            {
                finalPath[finalPath.Count - 1] = end;
            }
            
            // 7. 最后再做一次共线合并
            return MergeCollinearPointsStrict(finalPath);
        }
        
        /// <summary>
        /// 找到障碍物外部的安全点
        /// </summary>
        private Vector2 FindSafePointOutsideObstacles(Vector2 point)
        {
            // 检查点是否在任何障碍物内
            foreach (Rect obstacle in _obstacles)
            {
                Rect expandedObstacle = new Rect(
                    obstacle.x - _obstacleMargin,
                    obstacle.y - _obstacleMargin,
                    obstacle.width + _obstacleMargin * 2,
                    obstacle.height + _obstacleMargin * 2
                );
                
                if (expandedObstacle.Contains(point))
                {
                    // 找到最近的边缘点
                    return FindNearestEdgePoint(point, expandedObstacle);
                }
            }
            
            return point;
        }
        
        /// <summary>
        /// 找到矩形边缘上最近的点
        /// </summary>
        private Vector2 FindNearestEdgePoint(Vector2 point, Rect rect)
        {
            // 计算到四条边的距离
            float distToLeft = Mathf.Abs(point.x - rect.xMin);
            float distToRight = Mathf.Abs(point.x - rect.xMax);
            float distToBottom = Mathf.Abs(point.y - rect.yMin);
            float distToTop = Mathf.Abs(point.y - rect.yMax);
            
            float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            
            // 移动到最近的边缘外部（加一点额外距离）
            float extraMargin = _gridResolution * 2;
            
            if (minDist == distToLeft)
                return new Vector2(rect.xMin - extraMargin, point.y);
            else if (minDist == distToRight)
                return new Vector2(rect.xMax + extraMargin, point.y);
            else if (minDist == distToBottom)
                return new Vector2(point.x, rect.yMin - extraMargin);
            else
                return new Vector2(point.x, rect.yMax + extraMargin);
        }
        
        /// <summary>
        /// 生成Dead Cells风格的走廊路径
        /// 主要由水平和垂直线段组成，有清晰的拐角
        /// </summary>
        public List<Vector2> GenerateSplinePath(Vector2 start, Vector2 end, int segments = 20)
        {
            return GenerateDeadCellsStylePath(start, end);
        }
        
        /// <summary>
        /// 生成Dead Cells风格的走廊路径
        /// 起点和终点已经在房间外部（由GetPortalPosition向外延伸）
        /// </summary>
        private List<Vector2> GenerateDeadCellsStylePath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            // 保存原始起点和终点（确保最终路径以这些点开始和结束）
            Vector2 originalStart = start;
            Vector2 originalEnd = end;
            
            // 检查起点和终点是否在安全区域
            bool startSafe = IsPointInSafeZone(start);
            bool endSafe = IsPointInSafeZone(end);
            
            // 如果起点不在安全区域，延伸到安全区域
            Vector2 safeStart = startSafe ? start : ExtendPointOutsideRoom(start);
            Vector2 safeEnd = endSafe ? end : ExtendPointOutsideRoom(end);
            
            // 在房间外部空间生成阶梯状路径
            List<Vector2> middlePath = GenerateExternalStairPath(safeStart, safeEnd);
            
            // 如果阶梯路径无效，使用A*寻路
            if (middlePath.Count == 0 || !ValidatePathSegments(safeStart, middlePath, safeEnd))
            {
                middlePath = GenerateAStarExternalPath(safeStart, safeEnd);
            }
            
            // 如果A*也失败，尝试简单L形路径
            if (middlePath.Count == 0)
            {
                middlePath = GenerateSimpleLPath(safeStart, safeEnd);
            }
            
            // 构建完整路径：起点 -> 中间路径 -> 终点
            path.Add(originalStart);
            
            // 如果起点不安全且安全起点不同，添加安全起点
            if (!startSafe && Vector2.Distance(originalStart, safeStart) > 1f)
            {
                path.Add(safeStart);
            }
            
            // 添加中间路径（排除可能与起点/终点重复的点）
            foreach (Vector2 p in middlePath)
            {
                if (Vector2.Distance(p, path[path.Count - 1]) > 1f)
                {
                    path.Add(p);
                }
            }
            
            // 如果终点不安全且安全终点不同，添加安全终点
            if (!endSafe && Vector2.Distance(safeEnd, originalEnd) > 1f)
            {
                if (Vector2.Distance(safeEnd, path[path.Count - 1]) > 1f)
                {
                    path.Add(safeEnd);
                }
            }
            
            // 确保终点存在
            if (Vector2.Distance(originalEnd, path[path.Count - 1]) > 0.1f)
            {
                path.Add(originalEnd);
            }
            
            // 清理路径（但保护起点和终点）
            path = CleanupPathPreserveEndpoints(path, originalStart, originalEnd);
            path = EnforceManhattanPathPreserveEndpoints(path, originalStart, originalEnd);
            
            return path;
        }
        
        /// <summary>
        /// 清理路径，移除重复点和过近的点，但保护起点和终点
        /// </summary>
        private List<Vector2> CleanupPathPreserveEndpoints(List<Vector2> path, Vector2 start, Vector2 end)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> cleaned = new List<Vector2>();
            cleaned.Add(start); // 强制使用原始起点
            
            float minDistance = _gridResolution * 0.5f;
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                if (Vector2.Distance(path[i], cleaned[cleaned.Count - 1]) > minDistance &&
                    Vector2.Distance(path[i], end) > minDistance)
                {
                    cleaned.Add(path[i]);
                }
            }
            
            cleaned.Add(end); // 强制使用原始终点
            
            return cleaned;
        }
        
        /// <summary>
        /// 强制曼哈顿路径，但保护起点和终点
        /// 在插入corner点时验证安全性，避免穿过房间
        /// </summary>
        private List<Vector2> EnforceManhattanPathPreserveEndpoints(List<Vector2> path, Vector2 start, Vector2 end)
        {
            if (path.Count < 2)
                return path;
            
            List<Vector2> result = new List<Vector2>();
            result.Add(start); // 强制使用原始起点
            
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 prev = result[result.Count - 1];
                Vector2 curr = (i == path.Count - 1) ? end : path[i];
                
                float dx = curr.x - prev.x;
                float dy = curr.y - prev.y;
                
                // 如果是斜线，必须转换为阶梯状
                if (Mathf.Abs(dx) > 0.1f && Mathf.Abs(dy) > 0.1f)
                {
                    List<Vector2> stairPath = GenerateStairSegment(prev, curr);
                    foreach (Vector2 p in stairPath)
                    {
                        if (Vector2.Distance(p, result[result.Count - 1]) > 0.1f)
                        {
                            result.Add(p);
                        }
                    }
                }
                
                // 添加当前点
                if (Vector2.Distance(curr, result[result.Count - 1]) > 0.1f)
                {
                    result.Add(curr);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 生成阶梯状路径段（将斜线转换为阶梯）
        /// </summary>
        private List<Vector2> GenerateStairSegment(Vector2 start, Vector2 end)
        {
            List<Vector2> stair = new List<Vector2>();
            
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            
            // 方案1：先水平后垂直（L形）
            Vector2 corner1 = new Vector2(end.x, start.y);
            if (IsPointInSafeZone(corner1) && IsLineInSafeZone(start, corner1) && IsLineInSafeZone(corner1, end))
            {
                stair.Add(corner1);
                return stair;
            }
            
            // 方案2：先垂直后水平（L形）
            Vector2 corner2 = new Vector2(start.x, end.y);
            if (IsPointInSafeZone(corner2) && IsLineInSafeZone(start, corner2) && IsLineInSafeZone(corner2, end))
            {
                stair.Add(corner2);
                return stair;
            }
            
            // 方案3：Z形阶梯（中点偏移）
            float midX = (start.x + end.x) / 2f;
            float midY = (start.y + end.y) / 2f;
            
            // Z形：水平-垂直-水平
            Vector2 z1 = new Vector2(midX, start.y);
            Vector2 z2 = new Vector2(midX, end.y);
            if (IsPointInSafeZone(z1) && IsPointInSafeZone(z2) &&
                IsLineInSafeZone(start, z1) && IsLineInSafeZone(z1, z2) && IsLineInSafeZone(z2, end))
            {
                stair.Add(z1);
                stair.Add(z2);
                return stair;
            }
            
            // Z形：垂直-水平-垂直
            Vector2 z3 = new Vector2(start.x, midY);
            Vector2 z4 = new Vector2(end.x, midY);
            if (IsPointInSafeZone(z3) && IsPointInSafeZone(z4) &&
                IsLineInSafeZone(start, z3) && IsLineInSafeZone(z3, z4) && IsLineInSafeZone(z4, end))
            {
                stair.Add(z3);
                stair.Add(z4);
                return stair;
            }
            
            // 方案4：尝试偏移的Z形（向外绕行）
            for (float offset = 20f; offset <= 60f; offset += 20f)
            {
                // 向外偏移尝试
                Vector2[] offsets = new Vector2[]
                {
                    new Vector2(start.x, start.y + offset), new Vector2(end.x, start.y + offset), // 上
                    new Vector2(start.x, start.y - offset), new Vector2(end.x, start.y - offset), // 下
                    new Vector2(start.x + offset, start.y), new Vector2(start.x + offset, end.y), // 右
                    new Vector2(start.x - offset, start.y), new Vector2(start.x - offset, end.y), // 左
                };
                
                for (int j = 0; j < offsets.Length; j += 2)
                {
                    Vector2 o1 = offsets[j];
                    Vector2 o2 = offsets[j + 1];
                    if (IsPointInSafeZone(o1) && IsPointInSafeZone(o2) &&
                        IsLineInSafeZone(start, o1) && IsLineInSafeZone(o1, o2) && IsLineInSafeZone(o2, end))
                    {
                        stair.Add(o1);
                        stair.Add(o2);
                        return stair;
                    }
                }
            }
            
            // 所有方案都失败，使用简单的中点（可能产生斜线，但确保连通）
            stair.Add(new Vector2(end.x, start.y));
            return stair;
        }
        
        /// <summary>
        /// 将点延伸到所在房间的外部边界
        /// </summary>
        private Vector2 ExtendPointOutsideRoom(Vector2 point)
        {
            // 查找包含该点的房间
            Rect? containingRoom = null;
            foreach (Rect obs in _obstacles)
            {
                if (obs.Contains(point))
                {
                    containingRoom = obs;
                    break;
                }
            }
            
            if (!containingRoom.HasValue)
            {
                // 点不在任何房间内，检查是否在扩展边界内
                foreach (Rect expanded in _expandedObstacles)
                {
                    if (expanded.Contains(point))
                    {
                        // 移动到扩展边界外
                        return MoveToNearestEdge(point, expanded, 2f);
                    }
                }
                return point;
            }
            
            Rect room = containingRoom.Value;
            
            // 计算到四个边界的距离，选择最近的边界延伸
            float distToLeft = point.x - room.xMin;
            float distToRight = room.xMax - point.x;
            float distToBottom = point.y - room.yMin;
            float distToTop = room.yMax - point.y;
            
            float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            float extendDist = _obstacleMargin + 3f;
            
            if (minDist == distToLeft)
                return new Vector2(room.xMin - extendDist, point.y);
            else if (minDist == distToRight)
                return new Vector2(room.xMax + extendDist, point.y);
            else if (minDist == distToBottom)
                return new Vector2(point.x, room.yMin - extendDist);
            else
                return new Vector2(point.x, room.yMax + extendDist);
        }
        
        /// <summary>
        /// 在房间外部空间生成阶梯状路径（简化版，减少不必要的弯曲）
        /// </summary>
        private List<Vector2> GenerateExternalStairPath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            float absDx = Mathf.Abs(dx);
            float absDy = Mathf.Abs(dy);
            
            // 优先尝试简单L形路径（最简洁）
            List<Vector2> simplePath = GenerateSimpleLPath(start, end);
            if (simplePath.Count > 0 && ValidatePathInSafeZone(start, simplePath, end))
            {
                return simplePath;
            }
            
            // 计算阶梯数量（根据距离，但限制最多2个拐点）
            float totalDist = absDx + absDy;
            int steps = totalDist < 60f ? 1 : 2;
            
            // 尝试不同的阶梯模式，选择第一个有效的
            // 模式1：先水平后垂直（单拐点）
            Vector2 corner1 = new Vector2(end.x, start.y);
            if (IsPointInSafeZone(corner1) && IsLineInSafeZone(start, corner1) && IsLineInSafeZone(corner1, end))
            {
                path.Add(corner1);
                return path;
            }
            
            // 模式2：先垂直后水平（单拐点）
            Vector2 corner2 = new Vector2(start.x, end.y);
            if (IsPointInSafeZone(corner2) && IsLineInSafeZone(start, corner2) && IsLineInSafeZone(corner2, end))
            {
                path.Add(corner2);
                return path;
            }
            
            // 模式3：Z形路径（双拐点）- 仅在单拐点失败时使用
            if (steps >= 2)
            {
                float midX = (start.x + end.x) / 2f;
                float midY = (start.y + end.y) / 2f;
                
                // Z形：水平-垂直-水平
                Vector2 z1 = new Vector2(midX, start.y);
                Vector2 z2 = new Vector2(midX, end.y);
                if (IsPointInSafeZone(z1) && IsPointInSafeZone(z2) &&
                    IsLineInSafeZone(start, z1) && IsLineInSafeZone(z1, z2) && IsLineInSafeZone(z2, end))
                {
                    path.Add(z1);
                    path.Add(z2);
                    return path;
                }
                
                // Z形：垂直-水平-垂直
                Vector2 z3 = new Vector2(start.x, midY);
                Vector2 z4 = new Vector2(end.x, midY);
                if (IsPointInSafeZone(z3) && IsPointInSafeZone(z4) &&
                    IsLineInSafeZone(start, z3) && IsLineInSafeZone(z3, z4) && IsLineInSafeZone(z4, end))
                {
                    path.Add(z3);
                    path.Add(z4);
                    return path;
                }
            }
            
            // 所有简单模式都失败，返回空让A*处理
            return path;
        }
        
        /// <summary>
        /// 验证路径所有段都在安全区域
        /// </summary>
        private bool ValidatePathInSafeZone(Vector2 start, List<Vector2> middlePath, Vector2 end)
        {
            if (middlePath.Count == 0)
                return IsLineInSafeZone(start, end);
            
            // 检查起点到第一个中间点
            if (!IsLineInSafeZone(start, middlePath[0]))
                return false;
            
            // 检查中间点之间
            for (int i = 0; i < middlePath.Count - 1; i++)
            {
                if (!IsLineInSafeZone(middlePath[i], middlePath[i + 1]))
                    return false;
            }
            
            // 检查最后一个中间点到终点
            if (!IsLineInSafeZone(middlePath[middlePath.Count - 1], end))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// 原阶梯生成（已废弃，保留作为备用）
        /// </summary>
        private List<Vector2> GenerateExternalStairPathOld(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            float absDx = Mathf.Abs(dx);
            float absDy = Mathf.Abs(dy);
            
            // 计算阶梯数量（根据距离）
            float totalDist = absDx + absDy;
            if (totalDist < 30f)
            {
                // 距离较短，使用简单L形
                return GenerateSimpleLPath(start, end);
            }
            
            // 根据距离决定阶梯数量
            int steps = Mathf.Clamp((int)(totalDist / 40f), 2, 4);
            
            float stepX = dx / steps;
            float stepY = dy / steps;
            
            Vector2 current = start;
            
            // 随机选择阶梯模式
            int stairMode = _rng.Next(0, 3);
            
            for (int i = 1; i < steps; i++)
            {
                Vector2 nextTarget = new Vector2(start.x + stepX * i, start.y + stepY * i);
                
                if (stairMode == 0)
                {
                    // 模式0：先水平后垂直
                    Vector2 horizontal = new Vector2(nextTarget.x, current.y);
                    if (IsPointInSafeZone(horizontal))
                    {
                        path.Add(horizontal);
                        path.Add(nextTarget);
                        current = nextTarget;
                    }
                }
                else if (stairMode == 1)
                {
                    // 模式1：先垂直后水平
                    Vector2 vertical = new Vector2(current.x, nextTarget.y);
                    if (IsPointInSafeZone(vertical))
                    {
                        path.Add(vertical);
                        path.Add(nextTarget);
                        current = nextTarget;
                    }
                }
                else
                {
                    // 模式2：交替
                    bool horizontalFirst = (i % 2 == 0);
                    if (horizontalFirst)
                    {
                        Vector2 horizontal = new Vector2(nextTarget.x, current.y);
                        if (IsPointInSafeZone(horizontal))
                        {
                            path.Add(horizontal);
                            path.Add(nextTarget);
                        }
                    }
                    else
                    {
                        Vector2 vertical = new Vector2(current.x, nextTarget.y);
                        if (IsPointInSafeZone(vertical))
                        {
                            path.Add(vertical);
                            path.Add(nextTarget);
                        }
                    }
                    current = nextTarget;
                }
            }
            
            return path;
        }
        
        /// <summary>
        /// 生成简单的L形路径
        /// </summary>
        private List<Vector2> GenerateSimpleLPath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            // 尝试两种L形
            Vector2 corner1 = new Vector2(end.x, start.y);
            Vector2 corner2 = new Vector2(start.x, end.y);
            
            if (IsPointInSafeZone(corner1) && IsLineInSafeZone(start, corner1) && IsLineInSafeZone(corner1, end))
            {
                path.Add(corner1);
                return path;
            }
            
            if (IsPointInSafeZone(corner2) && IsLineInSafeZone(start, corner2) && IsLineInSafeZone(corner2, end))
            {
                path.Add(corner2);
                return path;
            }
            
            // 尝试Z形
            float midY = (start.y + end.y) / 2;
            Vector2 mid1 = new Vector2(start.x, midY);
            Vector2 mid2 = new Vector2(end.x, midY);
            
            if (IsPointInSafeZone(mid1) && IsPointInSafeZone(mid2) &&
                IsLineInSafeZone(start, mid1) && IsLineInSafeZone(mid1, mid2) && IsLineInSafeZone(mid2, end))
            {
                path.Add(mid1);
                path.Add(mid2);
                return path;
            }
            
            return path;
        }
        
        /// <summary>
        /// 使用A*在房间外部空间寻路
        /// </summary>
        private List<Vector2> GenerateAStarExternalPath(Vector2 start, Vector2 end)
        {
            Vector2Int gridStart = WorldToGrid(start);
            Vector2Int gridEnd = WorldToGrid(end);
            
            List<Vector2Int> astarPath = AStarManhattanSearch(gridStart, gridEnd);
            
            if (astarPath.Count > 0)
            {
                return ConvertToStrictManhattan(astarPath);
            }
            
            // A*失败，尝试带航点的寻路
            astarPath = FindPathWithWaypoints(gridStart, gridEnd);
            if (astarPath.Count > 0)
            {
                return ConvertToStrictManhattan(astarPath);
            }
            
            return new List<Vector2>();
        }
        
        /// <summary>
        /// 检查点是否在安全区域（所有房间外部）
        /// </summary>
        private bool IsPointInSafeZone(Vector2 point)
        {
            if (!_layoutBounds.Contains(point))
                return false;
            
            foreach (Rect expanded in _expandedObstacles)
            {
                if (expanded.Contains(point))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// 检查线段是否完全在安全区域
        /// </summary>
        private bool IsLineInSafeZone(Vector2 start, Vector2 end)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(start, end) / 2f);
            steps = Mathf.Max(steps, 10);
            
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                if (!IsPointInSafeZone(point))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// 验证路径段是否安全
        /// </summary>
        private bool ValidatePathSegments(Vector2 start, List<Vector2> middlePath, Vector2 end)
        {
            if (middlePath.Count == 0)
                return false;
            
            // 检查从起点到第一个中间点
            if (!IsLineInSafeZone(start, middlePath[0]))
                return false;
            
            // 检查中间点之间
            for (int i = 0; i < middlePath.Count - 1; i++)
            {
                if (!IsLineInSafeZone(middlePath[i], middlePath[i + 1]))
                    return false;
            }
            
            // 检查从最后一个中间点到终点
            if (!IsLineInSafeZone(middlePath[middlePath.Count - 1], end))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// 将点移动到矩形边缘外部
        /// </summary>
        private Vector2 MoveToNearestEdge(Vector2 point, Rect rect, float extraMargin)
        {
            float distToLeft = point.x - rect.xMin;
            float distToRight = rect.xMax - point.x;
            float distToBottom = point.y - rect.yMin;
            float distToTop = rect.yMax - point.y;
            
            float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            
            if (minDist == distToLeft)
                return new Vector2(rect.xMin - extraMargin, point.y);
            else if (minDist == distToRight)
                return new Vector2(rect.xMax + extraMargin, point.y);
            else if (minDist == distToBottom)
                return new Vector2(point.x, rect.yMin - extraMargin);
            else
                return new Vector2(point.x, rect.yMax + extraMargin);
        }
        
        /// <summary>
        /// 验证整个路径是否安全（不穿过任何障碍物）
        /// </summary>
        private bool ValidatePathSafety(List<Vector2> path)
        {
            if (path.Count < 2)
                return true;
            
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!IsLineWalkableSafe(path[i], path[i + 1]))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 生成绝对安全的A*路径
        /// </summary>
        private List<Vector2> GenerateSafeAStarPath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            path.Add(start);
            
            Vector2Int gridStart = WorldToGrid(start);
            Vector2Int gridEnd = WorldToGrid(end);
            
            // 使用带端点的A*寻路
            List<Vector2Int> astarPath = AStarSearchWithEndpoints(gridStart, gridEnd);
            
            if (astarPath.Count > 0)
            {
                List<Vector2> converted = ConvertToStrictManhattan(astarPath);
                path.AddRange(converted);
            }
            else
            {
                // 最后的最后：使用中间航点寻路
                List<Vector2Int> waypointPath = FindPathWithWaypoints(gridStart, gridEnd);
                if (waypointPath.Count > 0)
                {
                    List<Vector2> converted = ConvertToStrictManhattan(waypointPath);
                    path.AddRange(converted);
                }
            }
            
            path.Add(end);
            path = CleanupPath(path);
            path = EnforceManhattanPath(path);
            
            return path;
        }
        
        /// <summary>
        /// 找到最佳的曼哈顿路径（优先使用阶梯状路径）
        /// </summary>
        private List<Vector2> FindBestManhattanPath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            
            // 优先尝试阶梯状路径（更自然的走廊效果）
            List<Vector2> stairPath = GenerateStairPath(start, end);
            if (stairPath.Count > 0 && IsPathWalkable(start, stairPath, end))
            {
                return stairPath;
            }
            
            // 方案1: L形 - 先水平后垂直
            Vector2 corner1 = new Vector2(end.x, start.y);
            if (IsLineWalkableSafe(start, corner1) && IsLineWalkableSafe(corner1, end))
            {
                path.Add(corner1);
                return path;
            }
            
            // 方案2: L形 - 先垂直后水平
            Vector2 corner2 = new Vector2(start.x, end.y);
            if (IsLineWalkableSafe(start, corner2) && IsLineWalkableSafe(corner2, end))
            {
                path.Add(corner2);
                return path;
            }
            
            // 方案3-7: Z形路径，尝试不同的中间线位置
            float[] ratios = { 0.5f, 0.3f, 0.7f, 0.2f, 0.8f };
            foreach (float ratio in ratios)
            {
                // 水平中间线
                float midY = start.y + dy * ratio;
                Vector2 mid1 = new Vector2(start.x, midY);
                Vector2 mid2 = new Vector2(end.x, midY);
                if (IsLineWalkableSafe(start, mid1) && IsLineWalkableSafe(mid1, mid2) && IsLineWalkableSafe(mid2, end))
                {
                    path.Add(mid1);
                    path.Add(mid2);
                    return path;
                }
                
                // 垂直中间线
                float midX = start.x + dx * ratio;
                Vector2 mid3 = new Vector2(midX, start.y);
                Vector2 mid4 = new Vector2(midX, end.y);
                if (IsLineWalkableSafe(start, mid3) && IsLineWalkableSafe(mid3, mid4) && IsLineWalkableSafe(mid4, end))
                {
                    path.Add(mid3);
                    path.Add(mid4);
                    return path;
                }
            }
            
            // 方案8: 使用4方向A*寻路（这是最可靠的方案）
            Vector2Int gridStart = WorldToGrid(start);
            Vector2Int gridEnd = WorldToGrid(end);
            List<Vector2Int> astarPath = AStarManhattanSearch(gridStart, gridEnd);
            
            if (astarPath.Count > 0)
            {
                path = ConvertToStrictManhattan(astarPath);
                return path;
            }
            
            // 最后方案: 尝试L形路径，但必须经过障碍物检测
            Vector2 cornerA = new Vector2(end.x, start.y);
            Vector2 cornerB = new Vector2(start.x, end.y);
            
            // 尝试方案A: 先水平后垂直
            if (IsLineWalkableSafe(start, cornerA) && IsLineWalkableSafe(cornerA, end))
            {
                path.Add(cornerA);
                return path;
            }
            
            // 尝试方案B: 先垂直后水平
            if (IsLineWalkableSafe(start, cornerB) && IsLineWalkableSafe(cornerB, end))
            {
                path.Add(cornerB);
                return path;
            }
            
            // 如果所有方案都失败，使用绕行路径
            // 寻找一个远离所有障碍物的中间点
            Vector2 safeMiddle = FindSafeMiddlePoint(start, end);
            if (safeMiddle != Vector2.zero)
            {
                // 使用安全中间点创建U形或S形路径
                Vector2 mid1, mid2;
                if (Mathf.Abs(end.x - start.x) > Mathf.Abs(end.y - start.y))
                {
                    // 水平方向为主，创建垂直绕行
                    mid1 = new Vector2(start.x, safeMiddle.y);
                    mid2 = new Vector2(end.x, safeMiddle.y);
                }
                else
                {
                    // 垂直方向为主，创建水平绕行
                    mid1 = new Vector2(safeMiddle.x, start.y);
                    mid2 = new Vector2(safeMiddle.x, end.y);
                }
                
                if (IsLineWalkableSafe(start, mid1) && IsLineWalkableSafe(mid1, mid2) && IsLineWalkableSafe(mid2, end))
                {
                    path.Add(mid1);
                    path.Add(mid2);
                    return path;
                }
            }
            
            // 真正的最后方案：返回空路径，让上层处理
            return path;
        }
        
        /// <summary>
        /// 寻找一个远离所有障碍物的安全中间点
        /// </summary>
        private Vector2 FindSafeMiddlePoint(Vector2 start, Vector2 end)
        {
            float safeMargin = _obstacleMargin + 10f;
            
            // 尝试在布局边界的不同位置寻找安全点
            float[] offsets = { -50f, -30f, 30f, 50f };
            
            foreach (float offset in offsets)
            {
                // 尝试水平方向的偏移
                Vector2 candidate = new Vector2((start.x + end.x) / 2, (start.y + end.y) / 2 + offset);
                if (IsPointSafe(candidate, safeMargin))
                {
                    return candidate;
                }
                
                // 尝试垂直方向的偏移
                candidate = new Vector2((start.x + end.x) / 2 + offset, (start.y + end.y) / 2);
                if (IsPointSafe(candidate, safeMargin))
                {
                    return candidate;
                }
            }
            
            return Vector2.zero;
        }
        
        /// <summary>
        /// 检查点是否安全（远离所有障碍物）
        /// </summary>
        private bool IsPointSafe(Vector2 point, float margin)
        {
            if (!_layoutBounds.Contains(point))
                return false;
            
            foreach (Rect obstacle in _obstacles)
            {
                Rect expanded = new Rect(
                    obstacle.x - margin,
                    obstacle.y - margin,
                    obstacle.width + margin * 2,
                    obstacle.height + margin * 2
                );
                
                if (expanded.Contains(point))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 生成阶梯状路径（逐步升高/降低到达目标）
        /// 降低条件，确保更多情况下生成阶梯
        /// </summary>
        private List<Vector2> GenerateStairPath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            
            float absDx = Mathf.Abs(dx);
            float absDy = Mathf.Abs(dy);
            
            // 放宽条件：只要有一定距离就尝试阶梯
            // 至少需要20单位的总距离
            if (absDx + absDy < 20f)
            {
                return path;
            }
            
            // 根据距离比例决定阶梯数量
            float totalDist = absDx + absDy;
            int steps = Mathf.Clamp((int)(totalDist / 30f), 2, 4);
            
            // 随机选择阶梯类型
            int stairType = _rng.Next(0, 3);
            
            // 根据主要方向调整阶梯生成
            bool mainlyHorizontal = absDx > absDy;
            
            if (mainlyHorizontal)
            {
                // 水平为主：生成水平段+垂直跳跃
                float segmentWidth = dx / steps;
                float segmentHeight = dy / steps;
                
                Vector2 current = start;
                for (int i = 1; i < steps; i++)
                {
                    float nextX = start.x + segmentWidth * i;
                    // 先水平移动
                    Vector2 horizontal = new Vector2(nextX, current.y);
                    path.Add(horizontal);
                    // 再垂直移动
                    float nextY = start.y + segmentHeight * i;
                    Vector2 vertical = new Vector2(nextX, nextY);
                    path.Add(vertical);
                    current = vertical;
                }
            }
            else
            {
                // 垂直为主：生成垂直段+水平跳跃
                float segmentWidth = dx / steps;
                float segmentHeight = dy / steps;
                
                Vector2 current = start;
                for (int i = 1; i < steps; i++)
                {
                    float nextY = start.y + segmentHeight * i;
                    // 先垂直移动
                    Vector2 vertical = new Vector2(current.x, nextY);
                    path.Add(vertical);
                    // 再水平移动
                    float nextX = start.x + segmentWidth * i;
                    Vector2 horizontal = new Vector2(nextX, nextY);
                    path.Add(horizontal);
                    current = horizontal;
                }
            }
            
            return path;
        }
        
        /// <summary>
        /// 检查整个路径是否可通行
        /// </summary>
        private bool IsPathWalkable(Vector2 start, List<Vector2> middlePath, Vector2 end)
        {
            Vector2 prev = start;
            
            foreach (Vector2 point in middlePath)
            {
                if (!IsLineWalkableSafe(prev, point))
                    return false;
                prev = point;
            }
            
            if (middlePath.Count > 0)
            {
                return IsLineWalkableSafe(middlePath[middlePath.Count - 1], end);
            }
            
            return IsLineWalkableSafe(start, end);
        }
        
        /// <summary>
        /// 强制将路径转换为纯曼哈顿路径（消除所有斜线）
        /// </summary>
        private List<Vector2> EnforceManhattanPath(List<Vector2> path)
        {
            if (path.Count < 2)
                return path;
            
            List<Vector2> result = new List<Vector2>();
            result.Add(path[0]);
            
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 prev = result[result.Count - 1];
                Vector2 curr = path[i];
                
                float dx = curr.x - prev.x;
                float dy = curr.y - prev.y;
                
                // 如果是斜线（dx和dy都不为0），插入一个中间点
                if (Mathf.Abs(dx) > 0.1f && Mathf.Abs(dy) > 0.1f)
                {
                    // 插入L形拐点：先水平后垂直
                    Vector2 corner = new Vector2(curr.x, prev.y);
                    result.Add(corner);
                }
                
                result.Add(curr);
            }
            
            return result;
        }
        
        /// <summary>
        /// 根据点的位置推断出口方向
        /// </summary>
        private Direction GetExitDirection(Vector2 point)
        {
            // 检查点在哪个房间的边界上
            foreach (Rect obstacle in _obstacles)
            {
                float distToLeft = Mathf.Abs(point.x - obstacle.xMin);
                float distToRight = Mathf.Abs(point.x - obstacle.xMax);
                float distToBottom = Mathf.Abs(point.y - obstacle.yMin);
                float distToTop = Mathf.Abs(point.y - obstacle.yMax);
                
                float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
                float threshold = _gridResolution * 2;
                
                if (minDist < threshold)
                {
                    if (minDist == distToLeft) return Direction.West;
                    if (minDist == distToRight) return Direction.East;
                    if (minDist == distToBottom) return Direction.South;
                    if (minDist == distToTop) return Direction.North;
                }
            }
            
            // 默认向下
            return Direction.South;
        }
        
        /// <summary>
        /// 根据点的位置推断入口方向
        /// </summary>
        private Direction GetEntranceDirection(Vector2 point)
        {
            // 入口方向与出口方向逻辑相同
            return GetExitDirection(point);
        }
        
        /// <summary>
        /// 在指定方向上延伸
        /// </summary>
        private Vector2 ExtendInDirection(Vector2 point, Direction dir, float distance)
        {
            switch (dir)
            {
                case Direction.North: return new Vector2(point.x, point.y + distance);
                case Direction.South: return new Vector2(point.x, point.y - distance);
                case Direction.East: return new Vector2(point.x + distance, point.y);
                case Direction.West: return new Vector2(point.x - distance, point.y);
                default: return new Vector2(point.x, point.y - distance);
            }
        }
        
        /// <summary>
        /// 检查点是否安全（不在障碍物内）
        /// </summary>
        private bool IsPointSafe(Vector2 point)
        {
            foreach (Rect obstacle in _obstacles)
            {
                if (obstacle.Contains(point))
                    return false;
            }
            return _layoutBounds.Contains(point);
        }
        
        /// <summary>
        /// 在指定方向上找到安全点
        /// </summary>
        private Vector2 FindSafePointInDirection(Vector2 start, Direction dir, float maxDistance)
        {
            float step = _gridResolution;
            for (float d = step; d <= maxDistance * 2; d += step)
            {
                Vector2 testPoint = ExtendInDirection(start, dir, d);
                if (IsPointSafe(testPoint))
                    return testPoint;
            }
            return ExtendInDirection(start, dir, maxDistance);
        }
        
        /// <summary>
        /// 生成曼哈顿风格的路径（纯水平和垂直线段，无斜线）
        /// </summary>
        private List<Vector2> GenerateManhattanPath(Vector2 start, Vector2 end)
        {
            List<Vector2> path = new List<Vector2>();
            
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            
            if (Mathf.Abs(dx) < _gridResolution && Mathf.Abs(dy) < _gridResolution)
            {
                // 起点和终点很近，直接连接
                return path;
            }
            
            // 尝试多种L形路径
            // 方案1: 先水平后垂直
            Vector2 corner1 = new Vector2(end.x, start.y);
            if (IsLineWalkableSafe(start, corner1) && IsLineWalkableSafe(corner1, end))
            {
                path.Add(corner1);
                return path;
            }
            
            // 方案2: 先垂直后水平
            Vector2 corner2 = new Vector2(start.x, end.y);
            if (IsLineWalkableSafe(start, corner2) && IsLineWalkableSafe(corner2, end))
            {
                path.Add(corner2);
                return path;
            }
            
            // 方案3: 使用中间水平线的Z形路径
            float midY = (start.y + end.y) / 2;
            Vector2 mid1 = new Vector2(start.x, midY);
            Vector2 mid2 = new Vector2(end.x, midY);
            if (IsLineWalkableSafe(start, mid1) && IsLineWalkableSafe(mid1, mid2) && IsLineWalkableSafe(mid2, end))
            {
                path.Add(mid1);
                path.Add(mid2);
                return path;
            }
            
            // 方案4: 使用中间垂直线的Z形路径
            float midX = (start.x + end.x) / 2;
            Vector2 mid3 = new Vector2(midX, start.y);
            Vector2 mid4 = new Vector2(midX, end.y);
            if (IsLineWalkableSafe(start, mid3) && IsLineWalkableSafe(mid3, mid4) && IsLineWalkableSafe(mid4, end))
            {
                path.Add(mid3);
                path.Add(mid4);
                return path;
            }
            
            // 方案5: 尝试多个中间点位置
            for (float ratio = 0.3f; ratio <= 0.7f; ratio += 0.1f)
            {
                // 水平中间线
                midY = start.y + dy * ratio;
                mid1 = new Vector2(start.x, midY);
                mid2 = new Vector2(end.x, midY);
                if (IsLineWalkableSafe(start, mid1) && IsLineWalkableSafe(mid1, mid2) && IsLineWalkableSafe(mid2, end))
                {
                    path.Add(mid1);
                    path.Add(mid2);
                    return path;
                }
                
                // 垂直中间线
                midX = start.x + dx * ratio;
                mid3 = new Vector2(midX, start.y);
                mid4 = new Vector2(midX, end.y);
                if (IsLineWalkableSafe(start, mid3) && IsLineWalkableSafe(mid3, mid4) && IsLineWalkableSafe(mid4, end))
                {
                    path.Add(mid3);
                    path.Add(mid4);
                    return path;
                }
            }
            
            // 如果所有简单路径都失败，使用A*寻路并转换为曼哈顿
            Vector2Int gridStart = WorldToGrid(start);
            Vector2Int gridEnd = WorldToGrid(end);
            List<Vector2Int> astarPath = AStarManhattanSearch(gridStart, gridEnd);
            
            if (astarPath.Count > 0)
            {
                // 将A*路径转换为纯曼哈顿路径点
                path = ConvertToStrictManhattan(astarPath);
            }
            
            return path;
        }
        
        /// <summary>
        /// 曼哈顿风格的A*寻路（只允许4方向移动）
        /// </summary>
        private List<Vector2Int> AStarManhattanSearch(Vector2Int start, Vector2Int goal)
        {
            List<PathNode> openList = new List<PathNode>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, PathNode> allNodes = new Dictionary<Vector2Int, PathNode>();
            
            PathNode startNode = new PathNode(start);
            startNode.GCost = 0;
            startNode.HCost = ManhattanDistance(start, goal);
            openList.Add(startNode);
            allNodes[start] = startNode;
            
            int maxIterations = 20000;
            int iterations = 0;
            
            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                PathNode current = GetLowestFCostNode(openList);
                
                if (current.Position == goal)
                {
                    return ReconstructPath(current);
                }
                
                openList.Remove(current);
                closedSet.Add(current.Position);
                
                // 只使用4方向邻居（上下左右，无对角线）
                foreach (Vector2Int neighborPos in Get4DirectionNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighborPos))
                        continue;
                    
                    if (!IsWalkable(neighborPos) && neighborPos != goal)
                        continue;
                    
                    float moveCost = 1f;
                    float newGCost = current.GCost + moveCost;
                    
                    if (!allNodes.TryGetValue(neighborPos, out PathNode neighbor))
                    {
                        neighbor = new PathNode(neighborPos);
                        allNodes[neighborPos] = neighbor;
                    }
                    
                    if (newGCost < neighbor.GCost)
                    {
                        neighbor.Parent = current;
                        neighbor.GCost = newGCost;
                        neighbor.HCost = ManhattanDistance(neighborPos, goal);
                        
                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }
            
            return new List<Vector2Int>();
        }
        
        /// <summary>
        /// 获取4方向邻居（无对角线）
        /// </summary>
        private List<Vector2Int> Get4DirectionNeighbors(Vector2Int pos)
        {
            return new List<Vector2Int>
            {
                new Vector2Int(pos.x, pos.y + 1),  // 上
                new Vector2Int(pos.x, pos.y - 1),  // 下
                new Vector2Int(pos.x - 1, pos.y),  // 左
                new Vector2Int(pos.x + 1, pos.y)   // 右
            };
        }
        
        /// <summary>
        /// 曼哈顿距离
        /// </summary>
        private float ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        /// <summary>
        /// 将A*路径转换为严格的曼哈顿路径（只保留拐点）
        /// </summary>
        private List<Vector2> ConvertToStrictManhattan(List<Vector2Int> gridPath)
        {
            if (gridPath.Count <= 2)
            {
                List<Vector2> result = new List<Vector2>();
                foreach (var p in gridPath)
                {
                    result.Add(GridToWorld(p));
                }
                return result;
            }
            
            List<Vector2> path = new List<Vector2>();
            
            Vector2Int lastDirection = Vector2Int.zero;
            
            for (int i = 1; i < gridPath.Count; i++)
            {
                Vector2Int currentDir = gridPath[i] - gridPath[i - 1];
                
                // 规范化方向
                if (currentDir.x != 0) currentDir.x = currentDir.x > 0 ? 1 : -1;
                if (currentDir.y != 0) currentDir.y = currentDir.y > 0 ? 1 : -1;
                
                // 如果方向改变，添加前一个点作为拐点
                if (currentDir != lastDirection && lastDirection != Vector2Int.zero)
                {
                    path.Add(GridToWorld(gridPath[i - 1]));
                }
                
                lastDirection = currentDir;
            }
            
            return path;
        }
        
        /// <summary>
        /// 将路径简化为曼哈顿风格（只保留拐点）
        /// </summary>
        private List<Vector2> SimplifyToManhattan(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> simplified = new List<Vector2>();
            
            Vector2 lastDir = Vector2.zero;
            
            for (int i = 0; i < path.Count; i++)
            {
                if (i == 0)
                {
                    continue; // 跳过起点，由调用者添加
                }
                
                Vector2 currentDir = (path[i] - path[i - 1]).normalized;
                
                // 量化方向为水平或垂直
                if (Mathf.Abs(currentDir.x) > Mathf.Abs(currentDir.y))
                {
                    currentDir = new Vector2(Mathf.Sign(currentDir.x), 0);
                }
                else
                {
                    currentDir = new Vector2(0, Mathf.Sign(currentDir.y));
                }
                
                // 如果方向改变，添加拐点
                if (currentDir != lastDir && lastDir != Vector2.zero)
                {
                    simplified.Add(path[i - 1]);
                }
                
                lastDir = currentDir;
            }
            
            return simplified;
        }
        
        /// <summary>
        /// 清理路径，移除重复点和过近的点
        /// </summary>
        private List<Vector2> CleanupPath(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> cleaned = new List<Vector2>();
            cleaned.Add(path[0]);
            
            float minDistance = _gridResolution * 0.5f;
            
            for (int i = 1; i < path.Count; i++)
            {
                if (Vector2.Distance(path[i], cleaned[cleaned.Count - 1]) > minDistance)
                {
                    cleaned.Add(path[i]);
                }
            }
            
            // 确保终点存在
            if (Vector2.Distance(path[path.Count - 1], cleaned[cleaned.Count - 1]) > 0.1f)
            {
                cleaned.Add(path[path.Count - 1]);
            }
            
            return cleaned;
        }
        
        // ==================== A*寻路算法 ====================
        
        /// <summary>
        /// A*寻路，允许起点和终点作为特殊节点
        /// </summary>
        private List<Vector2Int> AStarSearchWithEndpoints(Vector2Int start, Vector2Int goal)
        {
            // 开放列表和关闭列表
            List<PathNode> openList = new List<PathNode>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, PathNode> allNodes = new Dictionary<Vector2Int, PathNode>();
            
            // 创建起点节点（起点总是有效的）
            PathNode startNode = new PathNode(start);
            startNode.GCost = 0;
            startNode.HCost = Heuristic(start, goal);
            openList.Add(startNode);
            allNodes[start] = startNode;
            
            int maxIterations = 20000;
            int iterations = 0;
            
            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                // 找到F值最小的节点
                PathNode current = GetLowestFCostNode(openList);
                
                // 到达目标（或足够接近）
                if (current.Position == goal || Vector2Int.Distance(current.Position, goal) <= _gridResolution)
                {
                    return ReconstructPath(current);
                }
                
                openList.Remove(current);
                closedSet.Add(current.Position);
                
                // 遍历邻居节点（8方向）
                foreach (Vector2Int neighborPos in GetNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighborPos))
                        continue;
                    
                    // 检查是否可通行（终点总是有效的）
                    bool isGoal = (neighborPos == goal);
                    if (!isGoal && !IsWalkable(neighborPos))
                        continue;
                    
                    // 计算新的G值
                    float moveCost = Vector2Int.Distance(current.Position, neighborPos);
                    // 添加穿越障碍物的惩罚
                    if (!IsWalkable(neighborPos) && !isGoal)
                    {
                        moveCost += 100f;
                    }
                    // 添加随机扰动以产生更自然的路径
                    moveCost += (float)_rng.NextDouble() * 0.3f;
                    float newGCost = current.GCost + moveCost;
                    
                    // 获取或创建邻居节点
                    if (!allNodes.TryGetValue(neighborPos, out PathNode neighbor))
                    {
                        neighbor = new PathNode(neighborPos);
                        allNodes[neighborPos] = neighbor;
                    }
                    
                    // 如果找到更短的路径
                    if (newGCost < neighbor.GCost)
                    {
                        neighbor.Parent = current;
                        neighbor.GCost = newGCost;
                        neighbor.HCost = Heuristic(neighborPos, goal);
                        
                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }
            
            // 找不到路径
            return new List<Vector2Int>();
        }
        
        /// <summary>
        /// 使用中间点绕行寻路
        /// </summary>
        private List<Vector2Int> FindPathWithWaypoints(Vector2Int start, Vector2Int goal)
        {
            // 尝试在布局区域的四个角落和中心添加中间点
            List<Vector2Int> waypoints = new List<Vector2Int>
            {
                new Vector2Int((int)(_layoutBounds.xMin / _gridResolution), (int)(_layoutBounds.yMin / _gridResolution)),
                new Vector2Int((int)(_layoutBounds.xMax / _gridResolution), (int)(_layoutBounds.yMin / _gridResolution)),
                new Vector2Int((int)(_layoutBounds.xMin / _gridResolution), (int)(_layoutBounds.yMax / _gridResolution)),
                new Vector2Int((int)(_layoutBounds.xMax / _gridResolution), (int)(_layoutBounds.yMax / _gridResolution)),
                new Vector2Int((int)(_layoutBounds.center.x / _gridResolution), (int)(_layoutBounds.center.y / _gridResolution))
            };
            
            // 找到最佳中间点
            Vector2Int bestWaypoint = start;
            float bestScore = float.MaxValue;
            
            foreach (var wp in waypoints)
            {
                if (IsWalkable(wp))
                {
                    float score = Vector2Int.Distance(start, wp) + Vector2Int.Distance(wp, goal);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestWaypoint = wp;
                    }
                }
            }
            
            // 尝试通过中间点寻路
            if (bestWaypoint != start)
            {
                List<Vector2Int> path1 = AStarSearchWithEndpoints(start, bestWaypoint);
                List<Vector2Int> path2 = AStarSearchWithEndpoints(bestWaypoint, goal);
                
                if (path1.Count > 0 && path2.Count > 0)
                {
                    // 合并路径（去除重复的中间点）
                    path1.AddRange(path2.GetRange(1, path2.Count - 1));
                    return path1;
                }
            }
            
            return new List<Vector2Int>();
        }
        
        /// <summary>
        /// 安全的路径平滑（不穿过障碍物）
        /// </summary>
        private List<Vector2> SmoothPathSafe(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> smoothed = new List<Vector2>();
            smoothed.Add(path[0]);
            
            int current = 0;
            while (current < path.Count - 1)
            {
                // 尝试跳过中间点直接连接（但必须检查是否穿过障碍物）
                int farthest = current + 1;
                for (int i = path.Count - 1; i > current + 1; i--)
                {
                    if (IsLineWalkableSafe(path[current], path[i]))
                    {
                        farthest = i;
                        break;
                    }
                }
                
                smoothed.Add(path[farthest]);
                current = farthest;
            }
            
            return smoothed;
        }
        
        /// <summary>
        /// 检查线段是否不穿过障碍物（使用扩展后的障碍物边界）
        /// </summary>
        private bool IsLineWalkableSafe(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / (_gridResolution * 0.3f));
            steps = Mathf.Max(steps, 20);
            
            // 使用更大的margin来防止走廊太靠近房间
            float safeMargin = _obstacleMargin + 5f;
            
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                // 检查点是否在任何扩展障碍物内
                foreach (Rect obstacle in _obstacles)
                {
                    // 创建扩展后的障碍物边界
                    Rect expandedObstacle = new Rect(
                        obstacle.x - safeMargin,
                        obstacle.y - safeMargin,
                        obstacle.width + safeMargin * 2,
                        obstacle.height + safeMargin * 2
                    );
                    
                    if (expandedObstacle.Contains(point))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 基于预烘焙网格的安全路径简化（强制曼哈顿风格）
        /// 只允许合并同一水平线或同一垂直线上的点，禁止斜线简化
        /// </summary>
        private List<Vector2> SimplifyPathSafeWithLinecast(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> simplified = new List<Vector2>();
            simplified.Add(path[0]);
            
            int current = 0;
            while (current < path.Count - 1)
            {
                // 从最远点开始尝试直连，但只允许曼哈顿方向
                int farthest = current + 1;
                for (int i = path.Count - 1; i > current + 1; i--)
                {
                    Vector2 p1 = path[current];
                    Vector2 p2 = path[i];
                    
                    // 【关键】只有当两点在同一水平线或同一垂直线上时才允许简化
                    bool isHorizontal = Mathf.Abs(p1.y - p2.y) < 0.5f;  // Y坐标相同
                    bool isVertical = Mathf.Abs(p1.x - p2.x) < 0.5f;    // X坐标相同
                    
                    // 如果既不是水平也不是垂直，跳过（禁止斜线简化）
                    if (!isHorizontal && !isVertical)
                        continue;
                    
                    // 只有共线时才检测是否可通行
                    if (IsLineCompletelyWalkableManhattan(p1, p2, isHorizontal))
                    {
                        farthest = i;
                        break;
                    }
                }
                
                simplified.Add(path[farthest]);
                current = farthest;
            }
            
            // 确保终点正确
            if (simplified.Count > 0 && Vector2.Distance(simplified[simplified.Count - 1], path[path.Count - 1]) > 0.1f)
            {
                simplified.Add(path[path.Count - 1]);
            }
            
            return simplified;
        }
        
        /// <summary>
        /// 检查曼哈顿线段是否完全可通行（带安全边距）
        /// </summary>
        private bool IsLineCompletelyWalkableManhattan(Vector2 start, Vector2 end, bool isHorizontal)
        {
            float distance = Vector2.Distance(start, end);
            
            // 高密度采样
            int steps = Mathf.Max(10, Mathf.CeilToInt(distance / (_gridResolution * 0.5f)));
            
            // 安全边距（垂直于线段方向检测）
            float safetyMargin = _gridResolution * 0.5f;
            
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                // 检测中心点
                if (!IsWorldPointWalkable(point))
                    return false;
                
                // 检测安全边距（垂直于移动方向）
                if (isHorizontal)
                {
                    // 水平线段：检测上下边距
                    if (!IsWorldPointWalkable(point + Vector2.up * safetyMargin))
                        return false;
                    if (!IsWorldPointWalkable(point + Vector2.down * safetyMargin))
                        return false;
                }
                else
                {
                    // 垂直线段：检测左右边距
                    if (!IsWorldPointWalkable(point + Vector2.left * safetyMargin))
                        return false;
                    if (!IsWorldPointWalkable(point + Vector2.right * safetyMargin))
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查线段是否完全可通行（使用预烘焙网格，高密度采样）
        /// </summary>
        private bool IsLineCompletelyWalkable(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            
            // 高密度采样：每半个网格单位检测一次
            int steps = Mathf.Max(20, Mathf.CeilToInt(distance / (_gridResolution * 0.5f)));
            
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                // 使用预烘焙网格进行 O(1) 检测
                if (!IsWorldPointWalkable(point))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 强制将路径规范化为曼哈顿风格（最终保险）
        /// 将任何斜线段转换为 L 形拐弯
        /// </summary>
        private List<Vector2> ForceManhattanPath(List<Vector2> path)
        {
            if (path.Count <= 1)
                return path;
            
            List<Vector2> manhattanPath = new List<Vector2>();
            manhattanPath.Add(path[0]);
            
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 prev = manhattanPath[manhattanPath.Count - 1];
                Vector2 curr = path[i];
                
                float dx = curr.x - prev.x;
                float dy = curr.y - prev.y;
                
                // 检查是否是斜线（既有X偏移又有Y偏移）
                bool hasXOffset = Mathf.Abs(dx) > 0.5f;
                bool hasYOffset = Mathf.Abs(dy) > 0.5f;
                
                if (hasXOffset && hasYOffset)
                {
                    // 斜线 -> 转换为 L 形
                    // 先水平后垂直
                    Vector2 corner = new Vector2(curr.x, prev.y);
                    manhattanPath.Add(corner);
                }
                
                manhattanPath.Add(curr);
            }
            
            return manhattanPath;
        }
        
        /// <summary>
        /// 去除路径中的重复点（距离小于阈值的连续点）
        /// </summary>
        private List<Vector2> RemoveDuplicatePoints(List<Vector2> path, float threshold = 0.5f)
        {
            if (path.Count <= 1)
                return path;
            
            List<Vector2> cleaned = new List<Vector2>();
            cleaned.Add(path[0]);
            
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 lastAdded = cleaned[cleaned.Count - 1];
                if (Vector2.Distance(path[i], lastAdded) >= threshold)
                {
                    cleaned.Add(path[i]);
                }
            }
            
            return cleaned;
        }
        
        /// <summary>
        /// 严格合并共线点（使用更小的容差）
        /// 仅当三个点完全在一条水平线或垂直线上时，删除中间点
        /// 绝不进行射线检测，绝不跨越空格连接
        /// </summary>
        private List<Vector2> MergeCollinearPointsStrict(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> simplified = new List<Vector2>();
            simplified.Add(path[0]);
            
            // 容差值：处理浮点数微小误差
            float tolerance = 0.01f;
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 prev = simplified[simplified.Count - 1];
                Vector2 curr = path[i];
                Vector2 next = path[i + 1];
                
                // 检查 prev -> curr 的方向
                bool seg1Horizontal = Mathf.Abs(prev.y - curr.y) < tolerance;
                bool seg1Vertical = Mathf.Abs(prev.x - curr.x) < tolerance;
                
                // 检查 curr -> next 的方向
                bool seg2Horizontal = Mathf.Abs(curr.y - next.y) < tolerance;
                bool seg2Vertical = Mathf.Abs(curr.x - next.x) < tolerance;
                
                // 如果两段都是水平，或者两段都是垂直，说明 curr 是多余的中间点
                if ((seg1Horizontal && seg2Horizontal) || (seg1Vertical && seg2Vertical))
                {
                    continue; // 跳过 curr
                }
                
                // 方向改变了（拐点），保留 curr
                simplified.Add(curr);
            }
            
            simplified.Add(path[path.Count - 1]);
            return simplified;
        }
        
        /// <summary>
        /// 合并共线点（删除中间的冗余点）- 旧版本，保留兼容
        /// </summary>
        private List<Vector2> MergeCollinearPoints(List<Vector2> path, float tolerance = 0.5f)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> merged = new List<Vector2>();
            merged.Add(path[0]);
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 prev = merged[merged.Count - 1];
                Vector2 curr = path[i];
                Vector2 next = path[i + 1];
                
                // 检查三点是否共线（水平或垂直）
                bool prevCurrHorizontal = Mathf.Abs(prev.y - curr.y) < tolerance;
                bool currNextHorizontal = Mathf.Abs(curr.y - next.y) < tolerance;
                bool prevCurrVertical = Mathf.Abs(prev.x - curr.x) < tolerance;
                bool currNextVertical = Mathf.Abs(curr.x - next.x) < tolerance;
                
                // 如果三点在同一水平线或同一垂直线上，跳过中间点
                bool isCollinearHorizontal = prevCurrHorizontal && currNextHorizontal;
                bool isCollinearVertical = prevCurrVertical && currNextVertical;
                
                if (!isCollinearHorizontal && !isCollinearVertical)
                {
                    // 不共线，保留当前点（拐点）
                    merged.Add(curr);
                }
                // 共线则跳过当前点
            }
            
            // 始终添加终点
            merged.Add(path[path.Count - 1]);
            
            return merged;
        }
        
        /// <summary>
        /// 完整的路径清洗流程
        /// </summary>
        private List<Vector2> CleanPath(List<Vector2> path, Vector2 exactStart, Vector2 exactEnd)
        {
            if (path.Count == 0)
                return path;
            
            // 1. 强制起点和终点精确对齐
            path[0] = exactStart;
            path[path.Count - 1] = exactEnd;
            
            // 2. 去除重复点
            path = RemoveDuplicatePoints(path);
            
            // 3. 合并共线点
            path = MergeCollinearPoints(path);
            
            // 4. 再次去重（合并后可能产生新的重复）
            path = RemoveDuplicatePoints(path);
            
            return path;
        }
        
        private List<Vector2Int> AStarSearch(Vector2Int start, Vector2Int goal)
        {
            // 开放列表和关闭列表
            List<PathNode> openList = new List<PathNode>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, PathNode> allNodes = new Dictionary<Vector2Int, PathNode>();
            
            // 创建起点节点
            PathNode startNode = new PathNode(start);
            startNode.GCost = 0;
            startNode.HCost = Heuristic(start, goal);
            openList.Add(startNode);
            allNodes[start] = startNode;
            
            int maxIterations = 10000;
            int iterations = 0;
            
            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                // 找到F值最小的节点
                PathNode current = GetLowestFCostNode(openList);
                
                // 到达目标
                if (current.Position == goal)
                {
                    return ReconstructPath(current);
                }
                
                openList.Remove(current);
                closedSet.Add(current.Position);
                
                // 遍历邻居节点（8方向）
                foreach (Vector2Int neighborPos in GetNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighborPos))
                        continue;
                    
                    // 检查是否可通行
                    if (!IsWalkable(neighborPos))
                        continue;
                    
                    // 计算新的G值
                    float moveCost = Vector2Int.Distance(current.Position, neighborPos);
                    // 添加随机扰动以产生更自然的路径
                    moveCost += (float)_rng.NextDouble() * 0.5f;
                    float newGCost = current.GCost + moveCost;
                    
                    // 获取或创建邻居节点
                    if (!allNodes.TryGetValue(neighborPos, out PathNode neighbor))
                    {
                        neighbor = new PathNode(neighborPos);
                        allNodes[neighborPos] = neighbor;
                    }
                    
                    // 如果找到更短的路径
                    if (newGCost < neighbor.GCost)
                    {
                        neighbor.Parent = current;
                        neighbor.GCost = newGCost;
                        neighbor.HCost = Heuristic(neighborPos, goal);
                        
                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }
            
            // 找不到路径
            return new List<Vector2Int>();
        }
        
        private PathNode GetLowestFCostNode(List<PathNode> nodes)
        {
            PathNode lowest = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].FCost < lowest.FCost ||
                    (nodes[i].FCost == lowest.FCost && nodes[i].HCost < lowest.HCost))
                {
                    lowest = nodes[i];
                }
            }
            return lowest;
        }
        
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            // 使用曼哈顿距离：引导A*优先走直角路径
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        private List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            // 【修改】只使用4方向邻居（上下左右），禁止对角线移动
            // 这是实现曼哈顿风格走廊的关键
            return new List<Vector2Int>
            {
                new Vector2Int(pos.x, pos.y + 1),   // 上
                new Vector2Int(pos.x, pos.y - 1),   // 下
                new Vector2Int(pos.x - 1, pos.y),   // 左
                new Vector2Int(pos.x + 1, pos.y)    // 右
            };
        }
        
        /// <summary>
        /// 检查网格位置是否可通行（使用预烘焙网格，O(1) 查询）
        /// </summary>
        private bool IsWalkable(Vector2Int gridPos)
        {
            // 将寻路网格坐标转换为烘焙网格索引
            int gx = gridPos.x - Mathf.FloorToInt(_gridOrigin.x / _gridResolution);
            int gy = gridPos.y - Mathf.FloorToInt(_gridOrigin.y / _gridResolution);
            
            // 边界检查
            if (gx < 0 || gx >= _bakedGridWidth || gy < 0 || gy >= _bakedGridHeight)
            {
                return false;
            }
            
            return _walkableGrid[gx, gy];
        }
        
        /// <summary>
        /// 检查世界坐标点是否可通行（使用预烘焙网格）
        /// </summary>
        private bool IsWorldPointWalkable(Vector2 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            return IsWalkable(gridPos);
        }
        
        private List<Vector2Int> ReconstructPath(PathNode endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PathNode current = endNode;
            
            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }
            
            path.Reverse();
            return path;
        }
        
        // ==================== 坐标转换 ====================
        
        private Vector2Int WorldToGrid(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / _gridResolution),
                Mathf.RoundToInt(worldPos.y / _gridResolution)
            );
        }
        
        private Vector2 GridToWorld(Vector2Int gridPos)
        {
            return new Vector2(
                gridPos.x * _gridResolution,
                gridPos.y * _gridResolution
            );
        }
        
        // ==================== 路径平滑 ====================
        
        private List<Vector2> SmoothPath(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            List<Vector2> smoothed = new List<Vector2>();
            smoothed.Add(path[0]);
            
            int current = 0;
            while (current < path.Count - 1)
            {
                // 尝试跳过中间点直接连接
                int farthest = current + 1;
                for (int i = path.Count - 1; i > current + 1; i--)
                {
                    if (IsLineWalkable(path[current], path[i]))
                    {
                        farthest = i;
                        break;
                    }
                }
                
                smoothed.Add(path[farthest]);
                current = farthest;
            }
            
            return smoothed;
        }
        
        private bool IsLineWalkable(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / _gridResolution);
            
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                Vector2Int gridPoint = WorldToGrid(point);
                
                if (!IsWalkable(gridPoint))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        // ==================== Catmull-Rom样条插值 ====================
        
        private Vector2 CatmullRomInterpolate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }
}
