using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ChosonTyping.Views;

/// <summary>잔잔한 화면 동작(설계서 7항 — 은은하게).</summary>
public static class ViewFx
{
    /// <summary>줄이 바뀔 때 아래에서 살짝 밀려 올라오며 나타난다.</summary>
    public static void SlideIn(UIElement el)
    {
        var tt = new TranslateTransform(0, 14);
        el.RenderTransform = tt;
        var dur = TimeSpan.FromMilliseconds(230);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(14, 0, dur) { EasingFunction = ease });
        el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, dur) { EasingFunction = ease });
    }
}
