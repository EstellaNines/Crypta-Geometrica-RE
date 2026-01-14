using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// GameManager 模块管理窗口
/// 通过菜单栏 Window/Game Framework/Module Manager 打开
/// </summary>
public class GameManagerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private GameManager gameManager;

    // 可添加的模块类型列表
    private List<Type> availableModuleTypes;
    private int selectedModuleIndex = 0;

    // 刷新计时
    private double lastRefreshTime;
    private const double REFRESH_INTERVAL = 0.5;

    [MenuItem("Crypta Geometrica: RE/Game Framework/Module Manager", false, 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<GameManagerWindow>("模块管理器");
        window.minSize = new Vector2(350, 400);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshAvailableModules();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnHierarchyChanged()
    {
        gameManager = null; // 强制重新查找
        Repaint();
    }

    private void OnGUI()
    {
        // 定时刷新
        if (EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawGameManagerStatus();
        EditorGUILayout.Space(10);

        DrawModuleList();
        EditorGUILayout.Space(10);

        DrawAddModuleSection();
        EditorGUILayout.Space(10);

        DrawQuickActions();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制标题栏
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("GameManager 模块管理器", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            gameManager = null;
            RefreshAvailableModules();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制 GameManager 状态
    /// </summary>
    private void DrawGameManagerStatus()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("GameManager 状态", EditorStyles.boldLabel);

        FindGameManager();

        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 GameManager！", UnityEditor.MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建 GameManager 结构", GUILayout.Height(30)))
            {
                GameManagerCreator.CreateGameManagerStructure();
                gameManager = null; // 强制重新查找
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("实例:", GUILayout.Width(60));
            EditorGUILayout.ObjectField(gameManager, typeof(GameManager), true);
            EditorGUILayout.EndHorizontal();

            var modules = GetCurrentModules();
            EditorGUILayout.LabelField($"已挂载模块: {modules.Count} 个");

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("状态: 运行中", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制模块列表
    /// </summary>
    private void DrawModuleList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("已挂载模块", EditorStyles.boldLabel);

        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("请先创建 GameManager", UnityEditor.MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var modules = GetCurrentModules();

        if (modules.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无模块，请使用下方区域添加", UnityEditor.MessageType.Info);
        }
        else
        {
            for (int i = 0; i < modules.Count; i++)
            {
                DrawModuleItem(modules[i], i);
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制单个模块项
    /// </summary>
    private void DrawModuleItem(MonoBehaviour module, int index)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // 启用开关
        bool isEnabled = module.enabled;
        bool newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
        if (newEnabled != isEnabled)
        {
            Undo.RecordObject(module, "Toggle Module");
            module.enabled = newEnabled;
        }

        // 序号和名称
        GUILayout.Label($"{index + 1}.", GUILayout.Width(20));
        EditorGUILayout.LabelField(module.GetType().Name, EditorStyles.boldLabel);

        // 定位按钮
        if (GUILayout.Button("定位", GUILayout.Width(45)))
        {
            Selection.activeGameObject = module.gameObject;
            EditorGUIUtility.PingObject(module.gameObject);
        }

        // 删除按钮
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("确认删除",
                $"删除模块 [{module.GetType().Name}]？", "删除", "取消"))
            {
                Undo.DestroyObjectImmediate(module.gameObject);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制添加模块区域
    /// </summary>
    private void DrawAddModuleSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("添加模块", EditorStyles.boldLabel);

        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("请先创建 GameManager", UnityEditor.MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (availableModuleTypes == null || availableModuleTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到可用模块类型", UnityEditor.MessageType.Warning);
            if (GUILayout.Button("刷新"))
            {
                RefreshAvailableModules();
            }
            EditorGUILayout.EndVertical();
            return;
        }

        // 过滤已存在的
        var existingTypes = GetCurrentModules().Select(m => m.GetType()).ToList();
        var available = availableModuleTypes.Where(t => !existingTypes.Contains(t)).ToList();

        if (available.Count == 0)
        {
            EditorGUILayout.HelpBox("所有模块已添加", UnityEditor.MessageType.Info);
        }
        else
        {
            string[] names = available.Select(t => t.Name).ToArray();

            if (selectedModuleIndex >= available.Count)
                selectedModuleIndex = 0;

            EditorGUILayout.BeginHorizontal();
            selectedModuleIndex = EditorGUILayout.Popup("选择模块", selectedModuleIndex, names);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button($"添加 {names[selectedModuleIndex]}", GUILayout.Height(28)))
            {
                AddModule(available[selectedModuleIndex]);
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制快捷操作
    /// </summary>
    private void DrawQuickActions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

        if (gameManager == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("全部启用", GUILayout.Height(25)))
        {
            SetAllModulesEnabled(true);
        }

        if (GUILayout.Button("全部禁用", GUILayout.Height(25)))
        {
            SetAllModulesEnabled(false);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("选中 GameManager", GUILayout.Height(25)))
        {
            Selection.activeGameObject = gameManager.gameObject;
            EditorGUIUtility.PingObject(gameManager.gameObject);
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            if (GUILayout.Button("打印模块信息 (Console)", GUILayout.Height(25)))
            {
                gameManager.PrintModules();
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 查找 GameManager
    /// </summary>
    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    /// <summary>
    /// 获取当前模块列表
    /// </summary>
    private List<MonoBehaviour> GetCurrentModules()
    {
        var result = new List<MonoBehaviour>();
        if (gameManager == null) return result;

        var children = gameManager.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var child in children)
        {
            if (child is IGameModule && !(child is GameManager))
            {
                result.Add(child);
            }
        }
        return result;
    }

    /// <summary>
    /// 刷新可用模块类型
    /// </summary>
    private void RefreshAvailableModules()
    {
        availableModuleTypes = new List<Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => typeof(IGameModule).IsAssignableFrom(t)
                                && typeof(MonoBehaviour).IsAssignableFrom(t)
                                && !t.IsAbstract
                                && t != typeof(GameManager));
                availableModuleTypes.AddRange(types);
            }
            catch { }
        }
    }

    /// <summary>
    /// 添加模块
    /// </summary>
    private void AddModule(Type moduleType)
    {
        if (gameManager == null) return;

        string name = $"[{moduleType.Name}]";
        GameObject obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, $"Add {moduleType.Name}");

        obj.transform.SetParent(gameManager.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.AddComponent(moduleType);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[GameManager] 添加模块: {moduleType.Name}");
    }

    /// <summary>
    /// 设置所有模块启用状态
    /// </summary>
    private void SetAllModulesEnabled(bool enabled)
    {
        var modules = GetCurrentModules();
        foreach (var m in modules)
        {
            Undo.RecordObject(m, enabled ? "Enable All" : "Disable All");
            m.enabled = enabled;
        }
    }
}
