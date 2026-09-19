# W-20260920-002 연구소 UI 재구성과 개별 아이템 구매·보급

Status: Completed

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

- 예약 진행 요청 후 `a7f88132661d83e26d4807c1b430bf0e62ab6127`을 Push하고 VerifyReservation의 READY_TO_IMPLEMENT를 확인했다.
- RunnerSupply 도메인에 Catalog/Definition/Rules/Result, Laboratory의 authoritative 구매·대기열 NetworkBehaviour, 탭·드롭다운·개별 슬롯 표시 컴포넌트를 추가했다.
- `LaboratoryUI`, `RunnerSupplyUI`, `LaboratorySupplyInventoryUI`를 표시·입력 역할로 정리했다. 기존 13개 강화 버튼의 컴포넌트/이벤트/비용·레벨 참조를 보존했다.
- `SupplyTowerManager`, `TowerBuildManager`, `PlayerBuilderTowerBuild`는 서버 구매 목록을 검증한 뒤 건설 Commit에서만 적재 목록을 소비한다. Client 결과 수신 시 중복 로컬 소비를 제거했다.
- `SupplyTower`, `PlayerRunner`, `PlayerRunnerUpgradeHandler`, `Item`에서 개별 ID/RunnerItemType으로 지급한다. 소지 한도에 걸린 항목은 타워에 보존하며 실제로 비었을 때만 제거한다. Runner 슬롯 수량은 NetworkArray로 복제하고 HUD 준비 시 현재 수량을 다시 표시한다.
- `StageBootstrapper.KIM`이 공급 네트워크·건설 소비자·연구소 UI·Runner 수량 HUD와 수령 피드백을 주입한다.
- `Builder Laboratory UI.prefab`을 Header / Research tabs / Runner Supply / 10-slot queue로 재구성했다. 생성한 헤더와 sliced panel을 적용했다. TMP 드롭다운은 기존 Runner UI의 5종 Sprite를 그대로 사용한다.
- `Player Builder UI.prefab`, `GamePresentation.unity`에서 해당 연구소의 이전 레이아웃 override를 제거하고 닫기 이벤트·초기 활성 상태를 보존했다. Scene 카메라의 Editor 자동 갱신 값은 작업 전 값으로 복원한다.
- `Laboratory.prefab`에 NetworkBehaviour와 Catalog를 연결했다. 기존 Global interest는 유지한다. Supply Tower/Player Runner prefab은 타입 배치 변경이 필요하지 않아 수정하지 않았다.
- 신규 `Runner Supply Catalog.asset`, 생성 PNG 2개 및 프롬프트 문서, `BuilderLaboratoryUIBuilder.cs`, `RunnerSupplyVerification.cs`와 신규 Asset/폴더의 .meta를 추가했다.
- 변경된 진입점·주입·편집 경로에 따른 문서 갱신: `Docs/Features/StageUI.md`, `TowerAndLaboratory.md`, `RunnerItemsAndSkills.md`, `StageInitialization.md`, `Docs/PROJECT_MAP.md`. manage-feature-work의 구현 후 통상 문서 갱신 단계이며 별도 기능 확장은 없다.
- 초기 소지량/상한, 기존 아이템 효과, 스킬/무기 보급의 의미, 업그레이드 가격과 성장 규칙, 입력 키는 변경하지 않았다.

## 검증 결과

- CheckStart: 동기화 확인, Active 충돌 없음.
- Item!A4:M9에서 5종의 실제 가격·해금 조건을 읽었다. Excel 수정 없음.
- 기존 UI 버튼 이벤트, Runner UI Sprite 참조, 구매 → 대기열 → 건설 배열 → SupplyTower → Runner 무작위 지급 연결부를 조사했다.
- Unity 6000.0.69f1에서 Assembly-CSharp와 Assembly-CSharp-Editor 컴파일 및 Fusion IL weaving을 확인했다.
- `RunnerSupplyVerification.Verify`: 7종 상품 및 5종 시트 가격·아이콘·타입, 정확한 잔액, 자원 부족, 10칸 한도, 센터 해금, manifest 순서/중복/위조/빈 목록, 선택 인벤토리 슬롯만 증가, 가득 찬 슬롯 거부를 통과했다. 이는 순수 규칙/인벤토리 검사이며 실제 네트워크 수령 증거와 구분한다.
- 프리팹 검사: 기존 강화 13개/탭 2개/구매 3개/닫기 1개, 드롭다운 5옵션과 pointer raycast, 슬롯 10개, 표시 컴포넌트의 직렬화 참조, 상위 프리팹 닫기 이벤트를 확인했다.
- Unity Camera 렌더링: 1920×1080, 1280×720, 실제 TMP 드롭다운을 펼친 상태, 개별/중복 아이템이 든 10칸 대기열을 확인했다. 소해상도 슬롯 이름은 18px(1920 기준)로 확대했다. 스크롤바와 첫 항목 시작 상태를 구성했다. 이미지의 자원·대기열은 검사용 데이터다.
- 검사 보고서와 프리뷰: `Library/RunnerSupplyTools/verification.txt`, `laboratory-1920.png`, `laboratory-1280.png`, `laboratory-1920-dropdown.png` (Git 제외).
- Host 실행 시도: GameRoot 테스트 진입의 Fusion Scene 로딩이 Presentation을 제외하여 `CinemachineSystem requires scene camera references`와 Territory 초기화 NullReferenceException이 발생했고, 65초 안에 공급 시스템 준비 상태에 도달하지 못했다. 자동 테스트를 종료하고 추가로 연 GameWorld/GameRoot를 닫았다. `playtest.txt`에 실패 원인을 기록했다. 구매·건설·수령의 실제 Host 검증 통과로 간주하지 않는다.
- 실제 Host·Client, 권한 없는 RPC, Late Join, 건설 실패/부분 수령/Despawn 실행 검증은 아래 절차로 남긴다. 정적 Authority 검토나 프리뷰로 Peer 동등성을 완료 처리하지 않는다.
- 최종 `git diff --check` 통과. 신규 Asset의 .meta와 기존 컴포넌트 참조, 무작위 Item 진입점 제거를 확인했다. Unity가 재직렬화한 UI/Laboratory YAML의 빈 값 끝 공백을 정리했으며, 최종 stage 전 공백 검사를 통과했다.

