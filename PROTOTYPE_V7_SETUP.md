# 7차 프로토타입: 로비와 던전 입출입

8차에서 상자/보스 경로 보상 반출과 영구 저장이 추가되었다. 현재 반출 규칙과 저장/복원 테스트는 PROTOTYPE_V8_SETUP.md를 참고한다.

## 실제 프로젝트와 구현 방향

- Unity 6000.6.7f1. 프로젝트는 C:/Users/com/dduk의 기존 Unity 프로젝트다. IDE에서 다른 Python 파일이 선택되어 있어도 그 파일/프로젝트는 수정하지 않았다.
- 기존 씬은 Assets/Scenes/SampleScene.unity 하나였고 재시작은 GameHUD2D의 현재 씬 재로드였다. 로비/입장 확인/씬 왕복/결과 통계는 없었다.
- SampleScene은 이름과 GUID를 유지해 던전으로 사용한다. Dungeon으로 이름을 바꾸거나 전투 씬을 별도로 복제하지 않았다.
- 기존 이동/카메라/전투/무기 개체/드롭/마나/웨이브/사망/클리어 코드를 유지하고 씬 경계와 최소 결과 집계만 추가했다.

## 플레이 흐름

Lobby 시작 → 기본 장비 확인 → 청록색 입장 지점으로 이동 → E → 확인창 → Enter Dungeon → 기존 웨이브 전투 → Game Over 또는 Stage Clear → 결과 확인 → Return to Lobby 또는 Restart Dungeon.

로비 플레이어/카메라와 던전 플레이어/카메라는 각 씬에 하나씩 저장된 별개 오브젝트다. 전환은 LoadSceneMode.Single을 사용해 이전 씬을 제거한다. 플레이어/카메라/아이템/결과를 DontDestroyOnLoad로 유지하지 않는다.

## 생성 파일과 씬

- Assets/Scenes/Lobby.unity 및 .meta — 새 로비 씬.
- Assets/Scripts/Flow/SceneFlow2D.cs — 로비/던전/재시작 경로와 비동기 전환 중복 방지.
- Assets/Scripts/Flow/DungeonSession2D.cs — 새 던전 마나 충전과 종료 결과 일회성 확정.
- Assets/Scripts/Flow/DungeonRunResult2D.cs — 처치 수/도달 웨이브/획득 골드/클리어 여부의 불변 결과 값.
- Assets/Scripts/Flow/LobbyController2D.cs — 로비 장비 UI, 입장 거리/E 입력, 확인/취소.
- Assets/Editor/PrototypePlayStartScene.cs — 에디터 Play 시작 씬 설정과 디버그 메뉴.
- 위 스크립트 및 Flow/Editor 폴더 .meta.
- Tests/Standalone/SceneFlowTests.cs — 전환 가드와 결과 확정의 독립 로직 검증.
- PROTOTYPE_V7_SETUP.md — 이 보고서.

원본 보존:

- Backups/Prototype7/SampleScene.before-v7.unity.txt
- Backups/Prototype7/EditorBuildSettings.before-v7.asset.txt

백업은 Assets 밖의 텍스트 파일이므로 Unity에 중복 씬으로 임포트되지 않는다.

## 수정 파일

- Assets/Scripts/UI/GameHUD2D.cs — 기존 전투 HUD 유지, 결과 통계/로비 복귀 추가, 재시작을 공통 전환 경로로 연결.
- Assets/Scripts/Player/PlayerGold2D.cs — 사망 시 0이 되는 CurrentGold와 별도로 던전 누적 EarnedGold 유지.
- Assets/Scripts/Player/PlayerMana2D.cs — 던전 입장 시 최대 마나 충전 메서드.
- Assets/Scripts/Waves/WaveManager2D.cs — 정상 처치마다 한 번 증가하는 Kills 카운터.
- Assets/Scenes/SampleScene.unity — 기존 GameUI에 DungeonSession2D와 참조 추가.
- ProjectSettings/EditorBuildSettings.asset — Lobby를 0번, SampleScene을 1번 활성 씬으로 등록. 기존 설정 오브젝트 참조 유지.
- Tests/Standalone/WaveProgressionTests.cs — 처치 수/사망 전 누적 획득량 확인 추가.
- PROTOTYPE_V6_SETUP.md — 후속 로비 단계 안내 링크.

## 로비 구성

기존 씬의 바닥/벽 네 개/Global Light 2D/플레이어 도형/물리/이동/카메라를 재사용해 구성했다.

- Player: 시작 위치 (-3, 0, 0), 기존 Move Speed=5, Rigidbody2D 중력 0/벽 충돌 유지.
- Main Camera: 기존 CameraFollow2D가 로비 Player를 추적.
- DungeonEntrance: 위치 (3, 0, 0), 청록색 사각형, 물리 충돌 없음.
- GameUI: LobbyController2D가 장비 정보/입장 버튼/안내/확인창을 실행 중 생성.
- 공격·마법·무기 획득·적·웨이브 컴포넌트는 로비에 배치하지 않았다. 따라서 E 획득과 입장 상호작용이 겹치지 않는다.

