# Round 30 — 적이 쓰러지는 결정타의 강조 (분석과 도입 검토, 2026-10-05)

사용자 지시: "다키스트 던전 이펙트 방식-적이 죽을 때 공격자와 피격자 강조 및 슬로우 모션에 대해 분석 및 우리 게임에 도입 가능성 검토". 호출 없음, 게임은 그대로다.
아래의 안은 모두 【제안】이다. 고르면 목업(MP4)을 먼저 보이고, 승인 뒤 Design/10 §5에 【확정】으로 적고 구현한다.

## 다키스트 던전이 하는 것 (출처 확인)

- **모든 행동에 카메라가 당겨진다**(Darkest Dungeon 1). 기술을 쓰면 공격·치유·강화 모두 줌이 들고, 행동은 대부분 자세 그림을 바꿔 끼우는 것이라 실제 움직임은 적지만 큰 그림과 극적인 줌이 그것을 가린다는 평이 있다 [2][3][4].
  카메라의 움직임은 대본 파일(`scripts/timescript/`)에 근접·원거리·아군 기술·**근접 치명타·원거리 치명타**·결의 시험·함정 등 따로 적혀 있다 [9][10][11]. 각 기술에는 자기 공격 그림이 있다 [5][6].
- **치명타만 따로 강하다**. 공식 설정에는 배경 흐림, 피벗 카메라, 진동(화면 흔들림)이 있고 줌을 끄는 설정은 없다 [1]. 사용자 모드 "Less Combat Zoom"·"Almost No Camera Zoom"은 줌을 빼되 치명타의 것은 남긴다(치명타의 맛 때문) [7][10].
- **적이 죽을 때만의 슬로우 모션은 확인되지 않았다.** 치명타로 죽은 적은 시체를 남기지 않고 [15], 중·대형 적은 자리를 막는 시체를 남긴다 [16]. 내레이터에 적을 죽일 때의 대사가 있다 [17]. 결정타 전용 정지·슬로우의 출처는 찾지 못했다(줌 동안 한 자세가 멈춰 보이는 것이 같은 일을 하는 것으로 보인다).
- Darkest Dungeon II는 전투 속도 설정(최대 1.5배)을 2025년에 더했고, 연출 장면은 1배 그대로다 [23][24]. 전투 연출 도구에 대한 GDC 2024 발표가 있다 [26].
- 미확인(기억): 공격자와 대상이 가운데로 당겨지며 커지는 것, 다른 유닛이 어두워지는지, 길이(초). 글 출처가 없다.

## 결정타에 정지·슬로우를 쓰는 다른 게임 (출처 확인)

- 격투 게임의 **히트스톱**: 맞는 순간 공격자와 피격자가 함께 잠깐 멈춘다. 센 공격일수록 길다 [31][32]. 사쿠라이 마사히로 "큰 순간에는 멈춰라" [34].
- 스매시브라더스 얼티밋의 **Special Zoom / Finish Zoom**: 큰 타격과 판을 끝내는 일격에 멈춤·슬로우·줌. 끌 수 없다 [33].
- Vlambeer의 "sleep": 처치·피격·폭발에 한두 프레임 멈춘다 [35]. 하데스: 보스 둘에 "마지막 일격 연출"을 더했다(패치 노트) [36]. 페글: 마지막 주황 핀에 줌과 슬로우 [38]. 스나이퍼 엘리트의 킬캠: 5편에서 빈도를 줄이거나 끈다 [39].
- 슬레이 더 스파이어, 인투 더 브리치, 백팩 배틀: 처치 전용 연출은 찾지 못했다(속도·흔들림 설정만) [40][41][42].

정리: "적이 죽는 순간 둘을 강조하고 느려지는" 연출은 다키스트 던전의 줌(행동마다, 치명타에 강하게)과 스매시·하데스·페글의 결정타 연출(판을 끝내는 일격에만)을 합친 것이다.

## 우리 게임과의 차이

