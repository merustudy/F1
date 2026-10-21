# Round 92 — 46단계 아이템 목록의 아이콘: 아이템 29·가방 2·포션 2 (2026-10-21) — 생성, 판정 대기

지시: 2026-10-21 사용자 **"해당 결정(2차로 남긴 것)은 미루고 현재 만들어진 아이템 아이콘 생성 후 보고. 승인 후 연결"**.
46단계(Design/19 판정 13 권장안: "승인 뒤 라운드로 묶어 생성, 그 전에는 이름 Placeholder")의 라운드다. 연결(배선)은 승인 뒤에만 한다.

## 범위와 상한

- 아이템 29(`item` 타입, 칸 모양의 캔버스: Architecture/13 "후처리 (`cell`)"), 가방 2(`bag` 타입, 가방 조각의 비율), 포션 2(`ui_icon` 타입 96x96, 외곽선 6 — 치유·방벽 포션과 같은 길).
- 호출 상한 **33**(항목당 한 번). 라운드 전의 장부 누계 331회 약 $8.95(Roadmap의 총 상한 $10). 한 번에 약 $0.03이라 33회면 약 $0.98 → 누계 약 $9.93. **재생성은 총 상한을 넘기므로 사용자가 상한을 올려야 한다**(아래 "판정"에서 묻는다).
- 기준 그림: 타입의 것(스타일 시트). 유니크 셋은 그 무기를 든 직업의 확정 그림을 `Reference`로(피바람 발키리, 잿불 대마법사, 새벽빛 주교 — 글라디우스의 길). 바람가르기는 방랑자의 그림이 임시(마검사)라 기준 그림 없이.
- 깃발(1×3)은 유일한 세로 조각이다: 칸 모양의 셈은 정사각 구도(§21, 1024x1024)를 고른다(`--size`는 전신 그림에만 쓴다). §21의 "비스듬히"를 `--extra`(기둥을 세워 위에서 아래로, 세로로 길고 좁게)로 덮어 그리고, 맞춘 뒤 `trim_items.py`가 빈 양옆을 잘라 1×3 조각에 든다. 비스듬히 나오면 재생성의 방향이다.
- 실행: `zsh ArtPipeline/Archive/92-item-icons/generate.sh`(`generate.log`). 있는 산출물은 건너뛴다.

## 소재

`ArtPipeline/Rosters/item.csv`(29행, `Reference`는 유니크 셋만)·`bag.csv`(2행)·`ui_icon.csv`(2행: 96x96, 외곽선 6)에 더한 행. 칸 → 캔버스는 Architecture/13 "후처리 (`cell`)"의 표(2×1·3×1 띠 1536x512 → 328x100, 3×2 사각 1536x1024 → 328x224, 1×1·2×2·1×3 정사각 1024x1024 → 328x348; 가방은 조각의 비율 — 2×2 328x328, 3×2 328x218).

