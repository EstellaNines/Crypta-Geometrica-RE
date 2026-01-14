# 数据导出系统 - Data Export System

## 概述

可扩展的数据导出架构，用于将各个系统的数据导出为JSON文件进行调试和检查。

## 架构设计

```
┌─────────────────────────────────────────────────────┐
│              DataExportManager                      │
│  统一管理所有数据导出器                              │
├─────────────────────────────────────────────────────┤
│  + RegisterExporter(IDataExporter)                  │
│  + ExportAll()                                      │
│  + GetAllExporters()                                │
└─────────────────────────────────────────────────────┘
                      ↑
                      │ 实现
         ┌────────────┴────────────┐
         │                         │
┌────────────────────┐   ┌────────────────────┐
│ IDataExporter      │   │ IDataExporter      │
│ 接口               │   │ 接口               │
├────────────────────┤   ├────────────────────┤
│ MessageManager     │   │ 其他系统           │
│ Exporter           │   │ Exporter           │
└────────────────────┘   └────────────────────┘
```

## 核心组件

### 1. IDataExporter 接口

所有需要导出数据的系统都应实现此接口。

```csharp
public interface IDataExporter
{
    string ExporterName { get; }    // 导出器名称
    string ExportToJson();          // 导出为JSON
    bool IsEnabled { get; }         // 是否启用
}
```

### 2. DataExportManager 管理器

统一管理所有数据导出器。

**主要方法：**
- `RegisterExporter(IDataExporter)` - 注册导出器
- `ExportAll()` - 导出所有数据
- `GetAllExporters()` - 获取所有导出器

**导出路径：**
`Assets/Resources/DataExports/`

### 3. MessageManagerExporter

MessageManager的数据导出器实现。

**导出内容：**
- 导出时间
- 事件总数
- 永久事件列表
- 每个事件的详细信息（监听器数量、是否永久等）

## 使用方法

### 在Editor中使用

Unity菜单栏：`Tools > Data Export`

**可用选项：**
1. **Export All Systems** - 导出所有系统数据
2. **Open Export Folder** - 打开导出文件夹
3. **Clear Export Folder** - 清空导出文件夹

### 导出文件格式

文件命名：`{ExporterName}_{yyyyMMdd_HHmmss}.json`

示例：`MessageManager_20260114_153045.json`

### MessageManager导出示例

```json
{
    "exportTime": "2026-01-14 15:30:45",
    "totalEvents": 5,
    "permanentEvents": [
        "SCENE_LOADING_START",
        "SCENE_LOADING_PROGRESS",
        "SCENE_LOADING_COMPLETED"
    ],
    "eventDetails": [
        {
            "eventType": "SCENE_LOADING_PROGRESS",
            "listenerCount": 1,
            "isPermanent": true
        },
        {
            "eventType": "GAME_START",
            "listenerCount": 2,
            "isPermanent": false
        }
    ]
}
```

## 如何添加新的导出器

### 步骤1：创建导出器类

```csharp
public class MySystemExporter : IDataExporter
{
    public string ExporterName => "MySystem";
    public bool IsEnabled => true;

    public string ExportToJson()
    {
        var data = new MySystemData
        {
            // 填充数据
        };
        
        return JsonUtility.ToJson(data, true);
    }

    [System.Serializable]
    private class MySystemData
    {
        public string someField;
        public int someValue;
    }
}
```

### 步骤2：注册导出器

在`DataExportEditor.cs`的`ExportAllSystems()`方法中添加：

```csharp
DataExportManager.Instance.RegisterExporter(new MySystemExporter());
```

### 步骤3：导出数据

点击菜单：`Tools > Data Export > Export All Systems`

## 最佳实践

1. **数据结构化**
   - 使用`[System.Serializable]`标记数据类
   - 使用清晰的字段命名

2. **错误处理**
   - 在`ExportToJson()`中捕获异常
   - 返回有意义的错误信息

3. **性能考虑**
   - 避免导出过大的数据
   - 考虑分页或过滤机制

4. **版本控制**
   - 在导出数据中包含版本信息
   - 便于追踪数据格式变化

## 应用场景

- **调试** - 检查系统状态
- **测试** - 验证数据正确性
- **分析** - 分析系统行为
- **文档** - 生成系统状态文档

## 注意事项

1. 导出文件保存在`Resources`文件夹中，会被打包到游戏中
2. 定期清理旧的导出文件
3. 不要在运行时频繁导出，仅用于调试
4. 敏感数据应加密或过滤

## 版本记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-01-14 | 初始版本，支持MessageManager导出 |
