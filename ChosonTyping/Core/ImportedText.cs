using System.IO;
using System.Text.Json;

namespace ChosonTyping.Core;

/// <summary>
/// 외부 긴글 불러오기(설계서 10.3). 긴글만 .txt를 불러와 전용형식 .ctp로
/// data\imported\에 읽기전용 저장한다. 다시 열 때마다 해시를 다시 견준다.
/// 파일련결(레지스트리)은 하지 않는다 — 《불러오기》 단추로만 연다.
/// </summary>
public static class ImportedText
{
    public static string ImportedDir => Path.Combine(AppConfig.DataDir, "imported");

    static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static ContentModule Save(string title, string source, string body)
    {
        Directory.CreateDirectory(ImportedDir);
        string id = "imported-" + DateTime.Now.ToString("yyMMdd-HHmmss");
        var m = new ContentModule
        {
            Id = id,
            Type = "longtext",
            Title = title,
            Source = source,
            Locked = true,
            Body = body.Replace("\r\n", "\n").Trim(),
        };
        m.Hash = "sha256:" + ContentModule.ComputeHash(m);

        string path = Path.Combine(ImportedDir, id + ".ctp");
        File.WriteAllText(path, JsonSerializer.Serialize(m, Opts));
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
        return m;
    }

    public static (List<ContentModule> Modules, List<string> Errors) LoadAll() =>
        ContentModule.LoadDir(ImportedDir, "*.ctp");
}
