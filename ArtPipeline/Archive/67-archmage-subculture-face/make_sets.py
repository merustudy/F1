# -*- coding: utf-8 -*-
"""Round 67 (the user, 2026-10-14: "마법사 이미지를 현재 이미지와 별개로 얼굴 기준을 중국이나 일본의 서브컬쳐 여자캐릭터 이미지을 검토 후
20% 40% 60% 80% 반영해서 재생성"): writes the four test sets face20/40/60/80 (STYLE_RUNTIME.md + character.csv).
Only the face changes between them: the base is Round 66's new look 2 (`66-archmage-hourglass/newlook2/`: the style sheet as the
reference, our §1~§4, the pretty-face test standard, the outfit and the current staff). Every set also carries the valkyrie's
proportions (measured: about 5.7 heads, top of the skull to the soles) and a calm look instead of the frown. No API call.
  .venv/bin/python ArtPipeline/Archive/67-archmage-subculture-face/make_sets.py
"""
from pathlib import Path

HERE = Path(__file__).resolve().parent
BASE = HERE.parent / "66-archmage-hourglass/newlook2/STYLE_RUNTIME.md"

# The face from our pretty-face test standard (0%) toward a modern Chinese and Japanese anime/game heroine face (100%), in the
# axes of README "검토": face shape, eyes (size, place, iris, lashes), brows, nose, mouth, blush, the lines of the face.
FACES = {
    20: ("our pretty face standard with a light touch, about one fifth, of a modern Chinese and Japanese anime and game "
         "heroine face: a small, slim oval face with a soft V-shaped jawline and a small pointed chin; large clear eyes, a "
         "little larger than usual, with a deep teal iris in two flat tones, a dark pupil and one small white highlight, "
         "double eyelids and one thick upper lash line with a small flick at the outer corner; soft, natural brows; a small, "
         "slim nose drawn only as a short line for the bridge and a tiny tip, never with the nostrils or the wings of the "
         "nose drawn; small, soft lips in a natural rose color; smooth clear skin. The face is drawn in the same hand-drawn "
         "ink line as the rest of the figure."),
    40: ("a blend of a little more than half our pretty face standard and a little less than half a modern Chinese and "
         "Japanese anime and game heroine face: a soft face whose rounded cheeks narrow to a small pointed chin, the lower "
         "face a little shorter; big clear eyes set a little lower on the head, tall oval deep teal irises in two or three "
         "flat tones with a dark pupil and two small white highlights, a thick dark upper lash line that flicks out at the "
         "outer corner and a few lower lashes; thin soft brows set high; a tiny nose drawn only as a short tick at the tip, "
         "never the nostrils or the wings of the nose; a small mouth with soft rose lips; a faint flat pink blush under the "
         "eyes. The lines inside the face are thinner and cleaner than the ink line of the body."),
    60: ("a blend of a little more than half a modern Chinese and Japanese anime and game heroine face and a little less "
         "than half our pretty face standard: a heart-shaped anime face with soft round cheeks narrowing to a small pointed "
         "chin and a short lower face, the eyes set at about the middle of the head; very large eyes, tall oval deep teal "
         "irises in three flat tones with a dark pupil and two white highlights, a bold dark upper lash line flicking out at "
         "the corner and a few lower lashes; thin high brows partly under the bangs; the nose only as a tiny tick or a small "
         "shadow at the tip, never the nostrils or the wings of the nose; a small mouth drawn as a short line with a hint of "
         "rose lip color; a flat pink blush under the eyes. Clean, thin lines on the face; the rest of the figure stays in "
         "our hand-drawn ink line."),
    80: ("a modern Chinese and Japanese anime and game heroine face, with only a trace of our pretty face standard left: a "
         "polished gacha-heroine face with a rounded skull, soft round cheeks, a very small pointed chin and a short lower "
         "face, the eyes set a little below the middle of the head; very large, expressive eyes with tall oval deep teal "
         "irises in three flat tones, a dark pupil, two white highlights and a lighter crescent at the bottom of the iris, a "
         "bold upper lash line with a flick and a few lower lashes; thin high brows under the bangs; the nose only as a tiny "
         "tick at the tip, never the nostrils or the wings of the nose; a small mouth line with a touch of rose; a flat pink "
         "blush under the eyes; fine, clean lines on the face. The rest of the figure stays in our hand-drawn ink line and "
         "flat color."),
}
EXPRESSION = (" A mercenary about to fight in a dark dungeon does not smile: the mouth is closed and the look is calm, cool and "
              "focused, the brows relaxed, not frowning. The only highlights in the whole figure are the small white ones in "
              "the eyes.\n")
