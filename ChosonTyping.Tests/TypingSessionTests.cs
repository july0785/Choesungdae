using System.IO;
using ChosonTyping.Core;

namespace ChosonTyping.Tests;

public class TypingSessionTests
{
    static KeyboardLayout Layout(string id) =>
        KeyboardLayout.Load(Path.Combine(AppContext.BaseDirectory, "data", "layouts", id + ".json"));

    [Fact]
    public void 국규에서_안을_치는_글쇠차례()
    {
        var s = new TypingSession("안", Layout("kukgyu"));
        Assert.Equal(("D", false), s.NextKey());
        s.Feed("D", false);                       // ㅇ
        Assert.Equal(("J", false), s.NextKey());
        s.Feed("J", false);                       // ㅏ
        Assert.Equal(("F", false), s.NextKey());
        s.Feed("F", false);                       // ㄴ
        Assert.True(s.Done);
        Assert.Null(s.NextKey());
        Assert.Equal(3, s.Strokes);
    }

    [Fact]
    public void 창덕에서는_과가_두_타()
    {
        var s = new TypingSession("과", Layout("changdeok"));
        s.Feed("R", false);                       // ㄱ
        Assert.Equal((",", false), s.NextKey());  // 통짜 ㅘ
        s.Feed(",", false);
        Assert.True(s.Done);
        Assert.Equal(2, s.Strokes);
    }

    [Fact]
    public void 국규에서는_과가_세_타()
    {
        var s = new TypingSession("과", Layout("kukgyu"));
        s.Feed("S", false);                       // ㄱ
        s.Feed("H", false);                       // ㅗ
        Assert.Equal(("J", false), s.NextKey());  // ㅏ
        s.Feed("J", false);
        Assert.True(s.Done);
    }

    [Fact]
    public void 윗글쇠_자모_안내()
    {
        var s = new TypingSession("빠", Layout("kukgyu"));
        Assert.Equal(("Q", true), s.NextKey());   // ㅃ = 윗글쇠+Q
    }

    [Fact]
    public void 틀리면_안내를_숨기고_정확도가_내려간다()
    {
        var s = new TypingSession("안", Layout("kukgyu"));
        s.Feed("D", false);                       // ㅇ (맞음)
        s.Feed("K", false);                       // ㅣ (틀림 — ㅏ여야 함)
        Assert.Null(s.NextKey());
        Assert.True(s.PositionalAccuracy < 100);
        s.Backspace();                            // ㅣ 되돌리기
        Assert.Equal(("J", false), s.NextKey());
    }

    [Fact]
    public void 사이띄기와_구두점도_계획된다()
    {
        var s = new TypingSession("가 나.", Layout("kukgyu"));
        foreach (var (t, sh) in new[] { ("S", false), ("J", false), (" ", false), ("F", false), ("J", false), (".", false) })
            s.Feed(t, sh);
        Assert.True(s.Done);
    }
}
