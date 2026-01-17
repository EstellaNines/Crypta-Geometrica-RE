using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 基于BFS的出生点/通关点计算器
    /// 寻找距离参考门最远的有效可站立位置
    /// </summary>
    public class SpawnExitPointCalculator
    {
        // 四方向偏移（上下左右）
        private static readonly Vector3Int[] Directions = new Vector3Int[]
        {
            new Vector3Int(0, 1, 0),   // 上
            new Vector3Int(0, -1, 0),  // 下
            new Vector3Int(-1, 0, 0),  // 左
            new Vector3Int(1, 0, 0)    // 右
        };
        
        // 候选点数量
        private const int CandidateCount = 10;
        
        /// <summary>
        /// 计算距离参考点最远的有效可站立位置
        /// </summary>
        /// <param name="referencePoint">参考门位置（出口或入口）</param>
        /// <param name="searchBounds">搜索边界（当前网格的Rect）</param>
        /// <param name="groundLayer">地面层Tilemap</param>
        /// <param name="platformLayer">平台层Tilemap</param>
        /// <returns>最远的有效可站立位置</returns>
        public Vector3 CalculateFarthestPoint(
            Vector3 referencePoint,
            Rect searchBounds,
            Tilemap groundLayer,
            Tilemap platformLayer)
        {
            // 将参考点转换为整数坐标
            Vector3Int startPos = new Vector3Int(
                Mathf.RoundToInt(referencePoint.x),
                Mathf.RoundToInt(referencePoint.y),
                0
            );
            
            // 构建距离场
            Dictionary<Vector3Int, int> distanceMap = BuildDistanceMap(startPos, searchBounds, groundLayer);
            
            if (distanceMap.Count == 0)
            {
                Debug.LogWarning($"BFS未找到任何有效位置，使用降级方案（边界中心）");
                return GetBoundsCenterFallback(searchBounds);
            }
            
            // 按距离降序排序，获取候选点
            var candidates = distanceMap
                .OrderByDescending(kvp => kvp.Value)
                .Take(CandidateCount)
                .Select(kvp => kvp.Key)
                .ToList();
            
            // 物理环境验证，找到第一个有效的可站立位置
            foreach (var candidate in candidates)
            {
                if (IsStandablePosition(candidate, groundLayer, platformLayer))
                {
                    return new Vector3(candidate.x + 0.5f, candidate.y + 0.5f, 0f);
                }
            }
            
            // 如果所有候选都不可站立，尝试在所有距离点中寻找
            var allStandable = distanceMap.Keys
                .Where(pos => IsStandablePosition(pos, groundLayer, platformLayer))
                .OrderByDescending(pos => distanceMap[pos])
                .FirstOrDefault();
            
            if (allStandable != default)
            {
                return new Vector3(allStandable.x + 0.5f, allStandable.y + 0.5f, 0f);
            }
            
            Debug.LogWarning($"未找到可站立位置，使用降级方案（边界中心）");
            return GetBoundsCenterFallback(searchBounds);
        }
        
        /// <summary>
        /// 使用BFS构建距离场
        /// </summary>
        private Dictionary<Vector3Int, int> BuildDistanceMap(
            Vector3Int startPos,
            Rect bounds,
            Tilemap groundLayer)
        {
            var distanceMap = new Dictionary<Vector3Int, int>();
            var queue = new Queue<Vector3Int>();
            
            // 初始化：将起点加入队列
            // 注意：起点可能在墙壁上（门的位置），需要找到附近的空气位置作为起点
            Vector3Int actualStart = FindNearestAirPosition(startPos, bounds, groundLayer);
            if (actualStart == default)
            {
                // 如果找不到空气位置，直接从起点开始
                actualStart = startPos;
            }
            
            queue.Enqueue(actualStart);
            distanceMap[actualStart] = 0;
            
            // BFS洪水填充
            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                int currentDistance = distanceMap[current];
                
                // 检查四个方向
                foreach (var dir in Directions)
                {
                    Vector3Int neighbor = current + dir;
                    
                    // 验证邻居是否有效
                    if (IsValidTile(neighbor, bounds, groundLayer, distanceMap))
                    {
                        distanceMap[neighbor] = currentDistance + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return distanceMap;
        }
        
        /// <summary>
        /// 在指定位置附近寻找最近的空气位置
        /// </summary>
        private Vector3Int FindNearestAirPosition(Vector3Int center, Rect bounds, Tilemap groundLayer)
        {
            // 搜索半径
            const int searchRadius = 5;
            
            for (int r = 0; r <= searchRadius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // 只检查边缘
                        
                        Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);
                        
                        if (IsInBounds(pos, bounds) && groundLayer.GetTile(pos) == null)
                        {
                            return pos;
                        }
                    }
                }
            }
            
            return default;
        }
        
        /// <summary>
        /// 检查瓦片是否有效（可通过的空气）
        /// </summary>
        private bool IsValidTile(
            Vector3Int pos,
            Rect bounds,
            Tilemap groundLayer,
            Dictionary<Vector3Int, int> distanceMap)
        {
            // 1. 边界检查
            if (!IsInBounds(pos, bounds))
                return false;
            
            // 2. 已访问检查
            if (distanceMap.ContainsKey(pos))
                return false;
            
            // 3. 墙壁检查 - GroundLayer有瓦片表示是墙壁/地面，不可通过
            if (groundLayer.GetTile(pos) != null)
                return false;
            
            return true; // 是空气，可通过
        }
        
        /// <summary>
        /// 检查位置是否在边界内
        /// </summary>
        private bool IsInBounds(Vector3Int pos, Rect bounds)
        {
            return pos.x >= bounds.xMin && pos.x < bounds.xMax &&
                   pos.y >= bounds.yMin && pos.y < bounds.yMax;
        }
        
        // 安全区尺寸常量
        private const int SafetyBoxWidth = 3;   // 安全区宽度（左1 + 中 + 右1）
        private const int SafetyBoxHeight = 4;  // 安全区高度（玩家位置 + 跳跃空间）
        
        /// <summary>
        /// 检查位置是否是有效的可站立位置（增强版：包含跳跃净空和横向活动域检测）
        /// </summary>
        private bool IsStandablePosition(Vector3Int pos, Tilemap groundLayer, Tilemap platformLayer)
        {
            // 1. 脚下检测 - (x, y-1) 必须有地面或平台
            Vector3Int below = new Vector3Int(pos.x, pos.y - 1, 0);
            bool hasFloor = groundLayer.GetTile(below) != null || 
                           (platformLayer != null && platformLayer.GetTile(below) != null);
            if (!hasFloor) return false;
            
            // 2. 当前位置检测 - 必须为空（不能在墙里）
            if (groundLayer.GetTile(pos) != null) return false;
            
            // 3. 跳跃净空检测 - 头顶 3 格必须无墙壁和平台
            for (int dy = 1; dy <= 3; dy++)
            {
                Vector3Int checkPos = new Vector3Int(pos.x, pos.y + dy, 0);
                
                // 检查墙壁
                if (groundLayer.GetTile(checkPos) != null)
                    return false;
                
                // 检查平台
                if (platformLayer != null && platformLayer.GetTile(checkPos) != null)
                    return false;
            }
            
            // 4. 横向活动域检测 - 不能左右都被堵死
            Vector3Int left = new Vector3Int(pos.x - 1, pos.y, 0);
            Vector3Int right = new Vector3Int(pos.x + 1, pos.y, 0);
            bool leftClear = groundLayer.GetTile(left) == null;
            bool rightClear = groundLayer.GetTile(right) == null;
            
            // 至少一侧有空间
            if (!leftClear && !rightClear)
                return false;
            
            // 5. 检查是否处于极窄的垂直井中（左右上方都被堵）
            if (!leftClear || !rightClear)
            {
                // 如果一侧被堵，检查上方是否也被堵（形成垂直井）
                Vector3Int leftAbove = new Vector3Int(pos.x - 1, pos.y + 1, 0);
                Vector3Int rightAbove = new Vector3Int(pos.x + 1, pos.y + 1, 0);
                bool leftAboveClear = groundLayer.GetTile(leftAbove) == null;
                bool rightAboveClear = groundLayer.GetTile(rightAbove) == null;
                
                // 如果形成狭窄的垂直通道，拒绝
                if ((!leftClear && !rightAboveClear) || (!rightClear && !leftAboveClear))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 降级方案：返回边界中心位置
        /// </summary>
        private Vector3 GetBoundsCenterFallback(Rect bounds)
        {
            return new Vector3(bounds.center.x, bounds.center.y, 0f);
        }
    }
}
