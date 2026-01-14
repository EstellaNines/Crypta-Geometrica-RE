/// <summary>
/// 可保存对象接口
/// 所有需要持久化状态的游戏对象必须实现此接口
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 唯一标识符
    /// 用于在加载时匹配数据到对应的对象
    /// 通常由 SaveableEntity 组件提供
    /// </summary>
    string SaveID { get; }

    /// <summary>
    /// 捕获当前状态
    /// 返回需要保存的数据对象
    /// </summary>
    /// <returns>可序列化的状态数据</returns>
    object CaptureState();

    /// <summary>
    /// 恢复状态
    /// 接收保存的数据并恢复对象状态
    /// </summary>
    /// <param name="state">之前保存的状态数据</param>
    void RestoreState(object state);
}
