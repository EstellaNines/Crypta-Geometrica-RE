using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 门户锚点数据 - 记录门的精确位置和朝向
    /// 用于确保走廊端点与门瓦片精确对齐
    /// </summary>
    [System.Serializable]
    public struct PortalAnchor
    {
        /// <summary>
        /// 锚点：门瓦片中心的精确世界坐标
        /// </summary>
        public Vector2 AnchorPoint;
        
        /// <summary>
        /// 水平接入点：从锚点向外延伸后的水平走廊段末端
        /// 用于确保所有走廊都从水平段开始
        /// </summary>
        public Vector2 HorizontalStubEnd;
        
        /// <summary>
        /// 引导点：水平接入段末端向外延伸的安全寻路起点
        /// </summary>
        public Vector2 ApproachPoint;
        
        /// <summary>
        /// 门的朝向
        /// </summary>
        public Direction Direction;
        
        /// <summary>
        /// 是入口还是出口
        /// </summary>
        public bool IsEntrance;
        
        /// <summary>
        /// 获取朝向的单位向量
        /// </summary>
        public Vector2 DirectionVector
        {
            get
            {
                switch (Direction)
                {
                    case Direction.North: return Vector2.up;
                    case Direction.South: return Vector2.down;
                    case Direction.East: return Vector2.right;
                    case Direction.West: return Vector2.left;
                    default: return Vector2.up;
                }
            }
        }
    }
    
    /// <summary>
    /// 多网格关卡管理器
    /// 在场景中生成多个独立的4x4网格关卡区域
    /// </summary>
    public class MultiGridLevelManager : MonoBehaviour
    {
        [TitleGroup("生成器引用")]
        [LabelText("灰盒关卡生成器")]
        [Required("必须指定灰盒关卡生成器组件")]
        public GrayboxLevelGenerator LevelGenerator;
        
        [TitleGroup("多网格布局")]
        [LabelText("网格总数量")]
        [Range(1, 8)]
        public int GridCount = 4;
        
        [TitleGroup("多网格布局")]
        [LabelText("布局区域宽度")]
        [SuffixLabel("瓦片", true)]
        [Range(100, 500)]
        public int LayoutAreaWidth = 200;
        
        [TitleGroup("多网格布局")]
        [LabelText("布局区域高度")]
        [SuffixLabel("瓦片", true)]
        [Range(100, 500)]
        public int LayoutAreaHeight = 200;
        
        [TitleGroup("多网格布局")]
        [LabelText("网格最小间距")]
        [SuffixLabel("瓦片", true)]
        [Range(8, 64)]
        public int MinGridSpacing = 16;
        
        [TitleGroup("多网格布局")]
        [LabelText("位置随机偏移")]
        [SuffixLabel("瓦片", true)]
        [Range(0, 32)]
        public int PositionRandomOffset = 16;
        
        [TitleGroup("随机性控制")]
        [LabelText("基础随机种子")]
        [InfoBox("设为0时使用完全随机种子")]
        public int BaseSeed = 0;
        
        [TitleGroup("随机性控制")]
        [LabelText("每网格独立种子")]
        public bool UseUniqueSeedPerGrid = true;
        
        [TitleGroup("随机性控制")]
        [LabelText("位置尝试次数")]
        [Range(50, 500)]
        public int MaxPlacementAttempts = 100;
        
        [TitleGroup("特殊区域设置")]
        [LabelText("中位数网格概率")]
        [ProgressBar(0, 1, ColorGetter = "GetSpecialChanceColor")]
        public float MedianGridSpecialChance = 0.8f;
        
        [TitleGroup("特殊区域设置")]
        [LabelText("其他网格概率")]
        [ProgressBar(0, 1, ColorGetter = "GetSpecialChanceColor")]
        public float OtherGridSpecialChance = 0.15f;
        
        [TitleGroup("调试显示")]
        [LabelText("显示网格边界")]
        public bool ShowGridBounds = true;
        
        [TitleGroup("调试显示")]
        [LabelText("网格边界颜色")]
        [ShowIf("ShowGridBounds")]
        public Color GridBoundsColor = new Color(0f, 1f, 0f, 0.5f);
        
        [TitleGroup("出入口标记")]
        [LabelText("显示出入口标记")]
        public bool ShowEntranceExitMarkers = true;
        
        [TitleGroup("出入口标记")]
        [LabelText("入口标记颜色")]
        [ShowIf("ShowEntranceExitMarkers")]
        public Color EntranceMarkerColor = new Color(0f, 1f, 0f, 1f);
        
        [TitleGroup("出入口标记")]
        [LabelText("出口标记颜色")]
        [ShowIf("ShowEntranceExitMarkers")]
        public Color ExitMarkerColor = new Color(0f, 0f, 0f, 1f);
        
        [TitleGroup("出入口标记")]
        [LabelText("标记大小")]
        [ShowIf("ShowEntranceExitMarkers")]
        [Range(1f, 10f)]
        public float MarkerSize = 4f;
        
        [TitleGroup("走廊设置")]
        [LabelText("显示走廊路径")]
        public bool ShowCorridorPaths = true;
        
        [TitleGroup("走廊设置")]
        [LabelText("走廊路径颜色")]
        [ShowIf("ShowCorridorPaths")]
        public Color CorridorPathColor = new Color(1f, 0.5f, 0f, 1f);
        
        [TitleGroup("走廊设置")]
        [LabelText("走廊线宽")]
        [ShowIf("ShowCorridorPaths")]
        [Range(1f, 5f)]
        public float CorridorLineWidth = 2f;
        
        [TitleGroup("走廊设置")]
        [LabelText("寻路网格分辨率")]
        [SuffixLabel("瓦片", true)]
        [Range(2, 16)]
        public int PathfindingResolution = 4;
        
        [TitleGroup("走廊设置")]
        [LabelText("障碍物安全边距")]
        [SuffixLabel("瓦片", true)]
        [Range(0, 32)]
        public float ObstacleMargin = 8f;
        
        [TitleGroup("走廊设置")]
        [LabelText("样条曲线段数")]
        [Range(10, 50)]
        public int SplineSegments = 30;
        
        // ==================== 生成控制 ====================
        
        [TitleGroup("生成控制", Order = 100)]
        [Button("生成多网格关卡", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void GenerateButton()
        {
            GenerateMultiGridLevel();
        }
        
        [TitleGroup("生成控制")]
        [Button("清除所有网格", ButtonSizes.Medium), GUIColor(0.8f, 0.4f, 0.4f)]
        public void ClearButton()
        {
            ClearAllGrids();
        }
        
        // ==================== 预览信息 ====================
        
        [TitleGroup("预览信息", Order = 101)]
        [ShowInInspector, LabelText("单网格宽度"), ReadOnly, SuffixLabel("瓦片", true)]
        private int PreviewSingleGridWidth => LevelGenerator != null ? LevelShape.GridWidth * LevelGenerator.RoomWidth : 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("单网格高度"), ReadOnly, SuffixLabel("瓦片", true)]
        private int PreviewSingleGridHeight => LevelGenerator != null ? LevelShape.GridHeight * LevelGenerator.RoomHeight : 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("已生成网格数"), ReadOnly]
        private int PreviewGeneratedGridCount => _gridPositions?.Count ?? 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("入口数量"), ReadOnly]
        private int PreviewEntranceCount => _entrancePositions?.Count ?? 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("出口数量"), ReadOnly]
        private int PreviewExitCount => _exitPositions?.Count ?? 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("网格位置列表"), ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        private List<Vector2Int> PreviewGridPositions => _gridPositions ?? new List<Vector2Int>();
        
        // ==================== 辅助方法 ====================
        
        /// <summary>
        /// 获取特殊区域概率条颜色
        /// </summary>
        private Color GetSpecialChanceColor(float value)
        {
            return Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 0.2f), value);
        }
        
        // 内部状态
        private System.Random _rng;
        private List<Rect> _placedGridBounds = new List<Rect>();
        private List<Vector2Int> _gridPositions = new List<Vector2Int>();
        
        // 存储每个网格的入口和出口锚点数据
        private List<PortalAnchor> _entranceAnchors = new List<PortalAnchor>();
        private List<PortalAnchor> _exitAnchors = new List<PortalAnchor>();
        
        // 兼容性：保留原有位置列表用于预览显示
        private List<Vector3> _entrancePositions = new List<Vector3>();
        private List<Vector3> _exitPositions = new List<Vector3>();
        private List<Direction> _entranceDirections = new List<Direction>();
        private List<Direction> _exitDirections = new List<Direction>();
        
        // 存储走廊路径（每条走廊连接相邻网格）
        private List<List<Vector2>> _corridorPaths = new List<List<Vector2>>();
        
        // 计算属性
        private int SingleGridWidth => LevelShape.GridWidth * LevelGenerator.RoomWidth;
        private int SingleGridHeight => LevelShape.GridHeight * LevelGenerator.RoomHeight;
        
        /// <summary>
        /// 生成多网格关卡
        /// </summary>
        public void GenerateMultiGridLevel()
        {
            if (!ValidateSetup()) return;
            
            // 初始化随机数生成器
            _rng = BaseSeed == 0 ? new System.Random() : new System.Random(BaseSeed);
            _placedGridBounds.Clear();
            _gridPositions.Clear();
            _entranceAnchors.Clear();
            _exitAnchors.Clear();
            _entrancePositions.Clear();
            _exitPositions.Clear();
            _entranceDirections.Clear();
            _exitDirections.Clear();
            
            // 清除所有Tilemap层
            LevelGenerator.TilemapLayers.ClearAll();
            
            Debug.Log($"开始生成多网格关卡: {GridCount}个网格, 布局区域: {LayoutAreaWidth}x{LayoutAreaHeight}");
            
            // 生成所有网格的随机位置
            if (!GenerateRandomPositions())
            {
                Debug.LogError("无法在指定区域内放置所有网格，请增加布局区域或减少网格数量");
                return;
            }
            
            // 计算中位数网格索引
            int medianIndex = _gridPositions.Count / 2;
            
            // 遍历生成每个网格
            for (int i = 0; i < _gridPositions.Count; i++)
            {
                Vector2Int pos = _gridPositions[i];
                int gridSeed = GetGridSeed(i);
                
                // 判断是否生成特殊区域
                bool isMedianGrid = (i == medianIndex);
                float specialChance = isMedianGrid ? MedianGridSpecialChance : OtherGridSpecialChance;
                bool generateSpecialArea = _rng.NextDouble() < specialChance;
                
                // 生成单个网格
                GenerateSingleGridAtOffset(pos.x, pos.y, gridSeed, generateSpecialArea);
                
                string specialInfo = generateSpecialArea ? ", 包含特殊区域" : "";
                Debug.Log($"  网格[{i}] 生成完成 (位置: {pos.x},{pos.y}, 种子: {gridSeed}{specialInfo})");
            }
            
            // 生成走廊路径
            GenerateCorridorPaths();
            
            Debug.Log($"多网格关卡生成完成! 共{_gridPositions.Count}个独立网格, {_corridorPaths.Count}条走廊");
        }
        
        /// <summary>
        /// 生成走廊路径（连接相邻网格的出入口）
        /// 实现双层走廊避免重叠：水平走廊使用双层，垂直走廊使用单层
        /// </summary>
        private void GenerateCorridorPaths()
        {
            _corridorPaths.Clear();
            
            if (_exitAnchors.Count < 2 || _entranceAnchors.Count < 2)
            {
                Debug.Log("网格数量不足，无法生成走廊");
                return;
            }
            
            // 创建布局边界
            Rect layoutBounds = new Rect(0, 0, LayoutAreaWidth, LayoutAreaHeight);
            
            // 创建寻路器
            CorridorPathfinder pathfinder = new CorridorPathfinder(
                _placedGridBounds,
                layoutBounds,
                PathfindingResolution,
                ObstacleMargin,
                BaseSeed
            );
            
            // 收集已生成的走廊段，用于检测重叠
            List<Rect> existingCorridorBounds = new List<Rect>();
            
            // 按照网格序号顺序连接：网格0的出口 -> 网格1的入口 -> 网格1的出口 -> 网格2的入口 ...
            for (int i = 0; i < _gridPositions.Count - 1; i++)
            {
                // 使用锚点数据
                PortalAnchor exitAnchor = _exitAnchors[i];
                PortalAnchor entranceAnchor = _entranceAnchors[i + 1];
                
                // 根据目标房间位置调整水平接入段方向
                Vector2 exitStubEnd = AdjustHorizontalStub(exitAnchor, entranceAnchor.AnchorPoint);
                Vector2 entranceStubEnd = AdjustHorizontalStub(entranceAnchor, exitAnchor.AnchorPoint);
                
                // 生成中间的 A* 路径（水平接入段末端到水平接入段末端）
                List<Vector2> middlePath = pathfinder.FindPath(exitStubEnd, entranceStubEnd);
                
                // 拼接完整路径（带距离检测防止重复点）
                List<Vector2> corridorPath = new List<Vector2>();
                float minDistance = 0.05f;  // 更严格的最小点间距
                
                // 段落 A：出口锚点 -> 水平接入段末端
                corridorPath.Add(exitAnchor.AnchorPoint);
                
                // 添加水平接入段中间点（如果是 North/South 方向需要拐点）
                if (exitAnchor.Direction == Direction.North || exitAnchor.Direction == Direction.South)
                {
                    // 垂直门：先垂直延伸，再水平
                    Vector2 verticalPoint = exitAnchor.HorizontalStubEnd;
                    // 调整水平方向指向目标
                    float horizontalDir = (entranceAnchor.AnchorPoint.x > exitAnchor.AnchorPoint.x) ? 1f : -1f;
                    Vector2 horizontalEnd = new Vector2(verticalPoint.x + horizontalDir * 3f, verticalPoint.y);
                    
                    AddPointIfFarEnough(corridorPath, verticalPoint, minDistance);
                    AddPointIfFarEnough(corridorPath, horizontalEnd, minDistance);
                }
                else
                {
                    // 水平门（East/West）：直接水平延伸
                    AddPointIfFarEnough(corridorPath, exitStubEnd, minDistance);
                }
                
                // 段落 B：A* 路径
                if (middlePath.Count > 0)
                {
                    foreach (var p in middlePath)
                    {
                        AddPointIfFarEnough(corridorPath, p, minDistance);
                    }
                }
                
                // 段落 C：入口水平接入段 -> 入口锚点
                if (entranceAnchor.Direction == Direction.North || entranceAnchor.Direction == Direction.South)
                {
                    // 垂直门：先水平，再垂直
                    float horizontalDir = (exitAnchor.AnchorPoint.x > entranceAnchor.AnchorPoint.x) ? 1f : -1f;
                    Vector2 horizontalEnd = new Vector2(entranceAnchor.HorizontalStubEnd.x + horizontalDir * 3f, entranceAnchor.HorizontalStubEnd.y);
                    
                    AddPointIfFarEnough(corridorPath, horizontalEnd, minDistance);
                    AddPointIfFarEnough(corridorPath, entranceAnchor.HorizontalStubEnd, minDistance);
                }
                else
                {
                    // 水平门：直接连接
                    AddPointIfFarEnough(corridorPath, entranceStubEnd, minDistance);
                }
                
                AddPointIfFarEnough(corridorPath, entranceAnchor.AnchorPoint, minDistance);
                
                // 【路径清洗】合并共线点
                corridorPath = CleanCorridorPath(corridorPath);
                
                if (corridorPath.Count >= 2)
                {
                    // 记录走廊边界用于后续重叠检测
                    Rect corridorBound = CalculatePathBounds(corridorPath);
                    existingCorridorBounds.Add(corridorBound);
                    
                    _corridorPaths.Add(corridorPath);
                    Debug.Log($"  走廊[{i}]: 网格{i}出口 -> 网格{i + 1}入口, 路径点数: {corridorPath.Count}");
                }
                else
                {
                    Debug.LogWarning($"  走廊[{i}]: 无法生成有效路径");
                }
            }
        }
        
        /// <summary>
        /// 根据目标位置调整水平接入段的方向
        /// </summary>
        private Vector2 AdjustHorizontalStub(PortalAnchor anchor, Vector2 targetPos)
        {
            float stubLength = 3f;
            
            switch (anchor.Direction)
            {
                case Direction.North:
                case Direction.South:
                    // 垂直门：水平方向指向目标
                    float horizontalDir = (targetPos.x > anchor.AnchorPoint.x) ? 1f : -1f;
                    return new Vector2(anchor.HorizontalStubEnd.x + horizontalDir * stubLength, anchor.HorizontalStubEnd.y);
                    
                case Direction.East:
                    // 东向门：向右延伸
                    return anchor.ApproachPoint;
                    
                case Direction.West:
                    // 西向门：向左延伸
                    return anchor.ApproachPoint;
                    
                default:
                    return anchor.ApproachPoint;
            }
        }
        
        /// <summary>
        /// 仅当距离足够远时添加点（避免重复点）
        /// </summary>
        private void AddPointIfFarEnough(List<Vector2> path, Vector2 point, float minDistance)
        {
            if (path.Count == 0 || Vector2.Distance(path[path.Count - 1], point) >= minDistance)
            {
                path.Add(point);
            }
        }
        
        /// <summary>
        /// 计算走廊层级偏移（避免与已有走廊重叠）
        /// </summary>
        private float CalculateCorridorLayerOffset(Vector2 start, Vector2 end, List<Rect> existingBounds, bool isMainlyHorizontal)
        {
            // 计算当前走廊的预估边界
            Rect currentBound = new Rect(
                Mathf.Min(start.x, end.x) - 5f,
                Mathf.Min(start.y, end.y) - 5f,
                Mathf.Abs(end.x - start.x) + 10f,
                Mathf.Abs(end.y - start.y) + 10f
            );
            
            // 检查与已有走廊的重叠
            int overlapCount = 0;
            foreach (Rect existing in existingBounds)
            {
                if (currentBound.Overlaps(existing))
                {
                    overlapCount++;
                }
            }
            
            // 根据重叠数量和走廊方向计算偏移
            if (overlapCount == 0)
                return 0f;
            
            // 水平走廊使用双层（上下偏移），垂直走廊使用单层（左右偏移）
            float baseOffset = 8f; // 基础偏移量
            if (isMainlyHorizontal)
            {
                // 水平走廊：交替上下偏移
                return (overlapCount % 2 == 0) ? baseOffset : -baseOffset;
            }
            else
            {
                // 垂直走廊：交替左右偏移
                return (overlapCount % 2 == 0) ? baseOffset : -baseOffset;
            }
        }
        
        /// <summary>
        /// 应用层级偏移到位置
        /// </summary>
        private Vector2 ApplyLayerOffset(Vector2 pos, Direction dir, float offset, bool isMainlyHorizontal)
        {
            if (Mathf.Approximately(offset, 0f))
                return pos;
            
            // 根据走廊主方向应用偏移
            if (isMainlyHorizontal)
            {
                // 水平走廊：在Y轴方向偏移
                return new Vector2(pos.x, pos.y + offset);
            }
            else
            {
                // 垂直走廊：在X轴方向偏移
                return new Vector2(pos.x + offset, pos.y);
            }
        }
        
        /// <summary>
        /// 清洗走廊路径：合并共线点，去除冗余
        /// </summary>
        private List<Vector2> CleanCorridorPath(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            float tolerance = 0.5f;
            List<Vector2> cleaned = new List<Vector2>();
            cleaned.Add(path[0]);
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 prev = cleaned[cleaned.Count - 1];
                Vector2 curr = path[i];
                Vector2 next = path[i + 1];
                
                // 检查三点是否共线（水平或垂直）
                bool prevCurrHorizontal = Mathf.Abs(prev.y - curr.y) < tolerance;
                bool currNextHorizontal = Mathf.Abs(curr.y - next.y) < tolerance;
                bool prevCurrVertical = Mathf.Abs(prev.x - curr.x) < tolerance;
                bool currNextVertical = Mathf.Abs(curr.x - next.x) < tolerance;
                
                bool isCollinearHorizontal = prevCurrHorizontal && currNextHorizontal;
                bool isCollinearVertical = prevCurrVertical && currNextVertical;
                
                // 不共线则保留（拐点）
                if (!isCollinearHorizontal && !isCollinearVertical)
                {
                    cleaned.Add(curr);
                }
            }
            
            // 始终添加终点
            cleaned.Add(path[path.Count - 1]);
            
            return cleaned;
        }
        
        /// <summary>
        /// 计算路径的边界矩形
        /// </summary>
        private Rect CalculatePathBounds(List<Vector2> path)
        {
            if (path.Count == 0)
                return new Rect();
            
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            
            foreach (Vector2 point in path)
            {
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }
            
            // 添加走廊宽度
            float corridorWidth = 4f;
            return new Rect(
                minX - corridorWidth,
                minY - corridorWidth,
                maxX - minX + corridorWidth * 2,
                maxY - minY + corridorWidth * 2
            );
        }
        
        /// <summary>
        /// 生成有序布局位置（随机化蛇形布局，保证走廊不交叉）
        /// </summary>
        private bool GenerateRandomPositions()
        {
            int gridWidth = SingleGridWidth;
            int gridHeight = SingleGridHeight;
            int spacing = MinGridSpacing;
            
            // 走廊通道宽度（房间之间的间距，用于放置走廊）
            int corridorSpace = spacing + 15;
            
            // 计算每行可以放置的房间数量（留有余地）
            int maxRoomsPerRow = Mathf.Max(1, (LayoutAreaWidth - corridorSpace) / (gridWidth + corridorSpace));
            
            // 随机决定每行实际放置的房间数量（1到最大值之间）
            List<int> rowRoomCounts = new List<int>();
            int remainingRooms = GridCount;
            
            while (remainingRooms > 0)
            {
                // 随机决定这一行放几个房间（至少1个，最多剩余数量或最大值）
                int maxInThisRow = Mathf.Min(maxRoomsPerRow, remainingRooms);
                int minInThisRow = Mathf.Max(1, maxInThisRow - 1);
                int roomsInRow = _rng.Next(minInThisRow, maxInThisRow + 1);
                
                rowRoomCounts.Add(roomsInRow);
                remainingRooms -= roomsInRow;
            }
            
            int rowsNeeded = rowRoomCounts.Count;
            
            // 检查是否有足够的垂直空间
            int totalHeightNeeded = rowsNeeded * gridHeight + (rowsNeeded - 1) * corridorSpace;
            if (totalHeightNeeded > LayoutAreaHeight)
            {
                Debug.LogError($"布局区域高度不足! 需要 {totalHeightNeeded}, 可用 {LayoutAreaHeight}");
                return false;
            }
            
            // 计算起始Y位置（从顶部开始，带随机偏移）
            int verticalPadding = LayoutAreaHeight - totalHeightNeeded;
            int startY = LayoutAreaHeight - gridHeight - _rng.Next(0, Mathf.Max(1, verticalPadding / 2));
            
            int gridIndex = 0;
            for (int row = 0; row < rowsNeeded && gridIndex < GridCount; row++)
            {
                int roomsInThisRow = rowRoomCounts[row];
                
                // 计算当前行的Y位置（带随机偏移）
                int baseY = startY - row * (gridHeight + corridorSpace);
                int yOffset = _rng.Next(-PositionRandomOffset / 2, PositionRandomOffset / 2 + 1);
                int y = Mathf.Clamp(baseY + yOffset, 0, LayoutAreaHeight - gridHeight);
                
                // 蛇形布局：偶数行从左到右，奇数行从右到左
                bool leftToRight = (row % 2 == 0);
                
                // 计算这一行的水平分布（随机化间距）
                int totalRowWidth = roomsInThisRow * gridWidth + (roomsInThisRow - 1) * corridorSpace;
                int horizontalPadding = LayoutAreaWidth - totalRowWidth;
                int startX = _rng.Next(0, Mathf.Max(1, horizontalPadding));
                
                for (int col = 0; col < roomsInThisRow && gridIndex < GridCount; col++)
                {
                    int actualCol = leftToRight ? col : (roomsInThisRow - 1 - col);
                    
                    // 计算X位置（带随机间距）
                    int randomSpacing = corridorSpace + _rng.Next(-5, 6);
                    int x = startX + actualCol * (gridWidth + randomSpacing);
                    
                    // 添加额外随机偏移
                    if (PositionRandomOffset > 0)
                    {
                        int maxOffset = Mathf.Min(PositionRandomOffset, corridorSpace / 3);
                        x += _rng.Next(-maxOffset, maxOffset + 1);
                    }
                    
                    // 确保在有效范围内且不重叠
                    x = Mathf.Clamp(x, 0, LayoutAreaWidth - gridWidth);
                    int finalY = Mathf.Clamp(y, 0, LayoutAreaHeight - gridHeight);
                    
                    // 检查是否与已放置的网格重叠
                    Rect newBounds = new Rect(x, finalY, gridWidth, gridHeight);
                    bool overlaps = false;
                    foreach (var existingBounds in _placedGridBounds)
                    {
                        Rect expandedExisting = new Rect(
                            existingBounds.x - spacing,
                            existingBounds.y - spacing,
                            existingBounds.width + spacing * 2,
                            existingBounds.height + spacing * 2
                        );
                        if (newBounds.Overlaps(expandedExisting))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    
                    // 如果重叠，尝试调整位置
                    if (overlaps)
                    {
                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            x = startX + actualCol * (gridWidth + corridorSpace) + _rng.Next(-20, 21);
                            x = Mathf.Clamp(x, 0, LayoutAreaWidth - gridWidth);
                            newBounds = new Rect(x, finalY, gridWidth, gridHeight);
                            
                            overlaps = false;
                            foreach (var existingBounds in _placedGridBounds)
                            {
                                Rect expandedExisting = new Rect(
                                    existingBounds.x - spacing,
                                    existingBounds.y - spacing,
                                    existingBounds.width + spacing * 2,
                                    existingBounds.height + spacing * 2
                                );
                                if (newBounds.Overlaps(expandedExisting))
                                {
                                    overlaps = true;
                                    break;
                                }
                            }
                            
                            if (!overlaps) break;
                        }
                    }
                    
                    _gridPositions.Add(new Vector2Int(x, finalY));
                    _placedGridBounds.Add(new Rect(x, finalY, gridWidth, gridHeight));
                    
                    gridIndex++;
                }
            }
            
            Debug.Log($"随机化蛇形布局完成: {rowsNeeded}行, 房间分布: {string.Join(",", rowRoomCounts)}");
            return true;
        }
        
        /// <summary>
        /// 清除所有网格
        /// </summary>
        public void ClearAllGrids()
        {
            if (LevelGenerator != null && LevelGenerator.TilemapLayers != null)
            {
                LevelGenerator.TilemapLayers.ClearAll();
                _placedGridBounds.Clear();
                _gridPositions.Clear();
                _entrancePositions.Clear();
                _exitPositions.Clear();
                _entranceDirections.Clear();
                _exitDirections.Clear();
                _corridorPaths.Clear();
                Debug.Log("所有网格已清除");
            }
        }
        
        /// <summary>
        /// 获取已生成的网格位置列表（供外部访问）
        /// </summary>
        public List<Vector2Int> GetGridPositions()
        {
            return new List<Vector2Int>(_gridPositions);
        }
        
        /// <summary>
        /// 获取已生成的网格边界列表（供外部访问）
        /// </summary>
        public List<Rect> GetGridBounds()
        {
            return new List<Rect>(_placedGridBounds);
        }
        
        /// <summary>
        /// 获取走廊路径列表（供外部访问）
        /// </summary>
        public List<List<Vector2>> GetCorridorPaths()
        {
            return _corridorPaths;
        }
        
        /// <summary>
        /// 在指定偏移位置生成单个网格
        /// </summary>
        private void GenerateSingleGridAtOffset(int offsetX, int offsetY, int seed, bool generateSpecialArea = true)
        {
            // 设置生成器的随机种子
            LevelGenerator.RandomSeed = seed;
            
            // 获取Tilemap引用
            var tilemapLayers = LevelGenerator.TilemapLayers;
            var tileSet = LevelGenerator.TileSet;
            
            // 调用生成器的内部方法（通过反射或直接调用公开方法）
            // 由于现有Generator不支持偏移，我们需要直接操作Tilemap
            GenerateGridContent(offsetX, offsetY, seed, generateSpecialArea);
        }
        
        /// <summary>
        /// 生成网格内容（带偏移）
        /// </summary>
        private void GenerateGridContent(int offsetX, int offsetY, int seed, bool generateSpecialArea = true)
        {
            var rng = new System.Random(seed);
            var tilemapLayers = LevelGenerator.TilemapLayers;
            var tileSet = LevelGenerator.TileSet;
            
            int roomWidth = LevelGenerator.RoomWidth;
            int roomHeight = LevelGenerator.RoomHeight;
            int wallThickness = LevelGenerator.WallThickness;
            
            // 初始化房间网格数据
            LevelShape currentShape = new LevelShape();
            RoomNode[,] roomGrid = new RoomNode[LevelShape.GridWidth, LevelShape.GridHeight];
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    roomGrid[x, y] = new RoomNode(x, y);
                    roomGrid[x, y].Type = RoomType.Side;
                }
            }
            
            // 生成关键路径（醉汉游走）
            GenerateCriticalPath(rng, currentShape, roomGrid, roomWidth, roomHeight);
            
            // 绘制外围墙壁
            DrawOuterWalls(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制洞穴填充
            DrawCaveFill(offsetX, offsetY, rng, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制房间连接
            DrawRoomConnections(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制入口和出口
            DrawEntranceAndExit(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制平台
            DrawPlatforms(offsetX, offsetY, rng, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制特殊区域（根据概率决定）
            if (generateSpecialArea)
            {
                DrawSpecialAreas(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight);
            }
        }
        
        /// <summary>
        /// 生成关键路径
        /// </summary>
        private void GenerateCriticalPath(System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            // 从顶排随机选择入口
            int startX = rng.Next(LevelShape.GridWidth);
            Vector2Int current = new Vector2Int(startX, 0);
            
            List<Vector2Int> path = new List<Vector2Int>();
            path.Add(current);
            shape.SetCell(current.x, current.y, true);
            
            // 每层先水平游走，再向下
            for (int row = 0; row < LevelShape.GridHeight; row++)
            {
                int horizontalDirection = rng.Next(2) == 0 ? -1 : 1;
                int horizontalSteps = rng.Next(1, 4);
                
                for (int step = 0; step < horizontalSteps; step++)
                {
                    int nextX = current.x + horizontalDirection;
                    
                    if (nextX < 0 || nextX >= LevelShape.GridWidth)
                    {
                        horizontalDirection = -horizontalDirection;
                        nextX = current.x + horizontalDirection;
                        if (nextX < 0 || nextX >= LevelShape.GridWidth) break;
                    }
                    
                    Vector2Int next = new Vector2Int(nextX, current.y);
                    if (!path.Contains(next)) path.Add(next);
                    shape.SetCell(next.x, next.y, true);
                    
                    // 添加连接
                    Direction dir = GetDirection(current, next);
                    roomGrid[current.x, current.y].AddConnection(dir);
                    roomGrid[next.x, next.y].AddConnection(dir.Opposite());
                    
                    current = next;
                }
                
                if (row < LevelShape.GridHeight - 1)
                {
                    Vector2Int next = new Vector2Int(current.x, current.y + 1);
                    if (!path.Contains(next)) path.Add(next);
                    shape.SetCell(next.x, next.y, true);
                    
                    Direction dir = GetDirection(current, next);
                    roomGrid[current.x, current.y].AddConnection(dir);
                    roomGrid[next.x, next.y].AddConnection(dir.Opposite());
                    
                    current = next;
                }
            }
            
            // 设置入口和出口
            Vector2Int entrance = path[0];
            Vector2Int exit = path[path.Count - 1];
            
            roomGrid[entrance.x, entrance.y].Type = RoomType.Start;
            roomGrid[entrance.x, entrance.y].IsCriticalPath = true;
            
            roomGrid[exit.x, exit.y].Type = RoomType.Exit;
            roomGrid[exit.x, exit.y].IsCriticalPath = true;
            
            foreach (var cell in path)
            {
                roomGrid[cell.x, cell.y].IsCriticalPath = true;
            }
            
            // 设置Boss房间
            if (path.Count >= 3)
            {
                var bossCell = path[path.Count - 2];
                if (roomGrid[bossCell.x, bossCell.y].Type != RoomType.Start &&
                    roomGrid[bossCell.x, bossCell.y].Type != RoomType.Exit)
                {
                    roomGrid[bossCell.x, bossCell.y].Type = RoomType.Boss;
                }
            }
        }
        
        /// <summary>
        /// 绘制外围墙壁
        /// </summary>
        private void DrawOuterWalls(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.WallLayer;
            var tile = LevelGenerator.TileSet.RedTile;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 北边
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                    {
                        FillRect(tilemap, tile, worldX, worldY + roomHeight - wallThickness, roomWidth, wallThickness);
                    }
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(tilemap, tile, worldX, worldY, roomWidth, wallThickness);
                    }
                    // 西边
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(tilemap, tile, worldX, worldY, wallThickness, roomHeight);
                    }
                    // 东边
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                    {
                        FillRect(tilemap, tile, worldX + roomWidth - wallThickness, worldY, wallThickness, roomHeight);
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制洞穴填充
        /// </summary>
        private void DrawCaveFill(int offsetX, int offsetY, System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            var tile = LevelGenerator.TileSet.OrangeTile;
            float fillDensity = LevelGenerator.FillDensity;
            int smoothIterations = LevelGenerator.SmoothIterations;
            
            // 添加边界墙壁
            int baseBoundaryWidth = 12;
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                        FillRect(tilemap, tile, worldX, worldY + roomHeight - baseBoundaryWidth, roomWidth, baseBoundaryWidth);
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                        FillRect(tilemap, tile, worldX, worldY, roomWidth, baseBoundaryWidth);
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                        FillRect(tilemap, tile, worldX, worldY, baseBoundaryWidth, roomHeight);
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                        FillRect(tilemap, tile, worldX + roomWidth - baseBoundaryWidth, worldY, baseBoundaryWidth, roomHeight);
                }
            }
            
            // 生成连贯洞穴填充
            GenerateConnectedCaveFill(offsetX, offsetY, rng, shape, roomWidth, roomHeight, fillDensity, smoothIterations);
            
            // 雕刻边界边缘
            CarveBoundaryEdges(offsetX, offsetY, rng, shape, roomWidth, roomHeight);
            
            // 清除房间连接通道
            ClearAllConnectionPassages(offsetX, offsetY, shape, roomGrid, roomWidth, roomHeight);
            
            // 雕刻曲折通道
            CarveWindingPath(offsetX, offsetY, rng, shape, roomGrid, roomWidth, roomHeight, wallThickness);
        }
        
        /// <summary>
        /// 生成连贯洞穴填充
        /// </summary>
        private void GenerateConnectedCaveFill(int offsetX, int offsetY, System.Random rng, LevelShape shape, int roomWidth, int roomHeight, float fillDensity, int smoothIterations)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            var tile = LevelGenerator.TileSet.OrangeTile;
            
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (shape.IsValidCell(gx, gy))
                    {
                        int worldX = gx * roomWidth;
                        int worldY = (LevelShape.GridHeight - 1 - gy) * roomHeight;
                        minX = Mathf.Min(minX, worldX);
                        maxX = Mathf.Max(maxX, worldX + roomWidth);
                        minY = Mathf.Min(minY, worldY);
                        maxY = Mathf.Max(maxY, worldY + roomHeight);
                    }
                }
            }
            
            int fillWidth = maxX - minX;
            int fillHeight = maxY - minY;
            if (fillWidth <= 0 || fillHeight <= 0) return;
            
            bool[,] cave = new bool[fillWidth, fillHeight];
            
            for (int y = 0; y < fillHeight; y++)
            {
                for (int x = 0; x < fillWidth; x++)
                {
                    int worldX = minX + x;
                    int worldY = minY + y;
                    int gx = worldX / roomWidth;
                    int gy = LevelShape.GridHeight - 1 - worldY / roomHeight;
                    
                    if (gx < 0 || gx >= LevelShape.GridWidth || gy < 0 || gy >= LevelShape.GridHeight || !shape.IsValidCell(gx, gy))
                        continue;
                    
                    int distToEdge = Mathf.Min(x, y, fillWidth - 1 - x, fillHeight - 1 - y);
                    float edgeFactor = (distToEdge < 6) ? 2.0f : 1.0f;
                    if (y < fillHeight / 4) edgeFactor *= 1.8f;
                    
                    cave[x, y] = rng.NextDouble() < fillDensity * edgeFactor;
                }
            }
            
            for (int i = 0; i < smoothIterations + 3; i++)
            {
                cave = SmoothCave(cave, fillWidth, fillHeight);
            }
            
            for (int y = 0; y < fillHeight; y++)
            {
                for (int x = 0; x < fillWidth; x++)
                {
                    if (cave[x, y])
                    {
                        int worldX = minX + x;
                        int worldY = minY + y;
                        int gx = worldX / roomWidth;
                        int gy = LevelShape.GridHeight - 1 - worldY / roomHeight;
                        
                        if (gx >= 0 && gx < LevelShape.GridWidth && gy >= 0 && gy < LevelShape.GridHeight && shape.IsValidCell(gx, gy))
                        {
                            tilemap.SetTile(new Vector3Int(offsetX + worldX, offsetY + worldY, 0), tile);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 细胞自动机平滑
        /// </summary>
        private bool[,] SmoothCave(bool[,] cave, int width, int height)
        {
            bool[,] newCave = new bool[width, height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int neighbors = CountNeighbors(cave, x, y, width, height);
                    newCave[x, y] = neighbors >= 4;
                }
            }
            
            return newCave;
        }
        
        /// <summary>
        /// 统计邻居数量
        /// </summary>
        private int CountNeighbors(bool[,] cave, int x, int y, int width, int height)
        {
            int count = 0;
            
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        count++;
                    }
                    else if (cave[nx, ny])
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 雕刻边界边缘
        /// </summary>
        private void CarveBoundaryEdges(int offsetX, int offsetY, System.Random rng, LevelShape shape, int roomWidth, int roomHeight)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            int carveDepth = 6;
            int carveVariation = 4;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 北边
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                    {
                        int baseY = worldY + roomHeight - 12;
                        for (int x = 0; x < roomWidth; x++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                tilemap.SetTile(new Vector3Int(worldX + x, baseY + y, 0), null);
                            }
                        }
                    }
                    
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                    {
                        int baseY = worldY + 12;
                        for (int x = 0; x < roomWidth; x++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                tilemap.SetTile(new Vector3Int(worldX + x, baseY - y - 1, 0), null);
                            }
                        }
                    }
                    
                    // 西边
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                    {
                        int baseX = worldX + 12;
                        for (int y = 0; y < roomHeight; y++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                tilemap.SetTile(new Vector3Int(baseX - x - 1, worldY + y, 0), null);
                            }
                        }
                    }
                    
                    // 东边
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                    {
                        int baseX = worldX + roomWidth - 12;
                        for (int y = 0; y < roomHeight; y++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                tilemap.SetTile(new Vector3Int(baseX + x, worldY + y, 0), null);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除所有连接通道
        /// </summary>
        private void ClearAllConnectionPassages(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            int passageWidth = 8;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    if (room.HasConnection(Direction.North))
                        ClearRect(tilemap, centerX - passageWidth / 2, worldY + roomHeight - 4, passageWidth, 4);
                    if (room.HasConnection(Direction.South))
                        ClearRect(tilemap, centerX - passageWidth / 2, worldY, passageWidth, 4);
                    if (room.HasConnection(Direction.East))
                        ClearRect(tilemap, worldX + roomWidth - 4, centerY - passageWidth / 2, 4, passageWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(tilemap, worldX, centerY - passageWidth / 2, 4, passageWidth);
                }
            }
        }
        
        /// <summary>
        /// 雕刻曲折通道
        /// </summary>
        private void CarveWindingPath(int offsetX, int offsetY, System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            int pathWidth = 5;
            int margin = wallThickness + 2;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    // 清除中心区域
                    ClearRect(tilemap, centerX - 4, centerY - 4, 8, 8);
                    
                    // 随机游走创建曲折通道
                    int currentX = centerX;
                    int currentY = centerY;
                    
                    for (int step = 0; step < 15; step++)
                    {
                        int dx = rng.Next(-3, 4);
                        int dy = rng.Next(-3, 4);
                        
                        int nextX = Mathf.Clamp(currentX + dx, worldX + margin, worldX + roomWidth - margin);
                        int nextY = Mathf.Clamp(currentY + dy, worldY + margin, worldY + roomHeight - margin);
                        
                        ClearLine(tilemap, currentX, currentY, nextX, nextY, pathWidth);
                        
                        currentX = nextX;
                        currentY = nextY;
                    }
                    
                    // 确保连接通道畅通
                    if (room.HasConnection(Direction.North))
                        ClearRect(tilemap, centerX - pathWidth / 2, centerY, pathWidth, roomHeight / 2);
                    if (room.HasConnection(Direction.South))
                        ClearRect(tilemap, centerX - pathWidth / 2, worldY + margin, pathWidth, roomHeight / 2);
                    if (room.HasConnection(Direction.East))
                        ClearRect(tilemap, centerX, centerY - pathWidth / 2, roomWidth / 2, pathWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(tilemap, worldX + margin, centerY - pathWidth / 2, roomWidth / 2, pathWidth);
                }
            }
        }
        
        /// <summary>
        /// 绘制房间连接
        /// </summary>
        private void DrawRoomConnections(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var wallTilemap = LevelGenerator.TilemapLayers.WallLayer;
            int passageWidth = 8;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    if (room.HasConnection(Direction.North))
                        ClearRect(wallTilemap, centerX - passageWidth / 2, worldY + roomHeight - wallThickness, passageWidth, wallThickness);
                    if (room.HasConnection(Direction.South))
                        ClearRect(wallTilemap, centerX - passageWidth / 2, worldY, passageWidth, wallThickness);
                    if (room.HasConnection(Direction.East))
                        ClearRect(wallTilemap, worldX + roomWidth - wallThickness, centerY - passageWidth / 2, wallThickness, passageWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(wallTilemap, worldX, centerY - passageWidth / 2, wallThickness, passageWidth);
                }
            }
        }
        
        /// <summary>
        /// 绘制入口和出口（根据房间位置智能选择方向）
        /// </summary>
        private void DrawEntranceAndExit(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var entranceTilemap = LevelGenerator.TilemapLayers.EntranceLayer;
            var exitTilemap = LevelGenerator.TilemapLayers.ExitLayer;
            var wallTilemap = LevelGenerator.TilemapLayers.WallLayer;
            var fillTilemap = LevelGenerator.TilemapLayers.FillLayer;
            var greenTile = LevelGenerator.TileSet.GreenTile;
            var blackTile = LevelGenerator.TileSet.BlackTile;
            
            int entranceWidth = LevelGenerator.EntranceWidth;
            int entranceHeight = LevelGenerator.EntranceHeight;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    if (room.Type == RoomType.Start)
                    {
                        // 根据房间在网格中的位置选择入口方向
                        Direction entranceDir = GetBestEntranceDirection(gx, gy, shape);
                        
                        // 绘制门户并获取精确锚点数据
                        PortalAnchor entranceAnchor = DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                            entranceWidth, entranceHeight, entranceDir, true,
                            wallTilemap, fillTilemap, entranceTilemap, greenTile);
                        
                        // 存储锚点数据
                        _entranceAnchors.Add(entranceAnchor);
                        
                        // 兼容性：同时更新原有列表用于预览
                        _entrancePositions.Add(new Vector3(entranceAnchor.AnchorPoint.x, entranceAnchor.AnchorPoint.y, 0));
                        _entranceDirections.Add(entranceDir);
                    }
                    
                    if (room.Type == RoomType.Exit)
                    {
                        // 根据房间在网格中的位置选择出口方向
                        Direction exitDir = GetBestExitDirection(gx, gy, shape);
                        
                        // 绘制门户并获取精确锚点数据
                        PortalAnchor exitAnchor = DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness,
                            entranceWidth, entranceHeight, exitDir, false,
                            wallTilemap, fillTilemap, exitTilemap, blackTile);
                        
                        // 存储锚点数据
                        _exitAnchors.Add(exitAnchor);
                        
                        // 兼容性：同时更新原有列表用于预览
                        _exitPositions.Add(new Vector3(exitAnchor.AnchorPoint.x, exitAnchor.AnchorPoint.y, 0));
                        _exitDirections.Add(exitDir);
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据房间位置获取最佳入口方向
        /// 简化逻辑：优先选择可用的外围方向
        /// </summary>
        private Direction GetBestEntranceDirection(int gx, int gy, LevelShape shape)
        {
            // 收集所有可用的外围方向（没有相邻房间的方向）
            List<Direction> availableDirections = new List<Direction>();
            
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            // 入口优先级：左 > 上 > 下 > 右
            if (canWest) availableDirections.Add(Direction.West);
            if (canNorth) availableDirections.Add(Direction.North);
            if (canSouth) availableDirections.Add(Direction.South);
            if (canEast) availableDirections.Add(Direction.East);
            
            if (availableDirections.Count > 0)
            {
                return availableDirections[0]; // 返回最优先的方向
            }
            
            // 默认顶部
            return Direction.North;
        }
        
        /// <summary>
        /// 根据房间位置获取最佳出口方向
        /// 简化逻辑：优先选择可用的外围方向
        /// </summary>
        private Direction GetBestExitDirection(int gx, int gy, LevelShape shape)
        {
            // 收集所有可用的外围方向（没有相邻房间的方向）
            List<Direction> availableDirections = new List<Direction>();
            
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            // 出口优先级：右 > 下 > 上 > 左（与入口相反）
            if (canEast) availableDirections.Add(Direction.East);
            if (canSouth) availableDirections.Add(Direction.South);
            if (canNorth) availableDirections.Add(Direction.North);
            if (canWest) availableDirections.Add(Direction.West);
            
            if (availableDirections.Count > 0)
            {
                return availableDirections[0]; // 返回最优先的方向
            }
            
            // 默认底部
            return Direction.South;
        }
        
        /// <summary>
        /// 获取外围方向（入口或出口）
        /// </summary>
        private Direction GetOuterDirection(int gx, int gy, LevelShape shape, bool isEntrance)
        {
            List<Direction> availableDirections = new List<Direction>();
            
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            if (isEntrance)
            {
                // 入口优先：左 > 上 > 右
                if (canWest) availableDirections.Add(Direction.West);
                if (canNorth) availableDirections.Add(Direction.North);
                if (canEast) availableDirections.Add(Direction.East);
            }
            else
            {
                // 出口优先：右 > 下 > 左
                if (canEast) availableDirections.Add(Direction.East);
                if (canSouth) availableDirections.Add(Direction.South);
                if (canWest) availableDirections.Add(Direction.West);
            }
            
            if (availableDirections.Count > 0)
            {
                return availableDirections[_rng.Next(availableDirections.Count)];
            }
            
            return isEntrance ? Direction.North : Direction.South;
        }
        
        /// <summary>
        /// 获取下一个网格的预期位置（用于方向预测）
        /// </summary>
        private Vector2Int GetNextGridPosition()
        {
            if (_gridPositions.Count == 0)
                return new Vector2Int(0, 0);
            
            Vector2Int lastPos = _gridPositions[_gridPositions.Count - 1];
            int spacing = MinGridSpacing + 15;
            
            // 简单预测：右侧或下方
            return new Vector2Int(lastPos.x + SingleGridWidth + spacing, lastPos.y);
        }
        
        /// <summary>
        /// 绘制入口/出口门户并返回精确锚点数据
        /// </summary>
        private PortalAnchor DrawPortal(int worldX, int worldY, int roomWidth, int roomHeight, int wallThickness,
            int portalWidth, int portalHeight, Direction direction, bool isEntrance,
            Tilemap wallTilemap, Tilemap fillTilemap, Tilemap portalTilemap, TileBase portalTile)
        {
            int centerX = worldX + roomWidth / 2;
            int centerY = worldY + roomHeight / 2;
            
            int portalX = 0, portalY = 0;
            
            switch (direction)
            {
                case Direction.North:
                    portalX = centerX - portalWidth / 2;
                    portalY = worldY + roomHeight - wallThickness;
                    ClearRect(wallTilemap, portalX, portalY, portalWidth, wallThickness);
                    ClearRect(fillTilemap, portalX, portalY - portalHeight, portalWidth, portalHeight + wallThickness);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, portalWidth, wallThickness);
                    break;
                    
                case Direction.South:
                    portalX = centerX - portalWidth / 2;
                    portalY = worldY;
                    ClearRect(wallTilemap, portalX, portalY, portalWidth, wallThickness);
                    ClearRect(fillTilemap, portalX, portalY, portalWidth, portalHeight + wallThickness);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, portalWidth, wallThickness);
                    break;
                    
                case Direction.West:
                    portalX = worldX;
                    portalY = centerY - portalWidth / 2;
                    ClearRect(wallTilemap, portalX, portalY, wallThickness, portalWidth);
                    ClearRect(fillTilemap, portalX, portalY, portalHeight + wallThickness, portalWidth);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, wallThickness, portalWidth);
                    break;
                    
                case Direction.East:
                    portalX = worldX + roomWidth - wallThickness;
                    portalY = centerY - portalWidth / 2;
                    ClearRect(wallTilemap, portalX, portalY, wallThickness, portalWidth);
                    ClearRect(fillTilemap, portalX - portalHeight, portalY, portalHeight + wallThickness, portalWidth);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, wallThickness, portalWidth);
                    break;
            }
            
            // 计算精确锚点（门瓦片中心）
            Vector2 anchorPoint = CalculatePortalAnchorPoint(portalX, portalY, portalWidth, wallThickness, direction);
            
            // 计算水平接入段和引导点
            float verticalExtend = Mathf.Max(wallThickness + 2f, ObstacleMargin + 1f);  // 垂直延伸距离
            float horizontalStubLength = 3f;  // 水平接入段长度
            float approachExtend = 2f;  // 引导点额外延伸
            
            Vector2 horizontalStubEnd;
            Vector2 approachPoint;
            
            // 根据门方向计算水平接入段
            // 注意：此时不知道目标房间位置，先使用默认方向，后续在走廊生成时会调整
            switch (direction)
            {
                case Direction.North:
                    // 北向门：先向上延伸，然后水平（默认向右，后续调整）
                    horizontalStubEnd = anchorPoint + Vector2.up * verticalExtend;
                    approachPoint = horizontalStubEnd + Vector2.right * horizontalStubLength;
                    break;
                case Direction.South:
                    // 南向门：先向下延伸，然后水平（默认向右，后续调整）
                    horizontalStubEnd = anchorPoint + Vector2.down * verticalExtend;
                    approachPoint = horizontalStubEnd + Vector2.right * horizontalStubLength;
                    break;
                case Direction.East:
                    // 东向门：直接水平向右（本身就是水平）
                    horizontalStubEnd = anchorPoint + Vector2.right * verticalExtend;
                    approachPoint = horizontalStubEnd + Vector2.right * horizontalStubLength;
                    break;
                case Direction.West:
                    // 西向门：直接水平向左（本身就是水平）
                    horizontalStubEnd = anchorPoint + Vector2.left * verticalExtend;
                    approachPoint = horizontalStubEnd + Vector2.left * horizontalStubLength;
                    break;
                default:
                    horizontalStubEnd = anchorPoint;
                    approachPoint = anchorPoint;
                    break;
            }
            
            return new PortalAnchor
            {
                AnchorPoint = anchorPoint,
                HorizontalStubEnd = horizontalStubEnd,
                ApproachPoint = approachPoint,
                Direction = direction,
                IsEntrance = isEntrance
            };
        }
        
        /// <summary>
        /// 计算门瓦片中心的精确锚点坐标
        /// </summary>
        private Vector2 CalculatePortalAnchorPoint(int portalX, int portalY, int portalWidth, int wallThickness, Direction direction)
        {
            // 根据方向计算门瓦片区域的中心点
            switch (direction)
            {
                case Direction.North:
                case Direction.South:
                    // 水平方向的门：中心在 (portalX + portalWidth/2, portalY + wallThickness/2)
                    return new Vector2(portalX + portalWidth / 2f, portalY + wallThickness / 2f);
                    
                case Direction.East:
                case Direction.West:
                    // 垂直方向的门：中心在 (portalX + wallThickness/2, portalY + portalWidth/2)
                    return new Vector2(portalX + wallThickness / 2f, portalY + portalWidth / 2f);
                    
                default:
                    return new Vector2(portalX, portalY);
            }
        }
        
        /// <summary>
        /// 计算引导点（锚点向外延伸）
        /// </summary>
        private Vector2 CalculateApproachPoint(Vector2 anchorPoint, Direction direction, float distance)
        {
            switch (direction)
            {
                case Direction.North:
                    return anchorPoint + Vector2.up * distance;
                case Direction.South:
                    return anchorPoint + Vector2.down * distance;
                case Direction.East:
                    return anchorPoint + Vector2.right * distance;
                case Direction.West:
                    return anchorPoint + Vector2.left * distance;
                default:
                    return anchorPoint;
            }
        }
        
        /// <summary>
        /// 获取门户中心位置（向外延伸，用于走廊连接）
        /// 确保延伸距离大于 ObstacleMargin，避免起点落在扩展障碍物内
        /// </summary>
        private Vector3 GetPortalPosition(int worldX, int worldY, int roomWidth, int roomHeight,
            int wallThickness, int portalWidth, Direction direction)
        {
            int centerX = worldX + roomWidth / 2;
            int centerY = worldY + roomHeight / 2;
            
            // 【关键修改】确保延伸距离大于 ObstacleMargin
            // 走廊起点必须在扩展障碍物边界之外，否则 A* 寻路会立即失败
            float extendDistance = Mathf.Max(wallThickness + 5f, ObstacleMargin + 3f);
            
            switch (direction)
            {
                case Direction.North:
                    // 向上延伸
                    return new Vector3(centerX, worldY + roomHeight + extendDistance, 0);
                case Direction.South:
                    // 向下延伸
                    return new Vector3(centerX, worldY - extendDistance, 0);
                case Direction.West:
                    // 向左延伸
                    return new Vector3(worldX - extendDistance, centerY, 0);
                case Direction.East:
                    // 向右延伸
                    return new Vector3(worldX + roomWidth + extendDistance, centerY, 0);
                default:
                    return new Vector3(centerX, centerY, 0);
            }
        }
        
        /// <summary>
        /// 绘制平台
        /// </summary>
        private void DrawPlatforms(int offsetX, int offsetY, System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            var fillTilemap = LevelGenerator.TilemapLayers.FillLayer;
            var tile = LevelGenerator.TileSet.BlueTile;
            
            int maxJumpHeight = LevelGenerator.MaxPlatformHeightDiff;
            int minGap = LevelGenerator.MinPlatformGap;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    if (room.Type == RoomType.Start || room.Type == RoomType.Exit) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    int platformLayers = roomHeight / (maxJumpHeight + minGap);
                    platformLayers = Mathf.Clamp(platformLayers, 1, 3);
                    
                    int lastY = worldY + wallThickness + 1;
                    
                    for (int layer = 0; layer < platformLayers; layer++)
                    {
                        int layerHeight = lastY + minGap + rng.Next(maxJumpHeight - minGap + 1);
                        if (layerHeight > worldY + roomHeight - 4) break;
                        
                        int pw = 4 + rng.Next(roomWidth / 3);
                        int px = worldX + 2 + rng.Next(roomWidth - pw - 4);
                        
                        FillRect(tilemap, tile, px, layerHeight, pw, 1);
                        ClearRect(fillTilemap, px - 1, layerHeight, pw + 2, 4);
                        
                        lastY = layerHeight;
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制特殊区域
        /// </summary>
        private void DrawSpecialAreas(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            var specialTilemap = LevelGenerator.TilemapLayers.SpecialLayer;
            var platformTilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            var fillTilemap = LevelGenerator.TilemapLayers.FillLayer;
            var yellowTile = LevelGenerator.TileSet.YellowTile;
            var blueTile = LevelGenerator.TileSet.BlueTile;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    if (room.Type == RoomType.Boss)
                    {
                        int bossAreaSize = 10;
                        FillRect(specialTilemap, yellowTile, centerX - bossAreaSize / 2, centerY - bossAreaSize / 2, bossAreaSize, bossAreaSize);
                        ClearRect(fillTilemap, worldX + 3, worldY + 3, roomWidth - 6, roomHeight - 6);
                        FillRect(platformTilemap, blueTile, worldX + 3, worldY + 3, roomWidth - 6, 1);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取网格种子
        /// </summary>
        private int GetGridSeed(int gridIndex)
        {
            if (UseUniqueSeedPerGrid)
            {
                return BaseSeed == 0 ? _rng.Next() : BaseSeed + gridIndex * 1000;
            }
            return BaseSeed == 0 ? _rng.Next() : BaseSeed;
        }
        
        /// <summary>
        /// 获取方向
        /// </summary>
        private Direction GetDirection(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            
            if (dy < 0) return Direction.North;
            if (dy > 0) return Direction.South;
            if (dx > 0) return Direction.East;
            if (dx < 0) return Direction.West;
            
            return Direction.North;
        }
        
        /// <summary>
        /// 验证设置
        /// </summary>
        private bool ValidateSetup()
        {
            if (LevelGenerator == null)
            {
                Debug.LogError("MultiGridLevelManager: 未配置LevelGenerator引用!");
                return false;
            }
            
            if (LevelGenerator.TilemapLayers == null || !LevelGenerator.TilemapLayers.IsValid())
            {
                Debug.LogError("MultiGridLevelManager: LevelGenerator的TilemapLayers未配置!");
                return false;
            }
            
            if (LevelGenerator.TileSet == null || !LevelGenerator.TileSet.IsValid())
            {
                Debug.LogError("MultiGridLevelManager: LevelGenerator的TileSet未配置!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 填充矩形区域
        /// </summary>
        private void FillRect(Tilemap tilemap, TileBase tile, int x, int y, int width, int height)
        {
            if (tilemap == null || tile == null) return;
            
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    tilemap.SetTile(new Vector3Int(x + dx, y + dy, 0), tile);
                }
            }
        }
        
        /// <summary>
        /// 清除矩形区域
        /// </summary>
        private void ClearRect(Tilemap tilemap, int x, int y, int width, int height)
        {
            if (tilemap == null) return;
            
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    tilemap.SetTile(new Vector3Int(x + dx, y + dy, 0), null);
                }
            }
        }
        
        /// <summary>
        /// 清除线段区域
        /// </summary>
        private void ClearLine(Tilemap tilemap, int x1, int y1, int x2, int y2, int width)
        {
            int steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1)) + 1;
            
            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (float)i / steps : 0;
                int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
                
                ClearRect(tilemap, x - width / 2, y - width / 2, width, width);
            }
        }
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            if (LevelGenerator == null) return;
            
            int gridWidth = SingleGridWidth;
            int gridHeight = SingleGridHeight;
            
            // 绘制网格边界
            if (ShowGridBounds)
            {
                // 绘制布局区域边界
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Vector3 layoutCenter = new Vector3(LayoutAreaWidth / 2f, LayoutAreaHeight / 2f, 0);
                Vector3 layoutSize = new Vector3(LayoutAreaWidth, LayoutAreaHeight, 0);
                Gizmos.DrawWireCube(layoutCenter, layoutSize);
                
                // 绘制已生成的网格边界
                Gizmos.color = GridBoundsColor;
                foreach (var bounds in _placedGridBounds)
                {
                    Vector3 center = new Vector3(
                        bounds.x + bounds.width / 2f,
                        bounds.y + bounds.height / 2f,
                        0);
                    Vector3 size = new Vector3(bounds.width, bounds.height, 0);
                    Gizmos.DrawWireCube(center, size);
                }
                
                // 如果没有已生成的网格，显示预估位置
                if (_placedGridBounds.Count == 0)
                {
                    Gizmos.color = new Color(GridBoundsColor.r, GridBoundsColor.g, GridBoundsColor.b, 0.2f);
                    
                    int cols = Mathf.Max(1, LayoutAreaWidth / (gridWidth + MinGridSpacing));
                    int rows = Mathf.Max(1, LayoutAreaHeight / (gridHeight + MinGridSpacing));
                    int showCount = Mathf.Min(GridCount, cols * rows);
                    
                    for (int i = 0; i < showCount; i++)
                    {
                        int col = i % cols;
                        int row = i / cols;
                        
                        float x = col * (gridWidth + MinGridSpacing) + gridWidth / 2f;
                        float y = row * (gridHeight + MinGridSpacing) + gridHeight / 2f;
                        
                        Vector3 center = new Vector3(x, y, 0);
                        Vector3 size = new Vector3(gridWidth, gridHeight, 0);
                        Gizmos.DrawWireCube(center, size);
                    }
                }
            }
            
            // 绘制入口和出口标记
            if (ShowEntranceExitMarkers)
            {
                // 绘制入口标记（根据方向绘制箭头）
                Gizmos.color = EntranceMarkerColor;
                for (int i = 0; i < _entrancePositions.Count; i++)
                {
                    Vector3 pos = _entrancePositions[i];
                    Direction dir = i < _entranceDirections.Count ? _entranceDirections[i] : Direction.North;
                    
                    // 绘制实心圆形
                    Gizmos.DrawSphere(pos, MarkerSize);
                    
                    // 根据方向绘制箭头（指向房间内部）
                    DrawDirectionalArrow(pos, dir, true);
                }
                
                // 绘制出口标记（根据方向绘制箭头）
                Gizmos.color = ExitMarkerColor;
                for (int i = 0; i < _exitPositions.Count; i++)
                {
                    Vector3 pos = _exitPositions[i];
                    Direction dir = i < _exitDirections.Count ? _exitDirections[i] : Direction.South;
                    
                    // 绘制实心圆形
                    Gizmos.DrawSphere(pos, MarkerSize);
                    
                    // 根据方向绘制箭头（指向房间外部）
                    DrawDirectionalArrow(pos, dir, false);
                }
            }
            
            // 绘制走廊路径
            if (ShowCorridorPaths && _corridorPaths != null && _corridorPaths.Count > 0)
            {
                Gizmos.color = CorridorPathColor;
                
                for (int corridorIndex = 0; corridorIndex < _corridorPaths.Count; corridorIndex++)
                {
                    List<Vector2> path = _corridorPaths[corridorIndex];
                    
                    if (path == null || path.Count < 2)
                        continue;
                    
                    // 绘制路径线段
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Vector3 start = new Vector3(path[i].x, path[i].y, 0);
                        Vector3 end = new Vector3(path[i + 1].x, path[i + 1].y, 0);
                        Gizmos.DrawLine(start, end);
                    }
                    
                    // 在路径中点绘制走廊编号
                    if (path.Count > 1)
                    {
                        int midIndex = path.Count / 2;
                        Vector3 midPoint = new Vector3(path[midIndex].x, path[midIndex].y, 0);
                        Gizmos.DrawSphere(midPoint, MarkerSize * 0.5f);
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据方向绘制箭头
        /// </summary>
        private void DrawDirectionalArrow(Vector3 pos, Direction direction, bool pointInward)
        {
            Vector3 arrowDir = Vector3.zero;
            Vector3 perpendicular = Vector3.zero;
            
            // 确定箭头方向（入口指向内部，出口指向外部）
            switch (direction)
            {
                case Direction.North:
                    arrowDir = pointInward ? Vector3.down : Vector3.up;
                    perpendicular = Vector3.right;
                    break;
                case Direction.South:
                    arrowDir = pointInward ? Vector3.up : Vector3.down;
                    perpendicular = Vector3.right;
                    break;
                case Direction.West:
                    arrowDir = pointInward ? Vector3.right : Vector3.left;
                    perpendicular = Vector3.up;
                    break;
                case Direction.East:
                    arrowDir = pointInward ? Vector3.left : Vector3.right;
                    perpendicular = Vector3.up;
                    break;
            }
            
            // 绘制箭头
            Vector3 arrowTip = pos + arrowDir * (MarkerSize * 2.5f);
            Vector3 arrowLeft = pos + arrowDir * (MarkerSize * 1.5f) + perpendicular * MarkerSize;
            Vector3 arrowRight = pos + arrowDir * (MarkerSize * 1.5f) - perpendicular * MarkerSize;
            
            Gizmos.DrawLine(pos, arrowTip);
            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }
        
        /// <summary>
        /// 获取所有入口位置（供外部访问）
        /// </summary>
        public List<Vector3> GetEntrancePositions()
        {
            return new List<Vector3>(_entrancePositions);
        }
        
        /// <summary>
        /// 获取所有出口位置（供外部访问）
        /// </summary>
        public List<Vector3> GetExitPositions()
        {
            return new List<Vector3>(_exitPositions);
        }
        
        /// <summary>
        /// 获取所有入口方向（供外部访问）
        /// </summary>
        public List<Direction> GetEntranceDirections()
        {
            return new List<Direction>(_entranceDirections);
        }
        
        /// <summary>
        /// 获取所有出口方向（供外部访问）
        /// </summary>
        public List<Direction> GetExitDirections()
        {
            return new List<Direction>(_exitDirections);
        }
    }
}
