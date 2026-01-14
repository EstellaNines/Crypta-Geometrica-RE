# 主界面动态视觉效果实施方案

## 1. 系统概述

本方案旨在为游戏主界面（Start Panel）实现两个核心视觉效果：
1. **封面图片动态展示** - 图片的缓动、缩放、淡入淡出等动态效果
2. **矩阵雨效果（Matrix Rain）** - 经典的数字/字符下落动画背景

### 设计目标

- 营造科技感/赛博朋克风格的视觉氛围
- 低性能开销，适配移动端
- 高度可配置，便于美术调整参数
- 与现有 UI 系统解耦

---

## 2. 模块架构设计

### 2.1 核心组件

| 组件名称 | 类型 | 职责 | 生命周期 |
|---------|------|------|----------|
| CoverImageAnimator | MonoBehaviour | 控制封面图片的动态效果（缩放、位移、淡入淡出） | 仅存在于主界面场景 |
| MatrixRainEffect | MonoBehaviour | 管理矩阵雨的生成、更新和回收 | 仅存在于主界面场景 |
| MatrixColumn | Class | 单列字符的数据和渲染逻辑 | 由 MatrixRainEffect 管理 |
| MatrixRainConfig | ScriptableObject | 矩阵雨效果的可配置参数 | 全局资源 |

### 2.2 层级结构

```
StartPanel (Canvas)
├── Background Layer (Order: 0)
│   └── MatrixRainContainer (RawImage + MatrixRainEffect)
├── Cover Layer (Order: 1)
│   └── CoverImage (Image + CoverImageAnimator)
├── UI Layer (Order: 2)
│   ├── TitleText
│   ├── StartButton
│   └── SettingsButton
└── Overlay Layer (Order: 3)
    └── FadePanel (用于场景过渡)
```

---

## 3. 封面图片动态展示

### 3.1 效果描述

封面图片将具备以下动态效果：

1. **呼吸缩放（Breathing Scale）**
   - 图片在 1.0 ~ 1.05 之间缓慢缩放
   - 周期：3-5秒
   - 缓动曲线：Sine InOut

2. **缓慢位移（Slow Pan）**
   - 图片在小范围内缓慢移动
   - 位移范围：±20像素
   - 周期：8-12秒

3. **淡入效果（Fade In）**
   - 场景加载后图片从透明渐变为不透明
   - 持续时间：1-2秒

4. **光晕脉冲（可选）**
   - 图片边缘的发光效果周期性变化

### 3.2 接口设计

```csharp
public class CoverImageAnimator : MonoBehaviour
{
    // 配置参数
    [Header("呼吸缩放")]
    public bool enableBreathing = true;
    public float breathingMinScale = 1.0f;
    public float breathingMaxScale = 1.05f;
    public float breathingDuration = 4f;

    [Header("缓慢位移")]
    public bool enablePanning = true;
    public Vector2 panRange = new Vector2(20f, 10f);
    public float panDuration = 10f;

    [Header("淡入效果")]
    public bool enableFadeIn = true;
    public float fadeInDuration = 1.5f;

    // 公共方法
    public void PlayFadeIn();
    public void PlayFadeOut(float duration, Action onComplete);
    public void SetPaused(bool paused);
    public void ResetToDefault();
}
```

### 3.3 实现要点

- 使用 `DOTween` 或自定义协程实现动画
- 多个效果可叠加运行
- 支持暂停/恢复（用于设置界面等场景）

---

## 4. 矩阵雨效果（Matrix Rain）

### 4.1 效果描述

经典的"黑客帝国"风格数字雨效果：

- 绿色字符从屏幕顶部下落
- 字符随机变化（数字、字母、符号）
- 头部字符最亮，尾部逐渐变暗
- 不同列速度略有差异
- 字符大小可配置

### 4.2 技术方案选择