# What §1 says about anime, and what §2 forbids about the eyes and the nose, per level.
STYLE_LINE = {20: "personality. Not realistic, not polished anime, not a cute mascot.",
              40: "personality. Not realistic, not a cute mascot.",
              60: "personality. Not realistic, not a cute mascot.",
              80: "personality. Not realistic, not a cute mascot."}
AVOID_EYES = {20: "big shiny anime eyes", 40: "huge sparkling anime eyes", 60: "sparkling star eyes", 80: "sparkling star eyes"}
AVOID_FACE_TAIL = {20: "", 40: "", 60: ", realistic nose, realistic lips", 80: ", realistic nose, realistic lips"}

SUBJECT = (
    "a seductive young female archmage about five and a half heads tall from the top of her skull to her soles (the hat "
    "not counted): a big head like the mercenaries on the sheet and legs no longer than her head and torso together, never "
    "a tall fashion-model figure; a slim hourglass figure: a full round bust, a very narrow cinched waist, curvy hips and "
    "slender arms and legs; long, straight, glossy jet-black hair with blunt bangs falling down her back; a dark sorceress "
    "outfit in black and deep muted teal with silver trim: a tall pointed wide-brimmed witch hat with a teal band, a black "
    "halter-neck corset top that bares her shoulders and her narrow waist, a short black skirt with a long open teal panel "
    "that falls behind her legs like a cape, a silver chain belt with two small potion vials, long black gloves above the "
    "elbows and black thigh-high heeled boots; holding a tall gnarled wooden staff whose forked top holds a red crystal in "
    "one hand and raising the other hand open as if casting, her face and eyes turned toward her enemy on the right and "
    "looking straight ahead at it; a pretty face as the composition says, with a calm, cool, focused look and the mouth "
    "closed: no smile")


def main():
    base = BASE.read_text(encoding="utf-8")
    body = base[base.index("## 1. Shared Style Rule"):]
    old_face = body[body.index("- Face (a pretty woman"):body.index("- Gaze:")]
    for level, face in FACES.items():
        name = f"face{level}"
        text = body.replace(old_face, f"- Face: {face}{EXPRESSION}")
        text = text.replace("personality. Not realistic, not polished anime, not a cute mascot.", STYLE_LINE[level])
        text = text.replace("big shiny anime eyes", AVOID_EYES[level])
        text = text.replace("heavy jaw, dark lipstick\n", f"heavy jaw, dark lipstick{AVOID_FACE_TAIL[level]}\n")
        head = (f"# F1 Style Runtime — 시험: 대마법사, 중국·일본 서브컬처 여캐 얼굴 {level}%\n\n"
                f"시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다(G19). `../make_sets.py`가 쓴다(손으로 고치지 않는다).\n"
                f"바탕은 Round 66 새 모습 2(`66-archmage-hourglass/newlook2/`). 바뀌는 것은 §4의 \"Face\"(이 단계의 얼굴), §1의 anime 한 줄, §2의 눈·코 금지뿐이다. "
                f"축과 단계의 표는 `../README.md` \"방법\".\n\n"
                f"```text\n.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_{name} \\\n"
                f"  --style ArtPipeline/Archive/67-archmage-subculture-face/{name}/STYLE_RUNTIME.md \\\n"
                f"  --roster ArtPipeline/Archive/67-archmage-subculture-face/{name}/character.csv\n```\n\n"
                f"**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).\n\n")
        folder = HERE / name
        folder.mkdir(exist_ok=True)
        (folder / "STYLE_RUNTIME.md").write_text(head + text, encoding="utf-8")
        (folder / "character.csv").write_text(
            "Key,Subject,Height,Flip,FaceDx,FaceDy,Gender,Hands\n" f"archmage,\"{SUBJECT}\",90,false,,,female,\n",
            encoding="utf-8")
        print(folder)


if __name__ == "__main__":
    main()
