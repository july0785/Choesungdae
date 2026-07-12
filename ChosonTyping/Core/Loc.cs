namespace ChosonTyping.Core;

/// <summary>
/// 화면 글자 번역(조선어·영어·일본어). 타자련습 내용(낱말·문장·가사)은 조선어 그대로이고,
/// 메뉴·안내·단추 같은 껍데기 글자만 번역한다. 건반 글쇠 이름은 조선 실물 그대로 둔다.
/// </summary>
public static class Loc
{
    /// <summary>지금 화면 언어: "ko" | "en" | "ja".</summary>
    public static string Lang { get; set; } = "ko";

    public static readonly (string Code, string Name)[] Languages =
    {
        ("ko", "조선어"), ("en", "English"), ("ja", "日本語"),
    };

    static int Idx => Lang switch { "en" => 1, "ja" => 2, _ => 0 };

    /// <summary>열쇠로 번역글을 얻는다. 없으면 열쇠 그대로.</summary>
    public static string S(string key) => Table.TryGetValue(key, out var v) ? v[Idx] : key;

    /// <summary>번역글에 값을 끼워넣는다({0},{1}...).</summary>
    public static string F(string key, params object[] args) => string.Format(S(key), args);

    // key => [조선어, English, 日本語]
    static readonly Dictionary<string, string[]> Table = new()
    {
        ["app.version"]      = new[] { "판 1.0", "v1.0", "版1.0" },
        ["app.caption"]      = new[] { "건반 익히기 프로그람", "Keyboard trainer", "キーボード練習プログラム" },
        ["tip.settings"]     = new[] { "설정", "Settings", "設定" },
        ["tip.min"]          = new[] { "줄이기", "Minimize", "最小化" },
        ["tip.max"]          = new[] { "키우기", "Maximize", "最大化" },
        ["tip.close"]        = new[] { "닫기", "Close", "閉じる" },

        ["nav.start"]        = new[] { "← 시작화면", "← Start", "← 開始画面" },
        ["nav.list"]         = new[] { "← 글 고르기", "← Pick a text", "← 文を選ぶ" },
        ["common.done"]      = new[] { "끝!", "Done!", "終わり！" },
        ["common.ok"]        = new[] { "확인", "OK", "確認" },

        ["stats.cpm"]        = new[] { "타속", "Speed", "速度" },
        ["stats.acc"]        = new[] { "정확도", "Accuracy", "正確度" },
        ["stats.unit"]       = new[] { "타/분", "kpm", "打/分" },
        ["stats.progress"]   = new[] { "진행률 {0}%", "Progress {0}%", "進行率 {0}%" },

        ["start.layouts"]    = new[] { "건반배렬", "Keyboard layout", "キーボード配列" },
        ["start.stages"]     = new[] { "련습단계", "Practice stage", "練習段階" },
        ["start.begin"]      = new[] { "련습 시작", "Start", "練習開始" },
        ["start.soon"]       = new[] { "준비중", "Soon", "準備中" },
        ["settings.language"]= new[] { "언어", "Language", "言語" },
        ["settings.theme"]   = new[] { "화면형식", "Theme", "画面形式" },
        ["theme.light"]      = new[] { "밝은 화면형식", "Light", "明るい" },
        ["theme.dark"]       = new[] { "어두운 화면형식", "Dark", "暗い" },

        ["layout.kukgyu"]        = new[] { "국규건반", "Kukgyu", "国規キーボード" },
        ["layout.kukgyu.sub"]    = new[] { "KPS 9256", "KPS 9256", "KPS 9256" },
        ["layout.changdeok"]     = new[] { "창덕건반", "Changdŏk", "昌徳キーボード" },
        ["layout.changdeok.sub"] = new[] { "", "", "" },
        ["layout.dubeol-std"]    = new[] { "대한민국 표준건반", "ROK Keyboard", "ROK Keyboard" },
        ["layout.dubeol-std.sub"]= new[] { "KS X 5002", "KS X 5002", "KS X 5002" },

        ["stage.drill"]      = new[] { "자리련습", "Key positions", "位置練習" },
        ["stage.drill.desc"] = new[] { "글쇠자리를 눈에 익히고 손에 익힙니다",
                                       "Learn where each key is, by eye and hand",
                                       "鍵の位置を目と手で覚えます" },
        ["stage.word"]       = new[] { "낱말련습", "Words", "単語練習" },
        ["stage.word.desc"]  = new[] { "화면에 나오는 낱말을 보고 정확히 칩니다",
                                       "Type the words shown, accurately",
                                       "画面の単語を正確に打ちます" },
        ["stage.sentence"]   = new[] { "짧은글련습", "Sentences", "短文練習" },
        ["stage.sentence.desc"]=new[]{ "짧은 문장을 되풀이해 치며 속도를 올립니다",
                                       "Build speed on short sentences",
                                       "短い文を繰り返し速度を上げます" },
        ["stage.long"]       = new[] { "긴글련습", "Long texts", "長文練習" },
        ["stage.long.desc"]  = new[] { "가요의 가사를 처음부터 끝까지 따라 칩니다",
                                       "Type a song's lyrics start to finish",
                                       "歌の歌詞を最初から最後まで打ちます" },
        ["stage.test"]       = new[] { "타자검정", "Typing test", "検定" },
        ["stage.test.desc"]  = new[] { "타자 속도와 정확도를 재여 급수를 매깁니다",
                                       "Measure speed and accuracy for a grade",
                                       "速度と正確度を測り級を判定します" },
        ["stage.rain"]       = new[] { "산성비", "Acid Rain", "酸性雨" },
        ["stage.rain.desc"]  = new[] { "떨어지는 낱말을 바닥에 닿기 전에 없앱니다",
                                       "Clear falling words before they land",
                                       "落ちる単語を着地前に消します" },

        ["drill.part1"]      = new[] { "기본자리", "Home row", "基本位置" },
        ["drill.part2"]      = new[] { "웃줄", "Top row", "上段" },
        ["drill.part3"]      = new[] { "아래줄", "Bottom row", "下段" },
        ["drill.part4"]      = new[] { "모든 자리", "All keys", "全位置" },
        ["drill.title"]      = new[] { "자리련습 · {0} {1}/{2}", "Key positions · {0} {1}/{2}", "位置練習 · {0} {1}/{2}" },
        ["drill.hint"]       = new[] { "{0}손가락 — {1} 자리", "{0} — {1} key", "{0} — {1}" },
        ["drill.next"]       = new[] { "다음 단계 — 넣기(Enter) · 시작화면 — Esc",
                                       "Next — Enter · Start screen — Esc",
                                       "次へ — Enter · 開始画面 — Esc" },

        ["finger.lp"] = new[] { "왼손 새끼", "left pinky", "左小指" },
        ["finger.lr"] = new[] { "왼손 약", "left ring", "左薬指" },
        ["finger.lm"] = new[] { "왼손 가운데", "left middle", "左中指" },
        ["finger.li"] = new[] { "왼손 집게", "left index", "左人差し指" },
        ["finger.ri"] = new[] { "오른손 집게", "right index", "右人差し指" },
        ["finger.rm"] = new[] { "오른손 가운데", "right middle", "右中指" },
        ["finger.rr"] = new[] { "오른손 약", "right ring", "右薬指" },
        ["finger.rp"] = new[] { "오른손 새끼", "right pinky", "右小指" },
        ["finger.th"] = new[] { "엄지", "thumb", "親指" },

        ["word.title"]       = new[] { "낱말련습 · {0}/{1}", "Words · {0}/{1}", "単語 · {0}/{1}" },
        ["word.empty"]       = new[] { "낱말이 아직 없습니다", "No words yet", "単語がまだありません" },
        ["word.retry"]       = new[] { "다시 — 넣기(Enter) · 시작화면 — Esc",
                                       "Again — Enter · Start — Esc", "もう一度 — Enter · 開始 — Esc" },
        ["word.escOnly"]     = new[] { "시작화면 — Esc", "Start — Esc", "開始画面 — Esc" },

        ["sent.title"]       = new[] { "짧은글련습 · {0}/{1}", "Sentences · {0}/{1}", "短文 · {0}/{1}" },
        ["sent.result"]      = new[] { "타속 {0} 타/분 · 정확도 {1} %",
                                       "Speed {0} kpm · Accuracy {1}%", "速度 {0} 打/分 · 正確度 {1}%" },
        ["sent.retry"]       = new[] { "다시 — 넣기(Enter) · 시작화면 — Esc",
                                       "Again — Enter · Start — Esc", "もう一度 — Enter · 開始 — Esc" },

        ["long.title"]       = new[] { "긴글련습 · {0} · {1}/{2}줄", "Long text · {0} · line {1}/{2}", "長文 · {0} · {1}/{2}行" },
        ["long.testTitle"]   = new[] { "타자검정 · {0} · {1}/{2}줄", "Test · {0} · line {1}/{2}", "検定 · {0} · {1}/{2}行" },
        ["long.grade"]       = new[] { "판정 — {0}", "Grade — {0}", "判定 — {0}" },
        ["long.retry"]       = new[] { "다시 — 넣기(Enter) · 글 고르기 — Esc",
                                       "Again — Enter · Pick text — Esc", "もう一度 — Enter · 選ぶ — Esc" },
        ["grade.s"]          = new[] { "특급", "Special", "特級" },
        ["grade.1"]          = new[] { "1급", "Grade 1", "1級" },
        ["grade.2"]          = new[] { "2급", "Grade 2", "2級" },
        ["grade.3"]          = new[] { "3급", "Grade 3", "3級" },
        ["grade.4"]          = new[] { "4급", "Grade 4", "4級" },
        ["grade.none"]       = new[] { "급외", "Ungraded", "級外" },

        ["list.long"]        = new[] { "긴글련습", "Long texts", "長文練習" },
        ["list.test"]        = new[] { "타자검정", "Typing test", "タイピング検定" },
        ["list.longSub"]     = new[] { "글 하나를 골라 처음부터 끝까지 따라 칩니다.",
                                       "Pick a text and type it start to finish.",
                                       "文を選んで最初から最後まで打ちます。" },
        ["list.testSub"]     = new[] { "글 하나를 골라 처음부터 끝까지 치면 속도와 정확도를 재여 급수를 매깁니다.",
                                       "Type a full text to get a speed/accuracy grade.",
                                       "全文を打つと速度と正確度で級が付きます。" },
        ["list.pick"]        = new[] { "글 고르기", "Choose a text", "文を選ぶ" },
        ["list.import"]      = new[] { "불러오기 (.txt)", "Import (.txt)", "読み込み (.txt)" },
        ["list.builtin"]     = new[] { "내장", "built-in", "内蔵" },
        ["list.imported"]    = new[] { "불러온 글", "imported", "読み込み" },
        ["list.empty"]       = new[] { "열수 있는 글이 없습니다 — 아래 《불러오기》로 .txt를 넣어보십시오.",
                                       "No texts — use Import below to add a .txt.",
                                       "文がありません — 下の読み込みで.txtを追加。" },

        ["rain.hud"]         = new[] { "점수 {0} · 단계 {1} · 목숨 {2} · 최고 {3}",
                                       "Score {0} · Level {1} · Lives {2} · Best {3}",
                                       "得点 {0} · 段階 {1} · 命 {2} · 最高 {3}" },
        ["rain.level"]       = new[] { "단계 {0}", "Level {0}", "段階 {0}" },
        ["rain.over"]        = new[] { "끝!", "Game Over", "終わり！" },
        ["rain.overDetail"]  = new[] { "점수 {0} · 단계 {1}", "Score {0} · Level {1}", "得点 {0} · 段階 {1}" },
        ["rain.best"]        = new[] { " — 최고기록 갱신!", " — New best!", " — 最高記録更新！" },
        ["rain.prevBest"]    = new[] { " · 최고 {0}", " · Best {0}", " · 最高 {0}" },
        ["rain.again"]       = new[] { "다시 — 넣기(Enter) · 시작화면 — Esc",
                                       "Again — Enter · Start — Esc", "もう一度 — Enter · 開始 — Esc" },
        ["rain.empty"]       = new[] { "낱말이 아직 없습니다", "No words yet", "単語がまだありません" },
        ["rain.emptyDetail"] = new[] { "낱말을 넣으면 산성비를 즐길수 있습니다.",
                                       "Add words to play Acid Rain.", "単語を追加すると遊べます。" },

        ["err.title"]        = new[] { "열기오유", "Open error", "オープンエラー" },
        ["err.skip"]         = new[] { "어긋난 모듈은 열지 않고 건너뜁니다.",
                                       "Corrupt modules are skipped.", "壊れたモジュールは飛ばします。" },
        ["imp.title"]        = new[] { "긴글 불러오기", "Import text", "文の読み込み" },
        ["imp.name"]         = new[] { "제목", "Title", "題名" },
        ["imp.source"]       = new[] { "출처", "Source", "出典" },
        ["imp.cancel"]       = new[] { "그만두기", "Cancel", "やめる" },
        ["imp.save"]         = new[] { "저장", "Save", "保存" },
        ["imp.filter"]       = new[] { "글 화일 (*.txt)|*.txt", "Text files (*.txt)|*.txt", "テキスト (*.txt)|*.txt" },
        ["imp.default"]      = new[] { "사용자 불러오기", "user import", "利用者読み込み" },
    };
}
