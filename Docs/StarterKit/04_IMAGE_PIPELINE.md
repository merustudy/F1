# 04. Image Generation Pipeline

C1 `ArtPipeline/`의 구조다. OpenAI 이미지 API로 투명 배경 2D 스프라이트를 만들고, 사람이 승인한 것만
Unity에 배선한다. 스크립트 원본은 [`tools/gen_image.py`](tools/gen_image.py)(C1 `test_imagegen.py` 복사본)다.

## 구성

```text
ArtPipeline
├─ STYLE_GUIDE.md          # 그림체의 전체 근거·예외·결정 기록 (사람용, 충돌 시 이쪽이 우선)
├─ STYLE_RUNTIME.md        # 스크립트가 실행 시 파싱하는 경량 규칙 (번호 섹션)
├─ References/<Type>/*.png # 타입별 스타일 레퍼런스 시트 (요청당 한 장만 첨부)
├─ Rosters/<category>.csv  # Id,Key,Type,Subject — 무엇을 그릴지
├─ Archive/<round>/        # README.md + samples/ — 라운드별 후보·결정·폐기 스타일
├─ tools/gen_image.py
└─ output/                 # 생성 직후 산출물 (gitignore)
```

## 환경

```bash
python3 -m venv .venv
```

```bash
.venv/bin/pip install openai pillow numpy
```

C1 기준: Python 3.9, `openai` 2.48, `pillow` 11.3, `numpy` 2.0 (numpy는 로컬 목업·측정용).

### API 키: macOS 로그인 키체인

```bash
security add-generic-password -s OPENAI_API_KEY -a "$USER" -w
```

(값은 프롬프트에 직접 입력한다. 이 명령은 사용자가 직접 실행한다.)

스크립트는 `security find-generic-password -s OPENAI_API_KEY -a <user> -w`로 읽는다.

- `os.environ`을 쓰지 않는다: export된 키는 모든 자식 프로세스, 셸 History, Crash Dump로 샌다.
- 키는 Client 생성자에만 넘기고 출력·로그·예외 메시지에 넣지 않는다.
- `.env`는 쓰지 않더라도 `.gitignore`에 넣어 둔다.

## 호출 형태

```python
client.images.edit(
    model="gpt-image-2",
    image=[reference_upload],      # 스타일 레퍼런스 1장
    prompt=prompt,
    size="1024x1024",
    quality="medium",              # low / medium / high
    background="transparent",
)
# response.data[0].b64_json -> PNG bytes
```

- `images.generate`가 아니라 **`images.edit` + 레퍼런스 이미지**다. 레퍼런스가 그림체 일관성의 대부분을 만든다.
- **실행 1회 = API 호출 1회.** 비용이 호출 단위라서 스크립트가 배치를 돌지 않는다. 배치는 밖에서 돌린다(아래).
- 레퍼런스는 확장자를 믿지 않고 Magic Byte로 PNG/JPEG/WEBP를 판별해 MIME을 붙인다(`.png` 이름에 JPEG가 든 적이 있고, API 오류 메시지가 원인을 가리키지 않는다).

## 프롬프트 조립: 문서가 Source

스크립트에 스타일 문구를 복사하지 않는다. `STYLE_RUNTIME.md`를 실행 시 읽어 조립한다.

```text
§9 Quick Shared Style Rule (```text 블록, 그대로)
+ "Subject: <subject>. <extra> <타입별 tail 문장>"
+ "Composition: <타입 섹션의 '- ' 불릿을 한 줄로>"
+ §8 Forbidden의 AVOID ```text 블록
+ 레퍼런스 문구 (스타일만 빌리고 내용은 복사하지 말 것)
```

`STYLE_RUNTIME.md`가 지켜야 하는 **형식 계약**(스크립트의 `style_sections`/`fenced_block`/`bullet_rules`):