| Key | 이름 | 칸 | Subject (요지) |
|---|---|---|---|
| `serrated_dagger` | 톱니 단검 | 2×1 띠 | a serrated dagger: short steel blade with a saw-toothed back edge, small dark crossguard, wrapped leather grip, lying level |
| `war_hammer` | 전쟁망치 | 2×2 정사각 | a war hammer: thick wooden haft, big squared iron head, flat face one side and a short blunt spike the other |
| `pickaxe` | 곡괭이 | 2×1 띠 | a miner's pickaxe: wooden handle, curved iron pick head with a pointed end and a flat chisel end, lying level |
| `berserker_axe` | 광전사의 도끼 | 2×2 정사각 | a berserker's axe: dark haft wrapped with a red cloth strip, one broad notched iron blade with a crescent edge |
| `crossbow` | 석궁 | 2×2 정사각 | a crossbow: wooden stock, steel bow across its front, taut string, short wooden trigger lever below |
| `frost_staff` | 서리 지팡이 | 3×1 띠 | a frost staff: pale ash-grey staff topped with a big jagged pale blue ice crystal, short frost spikes at its base |
| `shadow_staff` | 그늘 지팡이 | 3×1 띠 | a shadow staff: twisted black staff topped with a dark violet orb held by black thorny tendrils |
| `chant_staff` | 성가의 지팡이 | 3×1 띠 | a chant staff: smooth light staff topped with a brass ring holding a small white bell, white cloth strip under the ring |
| `frost_flask` | 얼음 플라스크 | 2×1 띠 | a round glass flask with a cork, pale blue liquid with white ice chips, lying on its side |
| `ash_pouch` | 재 주머니 | 2×1 띠 | a small grey cloth pouch tied with a dark cord, lying on its side, a puff of grey ash spilling from its mouth |
| `caltrops` | 마름쇠 | 2×1 띠 | three iron caltrops in a row, each four short thick spikes joined at the centre, one spike up |
| `hex_bone` | 저주의 뼈 | 2×1 띠 | a yellowed long bone carved with a few dark angular marks, a knot of dark red string around its middle |
| `censer` | 향로 | 2×1 띠 | a small round brass burner with a domed pierced lid, its chain laid out to the left, a wisp of pale smoke |
| `holy_water` | 성수 | 2×1 띠 | a clear glass vial with a silver cap and a small golden sun emblem, pale water, lying on its side |
| `war_drum` | 전투 북 | 2×2 정사각 | a round wooden drum with a tan hide top bound by dark rope, a red band, two drumsticks across the top |
| `banner` | 깃발 | 1×3 정사각(세워서) | a tall wooden pole upright with a brass spearhead, a long red flag with a cream stripe hanging and tapering to a swallow tail (`--extra`: 기둥을 세워 위에서 아래로) |
| `war_horn` | 나팔 | 2×1 띠 | a curved dark brown animal horn with a brass mouthpiece and a brass band near its bell, a leather strap, lying level |
| `bloodlust_charm` | 살기의 부적 | 2×1 띠 | a pendant of a big curved white fang in a dark iron clasp, two dark red drops on the fang, leather cord spread left and right |
| `spiked_shield` | 가시 방패 | 1×1 정사각 | a small round spiked buckler: wooden, steel rim, steel boss, four short thick steel spikes, seen from the front |
| `studded_armor` | 징 박은 갑옷 | 2×2 정사각 | a dark brown leather breastplate with rows of big round steel studs, rounded shoulder pieces, upright, no one inside |
| `leather_armor` | 가죽 갑옷 | 2×2 정사각 | a tan leather jerkin with a laced front, wide dark belt, rounded shoulder flaps, upright, no one inside |
| `robe` | 로브 | 2×1 띠 | a deep blue robe folded flat into a wide bundle, pointed hood on top, small brass clasp, seen from above |
| `cloth_hood` | 천 두건 | 2×1 띠 | a soft grey-white cloth hood with a short cape, laid on its side, a dark cloth tie at the neck |
| `goblin_sawblade` | 고블린 톱니 칼 | 2×1 띠 | a short crude iron sword with a saw-toothed edge, rust spots, grip wrapped in dirty cloth, lying level |
| `bristle_hide` | 가시 털가죽 | 2×1 띠 | a rough patch of grey-brown rat hide with stiff dark bristles standing up along its back, a leather tie at each end |
| `bloodgale` | 피바람 | 3×2 사각 | (기준 그림: 발키리) the valkyrie's battle axe reforged: the blade's edge stained deep red, a dark red cloth under the blade, richer-looking |
| `windcutter` | 바람가르기 | 2×2 정사각 | a slender pale longbow with its string, tips bound with silver, a short green ribbon at the grip, richer-looking |
| `cinder` | 잿불 | 3×1 띠 | (기준 그림: 대마법사) the archmage's fire staff reforged: a bigger brighter red-orange crystal giving off a few embers |
| `dawnlight` | 새벽빛 | 3×1 띠 | (기준 그림: 주교) the bishop's healing staff reforged: a larger golden sun disc with short straight rays |
| `knapsack` (bag) | 등짐 | 2×2 | top-down open leather square divided into exactly four equal empty compartments (2×2), thin rim, one brass buckle |
| `big_pack` (bag) | 큰 배낭 | 3×2 | top-down open dark leather rectangle divided into exactly six equal empty compartments (2 rows of 3), two brass buckles |
| `potion_cleanse` (ui_icon) | 정화 포션 | 96x96 | the round potion bottle of the shipped two, clear pale silver-white liquid, a small white cloth tag |
| `potion_regen` (ui_icon) | 재생 포션 | 96x96 | the same bottle, bright green liquid, a small green leaf tied on a string |

## 생성 (2026-10-21)

