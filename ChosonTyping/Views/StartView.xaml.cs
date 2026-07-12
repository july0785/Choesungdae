using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>시작화면(설계서 6항): 화면 언어 · 건반배렬 고르기 · 련습단계 고르기.</summary>
public partial class StartView : UserControl
{
    static readonly string[] LayoutOrder = { "kukgyu", "changdeok", "dubeol-std" };

    // 각 단계의 이름·설명 번역 열쇠. 여섯 단계 모두 켜짐.
    static readonly string[] StageKeys = { "drill", "word", "sentence", "long", "test", "rain" };

    readonly MainWindow _main;
    readonly List<KeyboardLayout> _layouts = new();
    readonly Dictionary<string, (Border Card, Ellipse Dot)> _cards = new();
    string _selectedId;

    public StartView(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        _selectedId = AppConfig.Load().Layout;

        foreach (var id in LayoutOrder)
        {
            var path = System.IO.Path.Combine(AppConfig.LayoutsDir, id + ".json");
            if (File.Exists(path)) _layouts.Add(KeyboardLayout.Load(path));
        }
        if (_layouts.Count == 0)
            throw new InvalidDataException("data\\layouts 에서 배렬 화일을 찾을수 없습니다.");
        if (_layouts.All(l => l.Id != _selectedId)) _selectedId = _layouts[0].Id;

        LangLabel.Text = Loc.S("settings.language");
        ThemeLabel.Text = Loc.S("settings.theme");
        LayoutsLabel.Text = Loc.S("start.layouts");
        StagesLabel.Text = Loc.S("start.stages");
        StartBtn.Content = Loc.S("start.begin");

        LoadBigLogo();
        BuildLangBar();
        BuildThemeBar();
        BuildCards();
        BuildStages();
    }

    /// <summary>큰 로고 — 밝은 화면형식엔 검은 로고, 어두운 화면형식엔 흰 로고. 없으면 글자로.</summary>
    void LoadBigLogo()
    {
        try
        {
            string name = App.ResolvedTheme == "dark" ? "logo-white" : "logo-black";
            foreach (var ext in new[] { ".png", ".ico" })
            {
                var p = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", name + ext);
                if (File.Exists(p))
                {
                    BigLogo.Source = BitmapFrame.Create(new Uri(p),
                        BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    BigLogo.Visibility = Visibility.Visible;
                    return;
                }
            }
        }
        catch { /* 로고가 깨지면 글자로 */ }
        BigWordmark.Text = "최승대";
        BigWordmark.Visibility = Visibility.Visible;
    }

    void BuildLangBar()
    {
        foreach (var (code, name) in Loc.Languages)
        {
            bool cur = code == Loc.Lang;
            var b = new Button
            {
                Content = name,
                Style = (Style)FindResource("QuietButton"),
                FontWeight = cur ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (Brush)FindResource(cur ? "Accent" : "Mid"),
                Margin = new Thickness(8, 0, 0, 0),
            };
            string c = code;
            b.Click += (_, _) => SwitchLang(c);
            LangBar.Children.Add(b);
        }
    }

    void SwitchLang(string code)
    {
        if (code == Loc.Lang) return;
        Loc.Lang = code;
        var config = AppConfig.Load();
        config.Lang = code;
        config.Save();
        _main.ApplyChrome();
        _main.Navigate(() => new StartView(_main));  // 새 언어로 다시 그림
    }

    void BuildThemeBar()
    {
        (string code, string key)[] opts = { ("light", "theme.light"), ("dark", "theme.dark") };
        foreach (var (code, key) in opts)
        {
            bool on = code == App.ResolvedTheme;
            var b = new Button
            {
                Content = Loc.S(key),
                Style = (Style)FindResource("QuietButton"),
                FontWeight = on ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (Brush)FindResource(on ? "Accent" : "Mid"),
                Margin = new Thickness(8, 0, 0, 0),
            };
            string c = code;
            b.Click += (_, _) => SetTheme(c);
            ThemeBar.Children.Add(b);
        }
    }

    void SetTheme(string code)
    {
        if (code == App.ResolvedTheme) return;
        var config = AppConfig.Load();
        config.Theme = code;
        config.Save();
        App.ApplyTheme(code);
        _main.Navigate(() => new StartView(_main));  // 새 화면형식으로 다시 그림
    }

    void BuildCards()
    {
        foreach (var layout in _layouts)
        {
            var name = new TextBlock
            {
                Text = Loc.S("layout." + layout.Id), FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Ink"),
            };
            var desc = new TextBlock
            {
                Text = Loc.S("layout." + layout.Id + ".desc"),
                FontSize = 12, Foreground = (Brush)FindResource("Mid"),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0),
            };
            var dot = new Ellipse
            {
                Width = 8, Height = 8, Fill = (Brush)FindResource("Sky"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
            };
            var grid = new Grid();
            grid.Children.Add(new StackPanel { Children = { name, desc } });
            grid.Children.Add(dot);

            var card = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("Hair"),
                Padding = new Thickness(18, 16, 18, 16),
                Margin = new Thickness(0, 0, 12, 0),
                Background = (Brush)FindResource("Paper"),
                Cursor = Cursors.Hand,
                Child = grid,
            };
            string id = layout.Id;
            card.MouseLeftButtonUp += (_, _) => Select(id);
            _cards[id] = (card, dot);
            LayoutCards.Children.Add(card);
        }
        Select(_selectedId, save: false);
    }

