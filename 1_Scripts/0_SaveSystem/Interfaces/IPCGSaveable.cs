/// <summary>
/// PCG 程序化生成可保存接口
/// 用于保存和恢复程序化生成关卡的种子和元数据
/// </summary>
public interface IPCGSaveable
{
    /// <summary>
    /// 主种子
    /// 用于重新生成完整关卡
    /// </summary>
    int MasterSeed { get; }

    /// <summary>
    /// 捕获 PCG 状态
    /// 返回种子和必要的元数据
    /// </summary>
    /// <returns>PCG 保存数据</returns>
    PCGSaveData CapturePCGState();

    /// <summary>
    /// 恢复 PCG 状态
    /// 使用保存的种子重新生成关卡
    /// </summary>
    /// <param name="state">之前保存的 PCG 数据</param>
    void RestorePCGState(PCGSaveData state);
}
