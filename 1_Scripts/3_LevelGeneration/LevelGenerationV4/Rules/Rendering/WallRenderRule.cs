using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 墙壁渲染规则
    /// 只在有效房间区域的外围边缘绘制墙壁，相邻房间之间不绘制
    /// </summary>
    [Serializable]
    public class WallRenderRule : GeneratorRuleBase
    {
        #region 渲染配置

        [TitleGroup("渲染配置")]
        [LabelText("墙壁厚度")]
        [Range(1, 5)]
        [SerializeField]
        private int _wallThickness = 2;

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

        // 缓存有效房间的网格位置集合
        private HashSet<Vector2Int> _validRoomPositions;

        /// <summary>
        /// 构造函数
        /// </summary>
        public WallRenderRule()
        {
            _ruleName = "WallRenderRule";
            _executionOrder = 105; // 在RoomRenderRule之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始渲染智能墙壁边界...");

            if (context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogWarning("没有有效房间，跳过墙壁渲染");
                return true;
            }

            // 获取瓦片配置（使用Context中的随机主题）
            var config = _tileConfig.GetConfig(context.Theme);
            if (config == null)
            {
                LogError($"未找到主题 {context.Theme} 的瓦片配置");
                return false;
            }

            // 清空地面Tilemap
            _groundTilemap?.ClearAllTiles();

            // 构建有效房间位置集合
            _validRoomPositions = new HashSet<Vector2Int>();
            foreach (var room in context.RoomNodes)
            {
                _validRoomPositions.Add(room.GridPosition);
            }

            // 为每个房间渲染外围墙壁
            int roomCount = 0;
            foreach (var room in context.RoomNodes)
            {
                if (token.IsCancellationRequested)
                {
                    LogWarning("渲染被取消");
                    return false;
                }

                RenderRoomWalls(context, room, config);
                roomCount++;

                if (roomCount % 4 == 0)
                {
                    await UniTask.Yield(token);
                }
            }

            LogInfo($"墙壁渲染完成: {roomCount} 个房间");
            return true;
        }

        /// <summary>
        /// 渲染单个房间的外围墙壁（只在没有相邻房间的方向画墙）
        /// </summary>
        private void RenderRoomWalls(DungeonContext context, RoomNode room, ThemeTileConfig config)
        {
            if (_groundTilemap == null || config.GroundRuleTile == null)
                return;

            var tile = config.GroundRuleTile;
            BoundsInt bounds = room.WorldBounds;
            Vector2Int gridPos = room.GridPosition;

            int startX = bounds.xMin;
            int startY = bounds.yMin;
            int width = bounds.size.x;
            int height = bounds.size.y;

            // 检查四个方向是否有相邻房间
            bool hasTop = _validRoomPositions.Contains(gridPos + new Vector2Int(0, 1));
            bool hasBottom = _validRoomPositions.Contains(gridPos + new Vector2Int(0, -1));
            bool hasLeft = _validRoomPositions.Contains(gridPos + new Vector2Int(-1, 0));
            bool hasRight = _validRoomPositions.Contains(gridPos + new Vector2Int(1, 0));

            // 上边墙（如果上方没有相邻房间）
            if (!hasTop)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    for (int y = startY + height - _wallThickness; y < startY + height; y++)
                    {
                        _groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }

            // 下边墙（如果下方没有相邻房间）
            if (!hasBottom)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    for (int y = startY; y < startY + _wallThickness; y++)
                    {
                        _groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }

            // 左边墙（如果左方没有相邻房间）
            if (!hasLeft)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    for (int x = startX; x < startX + _wallThickness; x++)
                    {
                        _groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }

            // 右边墙（如果右方没有相邻房间）
            if (!hasRight)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    for (int x = startX + width - _wallThickness; x < startX + width; x++)
                    {
                        _groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
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
