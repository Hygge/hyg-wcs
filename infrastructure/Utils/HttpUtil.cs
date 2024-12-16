using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using infrastructure.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace infrastructure.Utils;

[Component(ServiceLifetime.Singleton)]
public class HttpUtil
{

    private readonly ILogger<HttpUtil> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpUtil(ILogger<HttpUtil> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// get method async request
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string>? headers)
    {
        using var client = _httpClientFactory.CreateClient();
        if (headers is not null)
        {
            foreach (var keyValuePair in headers)
            {
                client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        return await client.GetAsync(new Uri(url));
        
    }
    /// <summary>
    /// post method request params to json
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> PostAsJsonAsync(string url, Dictionary<string, string>? headers, object content)
    {
        using var client = _httpClientFactory.CreateClient();
        if (headers is not null)
        {
            foreach (var keyValuePair in headers)
            {
                client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        return await client.PostAsJsonAsync(new Uri(url), content);
    }
    /// <summary>
    /// post method request params to  Encoding.UTF8 json
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> PostAsJsonUtf8Async(string url, Dictionary<string, string>? headers, object content)
    {
        using var client = _httpClientFactory.CreateClient();
        if (headers is not null)
        {
            foreach (var keyValuePair in headers)
            {
                client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        StringContent stringContent = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
        return await client.PostAsync(new Uri(url), stringContent);
    }
    

    /// <summary>
    /// get method synchronization request
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public HttpResponseMessage Get(string url, Dictionary<string, string>? headers)
    {
        using var client = _httpClientFactory.CreateClient();
        if (headers is not null)
        {
            foreach (var keyValuePair in headers)
            {
                client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        return client.GetAsync(new Uri(url)).GetAwaiter().GetResult();
    }





}