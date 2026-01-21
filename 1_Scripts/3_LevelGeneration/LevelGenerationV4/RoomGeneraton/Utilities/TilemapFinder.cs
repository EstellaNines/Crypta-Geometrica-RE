using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// Tilemap 和 TileConfigData 自动查找工具
    /// 通过 Unity Tag 系统自动识别对应的 Tilemap
    /// </summary>
    public static class TilemapFinder
    {
        /// <summary>
        /// 背景层标签
        /// </summary>
        public const string TAG_BACKGROUND = "Background";

        /// <summary>
        /// 地面层标签
        /// </summary>
        public const string TAG_GROUND = "Ground";

        /// <summary>
        /// 平台层标签
        /// </summary>
        public const string TAG_PLATFORM = "Platform";

        /// <summary>
        /// 根据层级自动查找对应的 Tilemap
        /// </summary>
        /// <param name="layer">Tilemap 层级</param>
        /// <returns>找到的 Tilemap，未找到返回 null</returns>
        public static Tilemap FindTilemapByLayer(TilemapLayer layer)
        {
            string tag = GetTagForLayer(layer);
            return FindTilemapByTag(tag);
        }

        /// <summary>
        /// 根据标签查找 Tilemap
        /// </summary>
        /// <param name="tag">Unity Tag</param>
        /// <returns>找到的 Tilemap，未找到返回 null</returns>
        public static Tilemap FindTilemapByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            GameObject go = GameObject.FindGameObjectWithTag(tag);
            if (go == null)
            {
                Debug.LogWarning($"[TilemapFinder] 未找到标签为 '{tag}' 的 GameObject");
                return null;
            }

            Tilemap tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogWarning($"[TilemapFinder] 标签为 '{tag}' 的 GameObject 没有 Tilemap 组件");
                return null;
            }

            return tilemap;
        }

        /// <summary>
        /// 查找场景中的 TileConfigData
        /// 优先从 Resources 文件夹加载，其次搜索所有已加载的 ScriptableObject
        /// </summary>
        /// <returns>找到的 TileConfigData，未找到返回 null</returns>
        public static TileConfigData FindTileConfig()
        {
            // 方法1：从 Resources 加载
            var config = Resources.Load<TileConfigData>("TileConfig");
            if (config != null)
            {
                return config;
            }

            // 方法2：搜索所有已加载的 TileConfigData
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TileConfigData");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<TileConfigData>(path);
                if (config != null)
                {
                    if (guids.Length > 1)
                    {
                        Debug.LogWarning($"[TilemapFinder] 找到多个 TileConfigData，使用第一个: {path}");
                    }
                    return config;
                }
            }
#endif

            Debug.LogWarning("[TilemapFinder] 未找到 TileConfigData");
            return null;
        }

        /// <summary>
        /// 获取层级对应的 Tag
        /// </summary>
        /// <param name="layer">Tilemap 层级</param>
        /// <returns>对应的 Unity Tag</returns>
        public static string GetTagForLayer(TilemapLayer layer)
        {
            return layer switch
            {
                TilemapLayer.Background => TAG_BACKGROUND,
                TilemapLayer.Ground => TAG_GROUND,
                TilemapLayer.Platform => TAG_PLATFORM,
                _ => null
            };
        }

        /// <summary>
        /// 一次性查找所有三个层级的 Tilemap
        /// </summary>
        /// <param name="background">背景层 Tilemap</param>
        /// <param name="ground">地面层 Tilemap</param>
        /// <param name="platform">平台层 Tilemap</param>
        /// <returns>是否全部找到</returns>
        public static bool FindAllTilemaps(out Tilemap background, out Tilemap ground, out Tilemap platform)
        {
            background = FindTilemapByLayer(TilemapLayer.Background);
            ground = FindTilemapByLayer(TilemapLayer.Ground);
            platform = FindTilemapByLayer(TilemapLayer.Platform);

            return background != null && ground != null && platform != null;
        }
    }
}
