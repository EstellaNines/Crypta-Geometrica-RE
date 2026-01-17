using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 单个颜色主题数据
    /// </summary>
    [System.Serializable]
    public class ThemeColorData
    {
        [Tooltip("主题颜色名称")]
        public string ColorName = "New Color";
        
        [Tooltip("主题颜色（编辑器识别用）")]
        public Color DisplayColor = Color.white;
        
        [Tooltip("地面规则瓦片")]
        public RuleTile GroundRuleTile;
        
        [Tooltip("平台规则瓦片")]
        public RuleTile PlatformRuleTile;
    }
    
    /// <summary>
    /// 房间主题配置数据对象
    /// 存储所有颜色主题的规则瓦片，支持随机选择
    /// </summary>
    [CreateAssetMenu(fileName = "RoomThemeConfig", menuName = "Crypta Geometrica:RE/PCG程序化关卡/V3/Room Theme Config")]
    public class RoomTheme : ScriptableObject
    {
        [Header("主题配置")]
        [Tooltip("红色主题")]
        public ThemeColorData RedTheme = new ThemeColorData { ColorName = "Red", DisplayColor = Color.red };
     
        [Tooltip("蓝色主题")]
        public ThemeColorData BlueTheme = new ThemeColorData { ColorName = "Blue", DisplayColor = Color.blue };
        
        [Tooltip("黄色主题")]
        public ThemeColorData YellowTheme = new ThemeColorData { ColorName = "Yellow", DisplayColor = Color.yellow };
        
        /// <summary>
        /// 随机获取一个颜色主题
        /// </summary>
        public ThemeColorData GetRandomTheme(System.Random rng)
        {
            int index = rng.Next(3);
            switch (index)
            {
                case 0: return RedTheme;
                case 1: return BlueTheme;
                case 2: return YellowTheme;
                default: return BlueTheme;
            }
        }
        
        /// <summary>
        /// 获取所有主题
        /// </summary>
        public ThemeColorData[] GetAllThemes()
        {
            return new ThemeColorData[] { RedTheme, BlueTheme, YellowTheme };
        }
    }
}
