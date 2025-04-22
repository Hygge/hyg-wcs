namespace infrastructure.Context;

internal class RequestContextDisposable : IDisposable
{
    internal RequestContextDisposable() { }
    
    public void Dispose()
    {
        RequestContext.ClearContext();
    }
}