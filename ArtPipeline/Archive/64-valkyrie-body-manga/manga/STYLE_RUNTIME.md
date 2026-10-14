# F1 Style Runtime — 시험: manga (실사 쪽 + 체인소맨 만화풍을 강하게 섞음)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다.
사용자 지시 "그림체 섞음(실사랑 더 가깝게, 만화 체인소맨 스타일 강하게)"를 이렇게 읽었다: 비율과 해부는 실사에 가깝게(약 7등신, 진짜 사람의 얼굴과 몸),
손은 『체인소맨』(후지모토 타츠키) 원작 만화의 펜 선 쪽으로 강하게. 그 만화의 그림은 붙이지 않고 특징만 글로 적는다(프롬프트에 작품·작가 이름을 넣지 않는다).

| 지키는 것 (우리) | 가져오는 것 |
|---|---|
| 발키리의 정체: 금발 곱슬과 땋은 가닥, 주근깨, 붉은 털 망토, 크림 상의, 남색 치마, 털 장화와 가죽 팔목, 외날 도끼, 오른쪽을 보는 전투 준비 자세 | 실사 쪽: 약 7등신, 사실적인 해부와 손발, 실제 여성의 얼굴 |
| 낮은 채도의 평면 색, 웃지 않는 얼굴, 어두운 외곽선, 투명 배경 | 체인소맨 원작 쪽: 거칠고 긁힌 듯한 펜 선, 고르지 않은 굵기와 끊김, 빠른 해칭, 굵은 먹 덩어리(스폿 블랙), 무표정하고 지친 눈(작은 홍채, 무거운 눈꺼풀) |

지금 그림을 기준 그림으로 붙이되 Round 63의 교훈(기준 그림이 그리는 방식까지 끌고 감)으로 §3을 반대로 적었다: **누구인지만 가져오고, 그리는 방식은 버린다.**

돌리는 법 (저장소 루트에서):

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key valkyrie --name valkyrie_manga --size 1024x1536 \
  --style ArtPipeline/Archive/64-valkyrie-body-manga/manga/STYLE_RUNTIME.md \
  --roster ArtPipeline/Archive/64-valkyrie-body-manga/manga/character.csv \
  --reference ArtPipeline/output/character/valkyrie.raw.png
```

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: a gritty, semi-realistic seinen manga illustration of a fantasy
mercenary, much closer to realism than to cartoon: realistic human anatomy and
proportions, about seven heads tall, a real woman's face and body, drawn in the
raw hand of a modern dark action manga.
Linework: raw, scratchy pen-and-ink lines with an uneven, nervous weight: thin
searching strokes on the forms, thicker hooked strokes on the contour, small
breaks and overshooting line ends, quick loose hatching in the shadows. A dark
ink outline closes the figure. It looks drawn fast with a dip pen, never traced,
never vector-clean.
Blacks: bold, solid black areas (spotted blacks) for the deepest shadows, the
gaps in the hair, the inside of the cloak, the boots and the leather.
Color: muted, desaturated colors laid flat under the ink like a printed color
manga page: dusty blonde, dried-blood crimson, bone white, faded navy, dark
leather brown, cold grey steel, a natural skin tone. No bright or saturated
colors.
Shading: the ink does the shading (hatching and solid blacks) with at most one
flat darker tone of color. No gradients, no airbrush, no glossy highlights.
Background: fully transparent. No scenery, no ground, no shadow under the feet,
no frame, no panel borders, no speed lines, no screentone background, no sparks.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: photograph, photorealistic photo, 3D render, CGI, painterly, watercolor,
airbrush, soft gradients, glossy highlights, neon, bright saturated colors,
pastel, big anime eyes, sparkles in the eyes, chibi, cute, cartoon mascot,
rubbery cartoon proportions, big head, short legs, clean vector line, uniform
line weight, smiling, grinning, blood, gore, wounds, nudity, see-through
clothing, underwear, speech bubbles, sound-effect lettering, screentone dots,
panel borders, drop shadow, ground shadow, scenery, background, light rays,
motion lines, frame, text, letters, numbers, logo, title, signature, watermark,
cropped figure, extra characters, two figures, a crowd
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image is the current cartoon figure of this character in our
game. Use it only for who she is: her long wild wavy blonde hair with braids,
her freckles, the dark red fur-trimmed cloak, the cream laced top, the short
torn navy skirt, the leather bracers, the fur-topped boots, the large
single-bladed battle axe held in both hands, and the braced attack-ready stance
facing and looking toward the viewer's right. Do NOT copy how it is drawn: not
its rubbery cartoon proportions, not its big head and short legs, not its
cartoon face, not its smooth even line, not its simple flat look. Redraw her
completely in the style above, as a real woman with realistic anatomy in a
gritty pen-and-ink manga hand. Do not reproduce the transparent or white
background; draw her alone on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in the stance the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: realistic, about seven heads tall, with a real head size; a strong, curvy, athletic woman's body with realistic muscle and soft shapes; hands and feet at real size.
- Face: realistic and attractive, with a deadpan, tired, dangerous stare: smaller irises with dark pupils, heavy upper lids, thin straight brows, a straight nose drawn with one or two lines, lips closed in a flat line. Freckles as a few small ink dots. Not a big-eyed anime face, not a caricature.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the irises sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- Hair: many loose, wild ink strands; the clumps in shadow filled with solid black.
- Clothes and gear of worn, real materials: fur drawn with quick scratchy strokes, leather with creases and stitches, the axe a heavy chipped iron blade on a leather-wrapped haft.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers, a signature or logos. Output a transparent PNG with all four corner pixels at alpha 0.