장비 UI는 기존 Club 에셋과 기본 투사체 마법을 표시한다. 장비 선택/스크롤 선택은 구현하지 않았다. 시작 무기를 나중에 바꾼다면 로비 Starting Weapon과 던전 Player의 Loadout → Club 참조를 함께 맞춰야 한다.

## 입장 지점 Inspector

Lobby의 GameUI → Lobby Controller 2D에 이미 연결했다.

| 필드 | 현재 연결/값 |
|---|---|
| Player Movement | 로비 Player의 기존 PlayerMovement2D |
| Entrance | DungeonEntrance의 Transform |
| Starting Weapon | 기존 Club.asset |
| Interaction Range | 1.5 |
| Basic Magic Name | Basic projectile (RMB) |

청록색 지점 1.5 이내에서 E 안내를 표시한다. 같은 거리 안에서는 장비 UI의 Dungeon Entrance 버튼도 사용할 수 있다. 확인창이 열리면 로비 이동을 잠시 비활성화하고, Cancel이면 기존 이동 활성 상태로 되돌린다. Enter Dungeon을 누를 때만 전환한다.

## 던전 Inspector

기존 SampleScene의 GameUI에 DungeonSession2D를 추가하고 다음을 연결했다.

- Player Health / Player Mana / Player Gold: 같은 기존 Player의 컴포넌트.
- Waves: 기존 WaveSystem의 WaveManager2D.
- GameHUD2D → Session: 같은 GameUI의 DungeonSession2D.

기존 플레이어/카메라/무기/투사체/아이템 프리팹 참조는 유지했다. 수동으로 새 플레이어나 카메라를 추가하지 않는다. 현재 작업에서는 추가 수동 연결이 필요 없다.

## 씬 전환과 입력

SceneFlow2D의 경로는 Lobby.unity와 기존 SampleScene.unity다. 경로를 변경한다면 이 상수와 Build Profiles Scene List를 함께 변경해야 한다.

- 전환 시작 후 완료 전까지 추가 입장/복귀/재시작 요청을 거부한다.
- 없는 씬 또는 로드 실패면 전환 가드를 해제하고 Console에 오류를 출력한다. 영구적으로 입력 잠금이 남지 않도록 했다.
- 새 씬은 플레이어/카메라/UI를 자체 생성한다. 이전 씬의 입력 액션은 기존 OnDisable/OnDestroy에서 정리되고, 바닥 아이템 목록도 기존 OnDisable에서 제거된다.
- 새 Player의 Awake/OnEnable에서 입력과 상태를 새로 초기화한다. 이전 씬의 UI나 플레이어를 가리키는 전역 참조는 없다.
- 전체 Time.timeScale을 변경하지 않는다.

## 임시 데이터와 결과

체력/마나/무기 개체/쿨타임/골드/웨이브/몬스터/바닥 아이템은 모두 던전 씬 소유다. 재시작/로비 복귀는 씬을 교체하므로 이전 개체는 폐기된다.

새 던전에서는:

- Health Awake → 최대 HP.
- DungeonSession Start → 최대 마나(Starting Mana가 낮게 설정되어 있어도 최대치).
- Loadout Awake → 기본 수치의 새 Club 개체.
- 공격/마법 컴포넌트 → 새 타이머, 사용 가능 상태.
- Gold Awake → CurrentGold=0, EarnedGold=0.
- WaveManager → 1웨이브, Kills=0.
- 런타임 생성 몬스터/아이템 → 이전 씬과 함께 제거.

기존 사망 시 CurrentGold=0 규칙은 유지한다. 결과에는 EarnedGold(이번 던전에서 실제 획득한 누적량)를 표시한다. 예를 들어 10골드를 얻고 죽으면 전투 HUD의 보유 골드는 0이지만 결과의 Gold earned는 10이다. 이 값은 영구 재화가 아니다.

처치는 기존 웨이브에 등록된 적의 정상 Died 이벤트에서 중복 없이 집계한다. 결과는 웨이브 판정 이후 한 번만 확정하며, 사망과 클리어가 겹치면 기존 사망 우선 규칙을 유지한다. 결과는 정수/불리언 값만 가지며 장비나 씬 오브젝트를 참조하지 않는다.

로비로 가면 결과 객체도 폐기한다. 로비에는 일반 드롭 무기/골드를 반출하지 않는다. 저장 파일, 영구 재화, 해금 상태, 보상 이전은 구현하지 않았다. 추후 영구 데이터는 이 던전 세션과 별도 구조로 연결하면 된다.

## 최초 시작과 빌드

Build Scene List:

