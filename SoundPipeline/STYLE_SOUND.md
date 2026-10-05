# Sound Style (runtime)

생성 스크립트가 실행할 때 읽는 소리 스타일 문서다. 방향과 결정은 `Docs/Design/12_Sound_Direction.md`(G11: 어두운 카툰), 규칙은 `Docs/Architecture/14_SOUND.md`가 가진다.
스크립트가 파싱한다: `## <번호>. <제목>` 아래의 ```` ```text ```` 블록 하나, 또는 `- ` 불릿. 게임·스튜디오·작곡가의 이름과 "in the style of"를 넣지 않는다.

## 1. Effect tail

효과음 문구의 꼬리. 소재(재료 + 동작 + 질감) 뒤에 붙고, 그 뒤에 길이 힌트가 붙는다.

```text
dark hand-drawn cartoon fantasy dungeon game, real materials of iron, wood, leather, bone and stone, weighty and dry, slightly exaggerated impact, short tight room, no reverb tail, no music
```

## 2. Sting tail

승리와 패배의 짧은 악구에 붙는 꼬리. 효과음 꼬리의 "no music" 대신 쓴다.

```text
dark hand-drawn cartoon fantasy dungeon game, small dark acoustic ensemble, minor key, dry intimate room, no vocals, ends cleanly
```

## 3. Music positive

배경음의 공통 positive. 곡마다의 positive(BPM은 숫자로) 뒤에 붙는다.

- instrumental only
- small dark chamber folk ensemble
- minor key
- loopable steady groove
- no build-up
- no ending
- dry intimate room production

## 4. Music negative

배경음의 공통 negative. 밝은 팔레트와 큰 편성, 끝의 페이드아웃을 뺀다.

- vocals
- singing
- choir
- cheerful
- major key
- bright and bouncy
- children's toy music
- ukulele
- glockenspiel
- epic orchestral swell
- big cinematic drums
- electronic dance
- synth lead
- 8-bit chiptune
- fade out
