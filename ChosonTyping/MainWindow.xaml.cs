using System.Windows;
using System.Windows.Controls;

namespace ChosonTyping;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Navigate(new Views.StartView(this));
    }

    /// <summary>화면 전환의 틀(설계서 3.1) — 모든 화면은 이 틀 안에서 갈아끼운다.</summary>
    public void Navigate(UserControl view) => Host.Content = view;
}
