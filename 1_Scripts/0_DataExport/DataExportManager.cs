using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 数据导出管理器
/// 统一管理所有数据导出器，提供批量导出功能
/// </summary>
public class DataExportManager
{
    private static DataExportManager instance;
    public static DataExportManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new DataExportManager();
            }
            return instance;
        }
    }

    private List<IDataExporter> exporters = new List<IDataExporter>();

    /// <summary>
    /// 导出目录路径
    /// </summary>
    public static string ExportPath => Path.Combine(Application.dataPath, "Resources", "DataExports");

    /// <summary>
    /// 注册导出器
    /// </summary>
    public void RegisterExporter(IDataExporter exporter)
    {
        if (exporter == null)
        {
            Debug.LogWarning("[DataExportManager] 尝试注册空导出器");
            return;
        }

        if (!exporters.Contains(exporter))
        {
            exporters.Add(exporter);
            Debug.Log($"[DataExportManager] 注册导出器: {exporter.ExporterName}");
        }
    }

    /// <summary>
    /// 取消注册导出器
    /// </summary>
    public void UnregisterExporter(IDataExporter exporter)
    {
        if (exporters.Contains(exporter))
        {
            exporters.Remove(exporter);
            Debug.Log($"[DataExportManager] 取消注册导出器: {exporter.ExporterName}");
        }
    }

    /// <summary>
    /// 导出所有已注册的导出器数据
    /// </summary>
    public void ExportAll()
    {
        // 确保导出目录存在
        if (!Directory.Exists(ExportPath))
        {
            Directory.CreateDirectory(ExportPath);
            Debug.Log($"[DataExportManager] 创建导出目录: {ExportPath}");
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var exporter in exporters)
        {
            if (!exporter.IsEnabled)
            {
                Debug.Log($"[DataExportManager] 跳过已禁用的导出器: {exporter.ExporterName}");
                continue;
            }

            try
            {
                string json = exporter.ExportToJson();
                string fileName = $"{exporter.ExporterName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(ExportPath, fileName);

                File.WriteAllText(filePath, json);
                Debug.Log($"[DataExportManager] 导出成功: {fileName}");
                successCount++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataExportManager] 导出失败 ({exporter.ExporterName}): {e.Message}");
                failCount++;
            }
        }

        Debug.Log($"[DataExportManager] 导出完成 - 成功: {successCount}, 失败: {failCount}");

#if UNITY_EDITOR
        // 刷新资源数据库
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// 获取所有已注册的导出器
    /// </summary>
    public List<IDataExporter> GetAllExporters()
    {
        return new List<IDataExporter>(exporters);
    }

    /// <summary>
    /// 清空所有导出器
    /// </summary>
    public void ClearExporters()
    {
        exporters.Clear();
        Debug.Log("[DataExportManager] 已清空所有导出器");
    }
}
