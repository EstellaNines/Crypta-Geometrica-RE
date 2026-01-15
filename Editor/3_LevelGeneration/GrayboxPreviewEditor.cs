using UnityEngine;
using UnityEditor;
using CryptaGeometrica.LevelGeneration;
using CryptaGeometrica.LevelGeneration.Graybox;

namespace CryptaGeometrica.Editor.LevelGeneration
{
    /// <summary>
    /// 灰盒预览编辑器窗口
    /// 用于快速预览和测试不同房间类型的模板
    /// </summary>
    public class GrayboxPreviewEditor : EditorWindow
    {
        private GrayboxRoomTemplates _roomTemplates;
        private GrayboxGridPreview _gridPreview;
        private RoomType _selectedRoomType = RoomType.LR;
        private Vector2Int _previewOrigin = Vector2Int.zero;
        
        // 网格预览设置
        private GrayboxGridPreview.LevelShapePresetType _shapePreset = GrayboxGridPreview.LevelShapePresetType.FullSquare;
        private string _customPattern = "1111,1111,1111,1111";
        
        // 房间尺寸设置
        private int _roomWidth = 32;
        private int _roomHeight = 32;
        private int _wallThickness = 2;
        private int _passageWidth = 6;
        
        private Vector2 _scrollPosition;
        
        // 生成状态记录
        private string _lastGeneratedRoom = "无";
        private int _generatedRoomCount = 0;
        private string _lastGenerationTime = "-";
        
        // 折叠状态
        private bool _showColorLegend = true;
        private bool _showGenerationStatus = true;
        private bool _showRoomTypeInfo = false;
        
        // 自定义形状编辑
        private int[,] _editableShape = new int[4, 4] {
            {1, 1, 1, 1},
            {1, 1, 1, 1},
            {1, 1, 1, 1},
            {1, 1, 1, 1}
        };
        
