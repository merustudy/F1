# Unity 2D Starter Kit (C1에서 추출)

C1(Unity 6000.3, 2D/URP, 1인 개발)에서 실제로 돌아가는 구조 중 **게임 내용과 무관하게 재사용할 수 있는 부분**만
뽑은 문서 묶음이다. 새 프로젝트 저장소에 이 폴더를 통째로 복사한 뒤 아래 순서로 적용한다.

- 추출 기준일: 2026-09-30, C1 `feature/vs-b03-symbol-art` @ `481409c`
- 표기: `<P>`는 새 프로젝트의 Prefix다(C1의 `C1`에 해당. 예: Namespace `<P>.Core`, asmdef `<P>.Runtime`).
- 이 문서들은 **제안**이다. 새 프로젝트에서 확정한 뒤 `CLAUDE.md`와 `Docs/Architecture/`로 옮기면 그때부터 규칙이 된다.

## 구성

| 파일 | 내용 | 요청 항목 |
|---|---|---|
| [`CLAUDE.template.md`](CLAUDE.template.md) | 새 프로젝트 root `CLAUDE.md` 초안 (게임 고유 규칙은 빈칸) | 추가 제안 |
| [`01_PROJECT_STRUCTURE.md`](01_PROJECT_STRUCTURE.md) | 저장소·`Assets/@*` 폴더 구조, 생성 시점 규칙 | 폴더 구조 |
| [`02_STATIC_DATA.md`](02_STATIC_DATA.md) | CSV → DataTransformer → JSON → Addressables → DataManager | 데이터 구조 |
| [`03_LOCALIZATION.md`](03_LOCALIZATION.md) | UI String Table과 Game Data `LocalizedText` 두 파이프라인, Locale, Font | 언어 구조 |
| [`04_IMAGE_PIPELINE.md`](04_IMAGE_PIPELINE.md) | OpenAI 이미지 생성, 스타일 문서, 승인 라운드, Unity 배선 | 이미지 생성 |
| [`05_AUDIO_PIPELINE.md`](05_AUDIO_PIPELINE.md) | ElevenLabs 효과음·배경음 생성, 후처리, Unity 배선 | 음악 생성 |
| [`06_RECOMMENDED_EXTRAS.md`](06_RECOMMENDED_EXTRAS.md) | Boot/Managers, Resource Scope, Save, asmdef, 검증 체인, Workstream, Git | 추가 제안 |
| [`tools/`](tools) | C1에서 그대로 복사한 스크립트와 설정 예시 (아래 표) | — |

### `tools/` (C1 원본 복사본 — 새 프로젝트에 맞게 고쳐 쓴다)

| 파일 | C1 원본 | 새 프로젝트에서 고칠 곳 |
|---|---|---|
| `gen_image.py` | `ArtPipeline/tools/test_imagegen.py` | `TYPES`(타입·섹션 번호·레퍼런스·fit), `*_REFERENCE` 경로, `REFERENCE_RULE` 문구의 "slot machine/reel" 표현, `DEFAULT_SUBJECT` |
| `STYLE_RUNTIME.example.md` | `ArtPipeline/STYLE_RUNTIME.md` | 전부 (그림체는 프로젝트마다 새로 정한다. **섹션 번호 구조만** 유지) |
| `gen_sfx.py` | `ArtPipeline/tools/gen_sfx.py` | `STYLE` 꼬리 문구, `ITEMS` |
| `gen_bgm.py` | `ArtPipeline/tools/gen_bgm.py` | `SHARED_POSITIVE`/`SHARED_NEGATIVE`, `ITEMS` |
| `merge_ui_font.py` | `ArtPipeline/tools/merge_ui_font.py` | Family 이름, 출력 경로 (Latin + 한글을 한 파일로 합칠 때만) |
| `gitignore.example`, `gitattributes.example` | 저장소 root | Vendor 경로 줄 |

## 적용 순서 (권장)

```text
1. Unity 프로젝트 생성 (2D URP), Git 초기화, gitignore/gitattributes 적용
2. CLAUDE.template.md -> root CLAUDE.md (게임 고유 섹션 채우기)
3. 01~03, 06을 Docs/Architecture/ Owner 문서로 옮기기 (프로젝트에 맞게 줄여서)
4. Boot/Main + AppRoot + Managers 최소 구현 (06)
5. 첫 CSV 하나로 Data 파이프라인 관통 (02) + 검증 체인 Batch 명령 확보 (06)
6. UI String Table + LocalizedText (03)
7. ArtPipeline/ 세팅: .venv, 키체인, STYLE 문서, 레퍼런스 한 장, 스모크 1회 (04)
8. 소리 (05) — 게임 루프가 돈 뒤에
```

각 단계는 "첫 실제 파일이 생길 때 그 폴더를 만든다"를 따른다. 빈 폴더를 미리 만들지 않는다.

## 가져가지 않는 것 (C1 고유)

- Slot/Payline/Score/Effect 도메인, `BigInteger` Score, `MultiplierScale = 1000`
- Manager **목록**(아홉 개)과 `RunManager`/`MetaManager`의 구체 책임 — 구조(명시적 Configure, 의존 방향 고정)만 가져간다
- Save DTO 내용, Profile 3칸, 정산 순서 — Atomic Write/Backup/실패 시 재시도 **패턴**만 가져간다
- PCG32 `RunRandom` — 새 게임에 결정적 재현(Resume, 시드)이 필요할 때만. 필요하면 06의 해당 절을 본다
- 그림체(Cute Clean Cartoon)와 소리 방향 — 파이프라인만 가져가고 스타일은 새로 정한다

## C1에서 배운 것 중 구조보다 중요했던 것

1. **문서가 Source, 코드는 그 문서를 읽는다.** 이미지 프롬프트는 `STYLE_RUNTIME.md`를 실행 시 파싱해 조립한다. 규칙이 두 곳에 있으면 어긋난다.
2. **CSV가 Source, JSON은 Git에 넣는 생성물.** stale이면 Build/Test가 실패한다. 조용한 기본값 치환이 없다.
3. **유료 API는 호출 전에 실패할 수 있는 것을 전부 실패시킨다.** 파일 존재, 스타일 문서 파싱, 레퍼런스 포맷 검사 → 그 다음 키 → 그 다음 1회 호출.
4. **생성물은 사람이 승인한 것만 Unity에 배선한다.** 후보 시트 → 항목별 판정 → 승인분만 PNG + CSV 키 + Addressables Entry → 검증 체인 1회.
5. **API 키는 macOS 로그인 키체인.** 환경 변수·`.env`·로그·예외 메시지에 넣지 않는다.
