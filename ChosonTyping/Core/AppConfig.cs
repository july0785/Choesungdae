using System.IO;
using System.Text.Json;

namespace ChosonTyping.Core;

/// <summary>설정은 전부 프로그람 옆 data\ 안에서 끝낸다(설계서 3항 — 포터블, %AppData% 금지).</summary>
public sealed class AppConfig
{
    public string Layout { get; set; } = "kukgyu";

    /// <summary>화면 결: "light"(낮) 또는 "dark"(밤).</summary>
    public string Theme { get; set; } = "light";

    public static string DataDir => Path.Combine(AppContext.BaseDirectory, "data");
    public static string LayoutsDir => Path.Combine(DataDir, "layouts");
    static string ConfigPath => Path.Combine(DataDir, "config.json");

    static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), Opts) ?? new();
        }
        catch (Exception)
        {
            // 깨진 설정은 기본값으로 되돌린다.
        }
        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, Opts));
    }
}
