using System.Net.Http;
using System.Text;
using System.Text.Json;
namespace Kirimise;

public class ChatService
{
    static readonly string BaseUrl = (Environment.GetEnvironmentVariable("IA_BASE_URL") ?? "https://api.groq.com/openai/v1").TrimEnd('/');
    static readonly string[] Preferidos =
    {
        "openai/gpt-oss-120b", "openai/gpt-oss-20b", "llama-3.3-70b-versatile",
        "llama-3.1-8b-instant", "qwen/qwen3-32b", "meta-llama/llama-4-scout-17b-16e-instruct"
    };
    static readonly string[] Descartar = { "whisper", "guard", "tts", "orpheus", "embed", "distil", "safeguard", "compound" };
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    readonly List<Dictionary<string, string>> _history = new();
    string? _model = Environment.GetEnvironmentVariable("IA_MODEL");

    public string? ApiKey { get; set; } = string.IsNullOrWhiteSpace(Secrets.GroqKey) ? Environment.GetEnvironmentVariable("GROQ_API_KEY") : Secrets.GroqKey;
    public string? ModelInUse => _model;
    public void Reset() => _history.Clear();

    public void DropLastExchange()
    {
        for (int i = 0; i < 2 && _history.Count > 0; i++) _history.RemoveAt(_history.Count - 1);
    }

    HttpRequestMessage Req(HttpMethod m, string path)
    {
        var r = new HttpRequestMessage(m, BaseUrl + path);
        r.Headers.Add("Authorization", "Bearer " + ApiKey);
        r.Headers.Add("User-Agent", "Kirimise/1.0");
        return r;
    }

    async Task<string> ElegirModeloAsync()
    {
        using var req = Req(HttpMethod.Get, "/models");
        using var res = await Http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new Exception($"API {(int)res.StatusCode}: {json}");
        using var doc = JsonDocument.Parse(json);
        var ids = doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(m => m.GetProperty("id").GetString() ?? "").Where(i => i.Length > 0).ToList();
        foreach (var p in Preferidos) if (ids.Contains(p)) return p;
        var otro = ids.FirstOrDefault(i => !Descartar.Any(d => i.Contains(d, StringComparison.OrdinalIgnoreCase)));
        return otro ?? throw new Exception("No encontré ningún modelo de chat disponible en tu cuenta.");
    }

    public async Task<string> AskAsync(string userText, string systemPrompt)
    {
        _history.Add(new() { ["role"] = "user", ["content"] = userText });
        if (_history.Count > 20) _history.RemoveRange(0, _history.Count - 20);
        try
        {
            for (int intento = 0; ; intento++)
            {
                _model ??= await ElegirModeloAsync();
                var messages = new List<Dictionary<string, string>> { new() { ["role"] = "system", ["content"] = systemPrompt } };
                messages.AddRange(_history);

                var payload = new Dictionary<string, object>
                {
                    ["model"] = _model, ["messages"] = messages, ["temperature"] = 0.5, ["max_tokens"] = 1500
                };
                if (_model.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase)) payload["reasoning_effort"] = "low";

                using var req = Req(HttpMethod.Post, "/chat/completions");
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var res = await Http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    if (intento == 0 && json.Contains("model_not_found")) { _model = null; continue; }
                    throw new Exception($"API {(int)res.StatusCode}: {json}");
                }

                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "";
                if (text.Length == 0) throw new Exception("Kirimi no pudo responder esa vez. Intenta de nuevo.");
                _history.Add(new() { ["role"] = "assistant", ["content"] = text });
                return text;
            }
        }
        catch
        {
            _history.RemoveAt(_history.Count - 1);
            throw;
        }
    }
}
