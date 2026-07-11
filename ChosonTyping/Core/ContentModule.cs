using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChosonTyping.Core;

/// <summary>모듈 열기오유(설계서 10.2) — 무결성검사가 어긋난 모듈은 열지 않고 건너뛴다.</summary>
public sealed class ModuleOpenException : Exception
{
    public string FilePath { get; }

    public ModuleOpenException(string path, string message)
        : base(message) => FilePath = path;
}

/// <summary>
/// 련습 콘텐츠 모듈(설계서 10항). 낱말·문장 모듈은 items를, 긴글·.ctp는 body를 해시한다.
/// 해시: sha256(UTF-8(items를 "\n"로 이은 것)) 또는 sha256(UTF-8(body)).
/// </summary>
public sealed class ContentModule
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Source { get; set; }
    public bool Locked { get; set; }
    public string Hash { get; set; } = "";
    public List<string>? Items { get; set; }
    public string? Body { get; set; }

    static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static ContentModule Load(string path)
    {
        ContentModule? m;
        try
        {
            m = JsonSerializer.Deserialize<ContentModule>(File.ReadAllText(path), Opts);
        }
        catch (Exception e)
        {
            throw new ModuleOpenException(path, $"화일을 읽을수 없습니다 ({e.Message})");
        }
        if (m is null) throw new ModuleOpenException(path, "화일이 비었습니다");

        string fname = Path.GetFileNameWithoutExtension(path);
        if (!string.Equals(fname, m.Id, StringComparison.OrdinalIgnoreCase))
            throw new ModuleOpenException(path, $"화일이름({fname})이 머리부 id({m.Id})와 다릅니다");

        if (!string.Equals(m.Hash, "sha256:" + ComputeHash(m), StringComparison.OrdinalIgnoreCase))
            throw new ModuleOpenException(path, "본문이 머리부 hash와 어긋납니다");

        return m;
    }

    public static string ComputeHash(ContentModule m)
    {
        string payload = m.Items is not null ? string.Join("\n", m.Items) : m.Body ?? "";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    /// <summary>폴더의 모듈을 전부 읽는다. 어긋난 모듈은 건너뛰고 오유 목록에 담는다.</summary>
    public static (List<ContentModule> Modules, List<string> Errors) LoadDir(string dir, string pattern = "*.json")
    {
        var ok = new List<ContentModule>();
        var errors = new List<string>();
        if (!Directory.Exists(dir)) return (ok, errors);
        foreach (var f in Directory.GetFiles(dir, pattern).OrderBy(f => f))
        {
            try
            {
                ok.Add(Load(f));
            }
            catch (ModuleOpenException e)
            {
                errors.Add($"《{Path.GetFileName(f)}》 — {e.Message}");
            }
        }
        return (ok, errors);
    }
}
