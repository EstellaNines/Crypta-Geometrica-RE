using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 地牢生成器主控制器
    /// 负责异步执行管线中的所有规则
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        #region 序列化字段

        [TitleGroup("配置")]
        [LabelText("管线配置")]
        [Required("请指定管线配置文件")]
        [SerializeField]
        private DungeonPipelineData _pipeline;

        [TitleGroup("配置")]
        [LabelText("随机种子")]
        [Tooltip("-1表示使用系统时间")]
        [SerializeField]
        private int _seed = -1;

        [TitleGroup("Tilemap引用")]
        [LabelText("背景层 (Background)")]
        [SerializeField]
        private Tilemap _backgroundTilemap;

        [TitleGroup("Tilemap引用")]
        [LabelText("地面层 (Ground)")]
        [SerializeField]
        private Tilemap _groundTilemap;

        [TitleGroup("Tilemap引用")]
        [LabelText("平台层 (Platform)")]
        [SerializeField]
        private Tilemap _platformTilemap;

        [TitleGroup("碰撞体引用")]
        [LabelText("地面层复合碰撞体")]
        [SerializeField]
        private CompositeCollider2D _groundCompositeCollider;

        [TitleGroup("碰撞体引用")]
        [LabelText("平台层复合碰撞体")]
        [SerializeField]
        private CompositeCollider2D _platformCompositeCollider;

        [TitleGroup("瓦片配置")]
        [LabelText("瓦片配置数据")]
        [Required("请指定瓦片配置")]
        [SerializeField]
        private TileConfigData _tileConfig;

        /// <summary>
        /// 瓦片配置数据（只读）
        /// </summary>
        public TileConfigData TileConfig => _tileConfig;

        #endregion

        #region 运行时状态

        private DungeonContext _context;
        private CancellationTokenSource _cts;
        private bool _isGenerating;
        private int _generationCount = 0;

        /// <summary>
        /// 生成序号（从1开始）
        /// </summary>
        public int GenerationCount => _generationCount;

        /// <summary>
        /// 是否正在生成中
        /// </summary>
        public bool IsGenerating => _isGenerating;

        /// <summary>
        /// 当前上下文（只读）
        /// </summary>
        public DungeonContext Context => _context;

        /// <summary>
        /// 管线配置
        /// </summary>
        public DungeonPipelineData Pipeline
        {
            get => _pipeline;
            set => _pipeline = value;
        }

        #endregion

        #region 事件

        /// <summary>
        /// 生成开始事件
        /// </summary>
        public event Action<int> OnGenerationStarted;

        /// <summary>
        /// 生成完成事件
        /// </summary>
        public event Action<bool> OnGenerationCompleted;

        /// <summary>
        /// 规则执行事件 (规则名, 是否成功)
        /// </summary>
        public event Action<string, bool> OnRuleExecuted;

        #endregion

        #region 公开方法

        /// <summary>
        /// 异步生成地牢
        /// </summary>
        /// <param name="seed">随机种子，-1表示使用系统时间</param>
        /// <returns>生成是否成功</returns>
        public async UniTask<bool> GenerateDungeonAsync(int seed = -1)
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[DungeonGenerator] 生成正在进行中，请等待完成");
                return false;
            }

            if (_pipeline == null)
            {
                Debug.LogError("[DungeonGenerator] 未指定管线配置");
                return false;
            }

            // 验证配置
            if (!_pipeline.ValidateAll(out var errors))
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[DungeonGenerator] 配置错误: {error}");
                }
                return false;
            }

            _isGenerating = true;
            _generationCount++;

            Debug.Log($"<color=cyan>[DungeonGenerator] === 生成序号: #{_generationCount} ===</color>");

            // 初始化
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            int actualSeed = seed == -1 ? (_seed == -1 ? Environment.TickCount : _seed) : seed;

            // 创建上下文
            _context?.Dispose();
            _context = new DungeonContext(actualSeed)
            {
                Token = _cts.Token,
                GridColumns = _pipeline.GridColumns,
                GridRows = _pipeline.GridRows,
                RoomSize = _pipeline.RoomSize,
                MapWidth = _pipeline.TotalWidth,
                MapHeight = _pipeline.TotalHeight
            };

            // 分配三层地形数据数组
            int totalTiles = _context.MapWidth * _context.MapHeight;
            _context.BackgroundTileData = new int[totalTiles];
            _context.GroundTileData = new int[totalTiles];
            _context.PlatformTileData = new int[totalTiles];

            if (_pipeline.EnableLogging)
            {
                Debug.Log($"[DungeonGenerator] 开始生成，种子={actualSeed}，尺寸={_context.MapWidth}x{_context.MapHeight}");
            }

            OnGenerationStarted?.Invoke(actualSeed);

            bool success = true;

            try
            {
                // 获取已启用的规则
                var rules = _pipeline.GetEnabledRules();

                if (rules.Count == 0)
                {
                    Debug.LogWarning("[DungeonGenerator] 没有启用的规则");
                }

                // 按顺序执行规则
                foreach (var rule in rules)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        Debug.LogWarning("[DungeonGenerator] 生成被取消");
                        success = false;
                        break;
                    }

                    if (_pipeline.EnableLogging)
                    {
                        Debug.Log($"[DungeonGenerator] 执行规则: {rule.RuleName} (Order={rule.ExecutionOrder})");
                    }

                    // 规则自行决定是否需要切换线程
                    // 计算密集型规则在内部调用 UniTask.SwitchToThreadPool()
                    // 渲染规则需要在主线程执行 Unity API

                    bool ruleSuccess;
                    try
                    {
                        ruleSuccess = await rule.ExecuteAsync(_context, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning($"[DungeonGenerator] 规则被取消: {rule.RuleName}");
                        ruleSuccess = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DungeonGenerator] 规则执行异常: {rule.RuleName}\n{ex}");
                        ruleSuccess = false;
                    }

                    OnRuleExecuted?.Invoke(rule.RuleName, ruleSuccess);

                    if (!ruleSuccess)
                    {
                        Debug.LogError($"[DungeonGenerator] 规则执行失败: {rule.RuleName}");
                        success = false;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DungeonGenerator] 生成过程发生异常\n{ex}");
                success = false;
            }
            finally
            {
                _isGenerating = false;
            }

            if (_pipeline.EnableLogging)
            {
                Debug.Log($"[DungeonGenerator] 生成{(success ? "完成" : "失败")}");
            }

            OnGenerationCompleted?.Invoke(success);

            return success;
        }

        /// <summary>
        /// 取消当前生成
        /// </summary>
        public void CancelGeneration()
        {
            if (_isGenerating)
            {
                _cts?.Cancel();
                Debug.Log("[DungeonGenerator] 已发送取消请求");
            }
        }

        /// <summary>
        /// 清理生成数据
        /// </summary>
        public void ClearGeneration()
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[DungeonGenerator] 正在生成中，无法清理");
                return;
            }

            _context?.Dispose();
            _context = null;

            // 清空Tilemap
            _backgroundTilemap?.ClearAllTiles();
            _groundTilemap?.ClearAllTiles();
            _platformTilemap?.ClearAllTiles();

            Debug.Log("[DungeonGenerator] 已清理生成数据");
        }

        #endregion

        #region 生命周期

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _context?.Dispose();
        }

        #endregion

        #region Gizmos调试

        [TitleGroup("调试")]
        [InfoBox(
            "Gizmos图例:\n" +
            "🟦 蓝色 = 起点房间 (Start) - 玩家入口，位于顶行\n" +
            "🟧 橙色 = 终点房间 (End) - 关卡出口，位于底行\n" +
            "⬜ 灰色 = 普通房间 (Normal)\n" +
            "🟡 黄球 = 侧向门位置 (Left/Right)",
            InfoMessageType.None)]
        [LabelText("显示网格Gizmos")]
        [SerializeField]
        private bool _showGridGizmos = true;

        [TitleGroup("调试")]
        [LabelText("网格线颜色")]
        [SerializeField]
        private Color _gizmoColor = new Color(0f, 1f, 0f, 0.5f);

        [TitleGroup("调试")]
        [LabelText("起点颜色(蓝)")]
        [SerializeField]
        private Color _startRoomColor = new Color(0f, 0.5f, 1f, 0.3f);

        [TitleGroup("调试")]
        [LabelText("终点颜色(橙)")]
        [SerializeField]
        private Color _endRoomColor = new Color(1f, 0.3f, 0f, 0.3f);

        private void OnDrawGizmos()
        {
            if (!_showGridGizmos || _pipeline == null)
                return;

            int cols = _pipeline.GridColumns;
            int rows = _pipeline.GridRows;
            Vector2Int roomSize = _pipeline.RoomSize;
            Vector3 origin = transform.position;

            // 绘制网格线框
            Gizmos.color = _gizmoColor;

            // 绘制垂直线
            for (int x = 0; x <= cols; x++)
            {
                Vector3 start = origin + new Vector3(x * roomSize.x, 0, 0);
                Vector3 end = origin + new Vector3(x * roomSize.x, rows * roomSize.y, 0);
                Gizmos.DrawLine(start, end);
            }

            // 绘制水平线
            for (int y = 0; y <= rows; y++)
            {
                Vector3 start = origin + new Vector3(0, y * roomSize.y, 0);
                Vector3 end = origin + new Vector3(cols * roomSize.x, y * roomSize.y, 0);
                Gizmos.DrawLine(start, end);
            }

            // 绘制房间节点（如果有Context数据）
            if (_context?.RoomNodes != null)
            {
                foreach (var node in _context.RoomNodes)
                {
                    Vector3 roomCenter = origin + new Vector3(
                        (node.GridPosition.x + 0.5f) * roomSize.x,
                        (node.GridPosition.y + 0.5f) * roomSize.y,
                        0
                    );
                    Vector3 roomExtent = new Vector3(roomSize.x * 0.9f, roomSize.y * 0.9f, 0);

                    // 根据房间类型设置颜色
                    if (node.Type == RoomType.Start)
                        Gizmos.color = _startRoomColor;
                    else if (node.Type == RoomType.End)
                        Gizmos.color = _endRoomColor;
                    else
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

                    Gizmos.DrawCube(roomCenter, roomExtent);

                    // 绘制侧向门方向指示
                    if (node.RestrictedDoorSide == WallDirection.Left)
                    {
                        Gizmos.color = Color.yellow;
                        Vector3 doorPos = roomCenter + new Vector3(-roomSize.x * 0.45f, 0, 0);
                        Gizmos.DrawSphere(doorPos, roomSize.x * 0.05f);
                    }
                    else if (node.RestrictedDoorSide == WallDirection.Right)
                    {
                        Gizmos.color = Color.yellow;
                        Vector3 doorPos = roomCenter + new Vector3(roomSize.x * 0.45f, 0, 0);
                        Gizmos.DrawSphere(doorPos, roomSize.x * 0.05f);
                    }
                }
            }
        }

        #endregion

        #region 编辑器按钮

#if UNITY_EDITOR
        [TitleGroup("测试")]
        [Button("生成地牢", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        [DisableIf("_isGenerating")]
        private async void GenerateInEditor()
        {
            await GenerateDungeonAsync();
        }

        [TitleGroup("测试")]
        [Button("取消生成", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.4f)]
        [EnableIf("_isGenerating")]
        private void CancelInEditor()
        {
            CancelGeneration();
        }

        [TitleGroup("测试")]
        [Button("清理数据", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        [DisableIf("_isGenerating")]
        private void ClearInEditor()
        {
            ClearGeneration();
        }
#endif

        #endregion
    }
}
