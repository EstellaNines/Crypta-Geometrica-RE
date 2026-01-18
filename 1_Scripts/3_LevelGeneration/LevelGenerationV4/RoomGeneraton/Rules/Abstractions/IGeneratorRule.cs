using System.Threading;
using Cysharp.Threading.Tasks;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 生成规则通用接口
    /// 所有宏观/微观/渲染规则必须实现此接口
    /// </summary>
    public interface IGeneratorRule
    {
        /// <summary>
        /// 规则显示名称（用于Inspector和日志）
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// 是否启用此规则
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 规则执行顺序（越小越先执行）
        /// </summary>
        int ExecutionOrder { get; }

        /// <summary>
        /// 异步执行生成逻辑
        /// </summary>
        /// <param name="context">共享的黑板数据</param>
        /// <param name="token">取消令牌</param>
        /// <returns>执行是否成功</returns>
        UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token);

        /// <summary>
        /// 验证规则配置是否有效
        /// </summary>
        /// <param name="errorMessage">错误信息（如果验证失败）</param>
        /// <returns>配置是否有效</returns>
        bool Validate(out string errorMessage);
    }
}