- 섹션 제목은 `## <번호>. <제목>`.
- Quick Rule 섹션과 Forbidden 섹션에는 ```` ```text ```` 블록이 하나씩 있다.
- 타입 섹션은 `- ` 불릿으로 규칙을 쓴다.
- 필요한 섹션이 비면 **호출 전에** 실패한다.

C1의 섹션 배치(그대로 쓸 필요는 없고 `TYPES`의 `section` 번호만 맞추면 된다): 1 Core / 2 Shape / 3 Outline /
4 Palette / 5 Shading / 6 Item / 7 Character / 8 Forbidden / 9 Quick Rule / 10 UI / 11 Monster.
예시는 [`tools/STYLE_RUNTIME.example.md`](tools/STYLE_RUNTIME.example.md).

### 타입 테이블

```python
TYPES = {
    "<type>": {
        "section": <STYLE_RUNTIME 섹션 번호>,
        "reference": <레퍼런스 경로>,
        "quality": "medium",
        "tail": "Draw it as a single <...>.",
        "fit": "symbol" | "icon" | "trim",
    },
}
```

새 프로젝트에서는 실제 필요한 타입만 정의한다. 예: `character`, `item`, `enemy`, `tile`, `ui_panel`, `background`.
배경처럼 불투명·비정사각 Asset은 `background="transparent"`와 `size`가 다르므로 타입별 옵션으로 빼야 한다
(C1 스크립트는 `SIZE`가 하나로 고정돼 있다).

## 후처리 (모델과 다투지 말고 산술로 강제)

모델은 "캔버스의 70~80%"를 지키지 않는다(첫 실행 94%). 다시 호출하는 대신 Pillow로 맞춘다.

| fit | 동작 | 용도 |
|---|---|---|
| `symbol` | 피사체 긴 변을 캔버스 78%로 **축소만** 하고 중앙 재배치 (여백 ≥ 10%) | 격자·칸에 들어가는 스프라이트 |
| `icon` | 같은 방식, 88% | 단독 아이콘 |
| `trim` | 잉크 경계로 자르기만 | nine-slice 패널 (여백이 있으면 늘어난다) |

- 피사체 경계는 **Alpha만으로** 잰다(`alpha > 8`). `Image.getbbox()`는 투명 픽셀 밑의 어두운 RGB를 내용으로 봐서 항상 캔버스 전체를 돌려준다.
- 확대하지 않는다. 작은 피사체를 키우면 외곽선이 뭉개진다.

저장 후 `verify_output`이 PNG 크기/ColorType, 피사체 비율, 네 변 여백, 중심 이탈(3% 이내), 반투명 비율(8% 미만이면 글로우 없음)을 출력한다.
"고유 색 수"는 참고값일 뿐 판정에 쓰지 않는다(안티앨리어싱 때문에 평면 채색도 수만 가지다).

## 사용

```bash
.venv/bin/python ArtPipeline/tools/gen_image.py --type item_symbol --name potion_red --subject "a round red potion bottle with a cork"
```

| 옵션 | 뜻 |
|---|---|
| `--type` | 타입 (섹션·레퍼런스·quality·fit 선택). **필수로 생각한다** |
| `--subject` | 영어, 명사 중심, 3~12 단어. 어떻게 보이는지는 스타일 문서가 맡는다 |
| `--name` | `output/<name>.png` |
| `--extra` | Subject 뒤에 붙는 제약 한 문장 (예: "no face, no white highlight at all") |
| `--reference <png>` | 기본 레퍼런스 대신 다른 이미지 |
| `--copy-reference` | 레퍼런스를 **그대로** 다시 그리기 (기존 승인 그림의 미세 수정용. 자기 프로젝트 그림에만) |
| `--quality` | `low`로 덮어써 비용 절감 |

zsh 주의: `GEN="python script.py"`처럼 공백 든 변수 하나로 묶으면 단어 분리가 안 돼 exit 127이다. `PYBIN`과 `SCRIPT`를 따로 둔다.

### 배치

Roster CSV를 읽어 한 줄씩 위 스크립트를 부르는 작은 러너를 쓴다(C1은 scratchpad에 그때그때 작성).

- 동시 4개 정도는 문제없었다. 아이콘 하나 약 30초(medium).
- Rate limit을 피하려면 "4개 / 65초" 식으로 간격을 둔다.
- 가끔 API가 빈 결과를 돌려준다(진행 로그는 찍히고 파일이 없음). 러너는 **파일 존재**로 성공을 판정하고, 빠진 것만 같은 Subject로 다시 돌린다.
- 중단할 때는 러너와 자식 프로세스를 함께 죽인다(`pkill -f gen_image.py`). 안 그러면 호출이 계속 나간다.

## Roster

```csv
SymbolId,Key,Type,Subject
1,sword,item_symbol,a short steel sword with a brown grip and a gold crossguard
2,bow,item_symbol,a curved wooden bow with a taut string
```

- Subject 문구를 Git에 남겨 재생성 요청("3번 다시, 더 둥글게")을 같은 문구 + 수정으로 재현한다.
- `Key`는 Game Data CSV의 `Key`와 같다. 파일명과 Address가 여기서 나온다.

## 승인 라운드 (C1에서 굳어진 절차)

```text
1. 소재 확정: 애매하면 번호 매긴 소재 후보를 먼저 제시 (호출 전, 무료)
2. 생성: 항목당 1회 (필요하면 후보 A/B/C)
3. 리뷰 시트: Pillow로 한 장에 [현재 / 후보 원본 / 실제 표시 크기(예: 128px, 66px) on 게임 배경색]
4. 판정: 항목별 "N 확정", "N 재생성 - 방향", "N 소재 교체"
5. 재생성은 요청된 항목만 1회씩. 회전·자르기·색 교체는 로컬 Pillow로 (API 호출 없이)
6. 승인분만 배선 -> 검증 체인 1회 (파일마다 돌리지 않는다)
7. Archive/<round>/README.md: 후보, 결정, 받아들여진 --extra 문구, 거절 사유
```

- **판정 없이 크레딧을 쓰지 않는다.** 요청받지 않은 이미지 생성을 하지 않는다.
- 실제 표시 크기로 줄여 보는 칸이 중요하다. 1024px에서 좋아 보여도 64px에서 뭉개진다.
- 받아들여진 `--extra` 문구와 거절 사유(예: 흰 하이라이트 3회 거절)를 기록해 두면 다음 라운드의 재생성이 크게 준다.
- 기존 Asset과 **모티프가 겹치지 않는지** 호출 전에 확인한다.
- 색만 다른 세트(동/은/금)는 마스터 한 장만 생성하고 나머지는 로컬 재채색 스크립트로 만든다(C1 `recolour_tokens.py`).
- UI 배치 검토는 이미지 API가 아니라 Prefab rect를 읽어 Pillow로 그린 목업으로 한다.

## Unity 배선

승인된 그림 한 장이 들어가는 길은 항상 같은 세 곳이다.

```text
1. PNG  -> Assets/@Art/<Domain>/<Category>/<key>.png
2. CSV  -> <Definition>Data.csv 의 IconResourceKey = "<domain>/<category>/<kebab-key>"
3. Entry-> Editor/Data/AddressablesSetup.cs 의 <Category>Entries 에
           ("Assets/@Art/.../<key>.png", "<domain>/<category>/<kebab-key>", Scope<App|Main|Run>) 한 줄