1. **실시간 오토 전투다.** 다키스트 던전은 한 번에 한 행동(턴제)이라 행동마다 멈춰도 된다. 우리는 여덟 유닛이 각자 쿨다운으로 발동해 1초에 몇 번씩 무언가 일어난다(02 §1). 행동마다의 줌은 화면을 쉴 새 없이 흔든다. **결정타에만** 쓰는 것이 맞다(사용자의 제안과 같다).
2. **결정론과 시간.** 연출은 결과를 바꾸지 못하고(09 §5), 전투 시간이 얼마나 빨리 흐를지는 화면이 정한다(Architecture/11: 배속·정지). 슬로우 모션은 "잠깐의 느린 배속"이라 결과·저장·이어하기·시뮬에 영향이 없다.
   다만 전투 시계는 Unity의 시간 배율과 따로 간다(`BattleScreen`이 `Time.unscaledDeltaTime`으로 `BattleClock`을 돌린다). 시계(전투)와 움직임(돌진·밀림·숫자·잔상)을 **함께** 느리게 해야 맞는다.
3. **공격자를 알 수 있다.** 사망 기록(`Died`)에는 공격자가 없지만 바로 앞의 피해 기록(`Damaged`, 같은 시각)에 공격자와 아이템이 있다. 화상·폭풍으로 죽으면 공격자가 없다.
4. **재료가 이미 있다.** 공격·피격 자세(용병 여섯, 몬스터 다섯. Round 23~28), 공격하는 유닛을 맨 앞에 그리기(Round 28), 무덤 동안 자리를 붙잡기(Round 21). 새 그림이 필요 없다.
5. **지금 적이 쓰러지면**: 잔상이 0.55초에 가라앉으며 사라지고 "쓰러짐"과 흔들림, 뒤의 적이 바로 걸어 들어온다. 무덤을 정할 때 사용자가 몬스터는 "다르게 처리 예정"이라 했다(Design/10 §5). 이번이 그 자리다.
6. 빈도: 한 전투의 적은 1~4. 결정타 연출은 전투마다 1~4번이다.

## 안 (【제안】)

| 안 | 무엇 | 비용 | 위험 |
|---|---|---|---|
| **A 결정타 슬로우 (권장)** | 적이 유닛의 아이템에 쓰러지는 순간 0.5초(x1의 실제 시간) 동안 전투와 움직임이 25% 속도. 공격자(공격 자세)와 쓰러지는 적(피격 자세, 슬로우가 끝날 때까지 보인다)을 맨 앞에, 나머지 무대는 어둡게(약 45%). 끝나면 지금처럼 잔상과 "쓰러짐". 마지막 적·보스는 0.8초와 큰 흔들림 | 연출 계층만, 새 그림 없음. 약 하루(목업 뒤) | 피로(전투당 1~4번, x1에서 +1~2초), 결과 창이 0.5~0.8초 늦음 |
| B A + 줌 | A에 더해 무대가 두 유닛 사이를 중심으로 1.08배로 당겨졌다 돌아온다(다키스트 던전의 카메라) | 무대(배경·빛·전장)를 한 부모로 묶는 구조 변경. +반나절~하루 | 화면 좌표로 놓는 연출(양초의 빛, 떠오르는 숫자)의 어긋남 |
| C 히트스톱만 | 결정타에 0.1초 정지(전투와 움직임) + 쓰러지는 적의 흰 번쩍임 + 흔들림 | 가장 작다. 반나절 | 짧아서 강조가 약할 수 있다 |
| D 행동마다 줌 | 다키스트 던전 그대로 | — | 실시간 오토 전투와 맞지 않는다(권장하지 않음) |

A의 세부(권장안, 목업에서 확인): 화상·폭풍으로 쓰러지면 연출 없음(지금처럼). 같은 시각에 여럿이 쓰러지면 한 번으로 묶어 모두 강조. 배속 x2는 시간 절반, x4는 끔. 슬로우 동안에도 포션은 쓸 수 있다(전투 시간 기준 입력이라 그대로).
화면을 닫으면 속도를 되돌린다. 용병의 사망은 지금의 무덤 그대로(이번 범위 밖).

