using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 灰盒4×4网格预览器
    /// 在场景中预览完整的关卡布局
    /// </summary>
    public class GrayboxGridPreview : MonoBehaviour
    {
        [Header("组件引用")]
        public GrayboxRoomTemplates RoomTemplates;
        public Tilemap TargetTilemap;
        
        [Header("网格设置")]
        [Tooltip("使用的关卡形状预设")]
        public LevelShapePresetType ShapePreset = LevelShapePresetType.FullSquare;
        
        [Tooltip("自定义形状字符串 (仅当ShapePreset为Custom时使用)")]
        public string CustomShapePattern = "1111,1111,1111,1111";
        
        [Header("房间间距")]
        [Tooltip("房间之间的间距（瓦片数）")]
        [Range(0, 8)]
        public int RoomSpacing = 2;
        
        [Header("调试显示")]
        [Tooltip("显示网格坐标")]
        public bool ShowGridCoordinates = true;
        
        [Tooltip("显示关键路径")]
        public bool ShowCriticalPath = true;
        
        private LevelShape _currentShape;
        private RoomNode[,] _roomGrid;
        private List<Vector2Int> _criticalPath = new List<Vector2Int>();
        
        /// <summary>
        /// 关卡形状预设类型
        /// </summary>
        public enum LevelShapePresetType
        {
            FullSquare,
            LShape,
            TShape,
            CrossShape,
            ZShape,
            VerticalStrip,
            HorizontalStrip,
            DiagonalShape,
            UserExample,
            Custom
        }
        
        /// <summary>
        /// 生成完整的4×4网格预览
        /// </summary>
        [ContextMenu("生成网格预览")]
        public void GenerateGridPreview()
        {
            if (RoomTemplates == null)
            {
                Debug.LogError("GrayboxGridPreview: RoomTemplates未设置!");
                return;
            }
            
            // 清除现有瓦片
            RoomTemplates.ClearTilemap();
            
            // 获取形状
            _currentShape = GetCurrentShape();
            
            // 初始化房间网格
            InitializeRoomGrid();
            
            // 生成简单的关键路径（用于演示）
            GenerateSimpleCriticalPath();
            
            // 生成每个房间
            GenerateAllRooms();
            
            Debug.Log($"网格预览生成完成! 有效格子数: {_currentShape.GetValidCellCount()}");
        }
        
        /// <summary>
        /// 清除预览
        /// </summary>
        [ContextMenu("清除预览")]
        public void ClearPreview()
        {
            if (RoomTemplates != null)
            {
                RoomTemplates.ClearTilemap();
            }
            _roomGrid = null;
            _criticalPath.Clear();
        }
        
        /// <summary>
        /// 获取当前选择的形状
        /// </summary>
        private LevelShape GetCurrentShape()
        {
            return ShapePreset switch
            {
                LevelShapePresetType.FullSquare => LevelShapePresets.FullSquare,
                LevelShapePresetType.LShape => LevelShapePresets.LShape,
                LevelShapePresetType.TShape => LevelShapePresets.TShape,
                LevelShapePresetType.CrossShape => LevelShapePresets.CrossShape,
                LevelShapePresetType.ZShape => LevelShapePresets.ZShape,
                LevelShapePresetType.VerticalStrip => LevelShapePresets.VerticalStrip,
                LevelShapePresetType.HorizontalStrip => LevelShapePresets.HorizontalStrip,
                LevelShapePresetType.DiagonalShape => LevelShapePresets.DiagonalShape,
                LevelShapePresetType.UserExample => LevelShapePresets.UserExample,
                LevelShapePresetType.Custom => LevelShape.FromString(CustomShapePattern),
                _ => LevelShapePresets.FullSquare
            };
        }
        
        /// <summary>
        /// 初始化房间网格
        /// </summary>
        private void InitializeRoomGrid()
        {
            _roomGrid = new RoomNode[LevelShape.GridWidth, LevelShape.GridHeight];
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    _roomGrid[x, y] = new RoomNode(x, y);
                    
                    if (_currentShape.IsValidCell(x, y))
                    {
                        _roomGrid[x, y].Type = RoomType.Side; // 默认为侧室
                    }
                }
            }
        }
        
        /// <summary>
        /// 生成简单的关键路径（用于演示）
        /// </summary>
        private void GenerateSimpleCriticalPath()
        {
            _criticalPath.Clear();
            
            var topCells = _currentShape.GetTopRowCells();
            var bottomCells = _currentShape.GetBottomRowCells();
            
            if (topCells.Count == 0 || bottomCells.Count == 0) return;
            
            // 选择起点
            var start = topCells[topCells.Count / 2];
            _roomGrid[start.x, start.y].Type = RoomType.Start;
            _roomGrid[start.x, start.y].IsCriticalPath = true;
            _criticalPath.Add(start);
            
            // 简单的向下路径
            Vector2Int current = start;
            int maxIterations = 20;
            int iterations = 0;
            
            while (current.y < LevelShape.GridHeight - 1 && iterations++ < maxIterations)
            {
                // 尝试向下
                Vector2Int next = current + Vector2Int.up; // Unity中Y向上
                
                // 在我们的网格中，y=0是顶部，y=3是底部
                next = new Vector2Int(current.x, current.y + 1);
                
                if (_currentShape.IsValidCell(next))
                {
                    // 添加连接
                    _roomGrid[current.x, current.y].AddConnection(Direction.South);
                    _roomGrid[next.x, next.y].AddConnection(Direction.North);
                    
                    current = next;
                    _roomGrid[current.x, current.y].Type = RoomType.LR;
                    _roomGrid[current.x, current.y].IsCriticalPath = true;
                    _criticalPath.Add(current);
                }
                else
                {
                    // 尝试水平移动
                    bool moved = false;
                    foreach (int dx in new[] { 1, -1 })
                    {
                        Vector2Int side = new Vector2Int(current.x + dx, current.y);
                        if (_currentShape.IsValidCell(side) && !_roomGrid[side.x, side.y].IsCriticalPath)
                        {
                            Direction dir = dx > 0 ? Direction.East : Direction.West;
                            _roomGrid[current.x, current.y].AddConnection(dir);
                            _roomGrid[side.x, side.y].AddConnection(dir.Opposite());
                            
                            current = side;
                            _roomGrid[current.x, current.y].Type = RoomType.LR;
                            _roomGrid[current.x, current.y].IsCriticalPath = true;
                            _criticalPath.Add(current);
                            moved = true;
                            break;
                        }
                    }
                    
                    if (!moved) break;
                }
            }
            
            // 标记终点
            if (_criticalPath.Count > 1)
            {
                var end = _criticalPath[_criticalPath.Count - 1];
                
                // 在终点前插入Boss房间
                if (_criticalPath.Count > 2)
                {
                    var boss = _criticalPath[_criticalPath.Count - 2];
                    _roomGrid[boss.x, boss.y].Type = RoomType.Boss;
                }
                
                _roomGrid[end.x, end.y].Type = RoomType.Exit;
            }
        }
        
        /// <summary>
        /// 生成所有房间
        /// </summary>
        private void GenerateAllRooms()
        {
            int roomWidth = RoomTemplates.RoomWidth;
            int roomHeight = RoomTemplates.RoomHeight;
            int totalWidth = roomWidth + RoomSpacing;
            int totalHeight = roomHeight + RoomSpacing;
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    if (_currentShape.IsValidCell(x, y))
                    {
                        // 计算房间原点（Y轴翻转，使y=0在顶部）
                        int worldX = x * totalWidth;
                        int worldY = (LevelShape.GridHeight - 1 - y) * totalHeight;
                        
                        Vector2Int origin = new Vector2Int(worldX, worldY);
                        RoomType roomType = _roomGrid[x, y].Type;
                        
                        RoomTemplates.GenerateRoomTemplate(roomType, origin);
                    }
                }
            }
        }
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_roomGrid == null || _currentShape == null) return;
            
            int roomWidth = RoomTemplates != null ? RoomTemplates.RoomWidth : 32;
            int roomHeight = RoomTemplates != null ? RoomTemplates.RoomHeight : 32;
            int totalWidth = roomWidth + RoomSpacing;
            int totalHeight = roomHeight + RoomSpacing;
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    if (_currentShape.IsValidCell(x, y))
                    {
                        int worldX = x * totalWidth;
                        int worldY = (LevelShape.GridHeight - 1 - y) * totalHeight;
                        
                        Vector3 center = new Vector3(worldX + roomWidth / 2f, worldY + roomHeight / 2f, 0);
                        
                        // 绘制房间边界
                        if (_roomGrid[x, y].IsCriticalPath && ShowCriticalPath)
                        {
                            Gizmos.color = Color.yellow;
                        }
                        else
                        {
                            Gizmos.color = Color.white;
                        }
                        
                        Gizmos.DrawWireCube(center, new Vector3(roomWidth, roomHeight, 0));
                        
#if UNITY_EDITOR
                        if (ShowGridCoordinates)
                        {
                            UnityEditor.Handles.Label(center + Vector3.up * (roomHeight / 2f + 2), 
                                $"[{x},{y}] {_roomGrid[x, y].Type}");
                        }
#endif
                    }
                }
            }
            
            // 绘制关键路径连线
            if (ShowCriticalPath && _criticalPath.Count > 1)
            {
                Gizmos.color = Color.green;
                
                for (int i = 0; i < _criticalPath.Count - 1; i++)
                {
                    var from = _criticalPath[i];
                    var to = _criticalPath[i + 1];
                    
                    int fromWorldX = from.x * totalWidth + roomWidth / 2;
                    int fromWorldY = (LevelShape.GridHeight - 1 - from.y) * totalHeight + roomHeight / 2;
                    int toWorldX = to.x * totalWidth + roomWidth / 2;
                    int toWorldY = (LevelShape.GridHeight - 1 - to.y) * totalHeight + roomHeight / 2;
                    
                    Gizmos.DrawLine(
                        new Vector3(fromWorldX, fromWorldY, 0),
                        new Vector3(toWorldX, toWorldY, 0)
                    );
                }
            }
        }
    }
}
