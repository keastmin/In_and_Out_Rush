# W-20260920-002 연구소 UI 재구성과 개별 아이템 구매·보급

Status: Reserved

## 동기화 기준

- Base Commit: 4636f915279cf6d6d939d57958a47945bc905e93
- 공용 Upstream: origin/rebuild-development-environment
- Branch: rebuild-development-environment
- CheckStart: AHEAD=0, BEHIND=0, READY_TO_CHECK_CONFLICTS. 제한 환경의 fetch 실패 후 권한을 확보하여 재실행했고 동기화를 확인했다.

## 담당자

현재 작업의 Codex 에이전트. 예약 Push와 구현 결과 Push는 각각 작업자의 명시적인 진행 요청 후 수행한다.

## 기능

Stage UI / Tower and Laboratory / Runner Items and Skills / 보급 타워 건설·상호작용

## 목표

- 연구소 Runner Supply의 아이템 항목을 아이콘·이름·가격·해금 상태를 보여주는 드롭다운과 구매 동작으로 바꾼다.
- 구매한 개별 아이템 식별자를 연구소 보급 대기열, 보급 타워의 네트워크 내용물, 러너 인벤토리까지 유지한다. 일반 Item 구매와 무작위 아이템 지급 경로를 대체한다.
- 보급 대기열의 각 슬롯에 구매한 아이템의 실제 Runner UI Sprite를 표시한다. 스킬·무기 보급의 기존 기능도 유지한다.
- 기존 타워·러너 업그레이드와 보급 기능에 접근할 수 있는 상태로 연구소 UI를 UGUI와 TMP로 재구성한다.
- UI의 RectTransform, LayoutGroup, ScrollRect, TMP 텍스트, 버튼, 드롭다운 템플릿과 Sprite 참조를 Prefab에서 개발자가 직접 수정할 수 있게 노출한다.
- imagegen으로 새 장식용 헤더·패널 이미지를 제작한다. 텍스트와 조작 요소는 개별 UGUI/TMP 객체로 유지한다.

## 읽을 문서와 Skill

- AGENTS.md, Docs/PROJECT_MAP.md, Docs/Work/README.md, Docs/Work/TEMPLATE.md
- Docs/Features/StageUI.md, TowerAndLaboratory.md, RunnerItemsAndSkills.md, StageInitialization.md
- 기획/기획서/10 아이템 기획.md, 16 연구소 UI 기획.md, 09 연구소 경제 업그레이드.md
- 기획/데이터/땅타 프로젝트 데이터 테이블 0.60v.xlsx: Item!A4:M9, Economy의 관련 규칙 확인
- manage-feature-work, photon-fusion-feature, replace-existing-feature, spreadsheets, imagegen

## 조사 결과와 가격 기준

Item 시트의 C/D 열은 광물/가스 가격이다. 구매 한 번에 아이템 한 개를 대기열 한 슬롯에 담는다.

| Item 행 | 데이터 ID | RunnerItemType | 이름 | 광물 | 가스 | 해금 조건 |
|---|---:|---|---|---:|---:|---|
| 5 | 7000 | Lifeline | 생명선 | 30 | 0 | 없음 |
| 6 | 7001 | Barrier | 방벽 | 30 | 0 | 없음 |
| 7 | 7002 | Incinerator | 소각기 | 30 | 20 | 화염 센터 |
| 8 | 7003 | ElectricGrenade | 전류탄 | 50 | 0 | 전격 센터 |
| 9 | 7004 | BiodecompositionDevice | 생분해 장치 | 0 | 60 | 생화학 센터 |

