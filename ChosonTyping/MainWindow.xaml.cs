using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChosonTyping.Core;

namespace ChosonTyping;

public partial class MainWindow : Window
{
    Func<UserControl>? _factory;
    DateTime _popupClosedAt;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) =>
            Shell.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        SettingsPopup.Closed += (_, _) => _popupClosedAt = DateTime.Now;
        SettingsPopup.Placement = PlacementMode.Custom;
        SettingsPopup.CustomPopupPlacementCallback = (size, target, _) => new[]
        {
            new CustomPopupPlacement(new Point(target.Width - size.Width + 14, target.Height - 6),
                                     PopupPrimaryAxis.Horizontal),
        };
        ApplyTaskbarIcon();
        UpdateLogo();
        ApplyChrome();
        BuildSettingsPopup();
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

    /// <summary>
    /// 화면 전환의 틀(설계서 3.1). 새 화면은 부드럽게 나타나고,
    /// 시작화면의 큰 로고는 련습으로 들어갈 때 창머리 왼쪽 우로 날아가 앉는다(돌아올 땐 반대로).
    /// </summary>
    public void Navigate(Func<UserControl> factory)
    {
        var oldView = Host.Content as UserControl;
        _factory = factory;
        var newView = factory();
        bool newIsStart = newView is Views.StartView;

        // 로고 비행 준비 — 떠나는 자리를 화면이 바뀌기 전에 재둔다.
        Rect? heroFrom = null;
        ImageSource? heroSrc = null;
        bool reverse = false;
        if (IsLoaded && Fx.ActualWidth > 0)
        {
            if (oldView is Views.StartView os && !newIsStart
                && os.HeroSource is { } s && os.HeroRect(Fx) is { } r)
            {
                heroSrc = s;
                heroFrom = r;
            }
            else if (oldView is not null and not Views.StartView && newIsStart
                     && LogoImage.Source is { } cs && LogoImage.ActualWidth > 0)
            {
                reverse = true;
                heroSrc = cs;
                heroFrom = RectOf(LogoImage);
            }
        }

        Host.Content = newView;
        ShowCornerIdentity(!newIsStart);
        FadeHost();

        if (heroSrc is null || heroFrom is null) return;

        if (!reverse)
        {
            LogoImage.Visibility = Visibility.Hidden;   // 다 날아온 뒤에 보여준다
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
                RunHero(heroSrc, heroFrom.Value, RectOf(LogoImage), () =>
                {
                    if (Host.Content == newView) ShowCornerIdentity(true);
                }));
        }
        else if (newView is Views.StartView ns)
        {
            ns.HideHeroLogo();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (ns.HeroRect(Fx) is { } to) RunHero(heroSrc, heroFrom.Value, to, ns.ShowHeroLogo);
                else ns.ShowHeroLogo();
            });
        }
    }

    /// <summary>화면을 다시 만들어 붙인다 — 언어·화면형식을 바꾼 뒤 새로 읽게.</summary>
    public void Refresh()
    {
        if (_factory is null) return;
        var view = _factory();
        Host.Content = view;
        ShowCornerIdentity(view is not Views.StartView);
        FadeHost();
    }

    /// <summary>창머리 글자(판·툴팁)를 지금 언어로 맞춘다.</summary>
    public void ApplyChrome()
    {
        VersionText.Text = Loc.S("app.version");
        GearBtn.ToolTip = Loc.S("tip.settings");
        MinBtn.ToolTip = Loc.S("tip.min");
        MaxBtn.ToolTip = Loc.S("tip.max");
        CloseBtn.ToolTip = Loc.S("tip.close");
    }

    // ── 설정 팝업(언어·화면형식) ──────────────────────

    void BuildSettingsPopup()
    {
        PopupLangLabel.Text = Loc.S("settings.language");
        PopupThemeLabel.Text = Loc.S("settings.theme");
        PopupLangBar.Children.Clear();
        foreach (var (code, name) in Loc.Languages)
            PopupLangBar.Children.Add(Choice(name, code == Loc.Lang, () => SwitchLang(code)));

        // 화면형식은 두쪽짜리 토글 — 켜진 쪽이 먹색으로 채워진다.
        PopupThemeBar.Children.Clear();
        var seg = new StackPanel { Orientation = Orientation.Horizontal };
        seg.Children.Add(SegChoice(Loc.S("theme.light"), App.ResolvedTheme == "light", () => SetTheme("light")));
        seg.Children.Add(SegChoice(Loc.S("theme.dark"), App.ResolvedTheme == "dark", () => SetTheme("dark")));
        PopupThemeBar.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(9),
            BorderBrush = (Brush)FindResource("Hair"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            Margin = new Thickness(8, 0, 0, 0),
            Child = seg,
        });
    }

    Button SegChoice(string text, bool on, Action act)
    {
        var b = new Button
        {
            Content = text,
            Style = (Style)FindResource("SegButton"),
            Background = on ? (Brush)FindResource("Ink") : Brushes.Transparent,
            Foreground = (Brush)FindResource(on ? "Paper" : "Mid"),
            FontWeight = on ? FontWeights.Bold : FontWeights.Normal,
        };
        b.Click += (_, _) => act();
        return b;
    }

    Button Choice(string text, bool on, Action act)
    {
        var b = new Button
        {
            Content = text,
            Style = (Style)FindResource("QuietButton"),
            FontWeight = on ? FontWeights.Bold : FontWeights.Normal,
            Foreground = (Brush)FindResource(on ? "Accent" : "Mid"),
            Margin = new Thickness(8, 0, 0, 0),
        };
        b.Click += (_, _) => act();
        return b;
    }

    void SwitchLang(string code)
    {
        if (code == Loc.Lang) return;
        Loc.Lang = code;
        var config = AppConfig.Load();
        config.Lang = code;
        config.Save();
        ApplyChrome();
        BuildSettingsPopup();
        Refresh();
    }

    void SetTheme(string code)
    {
        if (code == App.ResolvedTheme) return;
        var config = AppConfig.Load();
        config.Theme = code;
        config.Save();
        App.ApplyTheme(code);
        UpdateLogo();
        BuildSettingsPopup();
        Refresh();
    }

    void GearBtn_Click(object sender, RoutedEventArgs e)
    {
        // 팝업이 열린 채 톱니바퀴를 다시 누르면 닫힘(밖 클릭)이 먼저 오므로 곧장 되열지 않는다.
        if ((DateTime.Now - _popupClosedAt).TotalMilliseconds < 250) return;
        SettingsPopup.IsOpen = true;
    }

    // ── 로고·아이콘 ──────────────────────────────────

    static string AssetsDir => Path.Combine(AppContext.BaseDirectory, "Assets");

    static string? FindAsset(string baseName)
    {
        foreach (var ext in new[] { ".png", ".ico" })
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

    /// <summary>창머리 작은 로고 — 밝은 화면형식엔 검은 로고, 어두운 화면형식엔 흰 로고.</summary>
    void UpdateLogo()
    {
        try
        {
            var path = FindAsset(App.ResolvedTheme == "dark" ? "logo-white" : "logo-black");
            LogoImage.Source = path is null ? null : BitmapFrame.Create(
                new Uri(path), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
        }
        catch (Exception)
        {
            LogoImage.Source = null;   // 로고가 깨지면 글자 워드마크로
        }
    }

    /// <summary>창머리 왼쪽 로고는 련습화면에서만 보인다(시작화면에선 큰 로고가 있으니 숨김).</summary>
    void ShowCornerIdentity(bool show)
    {
        bool hasLogo = LogoImage.Source is not null;
        // Hidden(자리는 지킴)으로 숨겨야 로고 비행의 도착점을 잴수 있다.
        LogoImage.Visibility = show && hasLogo ? Visibility.Visible : Visibility.Hidden;
        WordmarkText.Visibility = show && !hasLogo ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── 화면 전환 연출 ────────────────────────────────

    Rect RectOf(FrameworkElement el) =>
        new(el.TranslatePoint(new Point(), Fx), new Size(el.ActualWidth, el.ActualHeight));

    void FadeHost()
    {
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Host.BeginAnimation(OpacityProperty, fade);
    }

    /// <summary>로고 비행: 덮개층의 복사본이 from에서 to로 미끄러져 간 뒤 실물과 바뀐다.</summary>
    void RunHero(ImageSource src, Rect from, Rect to, Action done)
    {
        if (from.Width < 1 || to.Width < 1)
        {
            done();
            return;
        }
        var img = new Image { Source = src, Stretch = Stretch.Uniform, Width = from.Width, Height = from.Height };
        Canvas.SetLeft(img, from.X);
        Canvas.SetTop(img, from.Y);
        Fx.Children.Add(img);

        var dur = TimeSpan.FromMilliseconds(430);
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        DoubleAnimation A(double f, double t) => new(f, t, dur) { EasingFunction = ease };

        var last = A(from.Height, to.Height);
        last.Completed += (_, _) =>
        {
            Fx.Children.Remove(img);
            done();
        };
        img.BeginAnimation(Canvas.LeftProperty, A(from.X, to.X));
        img.BeginAnimation(Canvas.TopProperty, A(from.Y, to.Y));
        img.BeginAnimation(WidthProperty, A(from.Width, to.Width));
        img.BeginAnimation(HeightProperty, last);
    }

    // ── 창 조작 ──────────────────────────────────────

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
