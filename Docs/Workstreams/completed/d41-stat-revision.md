# Slice D 41단계: 스탯 개정

Snapshot: 2026-10-20 (완료: 데이터·Data·Domain·저장 24·UI(Round 88 2~4차)·시뮬·Test·문서. 체인 `20261020-125728` CHAIN OK. 2026-10-20 세션 정리에서 주제별 커밋·푸시)

## Goal
사용자의 개정안(Design/17 §1)대로 스탯의 일이 바뀐다: 기준값은 효과마다, 물리/마법과 세기의 축(힘·지식·지혜), 민첩·지혜의 쿨타임, 물리·마법 치명타의 둘과 물리 배수(아이템·특성), 민첩의 명중과 마법의 언제나 명중, 체력의 HP 구간 합(직업 HP 없음), 인내의 피로·각성·빈사, 정신력 → 지혜, 직업 기본값 1~8, 저장 24. 보류: 최대 피로. 다음 걸음: 장비 요구 스탯. 수치는 42단계(맞추기)가 띠 안에 넣는다.

## State
- 2026-10-20 사용자 "스텟류 정정 검토 요청"(개정안 전문은 Design/17 §1) → 검토 보고(대조표, 수치의 크기, 빠진 정의 열한 곳, 판정할 것 열하나) → 판정 **"1. A(직업별 최초 스탯은 최소치 1 최대치 8정도에서 재조정) 2. A 3. A(추후 재조정) 4. C 5. b 6. 피해에만 7. 명중 물리에만 8~11. 권장 / 권장안 대로 수행"** → 새 41단계로 구현(40은 깊은 층의 아이템으로 남고 맞추기는 42).
- 구현·문서·시뮬 전후·레버 스윕 끝. 체인은 아래. 2026-10-20 세션 정리에서 커밋·푸시(40단계 구현 / 41단계 구현 / 기획·Architecture / Round 88 / Roadmap·Handoff).
- 다음 걸음(2026-10-20 사용자 "다음 세션 다음 걸음으로 미룬 것: 장비 요구 스탯(미달이면 장착 불가)"): 다음 세션은 장비 요구 스탯부터(아래 Next Action).