- `generate.sh` → 33회, 실패 0, 약 **$0.78**(띠 캔버스 1536x512는 한 번에 약 $0.02, 정사각·사각은 약 $0.03). 장부 누계 **364회 약 $9.74**(총 상한 $10 — 남은 것 약 $0.26, 재생성 8회쯤).
- 산출물: 원본 `ArtPipeline/output/<type>/<key>.raw.png`, 맞춘 그림 `<key>.png`(gitignore). 맞춘 후보를 `candidates/`에 복사했다(33).
- 리뷰 시트: `review-items.png`(아이템 29를 제 모양의 조각에 1배·2배로, 분류별, 지금의 단검·롱소드·미늘창·버클러·철 갑옷·불씨 플라스크 옆에), `review-bags-potions.png`(가방 둘을 상인 타일로 1배·2배, 포션 둘을 64px 주머니와 96px로, 지금 것 옆에). `review_items.py`(호출 없음, `--name key=candidate`로 2차 후보를 넣는다).

## 후보 — 읽은 것과 권장

| Key | 읽은 것 | 권장 |
|---|---|---|
| `serrated_dagger` | 톱니가 선 등날의 단검, 가죽 손잡이, 눕힘 | 확정 |
| `war_hammer` | 각진 쇠머리에 뭉툭한 못, 감은 자루, 비스듬히 | 확정 |
| `pickaxe` | 나무 자루에 굽은 곡괭이 머리(뾰족·납작), 눕힘. 머리가 작은 편이나 읽힌다 | 확정 |
| `berserker_axe` | 이 빠진 넓은 날, 붉은 천을 감은 자루 | 확정 |
| `crossbow` | 나무 몸통에 강철 활, 시위, 방아쇠 | 확정 |
| `frost_staff` | 잿빛 지팡이 끝의 푸른 얼음 결정 | 확정 |
| `shadow_staff` | 검은 지팡이 끝의 보라 구슬. **전체가 검어 어두운 칸에서 약하다**(구슬이 작다) | 재생성 — "구슬을 크고 밝은 보라로, 지팡이는 검정 대신 짙은 회색" |
| `chant_staff` | 밝은 지팡이 끝의 놋쇠 고리와 흰 종, 흰 천 | 확정 |
| `frost_flask` | 푸른 액체와 얼음 조각, 코르크, 눕힘 | 확정 |
| `ash_pouch` | 끈 묶은 잿빛 주머니에서 재가 쏟아짐 | 확정 |
| `caltrops` | 세 개의 **세 갈래** 별꼴(수리검처럼) — 소재의 "네 가시, 하나는 위로"가 아니다. 작게는 가시로 읽힌다 | 확정해도 되나, 원하면 재생성 — "네 가시의 마름쇠, 하나가 위로 선 것 셋" |
| `hex_bone` | 누런 뼈에 각진 검은 새김, 붉은 끈 | 확정 |
| `censer` | 놋쇠 향로와 왼쪽으로 늘어진 사슬, 연기 | 확정 |
| `holy_water` | 유리병에 금빛 해 문양, 은빛 마개, 눕힘 | 확정 |
| `war_drum` | 가죽 북에 붉은 띠, 북채 둘 | 확정 |
| `banner` | **세워진 기둥**에 걸린 붉은 깃발(크림 줄, 제비꼬리), 놋쇠 창끝 — `--extra`가 통했다. 맞춘 캔버스(328x348)의 가운데에 세로로 좁게 서 있어 자르면 1×3 조각에 든다 | 확정 |
| `war_horn` | 굽은 뿔에 놋쇠 마우스피스와 띠, 가죽 끈, 눕힘 | 확정 |
| `bloodlust_charm` | 쇠 집게에 물린 송곳니와 붉은 방울, 양옆으로 퍼진 가죽 끈. **송곳니가 작고 끈이 폭을 다 차지**해 2×1 조각(100x50)에서 송곳니가 아주 작다 | 재생성 — "송곳니를 크게, 끈은 짧고 굵게" |
| `spiked_shield` | 나무 방패에 강철 테·보스·가시 넷, 정면 | 확정 |
| `studded_armor` | 징 박힌 갈색 가죽 갑옷, 어깨, 허리띠 | 확정 |
| `leather_armor` | 갈색 가죽 갑옷 — **미늘창이 함께 그려졌다**(Round 87의 철 갑옷과 같은 §21 예시의 누출) | 재생성 — `--extra "Draw the armor alone: no weapon, no halberd, no pole, no spear, nothing behind or across it."`(87의 해법) |
| `robe` | 접은 푸른 로브, 두건, 놋쇠 걸쇠 | 확정 |
| `cloth_hood` | 옆으로 누운 흰 두건과 짧은 망토, 검은 매듭 | 확정 |
| `goblin_sawblade` | 녹슨 톱니 칼, 헝겊 손잡이, 눕힘 | 확정 |
| `bristle_hide` | 가시 털이 선 잿빛 가죽, 양 끝 가죽 끈 | 확정 |
| `bloodgale` | 발키리의 도끼꼴에 핏빛으로 물든 날과 붉은 천(3×2 사각) | 확정 |
| `windcutter` | 은빛 끝과 초록 리본의 긴 활, 비스듬히 | 확정 |
| `cinder` | 대마법사의 지팡이꼴, 끝에 크고 밝은 붉은 결정(불씨는 작게) | 확정 |
| `dawnlight` | 주교의 지팡이꼴, 끝에 큰 금빛 해 원판과 짧은 빛살 | 확정 |
| `knapsack` (가방) | 위에서 본 열린 가죽 가방, 2×2 칸, 버클 하나 | 확정 |
| `big_pack` (가방) | 2행 3열 여섯 칸, 버클 둘 | 확정 |
| `potion_cleanse` (포션) | 치유·방벽 포션과 같은 병, 은빛 액체, 흰 꼬리표 | 확정 |
| `potion_regen` (포션) | 같은 병, 초록 액체, 잎 하나 | 확정 |

