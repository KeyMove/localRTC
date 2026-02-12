using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

class SimpleHttpServer
{
    private static readonly Queue<string> dataStore = new Queue<string>();
    private const int MaxItems = 16;

    static void Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://*:8080/");
        
        listener.Start();
        Console.WriteLine("服务器启动，监听 http://*:8080/");
        Console.WriteLine("GET /?info=xxx  - 存储数据");
        Console.WriteLine("GET /           - 获取最近16条数据");

        while (true)
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            try
            {
                HandleRequest(request, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("错误: " + ex.Message);
                response.StatusCode = 500;
                var errorMsg = "{\"error\":\"" + EscapeJson(ex.Message) + "\"}";
                var errorBytes = Encoding.UTF8.GetBytes(errorMsg);
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                response.OutputStream.Close();
            }
        }
    }

    static void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        response.ContentType = "application/json";
        response.Headers.Add("Access-Control-Allow-Origin", "*");

        var query = HttpUtility.ParseQueryString(request.Url.Query);
        var info = query["info"];

        if (!string.IsNullOrEmpty(info))
        {
            lock (dataStore)
            {
                dataStore.Enqueue(info);
                if (dataStore.Count > MaxItems)
                    dataStore.Dequeue();
            }
            
            var responseBytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            Console.WriteLine("存储: " + info);
        }
        else
        {
            string[] items;
            lock (dataStore)
            {
                items = dataStore.ToArray();
            }
            
            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < items.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(items[i]);
            }
            sb.Append("]");
            
            var json = sb.ToString();
            var responseBytes = Encoding.UTF8.GetBytes(json);
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            Console.WriteLine("查询: 返回 " + items.Length + " 条数据");
        }

        response.OutputStream.Close();
    }

    static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}