## 다음 단계 (제안)

- 같은 전투의 결정타 순간을 지금 / A / B / C로 나란히 보이는 MP4 목업(느린 재생 포함. 호출 없음).
- 승인 뒤: Design/10 §5에 【확정】, `BattleClock`(느린 배율), `BattlePresenter`(결정타 찾기), `BattleScreen`(쓰러진 적을 잠깐 붙잡기, 열 순서), `BattleFxLayer`(무대 어둡게), Test(결정타에만, 화상·폭풍 제외, 시간 복구, 배속 비례), Architecture/12 "연출".

## 목업 (사용자 "A/b 만 목업 만들어줘", 호출 없음)

- 무대: `run_stage_shots.sh <scratch>`가 프로젝트를 복제해 보스 전투의 시작(멈춤)을 찍고, 유닛을 모두 숨겨 다시 찍는다(`game/ko_30_stage_empty.png`: 배경, 양초의 빛, 비네트. 비교용 `ko_30_stage_full.png`). 작업 트리는 그대로.
- `mock_kill_moment.py`: 그 무대에 Round 23의 파티(카이·엘라·세드릭·아스트리드, 4~1열)와 고블린 둘(1열 약탈자 15/80, 2열 주술사)을 Round 29의 발밑 표시와 함께 세운다. 0.4초에 아스트리드의 대도끼가 약탈자를 쓰러뜨린다(-27, 큰 피해).
  움직임은 게임의 수치다(돌진 0.1+0.2초와 공격 자세, 밀림 0.26초와 피격 자세, 절반의 붉은 번쩍임 0.28초, 숫자 84 오름, 흔들림 초당 36 감쇠, 잔상 0.55초 26 가라앉음, 걷기 0.35초). 그림은 게임처럼 줄여 그리고(밉 수준) 화면 비네트를 씌운다.
  그림의 자리와 크기는 게임의 같은 장면(`ko_30_stage_full`)과 맞춰 봤다.
- `mock-kill.mp4`(1920x536, 30fps, 약 27초): 지금 · A · B를 보통 속도로 두 번씩, 이어서 셋을 절반 속도로 한 번씩. `mock-steps.png`: 맞은 직후(+0.05초) · 0.25초 뒤 · 0.7초 뒤.
- 목업에서 정한 세부(판정 때 바꿀 수 있다): 맞는 순간의 숫자(-27)는 그때 뜨고(느리게 오른다), "쓰러짐"과 큰 흔들림은 슬로우가 끝날 때 잔상과 함께. 어둠은 0.08초에 들고 끝 무렵 0.12초에 걷힌다.
  어둠은 무대의 상자(헤더 아래부터 패널 위까지)에만 들고 포션 띠도 같이 어두워진다(게임에서는 전장 안의 덮개로 두 유닛의 열만 그 위에 그리는 방식). B의 줌은 두 유닛의 가운데, 몸 가운데 높이를 중심으로 0.12초에 들어가 끝에서 0.2초에 돌아온다.
- 판정할 것: A / B, 그리고 수치(슬로우 길이 0.5초, 속도 25%, 어둠 45%, 줌 1.08배).

## 첫 판정과 줌 목업 (2026-10-05, 사용자)

"1. B로 가자 2. 1.08배 줌이였는데 줌을 1.12배, 1.16배 한 목업도 보여줘. 또한 추가로 원래 공격시 화면 떨림이 있는데 이 슬로우 모션시에는 화면 떨림은 빼는 게 좋을 것 같아"
(이어서 "어둠 수치도 60%로 증가") — **B로 정함.** 줌의 배율은 목업을 보고 정한다.

