/// <summary>
/// 消息数据封装类
/// 用于传递复杂数据时使用
/// </summary>
public class MessageData
{
    public MessageType Type { get; private set; }
    public object Data { get; private set; }

    public MessageData(MessageType type, object data = null)
    {
        Type = type;
        Data = data;
    }

    /// <summary>
    /// 获取指定类型的数据
    /// </summary>
    public T GetData<T>()
    {
        if (Data is T typedData)
        {
            return typedData;
        }
        return default(T);
    }
}
