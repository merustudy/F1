# F1 Style Runtime

이미지를 생성할 때 스크립트(`tools/gen_image.py`)가 읽는 스타일 규칙이다. 스타일 문구는 이 파일에만 둔다.
방향과 기준 그림은 [`Docs/Design/10_Art_Direction.md`](../Docs/Design/10_Art_Direction.md), 파이프라인의 규칙은
[`Docs/Architecture/13_ART_PIPELINE.md`](../Docs/Architecture/13_ART_PIPELINE.md)가 소유한다.

**조립 순서:** 타입의 Style Rule → Subject → 타입 섹션 → (던전의 것이면) 던전 컨셉 → 타입의 Forbidden → 타입의 Reference Rule
**형식 계약:** 번호 섹션의 제목은 `## <번호>. <제목>`. §1, §2와 Reference Rule 섹션에는 ```` ```text ```` 블록이 하나씩 있고, 타입 섹션은 `- ` 불릿으로 쓴다.
던전 컨셉 섹션의 제목은 `## Dungeon: <DungeonData.Id>`이고, 그 안의 `### Creatures`(적)와 `### Place`(배경) 아래에 `- ` 불릿으로 쓴다.
번호를 바꾸면 `gen_image.py`의 섹션 번호도 함께 바꾼다.
오려 낸 그림(`character`, `enemy`, `item`)은 §1과 §2를, 배경은 자기 것(§10, §12)을 Style Rule과 Forbidden으로 쓴다: 배경은 불투명하고 장면을 그리기 때문이다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 직업의 전신 그림. 오른쪽을 본다 | `References/Character/style_ref_mercenary.jpg` (좌우를 뒤집어 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |
| `enemy` | 적의 전신 그림. 왼쪽을 본다. 던전 컨셉이 붙는다 | `References/Character/style_ref_mercenary.jpg` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §5 | §6 | `figure` |
| `background` | 화면의 배경. 불투명, 인물 없음. 던전의 배경이면 던전 컨셉이 붙는다 | `References/Character/style_ref_mercenary.jpg` (그대로 붙인다) | `2304x1536` | `medium` | `opaque` | §11 | §13 | `scene` |
| `item` | 아이템의 아이콘 | 그 아이템을 든 유닛의 확정된 그림(Roster의 `Reference`). 없으면 `References/Character/style_ref_mercenary.jpg` | `1024x1024` | `medium` | `transparent` | §7 | 든 유닛의 그림이면 §9, 아니면 §8 | `icon` |

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: flat 2D cartoon art for a fantasy mercenary game. Calm, grounded and a little
stoic. Hand drawn with bold, simple, rounded shapes. Not chibi and not realistic.
Linework: a bold, even, dark outline around every shape, slightly thinner lines
inside the figure, closed contours, no sketchy, broken or doubled lines.
Color: flat solid fills in muted earthy colors, three to five main colors per figure.
No gradients, no neon, no pastel.
Shading: flat color with at most one hard-edged darker tone in a few places, such as
the folds of a cloak. A few short light strokes on metal are allowed. No soft shading.
Detail: low. Clothes and gear are a few big plain shapes of flat color with at most
three or four simple ornaments in all, such as one brooch, one emblem and one belt
with a pouch. No trim along every edge, no engraving, no small buckles or studs.
Background: fully transparent. No scenery, no ground, no shadow under the feet, no
frame.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: chibi, super deformed, tall and thin, long legs, elegant fashion figure,
sharp jaw, thick eyebrows, eyelashes, pupils, irises, anime face, realistic,
photorealistic, 3D render, CGI, painterly, watercolor, oil painting, soft shading,
gradients, airbrush, texture, grain, glossy highlights, intricate detail, ornate trim,
engraving, many small parts, neon, glow, sparkles, drop shadow, ground shadow,
scenery, background, frame, text, letters, numbers, watermark, cropped figure, extra
characters
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
Use the attached image only as a style reference for line weight, body proportions
and build, face style, color range, shading amount and level of detail. Do not copy the
character shown in it: not the face, hair, outfit, colors or weapon. Do not reproduce
its plain background. Draw the described subject as a new, original character in the
same drawing style on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing upright on both feet.
- Proportions: an adult figure about six heads tall with a fairly large round head, a long torso and short sturdy legs with the knees set low. By default the build is heavy: thick arms and legs, big hands, big boots, a wide stance and a broad silhouette. When the subject asks for a lighter build, slim the body down to the build of the woman in the reference and keep everything else. Never long-legged, never tall and thin.
- Women: a female character has a clearly feminine figure like the woman in the reference: a full bust, a defined waist and wide hips. Her clothes fit the upper body and are belted at the waist so that this shape reads; a robe or a cloak never hides it.
- Face: very pale skin and a wide round face with two large blank white oval eyes that have a thick dark outline and no pupils, one thin curved lid line above each eye, a tiny tick for a nose and a very small mouth. No thick eyebrows, no eyelashes, no sharp jaw. A calm, expressionless look.
- Turn the figure toward the right side of the image, as the attached reference is turned: a three-quarter front view with the head and the chest facing the viewer's right.
- Let the figure hold the weapon the subject names, upright or at rest at its side, with the whole weapon inside the canvas. Keep the top of the weapon no higher than the top of the head, so the head is the highest point of the figure.
- Make the job readable from the silhouette: hair or headwear, the main garment and the weapon.
- Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet.
- Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.

## 5. Enemy

- Draw one enemy creature alone, the whole body, standing or crouching, with every limb, the tail and the weapon inside the canvas.
- Turn the creature toward the left side of the image: a three-quarter front view with the head and the chest facing the viewer's left.
- Proportions: follow the subject. A goblin is a short, hunched humanoid about four heads tall with a big head, long pointed ears, long arms, big hands and big bare feet. A beast keeps its animal build, drawn sturdy and simple.
- Face: blank eyes with a thick dark outline and no pupils, a simple mouth, and a few plain teeth or fangs where the creature calls for them. Sullen and menacing, never cute and never gory: no blood, no wounds.
- Draw it in the same hand as the mercenaries: big plain shapes, crude gear drawn as plain shapes, nothing small inside the body.
- Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet, the tail or the weapon.
- Draw one creature only: no second figure, no scenery, no ground line and no shadow under the feet.
- Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.

## 6. Enemy Reference Rule

The last part of an `enemy` prompt.

```text
Use the attached image only as a style reference for line weight, shape
simplification, color range, shading amount and level of detail. It shows a human
mercenary: do not copy her, and do not give the creature her body, face, hair,
outfit or weapon. Do not reproduce its plain background. Draw the described creature
as a new, original figure in the same drawing style on a transparent background.
```

## 7. Item

- Draw one item alone, large and centered: no hand, no character, no creature and no scene.
- It is an icon that is shown about 40 pixels high on a dark panel. Keep to the item's basic silhouette and one or two big features, in two to four flat colors. Nothing thin and nothing small: no engraving, no rivets, no fine string, no tiny gems.
- Lay a long item (a sword, an axe, a spear, a staff, a bow) on the diagonal from the lower left to the upper right, with its point or head at the upper right, so that it fills the square.
- Use mid-tone and light fills that stay readable on a dark background, and keep the darkest tone to the outline.
- Give it the same bold dark outline as the figures, a little thicker for its size.
- Keep the whole item inside the canvas with a clear empty margin on all four sides.
- Never add text, letters, numbers, a frame, a plate, a background, a ground shadow or a glow. Output a transparent PNG with all four corner pixels at alpha 0.

## 8. Item Reference Rule

The last part of an `item` prompt when the reference is the style reference.

```text
Use the attached image only as a style reference for line weight, shape
simplification, color range and shading amount. It shows a character: do not draw the
character, any part of a body or a hand, and do not copy the things she carries. Draw
only the described item, as a new standalone icon in the same drawing style on a
transparent background.
```

## 9. Held Item Reference Rule

The last part of an `item` prompt when the reference is the figure of the unit that holds the item.

```text
The attached image shows a character of this game holding the item. Draw that same
item alone as an icon: keep its shape, its parts and its colors as the character
holds it, simplified to read at a small size. Do not draw the character, any part of
a body or a hand, and do not reproduce the pose. Use the same drawing style: line
weight, flat colors and shading amount. Transparent background.
```

## 10. Background Style Rule

Prepend verbatim to a `background` prompt, in place of §1.

```text
Style: flat 2D cartoon background art for a fantasy mercenary game, drawn in the same
hand as its characters: bold, simple, rounded shapes. Calm and grounded. Not realistic.
Linework: a bold, even, dark outline around the big shapes, thinner lines inside
them, closed contours, no sketchy or broken lines.
Color: flat solid fills in muted earthy colors. No gradients, no neon, no pastel.
Shading: flat color with hard-edged darker and lighter tones. Light is drawn as flat
shapes with hard edges, never as a soft glow.
Depth: what is farther away is drawn darker, greyer and plainer.
Tone: darker and lower in contrast than the characters who will stand in front of it,
so that they stand out. The brightest spots are small.
```

## 11. Background

- Draw an empty place seen straight from the side, like a stage. It runs from left to right across the image, not away into the distance. No character, no creature and no figure of any kind.
- A level floor fills the lower half of the image and the back wall of the place rises behind it. The line where the wall meets the floor runs straight and level across the whole width, a little above the middle of the image.
- The characters will stand in a row on the floor, in the middle band of the image. Keep that band calm: no large object and no bright object in it.
- Give the back wall a rhythm: the same structure repeated at even intervals across the width, as a corridor repeats its arches. Put a few small lights on it, each with a small flat pool of light around it.
- Frame the scene with larger, darker things at the left and right edges and in the two lower corners, close to the viewer and drawn as near-silhouettes.
- Keep the floor plain. Panels will cover much of it.
- Make the image darker toward its edges and corners, with flat darker shapes rather than a soft fade.
- The image is opaque and fills the whole canvas from edge to edge. Never add text, letters, numbers, a frame, a border or a logo.

## 12. Background Forbidden

Paste as the AVOID block of a `background` prompt, in place of §2.

```text
avoid: characters, people, creatures, animals, faces, a view down a corridor into
the distance, a vanishing point, realistic, photorealistic, 3D render, CGI, painterly,
watercolor, oil painting, soft shading, gradients, airbrush, texture, grain, glossy
highlights, neon, glow, bloom, lens flare, sparkles, fog, text, letters, numbers,
watermark, frame, border, user interface
```

## 13. Background Reference Rule

The last part of a `background` prompt.

```text
Use the attached image only as a style reference for line weight, flat coloring and
shading amount. It shows a character of the game who will later stand in front of
this background: do not draw her or any other character, creature or figure. Draw the
described place as an empty scene in the same drawing style, filling the whole canvas.
```

## Dungeon: abandoned_mine

### Creatures

- This creature lives in an abandoned mine that goblins have taken over.
- Its blank eyes are a dull lantern yellow.
- It is dusted with soot and grey rock dust.
- A goblin wears one piece of scavenged mining gear, such as a dented miner's cap with a candle stub, a small tin lantern on the belt, a coil of rope or a leather apron. A beast carries no gear.
- Gear and accents use the colors of the mine: rust orange, soot grey and dull lantern yellow.

### Place

- The place is an abandoned mine that goblins have taken over: a rough rock tunnel.
- Old timber frames, two posts and a beam, shore the back wall at even intervals. A tin lantern with a candle stub hangs on some of the posts.
- A rusted rail track runs along the floor near the back wall. An ore cart, crates, coils of rope, a pickaxe and loose rock are what is left lying around.
- The colors of the mine: dark brown and soot grey rock, rust orange and dull lantern yellow.