- 소각기 행의 상태는 '추후 입력'이지만 가격 C7/D7은 명시돼 있다. 미입력 피해·공격 수치를 이번 작업에서 정하지 않는다.
- 가격·이름·아이콘·필요 센터를 Inspector에서 확인 가능한 Catalog ScriptableObject에 저장하고 UI와 서버 검증이 같은 정의를 사용한다. 원본 Excel은 읽기 전용이다.
- 아이콘은 Assets/03_Prefabs/UI/Runner/Player Runner UI.prefab의 itemSlotViews 순서에 연결된 실제 Sprite를 재사용한다. 새 아이템 아이콘을 만들지 않는다.
- 현재 구매는 UI에서 로컬 대기열 추가와 자원 차감을 따로 요청한다. 현재 건설은 Client가 보내는 int[]를 타워에 싣고 성공 통지 뒤 로컬 대기열을 지운다. 현재 타워 수령은 성공 여부와 무관하게 전체 보급을 지울 수 있다. 이 경계를 함께 바꿔야 개별 구매의 보존·중복 방지·실패 피드백을 보장할 수 있다.

## UI 구성안

- Header: 연구소 제목, 광물·가스 보유량, 닫기.
- Body/UpgradePane: 타워 업그레이드 / 러너 업그레이드 탭. 기존 업그레이드 버튼·가격·레벨·이벤트를 유지하고 행 정렬과 여백을 통일한다. 필요한 영역에 ScrollRect를 둔다.
- Body/RunnerSupplyPane: 스킬·무기 보급, 아이템 드롭다운, 선택 아이템 정보와 구매 버튼, 구매 불가 이유와 결과 메시지.
- Footer/SupplyQueue: 기존 10칸 대기열, 개별 아이템 Sprite와 이름/순서, 사용 중인 슬롯 수. 작은 화면에서도 영역이 겹치지 않도록 LayoutGroup과 크기 제약을 사용한다.
- 아이템 선택 자체로 구매되지 않게 하고 명시적인 구매 버튼으로 한 개를 구매한다. 해금되지 않은 아이템은 이름·아이콘·가격과 잠금 이유를 확인할 수 있으나 구매는 막는다.
- 생성 이미지는 배경·헤더 장식으로 사용하고, 텍스트 가독성·가격 대비·상태 구별은 UGUI 스타일로 유지한다.
- 편집용 Editor 빌더로 영구 Prefab 계층과 참조를 저장한다. 플레이 시작 때 UI 전체를 재생성하거나 Inspector 레이아웃을 매 프레임 덮어쓰지 않는다.

## 예상 수정 코드

기존 소비 경로:

- Assets/02_Scripts/Stage UI/Builder UI/Laboratory UI/LaboratoryUI.cs
- Assets/02_Scripts/Stage UI/Builder UI/Laboratory UI/RunnerSupplyUI.cs
- Assets/02_Scripts/Stage UI/Builder UI/Laboratory UI/LaboratorySupplyInventoryUI.cs
- Assets/02_Scripts/Stage UI/Builder UI/Laboratory UI/LaboratoryWidthLayoutConstraint.cs
- Assets/02_Scripts/Stage UI/Builder UI/Laboratory UI/TowerUpgradeUI.cs
- Assets/02_Scripts/Stage UI/Builder UI/Laboratory UI/RunnerUpgradeUI.cs
- Assets/02_Scripts/Stage UI/Builder UI/PlayerBuilderUI.cs
- Assets/02_Scripts/Stage UI/StageUIController.cs
- Assets/02_Scripts/Tower/SupplyTowerManager.cs
- Assets/02_Scripts/Tower/Towers/Support Tower/SupplyTower.cs
- Assets/02_Scripts/Tower/Build/TowerBuildManager.cs
- Assets/02_Scripts/Player/Player Builder/PlayerBuilderTowerBuild.cs
- Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs
- Assets/02_Scripts/Player/Player Runner/Network/PlayerRunnerUpgradeHandler.cs
- Assets/02_Scripts/Player/Player Runner/Item/RunnerItemInventory.cs
- Assets/02_Scripts/Player/Player Runner/Item/RunnerItemSlot.cs
- Assets/02_Scripts/Obtainable/Item.cs
- Assets/02_Scripts/Laboratory/Laboratory.cs
- Assets/02_Scripts/Stage/Network/StageBootstrapper.cs
- Assets/02_Scripts/Stage/Network/StageBootstrapper.KIM.cs