- `mock_kill_moment.py zoom` → `mock-kill-zoom.mp4`(B 1.08 · 1.12 · 1.16배를 보통 속도로 두 번씩, 이어서 셋을 절반 속도로 한 번씩), `mock-steps-zoom.png`. 호출 없음.
- 셋 모두 결정타 연출의 떨림이 없다: 맞는 순간의 큰 피해 떨림(6)과 쓰러질 때의 떨림(10)을 뺐다. 다른 떨림(결정타가 아닌 큰 피해, 빈사, 폭풍)은 그대로다. 어둠은 60%(첫 목업 45%).
- 판정할 것: 줌 1.08 / 1.12 / 1.16배. (그 밖의 수치: 슬로우 0.5초, 25%, 어둠 60%.)

## 판정과 구현 (2026-10-05, 사용자 "1.1배로 나머지 수치는 이대로 구현 시작")

**B, 줌 1.1배**, 슬로우 0.5초 동안 25%, 어둠 60%, 결정타의 떨림 없음. 기획은 Design/10 §5 "결정타"(【확정】), 화면의 규칙은 Architecture/12 "연출"의 "결정타".

- `BattlePresenter`: 적의 `Died` 바로 앞의 `Damaged`가 유닛에게서 온 아이템의 것이면 결정타(화상·폭풍·패시브는 아님). 그때는 큰 피해 떨림을 빼고, 잔상·"쓰러짐"·떨림 대신 화면에 넘긴다.
- `BattleClock.SlowPercent`: 고른 배속은 그대로 두고 전투 시간을 잠깐 느리게(25%).
- `BattleScreen`: 결정타(`Kill*` 상수, `KillMoment`): 실제 시간 0.5초(x2 절반, x4 없음) 동안 시계와 `Time.timeScale`이 25%, 쓰러진 적을 무대에 붙잡고 적이 자리를 지킨다. 어둠(`KillDark`)을 전장의 Column 사이에 두어 둘의 Column만 그 위에(친 유닛이 맨 위).
  무대의 두 층(`StageBack`: 배경과 양초의 빛, `StageFront`: 전장)을 두 그림의 가운데를 중심으로 1.1배(배율과 그만큼의 이동). 0.5초가 지나면 잔상과 "쓰러짐", 줌이 돌아온 뒤 결과 창. 화면이 닫히면 시간 배율을 되돌린다.
- `BattleFxLayer`: 잔상과 무덤이 그림이 보인 크기를 따른다(줌 안에서 생겨도 크기가 튀지 않게).
- 어둠의 알파: 목업은 화면 색(sRGB)에서 섞어 60%가 밝기 40%였다. 게임의 UI는 선형 색공간에서 섞어 알파 0.6이면 밝은 색이 약 3분의 2로만 어두워져(스크린샷에서 211 → 139) 목업보다 옅었다. 같은 어둠이 되게 알파를 0.87(1 - 0.4^2.2)로 했다.
- Prefab: 배경과 빛을 `StageBack`에, 전장을 `StageFront`에 넣고 전장에 `KillDark`. Test의 경로가 `Frame/StageFront/Field/…`, `Frame/StageBack/Background`로 바뀌었다.
- Test: `BattleClockTests` 둘, `Battle_AnEnemyFelledByAUnitsItem_IsAKillMoment`, `Battle_AtX4_AnEnemyFallsWithoutAKillMoment`, `Battle_AnEnemyTheStormKills_IsNoKillMoment`.
  전투를 끝낸 뒤 결과 창을 누르는 곳은 `UiTestUtil.WaitForResult`로, 적이 비운 행을 보는 곳은 `WaitForTheKillMoment`로 기다린다. 스크린샷 `ko_29_battle_kill_moment`(보스의 결정타 0.25초 뒤)를 더했다.
- 맡긴 범위의 권장안(사후 검토): 화상·폭풍·패시브 제외, 같은 때 여럿은 한 번에, 결정타 동안 또 쓰러지면 더함(시간은 그대로), x2 절반·x4 없음, 정지해도 흐름, 포션 사용 가능, 결과 창은 결정타 뒤, 용병은 무덤 그대로.
  제안의 "마지막 적·보스는 0.8초"는 목업에 없어 넣지 않았다(모든 결정타가 0.5초).