권장: **확정 29, 재생성 3**(그늘 지팡이·살기의 부적·가죽 갑옷), 마름쇠는 선택. 재생성 3~4회는 약 $0.10으로 총 상한($10) 안이다.

## 판정 1 (2026-10-21 사용자 **"재생성 3 / 선택 1 — 재생성 후 보고. 승인 후 연결"**)

넷을 1회씩 다시 그렸다(`generate2.log`, 이름 `<key>_v2`, 첫 후보는 `candidates/<key>.png`에 그대로). 호출 4회 약 $0.09 → 장부 누계 **368회 약 $9.83**(총 상한 $10, 남은 것 약 $0.17).

| Key | 2차의 방향(`--extra`) | 2차 후보(`candidates/<key>_v2.png`) 읽은 것 | 권장 |
|---|---|---|---|
| `leather_armor` | 갑옷만, 무기·미늘창·장대 없이(87의 해법) | 끈으로 여민 가죽 갑옷, 어깨 덮개, 금빛 버클 — 무기 없음 | 2차 확정 |
| `shadow_staff` | 구슬을 크고 밝은 보라(심은 더 밝게)로, 지팡이는 검정 대신 짙은 회색 | 짙은 회색의 꼬인 지팡이 끝에 큰 밝은 보라 구슬 — 어두운 바탕에서 읽힌다 | 2차 확정 |
| `bloodlust_charm` | 송곳니를 크게(띠 높이를 거의 채움), 쇠 집게는 위에, 끈은 짧고 굵게 | 큰 송곳니에 붉은 방울, 위의 검은 집게, 양옆의 짧은 가죽 끈 | 2차 확정 |
| `caltrops` | 가시 넷(셋은 바닥에, 하나는 위로), 납작한 세 갈래 별이 아니게 | 바닥의 가시 셋 위로 하나가 선 마름쇠 셋 | 2차 확정 |

리뷰 시트 둘은 2차 후보로 다시 그렸다(`review_items.py --name leather_armor=leather_armor_v2 …`).

## 판정 2 (2026-10-21 사용자 **"마름쇠 대신 다른 아이템으로 변경"** → 후보 셋(투척 단검·톱니 원반·가시 갈고리, 권장 투척 단검) → **"권장안 반영. 이미지 생성 후 보고. 내 승인 후 연결 및 구현"**)

