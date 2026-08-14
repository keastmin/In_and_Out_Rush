# ProjectIO 개발 문서

이 폴더는 두 협업자와 각자의 AI가 같은 프로젝트 구조와 작업 상태를 공유하기 위한 저장소 문서다.

## 읽기 순서

1. 저장소 루트 `AGENTS.md`
2. `PROJECT_MAP.md`
3. `Work/Active/`
4. 대상 `Features/<Feature>.md`
5. 필요한 `.agents/skills/`

## 문서 역할

- `PROJECT_MAP.md`: 기능 문서와 주요 진입점으로 보내는 라우터
- `Features/`: 현재 기능 책임, 연결부, Asset, 검증 방법
- `Work/Active/`: 현재 작업과 공용 Asset 예약
- `Work/Completed/`: 완료된 작업의 변경·검증 기록
- `Decisions/`: 협업자가 합의한 장기 구조 결정

문서가 실제 코드와 다르면 코드를 우선하고 관련 문서를 갱신한다. 클래스와 메서드를 모두 나열하지 않고 다음 작업자가 조사 범위를 줄이는 데 필요한 정보만 유지한다.
