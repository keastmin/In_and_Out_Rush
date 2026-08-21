# W-20260821-009 Game World 라이팅 개선

Status: Completed

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

- `GameWorld.unity`에 누락됐던 `RenderSettings`를 복원하고 Scene의 Sun을 Directional Light에 명시적으로 연결했다.
- `GameScene.unity`와 `GameWorld.unity`의 Directional Light 그림자 강도를 `1.0`에서 `0.75`, Normal Bias를 `0.4`에서 `0.25`, Shadow Angle을 `0`에서 `2`로 조정했다.
- `InfiniteHexGuide.shader`가 URP 메인 라이트의 shadow coordinate를 전달해 실시간 shadow attenuation을 적용하도록 수정했다.
- Grid 바닥의 고정 `0.2` 간접광 대신 Scene ambient probe를 사용하는 PBR Global Illumination 경로로 변경했다.
- `ProjectIOAdditiveSceneBuilder`가 원본 `GameScene`의 RenderSettings를 캡처해 재생성된 `GameWorld`에 적용하도록 보강했다.

## 검증 결과

- Unity Editor 자동 Domain Reload 완료. 최근 Editor 로그 800줄에서 C# 컴파일 오류, Shader 오류 및 compilation failed 기록이 없음을 확인했다.
- `GameWorld.unity`, `GameScene.unity` 각각 RenderSettings 1개, Sun 참조 1개, 참조 대상 Directional Light 1개, 중복 local fileID 0개를 확인했다.
- `Infinite Grid Ground.prefab`의 `m_ReceiveShadows: 1`과 Shader의 `GetMainLight(shadowCoord)`, `SampleSH(normalWS)` 적용을 확인했다.
- `git diff --check` 통과. Git의 기존 line-ending 변환 예고 외 공백 오류는 없다.
- `dotnet build ProjectIO.slnx --no-restore`는 Unity 생성 `Temp/obj/*/project.assets.json`이 없어 `NETSDK1004`로 실행되지 않았다. 코드 오류 결과가 아니며 별도 Restore는 수행하지 않았다.
- Unity Play Mode의 실제 카메라 구도에서 비조명 면, 영역 Mesh와 바닥 Plane 경계, 그림자 부드러움은 작업자 육안 확인이 남아 있다.

## 남은 위험

- 실제 카메라 구도와 다양한 Material에서의 최종 밝기 및 그림자 부드러움은 Unity Play Mode 육안 확인이 필요하다.
- 그래픽 장치와 Quality 단계에 따른 soft shadow 차이는 현재 PC URP 설정을 기준으로 확인해야 한다.
