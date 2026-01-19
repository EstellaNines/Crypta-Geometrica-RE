using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 平台渲染规则
    /// 将DungeonContext中的PlatformTileData渲染到Unity Tilemap
    /// </summary>
    [Serializable]
    public class PlatformRenderRule : GeneratorRuleBase
    {
        #region 引用

        [TitleGroup("Tilemap引用")]
        [LabelText("平台Tilemap")]
        [Required("需要指定平台Tilemap")]
        [SerializeField]
        private Tilemap _platformTilemap;

        [TitleGroup("Tilemap引用")]
        [LabelText("瓦片配置")]
        [Required("需要指定瓦片配置")]
        [SerializeField]
        private TileConfigData _tileConfig;

        #endregion

        #region 自动识别

#if UNITY_EDITOR
        [TitleGroup("自动识别")]
        [Button("自动查找引用", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void AutoFindReferences()
        {
            bool changed = false;

            if (_tileConfig == null)
            {
                _tileConfig = TilemapFinder.FindTileConfig();
                if (_tileConfig != null)
                {
                    UnityEngine.Debug.Log($"[PlatformRenderRule] 自动找到 TileConfigData: {_tileConfig.name}");
                    changed = true;
                }
            }

            if (_platformTilemap == null)
            {
                _platformTilemap = TilemapFinder.FindTilemapByLayer(TilemapLayer.Platform);
                if (_platformTilemap != null)
                {
                    UnityEngine.Debug.Log($"[PlatformRenderRule] 自动找到平台 Tilemap: {_platformTilemap.name}");
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEngine.Debug.Log("[PlatformRenderRule] 自动查找完成，请保存 Pipeline 资产");
            }
            else
            {
                UnityEngine.Debug.Log("[PlatformRenderRule] 所有引用已设置，无需自动查找");
            }
        }
#endif

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public PlatformRenderRule()
        {
            _ruleName = "PlatformRenderRule";
            _executionOrder = 120; // 在GroundRenderRule之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始渲染平台层...");

            if (_platformTilemap == null)
            {
                LogError("平台Tilemap未设置");
                return false;
            }

            if (_tileConfig == null)
            {
                LogError("瓦片配置未设置");
                return false;
            }

            // 注意：不再清空整个Tilemap，以支持世界生成器的多房间渲染
            // 如需清空，应由WorldGenerator或单独的清理规则处理

            // 获取瓦片配置（使用Context中的随机主题）
            var config = _tileConfig.GetConfig(context.Theme);
            if (config.PlatformRuleTile == null)
            {
                LogWarning("当前主题没有配置平台瓦片");
                return true;
            }

            int mapWidth = context.MapWidth;
            int mapHeight = context.MapHeight;
            int platformCount = 0;

            // 渲染平台
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int value = context.GetTile(TilemapLayer.Platform, x, y);
                    if (value == 1)
                    {
                        // 应用世界偏移
                        int worldX = x + context.WorldOffset.x;
                        int worldY = y + context.WorldOffset.y;
                        _platformTilemap.SetTile(new Vector3Int(worldX, worldY, 0), config.PlatformRuleTile);
                        platformCount++;
                    }
                }
            }

            await UniTask.Yield(token);

            LogInfo($"平台渲染完成，共渲染 {platformCount} 个平台瓦片");
            return true;
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_platformTilemap == null)
            {
                errorMessage = "平台Tilemap未设置";
                return false;
            }

            if (_tileConfig == null)
            {
                errorMessage = "瓦片配置未设置";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
