using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>
/// 긴글련습·타자검정: 글을 줄 단위로 따라 친다. 막지 않고 있는 그대로 잰다(설계서 11.3).
/// 검정이면 끝에 급수를 매긴다.
/// </summary>
public partial class LongTextView : UserControl
{
    readonly MainWindow _main;
    readonly KeyboardLayout _layout;
    readonly ContentModule _module;
    readonly bool _isTest;
    readonly List<string> _lines;
    readonly int _totalChars;
    readonly Stopwatch _watch = new();

    TypingSession _session = null!;
    int _index;
    int _doneStrokes;
    int _doneCorrect;
    int _doneCompared;
    int _doneChars;
    bool _finished;
    Window? _window;

    public LongTextView(MainWindow main, KeyboardLayout layout, ContentModule module, bool isTest)
    {
        InitializeComponent();
        _main = main;
        _layout = layout;
        _module = module;
        _isTest = isTest;
        Kb.SetLayout(layout);

        _lines = (module.Body ?? "").Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        if (_lines.Count == 0) _lines.Add("(빈 글)");
        _totalChars = _lines.Sum(l => l.Length);

        StartLine(0);

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

    void StartLine(int index)
    {
        _index = index;
        if (_index >= _lines.Count)
        {
            Finish();
            return;
        }
        _session = new TypingSession(_lines[_index], _layout);
        TitleText.Text = $"{(_isTest ? "타자검정" : "긴글련습")} · {_module.Title} · {_index + 1}/{_lines.Count}줄";
        PrevLine.Text = _index > 0 ? _lines[_index - 1] : "";
        NextLine.Text = _index + 1 < _lines.Count ? _lines[_index + 1] : "";
        HintText.Text = "넣기(Enter) = 다음 줄 · 틀려도 막지 않습니다";
        Refresh();
    }

    /// <summary>급수표 — 타속과 정확도를 함께 본다.</summary>
    static string Grade(double cpm, double acc)
    {
        if (cpm >= 500 && acc >= 98) return "특급";
        if (cpm >= 400 && acc >= 96) return "1급";
        if (cpm >= 300 && acc >= 94) return "2급";
        if (cpm >= 200 && acc >= 92) return "3급";
        if (cpm >= 100 && acc >= 90) return "4급";
        return "급외";
    }

    void Finish()
    {
        _finished = true;
        _watch.Stop();
        double acc = _doneCompared == 0 ? 100 : _doneCorrect * 100.0 / _doneCompared;
        double cpm = TypingStats.Cpm(_doneStrokes, _watch.Elapsed);

        PrevLine.Text = "";
        TargetLine.Inlines.Clear();
        TargetLine.Inlines.Add(new Run("끝!") { Foreground = (Brush)FindResource("Ink"), FontWeight = FontWeights.ExtraBold });
        NextLine.Text = _isTest
            ? $"타속 {cpm:0} 타/분 · 정확도 {acc:0} % — 판정: {Grade(cpm, acc)}"
            : $"타속 {cpm:0} 타/분 · 정확도 {acc:0} %";
        HintText.Text = "다시 — 넣기(Enter) · 글 고르기 — Esc";
        Kb.SetNext(null);
        Stats.Update(cpm, acc, 100);
    }

    void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            GoBack();
            return;
        }
        if (_finished)
        {
            if (e.Key is Key.Enter or Key.Return)
            {
                e.Handled = true;
                _main.Navigate(() => new LongTextView(_main, _layout, _module, _isTest));
            }
            return;
        }
        if (e.Key == Key.Back)
        {
            e.Handled = true;
            _session.Backspace();
            Refresh();
            return;
        }
        if (e.Key is Key.Enter or Key.Return)
        {
            e.Handled = true;
            NextLineGo();
            return;
        }

        string? tok = KeyMapper.ToToken(e.Key);
        if (tok is null) return;
        e.Handled = true;
        if (!_watch.IsRunning) _watch.Start();
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        if (_session.Feed(tok, shift)) Kb.Flash(tok);
        Refresh();
    }

    void NextLineGo()
    {
        _session.Composer.Flush();
        string target = _session.Target;
        string typed = _session.Typed;
        int compared = Math.Min(target.Length, typed.Length);
        for (int i = 0; i < compared; i++)
            if (target[i] == typed[i]) _doneCorrect++;
        _doneCompared += target.Length;
        _doneStrokes += _session.Strokes;
        _doneChars += target.Length;
        StartLine(_index + 1);
    }

    void Refresh()
    {
        string target = _session.Target;
        string typed = _session.Typed;

        TargetLine.Inlines.Clear();
        for (int i = 0; i < target.Length; i++)
        {
            var run = new Run(target[i].ToString());
            if (i < typed.Length)
                run.Foreground = (Brush)FindResource(typed[i] == target[i] ? "Ink" : "Wrong");
            else
                run.Foreground = (Brush)FindResource("Faint");
            TargetLine.Inlines.Add(run);
        }

        var next = _session.NextKey();
        Kb.SetNext(next?.Token);
        UpdateStats();
    }

    void UpdateStats()
    {
        string target = _session.Target;
        string typed = _session.Typed;
        int compared = Math.Min(target.Length, typed.Length);
        int correct = 0;
        for (int i = 0; i < compared; i++)
            if (target[i] == typed[i]) correct++;

        int totalCompared = _doneCompared + compared;
        double acc = totalCompared == 0 ? 100 : (_doneCorrect + correct) * 100.0 / totalCompared;
        int strokes = _doneStrokes + _session.Strokes;
        double cpm = TypingStats.Cpm(strokes, _watch.Elapsed);
        double prog = _totalChars == 0 ? 100 : (_doneChars + compared) * 100.0 / _totalChars;
        Stats.Update(cpm, acc, prog);
    }

    void GoBack() => _main.Navigate(() => new TextListView(_main, _layout, _isTest));

    void Back_Click(object sender, RoutedEventArgs e) => GoBack();
}
