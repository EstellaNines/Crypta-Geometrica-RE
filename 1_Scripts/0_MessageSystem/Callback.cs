/// <summary>
/// 消息系统委托定义
/// 支持0-3个参数的回调函数
/// </summary>

public delegate void Callback();
public delegate void Callback<T>(T arg1);
public delegate void Callback<T, U>(T arg1, U arg2);
public delegate void Callback<T, U, V>(T arg1, U arg2, V arg3);
