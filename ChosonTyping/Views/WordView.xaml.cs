using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>낱말련습: 화면의 낱말을 보고 정확히 친다. 다 맞게 치면 다음 낱말로.</summary>
public partial class WordView : UserControl
{
    const int Round = 30;

    readonly MainWindow _main;
    readonly KeyboardLayout _layout;
    readonly List<string> _words;
    readonly Stopwatch _watch = new();

    TypingSession _session = null!;
    int _index;
    int _doneStrokes;
    bool _finished;
    Window? _window;

    public WordView(MainWindow main, KeyboardLayout layout)
    {
        InitializeComponent();
        _main = main;
        _layout = layout;
        Kb.SetLayout(layout);

        var (modules, errors) = ContentModule.LoadDir(Path.Combine(AppConfig.DataDir, "words"));
        var pool = modules.Where(m => m.Items is not null).SelectMany(m => m.Items!).ToList();
        var rng = new Random();
        _words = pool.OrderBy(_ => rng.Next()).Take(Round).ToList();

        if (_words.Count == 0) ShowEmpty();
        else StartWord(0);

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

    /// <summary>낱말 모듈이 비었을 때(례: 삼흥사전 추출 대기) 빈 화면 안내.</summary>
    void ShowEmpty()
    {
        _finished = true;
        TitleText.Text = "낱말련습";
        TargetText.Inlines.Clear();
        TargetText.Inlines.Add(new Run("낱말이 아직 없습니다") { Foreground = (Brush)FindResource("Faint") });
        HintText.Text = "시작화면 — Esc";
        Kb.SetNext(null);
    }

    void StartWord(int index)
    {
        _index = index;
        if (_index >= _words.Count)
        {
            Finish();
            return;
        }
        _session = new TypingSession(_words[_index], _layout);
        TitleText.Text = $"낱말련습 · {_index + 1}/{_words.Count}";
        HintText.Text = "";
        ViewFx.SlideIn(WordStack);
        Refresh();
    }

    void Finish()
    {
        _finished = true;
        _watch.Stop();
        TargetText.Inlines.Clear();
        TargetText.Inlines.Add(new Run("끝!") { Foreground = (Brush)FindResource("Ink") });
        HintText.Text = "다시 — 넣기(Enter) · 시작화면 — Esc";
        Kb.SetNext(null);
        ViewFx.SlideIn(WordStack);
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
                _main.Navigate(() => new WordView(_main, _layout));
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
            if (_session.Done) NextWord();
            return;
        }

        string? tok = KeyMapper.ToToken(e.Key);
        if (tok is null) return;
        e.Handled = true;
        if (tok == " " && _session.Typed.Length >= _session.Target.Length)
        {
            if (_session.Done) NextWord();
            return;
        }

        if (!_watch.IsRunning) _watch.Start();
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        _session.Feed(tok, shift);
        if (_session.Done)
        {
            Kb.Flash(tok);
            NextWord();
        }
        else
        {
            Refresh();
        }
    }

    void NextWord()
    {
        _doneStrokes += _session.Strokes;
        StartWord(_index + 1);
    }

    void Refresh()
    {
        SentenceView.RenderOverlay(_session, TargetText, this);
        var next = _session.NextKey();
        Kb.SetNext(next?.Token);
        UpdateStats();
    }

    void UpdateStats()
    {
        int strokes = _doneStrokes + _session.Strokes;
        double cpm = TypingStats.Cpm(strokes, _watch.Elapsed);
        Stats.Update(cpm, _session.PositionalAccuracy, _index * 100.0 / _words.Count);
    }

    void Back_Click(object sender, RoutedEventArgs e) => _main.Navigate(() => new StartView(_main));
}
