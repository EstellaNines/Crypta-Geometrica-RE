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

            // 清空平台层
            _platformTilemap.ClearAllTiles();

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
                        _platformTilemap.SetTile(new Vector3Int(x, y, 0), config.PlatformRuleTile);
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
