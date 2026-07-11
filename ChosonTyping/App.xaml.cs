using System.Windows;
using ChosonTyping.Core;

namespace ChosonTyping;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme(AppConfig.Load().Theme);
    }

    /// <summary>낮/밤 갈아입기 — Themes\Light.xaml / Dark.xaml을 통째로 바꿔 끼운다.</summary>
    public static void ApplyTheme(string theme)
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri($"Themes/{(theme == "dark" ? "Dark" : "Light")}.xaml", UriKind.Relative),
        };
        var merged = Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(dict);
    }
}