    void Select(string id, bool save = true)
    {
        _selectedId = id;
        foreach (var (cardId, (card, dot)) in _cards)
        {
            bool sel = cardId == id;
            card.BorderBrush = (Brush)FindResource(sel ? "Sky" : "Hair");
            dot.Visibility = sel ? Visibility.Visible : Visibility.Collapsed;
        }
        if (save)
        {
            var config = AppConfig.Load();
            config.Layout = id;
            config.Save();
        }
    }

    void BuildStages()
    {
        for (int i = 0; i < StageKeys.Length; i++)
        {
            var row = new DockPanel();
            row.Children.Add(new TextBlock
            {
                Text = (i + 1).ToString(), FontSize = 12, Width = 22,
                Foreground = (Brush)FindResource("Faint"), VerticalAlignment = VerticalAlignment.Center,
            });
            row.Children.Add(new TextBlock
            {
                Text = Loc.S("stage." + StageKeys[i]), FontSize = 15, FontWeight = FontWeights.Bold, Width = 148,
                Foreground = (Brush)FindResource("Ink"), VerticalAlignment = VerticalAlignment.Center,
            });
            var tail = new TextBlock
            {
                Text = "→", FontSize = 14,
                Foreground = (Brush)FindResource("Faint"),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(tail, Dock.Right);
            row.Children.Add(tail);
            row.Children.Add(new TextBlock
            {
                Text = Loc.S("stage." + StageKeys[i] + ".desc"), FontSize = 13,
                Foreground = (Brush)FindResource("Mid"),
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
            });

            var border = new Border
            {
                BorderBrush = (Brush)FindResource("Hair"),
                BorderThickness = new Thickness(0, i == 0 ? 1 : 0, 0, 1),
                Padding = new Thickness(4, 13, 4, 13),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = row,
            };
            int stageIndex = i;
            border.MouseEnter += (_, _) => border.Background = (Brush)FindResource("Soft");
            border.MouseLeave += (_, _) => border.Background = Brushes.Transparent;
            border.MouseLeftButtonUp += (_, _) => StartStage(stageIndex);
            StageList.Children.Add(border);
        }
    }

    void StartStage(int stage)
    {
        var layout = _layouts.First(l => l.Id == _selectedId);
        _main.Navigate(stage switch
        {
            1 => () => new WordView(_main, layout),
            2 => () => new SentenceView(_main, layout),
            3 => (Func<UserControl>)(() => new TextListView(_main, layout, isTest: false)),
            4 => () => new TextListView(_main, layout, isTest: true),
            5 => () => new AcidRainView(_main, layout),
            _ => () => new KeyDrillView(_main, layout),
        });
    }

    void StartBtn_Click(object sender, RoutedEventArgs e) => StartStage(0);
}
