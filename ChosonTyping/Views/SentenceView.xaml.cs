using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>
/// 짧은글련습: 문장을 따라친다. 틀려도 막지 않고 빨갛게만 표시한다(설계서 11.3).
/// 지우면 본래색으로 돌아가고, 고쳐 치면 맞은 것으로 친다. 넣기(Enter)로 다음 문장.
/// </summary>
public partial class SentenceView : UserControl
{
    const int Round = 20;

    readonly MainWindow _main;
    readonly KeyboardLayout _layout;
    readonly List<string> _sentences;
    readonly Stopwatch _watch = new();

    TypingSession _session = null!;
    int _index;
    int _doneStrokes;
    int _doneCorrect;
    int _doneCompared;
    bool _finished;
    Window? _window;

    public SentenceView(MainWindow main, KeyboardLayout layout)
    {
        InitializeComponent();
        _main = main;
        _layout = layout;
        Kb.SetLayout(layout);

        var (modules, errors) = ContentModule.LoadDir(Path.Combine(AppConfig.DataDir, "sentences"));
        var pool = modules.Where(m => m.Items is not null).SelectMany(m => m.Items!).ToList();
        if (pool.Count == 0) pool.Add("오늘은 날씨가 맑습니다");
        var rng = new Random();
        _sentences = pool.OrderBy(_ => rng.Next()).Take(Round).ToList();

        StartSentence(0);

        Loaded += (_, _) =>
        {
            _window = Window.GetWindow(this);
            if (_window is not null) _window.PreviewKeyDown += OnKey;
            ErrorDialog.ShowErrors(_window, errors);
        };
        Unloaded += (_, _) =>
        {
            if (_window is not null) _window.PreviewKeyDown -= OnKey;
        };
    }

    void StartSentence(int index)
    {
        _index = index;
        if (_index >= _sentences.Count)
        {
            Finish();
            return;
        }
        _session = new TypingSession(_sentences[_index], _layout);
        TitleText.Text = $"짧은글련습 · {_index + 1}/{_sentences.Count}";
        HintText.Text = "틀려도 막지 않습니다 — 빨간 글자는 지우고 고칠수 있습니다 · 넣기(Enter) = 다음 문장";
        NextLine.Text = _index + 1 < _sentences.Count ? _sentences[_index + 1] : "";
        Refresh();
    }

    void Finish()
    {
        _finished = true;
        _watch.Stop();
        double acc = _doneCompared == 0 ? 100 : _doneCorrect * 100.0 / _doneCompared;
        double cpm = TypingStats.Cpm(_doneStrokes, _watch.Elapsed);
        TargetLine.Inlines.Clear();
        TargetLine.Inlines.Add(new Run("끝!") { Foreground = (Brush)FindResource("Ink"), FontWeight = FontWeights.ExtraBold });
        NextLine.Text = $"타속 {cpm:0} 타/분 · 정확도 {acc:0} %";
        HintText.Text = "다시 — 넣기(Enter) · 시작화면 — Esc";
        Kb.SetNext(null);
        Stats.Update(cpm, acc, 100);
    }

    void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _main.Navigate(() => new StartView(_main));
            return;
        }
        if (_finished)
        {
            if (e.Key is Key.Enter or Key.Return)
            {
                e.Handled = true;
                _main.Navigate(() => new SentenceView(_main, _layout));
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
            NextSentence();
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

    /// <summary>문장을 마감하고 다음으로 — 자리마다 본보기와 견줘 맞은 자리를 쌓는다(설계서 11.2).</summary>
    void NextSentence()
    {
        _session.Composer.Flush();
        string target = _session.Target;
        string typed = _session.Typed;
        int compared = Math.Min(target.Length, typed.Length);
        for (int i = 0; i < compared; i++)
            if (target[i] == typed[i]) _doneCorrect++;
        _doneCompared += target.Length;
        _doneStrokes += _session.Strokes;
        StartSentence(_index + 1);
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
        double prog = (_index * 100.0 + _session.Progress) / _sentences.Count;
        Stats.Update(cpm, acc, prog);
    }

    void Back_Click(object sender, RoutedEventArgs e) => _main.Navigate(() => new StartView(_main));
}