필요한 센터 조건의 읽기 연결부(기존 공격·속성 부여 규칙은 변경하지 않음):

- Assets/02_Scripts/Tower/Tower.cs
- Assets/02_Scripts/Tower/TowerManager.cs
- Assets/02_Scripts/Tower/Towers/Center Tower/CenterTower.cs

신규 도메인·표시·검증 코드:

- Assets/02_Scripts/Features/RunnerSupply/Public/RunnerSupplyDefinition.cs
- Assets/02_Scripts/Features/RunnerSupply/Public/RunnerSupplyCatalog.cs
- Assets/02_Scripts/Features/RunnerSupply/Public/RunnerSupplyResult.cs
- Assets/02_Scripts/Features/RunnerSupply/Logic/RunnerSupplyRules.cs
- Assets/02_Scripts/Features/RunnerSupply/Adapters/Fusion/RunnerSupplyNetwork.cs
- Assets/02_Scripts/Features/RunnerSupply/Presentation/LaboratoryTabsUI.cs
- Assets/02_Scripts/Features/RunnerSupply/Presentation/LaboratoryItemDropdownUI.cs
- Assets/02_Scripts/Features/RunnerSupply/Presentation/LaboratorySupplySlotUI.cs
- Assets/Editor/BuilderLaboratoryUIBuilder.cs
- Assets/Editor/RunnerSupplyVerification.cs
- 위 신규 코드의 개별 .meta 및 새 RunnerSupply/Public/Logic/Adapters/Adapters/Fusion/Presentation 폴더의 .meta

## 예약 Scene·Prefab·Data Asset

- Assets/03_Prefabs/UI/Builder/Laboratory/Builder Laboratory UI.prefab 및 .meta: UGUI/TMP 레이아웃·컴포넌트·참조 재구성.
- Assets/03_Prefabs/UI/Builder/Player Builder UI.prefab 및 .meta: 연구소 인스턴스의 외부 참조·레이아웃 연결.
- Assets/01_Scenes/GamePresentation.unity 및 .meta: 필요한 경우 연구소/빌더 UI 인스턴스의 직렬화 override 정합성 조정.
- Assets/03_Prefabs/Laboratory/Laboratory.prefab 및 .meta: 보급 구매·대기열 NetworkBehaviour와 Catalog 연결.
- Assets/03_Prefabs/Tower/Support Tower/Supply Tower.prefab 및 .meta: 보급 내용물·수령 컴포넌트 정합성.
- Assets/03_Prefabs/Player/Player Runner.prefab 및 .meta: 아이템 수량 복제·피드백에 필요한 컴포넌트 직렬화 정합성. 초기 수량·개별 효과 수치는 변경하지 않는다.
- 신규 Assets/08_Data/Runner Supply Catalog.asset 및 .meta.
- 신규 Assets/06_Sprites/UI/BuilderLaboratory/Generated/laboratory-header-v2.png 및 .meta.
- 신규 Assets/06_Sprites/UI/BuilderLaboratory/Generated/laboratory-panel-v2.png 및 .meta.
- 신규 Assets/06_Sprites/UI/BuilderLaboratory/Generated/laboratory-design-prompts.md 및 .meta: 생성 도구·프롬프트와 적용처 기록.

읽기 전용 참조: Runner UI/Item Slot Prefab, 기존 아이템 Sprite/atlas와 .meta, 기존 업그레이드·자원·스킬·무기 Sprite, 기존 TMP Font Asset, 원본 Excel.

## 공용 계약 또는 Bootstrapper 변경

