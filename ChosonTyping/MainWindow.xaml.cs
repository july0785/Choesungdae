using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChosonTyping.Core;

namespace ChosonTyping;

public partial class MainWindow : Window
{
    Func<UserControl>? _factory;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) =>
            Shell.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        Navigate(() => new Views.StartView(this));
    }

    /// <summary>화면 전환의 틀(설계서 3.1). 낮밤을 바꿀 때 다시 만들수 있게 만드는 법을 받는다.</summary>
    public void Navigate(Func<UserControl> factory)
    {
        _factory = factory;
        Host.Content = factory();
    }

    void AppBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void ThemeBtn_Click(object sender, RoutedEventArgs e)
    {
        var config = AppConfig.Load();
        config.Theme = config.Theme == "dark" ? "light" : "dark";
        config.Save();
        App.ApplyTheme(config.Theme);
        if (_factory is not null) Host.Content = _factory();
    }

    void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void MaxBtn_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}
