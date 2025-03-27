using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace infrastructure.Utils
{
    public class HttpContextUtil
    {
        public static string getUserName(HttpContext _httpContext)
        {
           var  claim = _httpContext.User.Claims.FirstOrDefault(c => c.Type == "userName");
           if (null == claim)
           {
               return string.Empty;
           }
           return claim.Value;
        }

        public static string getUserId(HttpContext _httpContext)
        {
            var claim = _httpContext.User.Claims.FirstOrDefault(c => c.Type == "Id");
            if (null == claim)
            {
                return string.Empty;
            }
            return claim.Value;
        }
        public static async Task<string> getRequestParams(HttpContext context)
        {
            // 保证request.body可以重复读取
            context.Request.EnableBuffering();
            var contentType = context.Request.ContentType;
            Dictionary<string, string> dictionary = new();
            var query = context.Request.Query;
            if (query.Count > 0)
            {
                Dictionary<string, string> qs = new();
                foreach (var queryKey in query.Keys)
                    qs.Add(queryKey, query[queryKey]);
                

                return JsonConvert.SerializeObject(qs);
                // dictionary.Add("Query", JsonConvert.SerializeObject(qs));
            }
            if (contentType != null && contentType.Contains("multipart/form-data"))
            {
                try
                {
                    var form = context.Request.Form;
                    if (form.Count > 0)
                        return JsonConvert.SerializeObject(context.Request.Form);
                        // dictionary.Add("Form", JsonConvert.SerializeObject(context.Request.Form));
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
           
                }
            }
            if (contentType != null && contentType.Contains("application/json"))
            {
                StreamReader stream = new StreamReader(context.Request.Body);
                string body = await stream.ReadToEndAsync();
                context.Request.Body.Position = 0;
                if (!string.IsNullOrEmpty(body))
                    // dictionary.Add("body", body);
                    return body;
            }
            
            // return dictionary.Count > 0? JsonConvert.SerializeObject(dictionary) : string.Empty;
            return string.Empty;
        }


        public static string getRequestIp(HttpContext context)
        {
            // https://blog.csdn.net/qq_34897745/article/details/106093714
            //return context.Request.Headers["X-Real-IP"].FirstOrDefault();
            return context.Connection.RemoteIpAddress.ToString();
        }
        
        public static string GetLocalIp()
        {
            var addressList = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            var ips = addressList.Where(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(address => address.ToString()).ToArray();
            if (ips.Length == 1)
                return ips.First();
            return ips.Where(address => !address.EndsWith(".1")).FirstOrDefault() ?? ips.FirstOrDefault();
        }

    }
}