- 기존 int 보급 코드는 스킬/무기와 개별 아이템을 식별할 수 있게 구체화하고 구매·보관·타워 적재·지급이 동일 정의를 따른다. 모호한 일반 Item의 무작위 지급 경로와 fallback 구매를 제거한다.
- Catalog와 authoritative 구매/대기열은 Laboratory의 전용 보급 컴포넌트가 담당한다. SupplyTowerManager는 필요한 기존 건설 소비자와의 연결 역할을 맡되 별도 로컬 대기열을 진실의 원천으로 유지하지 않는다.
- StageBootstrapper가 ResourceSystem, 구매 담당 네트워크 컴포넌트, Builder 입력/표시, Runner 및 건설 소비자에 필요한 참조를 전달한다. UI 이벤트나 새 Singleton을 의존성 전달 경로로 쓰지 않는다.
- 보급 타워 생성 시 서버 대기열의 실제 내용과 요청을 검증하고 성공한 적재만 한 번 소비한다. 건설 실패·비용 부족·유효하지 않은 요청에는 대기열과 지불 결과를 일관되게 보존한다.
- 아이템 수령은 지정 RunnerItemType에만 추가한다. 최대 소지량으로 받지 못한 항목은 타워에 보존하고 수령한 항목만 제거한다. 타워는 실제로 비었을 때만 Despawn한다.
- Bootstrapper 변경은 이 보급 흐름의 명시적 주입에 한정한다. 공통 초기화 재설계나 다른 건설 동작의 개편은 하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점: Builder Input Authority의 연구소 구매 UI, Runner Input Authority의 기존 F 상호작용.
- 구매 요청은 상품 식별자를 State Authority로 전달한다. 서버가 요청자의 Builder 역할/소유권, 준비 상태, 유효한 상품, 센터 해금, 10칸 한도, Catalog 가격과 자원을 확인하고 한 번만 지불·대기열 추가한다. Client 제공 가격을 신뢰하지 않는다.
- 성공의 지속 상태는 복제된 대기열/타워 내용물/러너 수량으로 표현한다. 실패와 구매 불가 원인은 요청 Peer에 명시적인 결과로 반환한다. UI는 서버 상태를 읽어 자원·잠금·슬롯·결과를 표시한다.
- Host 로컬 Builder와 Client Builder가 동일한 구매 검증·결과 계약을 따른다. Client도 목록 열기, 선택, 비용/잠금 확인, 성공/실패 피드백을 받을 수 있다.
- Runner 수령도 Host 로컬과 Client Input Authority 모두 같은 authoritative 지급 경로를 이용하고 수량 변경/가득 참/일부 수령 결과를 확인할 수 있어야 한다.
- 유효하지 않은 역할·아이템·변조된 보급 배열·중복 건설 완료·중복 F 처리로 무료 지급, 이중 차감, 이중 수령이 일어나지 않게 한다. Host가 서버와 로컬 Client 역할을 동시에 수행하는 경우에도 한 번 처리한다.
- Laboratory Spawn, UI 비활성 상태, Additive Scene 로딩 순서와 Runner의 늦은 Spawn을 고려한다. 준비되지 않은 상태는 명시적으로 표시하고 주입 이후 복구한다.
- Late Join/AOI 재진입은 현재 복제된 대기열·타워 내용물·러너 소지량을 복원한다. Despawn, Scene unload, UI 닫기/열기 때 이벤트를 중복 구독하지 않는다.
- 런타임 검증 계획: (A) Host Builder + Client Runner, (B) Client Builder + Host Runner 두 조합에서 가격/잠금/구매/슬롯 이미지/타워 적재/F 수령을 확인한다. 잔액 부족, 10칸 가득 참, 러너 해당 아이템 가득 참, 일부만 수령, 건설 거부, 빠른 반복 클릭과 F, Late Join 및 UI 재개방을 포함한다.
- 실제 Host·Client 실행을 수행하지 못하면 정적 검사나 컴파일을 동등성 증거로 표현하지 않고 해당 미검증 항목과 수동 재현 절차를 결과에 기록한다.

## 다른 활성 작업과 겹치는 부분

CheckStart와 Docs/Work/Active 조사 결과 README 외 예약이 없어 현재 충돌 없음. 위 보급 건설·Bootstrapper·PlayerRunner·연구소와 UI Prefab은 이번 작업의 공용 경계로 예약한다.

## 범위 밖

