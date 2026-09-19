# Stage UI

Status: Current

Last reviewed: 2026-09-20

## 책임

Runner·Builder HUD, 타이머, 자원, Tower·Laboratory UI와 로컬 입력 전달을 담당한다.

## 주요 진입점

- `Assets/02_Scripts/Stage UI/StageUIController.cs`
- `PlayerBuilderUI`, `PlayerRunnerUI`
- Builder Main UI, Laboratory UI, Resource UI, Timer UI
- `Assets/02_Scripts/UI/`

## 책임지지 않는 것

게임 규칙, 비용 차감, 네트워크 상태 변경을 소유하지 않는다. 사용자 의도를 실제 시스템에 전달한다.

## 관련 Asset

`GamePresentation.unity`의 Canvas와 EventSystem, UI prefab.

## 변경 시 확인

### 연구소 편집 경로

- `Assets/03_Prefabs/UI/Builder/Laboratory/Builder Laboratory UI.prefab`: 헤더, 타워/러너 강화 탭과 ScrollRect, Runner Supply 구매, 하단 10칸 대기열을 영구 UGUI/TMP 계층으로 저장한다.
- `Features/RunnerSupply/Presentation`: `LaboratoryTabsUI`, `LaboratoryItemDropdownUI`, `LaboratorySupplySlotUI`. 드롭다운 템플릿, 아이콘, 가격, 상태 문구, 구매 버튼 참조를 Inspector에서 수정한다.
- `RunnerSupplyUI`와 `LaboratorySupplyInventoryUI`는 Bootstrapper가 주입한 `RunnerSupplyNetwork`를 표시한다. UI는 비용 차감이나 대기열의 원본 상태를 소유하지 않는다.
- 아이템 이미지와 상품 데이터는 `Runner Supply Catalog.asset`에서 수정한다. 5종 아이템 Sprite는 기존 Runner UI 슬롯 이미지와 동일하다.
- `ProjectIO > Runner Supply > Build Laboratory`는 프리팹과 기획 기준 카탈로그를 명시적으로 재구성하는 Editor 도구다. 실행하면 수동 레이아웃 편집을 덮어쓰므로 일반 UI 수정은 저장된 프리팹에서 한다. 플레이 중 재구성은 없다.
- `Verify and Render Laboratory`는 `Library/RunnerSupplyTools`에 규칙·직렬화 참조 검사와 1920×1080, 1280×720 프리뷰를 출력한다. 실제 Host·Client 동작 검증과 구분한다.
- 상위 `Player Builder UI.prefab`와 `GamePresentation.unity`의 연구소 레이아웃 override는 새 프리팹을 따르도록 정리하며, 열기/닫기 연결과 초기 활성 상태는 유지한다.

- 역할별 UI 활성화
- 비활성 UI의 초기화와 참조
- 이벤트 listener 중복과 Scene unload 정리
- UI가 다른 시스템 의존성의 전달 통로가 되지 않는지

## 기술 부채

일부 UI가 구체적인 게임 시스템과 직접 연결되어 있다. 소비 기능의 공개 명령으로 점진적으로 좁혀야 한다.
