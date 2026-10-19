# Slice D 38단계: 얻은 특성

Snapshot: 2026-10-19 (완료: 구현·시뮬·체인 OK·목업 대조. 2026-10-19 세션 정리에서 커밋·푸시)

## Goal
원정이 살아 돌아온 용병에게 특성을 남긴다(Design/05 §6, 38단계 판정 7개): 정산에서 생환자마다 계기(클리어·후퇴, 빈사, 고통·각성)를 굴리고, 얻은 것을 로스터에 더하고, 정산 화면이 "새 특성" 줄로 보인다(Round 84 판정 7). 범위는 Architecture/09 "Slice D" 구현 순서 7.

## State
- 2026-10-19 상태 복구 뒤 사용자 **"판정할 것 7개 권장안 대로"**로 시작했다(10-19 아침에 보고한 설계 그대로: 확률 15·15·25·40·40%, 빈사의 장점 1/3, 새 짝 없음, 계기마다 따로, 런의 끝도 굴림, 정산 칩의 우클릭 카드, 저장 23).
- 구현·시뮬·문서 끝. 체인은 아래 Verification. 2026-10-19 세션 정리에서 주제별로 커밋·푸시.
- 곁가지(38단계 밖, 따로 커밋): 같은 날 사용자 보고 "기본 롱소드로 조합된 롱소드2성과 상점에서 파는 롱소드2성이 조합 안되는 현상" → 확인 결과 수정 전 코드(`4cb4076`, 08:58~09:17의 플레이)의 현상이고 지금 코드는 합친다. 회귀 Test `TierRulesTests.ABaseWeaponAtTwoStars_TakesTheShopsTwoStar_IntoAThreeStarBaseWeapon_EveryWay`만 더함(Roadmap 날짜 표).

## Done
- 데이터: `TraitData.csv`에 계기 열 `Source`와 얻은 일곱(노련함 `seasoned` Clear·패배의 기억 `shaken` Retreat·구사일생 `survivor`·흉터 `scarred`·절뚝임 `limp` DeathsDoor·악몽 `nightmares` Affliction·단련 `hardened` Virtue, 가중치 10), `BalanceData` `TraitChanceClearPercent` 15·`RetreatPercent` 15·`DeathsDoorPercent` 25·`AfflictionPercent` 40·`VirtuePercent` 40. Generated 다시 만듦(`Tools/Sim transform`).
- Data: `TraitSource`(DataEnums), `TraitData.Source`(얻은 것은 필수, 타고난 것은 없음 — 생성자 검사), 매퍼의 `Source` 열, `BalanceData` 키 다섯.
- Domain: `RngStream.Trait`(10), `ExpeditionMember.DeathsDoor`(`CompleteBattle`이 로그의 `DogEntered`로 켬), `TraitRules.SourcesOf`·`ChancePercent`·`AcquiredStream`·`DrawAcquired`, `RunRules.Settle`(경험치 다음, 로스터에 더함), `SettlementReport.NewTraits`(`AcquiredTraits`).
- 저장 23: `MemberRecord.DeathsDoor`, 매퍼 양쪽, `From22To23`(버전만: 없는 필드는 거짓).
- 시뮬: `run` 보고에 계기를 만난 생환자·얻은 수·끝에 산 용병의 얻은 장점/약점. 정책 그대로. 08 §45.
- 화면: `SettlementScreen`의 "새 특성" 줄 넷(`_newTraitRows`·`_newTraitLabels`·`_newTraitLines`, `ShowReady` → `ShowLines`: 줄마다 패널 46), 빌더 `UiPrefabSetup.Settlement`(뼈색 마름모·글·특성 줄 칩 셋·`SettleTraitTooltip`), `UiText.TraitChips(data, ids)`(직업 없이)와 얻은 특성 카드의 계기 줄(`Trait.SubFrom`·`Trait.From*` 다섯), `Settle.NewTraits`. 문구 일곱.
- Test: EditMode 새 `AcquiredTraitTests`(6), `ShippedDataTests`(얻은 일곱·확률), `StaticDataValidationTests`(계기 열), `StaticDataTransformerTests`(`Source`), `RunSaveMapperTests`(23의 빈사 표시·22 → 23, 버전 기댓값 23), `TestData`(키 0, 계기마다 하나 + 빈사 둘 + `grit_t`↔dull)·`TestCsv`; PlayMode `OneLap_…`의 정산(보고의 줄, 꾸민 네 줄·카드), 스크린샷 `_77_settlement_new_traits`·`_78_settlement_new_traits_four`.
- 문서: Design/05 §6(판정 7개 【확정】과 구현 줄)·07·08 §45, Architecture/05·07·08("얻은 특성", 시드 표)·11·12("특성의 표시"의 정산 줄과 카드의 계기 줄), Roadmap.

## Open
- 목업과 다른 것 하나(사후 검토): 목업은 새 줄을 [확인] 위 빈자리에 넣었고, 구현은 "레벨 업 가능" 줄처럼 패널을 줄마다 46 키운다.
- 39단계(맞추기와 직접 플레이)는 시작 지시를 받아서. 입력: 안전형 런 84~87%(띠 위), 끝난 날 32~35일(띠 아래), 끝에 산 용병의 얻은 장점 0.37·약점 0.42(균형형).
- 방랑자 그림(Round 83)·특성 아이콘(얻은 일곱 포함)은 이미지 API를 요청받으면.

## Verification
- `dotnet build Tools/Sim` OK, `validate` OK. 시뮬: 확률을 0으로 둔 복사본(scratchpad)이 08 §44의 승률을 그대로 냄(다른 난수 무관), 전후는 08 §45.
- 체인 `20261019-162844` CHAIN OK(EditMode 1017/1017, PlayMode 95/106 — 건너뜀 11은 그래픽 스크린샷 Test). 첫 실행에 통과.
- 곁가지의 회귀 Test: EditMode `20261019-165850` 1018/1018.
- 스크린샷 `20261019-traits38`(11/11, PNG 135): `ko_77`을 Round 84 `mock-settlement.png`와 대조 — 요소가 같다(`ArtPipeline/Archive/84-trait-display/impl-settlement.png`). `ko_78`·`en_78`(네 줄 + 레벨 업 줄)은 패널이 프레임 안(위 45, 아래 1035)이고 영어도 줄에 들어간다.

## Next Action (제안)
- 39단계(맞추기와 직접 플레이)는 시작 지시를 받아서. 다음 플레이에서 기본 무기와 산 무기의 합치기를 다시 보고(지금 코드는 합친다), 정산의 "새 특성" 줄을 실제로 본다.
