namespace ChosonTyping.Core;

/// <summary>글쇠 토큰 → 손가락 이름. 자리련습의 손가락 안내(설계서 6항)에 쓴다.</summary>
public static class FingerMap
{
    static readonly Dictionary<string, string> Map = new();

    static FingerMap()
    {
        void Add(string finger, params string[] toks)
        {
            foreach (var t in toks) Map[t] = finger;
        }

        Add("왼손 새끼", "`", "1", "Q", "A", "Z");
        Add("왼손 약", "2", "W", "S", "X");
        Add("왼손 가운데", "3", "E", "D", "C");
        Add("왼손 집게", "4", "5", "R", "T", "F", "G", "V", "B");
        Add("오른손 집게", "6", "7", "Y", "U", "H", "J", "N", "M");
        Add("오른손 가운데", "8", "I", "K", ",");
        Add("오른손 약", "9", "O", "L", ".");
        Add("오른손 새끼", "0", "-", "=", "P", ";", "'", "[", "]", "\\", "/");
        Map[" "] = "엄지";
    }

    public static string For(string token) => Map.TryGetValue(token, out var f) ? f : "";
}
