namespace ChosonTyping.Core;

/// <summary>
/// 본보기글 하나를 치는 한판 — 조합기·글쇠계획·측정을 한데 묶는다.
/// 낱말련습·짧은글련습·긴글련습·타자검정이 같이 쓴다.
/// 막지 않는다: 틀린 자모도 조합기에 들어가고, 색으로만 표시한다(설계서 11.3).
/// </summary>
public sealed class TypingSession
{
    readonly KeyboardLayout _layout;
    readonly List<List<KeyUnit>> _plan;
    readonly int _totalUnits;

    public string Target { get; }
    public HangulComposer Composer { get; } = new();
    public int Strokes { get; private set; }

    public TypingSession(string target, KeyboardLayout layout)
    {
        Target = target;
        _layout = layout;
        _plan = KeystrokePlanner.PlanText(target, layout);
        _totalUnits = _plan.Sum(u => u.Count);
    }

    public string Typed => Composer.Text;
    public bool Done => Typed == Target;

    /// <summary>정확도(%) — 친 자리의 마지막 상태 기준(설계서 11.1).</summary>
    public double PositionalAccuracy => TypingStats.Accuracy(Target, Typed);

    /// <summary>진행률(%).</summary>
    public double Progress => TypingStats.Progress(Target, Typed);

    /// <summary>물리 글쇠 하나를 먹인다. 글자를 만들었으면 true(타수에 셈).</summary>
    public bool Feed(string token, bool shift)
    {
        string? jamo = _layout.JamoFor(token, shift);
        if (jamo is { Length: 1 })
        {
            Composer.PutJamo(jamo[0]);
        }
        else if (token.Length == 1)
        {
            Composer.PutText(token[0]);
        }
        else
        {
            return false;
        }
        Strokes++;
        return true;
    }

    public void FeedNewline()
    {
        Composer.PutText('\n');
        Strokes++;
    }

    /// <summary>자모 단위 지우기 — 타수에 넣지 않는다.</summary>
    public bool Backspace() => Composer.Backspace();

    /// <summary>
    /// 다음 칠 글쇠. 지금까지 전부 옳게 쳤을 때만 안내하고,
    /// 틀린 데가 있으면 null(고치라는 뜻으로 안내를 숨긴다).
    /// </summary>
    public (string Token, bool Shift)? NextKey()
    {
        int consumed = CorrectUnits(out bool allCorrect);
        if (!allCorrect || consumed >= _totalUnits) return null;
        int acc = 0;
        foreach (var units in _plan)
        {
            if (consumed < acc + units.Count)
            {
                var u = units[consumed - acc];
                return (u.Token, u.Shift);
            }
            acc += units.Count;
        }
        return null;
    }

    /// <summary>옳게 진행된 글쇠 단위수. 조합 중 음절은 단위 접두사가 맞을 때만 센다.</summary>
    public int CorrectUnits(out bool allCorrect)
    {
        allCorrect = true;
        string typed = Typed;
        int composingLen = Composer.Composing.Length;
        int fullChars = typed.Length - composingLen;
        int units = 0;

        for (int i = 0; i < fullChars; i++)
        {
            if (i >= Target.Length || typed[i] != Target[i])
            {
                allCorrect = false;
                return units;
            }
            units += _plan[i].Count;
        }

        if (composingLen > 0)
        {
            int i = fullChars;
            if (i >= Target.Length)
            {
                allCorrect = false;
                return units;
            }
            var planUnits = _plan[i];
            var cur = Composer.ComposingUnits;
            if (cur.Count > planUnits.Count)
            {
                allCorrect = false;
                return units;
            }
            for (int k = 0; k < cur.Count; k++)
            {
                if (planUnits[k].Jamo != cur[k])
                {
                    allCorrect = false;
                    return units;
                }
            }
            units += cur.Count;
        }
        return units;
    }
}
