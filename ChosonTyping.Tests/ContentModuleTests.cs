using System.IO;
using ChosonTyping.Core;

namespace ChosonTyping.Tests;

public class ContentModuleTests
{
    static string TempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "choseungdae-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    static string WriteModule(string dir, string id, string json)
    {
        var p = Path.Combine(dir, id + ".json");
        File.WriteAllText(p, json);
        return p;
    }

    [Fact]
    public void 옳은_모듈은_열린다()
    {
        var m = new ContentModule { Id = "word-90", Type = "word", Title = "시험", Locked = true, Items = new() { "가", "나" } };
        m.Hash = "sha256:" + ContentModule.ComputeHash(m);
        var dir = TempDir();
        var p = WriteModule(dir, "word-90",
            $"{{\"id\":\"word-90\",\"type\":\"word\",\"title\":\"시험\",\"locked\":true,\"hash\":\"{m.Hash}\",\"items\":[\"가\",\"나\"]}}");
        var loaded = ContentModule.Load(p);
        Assert.Equal(2, loaded.Items!.Count);
    }

    [Fact]
    public void 본문이_바뀌면_열기오유()
    {
        var m = new ContentModule { Id = "word-91", Items = new() { "가", "나" } };
        m.Hash = "sha256:" + ContentModule.ComputeHash(m);
        var dir = TempDir();
        var p = WriteModule(dir, "word-91",
            $"{{\"id\":\"word-91\",\"type\":\"word\",\"title\":\"시험\",\"locked\":true,\"hash\":\"{m.Hash}\",\"items\":[\"가\",\"다\"]}}");
        Assert.Throws<ModuleOpenException>(() => ContentModule.Load(p));
    }

    [Fact]
    public void 화일이름_번호를_고치면_열기오유()
    {
        var m = new ContentModule { Id = "word-92", Items = new() { "가" } };
        m.Hash = "sha256:" + ContentModule.ComputeHash(m);
        var dir = TempDir();
        var p = WriteModule(dir, "word-99",
            $"{{\"id\":\"word-92\",\"type\":\"word\",\"title\":\"시험\",\"locked\":true,\"hash\":\"{m.Hash}\",\"items\":[\"가\"]}}");
        Assert.Throws<ModuleOpenException>(() => ContentModule.Load(p));
    }

    [Fact]
    public void 폴더_읽기는_깨진_모듈만_건너뛴다()
    {
        var dir = TempDir();
        var good = new ContentModule { Id = "word-93", Items = new() { "가" } };
        good.Hash = "sha256:" + ContentModule.ComputeHash(good);
        WriteModule(dir, "word-93",
            $"{{\"id\":\"word-93\",\"type\":\"word\",\"title\":\"시험\",\"locked\":true,\"hash\":\"{good.Hash}\",\"items\":[\"가\"]}}");
        WriteModule(dir, "word-94",
            "{\"id\":\"word-94\",\"type\":\"word\",\"title\":\"시험\",\"locked\":true,\"hash\":\"sha256:0000\",\"items\":[\"가\"]}");
        var (ok, errors) = ContentModule.LoadDir(dir);
        Assert.Single(ok);
        Assert.Single(errors);
    }

    [Fact]
    public void 내장_모듈이_전부_옳게_열린다()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        foreach (var sub in new[] { "words", "sentences", "longtext" })
        {
            var (ok, errors) = ContentModule.LoadDir(Path.Combine(dataDir, sub));
            Assert.Empty(errors);
            Assert.NotEmpty(ok);
        }
    }
}
