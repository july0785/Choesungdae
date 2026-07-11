using System.Windows;
using ChosonTyping.Core;

namespace ChosonTyping;

public partial class App : Application
{
    /// <summary>지금 적용된 화면형식("light"/"dark") — 설정이 auto면 계통값으로 풀린다.</summary>
    public static string ResolvedTheme { get; private set; } = "light";

    /// <summary>개발용: --stage n 으로 켜면 그 련습화면에서 바로 시작한다.</summary>
    public static int? StartupStage { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme(Resolve(AppConfig.Load().Theme));
        int at = Array.IndexOf(e.Args, "--stage");
        if (at >= 0 && at + 1 < e.Args.Length && int.TryParse(e.Args[at + 1], out int stage))
            StartupStage = stage;
        if (e.Args.Contains("--smoke")) RunSmoke();
    }

    /// <summary>자가진단: 모든 화면을 실제로 만들어보고 결과를 smoke.log에 적고 끝낸다.</summary>
    void RunSmoke()
    {
        string log = System.IO.Path.Combine(AppContext.BaseDirectory, "smoke.log");
        try
        {
            var layoutPath = System.IO.Path.Combine(AppConfig.LayoutsDir, "kukgyu.json");
            var layout = KeyboardLayout.Load(layoutPath);
            var mw = new MainWindow();
            _ = new Views.KeyDrillView(mw, layout);
            _ = new Views.WordView(mw, layout);
            _ = new Views.SentenceView(mw, layout);
            _ = new Views.TextListView(mw, layout, isTest: false);
            var (longs, _) = ContentModule.LoadDir(System.IO.Path.Combine(AppConfig.DataDir, "longtext"));
            if (longs.Count > 0) _ = new Views.LongTextView(mw, layout, longs[0], isTest: true);
            _ = new Views.AcidRainView(mw, layout);
            System.IO.File.WriteAllText(log, "ok");
            Shutdown(0);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(log, ex.ToString());
            Shutdown(1);
        }
    }

    /// <summary>"auto"를 계통(윈도우) 화면형식으로 풀어낸다.</summary>
    public static string Resolve(string theme) =>
        theme == "auto" ? (SystemTheme.AppsUseLight() ? "light" : "dark") : theme;

    /// <summary>밝은/어두운 화면형식 갈아입기 — Themes\Light.xaml / Dark.xaml을 통째로 바꿔 끼운다.</summary>
    public static void ApplyTheme(string theme)
    {
        ResolvedTheme = theme == "dark" ? "dark" : "light";
        var dict = new ResourceDictionary
        {
            Source = new Uri($"Themes/{(ResolvedTheme == "dark" ? "Dark" : "Light")}.xaml", UriKind.Relative),
        };
        var merged = Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(dict);
    }
}