## 남은 위험

- 기존 초기 소지량이 상당수 최대치이며 방벽 Prefab은 99/99이다. 테스트 시 해당 아이템을 먼저 사용하거나 소지 한도로 타워에 남는 동작을 확인해야 한다.
- 기존 테스트 진입의 Additive Scene 준비 문제는 이번 보급/UI 작업의 범위 밖이다. 정상 Lobby 경유 2-Peer 세션으로 아래 검증이 필요하다.
- 수동 레이아웃 편집은 저장된 프리팹에서 한다. Editor Build Laboratory 명령은 명시적으로 재구성하며 수동 편집/카탈로그 값을 기획 기본값으로 덮어쓴다.

## 남은 Host·Client 실행 절차

1. 정상 Lobby 경로로 Host=Builder, Client=Runner 세션을 시작하고 세 Game 씬이 모두 준비됐는지 확인한다. 반대 역할 조합으로도 반복한다.
2. 연구소 R/버튼으로 열고 5종 드롭다운 아이콘·가격, 탭의 모든 강화 항목, 닫기 버튼을 확인한다. 센터 없이 소각기/전류탄/생분해 구매가 잠기고 해당 센터 건설 후 열리는지 확인한다.
3. 서로 다른 아이템과 같은 아이템을 섞어 구매하고 비용이 한 번 차감되는지, 양 Peer에서 같은 순서·아이콘과 10칸 한도를 보는지 확인한다. 자원 부족/상한/연속 요청 거부 시 비용·목록이 변하지 않아야 한다.
4. 유효하지 않은 건설 위치·건설 비용 부족·위조 적재 목록은 구매 대기열을 보존해야 한다. 정상 보급 타워 건설은 실제 구매 목록을 적재하고 해당 항목만 대기열에서 한 번 제거해야 한다.
5. Runner가 3m 이내에서 타워를 바라보고 F를 누른다. 구매한 종류만 증가하고 full 슬롯의 항목은 타워에 남아야 한다. 일부 아이템을 사용한 뒤 다시 F를 눌러 남은 항목을 받고 빈 타워의 점유 해제/Despawn 및 수령 메시지를 확인한다.
6. 권한 없는 Peer의 구매/건설 요청이 거부되는지, 뒤늦게 들어온 Peer가 구매 대기열·적재 내용·현재 슬롯 수량을 복원하는지 확인한다. UI 닫기/다시 열기, 타워/연구소 Despawn 및 Scene 종료 시 중복 구독·NullReference가 없어야 한다.

## 최종 전달 상태

2026-09-20 사용자가 구현 내용과 질문·답변의 문서화 및 커밋을 명시적으로 요청했다. 이에 따라 작업 기록을 Completed로 이동하고 이번 작업의 구현·Asset·문서만 최종 커밋 대상으로 확정한다. 이 상태는 작업 기록의 인계를 뜻하며 실제 네트워크 플레이 검증 통과를 뜻하지 않는다. 실제 Host·Client 수령 검증은 미완료다.

- 별도 질의응답: `Docs/개발 일지/2026-09-20-연구소-보급-질의응답.md`. 센터 해금 조건, 시작 소지량, 수령 한도, F Raycast 판정과 아직 확정하지 못한 원인을 구분하여 기록했다.
- 기능 문서에서 완료 작업 및 질의응답으로 연결했다. 이번 문서화 단계에서는 게임 동작을 추가 변경하지 않았다.
