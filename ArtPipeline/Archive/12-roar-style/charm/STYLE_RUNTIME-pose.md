# F1 Style Runtime — charm, 자세 변형용 (기준 그림 = 우리의 확정 그림)

`STYLE_RUNTIME.md`(charm)와 §1·§2·§4가 같고 §3만 다르다: 기준 그림으로 **우리의 확정 그림**을 붙여 얼굴·머리·옷·색을 그대로 두고 자세만 바꾼다(얼굴 일관성). 시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. 채택한 방향(`../roar/STYLE_RUNTIME.md`: 포스터의 손맛 + 지금의 평면 색)에서
사용자 지시 "캐리커처를 기본으로 유지하면서 미형으로"에 따라, **체형의 과장과 표정의 개성은 캐리커처 그대로 두고 얼굴과 몸의 매력을 더한다**: 큰 표정 있는 눈(한쪽이 조금 커도 됨), 잘 다듬은 눈썹,
개성 있되 우아한 코, 잘생긴 턱, 자신 있는 큰 웃음·비웃음. 여성은 떡 벌어지거나 길쭉한 체형에서도 곡선이 드러난다. 그로테스크한 과장(튀어나온 눈, 거대한 코·턱)만 뺐다.
선·낙서 획·부푼 과장·색은 `roar`와 같다. **시선은 화면 오른쪽(적이 서는 쪽)**: 사용자 지시 "눈은 다 적을 바라보는 방향으로"(§4 Gaze, §3). 시선 규칙 전의 세 장은 `<key>_charm`, 뒤의 세 장은 `<key>_charm2`다. 기준 그림은 올린 포스터(저장소 밖). 체형과 얼굴은 소재가 적는다. `Height` 90, `Flip` false.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 세 직업을 캐리커처 체형에 매력적인 얼굴로 | 올린 포스터 (그대로 붙인다. 저장소 밖) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

## 1. Shared Style Rule

```text
Style: flat 2D cartoon art for a fantasy mercenary game, drawn in a loose, goofy,
exaggerated TV-cartoon hand: rubbery, bulbous shapes, boldly varied body types and
caricatured faces full of personality, but drawn appealing: under the exaggeration
every character is pretty, handsome or alluring in their own way. Energetic and
charming, never grotesque, never polished anime, never realistic.
Linework: a dark, medium-thick ink outline that is visibly hand drawn: its weight
varies along the stroke and the contour wobbles a little, closed contours, with a
few small scribbly marks inside the shapes (short strokes for fur, a few stubble
or freckle dots, a crease or two). No clean vector line, no doubled lines.
Color: flat solid fills in muted earthy colors, three to five main colors per
figure, as in our current look. No gradients, no neon, no pastel, no bright
saturated colors.
Shading: flat color with at most one hard-edged darker tone in a few places, such
as the folds of a cloak. No soft shading, no highlights.
Detail: low. Clothes and gear are a few big plain shapes with three or four simple
ornaments at most; the scribbly marks are the only small detail.
Background: fully transparent. No scenery, no ground, no shadow under the feet, no
frame, no rays, no motion lines, no sparks.
```

## 2. Forbidden

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, soft
shading, gradients, airbrush, textured fills, glossy highlights, neon, bright
saturated colors, pastel, anime face, cute mascot, chibi, grotesque, ugly, bulging
eyes, huge nose, huge jaw, missing teeth, clean vector line, uniform line weight,
eyelashes drawn one by one, sparkles, drop shadow, ground shadow, scenery,
background, light rays, motion lines, frame, text, letters, numbers, logo, title,
watermark, cropped figure, extra characters, two figures, a crowd, dot eyes, blank
eyes without pupils, elegant fashion figure
```

## 3. Reference Rule

```text
The attached image shows this exact character of our game, already drawn in our
style. Redraw the same character: keep her face exactly (the eyes, eyebrows, nose,
freckles and grin), her hair, her outfit, her weapon and her colors, and keep the
same drawing hand, line and flat coloring. Change only what the subject says: the
pose and the stance. She faces and looks toward the viewer's right, where her enemy
stands. Draw one figure only, alone, on a transparent background, and do not
reproduce the background of the attached image.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in an energetic, characterful stance, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated and rubbery, between three and five heads tall as the subject says. Every character has a strongly different body type (brawny and top-heavy, squat and barrel-shaped, tall and lanky, round and soft): follow the subject. A woman keeps a curvy figure (a full bust, a narrow waist, full hips) whatever her build. Rubbery limbs with no muscles drawn inside, big hands and big boots.
- Face: caricatured but attractive, as the subject says: large expressive eyes with dark pupils (one may be a touch larger than the other), well-shaped thick eyebrows, a distinctive but elegant nose (long and straight, small and upturned, or strong and straight), a strong handsome jaw for a man and soft pretty features for a woman, and a wide charming mouth (a big confident grin, a cocky half-smile, a sly smirk). A few freckle or stubble dots are fine. Each character gets its own face shape and expression, never grotesque.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
