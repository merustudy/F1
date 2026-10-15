# Round 65 — 발키리: 지금 그림체에 체인소맨 만화풍을 20·40·60·80% (2026-10-14, 중단)

시험 라운드다. **지금의 그림체와 게임의 그림은 바꾸지 않는다**(G19 그대로).

## 지시

사용자: "현재 발키리 그림체에 만화 체인소맨 그림체를 섞어서 20% 40% 60% 80% 테스트 이미지 생성".

## 방법

- 기준 그림: 지금 게임의 발키리(모래시계 체형, `output/character/valkyrie.raw.png`). 소재 `character.csv`(체형은 `Body` 열 대신 글로: 저장한 `hourglass`의 "4.5등신"이 높은 단계와 어긋나서).
- 비율은 모델이 숫자로 알아듣지 못하므로 축마다 단계를 글로 적었다(세트 `csm20/`·`csm40/`·`csm60/`·`csm80/`의 `STYLE_RUNTIME.md`):

| 축 | 20% | 40% | 60% | 80% |
|---|---|---|---|---|
| 비율 | 약 5등신, 큰 머리 | 약 5.5등신 | 약 6.5등신, 거의 실제 | 약 7등신, 실제 |
| 선 | 우리 외곽선, 조금 거칠게 | 펜 선, 해칭 | 거친 펜 선, 성긴 해칭 | 거친 펜, 외곽선만 우리 것 |
| 먹 | 작은 검정 한둘 | 군데군데 | 굵은 먹 덩어리 | 큰 먹 덩어리 |
| 얼굴 | 우리 얼굴, 눈꺼풀만 무겁게 | 중간 | 반실사, 무표정 | 실사, 무표정하고 지친 눈 |
| §3 기준 그림 규칙 | 거의 다 지킴 | 반 남짓 지킴 | 정체와 외곽선·색만 | 정체와 외곽선·색만 |

- Round 64 섞음이 "촘촘하고 정돈된 판타지 펜화"로 나온 것을 받아 60·80%는 선을 거칠고 성기게, 해칭보다 먹 덩어리로 적었다. 그 만화의 그림은 붙이지 않고 작품·작가 이름을 프롬프트에 넣지 않았다.
- 크기: 20·40%는 1024x1024, 60·80%는 1024x1536(키가 커서).

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key valkyrie --name valkyrie_csmNN --size <크기> --style ArtPipeline/Archive/65-valkyrie-csm-mix/csmNN/STYLE_RUNTIME.md --roster ArtPipeline/Archive/65-valkyrie-csm-mix/character.csv --reference ArtPipeline/output/character/valkyrie.raw.png
```

호출 4회, 약 $0.105(그 뒤 그림 누계 251회, 약 $6.55). 원본과 맞춘 그림은 `csmNN/`에.

## 상태

생성 뒤 사용자가 다른 요청(여성 캐릭터 목록 → Round 66 대마법사)으로 넘어가 **비교 목업과 판정은 만들지 않았다.** 다시 보려면 네 장(`csmNN/valkyrie_csmNN.png`)과 지금 발키리를 같은 높이·광산 무대로 놓는 목업을 만든다(Round 64 `mock_compare.py`의 길).
