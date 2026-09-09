# W-20260910-001 포트폴리오 README 개편

Status: Reserved

## 동기화 기준

- Base Commit: 686dd8aee33ccfee1a1d6f2b3d09fba6068d6e7a
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

- 2인 개발팀 공동 작업
- AI 협업 도구를 활용한 문서 조사·초안·검증 보조

## 기능

저장소 루트 포트폴리오와 프로젝트 소개

## 목표

- 처음 방문한 사람이 게임의 콘셉트, 핵심 플레이, 기술적 특징을 빠르게 이해할 수 있도록 루트 README를 개편한다.
- 2인 팀의 역할 분담과 AI를 활용한 협업 방식을 과장 없이 자연스럽게 보여 준다.
- 저장소의 실제 기능 문서와 코드 구조를 근거로 포트폴리오용 기술 설명을 작성한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `Docs/PROJECT_MAP.md`
- 관련 `Docs/Features/*.md`와 프로젝트 설정 파일

## 예상 수정 코드

- `README.md`
- `Docs/Work/Active/W-20260910-001-portfolio-readme.md`

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

없음

## 네트워크·Peer 동등성

해당 없음. 문서 표현만 변경하며 네트워크 동작과 플레이어 경험에는 영향을 주지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외에 진행 중인 예약이 없으며, 해당 안내 문서는 수정하지 않는다.

## 범위 밖

- 게임 코드, Scene, Prefab, ScriptableObject, 프로젝트 설정 변경
- 기능 계약 또는 프로젝트 라우팅 변경
- 사실 확인이 되지 않은 성과 수치, 출시 정보, 역할 또는 기술 스택 추가
- 이미지·영상 Asset의 신규 제작

## 완료 조건

- README가 프로젝트 개요, 핵심 플레이, 주요 시스템, 기술적 특징, 2인 협업 방식, 실행 환경과 문서 탐색 경로를 포트폴리오 형식으로 제공한다.
- AI 활용은 협업 과정의 일부로 명시하되, 홍보성 문구나 생성형 문체 없이 구체적인 작업 방식으로 설명한다.
- 모든 기술 설명과 링크가 저장소의 실제 파일 및 설정과 일치한다.
- `git diff --check`와 링크·경로 점검을 통과한다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

예약 단계. 구현 후 기록한다.

## 남은 위험

- 실행 화면과 팀원별 세부 역할 자료가 저장소에 없을 경우, 확인 가능한 내용만으로 포트폴리오를 구성한다.
