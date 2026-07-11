using System.Text;

namespace ChosonTyping.Core;

/// <summary>
/// 두벌식 모아쓰기 오토마타(설계서 5항). 윈도우 IME를 쓰지 않고 직접 조합한다.
/// 조합 중 음절은 글쇠 단위 목록(_units)로 관리한다 — 한 글쇠 = 한 단위라서
/// 백스페이스가 정확히 한 타를 되돌린다(창덕건반 통짜 겹모음 포함).
/// </summary>
public sealed class HangulComposer
{
    private readonly StringBuilder _committed = new();
    private readonly List<char> _units = new();

    /// <summary>지금 조합 중인 음절(없으면 빈 문자열).</summary>
    public string Composing { get; private set; } = "";

    /// <summary>조합 중 음절의 글쇠 단위 자모들 — 진행 판정용.</summary>
    public IReadOnlyList<char> ComposingUnits => _units;

    /// <summary>커밋된 글 + 조합 중 음절.</summary>
    public string Text => _committed + Composing;

    /// <summary>자모 한 타를 넣는다. 겹모음 문자(ㅘ 등)를 통째로 넣어도 된다(창덕건반).</summary>
    public void PutJamo(char j)
    {
        if (Jamo.IsVowel(j)) PutVowel(j);
        else PutConsonant(j);
        Composing = Derive(_units);
    }

    /// <summary>자모 아닌 글자(사이띄기·수자·영문 등) — 조합을 끝내고 그대로 붙인다.</summary>
    public void PutText(char ch)
    {
        Flush();
        _committed.Append(ch);
    }

    /// <summary>자모 단위 지우기. 조합 중이면 한 타를 되돌리고, 아니면 커밋된 글자를 글자째 지운다.</summary>
    public bool Backspace()
    {
        if (_units.Count > 0)
        {
            _units.RemoveAt(_units.Count - 1);
            Composing = Derive(_units);
            return true;
        }
        if (_committed.Length > 0)
        {
            _committed.Remove(_committed.Length - 1, 1);
            return true;
        }
        return false;
    }

    /// <summary>조합 중 음절을 확정한다.</summary>
    public void Flush()
    {
        if (Composing.Length == 0) return;
        _committed.Append(Composing);
        _units.Clear();
        Composing = "";
    }

    // ── 내부 ──────────────────────────────────────────

    private void PutVowel(char j)
    {
        var (l, v, t) = Split(_units);

        if (t.Count > 0)
        {
            // 도깨비불: 마지막 받침 단위가 새 음절의 초성으로 넘어간다 (값+ㅏ → 갑사).
            char moved = _units[^1];
            var stay = _units.GetRange(0, _units.Count - 1);
            _committed.Append(Derive(stay));
            _units.Clear();
            _units.Add(moved);
            _units.Add(j);
            return;
        }

        if (v.Count == 1 && Jamo.VowelPairs.ContainsKey((v[0], j)))
        {
            _units.Add(j);                    // ㅗ+ㅏ → ㅘ
            return;
        }

        if (v.Count > 0)
        {
            CommitUnits();                    // 겹모음 확장 불가 → 새 음절
            _units.Add(j);
            return;
        }

        _units.Add(j);                        // 빈 상태 또는 초성 뒤
        _ = l;
    }

    private void PutConsonant(char j)
    {
        var (l, v, t) = Split(_units);

        if (l is not null && v.Count > 0 && t.Count == 0 && Jamo.CanBeJong(j))
        {
            _units.Add(j);                    // 받침으로
            return;
        }

        if (t.Count == 1 && Jamo.JongPairs.ContainsKey((t[0], j)))
        {
            _units.Add(j);                    // 겹받침으로
            return;
        }

        if (_units.Count > 0) CommitUnits();  // 그 밖엔 새 음절
        _units.Add(j);
    }

    private void CommitUnits()
    {
        _committed.Append(Derive(_units));
        _units.Clear();
    }

    /// <summary>단위 목록 → (초성, 모음단위들, 받침단위들). 규칙에 맞는 목록만 들어온다.</summary>
    private static (char? l, List<char> v, List<char> t) Split(List<char> units)
    {
        char? l = null;
        var v = new List<char>();
        var t = new List<char>();
        foreach (var u in units)
        {
            if (Jamo.IsVowel(u)) v.Add(u);
            else if (v.Count == 0) l = u;
            else t.Add(u);
        }
        return (l, v, t);
    }

    private static string Derive(List<char> units)
    {
        if (units.Count == 0) return "";
        var (l, v, t) = Split(units);

        if (v.Count == 0) return l!.Value.ToString();

        char jung = v.Count == 2 ? Jamo.VowelPairs[(v[0], v[1])] : v[0];
        if (l is null) return jung.ToString();

        char? jong = t.Count switch
        {
            0 => null,
            1 => t[0],
            _ => Jamo.JongPairs[(t[0], t[1])],
        };
        int jongIdx = jong is null ? 0 : Jamo.Jong.IndexOf(jong.Value);
        return Jamo.Compose(Jamo.Cho.IndexOf(l.Value), Jamo.Jung.IndexOf(jung), jongIdx).ToString();
    }
}
