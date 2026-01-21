using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 主题瓦片配置
    /// 包含一个主题下所有层级的瓦片引用
    /// </summary>
    [Serializable]
    public class ThemeTileConfig
    {
        [LabelText("主题")]
        public TileTheme Theme;

        [TitleGroup("背景层")]
        [LabelText("背景瓦片数组")]
        [Tooltip("从Tileable纹理切割的多个瓦片，渲染时随机选择以产生自然效果")]
        [InfoBox("将切割后的所有瓦片拖入此数组，渲染时会随机选择")]
        public TileBase[] BackgroundTiles;

        [TitleGroup("地面层")]
        [LabelText("地面规则瓦片")]
        [Tooltip("Rule Tile，根据邻居自动变形")]
        public TileBase GroundRuleTile;

        [TitleGroup("平台层")]
        [LabelText("平台规则瓦片")]
        [Tooltip("Rule Tile，根据邻居自动变形")]
        public TileBase PlatformRuleTile;

        /// <summary>
        /// 随机获取一个背景瓦片
        /// </summary>
        /// <param name="rng">随机数生成器</param>
        /// <returns>随机选择的背景瓦片，数组为空返回null</returns>
        public TileBase GetRandomBackgroundTile(System.Random rng)
        {
            if (BackgroundTiles == null || BackgroundTiles.Length == 0)
                return null;
            return BackgroundTiles[rng.Next(BackgroundTiles.Length)];
        }
    }

    /// <summary>
    /// 瓦片配置数据
    /// 存储所有主题的瓦片引用
    /// </summary>
    [CreateAssetMenu(fileName = "TileConfig", menuName = "Crypta Geometrica:RE/PCG程序化关卡/V4/Tile Config")]
    public class TileConfigData : ScriptableObject
    {
        [TitleGroup("瓦片配置")]
        [LabelText("蓝色主题")]
        public ThemeTileConfig BlueTheme = new ThemeTileConfig { Theme = TileTheme.Blue };

        [TitleGroup("瓦片配置")]
        [LabelText("红色主题")]
        public ThemeTileConfig RedTheme = new ThemeTileConfig { Theme = TileTheme.Red };

        [TitleGroup("瓦片配置")]
        [LabelText("黄色主题")]
        public ThemeTileConfig YellowTheme = new ThemeTileConfig { Theme = TileTheme.Yellow };

        /// <summary>
        /// 根据主题获取瓦片配置
        /// </summary>
        /// <param name="theme">主题</param>
        /// <returns>对应主题的瓦片配置，Empty返回null</returns>
        public ThemeTileConfig GetConfig(TileTheme theme)
        {
            return theme switch
            {
                TileTheme.Blue => BlueTheme,
                TileTheme.Red => RedTheme,
                TileTheme.Yellow => YellowTheme,
                _ => null
            };
        }

        /// <summary>
        /// 根据主题和层级获取瓦片（背景层返回数组第一个）
        /// </summary>
        /// <param name="theme">主题</param>
        /// <param name="layer">层级</param>
        /// <returns>对应的瓦片，无效参数返回null</returns>
        public TileBase GetTile(TileTheme theme, TilemapLayer layer)
        {
            var config = GetConfig(theme);
            if (config == null) return null;

            return layer switch
            {
                TilemapLayer.Background => config.BackgroundTiles?.Length > 0 ? config.BackgroundTiles[0] : null,
                TilemapLayer.Ground => config.GroundRuleTile,
                TilemapLayer.Platform => config.PlatformRuleTile,
                _ => null
            };
        }

        /// <summary>
        /// 获取背景层的随机瓦片
        /// </summary>
        /// <param name="theme">主题</param>
        /// <param name="rng">随机数生成器</param>
        /// <returns>随机背景瓦片</returns>
        public TileBase GetRandomBackgroundTile(TileTheme theme, System.Random rng)
        {
            var config = GetConfig(theme);
            return config?.GetRandomBackgroundTile(rng);
        }
    }
}
