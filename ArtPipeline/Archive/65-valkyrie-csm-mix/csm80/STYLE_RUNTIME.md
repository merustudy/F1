# F1 Style Runtime — 시험: csm80 (지금 그림체 20% + 체인소맨 만화풍 80%)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. Round 65의 네 단계(20·40·60·80%) 가운데 넷째.
비율은 모델이 숫자로 알아듣지 못하므로 축마다(비율·선·먹·얼굴·색·명암·기준 그림 규칙) 어디쯤인지를 글로 적었다. 축의 표는 `../README.md` "방법".
그 만화의 그림은 붙이지 않고 특징만 글로 적는다(프롬프트에 작품·작가 이름을 넣지 않는다).
Round 64의 섞음(`Archive/64-valkyrie-body-manga/manga/`)을 바탕으로, 그것이 "촘촘하고 정돈된 판타지 펜화"로 나온 것을 받아 선은 거칠고 빠르고 성기게, 해칭보다 먹 덩어리로 적었다.
우리 쪽에서 남기는 것은 형체를 닫는 어두운 외곽선과 낮은 채도의 평면 색뿐이다.

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: a gritty, semi-realistic modern dark-action manga illustration of a
fantasy mercenary, with only a trace of our flat cartoon art left (the dark
closing outline and the flat muted colors): realistic human anatomy and
proportions, about seven heads tall, a real woman's face and body, drawn fast
in a raw manga pen.
Linework: raw, scratchy pen-and-ink lines with an uneven, nervous weight: thin
searching strokes inside the forms, thicker hooked strokes on the contour,
small breaks and overshooting line ends. A dark ink outline closes the figure.
Drawn fast and loose with a dip pen, with few lines: never traced, never
vector-clean, never neat or densely rendered.
Blacks: big, bold, solid black areas (spotted blacks) do most of the shadow:
the gaps in the hair, the inside of the cloak, the boots and the leather. Only
a little quick, loose hatching, never fine cross-hatching.
Color: muted, desaturated colors laid flat under the ink like a printed color
manga page: dusty blonde, dried-blood crimson, bone white, faded navy, dark
leather brown, cold grey steel, a natural skin tone. No bright or saturated
colors.
Shading: the ink does the shading (black areas and a little hatching) with at
most one flat darker tone of color. No gradients, no airbrush, no glossy
highlights.
Background: fully transparent. No scenery, no ground, no shadow under the feet,
no frame, no panel borders, no speed lines, no screentone background, no
sparks.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: photograph, photorealistic photo, 3D render, CGI, painterly, watercolor,
airbrush, soft gradients, glossy highlights, neon, bright saturated colors,
pastel, big anime eyes, sparkles in the eyes, chibi, cute, cartoon mascot,
rubbery cartoon proportions, big head, short legs, neat finely rendered
fantasy illustration, dense fine cross-hatching, clean vector line, uniform
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
navy skirt, the leather bracers, the fur-topped boots, the large single-bladed
battle axe held in both hands, her hourglass curves and the braced
attack-ready stance facing and looking toward the viewer's right. Of the way it
is drawn, keep only the dark outline that closes the figure and the flat muted
colors. Do NOT copy its rubbery cartoon proportions, its big head and short
legs, its cartoon face or its smooth, even line. Redraw her completely in the
style above, as a real woman with realistic anatomy in a raw manga pen, alone.
Do not reproduce the transparent or white background; draw her on a
transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in the stance the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: realistic, about seven heads tall, with a real head size; the hourglass curves the subject says on a strong woman's body with realistic muscle and soft shapes; hands and feet at real size.
- Face: realistic and attractive, with a deadpan, tired, dangerous stare: smaller irises with dark pupils, heavy upper eyelids, thin straight eyebrows, a straight nose drawn with one or two lines, lips closed in a flat line, freckles as a few small ink dots. Not a big-eyed anime face, not a caricature.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the irises sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: many loose, wild ink strands; the clumps in shadow filled with solid black.
- Clothes and gear of worn, real materials, drawn with few lines: fur in quick scratchy strokes, leather with a few creases and stitches, the axe a heavy chipped iron blade on a leather-wrapped haft.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers, a signature or logos. Output a transparent PNG with all four corner pixels at alpha 0.
