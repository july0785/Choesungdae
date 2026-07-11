namespace ChosonTyping.Core;

/// <summary>자리련습 단계(설계서 6항): 기본자리(가운데줄)부터 우·아래줄로 넓혀간다.</summary>
public sealed record DrillStage(string Name, string[] Tokens);

public static class DrillLesson
{
    public static readonly DrillStage[] Stages =
    {
        new("기본자리", new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" }),
        new("웃줄", new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" }),
        new("아래줄", new[] { "Z", "X", "C", "V", "B", "N", "M" }),
        new("모든 자리", new[]
        {
            "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
            "A", "S", "D", "F", "G", "H", "J", "K", "L",
            "Z", "X", "C", "V", "B", "N", "M",
        }),
    };

    /// <summary>배렬에서 자모가 있는 글쇠만 골라 마구잡이 차례를 만든다(같은 글쇠 연속 없음).</summary>
    public static List<string> Sequence(DrillStage stage, KeyboardLayout layout, int count = 30)
    {
        var pool = stage.Tokens.Where(t => layout.JamoFor(t, false) is not null).ToArray();
        var rng = new Random();
        var seq = new List<string>(count);
        string? prev = null;
        for (int i = 0; i < count; i++)
        {
            string pick;
            do
            {
                pick = pool[rng.Next(pool.Length)];
            } while (pick == prev && pool.Length > 1);
            seq.Add(pick);
            prev = pick;
        }
        return seq;
    }
}