```

그 다음 체인: `Transform -> Addressables Sync -> EditMode -> PlayMode` (06 참고).

`AddressablesSetup.Sync`가 하는 일(멱등):

- Entry 등록/이동, Address 지정, **Scope Label 정확히 하나**.
- Sprite Import 정책 강제(`SlotArtImporter`): Sprite, `alphaIsTransparency`, Mipmap/Filter/Compression/MaxSize 고정. 정책과 다르면 Force Reimport.
- 검증(`FindArtProblems`, Test에서도 호출): CSV의 모든 Art Key가 등록돼 있고 Label·Import 정책이 맞는가, **아무도 가리키지 않는 등록 Entry가 없는가**.

Scope는 "언제 화면에 필요한가"로 정한다. 로비에서 보이는 그림은 `scope-main`, 판 안에서만 보이면 `scope-run`.

> 작은 크기로 그리는 스프라이트는 Mipmap을 켜야 계단이 안 생기고, Tight Mesh 스프라이트를 UI Image에서
> 셰이더로 움직이면 잘린다(C1에서 실제로 겪었다). Import 정책을 코드 한 곳에 두고 Test로 고정한다.

## 스타일 문서 운영

- 운영 문서는 `STYLE_GUIDE.md`와 `STYLE_RUNTIME.md` **하나씩**. 그림체를 바꿀 때는 이전본을 `Archive/<style-name>/`로 옮기고 새로 쓴다.
- 그림체를 단계적으로 밀 때(C1: 손그림 느낌 50 → 75 → 90 → 100%)는 단계마다 규칙 원문 + 샘플을 Archive에 남겨 되돌아올 수 있게 한다.
- 팔레트 표(Base/Shadow/Light hex)를 Runtime 문서에 둔다. 로컬 후처리·UI 코드도 같은 hex를 쓴다.
- 작가·스튜디오·작품 이름, "in the style of"를 프롬프트에 넣지 않는다.