| 方案 | 优点 | 缺点 | 推荐场景 |
|------|------|------|----------|
| **方案A: UI Text 对象池** | 实现简单，易调试 | 性能一般，大量 Text 开销大 | 字符数量少（<200） |
| **方案B: Shader + RenderTexture** | 性能最优，效果最好 | 实现复杂，需要 Shader 知识 | 追求极致效果 |
| **方案C: 粒子系统** | Unity 原生支持，性能好 | 字符渲染受限 | 简化版效果 |
| **方案D: Texture2D 动态绘制** | 性能较好，灵活性高 | 需要手动管理纹理 | 平衡方案 |

**推荐方案：方案A（UI Text 对象池）** - 实现简单，效果可控，适合快速迭代。

### 4.3 数据结构设计

```csharp
/// <summary>
/// 矩阵雨配置（ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "MatrixRainConfig", menuName = "Config/MatrixRainConfig")]
public class MatrixRainConfig : ScriptableObject
{
    [Header("字符设置")]
    public string characterSet = "0123456789ABCDEF";  // 可用字符集
    public int fontSize = 16;
    public Font font;                                  // 推荐等宽字体

    [Header("颜色设置")]
    public Color headColor = new Color(0.8f, 1f, 0.8f, 1f);   // 头部亮色
    public Color tailColor = new Color(0f, 0.5f, 0f, 0.2f);   // 尾部暗色
    public Gradient trailGradient;                             // 拖尾渐变

    [Header("运动设置")]
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float characterChangeInterval = 0.1f;      // 字符变化间隔

    [Header("布局设置")]
    public int columnCount = 40;                      // 列数
    public int maxTrailLength = 20;                   // 最大拖尾长度
    public float spawnInterval = 0.05f;               // 新列生成间隔
}

/// <summary>
/// 单列数据
/// </summary>
public class MatrixColumn
{
    public int columnIndex;
    public float speed;
    public float currentY;
    public int trailLength;
    public List<MatrixCharacter> characters;
    public bool isActive;
}

/// <summary>
/// 单个字符数据
/// </summary>
public class MatrixCharacter
{
    public char character;
    public float alpha;
    public Text textComponent;  // UI Text 引用
}
```

### 4.4 核心逻辑流程

```
初始化
├── 1. 计算列数和间距
├── 2. 创建字符对象池
└── 3. 初始化所有列的起始状态

每帧更新
├── 1. 遍历所有活跃列
│   ├── 更新 Y 坐标（下落）
│   ├── 随机变化字符
│   └── 更新透明度渐变
├── 2. 检查列是否超出屏幕
│   └── 重置到顶部，随机新参数
└── 3. 随机激活新列

渲染
└── 更新所有 Text 组件的文本和颜色
```

### 4.5 接口设计

```csharp
public class MatrixRainEffect : MonoBehaviour
{
    [SerializeField] private MatrixRainConfig config;
    [SerializeField] private RectTransform container;

    // 公共方法
    public void Initialize();
    public void SetEnabled(bool enabled);
    public void SetIntensity(float intensity);  // 0~1，控制密度
    public void SetSpeed(float multiplier);     // 速度倍率
    public void Pause();
    public void Resume();
}
```

---

## 5. 详细实施流程

### 阶段一：基础框架搭建

**任务清单：**
1. 创建 `1_Scripts/1_StartPanelUI/` 目录结构
2. 创建 `MatrixRainConfig.cs` ScriptableObject
3. 创建 `CoverImageAnimator.cs` 基础框架
4. 创建 `MatrixRainEffect.cs` 基础框架

### 阶段二：封面图片动画实现

**任务清单：**
1. 实现呼吸缩放效果
2. 实现缓慢位移效果
3. 实现淡入淡出效果
4. 添加编辑器预览功能

### 阶段三：矩阵雨效果实现

**任务清单：**
1. 实现字符对象池
2. 实现单列下落逻辑
3. 实现字符随机变化
4. 实现颜色渐变效果
5. 实现列的生成和回收

### 阶段四：集成与优化

**任务清单：**
1. 创建 StartPanel 预制体
2. 集成两个效果组件
3. 性能测试与优化
4. 参数调优

---

## 6. 性能优化建议