- 데이터: `ItemData.csv`의 `caltrops` 행을 **`throwing_knives`(투척 단검, Throwing Knives)**로 바꿨다 — 자리·수치·가중치는 그대로(공격 2×1, 물리, 5.0초, 앞의 하나 출혈 3, 값 10, 8/8). Generated `ItemData.json` 다시 씀, `validate` OK(56 items). 기획: 02 §4 "아이템 목록", Design/19 헤더, 00 "Design/19의 판정" 16.
- 소재(`Rosters/item.csv`, 마름쇠 행을 대신함): three short throwing knives with dark steel blades and wrapped grips, lying level side by side with their points to the right, held in a worn brown leather strap. 마름쇠의 후보 둘(`candidates/caltrops.png`·`caltrops_v2.png`)은 거절로 남긴다.
- 생성 1회(`generate3.log`, 약 $0.02) → `candidates/throwing_knives.png`. 장부 누계 **369회 약 $9.85**(총 상한 $10, 남은 것 약 $0.15). 리뷰 시트는 투척 단검을 넣어 다시 그렸다.
- 읽은 것: 갈색 가죽 띠에 꽂힌 짙은 강철의 투척 칼 셋, 날끝이 오른쪽, 눕힘 — 2×1 띠를 가득 채우고 칼 셋이 또렷하다(톱니 단검 하나와 겹치지 않음). 권장 **확정**.

## 판정 3 (2026-10-21 사용자 **"승인"**) — 33 전부 연결

- 확정: 1차 후보 25 + 2차 후보 3(가죽 갑옷·그늘 지팡이·살기의 부적) + 투척 단검 = 아이템 29, 가방 2, 포션 2. 원본은 `approved/<key>.png`(맞춘 것)·`<key>.raw.png`(2차는 그 이름으로; 포션은 `cleansing_potion`·`regen_potion`). 거절: `candidates/caltrops.png`·`caltrops_v2.png`(마름쇠 자체를 뺌), 가죽 갑옷·그늘 지팡이·살기의 부적의 1차.
- 배선: 아이템 29 → `Assets/@Art/Item/<id>.png`(`trim_items.py`로 자름), 유니크 넷 `<id>_glow.png`(`glow_items.py`), `ItemData.csv`의 `Icon` `item/<kebab-id>`; 가방 둘 → `Assets/@Art/Bag/<id>.png`(맞춘 그림 그대로), `BagData.csv`의 `Icon`; 포션 둘 → `Assets/@Art/Potion/<id>.png`(96x96), `PotionData.csv`의 `Icon`. Generated JSON 다시 씀, `validate` OK. `ShippedDataTests`: 가방 그림의 단언을 다시 모두로, 아이템·포션·유니크 글로의 그림 단언을 46단계 Test에 더함.
- 라운드의 호출 38회 약 $0.90(33 + 재생성 4 + 투척 단검 1). 장부 누계 369회 약 $9.85(총 상한 $10).
- 체인: `20261021-142817`·`20261021-145033`은 PlayMode의 상점 Test 하나가 격자의 행 수 단언(8행 고정, 가방은 8행)에서 멈췄다 — 아이콘과 무관하게, 풀이 커진 뒤 그 시드의 상점이 큰 조각과 가방 여럿으로 9행을 쓴 것(격자는 설계대로 필요한 행을 더한다: `MerchantLayout`). Test를 규칙대로(8행 이상, 가방은 가방 행 이상에서 시작, 아이템과 가방 사이에 빈 행) 고쳐 `20261021-150110` **CHAIN OK**(setup OK — 새 PNG 33의 Import와 `.meta`, sim OK, EditMode 1087/1087, PlayMode 97/109 — 건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
- 스크린샷(`Tools/screenshots.sh`, `20261021-152336`, 12/12, Render 설정 변경 없음): `impl-shop.png`(`ko_42_shop`)·`impl-shop-pick.png`(`ko_43_shop_pick`) — 고정 시드의 4층 상점에 새 아이템 넷이 섰다: **석궁**(2×2, 툴팁 "요구 민첩 5"는 파티에 든 사람이 없어 붉음 — 규칙대로), **곡괭이**·**저주의 뼈**(2×1 띠), **깃발**(1×3 — 세로 조각을 기둥이 위에서 아래로 채운다), 지금의 창·단검·불씨 플라스크와 같은 두께의 선·같은 채색. 포션 열에 **재생 포션**(초록)이 치유·방벽 옆에 섰다. 이름 Placeholder는 어디에도 없다. 조각은 잘린 아이콘을 비율대로 채운다(1×3·2×2·2×1 모두). 유니크 넷은 상점·전리품에 나오지 않아 스크린샷에 없다(담금질 뒤 글로 — `glow_items.py --check` OK).
