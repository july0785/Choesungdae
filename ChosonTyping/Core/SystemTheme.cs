using Microsoft.Win32;

namespace ChosonTyping.Core;

/// <summary>윈도우 시스템의 밝은/어두운 화면형식 읽기(등록부는 읽기만 한다 — 포터블).</summary>
public static class SystemTheme
{
    const string Key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>프로그람(앱) 화면형식이 밝은가 — 켜질 때 화면형식 자동 지정용.</summary>
    public static bool AppsUseLight() => Read("AppsUseLightTheme");

    /// <summary>작업표시줄(계통) 화면형식이 밝은가 — 작업표시줄 아이콘 색 고르기용.</summary>
    public static bool TaskbarUsesLight() => Read("SystemUsesLightTheme");

    static bool Read(string name)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key);
            return k?.GetValue(name) is int v ? v != 0 : true;
        }
        catch (Exception)
        {
            return true;
        }
    }
}
