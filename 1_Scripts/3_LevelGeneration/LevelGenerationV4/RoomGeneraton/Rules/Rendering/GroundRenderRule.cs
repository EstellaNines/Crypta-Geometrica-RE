using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 地面层渲染规则
    /// 将context.GroundTileData渲染到Ground Tilemap
    /// </summary>
    [Serializable]
    public class GroundRenderRule : GeneratorRuleBase
    {
        #region 渲染配置

        [TitleGroup("渲染配置")]
        [LabelText("批量渲染块大小")]
        [Tooltip("每次批量设置的瓦片数量，避免单次操作过大")]
        [Range(100, 10000)]
        [SerializeField]
        private int _batchSize = 1000;

        #endregion

        #region 引用

        [TitleGroup("引用")]
        [LabelText("瓦片配置")]
        [Required]
        [SerializeField]
        private TileConfigData _tileConfig;

        [TitleGroup("引用")]
        [LabelText("地面Tilemap")]
        [Required]
        [SerializeField]
        private Tilemap _groundTilemap;

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
                    UnityEngine.Debug.Log($"[GroundRenderRule] 自动找到 TileConfigData: {_tileConfig.name}");
                    changed = true;
                }
            }

            if (_groundTilemap == null)
            {
                _groundTilemap = TilemapFinder.FindTilemapByLayer(TilemapLayer.Ground);
                if (_groundTilemap != null)
                {
                    UnityEngine.Debug.Log($"[GroundRenderRule] 自动找到地面 Tilemap: {_groundTilemap.name}");
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEngine.Debug.Log("[GroundRenderRule] 自动查找完成，请保存 Pipeline 资产");
            }
            else
            {
                UnityEngine.Debug.Log("[GroundRenderRule] 所有引用已设置，无需自动查找");
            }
        }
#endif

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public GroundRenderRule()
        {
            _ruleName = "GroundRenderRule";
            _executionOrder = 110; // 在RoomRenderRule之后执行，负责渲染 GroundTileData
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始渲染地面层...");

            if (context.GroundTileData == null)
            {
                LogWarning("GroundTileData为空，跳过地面层渲染");
                return true;
            }

            // 获取瓦片配置（使用Context中的随机主题）
            var config = _tileConfig.GetConfig(context.Theme);
            if (config == null)
            {
                LogError($"未找到主题 {context.Theme} 的瓦片配置");
                return false;
            }

            if (config.GroundRuleTile == null)
            {
                LogError("地面规则瓦片未设置");
                return false;
            }

            // 清空地面Tilemap
            _groundTilemap?.ClearAllTiles();

            int mapWidth = context.MapWidth;
            int mapHeight = context.MapHeight;
            int totalTiles = mapWidth * mapHeight;

            // 使用批量设置优化性能
            int tilesProcessed = 0;
            int tilesRendered = 0;

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    if (token.IsCancellationRequested)
                    {
                        LogWarning("渲染被取消");
                        return false;
                    }

                    int index = y * mapWidth + x;
                    int value = context.GroundTileData[index];

                    // 只渲染实心格子（value == 1）
                    if (value == 1)
                    {
                        _groundTilemap.SetTile(new Vector3Int(x, y, 0), config.GroundRuleTile);
                        tilesRendered++;
                    }

                    tilesProcessed++;

                    // 每处理一批让出控制权
                    if (tilesProcessed % _batchSize == 0)
                    {
                        await UniTask.Yield(token);
                    }
                }
            }

            LogInfo($"地面层渲染完成: 处理了 {tilesProcessed} 个格子，渲染了 {tilesRendered} 个实心瓦片");
            return true;
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_tileConfig == null)
            {
                errorMessage = "瓦片配置未设置";
                return false;
            }

            if (_groundTilemap == null)
            {
                errorMessage = "地面Tilemap未设置";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
