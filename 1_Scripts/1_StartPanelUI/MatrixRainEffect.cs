using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// 矩阵雨效果控制器
/// 使用 TextMeshPro 对象池实现经典的数字雨效果
/// </summary>
public class MatrixRainEffect : MonoBehaviour
{
    #region 配置

    [TitleGroup("配置")]
    [LabelText("效果配置")]
    [Required("请指定 MatrixRainConfig 配置文件")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    [SerializeField] private MatrixRainConfig config;

    [TitleGroup("配置")]
    [LabelText("容器")]
    [Required("请指定容器 RectTransform")]
    [SerializeField] private RectTransform container;

    [TitleGroup("运行时设置")]
    [LabelText("自动启动")]
    [SerializeField] private bool autoStart = true;

    [TitleGroup("运行时设置")]
    [LabelText("效果强度")]
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 1f;

    [TitleGroup("运行时设置")]
    [LabelText("速度倍率")]
    [Range(0.5f, 2f)]
    [SerializeField] private float speedMultiplier = 1f;

    #endregion

    #region 运行时状态（只读显示）

    [TitleGroup("运行时状态")]
    [LabelText("是否运行中")]
    [ShowInInspector, ReadOnly]
    private bool isRunning = false;

    [TitleGroup("运行时状态")]
    [LabelText("活跃列数")]
    [ShowInInspector, ReadOnly]
    private int activeColumnCount = 0;

    [TitleGroup("运行时状态")]
    [LabelText("对象池大小")]
    [ShowInInspector, ReadOnly]
    private int PoolSize => textPool?.Count ?? 0;

    [TitleGroup("运行时状态")]
    [LabelText("容器宽度")]
    [ShowInInspector, ReadOnly]
    private float ContainerWidth => containerWidth;

    [TitleGroup("运行时状态")]
    [LabelText("容器高度")]
    [ShowInInspector, ReadOnly]
    private float ContainerHeight => containerHeight;

    [TitleGroup("运行时状态")]
    [LabelText("列数")]
    [ShowInInspector, ReadOnly]
    private int ColumnCount => columns?.Count ?? 0;

    #endregion

    #region 私有变量

    private List<MatrixColumn> columns = new List<MatrixColumn>();
    private Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
    private float containerWidth;
    private float containerHeight;
    private float spawnTimer;

    #endregion

    #region 生命周期

    private void Start()
    {
        if (autoStart)
        {
            // 延迟一帧初始化，确保 RectTransform 尺寸已计算
            StartCoroutine(DelayedInitialize());
        }
    }

    private System.Collections.IEnumerator DelayedInitialize()
    {
        // 等待一帧，让 Canvas 完成布局计算
        yield return null;
        Initialize();
    }

    private void Update()
    {
        if (!isRunning) return;

        UpdateColumns();
        TrySpawnNewColumn();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    #endregion

    #region 公共方法

    [TitleGroup("调试")]
    [Button("初始化", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void Initialize()
    {
        if (config == null)
        {
            Debug.LogError("[MatrixRainEffect] 缺少 MatrixRainConfig 配置！");
            return;
        }

        if (container == null)
        {
            container = GetComponent<RectTransform>();
        }

        containerWidth = container.rect.width;
        containerHeight = container.rect.height;

        InitializeColumns();
        PrewarmPool();

        isRunning = true;
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/控制")]
    [Button("启用")]
    public void Enable()
    {
        SetEnabled(true);
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/控制")]
    [Button("禁用")]
    public void Disable()
    {
        SetEnabled(false);
    }

    public void SetEnabled(bool enabled)
    {
        isRunning = enabled;
        container.gameObject.SetActive(enabled);
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/状态")]
    [Button("暂停")]
    public void Pause()
    {
        isRunning = false;
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/状态")]
    [Button("恢复")]
    public void Resume()
    {
        isRunning = true;
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }

    public void SetSpeed(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.5f, 2f);
    }

    [TitleGroup("调试")]
    [Button("清理资源", ButtonSizes.Medium)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void Cleanup()
    {
        isRunning = false;

        foreach (var column in columns)
        {
            foreach (var charData in column.characters)
            {
                if (charData.textComponent != null)
                {
                    Destroy(charData.textComponent.gameObject);
                }
            }
        }

        columns.Clear();

        while (textPool.Count > 0)
        {
            var text = textPool.Dequeue();
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        activeColumnCount = 0;
    }

    #endregion

    #region 私有方法

    private void InitializeColumns()
    {
        columns.Clear();

        int columnCount = Mathf.Min(config.columnCount, Mathf.FloorToInt(containerWidth / config.columnSpacing));

        for (int i = 0; i < columnCount; i++)
        {
            var column = new MatrixColumn
            {
                columnIndex = i,
                xPosition = -containerWidth / 2f + i * config.columnSpacing + config.columnSpacing / 2f,
                speed = config.GetRandomSpeed(),
                currentY = containerHeight / 2f + Random.Range(0f, containerHeight),
                trailLength = config.GetRandomTrailLength(),
                characters = new List<MatrixCharacter>(),
                isActive = false,
                nextCharChangeTime = 0f
            };

            columns.Add(column);
        }
    }

    private void PrewarmPool()
    {
        int poolSize = config.columnCount * config.maxTrailLength;
        poolSize = Mathf.Min(poolSize, 500);

        for (int i = 0; i < poolSize; i++)
        {
            var text = CreateTextComponent();
            text.gameObject.SetActive(false);
            textPool.Enqueue(text);
        }
    }

    private TextMeshProUGUI CreateTextComponent()
    {
        GameObject go = new GameObject("MatrixChar");
        go.transform.SetParent(container, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = config.font;
        text.fontSize = config.fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(config.columnSpacing, config.fontSize + 4);

        return text;
    }

    private TextMeshProUGUI GetTextFromPool()
    {
        if (textPool.Count > 0)
        {
            var text = textPool.Dequeue();
            text.gameObject.SetActive(true);
            return text;
        }

        return CreateTextComponent();
    }

    private void ReturnTextToPool(TextMeshProUGUI text)
    {
        if (text == null) return;

        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
    }

    private void TrySpawnNewColumn()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= config.spawnInterval)
        {
            spawnTimer = 0f;

            if (Random.value > intensity) return;

            var inactiveColumns = columns.FindAll(c => !c.isActive);
            if (inactiveColumns.Count > 0)
            {
                var column = inactiveColumns[Random.Range(0, inactiveColumns.Count)];
                ActivateColumn(column);
            }
        }
    }

    private void ActivateColumn(MatrixColumn column)
    {
        column.isActive = true;
        column.currentY = containerHeight / 2f + config.fontSize;
        column.speed = config.GetRandomSpeed();
        column.trailLength = config.GetRandomTrailLength();
        column.nextCharChangeTime = Time.time + config.characterChangeInterval;
        activeColumnCount++;

        AddCharacterToColumn(column);
    }

    private void AddCharacterToColumn(MatrixColumn column)
    {
        var text = GetTextFromPool();
        var charData = new MatrixCharacter
        {
            character = config.GetRandomCharacter(),
            textComponent = text,
            yOffset = 0f
        };

        text.text = charData.character.ToString();
        text.color = config.headColor;

        var rect = text.rectTransform;
        rect.anchoredPosition = new Vector2(column.xPosition, column.currentY);

        column.characters.Insert(0, charData);
    }

    private void UpdateColumns()
    {
        float deltaTime = Time.deltaTime;

        foreach (var column in columns)
        {
            if (!column.isActive) continue;

            float moveAmount = column.speed * speedMultiplier * deltaTime;
            column.currentY -= moveAmount;

            for (int i = 0; i < column.characters.Count; i++)
            {
                var charData = column.characters[i];
                float yPos = column.currentY - i * (config.fontSize + 2);

                charData.textComponent.rectTransform.anchoredPosition =
                    new Vector2(column.xPosition, yPos);

                float t = (float)i / column.trailLength;
                Color color = config.GetColorAtPosition(t);
                color.a *= config.globalAlpha;

                if (i == 0 && config.flickerIntensity > 0)
                {
                    float flicker = 1f + Random.Range(-config.flickerIntensity, config.flickerIntensity);
                    color.r *= flicker;
                    color.g *= flicker;
                    color.b *= flicker;
                }

                charData.textComponent.color = color;
            }

            if (Time.time >= column.nextCharChangeTime)
            {
                column.nextCharChangeTime = Time.time + config.characterChangeInterval;
                RandomizeColumnCharacters(column);
            }

            if (column.characters.Count < column.trailLength)
            {
                AddCharacterToColumn(column);
            }

            float lastCharY = column.currentY - (column.characters.Count - 1) * (config.fontSize + 2);
            if (lastCharY < -containerHeight / 2f - config.fontSize * column.trailLength)
            {
                DeactivateColumn(column);
            }
        }
    }

    private void RandomizeColumnCharacters(MatrixColumn column)
    {
        int changeCount = Random.Range(1, Mathf.Max(2, column.characters.Count / 3));

        for (int i = 0; i < changeCount; i++)
        {
            int index = Random.Range(0, column.characters.Count);
            var charData = column.characters[index];
            charData.character = config.GetRandomCharacter();
            charData.textComponent.text = charData.character.ToString();
        }
    }

    private void DeactivateColumn(MatrixColumn column)
    {
        column.isActive = false;
        activeColumnCount--;

        foreach (var charData in column.characters)
        {
            ReturnTextToPool(charData.textComponent);
        }

        column.characters.Clear();
    }

    #endregion

    #region 内部类

    private class MatrixColumn
    {
        public int columnIndex;
        public float xPosition;
        public float speed;
        public float currentY;
        public int trailLength;
        public List<MatrixCharacter> characters;
        public bool isActive;
        public float nextCharChangeTime;
    }

    private class MatrixCharacter
    {
        public char character;
        public TextMeshProUGUI textComponent;
        public float yOffset;
    }

    #endregion
}
