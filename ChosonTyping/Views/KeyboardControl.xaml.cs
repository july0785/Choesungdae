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
               ("7",1.0),("8",1.0),("9",1.0),("0",1.0),("-",1.0),("=",1.0),("#후진건",1.8) },
        new[]{ ("#Tab건",1.8),("Q",1.0),("W",1.0),("E",1.0),("R",1.0),("T",1.0),("Y",1.0),
               ("U",1.0),("I",1.0),("O",1.0),("P",1.0),("[",1.0),("]",1.0),("\\",1.0) },
        new[]{ ("#CapsLock건",2.1),("A",1.0),("S",1.0),("D",1.0),("F",1.0),("G",1.0),("H",1.0),
               ("J",1.0),("K",1.0),("L",1.0),(";",1.0),("'",1.0),("#Enter건",1.9) },
        new[]{ ("#Shift건",2.6),("Z",1.0),("X",1.0),("C",1.0),("V",1.0),("B",1.0),("N",1.0),
               ("M",1.0),(",",1.0),(".",1.0),("/",1.0),("#Shift건R",2.6) },
        new[]{ ("# ",1.8),(" ",9.4),("#  ",1.8) },
    };

    readonly Dictionary<string, Border> _keys = new();
    readonly List<Border> _shiftKeys = new();
    readonly Dictionary<Border, DispatcherTimer> _flashTimers = new();
    Border? _next;
    bool _shiftLit;

    public KeyboardControl()
    {
        InitializeComponent();
    }

    public void SetLayout(KeyboardLayout layout)
    {
        Rows.Children.Clear();
        _keys.Clear();
        _shiftKeys.Clear();
        foreach (var t in _flashTimers.Values) t.Stop();
        _flashTimers.Clear();
        _next = null;
        _shiftLit = false;
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
        bool isSpace = tok == " ";
        string label = isSpace ? "공백건" : special ? tok[1..].TrimEnd('R') : tok;
        string? jamo = (special || isSpace) ? null : layout.JamoFor(tok, false);
        string? shiftJamo = (special || isSpace) ? null : layout.JamoFor(tok, true);
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

        bool soft = special || jamo is null;
        var bd = new Border
        {
            Width = 48 * w,
            Height = 48,
            CornerRadius = new CornerRadius(8),
            BorderBrush = (Brush)FindResource("Hair"),
            BorderThickness = new Thickness(1),
            Background = (Brush)FindResource(soft ? "Soft" : "Paper"),
            Margin = new Thickness(0, 0, 6, 0),
            Tag = soft,                       // 쉬는 바탕이 Soft인지 Paper인지 기억
            Child = grid,
        };
        if (!special && !_keys.ContainsKey(tok)) _keys[tok] = bd;
        if (tok.StartsWith("#Shift건")) _shiftKeys.Add(bd);
        return bd;
    }

    /// <summary>다음에 칠 글쇠를 강조한다. 윗글쇠가 필요하면 쉬프트건도 함께 강조.</summary>
    public void SetNext(string? tok, bool shift = false)
    {
        if (_next is not null) { ResetKey(_next); _next = null; }
        if (_shiftLit) { foreach (var s in _shiftKeys) ResetKey(s); _shiftLit = false; }

        if (tok is not null && _keys.TryGetValue(tok, out var bd))
        {
            LightKey(bd);
            _next = bd;
        }
        if (shift && _shiftKeys.Count > 0)
        {
            foreach (var s in _shiftKeys) LightKey(s);
            _shiftLit = true;
        }
    }

    void LightKey(Border bd)
    {
        bd.Background = (Brush)FindResource("Accent");
        bd.BorderBrush = (Brush)FindResource("Accent");
        foreach (TextBlock t in ((Grid)bd.Child).Children) t.Foreground = Brushes.White;
    }

    void ResetKey(Border bd)
    {
        bool soft = bd.Tag is true;
        bd.Background = (Brush)FindResource(soft ? "Soft" : "Paper");
        bd.BorderBrush = (Brush)FindResource("Hair");
        foreach (TextBlock t in ((Grid)bd.Child).Children)
            t.Foreground = (Brush)FindResource(soft ? "Mid" : (t.FontSize >= 18 ? "Ink" : "Faint"));
    }

    /// <summary>
    /// 친 글쇠는 잠깐(150ms) 회색 — 은은한 동작(설계서 7항).
    /// 같은 글쇠를 연타하면 도는 타이머의 시간만 늘인다 — 누른 색을 원래 색으로
    /// 잘못 기억해 자국이 남는 일(산성비 빠른 타자)이 없게.
    /// </summary>
    public void Flash(string tok)
    {
        if (!_keys.TryGetValue(tok, out var bd) || bd == _next) return;
        if (_flashTimers.TryGetValue(bd, out var running))
        {
            running.Stop();
            running.Start();
            return;
        }
        bd.Background = (Brush)FindResource("Hair");
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        t.Tick += (_, _) =>
        {
            t.Stop();
            _flashTimers.Remove(bd);
            if (bd != _next) ResetKey(bd);   // 그새 다음 글쇠로 강조됐으면 그대로 둔다
        };
        t.Start();
        _flashTimers[bd] = t;
    }
}
