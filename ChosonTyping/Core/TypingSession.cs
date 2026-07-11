namespace ChosonTyping.Core;

/// <summary>글자 하나의 형편(설계서 11.3): 안 침 / 맞음 / 틀림 / 조합 중.</summary>
public enum CharState
{
    Untyped,
    Correct,
    Wrong,
    Composing,
}

/// <summary>
/// 본보기글 하나를 치는 한판 — 조합기·글쇠계획·측정을 한데 묶는다.
/// 진행 판정은 글쇠 단위의 평면 대조로 한다. 받침이 다음 글자 몫으로
/// 잠시 붙는 도깨비불 과도 상태(례: 구수를 칠 때의 《굿》)도 옳은 진행으로 본다.
/// </summary>
public sealed class TypingSession
{
    readonly KeyboardLayout _layout;
    readonly List<List<KeyUnit>> _plan;
    readonly List<KeyUnit> _flat;

    public string Target { get; }
    public HangulComposer Composer { get; } = new();
    public int Strokes { get; private set; }

    public TypingSession(string target, KeyboardLayout layout)
    {
        Target = target;
        _layout = layout;
        _plan = KeystrokePlanner.PlanText(target, layout);
        _flat = _plan.SelectMany(u => u).ToList();
    }

    public string Typed => Composer.Text;
    public bool Done => Typed == Target;

    /// <summary>정확도(%) — 친 자리의 마지막 상태 기준(설계서 11.2).</summary>
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
        if (!allCorrect || consumed >= _flat.Count) return null;
        var u = _flat[consumed];
        return (u.Token, u.Shift);
    }

    /// <summary>
    /// 옳게 진행된 글쇠 단위수. 커밋된 글자는 글자로, 조합 중 음절은
    /// 단위 자모를 평면 계획에 대고 잰다(글자 경계를 넘는 과도 상태 포함).
    /// </summary>
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
            var cur = Composer.ComposingUnits;
            for (int k = 0; k < cur.Count; k++)
            {
                int fi = units + k;
                if (fi >= _flat.Count || _flat[fi].Jamo != cur[k])
                {
                    allCorrect = false;
                    return units;
                }
            }
            units += cur.Count;
        }
        return units;
    }

    /// <summary>본보기글 i번째 글자의 형편 — 화면 색칠의 근거.</summary>
    public CharState StateAt(int i)
    {
        string typed = Typed;
        int composingLen = Composer.Composing.Length;
        int fullChars = typed.Length - composingLen;

        if (i < fullChars)
            return i < Target.Length && typed[i] == Target[i] ? CharState.Correct : CharState.Wrong;

        if (composingLen > 0 && i == fullChars)
        {
            CorrectUnits(out bool allCorrect);
            return allCorrect ? CharState.Composing : CharState.Wrong;
        }
        return CharState.Untyped;
    }
}
