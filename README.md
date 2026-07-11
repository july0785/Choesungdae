<p align="center">
  <img src="ChosonTyping/Assets/logo-black.png#gh-light-mode-only" width="280" alt="최승대">
  <img src="ChosonTyping/Assets/logo-white.png#gh-dark-mode-only" width="280" alt="최승대">
</p>

<p align="center">조선어(문화어) 타자련습 프로그람</p>

<p align="center">
  <b><a href="../../releases/latest">⬇ 내려받기 (윈도우, 포터블)</a></b>
</p>

---

## 소개

**최승대**는 국규건반(KPS 9256)을 비롯한 조선식 건반배렬로 타자를 익히는 윈도우용 련습 프로그람입니다. 글자조합(모아쓰기)을 윈도우 IME에 맡기지 않고 직접 하기 때문에, 어느 글쇠를 눌렀는지 정확히 알고 자리를 짚어줍니다.

- **건반배렬 3종** — 국규건반(KPS 9256) · 창덕건반(겹모음 통짜 글쇠) · 두벌식 표준. 배렬은 `data\layouts\`의 JSON이라 얼마든지 더 넣을수 있습니다.
- **련습단계 6개** — 자리련습 · 낱말련습 · 짧은글련습 · 긴글련습 · 타자검정(급수 판정) · 산성비
- **조선 어휘로 배우기** — 낱말·문장이 문화어라서 타자와 함께 어휘도 익혀집니다.
- **밝은/어두운 화면형식** — 계통 설정을 따라가고, 창머리의 ◐ 단추로 바꿀수 있습니다.
- **포터블** — 설치 없이 폴더째 들고다니면 됩니다. 설정·콘텐츠 전부 프로그람 옆 `data\` 안에서 끝나고, 등기부(레지스트리)를 건드리지 않습니다.

## 쓰는 법

1. [Releases](../../releases/latest)에서 압축을 받아 아무 데나 풉니다.
2. `Choseungdae.exe`를 실행합니다.
3. 건반배렬과 련습단계를 골라 시작합니다.

긴글련습·타자검정에는 자기 글(.txt)을 불러올수 있습니다. 불러온 글은 `data\imported\`에 전용형식(.ctp)으로 잠겨 저장되고, 다시 열 때마다 무결성을 검사합니다.

## 측정 규칙

- **타속(타/분)** — 자모 한번이 1타. 지웠다 다시 친 글쇠까지 모두 세는 총타수입니다.
- **정확도(%)** — 고친 뒤 마지막 상태로 판정합니다. 틀렸다가 고쳤으면 맞은것으로 칩니다.
- 틀려도 막지 않습니다. 색으로만 알립니다.

## 만들기 (개발자용)

.NET 9 SDK가 있으면 됩니다.

```
dotnet test                # 단위시험
dotnet run --project ChosonTyping
dotnet publish ChosonTyping -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\최승대
```

건반배렬을 추가하려면 `ChosonTyping\data\layouts\`에 JSON을 하나 더 만들면 됩니다. 콘텐츠 모듈(낱말·문장·긴글)은 sha256 해시로 잠겨 있으며, 고친 뒤에는 `tools\Update-ModuleHash.ps1`로 다시 잠급니다.

## 라이선스

[GNU LGPL v3](COPYING.LESSER). 한컴타자연습의 코드·그림·소리·글자체는 일절 쓰지 않았으며, 구성만 참고했습니다.
