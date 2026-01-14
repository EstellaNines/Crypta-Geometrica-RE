using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// GameManager 自定义 Inspector
/// 提供模块可视化管理界面
/// </summary>
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private GameManager gameManager;
    private bool showModulesFoldout = true;
    private bool showAddModuleFoldout = false;
    private Vector2 scrollPosition;

    // 可添加的模块类型列表
    private static List<Type> availableModuleTypes;
    private int selectedModuleIndex = 0;

    private void OnEnable()
    {
        gameManager = (GameManager)target;
        RefreshAvailableModules();
    }

    /// <summary>
    /// 刷新可用模块类型列表
    /// </summary>
    private void RefreshAvailableModules()
    {
        availableModuleTypes = new List<Type>();

        // 获取所有实现 IGameModule 接口的 MonoBehaviour 类型
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
            catch (Exception)
            {
                // 忽略无法加载的程序集
            }
        }
    }

    public override void OnInspectorGUI()
    {
        // 标题
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GameManager 模块管理器", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 分隔线
        DrawSeparator();

        // 当前模块列表
        DrawModuleList();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // 添加模块区域
        DrawAddModuleSection();

        EditorGUILayout.Space(10);
        DrawSeparator();

        // 操作按钮
        DrawActionButtons();

        // 应用修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    /// <summary>
    /// 绘制当前模块列表
    /// </summary>
    private void DrawModuleList()
    {
        var modules = GetCurrentModules();

        showModulesFoldout = EditorGUILayout.Foldout(showModulesFoldout,
            $"已挂载模块 ({modules.Count})", true, EditorStyles.foldoutHeader);

        if (!showModulesFoldout) return;

        EditorGUI.indentLevel++;

        if (modules.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有挂载任何模块。\n使用下方「添加模块」区域来添加模块。", UnityEditor.MessageType.Info);
        }
        else
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));

            for (int i = 0; i < modules.Count; i++)
            {
                DrawModuleItem(modules[i], i);
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制单个模块项
    /// </summary>
    private void DrawModuleItem(MonoBehaviour module, int index)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // 启用/禁用开关
        bool isEnabled = module.enabled;
        bool newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
        if (newEnabled != isEnabled)
        {
            Undo.RecordObject(module, "Toggle Module");
            module.enabled = newEnabled;
        }

        // 模块名称和类型
        EditorGUILayout.LabelField($"{index + 1}. {module.GetType().Name}", EditorStyles.boldLabel);

        // 选择按钮
        if (GUILayout.Button("选择", GUILayout.Width(50)))
        {
            Selection.activeGameObject = module.gameObject;
            EditorGUIUtility.PingObject(module.gameObject);
        }

        // 删除按钮
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("删除", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("确认删除",
                $"确定要删除模块 [{module.GetType().Name}] 吗？\n这将删除整个子对象。",
                "删除", "取消"))
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
        showAddModuleFoldout = EditorGUILayout.Foldout(showAddModuleFoldout,
            "添加模块", true, EditorStyles.foldoutHeader);

        if (!showAddModuleFoldout) return;

        EditorGUI.indentLevel++;

        if (availableModuleTypes == null || availableModuleTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到可用的模块类型。\n请确保有实现 IGameModule 接口的 MonoBehaviour 类。", UnityEditor.MessageType.Warning);

            if (GUILayout.Button("刷新模块列表"))
            {
                RefreshAvailableModules();
            }
        }
        else
        {
            // 过滤已存在的模块
            var existingTypes = GetCurrentModules().Select(m => m.GetType()).ToList();
            var availableToAdd = availableModuleTypes.Where(t => !existingTypes.Contains(t)).ToList();

            if (availableToAdd.Count == 0)
            {
                EditorGUILayout.HelpBox("所有可用模块都已添加。", UnityEditor.MessageType.Info);
            }
            else
            {
                string[] moduleNames = availableToAdd.Select(t => t.Name).ToArray();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选择模块:", GUILayout.Width(80));
                selectedModuleIndex = EditorGUILayout.Popup(selectedModuleIndex, moduleNames);
                EditorGUILayout.EndHorizontal();

                // 确保索引有效
                if (selectedModuleIndex >= availableToAdd.Count)
                    selectedModuleIndex = 0;

                EditorGUILayout.Space(5);

                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                if (GUILayout.Button($"添加 {moduleNames[selectedModuleIndex]}", GUILayout.Height(25)))
                {
                    AddModule(availableToAdd[selectedModuleIndex]);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("刷新模块列表"))
            {
                RefreshAvailableModules();
            }
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制操作按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

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

        // 运行时信息
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("游戏运行中 - 模块已初始化", UnityEditor.MessageType.Info);

            if (GUILayout.Button("打印模块信息"))
            {
                gameManager.PrintModules();
            }
        }
    }

    /// <summary>
    /// 绘制分隔线
    /// </summary>
    private void DrawSeparator()
    {
        EditorGUILayout.Space(2);
        var rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(2);
    }

    /// <summary>
    /// 获取当前所有模块
    /// </summary>
    private List<MonoBehaviour> GetCurrentModules()
    {
        var modules = new List<MonoBehaviour>();

        if (gameManager == null) return modules;

        var childModules = gameManager.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var module in childModules)
        {
            if (module is IGameModule && !(module is GameManager))
            {
                modules.Add(module);
            }
        }

        return modules;
    }

    /// <summary>
    /// 添加模块
    /// </summary>
    private void AddModule(Type moduleType)
    {
        if (gameManager == null) return;

        // 创建子对象
        string moduleName = $"[{moduleType.Name}]";
        GameObject moduleObj = new GameObject(moduleName);

        Undo.RegisterCreatedObjectUndo(moduleObj, $"Add Module {moduleType.Name}");

        moduleObj.transform.SetParent(gameManager.transform);
        moduleObj.transform.localPosition = Vector3.zero;

        // 添加组件
        moduleObj.AddComponent(moduleType);

        // 标记场景已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[GameManager] 已添加模块: {moduleType.Name}");
    }

    /// <summary>
    /// 设置所有模块的启用状态
    /// </summary>
    private void SetAllModulesEnabled(bool enabled)
    {
        var modules = GetCurrentModules();
        foreach (var module in modules)
        {
            Undo.RecordObject(module, enabled ? "Enable All Modules" : "Disable All Modules");
            module.enabled = enabled;
        }

        Debug.Log($"[GameManager] 已{(enabled ? "启用" : "禁用")}所有模块");
    }
}
