"""Writes broken.csv and resolute.csv of round 40: the five mercenaries' state-pose subjects, in the grammar of the
valkyrie's approved ones (Archive/38-breakdown-fx/broken.csv, resolute.csv). The character part of each subject is the
one of Rosters/hit.csv (the approved description), split off before ". Pose:"; only the pose part is new. No API call.
  .venv/bin/python ArtPipeline/Archive/40-state-poses/write_rosters.py
"""
import csv
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROSTERS = HERE.parents[1] / "Rosters"
JOBS = ("knight", "bishop", "paladin", "archmage", "spellblade")

BROKEN_FACE = ("the eyes opened very wide and hollow, staring, their dark pupils tiny, the eyebrows slack and drooping at the outer "
               "ends, the mouth hanging a little open in a frozen trembling line, no teeth showing, dark hollows under the eyes as one "
               "hard-edged darker tone")
RESOLUTE_FACE = ("calm and fierce, the eyes narrowed with steady dark pupils fixed on the enemy to the right, the eyebrows level and "
                 "lowered, the mouth closed in a firm straight line with no smile")
BROKEN_END = "No sweat, no tears, no blood, no wound, no scream"
RESOLUTE_END = "No glow, no rays, no halo"

BROKEN = {
    "knight": (
        "Pose: his mind has broken; he stands in place, hollow with despair: his knees buckle a little, his shoulders cave inward and his "
        "whole upper body sags, his head hangs forward and low so that his face is turned down while his eyes still look up toward the enemy "
        "on the right; one slack gauntlet lets the longsword hang straight down at his side, point down, its tip resting on the floor line in "
        "front of his feet, the other gauntlet clutches his own forearm; both soles on the floor line. His expression: " + BROKEN_FACE +
        "; his short hair falls limp over his brow. The whole figure is drawn in darker, duller, colder versions of his own colors, the steel "
        "gone dull and grey, as if all warmth had drained from him. " + BROKEN_END),
    "bishop": (
        "Pose: her mind has broken; she stands in place, hollow with despair: her knees buckle a little, her shoulders cave inward and her "
        "round body sags, her head hangs forward and low, the mitre still on her head and tilting forward a little, so that her face is turned "
        "down while her eyes still look up toward the enemy on the right; one slack hand lets the staff lean loosely at her side, tilted, its "
        "gold cap resting on the floor line, the other hand clutches her own forearm; both soles on the floor line. Her expression: " +
        BROKEN_FACE + "; her braid hangs limp forward over her shoulder and loose strands fall over her brow and down over one eye. The whole "
        "figure is drawn in darker, duller, colder versions of her own colors, the cream gone grey and the gold gone dull, as if all warmth had "
        "drained from her. " + BROKEN_END),
    "paladin": (
        "Pose: his mind has broken; he stands in place, hollow with despair: his knees buckle a little, his shoulders cave inward and his huge "
        "chest sags, his head hangs forward and low so that his face is turned down while his eyes still look up toward the enemy on the right; "
        "his right hand (the arm on the viewer's left in the attached image) hangs slack and lets the mace hang straight down at his side with "
        "its spiked head resting on the floor line, his left arm (toward the viewer's right in the attached image) drops so that the shield, "
        "still on that arm, hangs low beside his leg with its lower point near the floor line; both soles on the floor line. His expression: " +
        BROKEN_FACE + "; his hair falls limp over his brow. The whole figure is drawn in darker, duller, colder versions of his own colors, the "
        "pale steel and the cream gone grey and the gold gone dull, as if all warmth had drained from him. " + BROKEN_END),
    "archmage": (
        "Pose: her mind has broken; she stands in place, hollow with despair: her knees buckle a little, her shoulders cave inward and her long "
        "body sags, her head hangs forward and low so that her face is turned down while her eyes still look up toward the enemy on the right; "
        "one slack hand lets the staff lean loosely at her side, tilted, its bottom resting on the floor line and the red crystal leaning away "
        "from her, the other hand clutches her own forearm, her wide sleeves hanging; both soles on the floor line. Her expression: " +
        BROKEN_FACE + "; her long grey hair hangs limp and ragged over her brow and down over one eye. The whole figure is drawn in darker, "
        "duller, colder versions of her own colors, the purple gone grey and the crystal gone dark, as if all warmth had drained from her. " +
        BROKEN_END),
    "spellblade": (
        "Pose: his mind has broken; he stands in place, hollow with despair: his knees buckle a little, his shoulders cave inward and his whole "
        "upper body sags, his head hangs forward and low so that his face is turned down while his eyes still look up toward the enemy on the "
        "right; his slack sword hand lets the longsword hang straight down at his side, point down, its tip resting on the floor line in front "
        "of his feet, the other hand clutches his own forearm; both soles on the floor line. His expression: " + BROKEN_FACE +
        "; the loose strands of his hair hang limp over his brow and down over one eye, and his scarf hangs limp. The whole figure is drawn in "
        "darker, duller, colder versions of his own colors, the red of the scarf gone dull and dark, as if all warmth had drained from him. " +
        BROKEN_END),
}

