# W-20260828-002 Territory Skill UI metadata 정합화

Status: Completed

## 동기화 기준

- Base Commit: 137b9c5930cbe865e239d242909c9e496a47a3aa
- 공용 Upstream: origin/rebuild-development-environment
- CheckStart: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`, 2026-08-28)

## 담당자

Codex `/root`

## 기능

`build-chunk-territory` Skill의 UI 표시 이름, 짧은 설명과 기본 prompt를 현재
`SKILL.md` 계약에 맞춘다.

## 목표

- UI metadata에서 폐기된 Legacy→Chunk 고정 전환과 단일 approved milestone 전제를
  제거한다.
- Territory의 설계, 이식, 교체, prototype 정리와 Chunk 기반 작업을 모두 현재 예약과
  실제 구현 계약에 따라 수행하는 Skill임을 짧고 명확하게 안내한다.
- 자동 Skill 선택 정책과 기존 dependency/policy 구조는 변경하지 않는다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `manage-feature-work`
- `build-chunk-territory`
- `skill-creator`
- `skill-creator/references/openai_yaml.md`

## 예상 수정 코드

- `.agents/skills/build-chunk-territory/agents/openai.yaml`
- `Docs/Work/Active/W-20260828-002-territory-skill-ui-metadata.md`

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

없음. UI metadata만 바꾸며 `SKILL.md`, Territory runtime, caller, 문서 라우팅과
Bootstrapper 계약은 변경하지 않는다.

## 네트워크·Peer 동등성

해당 없음. Fusion 상태, RPC, Authority, 입력과 Host·Client 플레이 체감을 변경하지 않는다.

## 다른 활성 작업과 겹치는 부분

- `W-20260827-001-runner-dual-pistols`는 Player Runner 코드, 입력과 Prefab을 예약한다.
- 이번 작업은 Territory Skill의 `agents/openai.yaml`만 수정하므로 파일, Asset,
  공용 계약과 의미상 겹치지 않는다.

## 범위 밖

- Territory runtime, query, expansion, Trail, replication과 consumer 변경
- `build-chunk-territory/SKILL.md`, `AGENTS.md`, 기능 문서와 Project Map 변경
- 미래 Chunk·Grid·Quadtree 설계 또는 구현 예약
- policy, dependency, icon과 brand metadata 추가
- Scene, Prefab, ScriptableObject, ProjectSettings, Package 변경

## 완료 조건

- `display_name`, `short_description`, `default_prompt`가 현재 Skill 책임과 일치한다.
- `short_description`이 25–64자 범위의 짧은 UI 설명이다.
- `default_prompt`가 `$build-chunk-territory`를 명시하고 특정 저장 구조나 과거
  milestone을 구현 전제로 강제하지 않는다.
- 모든 string은 인용되고 기존 `interface` 구조와 자동 invocation 기본값을 유지한다.
- `skill-creator/scripts/quick_validate.py`와 YAML parse가 통과한다.
- `git diff --check`, `git status`에 예약 밖 변경이 없다.

## 실제 변경

- `display_name`을 `Territory 구조 작업`으로 바꿨다.
- `short_description`을 Territory 설계·이식·교체·정리를 현재 계약에 따라 수행하는
  35자 설명으로 바꿨다.
- `default_prompt`가 현재 Territory 계약을 먼저 확인하고 예약된 설계, migration,
  replacement 또는 retirement phase를 적절히 검증하도록 수정했다.
- 기존 `interface` 구조만 유지했으며 policy, dependency, icon, brand metadata는
  추가하지 않았다.

## 검증 결과

- `CheckStart`: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`)
- 기존 Active 의미 충돌: 없음. Player Runner 예약 파일과 Asset을 제외했다.
- 예약 Commit `6824786` Push 뒤
  `VerifyReservation=READY_TO_IMPLEMENT`를 확인했다.
- `skill-creator/scripts/quick_validate.py`: `Skill is valid!`.
- PyYAML parse와 schema assertion: `YAML=VALID`.
- `short_description` 길이 35자, 모든 interface 값 string, top-level/interface key
  보존, `default_prompt`의 `$build-chunk-territory` 포함과 폐기된
  `approved chunk-territory milestone` 부재를 확인했다.
- PyYAML은 `ProjectIO/Temp`에만 임시 설치해 검증 후 삭제했다.
- runtime, Scene, Prefab, Territory 문서와 Player Runner 예약 파일은 변경하지 않았다.

## 남은 위험

- UI가 metadata를 cache하면 변경 반영 시점은 Codex app의 Skill 재탐색 주기에 좌우될 수
  있다. 저장소 파일 정합성 검증과 app cache 갱신은 구분한다.
- 실제 Codex app UI cache 재탐색은 이 저장소 검증에서 강제로 수행하지 않았다.
