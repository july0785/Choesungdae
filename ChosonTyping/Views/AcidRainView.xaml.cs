using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>
/// 산성비: 낱말이 우에서 아래로 떨어진다. 바닥에 닿기 전에 쳐서 없앤다.
/// 단계가 오를수록 빨라진다. 점수·최고기록 표시(설계서 6항).
/// </summary>
public partial class AcidRainView : UserControl
{
    const int StartLives = 5;
    const int KillsPerLevel = 6;   // 단계당 없앨 낱말수 — 자주 올라 체감되게

    readonly MainWindow _main;
    readonly KeyboardLayout _layout;
    readonly List<string> _pool;
    readonly Random _rng = new();
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    readonly Stopwatch _watch = new();
    readonly List<(TextBlock Block, string Word)> _falling = new();

    HangulComposer _composer = new();
    double _spawnCooldown;
    int _score;
    int _kills;
    int _level = 1;
    int _lives = StartLives;
    int _strokes;
    int _hitUnits;
    bool _over;
    Window? _window;

    public AcidRainView(MainWindow main, KeyboardLayout layout)
    {
        InitializeComponent();
        _main = main;
        _layout = layout;
        Kb.SetLayout(layout);
        Kb.SetNext(null);

        var (modules, errors) = ContentModule.LoadDir(Path.Combine(AppConfig.DataDir, "words"));
        _pool = modules.Where(m => m.Items is not null).SelectMany(m => m.Items!)
            .Where(w => w.Length <= 5).ToList();

        _timer.Tick += Tick;
        UpdateHud();

        Loaded += (_, _) =>
        {
            _window = Window.GetWindow(this);
            if (_window is not null) _window.PreviewKeyDown += OnKey;
            ErrorDialog.ShowErrors(_window, errors);
            if (_pool.Count == 0)
            {
                _over = true;
                OverTitle.Text = "낱말이 아직 없습니다";
                OverDetail.Text = "삼흥사전에서 낱말을 뽑으면 산성비를 즐길수 있습니다.";
                GameOverPanel.Visibility = Visibility.Visible;
                return;
            }
            _watch.Start();
            _timer.Start();
        };
        Unloaded += (_, _) =>
        {
            _timer.Stop();
            if (_window is not null) _window.PreviewKeyDown -= OnKey;
        };
    }

    // 단계가 오를수록 빨리 떨어지고 자주 쏟아진다. 1단계도 헐겁지 않게 시작.
    double Speed => 58 + 15 * (_level - 1);                        // px/초 (1단계 58 → 5단계 118 → 10단계 193)
    double SpawnEvery => Math.Max(0.45, 1.75 - 0.13 * (_level - 1)); // 초 (1단계 1.75 → 5단계 1.23 → 10단계 0.58)

    void Tick(object? sender, EventArgs e)
    {
        if (_over) return;
        double dt = _timer.Interval.TotalSeconds;

        _spawnCooldown -= dt;
        if (_spawnCooldown <= 0)
        {
            Spawn();
            _spawnCooldown = SpawnEvery;
        }

        for (int i = _falling.Count - 1; i >= 0; i--)
        {
            var (block, word) = _falling[i];
            double top = Canvas.GetTop(block) + Speed * dt;
            Canvas.SetTop(block, top);
            if (top > Field.ActualHeight - 26)
            {
                Field.Children.Remove(block);
                _falling.RemoveAt(i);
                _lives--;
                if (_lives <= 0)
                {
                    GameOver();
                    return;
                }
            }
            else if (top > Field.ActualHeight * 0.66)
            {
                block.Foreground = (Brush)FindResource("Wrong");
            }
        }
        UpdateHud();
    }

    /// <summary>낱말 하나 고르기. 단계가 오르면 두번 뽑아 더 긴 쪽을 택해 어려운 낱말이 자주 나오게.</summary>
    string PickWord()
    {
        string w = _pool[_rng.Next(_pool.Count)];
        if (_level >= 4)
        {
            string w2 = _pool[_rng.Next(_pool.Count)];
            if (w2.Length > w.Length) w = w2;
        }
        return w;
    }

