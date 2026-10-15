# F1 Style Runtime — 시험: csm20 (지금 그림체 80% + 체인소맨 만화풍 20%)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. Round 65의 네 단계(20·40·60·80%) 가운데 첫째.
비율은 모델이 숫자로 알아듣지 못하므로 축마다(비율·선·먹·얼굴·색·명암·기준 그림 규칙) 어디쯤인지를 글로 적었다. 축의 표는 `../README.md` "방법".
그 만화의 그림은 붙이지 않고 특징만 글로 적는다(프롬프트에 작품·작가 이름을 넣지 않는다).

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: our flat 2D cartoon art for a fantasy mercenary game, with a small touch
(about one fifth) of a raw, sketchy modern dark-action manga hand mixed in. It
is still clearly our loose, exaggerated TV-cartoon look: rubbery, bold,
simplified shapes with personality and a big head. Not realistic, not polished
anime, not a cute mascot.
Linework: our dark, medium-thick, hand-drawn ink outline that wobbles a little,
with closed contours, now a little rougher and faster: a few line ends
overshoot, a few small breaks, a few quick pen hatch strokes in the deepest
folds. No clean vector line, no doubled lines.
Blacks: one or two small solid black areas, such as the inside of the cloak.
Color: flat solid fills in muted earthy colors, three to five main colors per
figure. No gradients, no neon, no pastel, no bright saturated colors.
Shading: flat color with at most one hard-edged darker tone in a few places;
the few hatch strokes sit on top of it. No soft shading, no highlights.
Detail: low. Clothes and gear are a few big plain shapes; the scribbly marks
and the few hatch strokes are the only small detail.
Background: fully transparent. No scenery, no ground, no shadow under the feet,
no frame, no rays, no motion lines, no sparks.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, soft
shading, gradients, airbrush, textured fills, glossy highlights, neon, bright
saturated colors, pastel, anime face, cute mascot, chibi, grotesque, ugly,
clean vector line, uniform line weight, dense cross-hatching, screentone dots,
speech bubbles, sound-effect lettering, panel borders, sparkles, drop shadow,
ground shadow, scenery, background, light rays, motion lines, frame, text,
letters, numbers, logo, title, signature, watermark, cropped figure, extra
characters, two figures, a crowd, nudity, see-through clothing, underwear
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image is the current figure of this character in our game, drawn
in our style. Keep almost everything of it: who she is (her long wild wavy
blonde hair with braids, her freckles, the dark red fur-trimmed cloak, the
cream laced top, the short navy skirt, the leather bracers, the fur-topped
boots, the large single-bladed battle axe held in both hands), her hourglass
body, her braced stance, her face and the way she is drawn: the wobbly dark
outline, the rubbery big shapes, the big head and the flat muted colors.
Change only what the style above adds: a slightly rougher, faster pen line with
a few overshooting ends, a few quick hatch strokes, one or two small black
areas and a touch heavier upper eyelids. Draw her as a new drawing, alone,
facing and looking toward the viewer's right. Do not reproduce the transparent
or white background; draw her on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in the stance the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated and rubbery, about five heads tall, with a big head as big as the attached figure's. The hourglass curves the subject says. Rubbery limbs with no muscles drawn inside, big hands and big boots.
- Face: our caricatured but attractive face, as on the attached figure: large expressive eyes with dark pupils, arched eyebrows lowered and drawn a little together, a small upturned nose, freckles, the mouth closed in a firm line. Only a touch more deadpan than the attached figure: the upper eyelids a little heavier. Never smiling.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes and a few quick, loose ink strands.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. The axe oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers, a signature or logos. Output a transparent PNG with all four corner pixels at alpha 0.