## Done
- 기획: Design/17(제안과 판정의 기록), 00 "Slice D"(판정 표)·결정 줄, 02 §4(세기 축·기준 5·쿨타임·치명타 둘과 배수)·§5(민첩의 명중, 마법의 명중)·§6(인내의 빈사), 04 §3(인내 기준 5, 인내의 각성, 최대 피로 보류), 05 §2(지혜, 스탯의 일 표, 직업 기본값 1~8 표, HP 구간), 07(열과 키), 08 §48(전후와 스윕).
- 데이터: `JobData.csv`(MaxHp 삭제, `Wis`·`GrowthWis`, 기본값 전사 6·4·6·3·4·6 / 바이킹 7·5·5·3·4·4 / 방랑자 5·7·4·3·4·6 / 마법사 3·4·4·8·5·4 / 사제 3·4·5·6·7·5), `ItemData.csv`(ScalesWith 삭제, `School` — 마법: 화염·치유·등불 지팡이, 불씨 플라스크, 감독관의 포효, 심연의 울부짖음; 버클러 물리; `CritDamagePercent` 전투도끼·미늘창·글라디우스 50), `TraitData.csv`(`CritDamagePercent` 노련함 25), `BalanceData.csv`(`PowerBaseline` 5·`FatigueBaseline` 5·`CooldownPercentPerPoint` 1·`HpPerConPointLow/Mid/High` 5/10/15·`HpBandLow/Mid` 10/20·`HitPerDexPoint` 1·`DodgePerDexPoint` 1·`CritPerDexPoint` 1·`CritPerWisPoint` 1·`DogDeathPercentPerEndPoint` 1·`VirtuePerEndPoint` 1·`PhysicalCritDamagePercent`·`MagicCritDamagePercent` 150; `HpPercentPerConPoint`·`HitPerStatPoint`·`VirtuePerSpiPoint`·`DogDeathPercentPerConPoint`·`CritDamagePercent` 삭제), `UI_StaticText.csv`(`Stat.Wis` 지혜/WIS와 스탯 설명, `Item.AlwaysHits`, `Item.Cooldown`·`Item.Hit`의 설명). Generated 다시 만듦(Balance·Item·Job·Trait).
- Data: `Stat.Wis`, `ItemSchool`, `StatBlock.Wis`, `JobData`(HP 없음), `ItemData.School`(무기·공격 아이템은 비우면 물리, 지원·기타는 거절)·`CritDamagePercent`(0..500, 치명타 있는 물리 무기만)·파생 `ScalesWith`·`QuickenedBy`, `TraitData.CritDamagePercent`, `BalanceData`의 키와 검사(기준 셋이 범위 안, 가장 낮은 스탯의 세기 ≥ 1%, 가장 높은 스탯의 쿨타임 ≥ 1%, `HpBandLow` < `HpBandMid`), 매퍼(`JobMapper`·`ItemMapper`·`TraitMapper`).
- Domain: `StatRules` 다시 씀(`PowerPercent`는 `PowerBaseline`, 새 `CooldownPercent`·`CooldownMs`·`MaxHpOf(balance, 체력)`·`CritDamagePercent`, `CanMiss`는 물리만, `Accuracy`는 민첩(아이템 인자 없음), `CritChance`는 학파별, `VirtueChancePercent`·`DeathChanceShift`는 인내, `FatigueGain`은 `FatigueBaseline`; `HpPercent` 삭제), `LevelRules.MaxHpOf`(체력의 합 × 특성 %), `TraitBonus.CritDamagePercent`, `BattleEngine`(`CreateUnit`·페이즈의 쿨타임 보정 `EffectiveCooldown(기본, permille, 스탯 %)`, `RollHit`의 새 `Accuracy`, `RollCritical`의 배수).
- 저장: 24 — `StatsRecord.Wis`(`Spi`는 `int?`로 읽기만), `RunSaveMigrator.From23To24`(로스터·후보의 스탯을 (옛 + 1) / 2로 접고 `Spi` → `Wis`, 원정 구성원의 최대 HP를 새 체력으로·HP 비례·있던 사람은 최소 1), `RunSaveMapper`.
- UI(Round 88 2차 C안, 사용자 지시대로): 카드의 **효과 줄이 맨 위**(`UiPrefabSetup.Kit`의 `Effects` 라벨을 `Facts` 앞에), 쿨다운은 주인의 초(빠르면 `Good`·느리면 `BloodLight`), 물리의 명중 줄, **마법 장비는 명중 줄 없음**, **괄호의 스탯·곱 꼬리 삭제**(`UiText.StatScale`·`UiKeys.Item.StatScale`·`Item.AlwaysHits`와 문구 둘 삭제, `ItemTooltipView`는 별의 꼬리만), `UiKeys.Stat.Wis` 지혜, 스탯 값의 색(`UiText.StatColor`)은 `PowerBaseline`(5) 기준. 3차 "수치는 소수점 첫째 자리에서 반올림" → 4차 사용자 "소수점 둘째 자리에서 반올림하는 걸로 반영"(보여주는 항목에서): "보여지는 화면에서": 카드의 쿨다운 초(`UiText.RoundedSeconds`)와 카드·전투 로그·자막의 치명타·명중 %(`UiText.Percent`, 소수 둘 → 하나)를 소수 첫째 자리까지 반올림(효과 값은 전부터 정수, 전투 시계는 그대로). 틀은 그대로.
- 시뮬: 보고의 스탯 표기 `str/dex/con/int/wis/end`. 전후와 스윕은 08 §48.
- Test: EditMode `StatRulesTests` 다시 씀(기준·학파와 축·쿨타임·HP 구간·치명타 배수·런에서 전투까지·거절), `StatJudgementTests`(민첩의 명중, 물리만 빗나감과 마법의 명중, 학파별 치명타, 인내의 빈사·각성), `StatBattleTests`(지혜의 회복·보호막·화상, 지식의 회복, 쿨타임의 단축, 방어 장비는 그대로), `GrowthTests`·`TraitRulesTests`·`LevelRulesTests`(HP 구간), `RunSaveMapperTests`(24, `Migrate_From23_…`), `StaticDataTransformerTests`(Wis·MaxHp 없음·School·CritDamagePercent·키), `DefinitionTests`·`StaticDataValidationTests`(JobData 생성자), `TestData`(기준 10 유지, 쿨타임 0, HP 점당 0.10, 치유사 체력 6·타격수 8, `Item`의 `school`), `TestCsv`(열).
- 문서: Architecture/05(열과 키), 07(저장 24), 08("스탯"·"판정의 스탯"을 지금 규칙으로 다시 씀), 09(Slice D 표·구현 순서 40/41/42·Acceptance 2~4), 12(지혜·카드의 줄), CLAUDE.md §9(32~42), Roadmap(40 완료·41 진행·42 대기, 요약·날짜 표·사후 검토 대기), Handoff `completed/d40-deep-items.md`.

