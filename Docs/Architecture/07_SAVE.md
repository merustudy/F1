# 07. Save

Save 파일의 형식과 I/O, Backup과 복구, Migration, 저장 시점을 소유한다.

## 파일

경로는 `<persistentDataPath>/Saves/`다. 파일마다 같은 Directory에 `.tmp`와 한 세대 `.bak`이 있다.

| 파일 | 내용 | 주인 |
|---|---|---|
| `settings.json` | 전역 설정 (Locale) | `SettingManager` |
| `run.json` | 진행 중인 런 하나: 런 상태, 원정 진행 상태, 진행 중인 전투의 재현 기록 | `RunManager` (8단계에서 추가) |

저장 슬롯은 하나다. 새 런은 기존 `run.json`을 덮어쓴다.

## 책임

- `SaveManager`는 형식과 I/O만 한다: 직렬화, Atomic Write, Backup, 손상 복구.
- 상태를 적용하는 것은 그 상태의 주인이다. `SaveManager`는 Gameplay를 모른다.
- Save DTO는 Runtime State나 Domain Type이 아니다. DTO는 `Assets/@Scripts/Save/Models`에 두고 주인이 변환한다.
- DTO는 `SchemaVersion`을 가진다. 버전이 오르면 주인이 단계별 Migration 함수를 둔다.

## Atomic Write와 Backup

```text
같은 Directory에 .tmp 쓰기 -> Flush -> 기존 파일이 있으면 Replace(기존 파일은 .bak이 된다), 없으면 Move
```

- `.tmp`는 Load 후보가 아니다. 저장소를 초기화할 때 남아 있는 `.tmp`를 지운다.
- Load는 Primary를 읽고 검증한다. 실패하면 `.bak`을 읽고 검증한다.
- `.bak`으로 복구했으면 Primary를 `.bak` 내용으로 다시 쓴다. 이때 `.bak`은 건드리지 않는다(손상된 Primary가 정상 `.bak`을 덮지 않게).
- 둘 다 읽을 수 없으면 `Corrupt`다. 어떻게 할지는 주인이 정한다. 설정은 기본값으로 되돌린다.

## 저장 시점과 순서

```text
명령 검증 -> 상태 변경 -> Stable State -> 직렬화 Snapshot -> Atomic Save 성공 -> Typed Event -> UI/연출
```

- 상태가 확정될 때마다 저장한다. Build, Quit, Pause 시점의 마지막 저장에 의존하지 않는다.
- 확정된 손실(용병의 사망)은 이어하기로 되돌릴 수 없어야 한다.
- 저장에 실패하면 그 변경은 확정되지 않은 것이다. 주인은 상태를 되돌리거나(설정), 같은 Snapshot의 저장이 성공할 때까지
  다음 변경을 막는다(런). 실패를 무시하고 진행하지 않는다.
- Test는 전용 Save Root를 쓴다. 실제 Save 폴더를 건드리지 않는다.

런과 원정의 저장 시점, 이어하기의 상세는 8단계에서 이 문서에 추가한다 (G7).

## Validation

- 새 Save 파일이 위 표에 있고 주인이 하나인가?
- 저장 실패 경로에 Test가 있는가?
- DTO를 Domain이나 Runtime State로 쓰지 않는가?
- Test가 실제 Save 폴더를 쓰지 않는가?

## Deferred and Forbidden

- Deferred: 여러 저장 슬롯, 클라우드 저장, 암호화.
- Forbidden: `.tmp`를 Load 후보로 쓰기, 종료 시점 저장에 의존, 저장 실패를 Warning으로 넘기기, `PlayerPrefs`에 진행 상태 저장.