- 확인: 체인(EditMode 628, PlayMode 56 + 스크린샷 8 건너뜀)과 스크린샷 45장. 처음의 스크린샷에서 어둠이 보이지 않았던 것은 두 가지였다: 화면을 열 때 읽은 위치가 아직 배치 전이었다(→ 배치의 수치로 계산),
  그리고 알파 0.6이 선형 색공간에서는 옅었다(→ 0.87). 결과: `game/final_ko_29_battle_kill_moment.png`(보스의 결정타 0.25초 뒤), `final_ko_15_battle_after_advance.png`(끝난 뒤).

## 출처

1 https://darkestdungeon.wiki.gg/wiki/Pause_Menu · 2 https://www.gamegrin.com/previews/darkest-dungeon-preview/ · 3 https://www.rpgfan.com/review/darkest-dungeon/ · 4 https://www.dualshockers.com/darkest-dungeon-review-hello-darkness-my-old-friend/ ·
5 https://steamcommunity.com/sharedfiles/filedetails/?id=660036889 · 6 https://steamcommunity.com/app/262060/discussions/0/3196993831808293130/ · 7 https://steamcommunity.com/app/262060/discussions/4/618453594742852838/ ·
9 https://steamcommunity.com/sharedfiles/filedetails/?id=2263872101 · 10 https://steamcommunity.com/sharedfiles/filedetails/?id=1291421233 · 11 https://steamcommunity.com/app/262060/discussions/0/612823460256390354/ ·
15 https://darkestdungeon.wiki.gg/wiki/Critical_Hit_(Darkest_Dungeon) · 16 https://darkestdungeon.wiki.gg/wiki/Corpse · 17 https://darkestdungeon.wiki.gg/wiki/Narrator_(Darkest_Dungeon) ·
23 https://www.rpgsite.net/news/17938-darkest-dungeon-ii-steadfast-stewards-update-patch-notes-pc-mac-ps5-switch-xbox-later · 24 https://darkestdungeon.wiki.gg/wiki/Steadfast_Stewards_Retail_Release · 26 https://gdcvault.com/play/1034466/Independent-Games-Summit-Critical-Combat ·
31 https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/ · 32 https://www.ssbwiki.com/Hitlag · 33 https://www.ssbwiki.com/Special_Zoom · 34 https://www.youtube.com/watch?v=OdVkEOzdCPw ·
35 https://www.bluetengu.com/2014/12/12/art-of-screenshake-experiments/ · 36 https://www.supergiantgames.com/blog/hades-welcome-to-hell-update-patch-notes/ · 38 https://en.wikipedia.org/wiki/Peggle ·
39 https://worthplaying.com/article/2022/5/18/news/132019-sniper-elite-5-shows-off-enhanced-kill-cam-accessibility-features-screen-trailer/ · 40 https://steamcommunity.com/app/646570/discussions/0/2561864094348571914/ ·
41 https://nintendoeverything.com/into-the-breach-update-out-now-on-switch-version-1-2-77-patch-notes/ · 42 https://steamcommunity.com/app/2427700/discussions/0/3883851663709490158/
(번호는 조사 보고의 번호. 동영상과 일부 사이트는 직접 볼 수 없어 슬라이드 글, 위키, 패치 노트, 토론 글로 확인했다.)

## MP4 목업 (2026-10-05)

이 라운드의 MP4 목업은 사용자 지시("목업 mp4는 모두 삭제 해줘")로 지웠다. 휴지통의 `F1-mockup-mp4-20261005/30-kill-moment/`에 옮겨 두었다. 다시 보려면 `mock_kill_moment.py`로 만든다(호출 없음). Git에는 올린 적이 없고, 앞으로 만드는 MP4도 올리지 않는다(`.gitignore`).
