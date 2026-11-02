# 3차 프로토타입: 한 방에서 3웨이브 전투

## 유지한 기존 구조

- 기존 EnemyChase2D의 AI/물리/공통 피해/일회성 사망 처리를 재사용했다.
- 근접 타격/처치 마나 보상은 PlayerMelee2D에 그대로 두었다. 마법 처치는 마나를 회복하지 않는다.
- PlayerMovement2D, CameraFollow2D, PlayerMelee2D, PlayerShooting2D, Projectile2D, MagicAttackSettings2D는 이번 단계에서 수정하지 않았다.
- 기존 입력 에셋, 투사체 프리팹, 벽, 바닥, 카메라, 프로젝트 설정은 유지했다.
- 현재 씬의 적 Max Health가 3으로 조절되어 있어 새 Enemy 프리팹에도 3을 보존했다. 원한다면 프리팹에서 변경할 수 있다.

## 추가 기능

- 시작 시 1웨이브. 3 / 5 / 7마리를 기본 1초 간격으로 생성한다. 각 웨이브 첫 적은 시작 직후 생성한다.
- 생성할 적과 살아 있는 적이 모두 0일 때만 완료한다. 웨이브 사이에는 3초 대기한다.
- 마지막 웨이브를 끝내면 Stage Clear를 표시하고 이동/공격/마나 사용과 회복/접촉 피해를 중지한다.
- 처치마다 5골드. 근접/마법 공통이며 같은 적은 한 번만 지급한다. 3웨이브를 모두 처치하면 총 75골드다.
- 사망 시 골드를 즉시 0으로 만들고 생성과 웨이브 진행을 중지한다.
- 마지막 적 처치와 플레이어 사망이 결과 판정 전 같은 프레임에 발생하면 Game Over가 우선이다. 이미 Stage Clear가 확정된 후에는 접촉 피해를 받지 않는다.
- UI에 Wave / Remaining / Alive / Pending / Gold 및 Wave Start / Complete 알림을 추가했다. Stage Clear에서도 기존 Restart 버튼을 사용한다.

## Unity에서 바로 확인하기

1. Play를 끈 상태에서 외부 변경 알림에 Reload를 선택해 SampleScene을 다시 불러온다. 아직 저장하지 않은 별도 씬 편집이 있다면 먼저 다른 씬으로 보존한다.
2. Hierarchy의 WaveSystem에 Enemy Spawner 2D와 Wave Manager 2D가 있는지 확인한다.
3. Player에 Player Gold 2D가 추가되었는지 확인한다.
4. 기존 수동 Enemy는 비활성으로 보존되어 있다. 다시 활성화하면 관리 대상 밖의 적이 추가되므로 웨이브 테스트에서는 꺼 둔다.
5. Assets/Prefabs/Enemy.prefab이 새로 추가되었다. 기존 삼각형 외형/Collider/추적 속도/HP를 복사했고, Player 추적 대상은 생성할 때 주입한다.
6. 아래 참조는 이미 씬에 연결했으므로 새 컴포넌트를 중복 추가할 필요가 없다.
7. Play를 누르고 Game 창을 클릭해 조작한다.

## Inspector 연결 및 주요 수치

| 대상 | 필드 | 연결 / 기본값 |
|---|---|---|
| WaveSystem / Enemy Spawner 2D | Enemy Prefab | Assets/Prefabs/Enemy.prefab |
| 동일 | Player | 기존 Player Transform |
| 동일 | Room Bounds | X=-7.5, Y=-4.5, Width=15, Height=9 |
| 동일 | Minimum Player Distance | 3 (중심 간 거리) |
| 동일 | Spawn Interval | 1초 |
| 동일 | Minimum Spawn Radius | 0.8 |
| 동일 | Clearance Padding | 0.1 |
| 동일 | Attempts Per Spawn | 24 |
| 동일 | Blocking Layers | Everything |
| WaveSystem / Wave Manager 2D | Spawner | 같은 오브젝트의 Enemy Spawner 2D |
| 동일 | Player Health / Player Gold | 기존 Player의 해당 컴포넌트 |
| 동일 | Enemies Per Wave | Size=3, 값 3 / 5 / 7 |
| 동일 | Transition Duration | 3초 |
| 동일 | Gold Per Kill | 5 |
| 동일 | Announcement Duration | 2초 |
| GameUI / Game HUD 2D | Waves | WaveSystem의 Wave Manager 2D |
| 동일 | Player Health | 기존 Player Health 참조 유지 |

Wave 수는 Enemies Per Wave 배열의 Size로, 각 몬스터 수는 배열 값으로 조절한다. 각 값은 1 이상이어야 하며 잘못된 필수 설정은 Console 오류와 함께 진행을 중지한다. 배열/프리팹/참조 변경은 Play 전에 적용한다.

## 생성 위치 판정

Room Bounds는 **벽 안쪽 공간의 월드 좌표**다. WaveSystem Transform을 이동해도 이 사각형은 이동하지 않는다. 선택 시 Scene 창에 청록색 사각형으로 표시한다.

