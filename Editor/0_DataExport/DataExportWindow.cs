using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 数据导出窗口
/// 提供可视化界面选择和导出系统数据
/// </summary>
public class DataExportWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<string, bool> exporterSelection = new Dictionary<string, bool>();
    private List<IDataExporter> availableExporters = new List<IDataExporter>();

    [MenuItem("Crypta Geometrica: RE/Data Export Tool")]
    public static void ShowWindow()
    {
        DataExportWindow window = GetWindow<DataExportWindow>("Data Export Tool");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        InitializeExporters();
    }

    private void InitializeExporters()
    {
        availableExporters.Clear();
        exporterSelection.Clear();

        // 注册所有可用的导出器
        availableExporters.Add(new MessageManagerExporter());

        // 初始化选择状态（默认全选）
        foreach (var exporter in availableExporters)
        {
            exporterSelection[exporter.ExporterName] = true;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // 标题
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Data Export Tool", titleStyle);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Select systems to export", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);

        // 导出器选择区域
        EditorGUILayout.LabelField("Available Exporters", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (availableExporters.Count == 0)
        {
            EditorGUILayout.HelpBox("No exporters available. Please check your configuration.", UnityEditor.MessageType.Warning);
        }
        else
        {
            foreach (var exporter in availableExporters)
            {
                DrawExporterItem(exporter);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);

        // 操作按钮区域
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Select All", GUILayout.Height(30)))
        {
            SelectAll(true);
        }

        if (GUILayout.Button("Deselect All", GUILayout.Height(30)))
        {
            SelectAll(false);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 导出按钮
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("Export Selected Systems", GUILayout.Height(40)))
        {
            ExportSelectedSystems();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // 底部工具栏
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Open Export Folder", GUILayout.Height(25)))
        {
            OpenExportFolder();
        }

        if (GUILayout.Button("Clear Export Folder", GUILayout.Height(25)))
        {
            ClearExportFolder();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 导出路径信息
        EditorGUILayout.LabelField("Export Path:", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(DataExportManager.ExportPath, EditorStyles.textField, GUILayout.Height(20));
    }

    private void DrawExporterItem(IDataExporter exporter)
    {
        EditorGUILayout.BeginHorizontal("box");

        // 复选框
        bool isSelected = exporterSelection[exporter.ExporterName];
        bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
        exporterSelection[exporter.ExporterName] = newSelection;

        // 导出器名称
        EditorGUILayout.LabelField(exporter.ExporterName, EditorStyles.boldLabel);

        // 状态指示
        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
        if (exporter.IsEnabled)
        {
            statusStyle.normal.textColor = Color.green;
            EditorGUILayout.LabelField("Enabled", statusStyle, GUILayout.Width(60));
        }
        else
        {
            statusStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField("Disabled", statusStyle, GUILayout.Width(60));
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
    }

    private void SelectAll(bool select)
    {
        foreach (var exporter in availableExporters)
        {
            exporterSelection[exporter.ExporterName] = select;
        }
        Repaint();
    }

    private void ExportSelectedSystems()
    {
        // 清空管理器
        DataExportManager.Instance.ClearExporters();

        // 注册选中的导出器
        int selectedCount = 0;
        foreach (var exporter in availableExporters)
        {
            if (exporterSelection[exporter.ExporterName] && exporter.IsEnabled)
            {
                DataExportManager.Instance.RegisterExporter(exporter);
                selectedCount++;
            }
        }

        if (selectedCount == 0)
        {
            EditorUtility.DisplayDialog(
                "No Exporters Selected",
                "Please select at least one exporter.",
                "OK"
            );
            return;
        }

        // 执行导出
        DataExportManager.Instance.ExportAll();

        // 显示完成提示
        EditorUtility.DisplayDialog(
            "Export Complete",
            $"Successfully exported {selectedCount} system(s) to:\n{DataExportManager.ExportPath}",
            "OK"
        );
    }

    private void OpenExportFolder()
    {
        string path = DataExportManager.ExportPath;
        
        if (System.IO.Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Folder Not Found",
                "Export folder does not exist yet. Please export data first.",
                "OK"
            );
        }
    }

    private void ClearExportFolder()
    {
        string path = DataExportManager.ExportPath;
        
        if (!System.IO.Directory.Exists(path))
        {
            EditorUtility.DisplayDialog(
                "Folder Not Found",
                "Export folder does not exist.",
                "OK"
            );
            return;
        }

        if (EditorUtility.DisplayDialog(
            "Clear Export Folder",
            "Are you sure you want to delete all exported JSON files?",
            "Yes",
            "Cancel"))
        {
            string[] files = System.IO.Directory.GetFiles(path, "*.json");
            foreach (string file in files)
            {
                System.IO.File.Delete(file);
            }
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Clear Complete",
                $"Deleted {files.Length} file(s).",
                "OK"
            );
        }
    }
}
