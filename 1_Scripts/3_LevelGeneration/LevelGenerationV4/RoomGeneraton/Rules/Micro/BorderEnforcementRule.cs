using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 边界强制规则
    /// 确保每个房间的边缘在 GroundTileData 中为实心（门位置除外）
    /// 这是数据驱动架构的一部分，不直接操作 Tilemap
    /// </summary>
    [Serializable]
    public class BorderEnforcementRule : GeneratorRuleBase
    {
        #region 边界设置

        [TitleGroup("边界设置")]
        [LabelText("边界厚度")]
        [Tooltip("房间边缘强制为实心的厚度")]
        [Range(1, 5)]
        [SerializeField]
        private int _borderThickness = 2;

        [TitleGroup("边界设置")]
        [LabelText("门通道宽度")]
        [Tooltip("门位置保持空的宽度（半径）")]
        [Range(1, 4)]
        [SerializeField]
        private int _doorHalfWidth = 2;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public BorderEnforcementRule()
        {
            _ruleName = "BorderEnforcementRule";
            _executionOrder = 38; // 在 CellularAutomataRule(30) 和 EntranceExitRule(35) 之后，PathValidationRule(36) 之后
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始边界强制处理...");

            if (context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogWarning("房间节点列表为空，跳过边界强制");
                return true;
            }

            if (context.GroundTileData == null)
            {
                LogError("GroundTileData未初始化");
                return false;
            }

            // 构建房间位置查找表
            HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
            foreach (var room in context.RoomNodes)
            {
                roomPositions.Add(room.GridPosition);
            }

            int processedRooms = 0;

            foreach (var room in context.RoomNodes)
            {
                if (token.IsCancellationRequested)
                {
                    LogWarning("边界强制被取消");
                    return false;
                }

                EnforceBorder(context, room, roomPositions);
                processedRooms++;

                if (processedRooms % 8 == 0)
                {
                    await UniTask.Yield(token);
                }
            }

            LogInfo($"边界强制完成: 处理了 {processedRooms} 个房间");
            return true;
        }

        /// <summary>
        /// 强制单个房间的边界为实心（门位置除外）
        /// </summary>
        private void EnforceBorder(DungeonContext context, RoomNode room, HashSet<Vector2Int> roomPositions)
        {
            BoundsInt bounds = room.WorldBounds;
            int width = bounds.size.x;
            int height = bounds.size.y;
            int startX = bounds.xMin;
            int startY = bounds.yMin;

            int mapWidth = context.MapWidth;
            Vector2Int gridPos = room.GridPosition;

            // 计算门位置（包括房间间的门和起点/终点的外部入口/出口）
            HashSet<Vector2Int> doorPositions = CalculateDoorPositions(gridPos, width, height, roomPositions, room);

            // 强制边界为实心
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 检查是否在边界区域
                    bool isBorder = x < _borderThickness || x >= width - _borderThickness ||
                                    y < _borderThickness || y >= height - _borderThickness;

                    if (!isBorder)
                        continue;

                    // 检查是否是门位置
                    Vector2Int localPos = new Vector2Int(x, y);
                    if (doorPositions.Contains(localPos))
                        continue;

                    // 强制为实心
                    int worldX = startX + x;
                    int worldY = startY + y;

                    if (worldX >= 0 && worldX < mapWidth && worldY >= 0 && worldY < context.MapHeight)
                    {
                        int index = worldY * mapWidth + worldX;
                        context.GroundTileData[index] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 计算门位置（本地坐标），包括房间间的门和起点/终点的外部入口/出口
        /// </summary>
        private HashSet<Vector2Int> CalculateDoorPositions(Vector2Int gridPos, int width, int height, HashSet<Vector2Int> roomPositions, RoomNode room)
        {
            HashSet<Vector2Int> doors = new HashSet<Vector2Int>();

            int centerX = width / 2;
            int centerY = height / 2;

            // 右邻居 -> 右边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(1, 0)))
            {
                AddDoorPositions(doors, width, height, WallDirection.Right, centerX, centerY);
            }

            // 左邻居 -> 左边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(-1, 0)))
            {
                AddDoorPositions(doors, width, height, WallDirection.Left, centerX, centerY);
            }

            // 上邻居 -> 上边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(0, 1)))
            {
                AddDoorPositions(doors, width, height, WallDirection.Top, centerX, centerY);
            }

            // 下邻居 -> 下边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(0, -1)))
            {
                AddDoorPositions(doors, width, height, WallDirection.Bottom, centerX, centerY);
            }

            // 【关键修复】起点/终点房间的外部入口/出口
            if (room.Type == RoomType.Start || room.Type == RoomType.End)
            {
                // 根据 RestrictedDoorSide 保护外部入口/出口
                AddDoorPositions(doors, width, height, room.RestrictedDoorSide, centerX, centerY);
            }

            return doors;
        }

        /// <summary>
        /// 在指定方向添加门位置
        /// </summary>
        private void AddDoorPositions(HashSet<Vector2Int> doors, int width, int height, WallDirection direction, int centerX, int centerY)
        {
            switch (direction)
            {
                case WallDirection.Right:
                    for (int dy = -_doorHalfWidth; dy <= _doorHalfWidth; dy++)
                    {
                        for (int dx = 0; dx < _borderThickness + 1; dx++)
                        {
                            int posX = width - 1 - dx;
                            int posY = centerY + dy;
                            if (posY >= 0 && posY < height)
                                doors.Add(new Vector2Int(posX, posY));
                        }
                    }
                    break;

                case WallDirection.Left:
                    for (int dy = -_doorHalfWidth; dy <= _doorHalfWidth; dy++)
                    {
                        for (int dx = 0; dx < _borderThickness + 1; dx++)
                        {
                            int posY = centerY + dy;
                            if (posY >= 0 && posY < height)
                                doors.Add(new Vector2Int(dx, posY));
                        }
                    }
                    break;

                case WallDirection.Top:
                    for (int dx = -_doorHalfWidth; dx <= _doorHalfWidth; dx++)
                    {
                        for (int dy = 0; dy < _borderThickness + 1; dy++)
                        {
                            int posX = centerX + dx;
                            if (posX >= 0 && posX < width)
                                doors.Add(new Vector2Int(posX, height - 1 - dy));
                        }
                    }
                    break;

                case WallDirection.Bottom:
                    for (int dx = -_doorHalfWidth; dx <= _doorHalfWidth; dx++)
                    {
                        for (int dy = 0; dy < _borderThickness + 1; dy++)
                        {
                            int posX = centerX + dx;
                            if (posX >= 0 && posX < width)
                                doors.Add(new Vector2Int(posX, dy));
                        }
                    }
                    break;
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_borderThickness < 1)
            {
                errorMessage = "边界厚度必须至少为1";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