- 아이템 효과, 초기 보유 수량, 최대 소지 수량, 사용 키/슬롯 구성의 밸런스 변경.
- 새 스킬·무기 구현, 기존 타워/러너 업그레이드의 가격·성장 규칙 변경.
- 토큰 상점, 유물 관리, 배송 로켓과 직접 배송 규칙의 신규 구현.
- 게임 월드/Territory/몬스터 전투 규칙, 다른 화면 전체 리디자인, 프로젝트 패키지·asmdef 개편.
- 기존 아이템 원본 이미지와 기획 Excel 수정.

## 완료 조건

- 드롭다운에서 5종의 아이콘·이름·정확한 시트 가격을 확인하고 해금·구매 가능 상태와 실패 이유를 구분할 수 있다.
- 구매한 개별 아이템과 연구소 슬롯 이미지, 타워에 적재된 식별자, 실제 러너 수령 아이템이 일치한다. 무작위 Item 지급과 모호한 Item 슬롯 표시가 활성 경로에 남지 않는다.
- 구매 불가 시 지불/대기열을 잘못 변경하지 않고, 건설 실패나 러너의 소지 한도로 구매품을 조용히 잃지 않는다.
- 기존 업그레이드 버튼, 스킬/무기 보급, 자원 표시, 연구소 열기/닫기가 재구성된 UI에서 유지된다.
- 1920×1080 및 1280×720 기준으로 정상 화면/펼친 드롭다운/잠금·부족 상태/10칸 대기열의 겹침·잘림·텍스트 대비를 Unity에서 확인한다.
- Editor에서 UI 계층, LayoutGroup/RectTransform, TMP, 버튼 이벤트와 Catalog/Sprite 참조를 개발자가 직접 수정할 수 있다. 생성 이미지가 프로젝트 내 실제 UI에 적용돼 있다.
- Focused 규칙·적재·수령 검증과 Unity 컴파일, Prefab/Scene guid·fileID·이벤트 참조 검증을 수행하고 Host·Client 실행 여부를 분리 기록한다.
- git diff --check 및 정확한 git status/diff 검토를 수행한다.

## 전환과 롤백

- 전환은 기존 연구소 Prefab과 활성 구매·건설·수령 소비자를 한 번에 연결한다. 동일 구매에 기존 random Item 경로가 함께 실행되지 않게 한다.
- 기존 스킬/무기 보급의 의미는 유지한다. 호환용 진입점이 남으면 실제 소비자를 문서에 명시하고 새 단일 경로로 위임한다.
- 문제가 생기면 이 작업의 코드·Catalog·Prefab·Scene 변경을 함께 되돌리는 별도 수정으로 롤백한다. 작업 전의 모호한 보급 식별자와 새 저장 상태를 혼용하지 않고 새 세션에서 검증한다. 자동 reset/revert나 사용자 변경의 되돌리기는 하지 않는다.

## 실제 변경

예약 문서만 작성. 구현·이미지 생성·Asset 변경은 예약 진행 요청과 Push 검증 후 시작한다.

## 검증 결과

- CheckStart: 동기화 확인, Active 충돌 없음.
- Item!A4:M9에서 5종의 실제 가격·해금 조건을 읽었다. Excel 수정 없음.
- 기존 UI 버튼 이벤트, Runner UI Sprite 참조, 구매 → 대기열 → 건설 배열 → SupplyTower → Runner 무작위 지급 연결부를 조사했다.
- 구현 검증은 아직 수행하지 않았다.

## 남은 위험

- 기존 초기 소지량이 상당수 최대치이며 방벽 Prefab은 99/99이다. 테스트 시 해당 아이템을 먼저 사용하거나 소지 한도로 타워에 남는 동작을 확인해야 한다.
- UI 변경은 큰 계층 재구성이므로 Unity Editor API로 작성하고 외부 Prefab/Scene override와 TMP Font 참조를 점검한다.
- Host·Client 실행 검증은 구현 단계에서 가능한 실행 환경을 확인하고, 수행하지 못한 시나리오는 수동 절차와 함께 남긴다.
