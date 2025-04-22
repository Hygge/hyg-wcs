namespace infrastructure.Context;

/// <summary>
/// 上下文数据
/// </summary>
public static class RequestContext
{
    // 定义 AsyncLocal 存储当前请求的上下文数据
    private static readonly AsyncLocal<RequestContextData> _currentContext = new();

    public static RequestContextData Current => _currentContext.Value;
    
    /// <summary>
    /// 将请求上下文设置到线程全局区域
    /// </summary>
    /// <param name="userContext"></param>
    public static IDisposable SetContext(RequestContextData userContext)
    {
        _currentContext.Value = userContext;
        return new RequestContextDisposable();
    }
    
    /// <summary>
    /// 清除上下文
    /// </summary>
    public static void ClearContext()
    {
        _currentContext.Value = null;
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