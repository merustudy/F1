# Stage 07a, 07b — 규칙 명세, Domain, 시뮬

Snapshot: 2026-09-30 (완료)

## Goal

Slice A의 규칙이 기획문서에 확정돼 있고, 그 규칙이 순수 C# Domain으로 구현돼 있고, 같은 코드를 Unity 밖 시뮬이 돌려 수치를 낸다.

## Done

- 규칙(7a): `Docs/Design` 02, 03, 04, 05, 07, 09에 `【확정】 (일괄)`로 기록. 검토용 목록은 `Docs/Design/00_INDEX.md`.
- 데이터: `Assets/@Data/Source`의 CSV 9종(Balance, Job, Item, Potion, Enemy, EnemyGroup, Affinity, Dungeon, Mercenary)과 Generated JSON.
  Definition은 `Assets/@Scripts/Data/Definitions`, Mapper는 `Assets/@Scripts/Editor/Data/Transform/DefinitionMappers.cs`.
- Domain(`Assets/@Scripts/Gameplay`): `Random/Pcg32`, `Battle/BattleEngine`과 관련 Type, `Expedition/ExpeditionRules`와 `MapGenerator`,
  `Run/RunRules`. Owner 문서 `Docs/Architecture/08_GAMEPLAY_DOMAIN.md`.
- 시뮬(`Tools/Sim/src`): `Program`, `Simulations`(battle, expedition, trace), `Policies`(none, balanced, safe).
- Test: EditMode 300개. 규칙 Test는 `Assets/@Tests/EditMode/Support/TestData.cs`의 작은 데이터를 쓴다.
- 시뮬 보고: `Docs/Design/08_Simulation_Report.md` §5.

## 결정 (권장안, 일괄 승인 범위)

- G16: Slice A 규칙 전부. G8: 정수 밀리초, 고정 처리 순서, PCG32 스트림, 정수 수치.
- 난수는 전투에서 사망 판정과 후퇴 판정에만 쓴다. 타깃은 전부 결정적이다.
- 입력은 그 시각의 자동 사건 뒤에 적용한다. 받아들여진 입력만 기록한다.
- Definition을 JSON으로 직접 직렬화한다(별도 Record Type 없음). Load가 생성자 검증을 다시 거친다.
- 맵의 갈림길 확률도 상수라 `BalanceData`에 넣었다(`MapBranchChancePercent`).
- 콘텐츠(직업 수치와 패시브 넷, 아이템 19종, 적 5종, 던전 1개, 용병 6명의 이름)는 새로 만든 출발값이다.

## 시뮬로 잡은 출발값

- 기본 파티(knight, bishop, spellblade), 짧은 던전: balanced 정책 클리어 81.7%, 원정당 사망 0.22명. 입력 없음은 77.5%, 1.24명.
- 화상 절벽: 적의 화상 부여가 1초에 1중첩을 넘으면 결과가 급변한다. 적 주술사의 화상 계수를 30%로 두었다(등급 10에서 3중첩).
- 위험이 보스에 몰려 있다. 1~3층은 기본 파티에게 안전하다. 플레이테스트에서 볼 지점이다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 300/300, PlayMode 14/14.

## 알아둘 것

- 밸런스 작업: CSV 수정 -> `dotnet run --project Tools/Sim -- transform` -> `expedition`/`battle`/`trace`. Unity가 필요 없다.
- zsh에서는 공백이 든 명령을 변수에 넣어 실행하면 단어가 나뉘지 않는다. 함수로 감싼다.
- 새 효과 어휘(Enum)를 넣을 때는 Definition 생성자의 허용 조합과 `BattleEngine`의 처리를 함께 고친다.
