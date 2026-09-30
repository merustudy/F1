# Stage 06 — 언어

Snapshot: 2026-09-30 (완료)

## Goal

화면의 문구가 두 Locale(`ko-KR`, `en-US`)로 나오고, 한글이 깨지지 않고, 언어를 바꾸면 저장된다.

## Done

- Package: `com.unity.localization` 1.5.13.
- UI 문구: `Assets/@Localization/Source/UI_StaticText.csv`가 Source. `F1.Editor.Setup.LocalizationSetup.Sync`가 설정, Locale Asset,
  `F1_UI_Static` Collection을 CSV와 똑같이 만든다. 검증은 `UiStringSource`(Key 형식, 중복, 빈 값, Placeholder).
- 코드에서 쓰는 법: Key는 `F1.UI.UiKeys` 상수, 문구는 `F1.UI.UiStrings.Get(key, args)`. `{`가 든 문구는 Smart String.
- Locale: `F1.Core.UnityLocaleAdapter`가 Unity Localization을 만지는 유일한 Runtime 코드. `SettingManager.ChangeLocaleAsync`가
  적용 -> 저장 -> `LocaleChanged` 순서로 바꾸고, 적용이나 저장이 실패하면 이전 Locale로 되돌린다.
- Boot: BOOT-07(초기화, Locale Asset이 `LocalePolicy`와 같은지 확인), BOOT-08(저장된 Locale 적용).
- Font: `Assets/@Fonts/Source/Pretendard`(ttf, License, 출처와 SHA-256). `F1.Editor.Setup.FontSetup.Sync`가
  `Assets/@Fonts/TMP/Pretendard-Medium SDF.asset`을 굽는다. Static Atlas, 글자 집합이 같으면 건드리지 않는다.
- 체인의 setup 순서: Player 설정 -> Scene -> Data 변환 -> UI String Table -> Font Atlas -> Addressables.
- Owner 문서: `Docs/Architecture/06_LOCALIZATION.md` (CLAUDE.md §2, §8에 등록).

## 결정 (권장안, 일괄 승인 범위)

- Font는 Pretendard Medium 한 벌. Latin과 한글을 한 Family가 가진다. License는 SIL OFL 1.1.
- UI String CSV에 `Id` 열이 없다. Key로만 참조한다.
- Atlas에는 화면에 나올 수 있는 글자만 굽는다(ASCII, UI CSV 전체, Static Data CSV 전체). Dynamic Atlas를 쓰지 않는다.
  Play 중에 Font Asset이 바뀌지 않으므로 Git에 뜻하지 않은 변경이 생기지 않는다.
- Font Asset은 일반 Git에 둔다(LFS 아님). ttf는 LFS다.
- String Table은 선택한 Locale과 Fallback을 Preload한다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 327/327, PlayMode 19/19.
- setup을 연달아 두 번 돌려 `Assets`, `ProjectSettings`, `Packages`의 파일 Hash가 같음을 확인했다(멱등).
- Test를 돌린 뒤에도 생성 Asset이 바뀌지 않았다.

## 알아둘 것

- 문구를 고치면 `Tools/chain.sh setup`을 돌린다. CSV, Table Asset, Font Asset이 함께 바뀌고 같은 Commit에 넣는다.
- 새 글자가 Pretendard에 없으면 setup이 그 글자를 알려 주고 실패한다.
- Atlas는 1024x1024 한 장에서 시작한다. 글자가 넘치면 TMP가 Atlas를 한 장 더 만든다.
- Localization Package가 Addressables Group(`Localization-*`)과 Label을 스스로 만든다. `AddressablesSetup`은 `F1-*` Group만 관리한다.
- Player Build에서는 Addressables Content Build가 먼저 필요하다. Build 단계는 아직 만들지 않았다.
