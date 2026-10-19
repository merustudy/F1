# 리팩토링 검토 (2026-10-20): R17 제안 놓기의 한 몸, R18 UiPalette

Snapshot: 2026-10-20 (완료: 검토 보고 → 사용자 "권장안 진행" → R17·R18 구현, 문서, 체인 OK. 같은 날 세션 정리에서 주제별 커밋·푸시)

## Goal
10-12의 리팩토링 검토(`completed/b16-tuning.md` "리팩토링 검토 (2026-10-12)") 뒤 Slice C·D(24~39단계)가 더한 코드에 큰 리팩토링이 필요한지 다시 보고,
계기가 온 묶음만 40단계(맞추기와 직접 플레이) 전에 정리한다. 공개 표면(Manager의 메서드, Test)은 바꾸지 않는다.

## State
- 2026-10-20 사용자 "코드 리펙터링 필요여부 검토" → 검토 보고(아래 "검토 결과", 판정할 것 셋) → 사용자 **"권장안 진행"** → R17·R18 구현, Architecture/08·12, 이 Handoff, Roadmap.
- 커밋: 주제별 셋(R17 `9471dbc` / R18과 Prefab Stamp `829fb15` / Roadmap·Handoff `87a3c9d`)과 세션 정리의 하나. 2026-10-20 세션 정리에서 `origin/feature/slice-a`에 푸시.

## 검토 결과 (2026-10-20)

결론: **큰 리팩토링은 필요 없다**(10-12 결론 유지). 잰 것:

- 크기 63.7k줄(10-12 검토 때 49.9k, +28% = Slice C·D). Runtime 31.1k / Editor 8.2k / Test 22.6k / Sim 1.9k. Test EditMode 1,028·PlayMode 108.
- 불변조건: Domain·Data·Save의 `UnityEngine` 참조 0, Domain의 `Debug`·`DateTime`·`Random` 0, Manager끼리 `Managers.X` 0, UI의 규칙 계산 0(별·세기는 `StarRules`·`StatRules`를 부름),
  화면의 `Balance.X`는 문구 표시용 읽기뿐, TODO 0, `dotnet build Tools/Sim` 경고 0.
- 복붙(공백을 정규화한 7줄 창이 두 곳 이상): Runtime+Sim 0.6%, Editor 0.3%, Test 1.5%(최대 `UiTestUtil` 23줄×2).
- 새 코드의 모양: 규칙은 작은 Type(`TraitRules` 236, `StatRules` 170, `LevelRules` 136, `BuildingRules` 165), 특성 뷰 여덟이 30~120줄. 큰 파일의 성장은 완만
  (`BattleScreen` +106, `ExpeditionRules` +293, `BattleEngine` +155). 예외 `LobbyScreen` 235 → 853은 Slice C의 로비 전부이고 `RefreshTown/Strip/Barracks/Inn/Coach/RunOver`로 나뉜다.
- 긴 메서드: `StaticData.CheckReferences` 207, `Simulations.Run` 219, `BattleScreen.RefreshLoot` 119, `NodeMapScreen.Refresh` 112, `BattleEngine.Activate` 110 — 평면 목록이라 읽히고, 크기로 생긴 버그·충돌 기록이 없다.
- 쓰지 않는 것: `UiPalette.Party`·`Enemy` 둘(R18). `UiKeys`·`SoundEffect`는 모두 쓰인다.
- 하지 않기로 본 것(이유): `UiFlowTests` 4,283줄·`RunRules` 697줄의 분할(크기만의 이유), `Simulations.Run`(도구, 보고 줄이 더해지는 형), `CheckReferences`(평면 목록),
  세 격자 뷰의 기반 클래스(10-12 판단 유지), 코드 주석의 경위(날짜·Round) 1,095줄 걷어내기(출처 표기와 섞여 가려내는 비용 > 이득).

## Done

- **R17 제안 놓기의 한 몸**: 바닥의 드랍과 상점의 물건(`ItemOffer`)을 보드·인벤토리에 놓는 규칙이 전리품과 상점에 한 벌씩 있던 것(Can/Do 여섯 짝, `Pick`/`Pay`만 다름)을
  `ExpeditionRules`의 한 곳으로 — `CanPlaceOffer`·`PlaceOffer`(보드), `OfferHasRoomInInventory`·`PlaceOfferInInventory`(인벤토리의 첫 자리), `CanPlaceOfferInInventoryAt`·`PlaceOfferInInventoryAt`(고른 자리),
  `OfferMergesAt`·`OfferMergesInInventoryAt`. 전리품의 `TakeLoot…`·`LootMerges…`는 자리(`DropAt`)와 `Pick`을, 상점의 `BuyTo…`·`ShopMerges…`는 코인(`AffordableAt`, 새 helper)과 `Pay`를 얹은 껍질.
  화면의 손 둘(`BattleScreen.DropInHand`·`NodeMapScreen.ShopHand`, 부르는 Manager 메서드만 다름)은 `UI/OfferHand.cs`(`Source.Loot`·`Shop`, 명령을 감싸는 일은 화면이 `Action<Action>`으로 넘김) 하나로.
  `ExpeditionManager`의 공개 표면과 Test는 그대로. 줄: `ExpeditionRules` +1(껍질 −106, 한 몸 +107 — 주석 포함), 화면 둘 −109, `OfferHand` +99: 합쳐서 −9.
  줄 수가 아니라 **규칙의 몸이 둘에서 하나**가 된 것이 목적이다(10-18 `8355907`은 같은 규칙을 전리품·상점에 두 번 고쳤다).
