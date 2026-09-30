# Stage 08 — 저장/이어하기

Snapshot: 2026-09-30 (완료)

## Goal

전투 도중을 포함해 어느 시점에 앱을 끝내도 이어할 수 있다. 이어하기로 확정된 손실을 되돌릴 수 없다.

## Done

- DTO: `Assets/@Scripts/Save/Models/RunSaveData.cs` (런, 원정, 전투 기록. 문자열과 수만 가진다).
- 변환과 검증: `F1.Flow.RunSaveMapper`. 읽을 때 Static Data에 비춰 전부 검증하고, 하나라도 어긋나면 파일 전체가 무효다.
- Schema Version: `F1.Flow.RunSaveMigrator` (지금은 버전 1 하나. 단계 함수를 넣을 자리만 있다).
- `RunManager`: `Load`, `Save`(internal), `RetrySave`, `IsSaveBlocked`, `LoadStatus`, 이벤트 `SaveBlockedChanged`. 생성자가 `SaveManager`를 받는다.
- `ExpeditionManager`: 명령마다 `Commit`, `Restore`(저장된 원정과 전투 복원), `LastResume`.
- Boot: BOOT-09(런 Load와 원정 복원). 파일이 없거나 못 읽어도 Boot는 계속된다.
- UI: `SaveErrorOverlay`(Prefab, Addressables `ui/app/save-error-overlay`), 타이틀의 Save 상태 알림, 이어한 전투는 정지 상태로 연다.
- Owner 문서: `Docs/Architecture/07_SAVE.md` "run.json" 절. CLAUDE.md §4(의존 방향)와 §6(Save 불변조건) 갱신.

## 결정 (권장안, 일괄 승인 범위) — 사후 검토 대상 (G7)

- 파일 하나(`run.json`)에 런과 원정을 같이 담고 `RunManager`만 쓴다. 귀환 정산처럼 둘이 같이 바뀌는 변경이 한 번에 확정된다.
  그래서 `ExpeditionManager`는 `SaveManager`에 의존하지 않는다(CLAUDE.md §4를 고쳤다).
- 전투 중의 상태를 통째로 저장하지 않는다. 받아들여진 입력과 확정 시각만 저장하고, 이어할 때 재현한다.
- 전투 중 저장 시점: 입력(실패한 후퇴 포함), 아군의 빈사와 사망, 전투 종료. 시간이 흐르는 것만으로는 저장하지 않는다.
  그래서 끄고 켜면 마지막 저장 시점으로 돌아가 다시 진행하지만, 그 사이에는 확정된 일이 없다.
- 빈사 진입도 저장한다. 빈사가 된 뒤 끄고 켜서 "빈사가 되기 전"으로 돌아가는 것을 막기 위해서다.
- 저장 실패: 명령은 돌아오지만 다음 변경을 전부 막는다. 같은 Snapshot만 재시도한다. 막힌 채 끝내면 그 변경은 없던 일이다.
- 읽을 수 없는 파일: `.bak`으로 복구를 시도하고, 그것도 안 되면 런 없이 시작한다. 타이틀이 알린다.
- 규칙이나 데이터가 바뀌어 전투를 정확히 재현할 수 없으면, 지금 규칙으로 재현한 결과를 쓴다. 입력 기록이 성립하지 않으면 그 전투만 처음부터 다시 한다.
- 정산 보고는 저장하지 않는다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 460/460, PlayMode 35/35(스크린샷 2개 제외).
- EditMode: 단계마다 다시 시작해 같은 상태가 돌아오는지, 매 단계 다시 시작해도 끝 결과가 같은지, 저장된 사망을 되돌릴 수 없는지,
  실패한 후퇴가 다시 굴려지지 않는지, 망가뜨린 파일 50가지가 거부되는지, 저장 실패 때 막힘과 재시도.
- PlayMode: 로비에서 끄고 이어하기, 전투 중에 끄고 이어하기, 저장 실패 Overlay, 읽을 수 없는 파일 알림.

## 알아둘 것

- 상태를 바꾸는 새 명령을 넣을 때: `_run.RequireWritable()`로 시작하고 `Commit()`으로 끝낸다.
- DTO에 Field를 넣을 때: `RunSaveMapper`의 양방향 변환과 검증, `RunSaveMapperTests`의 왕복과 거부 사례를 같이 고친다.
  구조가 바뀌면 `RunSaveData.CurrentSchemaVersion`을 올리고 `RunSaveMigrator`에 단계를 넣는다.
- Save 파일 위치: `<persistentDataPath>/Saves/`. macOS에서 Editor로 돌리면 `~/Library/Application Support/funitup/F1/Saves/`,
  Build한 Player는 `~/Library/Application Support/com.funitup.f1/Saves/`다. Test는 임시 폴더를 쓴다.
- Player Build 확인(1회, 수동): macOS Mono Build가 성공했고, Build한 Player가 Boot를 끝까지 통과했다(Addressables, Localization, Data, UI).
  Build 단계는 체인에 없다. Build는 `Assets/Settings/UniversalRP.asset`, `ProjectSettings.asset`을 건드리고
  `Assets/AddressableAssetsData`에 `link.xml`, `OSX/` 등을 만든다. 무엇을 Git에 둘지는 Build 단계를 만들 때 정한다.
- 파일을 직접 고치거나 지워서 되돌리는 것은 막지 않는다.
