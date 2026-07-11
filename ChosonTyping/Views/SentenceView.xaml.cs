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
/// 짧은글련습: 문장을 따라친다. 틀려도 막지 않고 색으로만 알린다(설계서 11.3).
/// 본보기줄 아래에 실제 친 글이 보이고, 지난 줄·다음 줄이 가사처럼 흐르며 바뀐다.
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
        PrevLine.Text = _index > 0 ? _sentences[_index - 1] : "";
        NextLine.Text = _index + 1 < _sentences.Count ? _sentences[_index + 1] : "";
        NextLine2.Text = _index + 2 < _sentences.Count ? _sentences[_index + 2] : "";
        ViewFx.SlideIn(LyricStack);
        Refresh();
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
        NextLine.Text = $"타속 {cpm:0} 타/분 · 정확도 {acc:0} %";
        NextLine2.Text = "다시 — 넣기(Enter) · 시작화면 — Esc";
        Kb.SetNext(null);
        ViewFx.SlideIn(LyricStack);
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
        RenderOverlay(_session, TargetLine, this);
        var next = _session.NextKey();
        Kb.SetNext(next?.Token);
        UpdateStats();
    }

    /// <summary>
    /// 겹쳐쓰기 한 줄: 흐린 회색 본보기 우에 친 글이 얹힌다 — 낱말·긴글도 같이 쓴다.
    /// 맞으면(조합 중인 옳은 진행 포함) 또렷한 색, 틀리면 빨강. 아직 안 친 자리는 흐린 회색.
    /// 틀림은 입력중이든 아니든 틀림으로 본다(StateAt이 옳은 진행만 봐줌).
    /// </summary>
    internal static void RenderOverlay(TypingSession session, TextBlock line, FrameworkElement res)
    {
        string target = session.Target;
        string typed = session.Typed;
        line.Inlines.Clear();
        int n = Math.Max(target.Length, typed.Length);
        for (int i = 0; i < n; i++)
        {
            if (i == typed.Length)
                line.Inlines.Add(new Run("▏") { Foreground = (Brush)res.FindResource("Accent") });
            if (i < typed.Length)
            {
                bool wrong = session.StateAt(i) == CharState.Wrong;
                line.Inlines.Add(new Run(typed[i].ToString())
                {
                    Foreground = (Brush)res.FindResource(wrong ? "Wrong" : "Ink"),
                });
            }
            else
            {
                line.Inlines.Add(new Run(target[i].ToString()) { Foreground = (Brush)res.FindResource("Faint") });
            }
        }
        if (typed.Length >= n)
            line.Inlines.Add(new Run("▏") { Foreground = (Brush)res.FindResource("Accent") });
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