## Open
- **수치는 띠 밖**(08 §48): 광산 원정 2.9%, 런 균형형 4~6%·안전형 8~12%. 사용자가 고른 출발값의 자리이고 42단계 맞추기가 다룬다. 첫 레버의 스윕(데이터만): 판정의 기준 10 → 7과 깊은 층의 아이템이 가장 센 둘, 묶음(기준 7·HP 7·광산 복리 4%·세기 기준 4)이 균형형 61·안전형 82%(띠 문턱). 선택은 사용자.
- 기준 10의 판정(명중·치명타·회피·각성·빈사)이 출발값에서 모두 음수인 것(사후 검토).
- 장비 요구 스탯(판정 9)은 다음 걸음: 열 `ReqStat`·`ReqValue`, 미달이면 장착 불가(놓기 거부, 카드의 붉은 줄), 기본 무기는 직업 기본값이 충족. 최대 피로(판정 4)는 정신붕괴 개정과 함께.
- Round 88: 1차 A안 → 사용자 2차 지시 → C안 목업(`mock-cards-2.png`)대로 구현했다. 남은 판정 없음(사용자 확인은 직접 플레이에서).
- 방랑자 그림(Round 83)·특성 아이콘은 이미지 API 요청 시.

## Verification
- `dotnet build Tools/Sim -c Release` 경고 0·오류 0. `transform`(Balance·Item·Job·Trait 다시 씀) → `validate` OK.
- 시뮬 전후(08 §48): 저장소 빌드, 같은 시드.
- 체인 `20261020-123532`: setup OK(Prefab·문구 표·글꼴 atlas 다시 씀), sim OK, **EditMode 1041/1041, PlayMode 96/108**(건너뜀 12는 그래픽 스크린샷 Test). 판정만 `leftover`로 FAILED — 그 전에 중간에 멈춘 실행(`20261020-123201`, C안 구현 중 끊음)이 남긴 `Assets/InitTestScene*` 때문이고, 지우고 같은 코드로 다시 돌린 것은 아래.
- 체인 `20261020-125728` **CHAIN OK**(4차 반올림까지 넣은 마지막 코드): setup OK, sim OK, EditMode 1041/1041, PlayMode 96/108(건너뜀 12). `Assets/InitTestScene*` 없음.
- `git diff --check` 깨끗. 스크린샷은 뽑지 않았다(카드의 줄은 PlayMode Test가 지킨다: 효과 라벨이 사실 라벨 앞, 꼬리 없음, 0초 쿨다운 없음).

## Next Action (제안)
- **장비 요구 스탯**(사용자 지시: 다음 세션의 다음 걸음. Design/17 판정 9의 권장안): 아이템에 요구 스탯과 값(`ItemData.csv`의 선택 열 `ReqStat`·`ReqValue`, 예: 전투도끼 힘 7, 장궁 민첩 7, 화염 지팡이 지식 7, 치유의 지팡이 지식 5)을 두고, **미달이면 그 용병의 보드에 놓을 수 없다**(`ExpeditionRules`의 놓기 `Can…`이 거절, 손의 그림자도 거절색; 인벤토리·상점·전리품은 그대로), 카드에 "요구 힘 7"의 사실 줄(미달이면 붉은 핏빛), 직업의 **기본 무기는 그 직업의 기본값이 충족**하도록 값을 잡는다(데이터 검사), 시뮬 정책은 요구를 만족하는 용병에게만 넣는다. 절차는 CLAUDE.md §10: Owner 문서(Architecture/05·08·12)와 기획(02 §4·05 §2)의 설계를 보고하고 승인 뒤 구현, 화면의 줄은 작은 Round. 단계 자리는 권장 **새 42단계 "장비 요구 스탯"**, 맞추기와 직접 플레이는 43(사용자 판정).
- 그 뒤 맞추기(지금의 42단계): 08 §48의 스윕에서 묶음을 고르고(권장: 판정의 기준 7·HP 점당 7·광산 복리 4%·세기 기준 4를 출발점으로, 둘째 묶음의 문턱 21 → 26층을 더 재어 띠 안으로), 직접 플레이(지혜·카드의 줄·깊은 층의 적 보드·우클릭 돌리기·방어구).