기본 반경 0.8과 프리팹 루트 PolygonCollider2D의 실제 점/Offset/Scale로 계산한 반경 중 큰 쪽을 사용한다. 방 경계에서도 이 반경만큼 안쪽에 생성한다. 후보 위치는 플레이어와 최소 3만큼 떨어져 있어야 하고, 원형 여유 영역에 벽/장애물/플레이어/기존 적 Collider가 없어야 한다. Trigger와 투사체는 생성 차단물에서 제외한다.

현재 프리팹은 루트 PolygonCollider2D 하나를 사용한다. 향후 더 큰 자식 Collider를 추가한다면 Minimum Spawn Radius를 그 전체 형상을 감쌀 만큼 키운다. 바닥에는 기존처럼 Collider를 붙이지 않는다. Blocking Layers를 줄일 때는 벽/장애물/적/플레이어의 레이어를 반드시 포함한다.

위치를 한 번에 최대 24회(코드 상한 256회) 찾고 실패하면, 몬스터 생성 수를 증가시키지 않고 생성 간격 후에 재시도한다. 매 프레임 무한 반복하지 않는다. UI에 Spawn blocked - retrying...을 표시하고 Console 경고는 실패 상태 진입 때 한 번만 출력한다. 공간이 생기면 자동으로 복구한다. 유효 위치가 영구적으로 없으면 웨이브도 완료되지 않으므로 Room Bounds/거리/장애물을 수정해야 한다.

## 게임 상태

| 상태 | 동작 |
|---|---|
| Playing | 이동/근접/마법 가능, 생성과 처치 진행 |
| WaveTransition | 이동/근접/마법 가능, 3초 동안 생성 대기 |
| GameOver | 기존 사망 정지, 생성/웨이브 진행 중지, 골드 0, Game Over + Restart |
| StageClear | 생성/플레이어 이동/공격 중지, 골드 유지, Stage Clear + Restart |

GameOver는 전체 Time.time을 멈추지는 않는다. 이미 존재하는 적과 투사체는 기존처럼 움직일 수 있지만 웨이브는 진행하지 않고 추가 골드도 지급하지 않는다.

관리 대상은 스포너가 생성하고 WaveManager에 등록한 적이다. 비활성으로 보존한 수동 Enemy를 임의로 활성화하면 웨이브/골드 집계에 포함되지 않는다.

## Play 모드 테스트 절차 (실제 실행 필요)

### 웨이브와 보상

1. 시작 시 Wave 1 / 3, Remaining 3, Gold 0 및 Wave 1 Start를 확인한다.
2. 약 1초 간격으로 총 3마리가 나타나는지 확인한다. Pending은 생성마다 줄고 Alive는 증가하며 Remaining은 생성만으로 줄지 않아야 한다.
3. 첫 적을 다음 적이 생성되기 전에 처치해도 웨이브가 종료되지 않아야 한다.
4. 근접/마법으로 처치할 때마다 골드가 5씩 늘고, 마지막 적까지 처치하면 15가 되는지 확인한다.
5. Wave 1 Complete 알림과 3초 대기 후 Wave 2 Start를 확인한다. 전환 중에도 WASD/근접/마법은 가능해야 한다.
6. 2웨이브는 5마리, 3웨이브는 7마리인지 확인한다. 완료 누적 골드는 15 → 40 → 75다.
7. 마지막 적 처치 후 Stage Clear/Restart가 표시되고 이동/근접/마법이 멈추는지 확인한다.
8. 적 한 마리에 여러 Collider를 임시로 붙이거나 여러 공격을 동시에 맞혀도 한 번만 사망/집계/보상되는지 확인한다. 테스트 후 추가 Collider를 제거한다.
9. 근접의 타격 +10/처치 추가 +15 마나 보상과 마법 처치 회복 없음이 유지되는지 확인한다. 기본 적 HP는 현재 3이므로 근접 세 번 또는 마법 피해 누적으로 처치한다.

### 생성 안전성

1. 적이 벽/플레이어/다른 적 내부에 생기지 않고 방 안쪽에 나타나는지 확인한다.
2. 내부에 임시 장애물(BoxCollider2D, Is Trigger 끔)을 놓고 그 내부에 생성되지 않는지 확인한다.
3. Play 전에 Minimum Player Distance를 100으로 바꾸어 강제로 생성 위치가 없게 만든다. UI에 재시도가 표시되고 게임이 멈추거나 웨이브가 자동 완료되지 않는지 확인한다.
4. 실행 중 Minimum Player Distance를 3으로 돌리면 다음 재시도에 생성이 재개되는지 확인한다. 종료 후 저장된 테스트 수치도 원래 값 3인지 확인한다.
5. 외부 스크립트/Hierarchy에서 생성된 적을 피해 처리 없이 제거하면 골드를 주지 않고 해당 생성 수를 복구해 새 적을 보충한다. Destroy는 처치로 인정하지 않는다.

### 사망 및 재시작

