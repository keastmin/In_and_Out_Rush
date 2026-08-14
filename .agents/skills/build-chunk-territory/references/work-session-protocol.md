# Chunk Territory 장기 작업 프로토콜

여러 작업에 걸쳐 Legacy Territory를 Chunk 기반 파이프라인으로 옮길 때만 이 프로토콜을 사용한다. 짧은 단일 작업은 `Docs/Work/Active/`와 `Completed/` 기록만 사용한다.

## 장기 작업 문서 위치

장기 작업 루트는 `Docs/LongRunning/Territory/`로 한다.

- `CURRENT_MILESTONE.md`: 현재 작업 하나의 승인 범위
- `HANDOFF.md`: 검증된 상태와 다음 시작점
- `ROADMAP.md`: milestone 순서와 상태
- `CONTRACTS.md`: 승인된 좌표, revision, ownership, 공개 타입
- `MIGRATION.md`: Legacy 병행, consumer cutover, rollback
- `TEST_MATRIX.md`: domain, Host, Client, Late Join, consumer 검증
- `DECISIONS.md`: 승인·거절·결정 요청

필요한 문서만 실제 milestone이 시작될 때 만든다. 빈 장기 문서 세트를 미리 만들지 않는다.

## 승인 상태

`CURRENT_MILESTONE.md`에는 다음 중 하나의 상태를 둔다.

```text
Status: Draft | Review Needed | Approved | Complete | Superseded
```

구현에는 `Approved`가 필요하다. 공용 계약을 구현하거나 변경하면 `CONTRACTS.md`의 해당 결정도 Approved여야 한다.

## Current milestone 필수 항목

```markdown
# Current Milestone

Status: Approved

## Objective
## Prerequisites
## Read first
## Allowed files
## Reserved Scene·Prefab·Data Asset
## Prohibited changes
## Required behavior
## Acceptance criteria
## Rollback
## Out of scope
```

## Handoff 필수 항목

```markdown
# Handoff

## Completed outcome
## Changed files
## Decisions used
## Verification evidence
## Serialized or manual setup
## Known risks and failures
## Remaining legacy consumers
## Next bounded milestone
## Exact starting files
```

## 작업 경계

- 하나의 task에서 milestone 하나만 구현한다.
- 같은 milestone이 만든 실패는 같은 task에서 수정한다.
- Scene, Prefab, 공용 계약, roadmap, milestone, handoff를 병렬 작업하지 않는다.
- 코드와 handoff가 다르면 코드를 기준으로 차이를 기록한다.
- 다음 milestone은 준비만 하고 자동으로 구현하지 않는다.
