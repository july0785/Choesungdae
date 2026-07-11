using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>
/// 화면 건반(설계서 6·7항): 다음에 칠 글쇠 하나만 빨강, 친 글쇠는 잠깐 회색.
/// 물리 줄 배치는 불변이므로 코드에 두고, 자모 라벨만 배렬 JSON에서 받는다.
/// </summary>
public partial class KeyboardControl : UserControl
{
    // 물리 줄 정의: (토큰, 너비단위). 라벨만 있는 특수글쇠는 토큰 앞에 '#'.
    static readonly (string tok, double w)[][] PhysicalRows =
    {
        new[]{ ("`",1.0),("1",1.0),("2",1.0),("3",1.0),("4",1.0),("5",1.0),("6",1.0),
               ("7",1.0),("8",1.0),("9",1.0),("0",1.0),("-",1.0),("=",1.0),("#지우기",1.8) },
        new[]{ ("#탭",1.8),("Q",1.0),("W",1.0),("E",1.0),("R",1.0),("T",1.0),("Y",1.0),
               ("U",1.0),("I",1.0),("O",1.0),("P",1.0),("[",1.0),("]",1.0),("\\",1.0) },
        new[]{ ("#글자판",2.1),("A",1.0),("S",1.0),("D",1.0),("F",1.0),("G",1.0),("H",1.0),
               ("J",1.0),("K",1.0),("L",1.0),(";",1.0),("'",1.0),("#넣기",1.9) },
        new[]{ ("#윗글쇠",2.6),("Z",1.0),("X",1.0),("C",1.0),("V",1.0),("B",1.0),("N",1.0),
               ("M",1.0),(",",1.0),(".",1.0),("/",1.0),("#윗글쇠2",2.6) },
        new[]{ ("# ",1.8),("#사이띄기",9.4),("# 2",1.8) },
    };

    readonly Dictionary<string, Border> _keys = new();
    Border? _next;

    public KeyboardControl()
    {
        InitializeComponent();
    }

    public void SetLayout(KeyboardLayout layout)
    {
        Rows.Children.Clear();
        _keys.Clear();
        _next = null;
        foreach (var row in PhysicalRows)
        {
            var p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            foreach (var (tok, w) in row) p.Children.Add(MakeKey(tok, w, layout));
            Rows.Children.Add(p);
        }
    }

    Border MakeKey(string tok, double w, KeyboardLayout layout)
    {
        bool special = tok.StartsWith('#');
        string label = special ? tok[1..].TrimEnd('2') : tok;
        string? jamo = special ? null : layout.JamoFor(tok, false);
        string? shiftJamo = special ? null : layout.JamoFor(tok, true);
        if (shiftJamo == jamo) shiftJamo = null;

        var grid = new Grid();
        grid.Children.Add(new TextBlock
        {
            Text = jamo ?? label,
            FontSize = special ? 11 : (jamo is null ? 12 : 18),
            FontWeight = jamo is null ? FontWeights.Normal : FontWeights.SemiBold,
            Foreground = (Brush)FindResource(jamo is null ? "Mid" : "Ink"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (!special && jamo is not null)
        {
            grid.Children.Add(new TextBlock
            {
                Text = tok,
                FontSize = 9,
                Foreground = (Brush)FindResource("Faint"),
                Margin = new Thickness(6, 4, 0, 0),
            });
        }
        if (shiftJamo is not null)
        {
            grid.Children.Add(new TextBlock
            {
                Text = shiftJamo,
                FontSize = 10,
                Foreground = (Brush)FindResource("Faint"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 6, 0),
            });
        }

        var bd = new Border
        {
            Width = 48 * w,
            Height = 48,
            CornerRadius = new CornerRadius(8),
            BorderBrush = (Brush)FindResource("Hair"),
            BorderThickness = new Thickness(1),
            Background = special || jamo is null
                ? (Brush)FindResource("Soft")
                : (Brush)FindResource("Paper"),
            Margin = new Thickness(0, 0, 6, 0),
            Child = grid,
        };
        if (!special && !_keys.ContainsKey(tok)) _keys[tok] = bd;
        return bd;
    }

    /// <summary>다음에 칠 글쇠 하나만 빨강으로. null이면 강조 해제.</summary>
    public void SetNext(string? tok)
    {
        if (_next is not null)
        {
            _next.Background = (Brush)FindResource("Paper");
            _next.BorderBrush = (Brush)FindResource("Hair");
            foreach (TextBlock t in ((Grid)_next.Child).Children)
                t.Foreground = (Brush)FindResource(t.FontSize >= 18 ? "Ink" : "Faint");
            _next = null;
        }
        if (tok is null || !_keys.TryGetValue(tok, out var bd)) return;
        bd.Background = (Brush)FindResource("Accent");
        bd.BorderBrush = (Brush)FindResource("Accent");
        foreach (TextBlock t in ((Grid)bd.Child).Children) t.Foreground = Brushes.White;
        _next = bd;
    }

    /// <summary>친 글쇠는 잠깐(150ms) 회색 — 은은한 동작(설계서 7항).</summary>
    public void Flash(string tok)
    {
        if (!_keys.TryGetValue(tok, out var bd) || bd == _next) return;
        var old = bd.Background;
        bd.Background = (Brush)FindResource("Hair");
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        t.Tick += (_, _) =>
        {
            bd.Background = old;
            t.Stop();
        };
        t.Start();
    }
}