1. Assets/Scenes/Lobby.unity
2. Assets/Scenes/SampleScene.unity (던전)

에디터 스크립트가 기본 Play 시작 씬도 Lobby로 지정한다. 이 설정은 편집 중 열린 씬을 바꾸지 않고 Play 시작에만 적용한다.

- Tools → Prototype → Start Play From Lobby: 기본 전체 흐름.
- Tools → Prototype → Start Play From Current Scene: 던전 단독 디버그. 현재 에디터 세션 동안 이 선택을 유지한다.

기본 로비 시작 설정을 확인하려면 Start Play From Lobby를 선택하고 Play한다. 에디터 스크립트 임포트가 끝나기 전이라면 Lobby 씬을 직접 열어 실행해도 된다. 사용자가 별도 Build Profile Scene List 오버라이드를 만든 경우 그 목록에도 두 씬을 같은 순서로 등록해야 한다. 확인한 Assets 설정에는 별도 프로필 오버라이드가 없었다.

## Unity에서 직접 확인할 핵심 테스트

과도한 반복 없이 아래 흐름을 한 번씩 확인한다.

1. **시작**: Play 시 로비가 나오고 장비 정보/청록색 지점/벽·바닥이 보이는지 확인한다.
2. **이동/공격 차단**: WASD와 벽 충돌, 카메라 추적이 동작하고 좌우 클릭으로 공격/마법이 나가지 않아야 한다.
3. **입장 안내**: 오른쪽 청록색 지점에 접근하면 E 안내와 입장 버튼이 활성화되어야 한다.
4. **취소**: E → Cancel 후 로비가 유지되고 WASD 이동이 다시 가능해야 한다.
5. **입장**: E → Enter Dungeon. HP 최대/마나 최대/기본 Club/마법 Ready/웨이브 1/골드 0에서 시작해야 한다.
6. **기존 전투**: 이동·근접 충전/창끝/채찍 띠·마법·마나·드롭·E 획득·랜덤 개체/비교 UI·웨이브가 유지되는지 기본 동작을 확인한다.
7. **사망 복귀**: 일부 적을 처치하고 사망한다. Game Over에 Kills/Wave/Gold earned/클리어 여부와 Return to Lobby/Restart Dungeon이 표시되어야 한다. Return to Lobby 후 로비 이동이 가능하고 드롭 장비는 반출되지 않아야 한다.
8. **클리어 복귀**: 3웨이브를 끝내면 Stage Clear, 처치 15/도달 웨이브 3/획득 골드 75가 표시되어야 한다. 로비로 돌아갈 수 있어야 한다.
9. **재입장/재시작**: 로비 재입장과 결과 화면의 Restart Dungeon 모두 초기 상태여야 한다. 이전 몬스터/바닥 아이템/랜덤 장비/골드가 남지 않아야 한다.
10. **중복 확인**: 전환 버튼을 빠르게 두 번 눌러도 한 번만 이동해야 한다. Hierarchy에 Player/Main Camera/각 씬 Canvas/EventSystem이 중복되지 않고 Console 오류가 없어야 한다.

클리어 확인을 쉽게 하려고 테스트 수치를 바꾼 경우 원래 수치로 되돌린다. 로비/던전 설정을 Play 중 새로 저장하지 않도록 한다.

## 검증 결과와 미확인 사항

- 모든 런타임 C# 코드를 설치된 Unity 참조 어셈블리/Roslyn으로 별도 컴파일: 오류 0.
- 에디터 시작 씬 코드도 UNITY_EDITOR 정의/UnityEditor 참조로 별도 컴파일 통과.
- 씬의 fileID 중복/끊어진 로컬 참조, 각 씬의 Player/Camera 1개, 로비의 전투/획득/웨이브 제외, 첫 빌드 씬 순서, GUID 중복 검사 통과.
- 가짜 의존성을 사용한 실제 씬 흐름/세션/결과 코드 테스트 6개 통과: 중복 요청 차단, 실패 가드 복구, Single 모드/경로, 입장 마나 충전, 사망 전 누적 골드/사망 우선/결과 고정, 클리어 결과.
- 기존 웨이브/골드 핵심 로직 테스트 8개 통과. 처치 수와 사망 후 누적 획득량 보존도 포함했다.
- 실제 Unity 에디터 임포트/컴파일, SceneManager 로드, 이동/카메라/물리/UI 버튼/취소 후 입력/씬 왕복은 직접 실행하지 않았다. 위 필수 Play 테스트는 미확인이다.
- 기존 골드가 사망 시 초기화되어 결과 획득량이 사라지는 구조는 별도 누적 카운터로 해결했다. 확인한 코드/컴파일 범위에서 추가 오류는 발견하지 못했다.
- 상점/영구 성장/저장/반출/스크롤 선택/보스/새 몬스터 AI는 구현하지 않았다.
