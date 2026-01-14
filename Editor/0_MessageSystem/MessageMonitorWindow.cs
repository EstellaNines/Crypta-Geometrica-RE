#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 全局消息系统监控器窗口
/// 基于Odin Inspector实现实时消息监控
/// </summary>
public class MessageMonitorWindow : OdinEditorWindow
{
    #region 窗口配置

    [MenuItem("Crypta Geometrica: RE/Message Monitor %#E")]
    private static void OpenWindow()
    {
        var window = GetWindow<MessageMonitorWindow>();
        window.titleContent = new GUIContent("Message Monitor", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
        window.minSize = new Vector2(800, 400);
        window.Show();
    }

    #endregion

    #region 状态变量

    private bool isDynamicMode = true;
    private bool isPaused = false;
    private Vector2 scrollPosition;
    private string filterText = "";
    private MessageType? filterMessageType = null;
    private string filterActionType = "All";
    private int maxDisplayCount = 100;
    private bool enableGrouping = true;
    private Dictionary<string, bool> groupFoldoutStates = new Dictionary<string, bool>();

    // 列宽设置
    private float colWidth_Index = 40;
    private float colWidth_Time = 80;
    private float colWidth_Color = 30;
    private float colWidth_Type = 80;
    private float colWidth_Message = 120;
    private float colWidth_Param1 = 100;
    private float colWidth_Param2 = 100;
    private float colWidth_Param3 = 100;
    
    // 拖拽状态
    private int draggingColumnIndex = -1;
    private float dragStartX = 0;
    private float dragStartWidth = 0;

    // 颜色定义
    private readonly Color greenColor = new Color(0.2f, 0.9f, 0.3f);
    private readonly Color redColor = new Color(0.9f, 0.3f, 0.3f);
    private readonly Color yellowColor = new Color(1f, 0.9f, 0.3f);

    #endregion

    #region 生命周期

    protected override void OnEnable()
    {
        base.OnEnable();
        MessageMonitor.OnNewRecord += OnNewRecordReceived;
        EditorApplication.update += OnEditorUpdate;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        MessageMonitor.OnNewRecord -= OnNewRecordReceived;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnNewRecordReceived()
    {
        if (isDynamicMode && !isPaused)
        {
            Repaint();
        }
    }

    private void OnEditorUpdate()
    {
        if (isDynamicMode && !isPaused)
        {
            Repaint();
        }
    }

    #endregion

    #region GUI绘制

    protected override void DrawEditors()
    {
        DrawHeader();
        DrawToolbar();
        DrawFilterBar();
        GUILayout.Space(5);
        DrawRecordTable();
    }

    /// <summary>
    /// 绘制头部状态栏
    /// </summary>
    private void DrawHeader()
    {
        SirenixEditorGUI.BeginBox();
        EditorGUILayout.BeginHorizontal();

        // 标题
        GUILayout.Label("全局消息系统监控器", SirenixGUIStyles.BoldTitle);
        GUILayout.FlexibleSpace();

        // 状态指示灯
        Color statusColor = isDynamicMode ? greenColor : redColor;
        string statusText = isDynamicMode ? "动态模式" : "静态模式";
        
        // 绘制圆点
        Rect dotRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
        EditorGUI.DrawRect(new Rect(dotRect.x + 2, dotRect.y + 2, 12, 12), statusColor);
        
        GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
        statusStyle.normal.textColor = statusColor;
        GUILayout.Label(statusText, statusStyle, GUILayout.Width(70));

        // 暂停指示
        if (isPaused)
        {
            GUIStyle pauseStyle = new GUIStyle(EditorStyles.boldLabel);
            pauseStyle.normal.textColor = yellowColor;
            GUILayout.Label("⏸ 已暂停", pauseStyle, GUILayout.Width(60));
        }

        // 记录数量
        GUILayout.Label($"记录: {MessageMonitor.RecordCount}/{MessageMonitor.MaxRecords}", GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
        SirenixEditorGUI.EndBox();
    }

    /// <summary>
    /// 绘制工具栏
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 动态/静态切换
        if (GUILayout.Button(isDynamicMode ? "切换静态" : "切换动态", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            isDynamicMode = !isDynamicMode;
        }

        // 暂停按钮
        GUI.backgroundColor = isPaused ? yellowColor : Color.white;
        if (GUILayout.Button(isPaused ? "▶ 继续" : "⏸ 暂停", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            isPaused = !isPaused;
            MessageMonitor.IsPaused = isPaused;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // 清空按钮
        if (GUILayout.Button("清空记录", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            MessageMonitor.ClearRecords();
            Repaint();
        }

        // 刷新按钮
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            Repaint();
        }

        GUILayout.FlexibleSpace();

        // 最大显示数量
        GUILayout.Label("显示数量:", GUILayout.Width(60));
        maxDisplayCount = EditorGUILayout.IntField(maxDisplayCount, GUILayout.Width(50));
        maxDisplayCount = Mathf.Clamp(maxDisplayCount, 10, 500);

        // 分组开关
        GUI.backgroundColor = enableGrouping ? greenColor : Color.white;
        if (GUILayout.Button(enableGrouping ? "分组" : "列表", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            enableGrouping = !enableGrouping;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        // 启用/禁用监控
        GUI.backgroundColor = MessageMonitor.IsEnabled ? greenColor : redColor;
        if (GUILayout.Button(MessageMonitor.IsEnabled ? "监控中" : "已禁用", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            MessageMonitor.ToggleEnabled();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制过滤栏
    /// </summary>
    private void DrawFilterBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("过滤:", GUILayout.Width(35));

        // 文本过滤
        filterText = EditorGUILayout.TextField(filterText, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        GUILayout.Space(10);

        // 操作类型过滤
        GUILayout.Label("操作:", GUILayout.Width(35));
        string[] actionTypes = { "All", "Broadcast", "AddListener", "RemoveListener" };
        int currentIndex = System.Array.IndexOf(actionTypes, filterActionType);
        currentIndex = EditorGUILayout.Popup(currentIndex, actionTypes, EditorStyles.toolbarPopup, GUILayout.Width(100));
        filterActionType = actionTypes[currentIndex];

        GUILayout.FlexibleSpace();

        // 清除过滤
        if (GUILayout.Button("清除过滤", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            filterText = "";
            filterActionType = "All";
            filterMessageType = null;
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制记录表格
    /// </summary>
    private void DrawRecordTable()
    {
        // 表头
        DrawTableHeader();

        // 表格内容
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var records = GetFilteredRecords();

        if (enableGrouping)
        {
            DrawGroupedRecords(records);
        }
        else
        {
            DrawFlatRecords(records);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制平铺列表
    /// </summary>
    private void DrawFlatRecords(List<MessageRecord> records)
    {
        int displayCount = Mathf.Min(records.Count, maxDisplayCount);

        // 从最新的开始显示
        for (int i = records.Count - 1; i >= records.Count - displayCount && i >= 0; i--)
        {
            DrawRecordRow(records[i], i % 2 == 0);
        }
    }

    /// <summary>
    /// 绘制分组记录
    /// </summary>
    private void DrawGroupedRecords(List<MessageRecord> records)
    {
        // 按消息名称分组
        var groups = records.GroupBy(r => r.MessageName)
                           .OrderByDescending(g => g.Max(r => r.Index))
                           .ToList();

        int totalDisplayed = 0;
        foreach (var group in groups)
        {
            if (totalDisplayed >= maxDisplayCount) break;

            string groupKey = group.Key;
            var groupRecords = group.OrderByDescending(r => r.Index).ToList();

            // 确保折叠状态存在
            if (!groupFoldoutStates.ContainsKey(groupKey))
            {
                groupFoldoutStates[groupKey] = false;
            }

            // 绘制分组头
            DrawGroupHeader(groupKey, groupRecords.Count, groupRecords[0]);

            // 如果展开，显示组内记录
            if (groupFoldoutStates[groupKey])
            {
                int groupDisplayCount = Mathf.Min(groupRecords.Count, maxDisplayCount - totalDisplayed);
                for (int i = 0; i < groupDisplayCount; i++)
                {
                    DrawRecordRow(groupRecords[i], i % 2 == 0);
                    totalDisplayed++;
                }
            }
            else
            {
                totalDisplayed++;
            }
        }
    }

    /// <summary>
    /// 绘制分组头
    /// </summary>
    private void DrawGroupHeader(string groupName, int count, MessageRecord latestRecord)
    {
        EditorGUILayout.BeginHorizontal();

        // 折叠箭头和背景（浅色）
        Color headerBg = new Color(0.85f, 0.85f, 0.9f);
        Rect headerRect = EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(headerRect, headerBg);

        // 折叠按钮
        bool isExpanded = groupFoldoutStates[groupName];
        string arrow = isExpanded ? "▼" : "▶";
        if (GUILayout.Button(arrow, EditorStyles.label, GUILayout.Width(20)))
        {
            groupFoldoutStates[groupName] = !groupFoldoutStates[groupName];
        }

        // 消息名称（加粗，深色）
        GUIStyle boldStyle = new GUIStyle(EditorStyles.boldLabel);
        Color actionColor = latestRecord.GetActionColor();
        boldStyle.normal.textColor = new Color(actionColor.r * 0.6f, actionColor.g * 0.6f, actionColor.b * 0.6f);
        GUILayout.Label(groupName, boldStyle, GUILayout.Width(150));

        // 计数
        GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel);
        countStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
        GUILayout.Label($"({count} 条)", countStyle, GUILayout.Width(60));

        // 最新时间
        GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel);
        timeStyle.normal.textColor = Color.black;
        GUILayout.Label($"最新: {latestRecord.Timestamp}", timeStyle, GUILayout.Width(100));

        // 最新参数预览
        if (!string.IsNullOrEmpty(latestRecord.Param1))
        {
            GUIStyle paramStyle = new GUIStyle(EditorStyles.miniLabel);
            paramStyle.normal.textColor = Color.black;
            GUILayout.Label($"参数: {latestRecord.Param1}", paramStyle, GUILayout.ExpandWidth(true));
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);
    }

    /// <summary>
    /// 绘制表头
    /// </summary>
    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.alignment = TextAnchor.MiddleCenter;

        DrawResizableColumn("#", headerStyle, ref colWidth_Index, 0);
        DrawResizableColumn("时间", headerStyle, ref colWidth_Time, 1);
        DrawResizableColumn("颜色", headerStyle, ref colWidth_Color, 2);
        DrawResizableColumn("类型", headerStyle, ref colWidth_Type, 3);
        DrawResizableColumn("消息名称", headerStyle, ref colWidth_Message, 4);
        DrawResizableColumn("参数1", headerStyle, ref colWidth_Param1, 5);
        DrawResizableColumn("参数2", headerStyle, ref colWidth_Param2, 6);
        DrawResizableColumn("参数3", headerStyle, ref colWidth_Param3, 7);
        GUILayout.Label("备注", headerStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制可调整大小的列
    /// </summary>
    private void DrawResizableColumn(string label, GUIStyle style, ref float width, int columnIndex)
    {
        Rect rect = GUILayoutUtility.GetRect(width, 20, GUILayout.Width(width));
        GUI.Label(rect, label, style);

        // 绘制分隔线
        Rect separatorRect = new Rect(rect.x + rect.width - 1, rect.y, 2, rect.height);
        EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

        // 处理拖拽
        Rect dragRect = new Rect(rect.x + rect.width - 3, rect.y, 6, rect.height);
        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.ResizeHorizontal);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && dragRect.Contains(e.mousePosition))
        {
            draggingColumnIndex = columnIndex;
            dragStartX = e.mousePosition.x;
            dragStartWidth = width;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && draggingColumnIndex == columnIndex)
        {
            float delta = e.mousePosition.x - dragStartX;
            width = Mathf.Max(30, dragStartWidth + delta);
            Repaint();
            e.Use();
        }
        else if (e.type == EventType.MouseUp && draggingColumnIndex == columnIndex)
        {
            draggingColumnIndex = -1;
            e.Use();
        }
    }

    /// <summary>
    /// 绘制单行记录
    /// </summary>
    private void DrawRecordRow(MessageRecord record, bool isEvenRow)
    {
        // 交替背景色（浅色）
        Color bgColor = isEvenRow ? new Color(0.95f, 0.95f, 0.95f) : new Color(1f, 1f, 1f);
        Rect rowRect = EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(rowRect, bgColor);

        GUIStyle cellStyle = new GUIStyle(EditorStyles.label);
        cellStyle.alignment = TextAnchor.MiddleLeft;
        cellStyle.normal.textColor = Color.black;

        // 序号
        GUILayout.Label(record.Index.ToString(), cellStyle, GUILayout.Width(colWidth_Index));

        // 时间戳
        GUILayout.Label(record.Timestamp, cellStyle, GUILayout.Width(colWidth_Time));

        // 颜色标签
        Rect colorRect = GUILayoutUtility.GetRect(20, 16, GUILayout.Width(colWidth_Color));
        EditorGUI.DrawRect(new Rect(colorRect.x + 5, colorRect.y + 2, 20, 12), record.GetTypeColor());

        // 操作类型（带颜色，深色版本）
        GUIStyle actionStyle = new GUIStyle(EditorStyles.label);
        Color actionColor = record.GetActionColor();
        // 将浅色转为深色以适应白色背景
        actionStyle.normal.textColor = new Color(actionColor.r * 0.6f, actionColor.g * 0.6f, actionColor.b * 0.6f);
        GUILayout.Label(record.ActionType, actionStyle, GUILayout.Width(colWidth_Type));

        // 消息名称
        GUILayout.Label(record.MessageName, cellStyle, GUILayout.Width(colWidth_Message));

        // 参数1-3
        GUILayout.Label(record.Param1, cellStyle, GUILayout.Width(colWidth_Param1));
        GUILayout.Label(record.Param2, cellStyle, GUILayout.Width(colWidth_Param2));
        GUILayout.Label(record.Param3, cellStyle, GUILayout.Width(colWidth_Param3));

        // 备注
        GUILayout.Label(record.Remark, cellStyle, GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 获取过滤后的记录
    /// </summary>
    private List<MessageRecord> GetFilteredRecords()
    {
        var records = MessageMonitor.Records;

        if (string.IsNullOrEmpty(filterText) && filterActionType == "All" && filterMessageType == null)
        {
            return records;
        }

        return records.Where(r =>
        {
            // 文本过滤
            if (!string.IsNullOrEmpty(filterText))
            {
                bool matchText = r.MessageName.ToLower().Contains(filterText.ToLower()) ||
                                r.Param1.ToLower().Contains(filterText.ToLower()) ||
                                r.Param2.ToLower().Contains(filterText.ToLower()) ||
                                r.Param3.ToLower().Contains(filterText.ToLower()) ||
                                r.Remark.ToLower().Contains(filterText.ToLower());
                if (!matchText) return false;
            }

            // 操作类型过滤
            if (filterActionType != "All" && r.ActionType != filterActionType)
            {
                return false;
            }

            // 消息类型过滤
            if (filterMessageType.HasValue && r.MessageType != filterMessageType.Value)
            {
                return false;
            }

            return true;
        }).ToList();
    }

    #endregion
}
#endif
