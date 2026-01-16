using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;
using CryptaGeometrica.LevelGeneration.Graybox;

namespace CryptaGeometrica.LevelGeneration.Editor
{
    /// <summary>
    /// 多房间关卡生成器编辑器窗口
    /// 提供可视化预览和参数配置
    /// </summary>
    public class MultiGridMapEditorWindow : EditorWindow
    {
        // ==================== 窗口引用 ====================
        private MultiGridLevelManager _targetManager;
        private Vector2 _leftPanelScroll;
        private Vector2 _rightPanelScroll;
        private float _splitRatio = 0.35f;
        
        // ==================== 预览设置 ====================
        private float _previewScale = 1f;
        private Vector2 _previewOffset = Vector2.zero;
        private bool _isDragging = false;
        private Vector2 _lastMousePos;
        
        // ==================== 样式缓存 ====================
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _legendBoxStyle;
        private GUIStyle _infoLabelStyle;
        private bool _stylesInitialized = false;
        
        // ==================== 颜色定义 ====================
        private readonly Color _wallColor = new Color(1f, 0.5f, 0f, 1f);      // 橙色 - 墙壁
        private readonly Color _fillColor = new Color(0.3f, 0.3f, 0.3f, 1f);  // 灰色 - 填充
        private readonly Color _platformColor = new Color(0f, 0.5f, 1f, 1f);  // 蓝色 - 平台
        private readonly Color _entranceColor = new Color(0f, 1f, 0f, 1f);    // 绿色 - 入口
        private readonly Color _exitColor = new Color(0f, 0f, 0f, 1f);        // 黑色 - 出口
        private readonly Color _specialColor = new Color(1f, 1f, 0f, 1f);     // 黄色 - 特殊区域
        private readonly Color _gridBoundsColor = new Color(0f, 1f, 0f, 0.5f); // 绿色半透明 - 网格边界
        private readonly Color _layoutAreaColor = new Color(1f, 1f, 0f, 0.3f); // 黄色半透明 - 布局区域
        
        [MenuItem("Crypta Geometrica: RE/CryptaGeometricaMapEditor/多房间关卡生成器")]
        public static void ShowWindow()
        {
            var window = GetWindow<MultiGridMapEditorWindow>();
            window.titleContent = new GUIContent("多房间关卡生成器");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 自动查找场景中的MultiGridLevelManager
            FindTargetManager();
            
            // 订阅场景变化事件
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        private void OnHierarchyChanged()
        {
            if (_targetManager == null)
            {
                FindTargetManager();
            }
            Repaint();
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            // 场景变化时刷新窗口
            Repaint();
        }
        
        private void FindTargetManager()
        {
            _targetManager = FindObjectOfType<MultiGridLevelManager>();
        }
        
        private void InitStyles()
        {
            if (_stylesInitialized) return;
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            
            _legendBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8)
            };
            
            _infoLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };
            
            _stylesInitialized = true;
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            // 绘制工具栏
            DrawToolbar();
            
            // 主内容区域
            EditorGUILayout.BeginHorizontal();
            
            // 左侧参数面板
            float leftWidth = position.width * _splitRatio;
            EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();
            
            // 分隔线
            DrawSplitter();
            
