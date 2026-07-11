namespace ChosonTyping.Core;

/// <summary>글쇠 단위 하나: 물리 토큰, 윗글쇠 여부, 만들어지는 자모('\0'이면 자모 아닌 그대로 글자).</summary>
public sealed record KeyUnit(string Token, bool Shift, char Jamo);

/// <summary>
/// 본보기글을 배렬에 맞는 글쇠 차례로 풀어낸다 — 다음 칠 글쇠 안내와 진행 판정의 근거.
/// 창덕건반처럼 겹모음 글쇠가 있으면 한 타로, 없으면 두 타로 푼다.
/// </summary>
public static class KeystrokePlanner
{
    /// <summary>자모 → (토큰, 윗글쇠). 기본글쇠 우선.</summary>
    public static Dictionary<char, (string Tok, bool Shift)> ReverseMap(KeyboardLayout layout)
    {
        var map = new Dictionary<char, (string, bool)>();
        foreach (var (tok, def) in layout.Keys)
        {
            if (def.Base is { Length: 1 } b && !map.ContainsKey(b[0])) map[b[0]] = (tok, false);
        }
        foreach (var (tok, def) in layout.Keys)
        {
            if (def.Shift is { Length: 1 } s && !map.ContainsKey(s[0])) map[s[0]] = (tok, true);
        }
        return map;
    }

    /// <summary>글자 하나의 글쇠 단위 목록. 칠수 없는 글자면 null.</summary>
    public static List<KeyUnit>? PlanChar(char c, KeyboardLayout layout, Dictionary<char, (string Tok, bool Shift)> rev)
    {
        if (c >= 0xAC00 && c <= 0xD7A3)
        {
            int code = c - 0xAC00;
            char l = Jamo.Cho[code / (21 * 28)];
            char v = Jamo.Jung[code / 28 % 21];
            int t = code % 28;
            var units = new List<KeyUnit>();

            if (!rev.TryGetValue(l, out var lk)) return null;
            units.Add(new KeyUnit(lk.Tok, lk.Shift, l));

            if (rev.TryGetValue(v, out var vk))
            {
                units.Add(new KeyUnit(vk.Tok, vk.Shift, v));
            }
            else
            {
                var pair = Jamo.VowelPairs.FirstOrDefault(p => p.Value == v);
                if (pair.Value != v) return null;
                if (!rev.TryGetValue(pair.Key.Item1, out var v1) ||
                    !rev.TryGetValue(pair.Key.Item2, out var v2)) return null;
                units.Add(new KeyUnit(v1.Tok, v1.Shift, pair.Key.Item1));
                units.Add(new KeyUnit(v2.Tok, v2.Shift, pair.Key.Item2));
            }

            if (t > 0)
            {
                char tc = Jamo.Jong[t];
                if (rev.TryGetValue(tc, out var tk))
                {
                    units.Add(new KeyUnit(tk.Tok, tk.Shift, tc));
                }
                else
                {
                    var pair = Jamo.JongPairs.FirstOrDefault(p => p.Value == tc);
                    if (pair.Value != tc) return null;
                    if (!rev.TryGetValue(pair.Key.Item1, out var t1) ||
                        !rev.TryGetValue(pair.Key.Item2, out var t2)) return null;
                    units.Add(new KeyUnit(t1.Tok, t1.Shift, pair.Key.Item1));
                    units.Add(new KeyUnit(t2.Tok, t2.Shift, pair.Key.Item2));
                }
            }
            return units;
        }

        // 한글이 아닌 글자: 그 글쇠가 자모를 내지 않을 때만 그대로 칠수 있다.
        string tok2 = c.ToString();
        if (c == ' ') return new List<KeyUnit> { new(" ", false, '\0') };
        if (c == '\n') return new List<KeyUnit> { new("\n", false, '\0') };
        if (layout.JamoFor(tok2, false) is null)
            return new List<KeyUnit> { new(tok2, false, '\0') };
        return null;
    }

    /// <summary>본보기글 전체를 글자별 글쇠 단위 목록으로. 칠수 없는 글자는 빈 목록.</summary>
    public static List<List<KeyUnit>> PlanText(string text, KeyboardLayout layout)
    {
        var rev = ReverseMap(layout);
        var plan = new List<List<KeyUnit>>(text.Length);
        foreach (var c in text)
            plan.Add(PlanChar(c, layout, rev) ?? new List<KeyUnit>());
        return plan;
    }
}