- **R18**: `UiPalette.Party`·`Enemy` 삭제(참조 없음. `Bag`은 `BagRim`이 써서 둠). UI 소스의 해시인 `Assets/@Prefabs/UI/UiPrefabStamp.txt`는 체인의 setup이 다시 썼다(같은 커밋).
- 문서: Architecture/08("격자 보드"의 한 곳 줄, "상점"의 사는 명령), Architecture/12("격자 보드"의 손이 든 것, "상점"의 `OfferHand`). Roadmap(맨 위 요약의 39·40단계와 10-20, 날짜 표).
- 10-12의 미룬 표 갱신(아래).

## 미룬 묶음 (10-12의 표를 갱신. 계기가 오면 그 라운드의 첫 커밋으로)

| # | 무엇 | 계기 | 2026-10-20 |
|---|---|---|---|
| R11 | 이긴 뒤 전투 화면과 `PartySideView`의 손·인벤토리 배선 두 벌(`AimFrom`·`OnHeldPressed`·칸 핸들러·합치기 미리보기), Round 54 뒤 꺼진 그림자 부품, `InventoryGridView`의 `GridGeometry` 사본 | 두 화면의 손을 함께 바꾸는 라운드 | 손 클래스 둘은 R17로 하나(`OfferHand`). 배선은 전투 234줄 vs 파티 206줄, 이름을 맞추면 공통 53/101 — 나머지는 전투 고유(승리 뒤 guard·클록·결정타)라 떼도 고칠 곳이 줄지 않음. 계속 미룸 |
| R13 | `ItemOffer` → `EquippedItem`, 전리품·상점 놓기 짝, `OfferRecord` 식 둘, UI의 `Tier + 1` 셋, 상점 후보 조건 둘 | G18 구현(채택안에 맞는 항목만) | 놓기 짝은 R17로 끝. `ItemOffer` → 아이템/가방 변환은 `NodeMapScreen` 10곳(표시)·Sim `Policies` 6곳(정책)에 남음 — 미룸 |
| R14 | `StarDamage`의 두 뜻(주는 쪽·받는 쪽), `RollCritical`의 필드 반환, 효과 단위 판단 둘(선택) | 별·치명타·효과 어휘를 바꿀 때 | 그대로 |
| R2/R3 | 결정타·붕괴 연출(`StageMoment`)을 `BattleScreen`에서 떼기 | 연출을 바꾸는 라운드 | 그대로(Round 85는 표시라 계기가 아님) |
| R15 | run.json 옛 변환 단계 | — | **해결**: 10-18 "옛 저장은 삭제"로 `OldestReadableVersion = 21`, `RunSaveMigrator` 76줄 |
| R16 | "전투 사이" 판정을 원정 단계 하나로 | 원정 단계가 늘거나 이긴 뒤 흐름을 바꿀 때 | Rules(`ExpeditionPhase` 넷)와 Manager(`GamePhase` 넷)의 두 목록. `Finished`가 있어 `!= InBattle`로 못 줄임. 그대로 |

## Open
- 없음. 40단계(맞추기와 직접 플레이)는 시작 지시를 받아서.

## Verification
- `dotnet build Tools/Sim -c Release` 경고 0·오류 0.
- 시뮬 전후: 편집 전 트리를 scratchpad에 따로 빌드한 것과 편집 뒤를 같은 명령·시드로 — `expedition --policy balanced --runs 1000 --seed 1`(광산 88.8%), `run --policy balanced|safe --runs 300 --seed 1`(78.3 / 84.7%, 08 §46과 같음) —
  출력 264줄이 **byte 단위로 같다**(규칙의 한 몸은 결과를 바꾸지 않았다).
- 체인 `20261020-002609` **CHAIN OK**: setup OK, sim OK(Generated가 Source와 같고 Load됨), EditMode 1028/1028, PlayMode 96/108(건너뜀 12는 그래픽 스크린샷 Test, 10-19의 체인과 같은 수). `Assets/InitTestScene*` 없음.
- `git diff --check` 깨끗. 스크린샷은 뽑지 않았다(화면의 모양을 바꾼 것이 없다: 손의 동작은 PlayMode의 전리품·상점 Test가 지킨다).

## Next Action (제안)
- 40단계(맞추기와 직접 플레이)는 시작 지시를 받아서. 레버는 08 §46의 읽음(정책이 방어구를 누구에게 넣는가, 보호막의 크기)과 §36~§40의 권장(세기 %·성장 등급·방랑자의 HP·치명타 %p), 띠는 Design/04 §8·08 §12.
