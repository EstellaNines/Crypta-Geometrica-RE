using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 世界生成器主控制器
    /// 管理世界生成的整体流程，执行规则管线
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        #region 配置引用

        [TitleGroup("配置")]
        [LabelText("世界管线配置")]
        [Required("必须指定世界管线配置")]
        [SerializeField]
        private WorldPipelineData _pipeline;

        [TitleGroup("配置")]
        [LabelText("房间生成器")]
        [Required("必须指定房间生成器")]
        [SerializeField]
        private DungeonGenerator _dungeonGenerator;

        #endregion

        #region 生成参数

        [TitleGroup("生成参数")]
        [LabelText("随机种子")]
        [Tooltip("-1 使用系统时间")]
        [SerializeField]
        private int _seed = -1;

        [TitleGroup("生成参数")]
        [LabelText("自动启动生成")]
        [Tooltip("场景加载时自动开始生成")]
        [SerializeField]
        private bool _autoGenerate = false;

        #endregion

        #region 调试

        [TitleGroup("调试")]
        [LabelText("启用日志")]
        [SerializeField]
        private bool _enableLogging = true;

        [TitleGroup("调试")]
        [LabelText("绘制Gizmos")]
        [SerializeField]
        private bool _drawGizmos = true;

        [TitleGroup("调试")]
        [LabelText("网格颜色")]
        [ShowIf("_drawGizmos")]
        [SerializeField]
        private Color _gridColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);

        [TitleGroup("调试")]
        [LabelText("房间颜色")]
        [ShowIf("_drawGizmos")]
        [SerializeField]
        private Color _roomColor = new Color(0.2f, 0.5f, 1f, 0.7f);

        [TitleGroup("调试")]
        [LabelText("入口颜色")]
        [ShowIf("_drawGizmos")]
        [SerializeField]
        private Color _entranceColor = Color.green;

        [TitleGroup("调试")]
        [LabelText("出口颜色")]
        [ShowIf("_drawGizmos")]
        [SerializeField]
        private Color _exitColor = Color.yellow;

        #endregion

        #region 状态

        [TitleGroup("状态")]
        [LabelText("当前上下文")]
        [ShowInInspector]
        [ReadOnly]
        private WorldContext _context;

        [TitleGroup("状态")]
        [LabelText("是否生成中")]
        [ShowInInspector]
        [ReadOnly]
        private bool _isGenerating;

        [TitleGroup("状态")]
        [LabelText("上次生成时间(ms)")]
        [ShowInInspector]
        [ReadOnly]
        private long _lastGenerationTimeMs;

        private CancellationTokenSource _cts;

        #endregion

        #region 公共属性

        /// <summary>
        /// 世界管线配置
        /// </summary>
        public WorldPipelineData Pipeline => _pipeline;

        /// <summary>
        /// 房间生成器
        /// </summary>
        public DungeonGenerator DungeonGenerator => _dungeonGenerator;

        /// <summary>
        /// 当前上下文
        /// </summary>
        public WorldContext Context => _context;

        /// <summary>
        /// 是否正在生成
        /// </summary>
        public bool IsGenerating => _isGenerating;

        /// <summary>
        /// 世界节点列表
        /// </summary>
        public List<WorldNode> Nodes => _context?.Nodes;

        #endregion

        #region 事件

        /// <summary>
        /// 生成开始事件
        /// </summary>
        public event Action OnGenerationStarted;

        /// <summary>
        /// 生成完成事件
        /// </summary>
        public event Action<bool> OnGenerationCompleted;

        /// <summary>
        /// 生成进度事件 (当前规则索引, 总规则数)
        /// </summary>
        public event Action<int, int> OnGenerationProgress;

        #endregion

        #region Unity生命周期

        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateWorldAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            CancelGeneration();
            _context?.Dispose();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 异步生成世界
        /// </summary>
        /// <param name="seed">随机种子（-1使用配置值）</param>
        /// <returns>是否成功</returns>
        public async UniTask<bool> GenerateWorldAsync(int seed = -1)
        {
            if (_isGenerating)
            {
                LogWarning("已有生成任务在运行中");
                return false;
            }

            // 验证配置
            if (!ValidateConfiguration())
            {
                return false;
            }

            _isGenerating = true;
            OnGenerationStarted?.Invoke();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 创建取消令牌
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                // 确定种子
                int actualSeed = seed != -1 ? seed : _seed;
                LogInfo($"开始生成世界 (种子: {actualSeed})");

                // 清除之前的生成结果（但不取消当前任务）
                if (_dungeonGenerator != null)
                {
                    _dungeonGenerator.ClearGeneration();
                }

                // 创建上下文
                _context?.Dispose();
                _context = new WorldContext(
                    _pipeline.RoomCount,
                    _pipeline.RoomPixelSize,
                    actualSeed
                );
                _context.Token = _cts.Token;
                _context.DungeonGenerator = _dungeonGenerator;

                // 获取启用的规则
                var rules = _pipeline.GetEnabledRules();
                if (rules.Count == 0)
                {
                    LogError("没有启用的规则");
                    return false;
                }

                LogInfo($"执行 {rules.Count} 条规则");

                // 执行规则管线
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];

                    if (_cts.Token.IsCancellationRequested)
                    {
                        LogWarning("生成已取消");
                        return false;
                    }

                    OnGenerationProgress?.Invoke(i, rules.Count);

                    LogInfo($"执行规则 [{i + 1}/{rules.Count}]: {rule.RuleName}");

                    bool success = await rule.ExecuteAsync(_context, _cts.Token);

                    if (!success)
                    {
                        LogError($"规则执行失败: {rule.RuleName}");
                        return false;
                    }
                }

                stopwatch.Stop();
                _lastGenerationTimeMs = stopwatch.ElapsedMilliseconds;

                LogInfo($"世界生成完成 ({_context.Nodes.Count} 个房间, 耗时: {_lastGenerationTimeMs}ms)");

                return true;
            }
            catch (OperationCanceledException)
            {
                LogWarning("生成已取消");
                return false;
            }
            catch (Exception e)
            {
                LogError($"生成异常: {e.Message}\n{e.StackTrace}");
                return false;
            }
            finally
            {
                _isGenerating = false;
                OnGenerationCompleted?.Invoke(_context?.Nodes?.Count > 0);
            }
        }

        /// <summary>
        /// 取消生成
        /// </summary>
        public void CancelGeneration()
        {
            if (_isGenerating && _cts != null)
            {
                LogInfo("取消生成...");
                _cts.Cancel();
            }
        }

        /// <summary>
        /// 清除生成结果
        /// </summary>
        public void ClearGeneration()
        {
            CancelGeneration();

            _context?.Dispose();
            _context = null;

            // 清除房间生成器的结果
            if (_dungeonGenerator != null)
            {
                _dungeonGenerator.ClearGeneration();
            }

            LogInfo("生成结果已清除");
        }

        #endregion

        #region 编辑器按钮

        [TitleGroup("操作")]
        [Button("生成世界", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void EditorGenerateWorld()
        {
            GenerateWorldAsync().Forget();
        }

        [TitleGroup("操作")]
        [Button("取消生成", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.2f)]
        [ShowIf("_isGenerating")]
        private void EditorCancelGeneration()
        {
            CancelGeneration();
        }

        [TitleGroup("操作")]
        [Button("清除结果", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void EditorClearGeneration()
        {
            ClearGeneration();
        }

        [TitleGroup("配置")]
        [Button("验证配置", ButtonSizes.Medium)]
        private void EditorValidateConfiguration()
        {
            if (ValidateConfiguration())
            {
                Debug.Log("[WorldGenerator] ✓ 配置验证通过");
            }
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        private bool ValidateConfiguration()
        {
            if (_pipeline == null)
            {
                LogError("未指定世界管线配置");
                return false;
            }

            if (_dungeonGenerator == null)
            {
                LogError("未指定房间生成器");
                return false;
            }

            if (!_pipeline.ValidateAll(out var errors))
            {
                foreach (var error in errors)
                {
                    LogError($"配置错误: {error}");
                }
                return false;
            }

            return true;
        }

        #endregion

        #region 日志

        private void LogInfo(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[WorldGenerator] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_enableLogging)
            {
                Debug.LogWarning($"[WorldGenerator] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[WorldGenerator] {message}");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;
            if (_pipeline == null) return;

            var gridSize = _pipeline.GridSize;
            var roomSize = _pipeline.RoomPixelSize;

            // 绘制网格
            Gizmos.color = _gridColor;
            for (int x = 0; x <= gridSize; x++)
            {
                var startX = new Vector3(x * roomSize.x, 0, 0);
                var endX = new Vector3(x * roomSize.x, gridSize * roomSize.y, 0);
                Gizmos.DrawLine(startX, endX);
            }
            for (int y = 0; y <= gridSize; y++)
            {
                var startY = new Vector3(0, y * roomSize.y, 0);
                var endY = new Vector3(gridSize * roomSize.x, y * roomSize.y, 0);
                Gizmos.DrawLine(startY, endY);
            }

            // 绘制已放置的房间
            if (_context?.Nodes != null)
            {
                for (int i = 0; i < _context.Nodes.Count; i++)
                {
                    // [FIX] 每次迭代重置房间颜色，防止出入口颜色污染后续房间
                    Gizmos.color = _roomColor;
                    var node = _context.Nodes[i];
                    var center = new Vector3(
                        node.WorldPixelOffset.x + roomSize.x / 2f,
                        node.WorldPixelOffset.y + roomSize.y / 2f,
                        0
                    );
                    var size = new Vector3(roomSize.x * 0.9f, roomSize.y * 0.9f, 1);
                    Gizmos.DrawCube(center, size);

#if UNITY_EDITOR
                    // 绘制房间序号（大字体居中）
                    var style = new GUIStyle
                    {
                        fontSize = 24,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    UnityEditor.Handles.Label(center, (i + 1).ToString(), style);

                    // 绘制出入口位置（十字准星样式）
                    if (node.HasEntranceExitData)
                    {
                        // 入口位置（使用配置颜色）
                        Gizmos.color = _entranceColor;
                        var entrancePos = new Vector3(node.EntrancePosition.x, node.EntrancePosition.y, 0);
                        DrawCrosshair(entrancePos, 3f);
                        
                        // 入口标签
                        var entranceStyle = new GUIStyle
                        {
                            fontSize = 10,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = _entranceColor }
                        };
                        UnityEditor.Handles.Label(entrancePos + Vector3.up * 5, "IN", entranceStyle);

                        // 出口位置（使用配置颜色）
                        Gizmos.color = _exitColor;
                        var exitPos = new Vector3(node.ExitPosition.x, node.ExitPosition.y, 0);
                        DrawCrosshair(exitPos, 3f);
                        
                        // 出口标签
                        var exitStyle = new GUIStyle
                        {
                            fontSize = 10,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = _exitColor }
                        };
                        UnityEditor.Handles.Label(exitPos + Vector3.up * 5, "OUT", exitStyle);
                    }
#endif
                }
            }
        }

        /// <summary>
        /// 绘制十字准星
        /// </summary>
        private void DrawCrosshair(Vector3 center, float size)
        {
            // 圆圈
            Gizmos.DrawWireSphere(center, size);
            // 十字线
            Gizmos.DrawLine(center + Vector3.left * size * 1.5f, center + Vector3.right * size * 1.5f);
            Gizmos.DrawLine(center + Vector3.up * size * 1.5f, center + Vector3.down * size * 1.5f);
        }

        #endregion
    }
}
