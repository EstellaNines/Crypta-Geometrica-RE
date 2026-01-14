/// <summary>
/// 游戏模块接口
/// 所有挂载到GameManager的系统必须实现此接口
/// </summary>
public interface IGameModule
{
    /// <summary>
    /// 模块初始化（在GameManager.Awake中按顺序调用）
    /// </summary>
    void OnInit();

    /// <summary>
    /// 模块轮询（在GameManager.Update中调用，可选实现）
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    void OnUpdate(float deltaTime);

    /// <summary>
    /// 模块销毁清理（在GameManager.OnDestroy中调用）
    /// </summary>
    void OnDispose();
}
