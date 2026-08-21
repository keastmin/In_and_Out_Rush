# W-20260821-009 Game World 라이팅 개선

Status: Reserved

## 동기화 기준

- Base Commit: d9bf26718aa3e3b8757b2acc27b1a03a6e835f70
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Game World Scene 환경광과 Directional Light 그림자 품질

## 목표

- Directional Light를 직접 받지 않는 면도 재질 색상을 구분할 수 있도록 `GameWorld`의 환경광을 복원하고 조정한다.
- 지나치게 강하고 딱딱하게 보이는 Directional Light 그림자를 완화한다.
- `Infinite Grid Ground` 전용 Shader가 메인 라이트의 그림자 감쇠를 적용하도록 수정해 영역 Mesh와 바닥 Plane의 그림자 수신 방식을 일치시킨다.
- Additive Scene 재생성 후에도 원본 Scene의 라이팅 설정이 `GameWorld`에 보존되도록 한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/SessionAndScenes.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Features/Territory.md`
- `.agents/skills/manage-feature-work/SKILL.md`

## 예상 수정 코드

- `Assets/Editor/ProjectIOAdditiveSceneBuilder.cs`
- `Assets/10_Shaders/Grid/Infinite Grid/InfiniteHexGuide.shader`

## 예약 Scene·Prefab·Data Asset

- `Assets/01_Scenes/GameWorld.unity`
- `Assets/01_Scenes/GameScene.unity`

## 공용 계약 또는 Bootstrapper 변경

없음

## 네트워크·Peer 동등성

해당 없음. 로컬 렌더링 Scene 설정만 변경하며 Fusion 상태, Spawn, RPC, Authority 또는 플레이어 상호작용 계약은 변경하지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. 확인 시 `Docs/Work/Active/README.md` 외 다른 Active 예약이 없다.

## 범위 밖

- URP Pipeline Asset과 전역 Quality 설정 변경
- `Infinite Grid Ground` 전용 Shader 외의 Material, Shader, VFX, 카메라 및 포스트 프로세싱 변경
- 라이팅 베이크와 Lightmap 신규 생성
- Grid·Territory 게임 규칙과 네트워크 로직 변경

## 완료 조건

- `GameWorld`에 명시적인 환경광·반사 설정이 저장되어 비조명 면이 완전한 검정으로 뭉개지지 않는다.
- Directional Light의 그림자 강도와 부드러움이 조정되어 형태를 읽을 수 있는 자연스러운 그림자가 된다.
- `Infinite Grid Ground`가 Directional Light의 실시간 그림자를 수신하며 영역 Mesh 위와 바닥 Plane 위에서 그림자 유무가 부자연스럽게 달라지지 않는다.
- `InfiniteHexGuide.shader`가 URP 메인 라이트의 shadow coordinate와 shadow attenuation을 사용하면서 기존 Grid·Territory·미리보기 오버레이를 보존한다.
- `GameScene`의 기준 라이팅과 `GameWorld`의 결과가 일치하고 Additive Scene Builder가 Scene 설정을 복사한다.
- Scene YAML 무결성, Unity 프로젝트 컴파일, `git diff --check`, 예약 외 변경 부재를 확인한다.

## 실제 변경

구현 전

## 검증 결과

구현 전

## 남은 위험

실제 카메라 구도와 다양한 Material에서의 최종 밝기 체감은 Unity Play Mode 육안 확인이 필요하다.
