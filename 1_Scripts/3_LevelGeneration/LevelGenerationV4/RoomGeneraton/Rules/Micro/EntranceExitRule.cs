using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 入口/出口拆墙规则
    /// 确保起点和终点房间的入口/出口位置是可通行的
    /// </summary>
    [Serializable]
    public class EntranceExitRule : GeneratorRuleBase
    {
        #region 参数

        [TitleGroup("拆墙参数")]
        [LabelText("挖掘半径")]
        [Tooltip("入口/出口周围挖掘的半径（格）")]
        [Range(2, 8)]
        [SerializeField]
        private int _carveRadius = 4;

        [TitleGroup("拆墙参数")]
        [LabelText("确保底部平台")]
        [Tooltip("在入口/出口下方保留实心地面供玩家站立")]
        [SerializeField]
        private bool _ensureFloor = true;

        [TitleGroup("拆墙参数")]
        [LabelText("地面厚度")]
        [ShowIf("_ensureFloor")]
        [Range(1, 3)]
        [SerializeField]
        private int _floorThickness = 2;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public EntranceExitRule()
        {
            _ruleName = "EntranceExitRule";
            _executionOrder = 35; // 在CellularAutomataRule之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始入口/出口拆墙处理...");

            if (context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogWarning("房间节点列表为空");
                return true;
            }

            // 找到起点和终点房间
            RoomNode startRoom = null;
            RoomNode endRoom = null;

            foreach (var room in context.RoomNodes)
            {
                if (room.Type == RoomType.Start)
                    startRoom = room;
                else if (room.Type == RoomType.End)
                    endRoom = room;
            }

            // 处理起点房间
            if (startRoom != null)
            {
                LogInfo($"处理起点房间: {startRoom.GridPosition}");
                CarveEntrance(context, startRoom);
            }
            else
            {
                LogWarning("未找到起点房间");
            }

            // 处理终点房间
            if (endRoom != null)
            {
                LogInfo($"处理终点房间: {endRoom.GridPosition}");
                CarveExit(context, endRoom);
            }
            else
            {
                LogWarning("未找到终点房间");
            }

            await UniTask.Yield(token);

            LogInfo("入口/出口拆墙完成");
            return true;
        }

        /// <summary>
        /// 挖掘入口区域（位置与Gizmos黄球一致）
        /// </summary>
        private void CarveEntrance(DungeonContext context, RoomNode room)
        {
            Vector2Int pos = CalculateDoorPosition(room);
            LogInfo($"入口挖掘位置: ({pos.x}, {pos.y}), 门方向: {room.RestrictedDoorSide}");
            CarveCircle(context, pos.x, pos.y);
        }

        /// <summary>
        /// 挖掘出口区域（位置与Gizmos黄球一致）
        /// </summary>
        private void CarveExit(DungeonContext context, RoomNode room)
        {
            Vector2Int pos = CalculateDoorPosition(room);
            LogInfo($"出口挖掘位置: ({pos.x}, {pos.y}), 门方向: {room.RestrictedDoorSide}");
            CarveCircle(context, pos.x, pos.y);
        }

        /// <summary>
        /// 计算门位置（与Gizmos黄球位置一致）
        /// doorPos = roomCenter + (±0.45 * roomSize.x, 0)
        /// </summary>
        private Vector2Int CalculateDoorPosition(RoomNode room)
        {
            BoundsInt bounds = room.WorldBounds;
            
            // 房间中心
            int centerX = bounds.xMin + bounds.size.x / 2;
            int centerY = bounds.yMin + bounds.size.y / 2;
            
            // 根据门方向计算X偏移（与Gizmos一致：±0.45 * roomSize.x）
            int xOffset = (int)(bounds.size.x * 0.45f);
            
            int x;
            switch (room.RestrictedDoorSide)
            {
                case WallDirection.Left:
                    x = centerX - xOffset;
                    break;
                case WallDirection.Right:
                    x = centerX + xOffset;
                    break;
                default:
                    x = centerX;
                    break;
            }

            return new Vector2Int(x, centerY);
        }

        /// <summary>
        /// 挖掘圆形区域（带可选底部地面）
        /// </summary>
        private void CarveCircle(DungeonContext context, int centerX, int centerY)
        {
            int radiusSquared = _carveRadius * _carveRadius;

            for (int dy = -_carveRadius; dy <= _carveRadius; dy++)
            {
                for (int dx = -_carveRadius; dx <= _carveRadius; dx++)
                {
                    // 圆形判定
                    if (dx * dx + dy * dy > radiusSquared)
                        continue;

                    int x = centerX + dx;
                    int y = centerY + dy;

                    // 如果需要保留底部地面
                    if (_ensureFloor && dy < -_carveRadius + _floorThickness)
                    {
                        // 保持实心（不挖掘）
                        continue;
                    }

                    // 挖掘（设为空）
                    context.SetTile(TilemapLayer.Ground, x, y, 0);
                }
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_carveRadius < 2)
            {
                errorMessage = "挖掘半径太小";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
