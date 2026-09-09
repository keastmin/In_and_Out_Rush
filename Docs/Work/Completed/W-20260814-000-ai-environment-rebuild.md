# W-20260814-000 AI 협업 개발 환경 재구성

Status: Complete

## 목표

두 협업자와 각자의 AI가 Project Map, 기능 문서, 작업별 예약, 공통 Skill을 사용해 전체 프로젝트 재탐색과 공용 Asset 충돌을 줄이도록 저장소 환경을 구성한다.

## 실제 변경

- 개인별 수정 권한을 제거한 공통 `AGENTS.md`
- `Docs/PROJECT_MAP.md`와 모든 활성 기능의 문서
- `Docs/Work/Active`, `Completed`, 작업 template과 Scene·Prefab 예약 규칙
- 장기 결정 ADR과 장기 작업에만 사용하는 handoff 원칙
- 일반 `migrate-feature-slice`, 완전 교체, Fusion, Chunk Territory Skill 역할 분리
- `.gitignore`에서 `AGENTS.md`와 `.agents/` 제외를 제거해 협업자가 pull할 수 있게 변경
- Resource Spawn을 첫 도메인 구조·asmdef 파일럿으로 적용

## 검증 결과

- Project Map의 모든 기능 문서 링크와 현재 코드 디렉터리 귀속 확인
- Skill frontmatter와 파일 인코딩 수동 확인
- `skill-creator`의 `quick_validate.py`는 실행 환경에 PyYAML이 없어 `ModuleNotFoundError: yaml`로 자동 검증하지 못함
- Resource Spawn의 Unity import, EditMode 테스트, 전체 프로젝트 build 성공

## 남은 운영 확인

두 협업자가 실제로 서로 다른 작은 작업을 `Docs/Work/Active/`에 예약해 진행한 뒤 조사 시간, 충돌 탐지, 문서 갱신 부담을 회고해야 한다.
