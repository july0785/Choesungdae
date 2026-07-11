using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>시작화면(설계서 6항): 건반배렬 고르기 + 련습단계 고르기.</summary>
public partial class StartView : UserControl
{
    static readonly string[] LayoutOrder = { "kukgyu", "changdeok", "dubeol-std" };

    static readonly Dictionary<string, string> LayoutDesc = new()
    {
        ["kukgyu"] = "KPS 9256 · 왼손 자음, 오른손 모음",
        ["changdeok"] = "겹모음 아홉을 가장자리 글쇠 하나로",
        ["dubeol-std"] = "남측 표준 배렬",
    };

    static readonly (string Name, string Desc, bool Enabled)[] Stages =
    {
        ("자리련습", "글쇠자리를 눈에 익히고 손에 익힙니다", true),
        ("낱말련습", "화면에 나오는 낱말을 보고 정확히 칩니다", false),
        ("짧은글련습", "짧은 문장을 되풀이해 치며 속도를 올립니다", false),
        ("긴글련습", "가요의 가사를 처음부터 끝까지 따라 칩니다", false),
        ("타자검정", "타자 속도와 정확도를 재여 급수를 매깁니다", false),
        ("산성비", "떨어지는 낱말을 바닥에 닿기 전에 없앱니다", false),
    };

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

        BuildCards();
        BuildStages();
    }

    void BuildCards()
    {
        foreach (var layout in _layouts)
        {
            var name = new TextBlock
            {
                Text = layout.Name, FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Ink"),
            };
            var desc = new TextBlock
            {
                Text = LayoutDesc.GetValueOrDefault(layout.Id, ""),
                FontSize = 12, Foreground = (Brush)FindResource("Mid"),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0),
            };
            var dot = new Ellipse
            {
                Width = 8, Height = 8, Fill = (Brush)FindResource("Blue"),
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
            card.BorderBrush = (Brush)FindResource(sel ? "Blue" : "Hair");
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
        for (int i = 0; i < Stages.Length; i++)
        {
            var (name, desc, enabled) = Stages[i];
            var row = new DockPanel();
            row.Children.Add(new TextBlock
            {
                Text = (i + 1).ToString(), FontSize = 12, Width = 22,
                Foreground = (Brush)FindResource("Faint"), VerticalAlignment = VerticalAlignment.Center,
            });
            row.Children.Add(new TextBlock
            {
                Text = name, FontSize = 15, FontWeight = FontWeights.Bold, Width = 120,
                Foreground = (Brush)FindResource(enabled ? "Ink" : "Faint"),
                VerticalAlignment = VerticalAlignment.Center,
            });
            var tail = new TextBlock
            {
                Text = enabled ? "→" : "준비중",
                FontSize = enabled ? 14 : 11,
                Foreground = (Brush)FindResource("Faint"),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(tail, Dock.Right);
            row.Children.Add(tail);
            row.Children.Add(new TextBlock
            {
                Text = desc, FontSize = 13,
                Foreground = (Brush)FindResource(enabled ? "Mid" : "Faint"),
                VerticalAlignment = VerticalAlignment.Center,
            });

            var border = new Border
            {
                BorderBrush = (Brush)FindResource("Hair"),
                BorderThickness = new Thickness(0, i == 0 ? 1 : 0, 0, 1),
                Padding = new Thickness(4, 13, 4, 13),
                Background = Brushes.Transparent,
                Child = row,
            };
            if (enabled)
            {
                border.Cursor = Cursors.Hand;
                border.MouseEnter += (_, _) => border.Background = (Brush)FindResource("Soft");
                border.MouseLeave += (_, _) => border.Background = Brushes.Transparent;
                border.MouseLeftButtonUp += (_, _) => StartDrill();
            }
            StageList.Children.Add(border);
        }
    }

    void StartDrill()
    {
        var layout = _layouts.First(l => l.Id == _selectedId);
        _main.Navigate(() => new KeyDrillView(_main, layout));
    }

    void StartBtn_Click(object sender, RoutedEventArgs e) => StartDrill();
}