### 6.1 对象池策略

- 预创建足够数量的 Text 对象
- 使用 `SetActive(false)` 而非 `Destroy`
- 避免运行时 `Instantiate`

### 6.2 更新频率控制

- 字符变化不需要每帧更新，可降低到 10Hz
- 使用 `Time.frameCount % N == 0` 分帧处理

### 6.3 渲染优化

- 所有 Text 使用同一材质
- 考虑使用 TextMeshPro 替代 Unity Text
- 关闭不必要的 Raycast Target

---

## 7. 可配置参数汇总

### 封面图片动画

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| breathingMinScale | float | 1.0 | 呼吸最小缩放 |
| breathingMaxScale | float | 1.05 | 呼吸最大缩放 |
| breathingDuration | float | 4.0 | 呼吸周期（秒） |
| panRange | Vector2 | (20, 10) | 位移范围（像素） |
| panDuration | float | 10.0 | 位移周期（秒） |
| fadeInDuration | float | 1.5 | 淡入时长（秒） |

### 矩阵雨效果

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| characterSet | string | "0-9A-F" | 可用字符集 |
| fontSize | int | 16 | 字体大小 |
| columnCount | int | 40 | 列数 |
| minSpeed | float | 50 | 最小下落速度 |
| maxSpeed | float | 150 | 最大下落速度 |
| maxTrailLength | int | 20 | 最大拖尾长度 |
| headColor | Color | 亮绿 | 头部颜色 |
| tailColor | Color | 暗绿 | 尾部颜色 |

---

## 8. 使用示例

```csharp
// 获取组件引用
var coverAnimator = FindObjectOfType<CoverImageAnimator>();
var matrixRain = FindObjectOfType<MatrixRainEffect>();

// 场景加载后播放淡入
coverAnimator.PlayFadeIn();

// 调整矩阵雨强度
matrixRain.SetIntensity(0.8f);

// 进入设置界面时暂停效果
coverAnimator.SetPaused(true);
matrixRain.Pause();

// 退出设置界面时恢复
coverAnimator.SetPaused(false);
matrixRain.Resume();
```

---

## 9. 依赖项

| 依赖 | 是否必须 | 说明 |
|------|----------|------|
| DOTween | 必须 | 用于封面图片动画和过渡效果 |
| TextMeshPro | 必须 | 矩阵雨字符渲染 |

---

## 10. 已实现文件清单

| 文件 | 说明 |
|------|------|
| `MatrixRainConfig.cs` | ScriptableObject 配置文件，存储矩阵雨所有参数 |
| `CoverImageAnimator.cs` | 封面图片动画控制器（呼吸、位移、淡入淡出） |
| `MatrixRainEffect.cs` | 矩阵雨效果核心逻辑（对象池 + TMP） |
| `StartPanelController.cs` | 主界面控制器，协调所有效果 |

---

## 11. Unity 配置步骤

### 11.1 创建配置资源

1. 在 Project 窗口右键 → Create → Config → MatrixRainConfig
2. 配置字符集、颜色、速度等参数
3. 指定 TMP 字体资源（推荐等宽字体）

### 11.2 场景层级结构

```
StartPanel (Canvas)
├── MatrixRainContainer (RawImage)
│   └── [挂载 MatrixRainEffect]
├── CoverImage (Image)
│   └── [挂载 CoverImageAnimator]
├── UIContainer
│   ├── TitleText (TMP)
│   ├── StartButton (Button)
│   └── SettingsButton (Button)
└── [挂载 StartPanelController]
```

### 11.3 组件配置

**MatrixRainEffect:**
- 拖入 MatrixRainConfig 资源
- Container 指向自身 RectTransform

**CoverImageAnimator:**
- Target Image 指向封面 Image 组件
- 调整呼吸/位移参数

**StartPanelController:**
- 关联所有效果组件和按钮

---

## 12. 版本记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.1 | 2026-01-14 | 完成代码实现（DOTween + TMP） |
| 1.0 | 2026-01-14 | 初始方案 |