    void Spawn()
    {
        // 화면에 이미 있는 낱말과 겹치지 않게 몇번 다시 뽑는다(생성 낭비 방지).
        string word = PickWord();
        for (int t = 0; t < 5 && _falling.Any(f => f.Word == word); t++) word = PickWord();
        if (_falling.Any(f => f.Word == word)) return;

        var block = new TextBlock
        {
            Text = word, FontSize = 20, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("Ink"),
        };
        double width = Math.Max(Field.ActualWidth, 400);
        Canvas.SetLeft(block, 16 + _rng.NextDouble() * (width - 120));
        Canvas.SetTop(block, -24);
        Field.Children.Add(block);
        _falling.Add((block, word));
    }

    void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _main.Navigate(() => new StartView(_main));
            return;
        }
        if (_over)
        {
            if (e.Key is Key.Enter or Key.Return)
            {
                e.Handled = true;
                _main.Navigate(() => new AcidRainView(_main, _layout));
            }
            return;
        }
        if (e.Key == Key.Back)
        {
            e.Handled = true;
            _composer.Backspace();
            RefreshInput();
            return;
        }
        if (e.Key is Key.Enter or Key.Return)
        {
            e.Handled = true;
            _composer = new HangulComposer();
            RefreshInput();
            return;
        }

        string? tok = KeyMapper.ToToken(e.Key);
        if (tok is null || tok == " ") return;
        e.Handled = true;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        string? jamo = _layout.JamoFor(tok, shift);
        if (jamo is { Length: 1 }) _composer.PutJamo(jamo[0]);
        else if (tok.Length == 1) _composer.PutText(tok[0]);
        else return;
        _strokes++;
        Kb.Flash(tok);
        TryMatch();
        RefreshInput();
    }

    void TryMatch()
    {
        string typed = _composer.Text;
        for (int i = 0; i < _falling.Count; i++)
        {
            if (_falling[i].Word != typed) continue;
            var (block, word) = _falling[i];
            Field.Children.Remove(block);
            _falling.RemoveAt(i);
            _score += word.Length * 10;
            _kills++;
            _hitUnits += KeystrokePlanner.PlanText(word, _layout).Sum(u => u.Count);
            if (_kills % KillsPerLevel == 0)
            {
                _level++;
                FlashLevel();
            }
            _composer = new HangulComposer();
            UpdateHud();
            return;
        }
    }

    void GameOver()
    {
        _over = true;
        _timer.Stop();
        _watch.Stop();
        var config = AppConfig.Load();
        bool best = _score > config.HighScore;
        if (best)
        {
            config.HighScore = _score;
            config.Save();
        }
        OverTitle.Text = "끝!";
        OverDetail.Text = $"점수 {_score:N0} · 단계 {_level}" + (best ? " — 최고기록 갱신!" : $" · 최고 {config.HighScore:N0}");
        GameOverPanel.Visibility = Visibility.Visible;
        UpdateHud();
    }

    /// <summary>단계가 오른 순간 밭 가운데에 "단계 N"을 잠깐 띄워 진행이 느껴지게.</summary>
    void FlashLevel()
    {
        var t = new TextBlock
        {
            Text = $"단계 {_level}",
            FontSize = 46,
            FontWeight = FontWeights.ExtraBold,
            Foreground = (Brush)FindResource("Accent"),
            IsHitTestVisible = false,
        };
        t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double width = Math.Max(Field.ActualWidth, 400);
        Canvas.SetLeft(t, (width - t.DesiredSize.Width) / 2);
        Canvas.SetTop(t, Field.ActualHeight / 2 - 34);
        Field.Children.Add(t);

        var fade = new System.Windows.Media.Animation.DoubleAnimation(0.55, 0.0, TimeSpan.FromMilliseconds(1000));
        fade.Completed += (_, _) => Field.Children.Remove(t);
        t.BeginAnimation(OpacityProperty, fade);
    }

    void RefreshInput() => InputText.Text = _composer.Text.Length == 0 ? " " : _composer.Text;

    void UpdateHud()
    {
        int high = Math.Max(_score, AppConfig.Load().HighScore);
        ScoreText.Text = $"점수 {_score:N0} · 단계 {_level} · 목숨 {_lives} · 최고 {high:N0}";
        double cpm = TypingStats.Cpm(_strokes, _watch.Elapsed);
        double acc = _strokes == 0 ? 100 : Math.Min(100, _hitUnits * 100.0 / _strokes);
        Stats.Update(cpm, acc, _kills % KillsPerLevel * (100.0 / KillsPerLevel));
    }

    void Back_Click(object sender, RoutedEventArgs e) => _main.Navigate(() => new StartView(_main));
}
