# Pretendard

UI Font. Latin과 한글을 한 Family가 모두 가진다.

- 파일: `Pretendard-Medium.ttf`
- 출처: https://github.com/orioncactus/pretendard (v1.3.9), `packages/pretendard/dist/public/static/alternative/Pretendard-Medium.ttf`
- License: SIL Open Font License 1.1 (`LICENSE.txt`). 게임에 포함해 배포할 수 있다. Font 파일 자체를 고쳐서 같은 이름으로 배포하지 않는다.
- SHA-256: `3bae579377eb8e9ac412cb4809ebc3de1d956ed75995c1e346f0c1311053f4e2`

## TMP Font Asset

`Assets/@Fonts/TMP/Pretendard-Medium SDF.asset`은 이 파일에서 만든 생성물이다. 손으로 고치지 않는다.

- 만드는 코드: `F1.Editor.Setup.FontSetup` (메뉴 `F1/Setup/Sync Font Asset`, 체인의 setup 단계가 부른다).
- Atlas에는 화면에 나올 수 있는 글자만 굽는다: ASCII, `UI_StaticText.csv`의 모든 문구, Static Data의 모든 이름.
  문구를 바꾸면 setup이 Atlas를 다시 굽는다.
- 규칙: `Docs/Architecture/06_LOCALIZATION.md` "Font".
