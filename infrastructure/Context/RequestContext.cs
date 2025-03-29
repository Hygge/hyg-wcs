namespace infrastructure.Context;

/// <summary>
/// 上下文数据
/// </summary>
public static class RequestContext
{
    // 定义 AsyncLocal 存储当前请求的上下文数据
    private static readonly AsyncLocal<RequestContextData> _currentContext = new();

    public static RequestContextData Current
    {
        get => _currentContext.Value;
        set => _currentContext.Value = value;
    }
}

/// <summary>
/// 上下文数据模型
/// </summary>
public class RequestContextData
{
    public string? userName { get; set; }
    public long? userId { get; set; }
}