        [MenuItem("Crypta Geometrica: RE/CryptaGeometricaMapEditor/灰盒预览工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<GrayboxPreviewEditor>("灰盒预览");
            window.minSize = new Vector2(350, 500);
        }
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.LabelField("灰盒预览工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            DrawComponentSection();
            EditorGUILayout.Space(10);
            
            DrawSingleRoomSection();
            EditorGUILayout.Space(10);
            
            DrawGridPreviewSection();
            EditorGUILayout.Space(10);
            
            DrawRoomSettingsSection();
            EditorGUILayout.Space(10);
            
            DrawQuickActionsSection();
            EditorGUILayout.Space(10);
            
            DrawColorLegendSection();
            EditorGUILayout.Space(10);
            
            DrawGenerationStatusSection();
            EditorGUILayout.Space(10);
            
            DrawRoomTypeInfoSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制颜色图例部分
        /// </summary>
        private void DrawColorLegendSection()
        {
            _showColorLegend = EditorGUILayout.Foldout(_showColorLegend, "瓦片颜色图例", true);
            
            if (!_showColorLegend) return;
            
            EditorGUILayout.BeginVertical("box");
            
            // 红色
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.red;
            GUILayout.Box("", GUILayout.Width(30), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("红色 - 地面/墙壁 (实心障碍物，不可穿越)");
            EditorGUILayout.EndHorizontal();
            
            // 蓝色
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.blue;
            GUILayout.Box("", GUILayout.Width(30), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("蓝色 - 平台 (可站立，可从下方穿越)");
            EditorGUILayout.EndHorizontal();
            
            // 绿色
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            GUILayout.Box("", GUILayout.Width(30), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("绿色 - 通道/出入口 (房间连接点)");
            EditorGUILayout.EndHorizontal();
            
            // 黄色
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.yellow;
            GUILayout.Box("", GUILayout.Width(30), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("黄色 - 特殊区域 (商店/Boss/陷阱)");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制生成状态部分
        /// </summary>
        private void DrawGenerationStatusSection()
        {
            _showGenerationStatus = EditorGUILayout.Foldout(_showGenerationStatus, "生成状态", true);
            
            if (!_showGenerationStatus) return;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("最后生成房间:", _lastGeneratedRoom);
            EditorGUILayout.LabelField("已生成房间数:", _generatedRoomCount.ToString());
            EditorGUILayout.LabelField("最后生成时间:", _lastGenerationTime);
            
            if (_roomTemplates != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("当前配置:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  房间尺寸: {_roomTemplates.RoomWidth} x {_roomTemplates.RoomHeight}");
                EditorGUILayout.LabelField($"  墙壁厚度: {_roomTemplates.WallThickness}");
                EditorGUILayout.LabelField($"  通道宽度: {_roomTemplates.PassageWidth}");
                
                // 检查瓦片配置状态
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("瓦片配置状态:", EditorStyles.boldLabel);
                DrawTileStatus("  红色瓦片", _roomTemplates.RedTile);
                DrawTileStatus("  蓝色瓦片", _roomTemplates.BlueTile);
                DrawTileStatus("  绿色瓦片", _roomTemplates.GreenTile);
                DrawTileStatus("  黄色瓦片", _roomTemplates.YellowTile);
                DrawTileStatus("  目标Tilemap", _roomTemplates.TargetTilemap);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制瓦片状态
        /// </summary>
        private void DrawTileStatus(string label, Object obj)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            
            if (obj != null)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ 已配置");
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("✗ 未配置");
            }
            
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制房间类型说明部分
        /// </summary>
        private void DrawRoomTypeInfoSection()
        {
            _showRoomTypeInfo = EditorGUILayout.Foldout(_showRoomTypeInfo, "房间类型说明", true);
            
            if (!_showRoomTypeInfo) return;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  起点房间，玩家初始位置，右侧有通道");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Exit", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  终点房间，关卡出口，左侧有通道");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("LR", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  左右贯通房间，水平移动通道");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Drop", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  下落房间，底部开口允许向下");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Landing", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  着陆房间，顶部开口接收上层");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Side", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  侧室，非关键路径的可选房间");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Shop", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  商店房间，黄色特殊区域");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Abyss", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  深渊竖井，垂直贯通区域");
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Boss", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  Boss房间，1.3倍尺寸，黄色标记");
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制组件引用部分
        /// </summary>
        private void DrawComponentSection()
        {
            EditorGUILayout.LabelField("组件引用", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _roomTemplates = (GrayboxRoomTemplates)EditorGUILayout.ObjectField(
                "房间模板生成器", _roomTemplates, typeof(GrayboxRoomTemplates), true);
            
            _gridPreview = (GrayboxGridPreview)EditorGUILayout.ObjectField(
                "网格预览器", _gridPreview, typeof(GrayboxGridPreview), true);
            
            if (EditorGUI.EndChangeCheck() && _roomTemplates != null)
            {
                _roomWidth = _roomTemplates.RoomWidth;
                _roomHeight = _roomTemplates.RoomHeight;
                _wallThickness = _roomTemplates.WallThickness;
                _passageWidth = _roomTemplates.PassageWidth;
            }
            
            if (_roomTemplates == null)
            {
                EditorGUILayout.HelpBox("请在场景中创建GrayboxRoomTemplates组件并拖入此处", UnityEditor.MessageType.Info);
                
                if (GUILayout.Button("在场景中查找"))
                {
                    _roomTemplates = FindObjectOfType<GrayboxRoomTemplates>();
                    _gridPreview = FindObjectOfType<GrayboxGridPreview>();
                }
            }
        }
        
        /// <summary>
        /// 绘制单房间预览部分
        /// </summary>
        private void DrawSingleRoomSection()
        {
            EditorGUILayout.LabelField("单房间预览", EditorStyles.boldLabel);
            
            _selectedRoomType = (RoomType)EditorGUILayout.EnumPopup("房间类型", _selectedRoomType);
            _previewOrigin = EditorGUILayout.Vector2IntField("生成原点", _previewOrigin);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = _roomTemplates != null;
            
            if (GUILayout.Button("生成房间", GUILayout.Height(30)))
            {
                GenerateSingleRoom();
            }
            
            if (GUILayout.Button("清除", GUILayout.Height(30)))
            {
                ClearTilemap();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // 快速房间类型按钮
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("快速选择:");
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start")) { _selectedRoomType = RoomType.Start; GenerateSingleRoom(); }
            if (GUILayout.Button("Exit")) { _selectedRoomType = RoomType.Exit; GenerateSingleRoom(); }
            if (GUILayout.Button("LR")) { _selectedRoomType = RoomType.LR; GenerateSingleRoom(); }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Drop")) { _selectedRoomType = RoomType.Drop; GenerateSingleRoom(); }
            if (GUILayout.Button("Landing")) { _selectedRoomType = RoomType.Landing; GenerateSingleRoom(); }
            if (GUILayout.Button("Side")) { _selectedRoomType = RoomType.Side; GenerateSingleRoom(); }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Shop")) { _selectedRoomType = RoomType.Shop; GenerateSingleRoom(); }
            if (GUILayout.Button("Abyss")) { _selectedRoomType = RoomType.Abyss; GenerateSingleRoom(); }
            if (GUILayout.Button("Boss")) { _selectedRoomType = RoomType.Boss; GenerateSingleRoom(); }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制网格预览部分
        /// </summary>
        private void DrawGridPreviewSection()
        {
            EditorGUILayout.LabelField("4×4网格预览", EditorStyles.boldLabel);
            
            _shapePreset = (GrayboxGridPreview.LevelShapePresetType)EditorGUILayout.EnumPopup(
                "关卡形状", _shapePreset);
            
            if (_shapePreset == GrayboxGridPreview.LevelShapePresetType.Custom)
            {
                _customPattern = EditorGUILayout.TextField("自定义形状", _customPattern);
                EditorGUILayout.HelpBox("格式: 0010,1111,0111,0000 (逗号分隔行)", UnityEditor.MessageType.Info);
            }
            
            // 显示形状预览
            DrawShapePreview();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = _gridPreview != null;
            
            if (GUILayout.Button("生成网格预览", GUILayout.Height(30)))
            {
                GenerateGridPreview();
            }
            
            if (GUILayout.Button("清除网格", GUILayout.Height(30)))
            {
                ClearGridPreview();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制形状预览 (可点击切换)
        /// </summary>
        private void DrawShapePreview()
        {
            // 同步当前形状到可编辑数组（仅在非自定义模式时）
            if (_shapePreset != GrayboxGridPreview.LevelShapePresetType.Custom)
            {
                SyncPresetToEditableShape();
            }
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("点击格子切换启用/禁用:", EditorStyles.miniLabel);
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    bool isValid = _editableShape[x, y] == 1;
                    GUI.backgroundColor = isValid ? Color.green : Color.gray;
                    
                    // 可点击的按钮
                    if (GUILayout.Button(isValid ? "■" : "□", GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        // 切换格子状态
                        _editableShape[x, y] = isValid ? 0 : 1;
                        
                        // 自动切换到自定义模式
                        _shapePreset = GrayboxGridPreview.LevelShapePresetType.Custom;
                        
                        // 更新自定义形状字符串
                        UpdateCustomPatternFromEditableShape();
                    }
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            GUI.backgroundColor = Color.white;
            
            // 显示有效格子数
            int validCount = CountValidCells();
            EditorGUILayout.LabelField($"有效格子数: {validCount}/16", EditorStyles.miniLabel);
            
            // 快捷操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                SetAllCells(true);
            }
            if (GUILayout.Button("清空", GUILayout.Width(60)))
            {
                SetAllCells(false);
            }
            if (GUILayout.Button("反选", GUILayout.Width(60)))
            {
                InvertCells();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 将预设形状同步到可编辑数组
        /// </summary>
        private void SyncPresetToEditableShape()
        {
            LevelShape preset = _shapePreset switch
            {
                GrayboxGridPreview.LevelShapePresetType.FullSquare => LevelShapePresets.FullSquare,
                GrayboxGridPreview.LevelShapePresetType.LShape => LevelShapePresets.LShape,
                GrayboxGridPreview.LevelShapePresetType.TShape => LevelShapePresets.TShape,
                GrayboxGridPreview.LevelShapePresetType.CrossShape => LevelShapePresets.CrossShape,
                GrayboxGridPreview.LevelShapePresetType.ZShape => LevelShapePresets.ZShape,
                GrayboxGridPreview.LevelShapePresetType.VerticalStrip => LevelShapePresets.VerticalStrip,
                GrayboxGridPreview.LevelShapePresetType.HorizontalStrip => LevelShapePresets.HorizontalStrip,
                GrayboxGridPreview.LevelShapePresetType.DiagonalShape => LevelShapePresets.DiagonalShape,
                GrayboxGridPreview.LevelShapePresetType.UserExample => LevelShapePresets.UserExample,
                _ => LevelShapePresets.FullSquare
            };
            
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    _editableShape[x, y] = preset.OccupancyMask[x, y];
                }
            }
        }
        
        /// <summary>
        /// 从可编辑数组更新自定义形状字符串
        /// </summary>
        private void UpdateCustomPatternFromEditableShape()
        {
            var rows = new string[4];
            for (int y = 0; y < 4; y++)
            {
                rows[y] = "";
                for (int x = 0; x < 4; x++)
                {
                    rows[y] += _editableShape[x, y] == 1 ? "1" : "0";
                }
            }
            _customPattern = string.Join(",", rows);
        }
        
        /// <summary>
        /// 计算有效格子数
        /// </summary>
        private int CountValidCells()
        {
            int count = 0;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (_editableShape[x, y] == 1) count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// 设置所有格子
        /// </summary>
        private void SetAllCells(bool valid)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    _editableShape[x, y] = valid ? 1 : 0;
                }
            }
            _shapePreset = GrayboxGridPreview.LevelShapePresetType.Custom;
            UpdateCustomPatternFromEditableShape();
        }
        
        /// <summary>
        /// 反选所有格子
        /// </summary>
        private void InvertCells()
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    _editableShape[x, y] = _editableShape[x, y] == 1 ? 0 : 1;
                }
            }
            _shapePreset = GrayboxGridPreview.LevelShapePresetType.Custom;
            UpdateCustomPatternFromEditableShape();
        }
        
        /// <summary>
        /// 绘制房间设置部分
        /// </summary>
        private void DrawRoomSettingsSection()
        {
            EditorGUILayout.LabelField("房间尺寸设置", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            _roomWidth = EditorGUILayout.IntSlider("房间宽度", _roomWidth, 16, 64);
            _roomHeight = EditorGUILayout.IntSlider("房间高度", _roomHeight, 16, 64);
            _wallThickness = EditorGUILayout.IntSlider("墙壁厚度", _wallThickness, 1, 4);
            _passageWidth = EditorGUILayout.IntSlider("通道宽度", _passageWidth, 4, 12);
            
            if (EditorGUI.EndChangeCheck() && _roomTemplates != null)
            {
                ApplyRoomSettings();
            }
            
            if (GUILayout.Button("应用设置"))
            {
                ApplyRoomSettings();
            }
        }
        
        /// <summary>
        /// 绘制快速操作部分
        /// </summary>
        private void DrawQuickActionsSection()
        {
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
            
            if (GUILayout.Button("生成所有房间类型预览"))
            {
                GenerateAllRoomTypesPreview();
            }
            
            if (GUILayout.Button("聚焦到Tilemap"))
            {
                FocusOnTilemap();
            }
        }
        
        /// <summary>
        /// 生成单个房间
        /// </summary>
        private void GenerateSingleRoom()
        {
            if (_roomTemplates == null) return;
            
            _roomTemplates.ClearTilemap();
            _roomTemplates.GenerateRoomTemplate(_selectedRoomType, _previewOrigin);
            
            // 更新生成状态
            _lastGeneratedRoom = _selectedRoomType.ToString();
            _generatedRoomCount = 1;
            _lastGenerationTime = System.DateTime.Now.ToString("HH:mm:ss");
            
            SceneView.RepaintAll();
            Repaint();
        }
        
        /// <summary>
        /// 清除Tilemap
        /// </summary>
        private void ClearTilemap()
        {
            if (_roomTemplates != null)
            {
                _roomTemplates.ClearTilemap();
            }
            
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 生成网格预览
        /// </summary>
        private void GenerateGridPreview()
        {
            if (_gridPreview == null) return;
            
            _gridPreview.ShapePreset = _shapePreset;
            _gridPreview.CustomShapePattern = _customPattern;
            _gridPreview.GenerateGridPreview();
            
            // 更新生成状态
            LevelShape shape = GetCurrentShape();
            _lastGeneratedRoom = $"网格预览 ({_shapePreset})";
            _generatedRoomCount = shape.GetValidCellCount();
            _lastGenerationTime = System.DateTime.Now.ToString("HH:mm:ss");
            
            SceneView.RepaintAll();
            Repaint();
        }
        
        /// <summary>
        /// 清除网格预览
        /// </summary>
        private void ClearGridPreview()
        {
            if (_gridPreview != null)
            {
                _gridPreview.ClearPreview();
            }
            
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 应用房间设置
        /// </summary>
        private void ApplyRoomSettings()
        {
            if (_roomTemplates == null) return;
            
            Undo.RecordObject(_roomTemplates, "Apply Room Settings");
            
            _roomTemplates.RoomWidth = _roomWidth;
            _roomTemplates.RoomHeight = _roomHeight;
            _roomTemplates.WallThickness = _wallThickness;
            _roomTemplates.PassageWidth = _passageWidth;
            
            EditorUtility.SetDirty(_roomTemplates);
        }
        
        /// <summary>
        /// 获取当前选择的形状
        /// </summary>
        private LevelShape GetCurrentShape()
        {
            return _shapePreset switch
            {
                GrayboxGridPreview.LevelShapePresetType.FullSquare => LevelShapePresets.FullSquare,
                GrayboxGridPreview.LevelShapePresetType.LShape => LevelShapePresets.LShape,
                GrayboxGridPreview.LevelShapePresetType.TShape => LevelShapePresets.TShape,
                GrayboxGridPreview.LevelShapePresetType.CrossShape => LevelShapePresets.CrossShape,
                GrayboxGridPreview.LevelShapePresetType.ZShape => LevelShapePresets.ZShape,
                GrayboxGridPreview.LevelShapePresetType.VerticalStrip => LevelShapePresets.VerticalStrip,
                GrayboxGridPreview.LevelShapePresetType.HorizontalStrip => LevelShapePresets.HorizontalStrip,
                GrayboxGridPreview.LevelShapePresetType.DiagonalShape => LevelShapePresets.DiagonalShape,
                GrayboxGridPreview.LevelShapePresetType.UserExample => LevelShapePresets.UserExample,
                GrayboxGridPreview.LevelShapePresetType.Custom => LevelShape.FromString(_customPattern),
                _ => LevelShapePresets.FullSquare
            };
        }
        
        /// <summary>
        /// 生成所有房间类型预览
        /// </summary>
        private void GenerateAllRoomTypesPreview()
        {
            if (_roomTemplates == null) return;
            
            _roomTemplates.ClearTilemap();
            
            int spacing = _roomWidth + 4;
            int col = 0;
            int row = 0;
            
            foreach (RoomType roomType in System.Enum.GetValues(typeof(RoomType)))
            {
                if (roomType == RoomType.None) continue;
                
                Vector2Int origin = new Vector2Int(col * spacing, row * spacing);
                _roomTemplates.GenerateRoomTemplate(roomType, origin);
                
                col++;
                if (col >= 3)
                {
                    col = 0;
                    row++;
                }
            }
            
            // 更新生成状态
            _lastGeneratedRoom = "所有房间类型";
            _generatedRoomCount = 9; // 9种房间类型
            _lastGenerationTime = System.DateTime.Now.ToString("HH:mm:ss");
            
            SceneView.RepaintAll();
            Repaint();
            Debug.Log("已生成所有房间类型预览");
        }
        
        /// <summary>
        /// 聚焦到Tilemap
        /// </summary>
        private void FocusOnTilemap()
        {
            if (_roomTemplates != null && _roomTemplates.TargetTilemap != null)
            {
                Selection.activeGameObject = _roomTemplates.TargetTilemap.gameObject;
                SceneView.FrameLastActiveSceneView();
            }
        }
    }
}