RESOLUTE = {
    "knight": (
        "Pose: his resolve has hardened into calm fury; he stands in place, tall and squared: his feet planted apart with both soles on the "
        "floor line, his chest out and his shoulders pulled back, his chin raised and his head held high, one gauntlet gripping his longsword "
        "and holding it upright beside his shoulder with the blade pointing straight up and its tip high above his head, the other hand a "
        "clenched fist at his side. His expression: " + RESOLUTE_FACE + "; his tabard lifts a little to the left as if in a wind. The whole "
        "figure is drawn in warmer, brighter versions of his own colors, with one hard-edged pale gold tone along the top edges of his hair, his "
        "pauldrons and the blade as a rim of light. " + RESOLUTE_END),
    "bishop": (
        "Pose: her resolve has hardened into calm fury; she stands in place, tall and squared: her feet planted apart with both soles on the "
        "floor line, her chest out and her shoulders pulled back, her chin raised and her head held high, one hand gripping her staff and "
        "holding it upright beside her shoulder with the gold sun at its top high above her head, the other hand a clenched fist at her side. "
        "Her expression: " + RESOLUTE_FACE + "; her braid and her cape lift and flow back to the left as if in a wind. The whole figure is drawn "
        "in warmer, brighter versions of her own colors, with one hard-edged pale gold tone along the top edges of her mitre, her shoulders and "
        "her cape as a rim of light. " + RESOLUTE_END),
    "paladin": (
        "Pose: his resolve has hardened into calm fury; he stands in place, tall and squared: his feet planted apart with both soles on the "
        "floor line, his chest out and his shoulders pulled back, his chin raised and his head held high, his right hand (the arm on the "
        "viewer's left in the attached image) gripping the mace and holding it upright beside his shoulder with its spiked head high above his "
        "head, the shield on his left arm (toward the viewer's right in the attached image) held squarely in front of his chest with its face "
        "toward the enemy. His expression: " + RESOLUTE_FACE + "; his cloak lifts and flows back to the left as if in a wind. The whole figure "
        "is drawn in warmer, brighter versions of his own colors, with one hard-edged pale gold tone along the top edges of his hair, his "
        "pauldrons and the rim of the shield as a rim of light. " + RESOLUTE_END),
    "archmage": (
        "Pose: her resolve has hardened into calm fury; she stands in place, tall and squared: her feet planted apart with both soles on the "
        "floor line, her chest out and her shoulders pulled back, her chin raised and her head held high, one hand gripping her staff and "
        "holding it upright beside her shoulder with the red crystal high above her head, the other hand a clenched fist at her side. Her "
        "expression: " + RESOLUTE_FACE + "; her long hair and her wide sleeves lift and flow back to the left as if in a wind. The whole figure "
        "is drawn in warmer, brighter versions of her own colors, with one hard-edged pale gold tone along the top edges of her hair and her "
        "shoulders as a rim of light. " + RESOLUTE_END),
    "spellblade": (
        "Pose: his resolve has hardened into calm fury; he stands in place, tall and squared: his feet planted apart with both soles on the "
        "floor line, his chest out and his shoulders pulled back, his chin raised and his head held high, one hand gripping his longsword and "
        "holding it upright beside his shoulder with the blade pointing straight up and its tip high above his head, the other hand a clenched "
        "fist at his side. His expression: " + RESOLUTE_FACE + "; his coat tails, his scarf and the loose strands of his hair lift and flow back "
        "to the left as if in a wind. The whole figure is drawn in warmer, brighter versions of his own colors, with one hard-edged pale gold "
        "tone along the top edges of his hair and his shoulders as a rim of light. " + RESOLUTE_END),
}


def character_parts():
    with (ROSTERS / "hit.csv").open(encoding="utf-8", newline="") as handle:
        rows = {row["Key"]: row["Subject"] for row in csv.DictReader(handle)}
    out = {}
    for job in JOBS:
        head, sep, _ = rows[job].partition(". Pose:")
        assert sep, job
        out[job] = head
    return out


def write(name, poses, parts):
    path = HERE / f"{name}.csv"
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        writer = csv.writer(handle, lineterminator="\n")
        writer.writerow(["Key", "Subject"])
        for job in JOBS:
            writer.writerow([job, f"{parts[job]}. {poses[job]}"])
    print(path.relative_to(HERE.parents[2]), sum(len(poses[j]) for j in JOBS) // len(JOBS), "chars of pose on average")


parts = character_parts()
write("broken", BROKEN, parts)
write("resolute", RESOLUTE, parts)
