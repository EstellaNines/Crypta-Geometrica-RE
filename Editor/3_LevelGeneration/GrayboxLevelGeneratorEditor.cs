using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using CryptaGeometrica.LevelGeneration;
using CryptaGeometrica.LevelGeneration.Graybox;

namespace CryptaGeometrica.Editor.LevelGeneration
{
    /// <summary>
    /// 灰盒关卡生成器自定义Inspector
    /// </summary>
    [CustomEditor(typeof(GrayboxLevelGenerator))]
    public class GrayboxLevelGeneratorEditor : UnityEditor.Editor
    {
        private GrayboxLevelGenerator _generator;
        private GrayboxGridPreview.LevelShapePresetType _shapePreset = GrayboxGridPreview.LevelShapePresetType.FullSquare;
        
        // 可编辑形状
        private int[,] _editableShape = new int[4, 4] {
            {1, 1, 1, 1},
            {1, 1, 1, 1},
            {1, 1, 1, 1},
            {1, 1, 1, 1}
        };
        
        private bool _showShapeEditor = true;
        private bool _showColorLegend = true;
        
        private void OnEnable()
        {
            _generator = (GrayboxLevelGenerator)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 绘制默认Inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("关卡形状编辑器", EditorStyles.boldLabel);
            
            // 形状预设选择
            _shapePreset = (GrayboxGridPreview.LevelShapePresetType)EditorGUILayout.EnumPopup(
                "形状预设", _shapePreset);
            
            if (_shapePreset != GrayboxGridPreview.LevelShapePresetType.Custom)
            {
                if (GUILayout.Button("加载预设"))
                {
                    LoadPreset();
                }
            }
            
            // 形状编辑器
            _showShapeEditor = EditorGUILayout.Foldout(_showShapeEditor, "4×4形状编辑 (点击切换)", true);
            
            if (_showShapeEditor)
            {
                DrawShapeEditor();
            }
            
            EditorGUILayout.Space(10);
            
            // 生成按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("生成关卡", GUILayout.Height(35)))
            {
                GenerateLevel();
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("清除关卡", GUILayout.Height(35)))
            {
                _generator.ClearLevel();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 颜色图例
            _showColorLegend = EditorGUILayout.Foldout(_showColorLegend, "瓦片颜色图例", true);
            
            if (_showColorLegend)
            {
                DrawColorLegend();
            }
            
            EditorGUILayout.Space(10);
            
            // 快速设置
            DrawQuickSetup();
        }
        
        /// <summary>
        /// 绘制形状编辑器
        /// </summary>
        private void DrawShapeEditor()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("点击格子切换启用/禁用:", EditorStyles.miniLabel);
            
            for (int y = 0; y < 4; y++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                for (int x = 0; x < 4; x++)
                {
                    bool isValid = _editableShape[x, y] == 1;
                    GUI.backgroundColor = isValid ? Color.green : Color.gray;
                    
                    if (GUILayout.Button(isValid ? "■" : "□", GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        _editableShape[x, y] = isValid ? 0 : 1;
                        _shapePreset = GrayboxGridPreview.LevelShapePresetType.Custom;
                    }
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            GUI.backgroundColor = Color.white;
            
            // 有效格子数
            int validCount = CountValidCells();
            EditorGUILayout.LabelField($"有效格子数: {validCount}/16", EditorStyles.miniLabel);
            
            // 快捷按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60))) SetAllCells(true);
            if (GUILayout.Button("清空", GUILayout.Width(60))) SetAllCells(false);
            if (GUILayout.Button("反选", GUILayout.Width(60))) InvertCells();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制颜色图例
        /// </summary>
        private void DrawColorLegend()
        {
            EditorGUILayout.BeginVertical("box");
            
            DrawColorRow(Color.red, "红色 - 外围墙壁 (4×4边界)");
            DrawColorRow(new Color(1f, 0.5f, 0f), "橙色 - 洞穴填充 (随机生成)");
            DrawColorRow(Color.blue, "蓝色 - 平台 (可站立)");
            DrawColorRow(Color.green, "绿色 - 入口 (Start房间)");
            DrawColorRow(Color.black, "黑色 - 出口 (Exit房间)");
            DrawColorRow(Color.yellow, "黄色 - 特殊区域 (Boss/Shop)");
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制颜色行
        /// </summary>
        private void DrawColorRow(Color color, string description)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = color;
            GUILayout.Box("", GUILayout.Width(25), GUILayout.Height(18));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField(description);
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制快速设置
        /// </summary>
        private void DrawQuickSetup()
        {
            EditorGUILayout.LabelField("快速设置", EditorStyles.boldLabel);
            
            if (GUILayout.Button("创建Tilemap层级结构"))
            {
                CreateTilemapHierarchy();
            }
            
            EditorGUILayout.HelpBox(
                "点击上方按钮自动创建Grid和2层Tilemap:\n" +
                "- GroundLayer (地面层 - 墙壁和填充合并)\n" +
                "- PlatformLayer (平台层)", 
                UnityEditor.MessageType.Info);
        }
        
        /// <summary>
        /// 创建Tilemap层级结构（简化版）
        /// </summary>
        private void CreateTilemapHierarchy()
        {
            // 创建Grid
            GameObject gridObj = new GameObject("GrayboxGrid");
            var grid = gridObj.AddComponent<Grid>();
            
            // 创建2层Tilemap（简化版）
            string[] layerNames = { "GroundLayer", "PlatformLayer" };
            int[] sortingOrders = { 0, 1 };
            
            Tilemap[] tilemaps = new Tilemap[2];
            
            for (int i = 0; i < layerNames.Length; i++)
            {
                GameObject tilemapObj = new GameObject(layerNames[i]);
                tilemapObj.transform.SetParent(gridObj.transform);
                
                var tilemap = tilemapObj.AddComponent<Tilemap>();
                var renderer = tilemapObj.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = sortingOrders[i];
                
                // 添加 TilemapCollider2D
                var collider = tilemapObj.AddComponent<TilemapCollider2D>();
                
                tilemaps[i] = tilemap;
            }
            
            // 初始化TilemapLayers
            if (_generator.TilemapLayers == null)
            {
                _generator.TilemapLayers = new GrayboxTilemapLayers();
            }
            
            _generator.TilemapLayers.GroundLayer = tilemaps[0];
            _generator.TilemapLayers.PlatformLayer = tilemaps[1];
            
            EditorUtility.SetDirty(_generator);
            
            Debug.Log("Tilemap层级结构已创建（简化版：GroundLayer + PlatformLayer）!");
            
            // 选中Grid
            Selection.activeGameObject = gridObj;
        }
        
        /// <summary>
        /// 加载预设
        /// </summary>
        private void LoadPreset()
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
        /// 生成关卡
        /// </summary>
        private void GenerateLevel()
        {
            LevelShape shape = LevelShape.FromArray(_editableShape);
            _generator.GenerateLevel(shape);
            SceneView.RepaintAll();
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
        }
    }
}
