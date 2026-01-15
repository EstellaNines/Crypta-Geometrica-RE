using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration
{
    /// <summary>
    /// 关卡形状定义 - 使用4×4位掩码表示占位情况
    /// 支持不规则形状（如L形、T形、十字形等）
    /// </summary>
    [System.Serializable]
    public class LevelShape
    {
        /// <summary>
        /// 4×4占位掩码矩阵
        /// 1=可用格子, 0=不可用格子
        /// </summary>
        public int[,] OccupancyMask = new int[4, 4];
        
        /// <summary>
        /// 网格宽度 (固定为4)
        /// </summary>
        public const int GridWidth = 4;
        
        /// <summary>
        /// 网格高度 (固定为4)
        /// </summary>
        public const int GridHeight = 4;
        
        /// <summary>
        /// 默认构造函数 - 创建全空形状
        /// </summary>
        public LevelShape()
        {
            OccupancyMask = new int[GridWidth, GridHeight];
        }
        
        /// <summary>
        /// 从字符串模式初始化
        /// 格式: "0010,1111,0111,0000" (逗号分隔行，从上到下)
        /// </summary>
        /// <param name="pattern">字符串模式</param>
        /// <returns>LevelShape实例</returns>
        public static LevelShape FromString(string pattern)
        {
            var shape = new LevelShape();
            var rows = pattern.Replace(" ", "").Split(',');
            
            for (int y = 0; y < GridHeight && y < rows.Length; y++)
            {
                for (int x = 0; x < GridWidth && x < rows[y].Length; x++)
                {
                    shape.OccupancyMask[x, y] = rows[y][x] == '1' ? 1 : 0;
                }
            }
            
            return shape;
        }
        
        /// <summary>
        /// 从二维数组初始化
        /// </summary>
        /// <param name="mask">4×4整数数组</param>
        /// <returns>LevelShape实例</returns>
        public static LevelShape FromArray(int[,] mask)
        {
            var shape = new LevelShape();
            
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (x < mask.GetLength(0) && y < mask.GetLength(1))
                    {
                        shape.OccupancyMask[x, y] = mask[x, y] > 0 ? 1 : 0;
                    }
                }
            }
            
            return shape;
        }
        
        /// <summary>
        /// 获取所有有效格子坐标
        /// </summary>
        /// <returns>有效格子坐标列表</returns>
        public List<Vector2Int> GetValidCells()
        {
            var cells = new List<Vector2Int>();
            
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (OccupancyMask[x, y] == 1)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// 检查格子是否有效
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否有效</returns>
        public bool IsValidCell(int x, int y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
            {
                return false;
            }
            return OccupancyMask[x, y] == 1;
        }
        
        /// <summary>
        /// 检查格子是否有效 (Vector2Int版本)
        /// </summary>
        /// <param name="coord">坐标</param>
        /// <returns>是否有效</returns>
        public bool IsValidCell(Vector2Int coord)
        {
            return IsValidCell(coord.x, coord.y);
        }
        
        /// <summary>
        /// 设置格子状态
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="valid">是否有效</param>
        public void SetCell(int x, int y, bool valid)
        {
            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
            {
                OccupancyMask[x, y] = valid ? 1 : 0;
            }
        }
        
        /// <summary>
        /// 获取有效格子数量
        /// </summary>
        /// <returns>有效格子数量</returns>
        public int GetValidCellCount()
        {
            int count = 0;
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (OccupancyMask[x, y] == 1)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        
        /// <summary>
        /// 获取指定格子的相邻有效格子
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>相邻有效格子列表</returns>
        public List<Vector2Int> GetValidNeighbors(int x, int y)
        {
            var neighbors = new List<Vector2Int>();
            
            // 北
            if (IsValidCell(x, y - 1)) neighbors.Add(new Vector2Int(x, y - 1));
            // 东
            if (IsValidCell(x + 1, y)) neighbors.Add(new Vector2Int(x + 1, y));
            // 南
            if (IsValidCell(x, y + 1)) neighbors.Add(new Vector2Int(x, y + 1));
            // 西
            if (IsValidCell(x - 1, y)) neighbors.Add(new Vector2Int(x - 1, y));
            
            return neighbors;
        }
        
        /// <summary>
        /// 获取顶部行的有效格子
        /// </summary>
        /// <returns>顶部行有效格子列表</returns>
        public List<Vector2Int> GetTopRowCells()
        {
            var cells = new List<Vector2Int>();
            
            // 从上到下查找第一个有有效格子的行
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (OccupancyMask[x, y] == 1)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
                if (cells.Count > 0) break;
            }
            
            return cells;
        }
        
        /// <summary>
        /// 获取底部行的有效格子
        /// </summary>
        /// <returns>底部行有效格子列表</returns>
        public List<Vector2Int> GetBottomRowCells()
        {
            var cells = new List<Vector2Int>();
            
            // 从下到上查找第一个有有效格子的行
            for (int y = GridHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (OccupancyMask[x, y] == 1)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
                if (cells.Count > 0) break;
            }
            
            return cells;
        }
        
        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            var rows = new string[GridHeight];
            
            for (int y = 0; y < GridHeight; y++)
            {
                var row = "";
                for (int x = 0; x < GridWidth; x++)
                {
                    row += OccupancyMask[x, y] == 1 ? "1" : "0";
                }
                rows[y] = row;
            }
            
            return string.Join(",", rows);
        }
        
        /// <summary>
        /// 可视化输出 (用于调试)
        /// </summary>
        /// <returns>可视化字符串</returns>
        public string ToVisualString()
        {
            var result = "";
            
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    result += OccupancyMask[x, y] == 1 ? "■ " : "□ ";
                }
                result += "\n";
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// 预定义的关卡形状库
    /// </summary>
    public static class LevelShapePresets
    {
        /// <summary>
        /// 完整4×4方形
        /// ■ ■ ■ ■
        /// ■ ■ ■ ■
        /// ■ ■ ■ ■
        /// ■ ■ ■ ■
        /// </summary>
        public static LevelShape FullSquare => LevelShape.FromString(
            "1111," +
            "1111," +
            "1111," +
            "1111"
        );
        
        /// <summary>
        /// L形
        /// ■ □ □ □
        /// ■ □ □ □
        /// ■ ■ ■ ■
        /// ■ ■ ■ ■
        /// </summary>
        public static LevelShape LShape => LevelShape.FromString(
            "1000," +
            "1000," +
            "1111," +
            "1111"
        );
        
        /// <summary>
        /// T形
        /// ■ ■ ■ ■
        /// □ ■ ■ □
        /// □ ■ ■ □
        /// □ ■ ■ □
        /// </summary>
        public static LevelShape TShape => LevelShape.FromString(
            "1111," +
            "0110," +
            "0110," +
            "0110"
        );
        
        /// <summary>
        /// 十字形
        /// □ ■ ■ □
        /// ■ ■ ■ ■
        /// ■ ■ ■ ■
        /// □ ■ ■ □
        /// </summary>
        public static LevelShape CrossShape => LevelShape.FromString(
            "0110," +
            "1111," +
            "1111," +
            "0110"
        );
        
        /// <summary>
        /// Z形
        /// ■ ■ ■ □
        /// □ ■ ■ □
        /// □ ■ ■ □
        /// □ ■ ■ ■
        /// </summary>
        public static LevelShape ZShape => LevelShape.FromString(
            "1110," +
            "0110," +
            "0110," +
            "0111"
        );
        
        /// <summary>
        /// 用户示例形状
        /// □ □ ■ □
        /// ■ ■ ■ ■
        /// □ ■ ■ ■
        /// □ □ □ □
        /// </summary>
        public static LevelShape UserExample => LevelShape.FromString(
            "0010," +
            "1111," +
            "0111," +
            "0000"
        );
        
        /// <summary>
        /// 竖条形
        /// □ ■ ■ □
        /// □ ■ ■ □
        /// □ ■ ■ □
        /// □ ■ ■ □
        /// </summary>
        public static LevelShape VerticalStrip => LevelShape.FromString(
            "0110," +
            "0110," +
            "0110," +
            "0110"
        );
        
        /// <summary>
        /// 横条形
        /// □ □ □ □
        /// ■ ■ ■ ■
        /// ■ ■ ■ ■
        /// □ □ □ □
        /// </summary>
        public static LevelShape HorizontalStrip => LevelShape.FromString(
            "0000," +
            "1111," +
            "1111," +
            "0000"
        );
        
        /// <summary>
        /// 对角形
        /// ■ ■ □ □
        /// ■ ■ ■ □
        /// □ ■ ■ ■
        /// □ □ ■ ■
        /// </summary>
        public static LevelShape DiagonalShape => LevelShape.FromString(
            "1100," +
            "1110," +
            "0111," +
            "0011"
        );
    }
}
