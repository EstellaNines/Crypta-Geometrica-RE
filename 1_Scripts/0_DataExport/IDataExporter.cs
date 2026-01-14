/// <summary>
/// 数据导出器接口
/// 所有需要导出数据的系统都应实现此接口
/// </summary>
public interface IDataExporter
{
    /// <summary>
    /// 导出器名称（用于文件命名）
    /// </summary>
    string ExporterName { get; }

    /// <summary>
    /// 导出数据为JSON字符串
    /// </summary>
    /// <returns>JSON格式的数据</returns>
    string ExportToJson();

    /// <summary>
    /// 是否启用此导出器
    /// </summary>
    bool IsEnabled { get; }
}
