using System;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 门位置约束数据
    /// 用于世界生成器向 V4 房间生成器传递门位置要求
    /// </summary>
    [Serializable]
    public struct DoorConstraint
    {
        #region 门存在性标记

        /// <summary>
        /// 是否有北侧门（上方）
        /// </summary>
        public bool HasNorthDoor;

        /// <summary>
        /// 是否有南侧门（下方）
        /// </summary>
        public bool HasSouthDoor;

        /// <summary>
        /// 是否有东侧门（右侧）
        /// </summary>
        public bool HasEastDoor;

        /// <summary>
        /// 是否有西侧门（左侧）
        /// </summary>
        public bool HasWestDoor;

        #endregion

        #region 门精确位置（可选，用于对齐）

        /// <summary>
        /// 北侧门 X 坐标（相对于房间左下角）
        /// -1 表示自动计算（居中）
        /// </summary>
        public int NorthDoorX;

        /// <summary>
        /// 南侧门 X 坐标（相对于房间左下角）
        /// -1 表示自动计算（居中）
        /// </summary>
        public int SouthDoorX;

        /// <summary>
        /// 东侧门 Y 坐标（相对于房间左下角）
        /// -1 表示自动计算（居中）
        /// </summary>
        public int EastDoorY;

        /// <summary>
        /// 西侧门 Y 坐标（相对于房间左下角）
        /// -1 表示自动计算（居中）
        /// </summary>
        public int WestDoorY;

        #endregion

        #region 门尺寸

        /// <summary>
        /// 门宽度（瓦片数）
        /// </summary>
        public int DoorWidth;

        /// <summary>
        /// 门高度（瓦片数）
        /// </summary>
        public int DoorHeight;

        #endregion

        #region 工厂方法

        /// <summary>
        /// 创建默认门约束（无门）
        /// </summary>
        public static DoorConstraint None => new DoorConstraint
        {
            HasNorthDoor = false,
            HasSouthDoor = false,
            HasEastDoor = false,
            HasWestDoor = false,
            NorthDoorX = -1,
            SouthDoorX = -1,
            EastDoorY = -1,
            WestDoorY = -1,
            DoorWidth = 4,
            DoorHeight = 4
        };

        /// <summary>
        /// 根据邻居方向创建门约束
        /// </summary>
        /// <param name="neighborDirections">邻居方向列表</param>
        /// <param name="doorWidth">门宽度</param>
        /// <param name="doorHeight">门高度</param>
        /// <returns>门约束</returns>
        public static DoorConstraint FromNeighbors(
            Vector2Int[] neighborDirections, 
            int doorWidth = 4, 
            int doorHeight = 4)
        {
            var constraint = None;
            constraint.DoorWidth = doorWidth;
            constraint.DoorHeight = doorHeight;

            foreach (var dir in neighborDirections)
            {
                if (dir == Vector2Int.up)
                    constraint.HasNorthDoor = true;
                else if (dir == Vector2Int.down)
                    constraint.HasSouthDoor = true;
                else if (dir == Vector2Int.right)
                    constraint.HasEastDoor = true;
                else if (dir == Vector2Int.left)
                    constraint.HasWestDoor = true;
            }

            return constraint;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取门数量
        /// </summary>
        public int DoorCount
        {
            get
            {
                int count = 0;
                if (HasNorthDoor) count++;
                if (HasSouthDoor) count++;
                if (HasEastDoor) count++;
                if (HasWestDoor) count++;
                return count;
            }
        }

        /// <summary>
        /// 是否有任何门
        /// </summary>
        public bool HasAnyDoor => HasNorthDoor || HasSouthDoor || HasEastDoor || HasWestDoor;

        /// <summary>
        /// 计算指定方向门的实际位置
        /// </summary>
        /// <param name="direction">门方向</param>
        /// <param name="roomWidth">房间宽度</param>
        /// <param name="roomHeight">房间高度</param>
        /// <returns>门的起始坐标（相对于房间左下角）</returns>
        public Vector2Int GetDoorPosition(Vector2Int direction, int roomWidth, int roomHeight)
        {
            if (direction == Vector2Int.up)
            {
                int x = NorthDoorX >= 0 ? NorthDoorX : (roomWidth - DoorWidth) / 2;
                return new Vector2Int(x, roomHeight - 1);
            }
            else if (direction == Vector2Int.down)
            {
                int x = SouthDoorX >= 0 ? SouthDoorX : (roomWidth - DoorWidth) / 2;
                return new Vector2Int(x, 0);
            }
            else if (direction == Vector2Int.right)
            {
                int y = EastDoorY >= 0 ? EastDoorY : (roomHeight - DoorHeight) / 2;
                return new Vector2Int(roomWidth - 1, y);
            }
            else if (direction == Vector2Int.left)
            {
                int y = WestDoorY >= 0 ? WestDoorY : (roomHeight - DoorHeight) / 2;
                return new Vector2Int(0, y);
            }

            return Vector2Int.zero;
        }

        public override string ToString()
        {
            return $"[DoorConstraint N={HasNorthDoor} S={HasSouthDoor} E={HasEastDoor} W={HasWestDoor}]";
        }

        #endregion
    }
}
