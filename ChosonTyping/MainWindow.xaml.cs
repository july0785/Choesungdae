using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ChosonTyping.Core;

namespace ChosonTyping;

public partial class MainWindow : Window
{
    Func<UserControl>? _factory;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) =>
            Shell.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        ApplyTaskbarIcon();
        ApplyChrome();
        Navigate(() => new Views.StartView(this));
        if (App.StartupStage is int stage) NavigateStage(stage);
    }

    /// <summary>개발용 지름길 — 설정된 배렬로 해당 련습화면을 바로 연다.</summary>
    void NavigateStage(int stage)
    {
        var layout = Core.KeyboardLayout.Load(
            System.IO.Path.Combine(AppConfig.LayoutsDir, AppConfig.Load().Layout + ".json"));
        Navigate(stage switch
        {
            1 => () => new Views.WordView(this, layout),
            2 => () => new Views.SentenceView(this, layout),
            3 => (Func<UserControl>)(() => new Views.TextListView(this, layout, isTest: false)),
            4 => () => new Views.TextListView(this, layout, isTest: true),
            5 => () => new Views.AcidRainView(this, layout),
            _ => () => new Views.KeyDrillView(this, layout),
        });
    }

    /// <summary>화면 전환의 틀(설계서 3.1). 화면형식을 바꿀 때 다시 만들수 있게 만드는 법을 받는다.</summary>
    public void Navigate(Func<UserControl> factory)
    {
        _factory = factory;
        Host.Content = factory();
    }

    /// <summary>화면을 다시 만들어 붙인다 — 언어를 바꾼 뒤 번역글을 새로 읽게.</summary>
    public void Refresh()
    {
        if (_factory is not null) Host.Content = _factory();
    }

    /// <summary>창머리 글자(판·툴팁)를 지금 언어로 맞춘다.</summary>
    public void ApplyChrome()
    {
        VersionText.Text = Loc.S("app.version");
        MinBtn.ToolTip = Loc.S("tip.min");
        MaxBtn.ToolTip = Loc.S("tip.max");
        CloseBtn.ToolTip = Loc.S("tip.close");
    }

    static string AssetsDir => Path.Combine(AppContext.BaseDirectory, "Assets");

    static string? FindAsset(string baseName)
    {
        foreach (var ext in new[] { ".ico", ".png" })
        {
            var p = Path.Combine(AssetsDir, baseName + ext);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    /// <summary>작업표시줄 아이콘 — 작업표시줄이 어두우면 흰 아이콘, 밝으면 검은 아이콘.</summary>
    void ApplyTaskbarIcon()
    {
        try
        {
            var path = FindAsset(SystemTheme.TaskbarUsesLight() ? "icon-square-black" : "icon-square-white");
            if (path is not null)
                Icon = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
        }
        catch (Exception)
        {
            // 아이콘이 깨져도 프로그람은 돈다.
        }
    }

    void AppBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void MaxBtn_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}