            // 右侧预览面板
            EditorGUILayout.BeginVertical();
            DrawRightPanel();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // 处理输入事件
            HandleInputEvents();
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                FindTargetManager();
                Repaint();
            }
            
            if (GUILayout.Button("定位管理器", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (_targetManager != null)
                {
                    Selection.activeGameObject = _targetManager.gameObject;
                    EditorGUIUtility.PingObject(_targetManager);
                }
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"预览缩放: {_previewScale:F2}x", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("重置视图", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _previewScale = 1f;
                _previewOffset = Vector2.zero;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制左侧参数面板
        /// </summary>
        private void DrawLeftPanel()
        {
            _leftPanelScroll = EditorGUILayout.BeginScrollView(_leftPanelScroll);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("参数设置", _headerStyle);
            EditorGUILayout.Space(5);
            
            // 目标管理器选择
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("目标管理器", _subHeaderStyle);
            
            EditorGUI.BeginChangeCheck();
            _targetManager = (MultiGridLevelManager)EditorGUILayout.ObjectField(
                "管理器组件", _targetManager, typeof(MultiGridLevelManager), true);
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
            
            if (_targetManager == null)
            {
                EditorGUILayout.HelpBox("请在场景中创建MultiGridLevelManager组件，或将其拖拽到此处", UnityEditor.MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            if (_targetManager != null)
            {
                // 生成控制按钮
                DrawGenerationControls();
                
                EditorGUILayout.Space(10);
                
                // 参数编辑
                DrawParameterEditor();
                
                EditorGUILayout.Space(10);
                
                // 生成状态信息
                DrawStatusInfo();
            }
            
            EditorGUILayout.Space(10);
            
            // 图例说明
            DrawLegend();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制生成控制按钮
        /// </summary>
        private void DrawGenerationControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("生成控制", _subHeaderStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("生成多网格关卡", GUILayout.Height(30)))
            {
                _targetManager.GenerateMultiGridLevel();
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
            if (GUILayout.Button("清除所有", GUILayout.Height(30), GUILayout.Width(80)))
            {
                _targetManager.ClearAllGrids();
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制参数编辑器
        /// </summary>
        private void DrawParameterEditor()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("布局参数", _subHeaderStyle);
            
            EditorGUI.BeginChangeCheck();
            
            _targetManager.GridCount = EditorGUILayout.IntSlider("网格数量", _targetManager.GridCount, 1, 8);
            _targetManager.LayoutAreaWidth = EditorGUILayout.IntSlider("布局区域宽度", _targetManager.LayoutAreaWidth, 100, 500);
            _targetManager.LayoutAreaHeight = EditorGUILayout.IntSlider("布局区域高度", _targetManager.LayoutAreaHeight, 100, 500);
            _targetManager.MinGridSpacing = EditorGUILayout.IntSlider("最小间距", _targetManager.MinGridSpacing, 8, 64);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("随机性控制", _subHeaderStyle);
            _targetManager.BaseSeed = EditorGUILayout.IntField("随机种子 (0=随机)", _targetManager.BaseSeed);
            _targetManager.UseUniqueSeedPerGrid = EditorGUILayout.Toggle("每网格独立种子", _targetManager.UseUniqueSeedPerGrid);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("特殊区域概率", _subHeaderStyle);
            _targetManager.MedianGridSpecialChance = EditorGUILayout.Slider("中位数网格", _targetManager.MedianGridSpecialChance, 0f, 1f);
            _targetManager.OtherGridSpecialChance = EditorGUILayout.Slider("其他网格", _targetManager.OtherGridSpecialChance, 0f, 1f);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_targetManager);
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制状态信息
        /// </summary>
        private void DrawStatusInfo()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("生成状态", _subHeaderStyle);
            
            var positions = _targetManager.GetGridPositions();
            var entrances = _targetManager.GetEntrancePositions();
            var exits = _targetManager.GetExitPositions();
            
            EditorGUILayout.LabelField($"已生成网格数: {positions.Count}");
            EditorGUILayout.LabelField($"入口数量: {entrances.Count}");
            EditorGUILayout.LabelField($"出口数量: {exits.Count}");
            
            if (positions.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("网格位置:", EditorStyles.miniLabel);
                
                for (int i = 0; i < positions.Count; i++)
                {
                    EditorGUILayout.LabelField($"  网格[{i}]: ({positions[i].x}, {positions[i].y})", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制图例说明
        /// </summary>
        private void DrawLegend()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("瓦片图例", _subHeaderStyle);
            
            DrawLegendItem(_wallColor, "橙色", "墙壁/边界");
            DrawLegendItem(_fillColor, "灰色", "洞穴填充");
            DrawLegendItem(_platformColor, "蓝色", "平台");
            DrawLegendItem(_entranceColor, "绿色", "入口 (↓指向内部)");
            DrawLegendItem(_exitColor, "黑色", "出口 (↓指向外部)");
            DrawLegendItem(_specialColor, "黄色", "特殊区域/Boss房");
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("预览标记", _subHeaderStyle);
            DrawLegendItem(_gridBoundsColor, "绿框", "网格边界");
            DrawLegendItem(_layoutAreaColor, "黄框", "布局区域");
            DrawLegendItem(new Color(1f, 0.5f, 0f, 1f), "橙线", "走廊路径 (Cn=走廊编号)");
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制图例项
        /// </summary>
        private void DrawLegendItem(Color color, string colorName, string description)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 绘制颜色方块
            Rect colorRect = GUILayoutUtility.GetRect(20, 16, GUILayout.Width(20));
            EditorGUI.DrawRect(colorRect, color);
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, colorRect.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.yMax - 1, colorRect.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, 1, colorRect.height), Color.black);
            EditorGUI.DrawRect(new Rect(colorRect.xMax - 1, colorRect.y, 1, colorRect.height), Color.black);
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField($"{colorName}: {description}", _infoLabelStyle);
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制分隔线
        /// </summary>
        private void DrawSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(4, position.height - 20, GUILayout.Width(4));
            EditorGUI.DrawRect(splitterRect, new Color(0.2f, 0.2f, 0.2f));
            
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _isDragging = true;
                Event.current.Use();
            }
        }
        
        /// <summary>
        /// 绘制右侧预览面板
        /// </summary>
        private void DrawRightPanel()
        {
            EditorGUILayout.LabelField("预览视图", _headerStyle);
            
            // 预览区域
            Rect previewRect = GUILayoutUtility.GetRect(
                position.width * (1 - _splitRatio) - 20, 
                position.height - 60);
            
            // 绘制背景
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));
            
            if (_targetManager != null)
            {
                // 绘制预览内容
                DrawPreviewContent(previewRect);
            }
            else
            {
                // 绘制提示信息
                GUI.Label(previewRect, "请选择或创建MultiGridLevelManager", 
                    new GUIStyle(GUI.skin.label) 
                    { 
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 14
                    });
            }
            
            // 绘制预览区域边框
            Handles.color = Color.gray;
            Handles.DrawWireDisc(previewRect.center, Vector3.forward, 0);
            
            // 绘制边框
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.yMax - 1, previewRect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, 1, previewRect.height), Color.gray);
            EditorGUI.DrawRect(new Rect(previewRect.xMax - 1, previewRect.y, 1, previewRect.height), Color.gray);
        }
        
        /// <summary>
        /// 绘制预览内容
        /// </summary>
        private void DrawPreviewContent(Rect previewRect)
        {
            // 计算预览参数
            float layoutWidth = _targetManager.LayoutAreaWidth;
            float layoutHeight = _targetManager.LayoutAreaHeight;
            
            // 计算缩放比例使布局区域适应预览区域
            float scaleX = (previewRect.width - 40) / layoutWidth;
            float scaleY = (previewRect.height - 40) / layoutHeight;
            float baseScale = Mathf.Min(scaleX, scaleY);
            float finalScale = baseScale * _previewScale;
            
            // 计算偏移使内容居中
            Vector2 center = previewRect.center + _previewOffset;
            Vector2 layoutSize = new Vector2(layoutWidth * finalScale, layoutHeight * finalScale);
            Vector2 layoutOrigin = center - layoutSize / 2;
            
            // 绘制布局区域边界
            Rect layoutRect = new Rect(layoutOrigin.x, layoutOrigin.y, layoutSize.x, layoutSize.y);
            EditorGUI.DrawRect(layoutRect, new Color(_layoutAreaColor.r, _layoutAreaColor.g, _layoutAreaColor.b, 0.1f));
            DrawRectOutline(layoutRect, _layoutAreaColor, 2);
            
            // 绘制Tilemap瓦片内容
            if (_targetManager.LevelGenerator != null && _targetManager.LevelGenerator.TilemapLayers != null)
            {
                DrawTilemapContent(previewRect, layoutOrigin, layoutHeight, finalScale);
            }
            
            // 绘制已生成的网格
            var positions = _targetManager.GetGridPositions();
            var bounds = _targetManager.GetGridBounds();
            var entrances = _targetManager.GetEntrancePositions();
            var exits = _targetManager.GetExitPositions();
            var entranceDirs = _targetManager.GetEntranceDirections();
            var exitDirs = _targetManager.GetExitDirections();
            
            // 绘制网格边界
            for (int i = 0; i < bounds.Count; i++)
            {
                Rect gridBound = bounds[i];
                Rect screenRect = new Rect(
                    layoutOrigin.x + gridBound.x * finalScale,
                    layoutOrigin.y + (layoutHeight - gridBound.y - gridBound.height) * finalScale,
                    gridBound.width * finalScale,
                    gridBound.height * finalScale
                );
                
                // 绘制网格边框
                DrawRectOutline(screenRect, _gridBoundsColor, 2);
                
                // 绘制网格编号
                GUI.Label(new Rect(screenRect.x + 5, screenRect.y + 5, 30, 20), 
                    $"[{i}]", 
                    new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } });
            }
            
            // 绘制入口标记
            for (int i = 0; i < entrances.Count; i++)
            {
                Vector3 pos = entrances[i];
                Direction dir = i < entranceDirs.Count ? entranceDirs[i] : Direction.North;
                
                Vector2 screenPos = new Vector2(
                    layoutOrigin.x + pos.x * finalScale,
                    layoutOrigin.y + (layoutHeight - pos.y) * finalScale
                );
                
                DrawPortalMarker(screenPos, dir, true, finalScale);
            }
            
            // 绘制出口标记
            for (int i = 0; i < exits.Count; i++)
            {
                Vector3 pos = exits[i];
                Direction dir = i < exitDirs.Count ? exitDirs[i] : Direction.South;
                
                Vector2 screenPos = new Vector2(
                    layoutOrigin.x + pos.x * finalScale,
                    layoutOrigin.y + (layoutHeight - pos.y) * finalScale
                );
                
                DrawPortalMarker(screenPos, dir, false, finalScale);
            }
            
            // 绘制走廊路径
            var corridorPaths = _targetManager.GetCorridorPaths();
            if (corridorPaths != null && corridorPaths.Count > 0)
            {
                DrawCorridorPaths(corridorPaths, layoutOrigin, layoutHeight, finalScale);
            }
            
            // 绘制预估网格位置（如果没有生成）
            if (positions.Count == 0)
            {
                DrawEstimatedGrids(layoutOrigin, layoutSize, finalScale);
            }
        }
        
        /// <summary>
        /// 绘制走廊路径
        /// </summary>
        private void DrawCorridorPaths(List<List<Vector2>> corridorPaths, Vector2 layoutOrigin, float layoutHeight, float scale)
        {
            Color corridorColor = new Color(1f, 0.5f, 0f, 1f); // 橙色
            
            for (int corridorIndex = 0; corridorIndex < corridorPaths.Count; corridorIndex++)
            {
                List<Vector2> path = corridorPaths[corridorIndex];
                
                if (path == null || path.Count < 2)
                    continue;
                
                // 绘制路径线段
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector2 start = new Vector2(
                        layoutOrigin.x + path[i].x * scale,
                        layoutOrigin.y + (layoutHeight - path[i].y) * scale
                    );
                    Vector2 end = new Vector2(
                        layoutOrigin.x + path[i + 1].x * scale,
                        layoutOrigin.y + (layoutHeight - path[i + 1].y) * scale
                    );
                    
                    // 绘制线段
                    Handles.color = corridorColor;
                    Handles.DrawLine(start, end);
                }
                
                // 在路径起点和终点绘制标记
                if (path.Count > 0)
                {
                    // 起点（出口）
                    Vector2 startPos = new Vector2(
                        layoutOrigin.x + path[0].x * scale,
                        layoutOrigin.y + (layoutHeight - path[0].y) * scale
                    );
                    EditorGUI.DrawRect(new Rect(startPos.x - 3, startPos.y - 3, 6, 6), corridorColor);
                    
                    // 终点（入口）
                    Vector2 endPos = new Vector2(
                        layoutOrigin.x + path[path.Count - 1].x * scale,
                        layoutOrigin.y + (layoutHeight - path[path.Count - 1].y) * scale
                    );
                    EditorGUI.DrawRect(new Rect(endPos.x - 3, endPos.y - 3, 6, 6), corridorColor);
                    
                    // 绘制走廊编号
                    int midIndex = path.Count / 2;
                    Vector2 midPos = new Vector2(
                        layoutOrigin.x + path[midIndex].x * scale,
                        layoutOrigin.y + (layoutHeight - path[midIndex].y) * scale
                    );
                    GUI.Label(new Rect(midPos.x - 10, midPos.y - 10, 20, 20),
                        $"C{corridorIndex}",
                        new GUIStyle(GUI.skin.label) 
                        { 
                            normal = { textColor = Color.white },
                            fontSize = 10,
                            alignment = TextAnchor.MiddleCenter
                        });
                }
            }
        }
        
        /// <summary>
        /// 绘制Tilemap瓦片内容
        /// </summary>
        private void DrawTilemapContent(Rect previewRect, Vector2 layoutOrigin, float layoutHeight, float scale)
        {
            var layers = _targetManager.LevelGenerator.TilemapLayers;
            if (layers == null) return;
            
            // 计算瓦片像素大小（最小1像素）
            float tilePixelSize = Mathf.Max(1f, scale);
            
            // 只有在缩放足够大时才绘制瓦片
            if (tilePixelSize < 0.5f) return;
            
            // 按图层顺序绘制（从底层到顶层）
            // 1. 填充层 (灰色)
            if (layers.FillLayer != null)
            {
                DrawTilemapLayer(layers.FillLayer, layoutOrigin, layoutHeight, scale, tilePixelSize, _fillColor);
            }
            
            // 2. 墙壁层 (橙色)
            if (layers.WallLayer != null)
            {
                DrawTilemapLayer(layers.WallLayer, layoutOrigin, layoutHeight, scale, tilePixelSize, _wallColor);
            }
            
            // 3. 平台层 (蓝色)
            if (layers.PlatformLayer != null)
            {
                DrawTilemapLayer(layers.PlatformLayer, layoutOrigin, layoutHeight, scale, tilePixelSize, _platformColor);
            }
            
            // 4. 特殊层 (黄色)
            if (layers.SpecialLayer != null)
            {
                DrawTilemapLayer(layers.SpecialLayer, layoutOrigin, layoutHeight, scale, tilePixelSize, _specialColor);
            }
            
            // 5. 入口层 (绿色)
            if (layers.EntranceLayer != null)
            {
                DrawTilemapLayer(layers.EntranceLayer, layoutOrigin, layoutHeight, scale, tilePixelSize, _entranceColor);
            }
            
            // 6. 出口层 (黑色)
            if (layers.ExitLayer != null)
            {
                DrawTilemapLayer(layers.ExitLayer, layoutOrigin, layoutHeight, scale, tilePixelSize, _exitColor);
            }
        }
        
        /// <summary>
        /// 绘制单个Tilemap图层
        /// </summary>
        private void DrawTilemapLayer(Tilemap tilemap, Vector2 layoutOrigin, float layoutHeight, float scale, float tilePixelSize, Color color)
        {
            if (tilemap == null) return;
            
            BoundsInt bounds = tilemap.cellBounds;
            
            // 限制绘制范围以提高性能
            int maxTiles = 10000;
            int tileCount = 0;
            
            for (int x = bounds.xMin; x < bounds.xMax && tileCount < maxTiles; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax && tileCount < maxTiles; y++)
                {
                    if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                    {
                        // 计算屏幕位置
                        float screenX = layoutOrigin.x + x * scale;
                        float screenY = layoutOrigin.y + (layoutHeight - y - 1) * scale;
                        
                        Rect tileRect = new Rect(screenX, screenY, tilePixelSize, tilePixelSize);
                        EditorGUI.DrawRect(tileRect, color);
                        
                        tileCount++;
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制门户标记
        /// </summary>
        private void DrawPortalMarker(Vector2 screenPos, Direction direction, bool isEntrance, float scale)
        {
            Color color = isEntrance ? _entranceColor : _exitColor;
            float markerSize = 8 * Mathf.Max(0.5f, scale * 0.1f);
            
            // 绘制圆形标记
            Rect markerRect = new Rect(screenPos.x - markerSize, screenPos.y - markerSize, markerSize * 2, markerSize * 2);
            
            // 简化：绘制方形代替圆形
            EditorGUI.DrawRect(markerRect, color);
            
            // 绘制方向箭头
            Vector2 arrowDir = GetArrowDirection(direction, isEntrance);
            Vector2 arrowStart = screenPos;
            Vector2 arrowEnd = screenPos + arrowDir * markerSize * 2;
            
            Handles.color = color;
            Handles.DrawLine(arrowStart, arrowEnd);
            
            // 箭头头部
            Vector2 perpendicular = new Vector2(-arrowDir.y, arrowDir.x);
            Vector2 arrowHead1 = arrowEnd - arrowDir * markerSize * 0.5f + perpendicular * markerSize * 0.5f;
            Vector2 arrowHead2 = arrowEnd - arrowDir * markerSize * 0.5f - perpendicular * markerSize * 0.5f;
            
            Handles.DrawLine(arrowEnd, arrowHead1);
            Handles.DrawLine(arrowEnd, arrowHead2);
        }
        
        /// <summary>
        /// 获取箭头方向
        /// </summary>
        private Vector2 GetArrowDirection(Direction direction, bool isEntrance)
        {
            // 入口指向内部，出口指向外部
            // 注意：屏幕坐标Y轴向下
            switch (direction)
            {
                case Direction.North:
                    return isEntrance ? Vector2.down : Vector2.up;
                case Direction.South:
                    return isEntrance ? Vector2.up : Vector2.down;
                case Direction.West:
                    return isEntrance ? Vector2.right : Vector2.left;
                case Direction.East:
                    return isEntrance ? Vector2.left : Vector2.right;
                default:
                    return Vector2.down;
            }
        }
        
        /// <summary>
        /// 绘制预估网格位置
        /// </summary>
        private void DrawEstimatedGrids(Vector2 layoutOrigin, Vector2 layoutSize, float scale)
        {
            if (_targetManager.LevelGenerator == null) return;
            
            int gridWidth = LevelShape.GridWidth * _targetManager.LevelGenerator.RoomWidth;
            int gridHeight = LevelShape.GridHeight * _targetManager.LevelGenerator.RoomHeight;
            int spacing = _targetManager.MinGridSpacing;
            
            int cols = Mathf.Max(1, _targetManager.LayoutAreaWidth / (gridWidth + spacing));
            int rows = Mathf.Max(1, _targetManager.LayoutAreaHeight / (gridHeight + spacing));
            int showCount = Mathf.Min(_targetManager.GridCount, cols * rows);
            
            Color estimatedColor = new Color(_gridBoundsColor.r, _gridBoundsColor.g, _gridBoundsColor.b, 0.2f);
            
            for (int i = 0; i < showCount; i++)
            {
                int col = i % cols;
                int row = i / cols;
                
                float x = col * (gridWidth + spacing);
                float y = row * (gridHeight + spacing);
                
                Rect screenRect = new Rect(
                    layoutOrigin.x + x * scale,
                    layoutOrigin.y + (_targetManager.LayoutAreaHeight - y - gridHeight) * scale,
                    gridWidth * scale,
                    gridHeight * scale
                );
                
                EditorGUI.DrawRect(screenRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                DrawRectOutline(screenRect, estimatedColor, 1);
                
                GUI.Label(new Rect(screenRect.x + 5, screenRect.y + 5, 50, 20),
                    $"预估[{i}]",
                    new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(1, 1, 1, 0.5f) }, fontSize = 10 });
            }
        }
        
        /// <summary>
        /// 绘制矩形边框
        /// </summary>
        private void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        
        /// <summary>
        /// 处理输入事件
        /// </summary>
        private void HandleInputEvents()
        {
            Event e = Event.current;
            
            // 处理分隔线拖拽
            if (_isDragging)
            {
                if (e.type == EventType.MouseDrag)
                {
                    _splitRatio = Mathf.Clamp(e.mousePosition.x / position.width, 0.2f, 0.5f);
                    Repaint();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _isDragging = false;
                }
            }
            
            // 处理预览区域滚轮缩放
            if (e.type == EventType.ScrollWheel)
            {
                _previewScale = Mathf.Clamp(_previewScale - e.delta.y * 0.05f, 0.1f, 5f);
                e.Use();
                Repaint();
            }
            
            // 处理预览区域拖拽平移
            if (e.type == EventType.MouseDrag && e.button == 2) // 中键拖拽
            {
                _previewOffset += e.delta;
                e.Use();
                Repaint();
            }
        }
    }
}