1. 일부 처치로 골드를 얻은 뒤 적과 접촉해 사망한다. 체력 0, 골드 0, Game Over/Restart가 표시되어야 한다.
2. 몇 초 기다려도 새 적이 생성되거나 다음 웨이브로 넘어가지 않아야 한다.
3. 전환 대기 중 사망했을 때도 다음 웨이브가 시작되지 않는지 확인한다(일반 흐름에서는 남은 적이 없으므로 별도 테스트 배치가 필요할 수 있다).
4. Game Over의 Restart를 누르면 체력/마나/쿨타임/플레이어 위치/적/웨이브 1/골드 0/UI가 초기화되어야 한다.
5. Stage Clear의 Restart도 동일하게 초기화되고 즉시 다시 조작할 수 있어야 한다.
6. 여러 번 재시작해 적/골드 이벤트나 UI가 중복되지 않는지 확인한다.
7. Console에 컴파일/실행 오류가 없는지 확인한다.

## 생성 파일

- Assets/Scripts/Waves/EnemySpawner2D.cs — 안전한 위치 선택과 적 생성/추적 대상 주입.
- Assets/Scripts/Waves/WaveManager2D.cs — 웨이브/4가지 상태/집계/사망 우선/골드 지급.
- Assets/Scripts/Player/PlayerGold2D.cs — 플레이 중 골드와 사망 초기화. 소비/영구 저장 없음.
- Assets/Prefabs/Enemy.prefab — 기존 삼각형 적을 복사한 자동 생성용 프리팹.
- 위 에셋/스크립트 및 Waves 폴더의 .meta — 고정 GUID 참조.
- Tests/Standalone/WaveProgressionTests.cs — 실제 WaveManager/PlayerGold 소스에 가짜 의존성을 연결하는 상태 로직 검증.
- Tests/Standalone/Run-WaveProgressionTests.ps1 — 로컬 Unity 번들 .NET SDK로 위 테스트를 컴파일/실행하는 선택적 도구. 실행 정책이 스크립트를 허용하는 환경에서 사용한다.
- PROTOTYPE_V3_SETUP.md — 이 보고서.

## 수정 파일

- Assets/Scripts/Enemy/EnemyChase2D.cs — 일회성 사망/제거 이벤트 및 생성 후 추적 대상 연결 메서드. 기존 AI/체력/피해는 유지.
- Assets/Scripts/Player/PlayerHealth2D.cs — 사망 이벤트와 StageClear 정지/피해 차단. 기존 사망 조작 정지 코드를 재사용.
- Assets/Scripts/Player/PlayerMana2D.cs — 사망 외에 클리어 확정 후에도 소비/회복 차단.
- Assets/Scripts/UI/GameHUD2D.cs — 웨이브/적 수/골드/알림 및 공통 결과 화면. Restart 허용 조건 확장.
- Assets/Scenes/SampleScene.unity — WaveSystem, PlayerGold, UI 참조 추가. 기존 수동 Enemy 비활성 보존.

## 검증 결과 및 한계

- 설치된 Unity 참조 어셈블리와 Roslyn으로 모든 런타임 스크립트를 별도 컴파일: 오류 0. Inspector 연결 필드의 CS0649 경고는 남아 있다.
- 씬/적 프리팹의 중복 fileID와 끊어진 로컬 참조, 에셋 GUID 중복 여부 검사 통과. 기존 투사체 연결/적 HP 3/웨이브 3·5·7 연결 확인.
- 독립 상태 로직 테스트 8개 통과: 미생성 적을 포함한 완료 판정, 3초 전환, 총 15마리/75골드/클리어, 중복 처치, 사망 정지/골드 초기화, 동시 사망 우선, 생성 실패 재시도, 외부 제거 보충.
- 테스트는 실제 WaveManager2D/PlayerGold2D 소스를 사용하지만 Unity 시간/생성기/적/플레이어는 가짜 의존성으로 대체했다. Unity 물리/입력/화면/UI/생명주기를 실행한 테스트가 아니다. 새 인스턴스 초기화 검증도 실제 SceneManager 재로드 검증을 대신하지 않는다.
- 현재 환경은 .ps1 실행 정책으로 선택적 실행 도구가 차단되어, 해당 도구 대신 컴파일러와 생성된 테스트 DLL을 직접 명령으로 실행했다. 시스템 실행 정책은 변경하지 않았다.
- Unity 에디터 임포트/컴파일과 Play 모드, 실제 생성 위치/충돌/조작감/UI 버튼/씬 재시작은 직접 확인하지 않았다.

## 기존 시스템과 맞물리는 부분

- 적 사망 이벤트는 즉시 집계하지만 웨이브 완료는 공격 처리 이후 LateUpdate에서 결정한다. 근접 처치 마나 보상이 적용된 뒤 클리어 정지가 적용된다.
- GameOver/StageClear는 PlayerHealth2D의 공통 조작 정지를 사용하며 전체 시간 배율을 변경하지 않는다.
- 골드는 WaveManager에 등록된 적만 지급한다. 같은 적의 Died/Removed 중복 통지는 집계/지급을 반복하지 않는다.
- 전투 중 외부 Destroy는 정상 처치와 구분하여 대체 생성한다. Scene 재로드에서는 기존 오브젝트와 이벤트 구독이 정리된다.
- 테스트용 적/외형/맵은 유지했으며 보스·상점·성장·장비·랜덤 보상은 추가하지 않았다.
