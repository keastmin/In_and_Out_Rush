# ADR-0001 AI 협업 개발 환경

Status: Accepted

Date: 2026-08-14

## 배경

두 협업자가 각각 AI를 사용해 개발할 때 전체 프로젝트 재탐색, 공용 Bootstrapper와 Scene·Prefab 충돌, 문서 불일치, Legacy와 새 경로의 중복 실행을 줄여야 한다.

## 결정

1. `Docs/PROJECT_MAP.md`를 작업 라우터로 사용하고 구조 변경 후 갱신한다.
2. 활성 작업은 `Docs/Work/Active/`의 작업별 파일로 관리한다.
3. 현재 활성 기능 전체에 기능 문서를 둔다.
4. Territory 전용 Skill은 Chunk 기반 Territory의 장기 마이그레이션 지침으로 유지한다.
5. 일반 기능의 단계적 이전은 `migrate-feature-slice` Skill로 분리한다.
6. 첫 리팩토링과 asmdef 파일럿은 Resource Spawn으로 진행한다.
7. 기능 폴더는 도메인별 구조를 따르되 필요한 하위 계층만 만든다.
8. Scene·Prefab 영구 담당자는 두지 않는다. 작업별 예약 후 한 작업에서만 수정한다.
9. Handoff는 여러 작업에 걸치는 장기 마이그레이션에만 사용한다.
10. `AGENTS.md`와 저장소 Skill은 개인별 소유권이 아닌 공통 규칙으로 관리한다.

## 결과

- AI는 Project Map과 기능 문서에서 최소 문맥을 읽는다.
- 충돌 가능성이 있는 파일과 Asset은 구현 전에 드러난다.
- Resource Spawn 파일럿 결과로 asmdef 확대 여부를 다시 결정한다.
- 개인별 코드 소유권 제한은 제거하고 작업별 예약과 Git 검토로 대체한다.
