using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChosonTyping.Core;

namespace ChosonTyping.Views;

/// <summary>긴글 목록: 내장 긴글과 불러온 글(.ctp)이 나란히 뜬다(설계서 10.3).</summary>
public partial class TextListView : UserControl
{
    readonly MainWindow _main;
    readonly KeyboardLayout _layout;
    readonly bool _isTest;

    public TextListView(MainWindow main, KeyboardLayout layout, bool isTest)
    {
        InitializeComponent();
        _main = main;
        _layout = layout;
        _isTest = isTest;
        TitleText.Text = _isTest ? "타자검정" : "긴글련습";
        SubText.Text = _isTest
            ? "글 하나를 골라 처음부터 끝까지 치면 속도와 정확도를 재여 급수를 매깁니다."
            : "글 하나를 골라 처음부터 끝까지 따라 칩니다.";
        Loaded += (_, _) => Reload();
    }

    void Reload()
    {
        TextList.Children.Clear();
        var errors = new List<string>();

        var (builtin, e1) = ContentModule.LoadDir(Path.Combine(AppConfig.DataDir, "longtext"));
        var (imported, e2) = ImportedText.LoadAll();
        errors.AddRange(e1);
        errors.AddRange(e2);

        int i = 0;
        foreach (var (m, tag) in builtin.Select(m => (m, "내장"))
                     .Concat(imported.Select(m => (m, "불러온 글"))))
        {
            TextList.Children.Add(MakeRow(m, tag, i++));
        }
        if (i == 0)
        {
            TextList.Children.Add(new TextBlock
            {
                Text = "열수 있는 글이 없습니다 — 아래 《불러오기》로 .txt를 넣어보십시오.",
                FontSize = 13, Foreground = (Brush)FindResource("Faint"),
                Margin = new Thickness(4, 8, 0, 8),
            });
        }
        ErrorDialog.ShowErrors(Window.GetWindow(this), errors);
    }

    Border MakeRow(ContentModule m, string tag, int index)
    {
        var row = new DockPanel();
        row.Children.Add(new TextBlock
        {
            Text = (index + 1).ToString(), FontSize = 12, Width = 22,
            Foreground = (Brush)FindResource("Faint"), VerticalAlignment = VerticalAlignment.Center,
        });
        var tagText = new TextBlock
        {
            Text = tag, FontSize = 11,
            Foreground = (Brush)FindResource(tag == "내장" ? "Faint" : "Sky"),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(tagText, Dock.Right);
        row.Children.Add(tagText);
        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = m.Title, FontSize = 15, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("Ink"),
        });
        if (!string.IsNullOrEmpty(m.Source))
        {
            body.Children.Add(new TextBlock
            {
                Text = m.Source, FontSize = 12, Foreground = (Brush)FindResource("Mid"),
                Margin = new Thickness(0, 2, 0, 0),
            });
        }
        row.Children.Add(body);

        var border = new Border
        {
            BorderBrush = (Brush)FindResource("Hair"),
            BorderThickness = new Thickness(0, index == 0 ? 1 : 0, 0, 1),
            Padding = new Thickness(4, 12, 4, 12),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = row,
        };
        border.MouseEnter += (_, _) => border.Background = (Brush)FindResource("Soft");
        border.MouseLeave += (_, _) => border.Background = Brushes.Transparent;
        border.MouseLeftButtonUp += (_, _) =>
            _main.Navigate(() => new LongTextView(_main, _layout, m, _isTest));
        return border;
    }

    void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "글 화일 (*.txt)|*.txt",
            Title = "긴글 불러오기",
        };
        if (dlg.ShowDialog() != true) return;

        string body;
        try
        {
            body = File.ReadAllText(dlg.FileName);
        }
        catch (Exception ex)
        {
            ErrorDialog.ShowErrors(Window.GetWindow(this), new[] { $"《{Path.GetFileName(dlg.FileName)}》 — {ex.Message}" });
            return;
        }
        if (body.Trim().Length == 0) return;

        var input = new InputDialog(Path.GetFileNameWithoutExtension(dlg.FileName), "사용자 불러오기")
        {
            Owner = Window.GetWindow(this),
        };
        if (input.ShowDialog() != true) return;

        ImportedText.Save(input.TitleText, input.SourceText, body);
        Reload();
    }

    void Back_Click(object sender, RoutedEventArgs e) => _main.Navigate(() => new StartView(_main));
}
