# F1 Style Runtime — 시험: csm40 (지금 그림체 60% + 체인소맨 만화풍 40%)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. Round 65의 네 단계(20·40·60·80%) 가운데 둘째.
비율은 모델이 숫자로 알아듣지 못하므로 축마다(비율·선·먹·얼굴·색·명암·기준 그림 규칙) 어디쯤인지를 글로 적었다. 축의 표는 `../README.md` "방법".
그 만화의 그림은 붙이지 않고 특징만 글로 적는다(프롬프트에 작품·작가 이름을 넣지 않는다).

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: a blend of our flat 2D cartoon art for a fantasy mercenary game (a
little more than half) and a raw, sketchy modern dark-action manga hand (a
little less than half). Still a stylized cartoon figure with bold, simplified
shapes and a fairly big head, but clearly drawn rougher and closer to a manga
pen. Not realistic, not polished anime, not a cute mascot.
Linework: a dark ink outline of uneven weight that closes the figure, drawn
fast with a pen: scratchy in places, line ends that overshoot, small breaks,
thinner lines inside the shapes. Quick, loose hatching in the shadows of the
cloak, the hair and the skirt. No clean vector line, no doubled lines.
Blacks: some solid black areas (spotted blacks): the inside of the cloak, the
deepest gaps in the hair, the boots.
Color: flat solid fills in muted earthy colors, three to five main colors per
figure. No gradients, no neon, no pastel, no bright saturated colors.
Shading: flat color with at most one hard-edged darker tone, plus the hatching
and the black areas. No soft shading, no highlights.
Detail: low to medium: big plain shapes, with a few creases and stitches drawn
in quick pen lines.
Background: fully transparent. No scenery, no ground, no shadow under the feet,
no frame, no rays, no motion lines, no sparks.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: photograph, photorealistic, 3D render, CGI, painterly, watercolor, soft
shading, gradients, airbrush, textured fills, glossy highlights, neon, bright
saturated colors, pastel, big anime eyes, sparkles in the eyes, cute mascot,
chibi, grotesque, ugly, clean vector line, uniform line weight, neat finely
rendered illustration, dense fine cross-hatching, screentone dots, speech
bubbles, sound-effect lettering, panel borders, drop shadow, ground shadow,
scenery, background, light rays, motion lines, frame, text, letters, numbers,
logo, title, signature, watermark, cropped figure, extra characters, two
figures, a crowd, nudity, see-through clothing, underwear
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image is the current figure of this character in our game, drawn
in our style. Keep who she is (her long wild wavy blonde hair with braids, her
freckles, the dark red fur-trimmed cloak, the cream laced top, the short navy
skirt, the leather bracers, the fur-topped boots, the large single-bladed battle
axe held in both hands), her hourglass body, her braced stance and our flat
muted colors. Keep a little more than half of the way it is drawn: the bold
simplified shapes, a fairly big head and the stylized pretty face. Change the
rest as the style above says: a rougher manga pen line with hatching and black
areas, a slightly taller figure with less rubbery limbs, a cooler and more
deadpan face. Draw her as a new drawing, alone, facing and looking toward the
viewer's right. Do not reproduce the transparent or white background; draw her
on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in the stance the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: stylized, about five and a half heads tall: the head a little smaller than the attached figure's, but still a little big. The hourglass curves the subject says. Limbs less rubbery than the attached figure, with a hint of knees and muscle; hands and boots a little big.
- Face: between our cartoon face and a deadpan manga face: medium-sized eyes with dark pupils and heavier upper eyelids, thinner eyebrows lowered a little, a small nose drawn with a few lines, freckles as small ink dots, lips closed in a flat line. A cool, flat, dangerous look; still pretty and stylized. Never smiling.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: clumps broken into loose, wild ink strands, the deepest gaps filled solid black.
- Clothes and gear as a few big shapes that fit the figure, with some creases and stitches in quick pen lines; fur drawn with quick scratchy strokes. The axe large and heavy, a little less simplified than the attached figure's.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers, a signature or logos. Output a transparent PNG with all four corner pixels at alpha 0.
