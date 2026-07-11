using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>
/// 자리련습(설계서 6항): 눌러야 할 글쇠만 빨강으로 빛나고, 손가락 안내가 따라간다.
/// 기본자리(가운데줄)부터 우·아래줄로 넓혀간다. 초보 단계라 맞아야 다음으로 넘어간다.
/// </summary>
public partial class KeyDrillView : UserControl
{
    readonly MainWindow _main;
    readonly KeyboardLayout _layout;
    readonly Stopwatch _watch = new();
    readonly Dictionary<string, List<Rectangle>> _fingerBars = new();

    List<string> _seq = new();
    int _stage;
    int _idx;
    int _hits;
    int _strokes;
    bool _done;
    Window? _window;

    public KeyDrillView(MainWindow main, KeyboardLayout layout, int stage = 0)
    {
        InitializeComponent();
        _main = main;
        _layout = layout;
        Kb.SetLayout(layout);
        BuildHands();
        StartStage(stage);

        Loaded += (_, _) =>
        {
            _window = Window.GetWindow(this);
            if (_window is not null) _window.PreviewKeyDown += OnKey;
        };
        Unloaded += (_, _) =>
        {
            if (_window is not null) _window.PreviewKeyDown -= OnKey;
        };
    }

    void StartStage(int stage)
    {
        _stage = ((stage % DrillLesson.Stages.Length) + DrillLesson.Stages.Length) % DrillLesson.Stages.Length;
        _seq = DrillLesson.Sequence(DrillLesson.Stages[_stage], _layout);
        _idx = 0;
        _hits = 0;
        _strokes = 0;
        _done = false;
        _watch.Reset();
        TitleText.Text = $"자리련습 · {DrillLesson.Stages[_stage].Name} {_stage + 1}/{DrillLesson.Stages.Length}";
        UpdatePrompt();
        Stats.Update(0, 100, 0);
    }

    void UpdatePrompt()
    {
        if (_idx >= _seq.Count)
        {
            _done = true;
            BigJamo.Text = "끝!";
            BigJamo.Foreground = (Brush)FindResource("Ink");
            HintText.Text = "다음 단계 — 넣기(Enter) · 시작화면 — Esc";
            SeqText.Inlines.Clear();
            Kb.SetNext(null);
            SetActiveFinger("");
            return;
        }

        string tok = _seq[_idx];
        BigJamo.Text = _layout.JamoFor(tok, false);
        BigJamo.Foreground = (Brush)FindResource("Ink");

        string finger = FingerMap.For(tok);
        HintText.Inlines.Clear();
        HintText.Inlines.Add(new Run(finger) { Foreground = (Brush)FindResource("Red"), FontWeight = FontWeights.Bold });
        HintText.Inlines.Add(new Run($"손가락 — {tok} 자리") { Foreground = (Brush)FindResource("Mid") });

        Kb.SetNext(tok);
        SetActiveFinger(finger);
        UpdateSeqStrip();
    }

    /// <summary>차례띠: 친 것은 먹색, 지금 것은 빨강 밑줄, 남은 것은 연회색(11자 창).</summary>
    void UpdateSeqStrip()
    {
        SeqText.Inlines.Clear();
        int from = Math.Max(0, _idx - 4);
        int to = Math.Min(_seq.Count, _idx + 7);

        string JamoOf(int i) => _layout.JamoFor(_seq[i], false) ?? "";

        if (from > 0)
            SeqText.Inlines.Add(new Run("… ") { Foreground = (Brush)FindResource("Faint") });
        for (int i = from; i < to; i++)
        {
            var run = new Run(JamoOf(i) + (i < to - 1 ? " " : ""));
            if (i < _idx)
            {
                run.Foreground = (Brush)FindResource("Ink");
            }
            else if (i == _idx)
            {
                run.Foreground = (Brush)FindResource("Ink");
                run.FontWeight = FontWeights.ExtraBold;
                run.TextDecorations = new TextDecorationCollection
                {
                    new TextDecoration
                    {
                        Location = TextDecorationLocation.Underline,
                        Pen = new Pen((Brush)FindResource("Red"), 2),
                        PenOffset = 2,
                    },
                };
            }
            else
            {
                run.Foreground = (Brush)FindResource("Faint");
            }
            SeqText.Inlines.Add(run);
        }
        if (to < _seq.Count)
            SeqText.Inlines.Add(new Run(" …") { Foreground = (Brush)FindResource("Faint") });
    }

    void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            GoBack();
            return;
        }
        if (_done)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                StartStage(_stage + 1);
            }
            return;
        }

        string? tok = KeyMapper.ToToken(e.Key);
        if (tok is null) return;
        e.Handled = true;

        if (!_watch.IsRunning) _watch.Start();
        _strokes++;

        if (tok == _seq[_idx])
        {
            _hits++;
            Kb.Flash(tok);
            _idx++;
            UpdatePrompt();
        }
        else
        {
            FlashWrong();
        }
        UpdateStats();
    }

    /// <summary>틀리면 큰 자모가 잠깐 빨강 — 틀림은 빨강(설계서 11.3).</summary>
    void FlashWrong()
    {
        BigJamo.Foreground = (Brush)FindResource("Red");
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        t.Tick += (_, _) =>
        {
            BigJamo.Foreground = (Brush)FindResource("Ink");
            t.Stop();
        };
        t.Start();
    }

    void UpdateStats()
    {
        double acc = _strokes == 0 ? 100 : _hits * 100.0 / _strokes;
        double cpm = TypingStats.Cpm(_strokes, _watch.Elapsed);
        double prog = _seq.Count == 0 ? 0 : _idx * 100.0 / _seq.Count;
        Stats.Update(cpm, acc, prog);
    }

    // ── 손 그림: 회색조 손가락 막대, 지금 쓸 손가락만 빨강(확정 시안) ──

    static readonly (string Name, double H)[] LeftFingers =
    {
        ("왼손 새끼", 34), ("왼손 약", 48), ("왼손 가운데", 56), ("왼손 집게", 50), ("엄지", 26),
    };

    static readonly (string Name, double H)[] RightFingers =
    {
        ("엄지", 26), ("오른손 집게", 50), ("오른손 가운데", 56), ("오른손 약", 48), ("오른손 새끼", 34),
    };

    void BuildHands()
    {
        void Build(StackPanel panel, (string Name, double H)[] fingers)
        {
            foreach (var (name, h) in fingers)
            {
                var bar = new Rectangle
                {
                    Width = 16,
                    Height = h,
                    RadiusX = 8,
                    RadiusY = 8,
                    Fill = (Brush)FindResource("Hair"),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(3, 0, 3, 0),
                };
                if (!_fingerBars.TryGetValue(name, out var list))
                    _fingerBars[name] = list = new List<Rectangle>();
                list.Add(bar);
                panel.Children.Add(bar);
            }
        }
        Build(LeftHand, LeftFingers);
        Build(RightHand, RightFingers);
    }

    void SetActiveFinger(string finger)
    {
        foreach (var list in _fingerBars.Values)
            foreach (var bar in list)
                bar.Fill = (Brush)FindResource("Hair");
        if (finger.Length > 0 && _fingerBars.TryGetValue(finger, out var active))
            foreach (var bar in active)
                bar.Fill = (Brush)FindResource("Red");
        HandCap.Text = finger.Length > 0 ? finger : "";
    }

    void GoBack() => _main.Navigate(new StartView(_main));

    void BackBtn_Click(object sender, RoutedEventArgs e) => GoBack();
}
