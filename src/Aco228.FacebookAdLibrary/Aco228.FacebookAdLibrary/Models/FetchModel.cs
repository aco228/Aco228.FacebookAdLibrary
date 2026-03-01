using System.Text;
using System.Text.Json;
using Microsoft.Playwright;

namespace Aco228.FacebookAdLibrary.Models;

public class FetchModel
{
    public string Url { get; set; }
    public string Method { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? PostData { get; set; }
    private Dictionary<string, string> _postData = new();

    public FetchModel() { }
    public FetchModel(IRequest request)
    {
        Url = request.Url;
        Method = request.Method;
        Headers = request.Headers.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty);
        PostData = request.PostData;
    }

    public FetchModel Duplicate()
        => new()
        {
            Headers = Headers,
            PostData = PostData,
            Url = Url,
            Method = Method,
        };

    public Dictionary<string, string> GetPostData()
    {
        if (_postData.Any()) return _postData;
        foreach (var postData in PostData.Split('&'))
        {
            var postSplit = postData.Split("=");
            _postData.Add(postSplit[0], postSplit[1]);
        }
        return _postData;
    }

    public FetchModel ReplacePostData(string variableName, string newValue)
    {
        GetPostData()[variableName] = newValue;
        PostData = string.Join("&", GetPostData().Select(x => $"{x.Key}={x.Value}"));
        return this;
    }

    public string SaveAsFetch()
    {
        var sb = new StringBuilder();
        sb.Append("(async () => {");
        sb.AppendLine($"const res = await fetch(\"{Url}\", {{");

        if (!string.Equals(Method, "GET", StringComparison.OrdinalIgnoreCase))
            sb.AppendLine($"  method: \"{Method}\",");

        if (Headers.Any())
        {
            var headerJson = JsonSerializer.Serialize(Headers, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            sb.AppendLine($"  headers: {headerJson},");
        }

        if (!string.IsNullOrEmpty(PostData))
        {
            // Escape body safely for JS string
            var escapedBody = PostData
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");

            sb.AppendLine($"  body: \"{escapedBody}\",");
        }

        sb.AppendLine("  credentials: \"include\"");
        sb.Append("});");
        sb.Append("const txt = await res.text();");
        sb.Append("return txt;");
        sb.Append("})();");
        return sb.ToString();
    }
}