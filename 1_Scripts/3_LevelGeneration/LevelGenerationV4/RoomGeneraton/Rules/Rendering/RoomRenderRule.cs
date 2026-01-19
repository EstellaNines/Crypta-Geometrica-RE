using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 房间渲染规则
    /// 只渲染有效房间（Context.RoomNodes中的房间）的背景层和地面层边框
    /// </summary>
    [Serializable]
    public class RoomRenderRule : GeneratorRuleBase
    {
        #region 引用

        [TitleGroup("引用")]
        [LabelText("瓦片配置")]
        [Required]
        [SerializeField]
        private TileConfigData _tileConfig;

        [TitleGroup("引用")]
        [LabelText("背景Tilemap")]
        [Required]
        [SerializeField]
        private Tilemap _backgroundTilemap;

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
                    UnityEngine.Debug.Log($"[RoomRenderRule] 自动找到 TileConfigData: {_tileConfig.name}");
                    changed = true;
                }
            }

            if (_backgroundTilemap == null)
            {
                _backgroundTilemap = TilemapFinder.FindTilemapByLayer(TilemapLayer.Background);
                if (_backgroundTilemap != null)
                {
                    UnityEngine.Debug.Log($"[RoomRenderRule] 自动找到背景 Tilemap: {_backgroundTilemap.name}");
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEngine.Debug.Log("[RoomRenderRule] 自动查找完成，请保存 Pipeline 资产");
            }
            else
            {
                UnityEngine.Debug.Log("[RoomRenderRule] 所有引用已设置，无需自动查找");
            }
        }
#endif

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public RoomRenderRule()
        {
            _ruleName = "RoomRenderRule";
            _executionOrder = 100; // 渲染规则在宏观规则之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始渲染有效房间...");

            if (context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogWarning("没有有效房间需要渲染");
                return true;
            }

            // 获取瓦片配置（使用Context中的随机主题）
            var config = _tileConfig.GetConfig(context.Theme);
            if (config == null)
            {
                LogError($"未找到主题 {context.Theme} 的瓦片配置");
                return false;
            }
            
            LogInfo($"当前主题: {context.Theme}");

            // 清空背景Tilemap
            _backgroundTilemap?.ClearAllTiles();

            // 渲染每个有效房间
            int roomCount = 0;
            foreach (var room in context.RoomNodes)
            {
                if (token.IsCancellationRequested)
                {
                    LogWarning("渲染被取消");
                    return false;
                }

                RenderRoom(context, room, config);
                roomCount++;

                // 每渲染几个房间让出控制权
                if (roomCount % 4 == 0)
                {
                    await UniTask.Yield(token);
                }
            }

            LogInfo($"渲染完成: {roomCount} 个房间");
            return true;
        }

        /// <summary>
        /// 渲染单个房间（只渲染背景层）
        /// </summary>
        private void RenderRoom(DungeonContext context, RoomNode room, ThemeTileConfig config)
        {
            BoundsInt bounds = room.WorldBounds;
            int startX = bounds.xMin;
            int startY = bounds.yMin;
            int width = bounds.size.x;
            int height = bounds.size.y;

            // 只渲染背景层（边界由 BorderEnforcementRule 在 GroundTileData 中处理）
            RenderBackground(context, startX, startY, width, height, config);
        }

        /// <summary>
        /// 渲染房间背景
        /// </summary>
        private void RenderBackground(DungeonContext context, int startX, int startY, int width, int height, ThemeTileConfig config)
        {
            if (_backgroundTilemap == null || config.BackgroundTiles == null || config.BackgroundTiles.Length == 0)
                return;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    // 随机选择背景瓦片
                    var tile = config.GetRandomBackgroundTile(context.RNG);
                    _backgroundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_tileConfig == null)
            {
                errorMessage = "瓦片配置未设置";
                return false;
            }

            if (_backgroundTilemap == null)
            {
                errorMessage = "背景Tilemap未设置";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
