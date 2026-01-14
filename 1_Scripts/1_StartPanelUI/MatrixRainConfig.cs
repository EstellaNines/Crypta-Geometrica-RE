using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// 矩阵雨效果配置
/// ScriptableObject 便于美术调整参数
/// </summary>
[CreateAssetMenu(fileName = "MatrixRainConfig", menuName = "Crypta Geometrica:RE/主菜单动效/Config/MatrixRainConfig")]
public class MatrixRainConfig : ScriptableObject
{
    [TitleGroup("字符设置")]
    [LabelText("字符集")]
    [Tooltip("可用字符集")]
    public string characterSet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ@#$%&*";

    [TitleGroup("字符设置")]
    [LabelText("字体大小")]
    [Range(8, 32)]
    public int fontSize = 14;

    [TitleGroup("字符设置")]
    [LabelText("TMP字体")]
    [Tooltip("TMP字体资源")]
    public TMP_FontAsset font;

    [TitleGroup("颜色设置")]
    [LabelText("头部颜色")]
    [Tooltip("头部字符颜色（最亮）")]
    public Color headColor = new Color(0.9f, 1f, 0.9f, 1f);

    [TitleGroup("颜色设置")]
    [LabelText("主体颜色")]
    [Tooltip("主体颜色")]
    public Color bodyColor = new Color(0f, 0.8f, 0.2f, 1f);

    [TitleGroup("颜色设置")]
    [LabelText("尾部颜色")]
    [Tooltip("尾部颜色（最暗）")]
    public Color tailColor = new Color(0f, 0.3f, 0f, 0.1f);

    [TitleGroup("运动设置")]
    [LabelText("最小速度")]
    [Range(20f, 100f)]
    public float minSpeed = 40f;

    [TitleGroup("运动设置")]
    [LabelText("最大速度")]
    [Range(100f, 300f)]
    public float maxSpeed = 120f;

    [TitleGroup("运动设置")]
    [LabelText("字符变化间隔")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0.02f, 0.2f)]
    public float characterChangeInterval = 0.05f;

    [TitleGroup("布局设置")]
    [LabelText("列数")]
    [Range(10, 120)]
    public int columnCount = 50;

    [TitleGroup("布局设置")]
    [LabelText("最小拖尾长度")]
    [Range(5, 15)]
    public int minTrailLength = 8;

    [TitleGroup("布局设置")]
    [LabelText("最大拖尾长度")]
    [Range(15, 40)]
    public int maxTrailLength = 25;

    [TitleGroup("布局设置")]
    [LabelText("列间距")]
    [Range(10f, 40f)]
    public float columnSpacing = 18f;

    [TitleGroup("布局设置")]
    [LabelText("生成间隔")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0.01f, 0.2f)]
    public float spawnInterval = 0.03f;

    [TitleGroup("效果设置")]
    [LabelText("整体透明度")]
    [Range(0f, 1f)]
    public float globalAlpha = 0.85f;

    [TitleGroup("效果设置")]
    [LabelText("闪烁强度")]
    [Range(0f, 1f)]
    public float flickerIntensity = 0.3f;

    /// <summary>
    /// 获取随机字符
    /// </summary>
    public char GetRandomCharacter()
    {
        if (string.IsNullOrEmpty(characterSet))
            return '0';
        return characterSet[Random.Range(0, characterSet.Length)];
    }

    /// <summary>
    /// 获取随机速度
    /// </summary>
    public float GetRandomSpeed()
    {
        return Random.Range(minSpeed, maxSpeed);
    }

    /// <summary>
    /// 获取随机拖尾长度
    /// </summary>
    public int GetRandomTrailLength()
    {
        return Random.Range(minTrailLength, maxTrailLength + 1);
    }

    /// <summary>
    /// 根据位置获取颜色（0=头部，1=尾部）
    /// </summary>
    public Color GetColorAtPosition(float t)
    {
        if (t < 0.1f)
        {
            return Color.Lerp(headColor, bodyColor, t / 0.1f);
        }
        else
        {
            float bodyT = (t - 0.1f) / 0.9f;
            return Color.Lerp(bodyColor, tailColor, bodyT);
        }
    }
}
