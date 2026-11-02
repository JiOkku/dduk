# 10차 프로토타입: 속성별 스테이지 진행

후속 11차에서 마지막 웨이브 뒤 보스 전투와 마석 정산을 추가했다. 현재 동작 및 테스트는 [PROTOTYPE_V11_SETUP.md](PROTOTYPE_V11_SETUP.md)를 우선 참고한다.

기존 Unity 6000.6.7f1, Input System 1.20.1과 로비/전투 씬을 유지한다. 새 씬, 플레이어, 카메라, 별도 웨이브 관리자는 만들지 않았다.

## 플레이 흐름

Lobby에서 시작 스테이지 선택 → SampleScene의 선택한 스테이지 → Stage Clear / Next Stage → 다음 스테이지 … Stage 6 → Dungeon Clear → 로비.

로비의 Dungeon Entrance 버튼 오른쪽에 시작 스테이지와 `< Previous` / `Next >` 버튼이 표시된다. 원하는 스테이지를 선택한 뒤 기존처럼 입구에 접근해 E키 또는 Dungeon Entrance 버튼으로 확인창을 열고 입장한다. 확인창에도 선택한 스테이지 이름이 표시된다. 예를 들어 Stage 4를 선택하면 4 → 5 → 6 순서로 진행하며, 건너뛴 1~3의 보상은 지급하지 않는다. 취소하면 로비와 선택을 유지한다.

Dungeon Restart는 이번 입장 때 선택한 시작 스테이지로 돌아간다. 로비 복귀 후에도 선택은 유지하지만 플레이어 체력/마나/무기/웨이브/골드 등의 전투 데이터는 새로 초기화한다. 선택은 현재 실행 중에만 보관하며, 게임을 새로 실행하면 기본 Stage 1이다. 잘못되었거나 삭제된 스테이지 ID는 Stage 1로 대체한다.

각 스테이지는 기존 3/5/7마리의 3웨이브, 생성 간격 1초, 웨이브 대기 3초를 사용한다. 적 처치당 골드 5는 유지한다. 중간 클리어는 영구 보상 정산을 실행하지 않는다.

| 스테이지 | 속성 | 체력 배율 | 접촉 피해 배율 | 이동 배율 | 클리어 임시 골드 |
|---|---|---|---|---|---|
| 1 | Fire | 1.0 | 1.0 | 1.0 | 10 |
| 2 | Grass | 1.2 | 1.0 | 1.0 | 15 |
| 3 | Water | 1.4 | 1.2 | 1.0 | 20 |
| 4 | Fire | 1.6 | 1.2 | 1.0 | 25 |
| 5 | Grass | 1.8 | 1.4 | 1.0 | 30 |
| 6 | Water | 2.0 | 1.4 | 1.0 | 35 |

## 생성한 파일

- `Assets/Scripts/Waves/DungeonStageData2D.cs`: 스테이지 SO, 웨이브와 적 그룹 데이터. 각 그룹은 기존 적 프리팹, 속성 데이터, 마릿수를 가진다. 그룹을 여러 개 등록하면 같은 웨이브에서 서로 다른 적 구성을 순서대로 생성한다.
- `Assets/Scripts/Waves/DungeonStageProgress2D.cs`: 던전의 현재 스테이지와 Preparing / Fighting / StageCleared / Transitioning / DungeonCleared / GameOver 상태. 중복 완료와 중복 전환을 거부한다.
- `Assets/Scripts/Waves/DungeonStageCatalog2D.cs`, `Assets/Settings/Stages/DungeonStages.asset` 및 `.meta`: 로비와 던전이 공유하는 스테이지 목록. 양쪽이 같은 순서와 ID를 사용한다.
- `Assets/Scripts/Enemy/EnemyElementData2D.cs`: 색상과 독립된 적 속성 데이터. 색상을 변경해도 속성 값은 바뀌지 않는다.
- `Assets/Settings/Stages/Stage01_Fire.asset`, `Stage02_Grass.asset`, `Stage03_Water.asset`, `Stage04_Fire.asset`, `Stage05_Grass.asset`, `Stage06_Water.asset`.
- `Assets/Settings/Enemies/FireEnemy.asset`, `GrassEnemy.asset`, `WaterEnemy.asset`.
- 위 Unity 스크립트/에셋과 새 폴더의 `.meta` 파일.
- `Tests/Standalone/DungeonStageProgressTests.cs`: 실제 진행 상태 코드의 중복 완료/전환, 사망 우선, 새 플레이 초기화 검사.
- `Backups/SampleScene.before-v10.unity.txt`: 수정 전 던전 씬 백업. Unity에 추가 씬으로 임포트되지 않도록 Assets 밖의 텍스트로 보관했다.
- 이 문서.

## 수정한 파일

- `Assets/Scripts/Waves/WaveManager2D.cs`: 기존 생성/처치/웨이브 진행 재사용. 스테이지 데이터 적용, 중간 클리어, 중복 방지, 정리와 다음 스테이지 시작을 통합했다. 기존 `StageClear` enum 값은 최종 클리어 의미로 유지하고 `StageCompleted`, `StageTransition`을 추가했다.
- `Assets/Scripts/Waves/EnemySpawner2D.cs`: 기존 유효 위치 탐색과 생성 간격을 유지하며 그룹의 프리팹과 스테이지/속성 데이터를 받아 생성한다.
- `Assets/Scripts/Enemy/EnemyChase2D.cs`: 생성된 개체에 체력/이동 배율, 접촉 피해 배율, 속성 및 색상을 적용한다. AI, 피격과 사망 이벤트는 유지했다.
- `Assets/Scripts/Player/PlayerHealth2D.cs`: 기존 접촉 피해량에 적의 배율을 적용한다. 중간 클리어에서 조작을 정지하고 다음 스테이지에서 기존 활성 상태와 Rigidbody 제약을 복구한다.
- `Assets/Scripts/Rewards/TreasureChest2D.cs`: 같은 상자를 다음 스테이지에서 재사용하도록 개봉 상태와 색상/사망 이벤트 구독을 초기화하는 메서드를 추가했다.
- `Assets/Scripts/Flow/DungeonRunResult2D.cs`: 도달 스테이지를 결과에 추가했다.
- `Assets/Scripts/Flow/DungeonSession2D.cs`: 기존 사망/최종 클리어 결과에 도달 스테이지를 기록한다. 중간 클리어에서는 결과/보상 정산을 만들지 않는다.
- `Assets/Scripts/UI/GameHUD2D.cs`: 스테이지/전체 수/속성, 중간 클리어 보상과 Next Stage, 최종 Dungeon Clear와 도달 스테이지 표시.
- `Assets/Scenes/SampleScene.unity`: 기존 WaveSystem에 6개 스테이지와 Floor SpriteRenderer를 연결했다. 씬 이름과 GUID, 플레이어/카메라/상자 및 기존 프리팹 참조는 유지했다.
- 로비 선택 후속 변경: `LobbyController2D.cs`에 선택 UI와 확인 문구를 추가하고, `SceneFlow2D.cs`에 입장 스테이지 ID 전달을 추가했다. `Lobby.unity`와 `SampleScene.unity`에 같은 Stage Catalog를 연결했다. 추가 수정 전 백업은 `Backups/Lobby.before-stage-selection.unity.txt`, `Backups/SampleScene.before-lobby-stage-selection.unity.txt`다.

## Inspector 설정과 연결

이미 기존 던전 씬에 연결해 두었으므로 필수 수동 연결은 없다. Play를 중지하고 Unity의 임포트를 기다린다. 외부 씬 변경 안내가 뜨면 디스크 변경을 적용하려는 경우 Reload를 선택한다. 에디터에서 따로 저장하지 않은 변경이 있으면 먼저 보존한다.

1. **Assets/Settings/Stages/DungeonStages.asset → Stages**에 6개 에셋이 순서대로 들어 있는지 확인한다. 이 목록의 다음 항목이 다음 스테이지다. 배열 순서/크기가 진행 순서/전체 수를 결정하며 코드 수정은 필요 없다. 같은 Stage ID를 중복 등록하면 시작 시 오류를 표시한다. SampleScene의 WaveSystem → Wave Manager 2D → Stage Catalog와 Lobby의 LobbyController2D → Stage Catalog는 모두 이 에셋에 연결되어 있다.
2. 속성을 바꾸려면 해당 Stage 에셋의 **Element**, 각 Waves/Enemies의 **Enemy Data**, **Environment Color**를 함께 설정한다. 현재 순서는 개별 데이터로 Fire / Grass / Water / Fire / Grass / Water를 지정했다. 색상에서 속성을 추측하지 않는다.
3. 각 Stage 에셋의 **Waves → Enemies**에서 **Prefab**, **Enemy Data**, **Count**를 설정한다. 기본은 기존 `Assets/Prefabs/Enemy.prefab`이다. 다른 드롭 테이블은 기존 Enemy를 복제한 프리팹의 EnemyWeaponDrop2D.Drop Table에 지정한 뒤 해당 그룹 Prefab에 연결한다. 새 AI는 추가하지 않았다.
4. **Health Multiplier / Damage Multiplier / Speed Multiplier / Clear Gold**로 난이도와 보상을 조절한다. **Environment Renderer**는 기존 Floor SpriteRenderer에 연결되어 있다.
5. 현재 상자 1개는 기존 씬의 위치를 그대로 사용한다. 추가 상자를 만들 필요는 없다. 상자는 스테이지 이동 시 초기화되며, 같은 스테이지에서는 기존 Opened 검사로 재개봉을 막는다. 상자 위치/보상 수치는 기존 TreasureChest 프리팹 또는 씬 인스턴스에서 조정한다.
6. HUD는 기존처럼 코드로 생성한다. Stage 텍스트와 Next Stage 버튼을 Canvas에 수동으로 추가하거나 OnClick을 연결할 필요가 없다. GameHUD의 Player Health / Waves / Session 기존 참조를 유지한다.

## 전환과 데이터 유지 규칙

- 다음 스테이지 버튼은 중간 클리어에서만 허용한다. 첫 입력에서 즉시 Transitioning으로 바뀌므로 같은 프레임의 추가 입력은 거부된다. 정리 후 한 프레임 기다렸다가 다음 스테이지의 Wave 1을 시작한다.
- 플레이어와 카메라는 계속 같은 개체다. 현재 HP, 마나, 장착 무기 개체와 랜덤 수치, 장착 마법, 임시 골드와 누적 처치/상자 보상을 유지한다. 자동 회복은 없다.
- 기존 공격 타이머는 재설정하지 않는다. 시간은 클리어 화면에서도 흐르므로 쿨타임은 자연스럽게 경과한다. 몽둥이의 충전 시간은 기존 공격 컴포넌트의 활성화 규칙에 따라 재시작한다.
- 바닥 무기는 기존 Consume을 사용해 비활성화/제거한다. 투사체와 이전 적도 제거하며, 적을 사망시키는 메서드는 호출하지 않는다. 정리로 처치 보상/드롭/마나를 지급하지 않는다.
- 스테이지 클리어 임시 골드는 완료가 처음 승인될 때 한 번만 지급한다. 모든 기본 스테이지 완료 시 처치 90, 처치 골드 450 + 클리어 골드 135 = 총 임시 골드 585다.
- 상자 보상은 기존 던전 보상 목록에 계속 누적한다. 매 스테이지 상자를 전부 열면 토큰 60개와 보상 무기 6개가 최종 클리어 정산 대상이다. 개발용 보스 보상은 별도다.
- 최종 스테이지에서만 기존 Finish 정산을 호출한다. 일반 몬스터 무기/임시 골드는 반출하지 않으며, 사망 시 반출 가능 보상도 기존 규칙대로 소실된다.
- 로비 복귀/던전 재시작은 기존 Single 씬 로드를 사용한다. 새 던전에서는 선택한 시작 스테이지, 기본 몽둥이, 최대 HP/마나, 골드 0, 새 상자로 시작한다. 로비의 장착 마법과 영구 보상은 유지한다.
- 저장 형식과 저장 코드는 변경하지 않았다. 진행 중인 스테이지는 로컬 저장 대상이 아니다.

## 수치 적용과 주의점

체력과 접촉 피해는 모두 `Mathf.RoundToInt(기존 값 × 배율)` 후 최소 1로 제한한다. 이동 속도는 기존 프리팹 속도에 배율을 곱한다. ScriptableObject와 프리팹 원본은 런타임에 수정하지 않는다.

현재 적 기본 HP가 2이므로 실제 HP는 2 / 2 / 3 / 3 / 4 / 4다. 접촉 피해는 실제 프로젝트의 기존 구조를 유지해 PlayerHealth의 Contact Damage(현재 1)를 기준으로 계산한다. 따라서 요청한 1.0~1.4 배율에서는 반올림 후 모두 피해 1이다. 피해 2를 테스트하려면 Stage의 Damage Multiplier를 1.5 이상으로 올리면 된다. 임의로 기본 피해나 요청 배율을 바꾸지는 않았다.

기존 적 AI/드롭 테이블/공격/마나 회복/마법 데이터는 유지했다. 속성 상성, 저항, 상태이상, 보스 및 고유 맵은 구현하지 않았다. 의도적으로 같은 방과 같은 추적 AI를 재사용한다.

## Unity Play 모드 수동 확인

1. 로비에서 사용할 마법을 장착한다. Previous / Next로 Stage 4를 선택하고 E키 확인창의 이름이 일치하는지 확인한다. 한 번 취소 후 다시 입장해 Stage 4 / 6, Fire, Wave 1 / 3과 최대 HP/마나·몽둥이·골드 0을 확인한다. 재시작도 Stage 4부터 시작해야 한다. 아래 전체 진행 검사는 로비에서 Stage 1을 선택해 진행한다.
2. 상자를 E로 열고 같은 스테이지에서 두 번 열 수 없는지 확인한다. 몬스터 드롭 무기를 주워 개체 수치와 마나/HP/골드를 기록한다.
3. 3웨이브를 모두 완료하면 Stage Clear와 Gold +10, Next Stage가 표시되어야 한다. 이때 공격·마법·이동·획득은 멈추고 로비 보상 정산은 아직 실행되지 않아야 한다.
4. Next Stage를 연속 클릭한다. Stage 2 / 6, Grass, Wave 1 / 3으로 한 단계만 이동해야 한다. HP/마나/장착 무기 수치/골드가 그대로인지, 바닥 무기가 사라졌는지, 상자가 다시 한 번 열리는지 확인한다.
5. 전환 후 WASD/벽 충돌/조준/좌클릭/우클릭/E키가 다시 작동하는지 확인한다. 몬스터 색상과 EnemyChase의 실제 체력을 확인한다. 피해 배율 검증 시 위 정수 반올림 규칙에 주의한다.
6. 다음 스테이지도 반복해 Fire → Grass → Water → Fire → Grass → Water 순서와 스테이지별 골드 추가 지급을 확인한다. 테스트 시간을 줄이려면 에셋 수치를 기록한 뒤 각 웨이브 마릿수를 잠시 1로 낮추고 확인 후 복구한다.
7. 마지막 스테이지에서는 Next Stage 대신 Dungeon Clear, 누적 처치/도달 스테이지·웨이브/골드/반출 및 소실 내역, 로비 복귀/재시작 버튼이 표시되어야 한다.
8. 중간 스테이지에서 사망하는 별도 플레이를 확인한다. Game Over에 현재 스테이지와 웨이브가 표시되고, 상자 보상이 반출되지 않아야 한다.
9. 로비 복귀 후 영구 보상과 장착 마법을 확인하고 시작 스테이지를 다시 선택해 재입장한다. 선택한 스테이지의 Wave 1과 초기 상태로 시작하며 이전 적/아이템/UI/플레이어/카메라가 남지 않아야 한다.

## 수행한 검사와 한계

- Unity 설치에 포함된 컴파일러와 Unity 참조를 사용한 런타임/에디터 조건부 코드 컴파일 통과.
- 실제 DungeonStageProgress 코드로 중복 완료/버튼 입력, 동시 사망 우선, 전환 중 사망, 새 플레이 초기화, 선택 스테이지 시작/재시작 및 남은 스테이지 진행의 독립 시나리오 4개 통과.
- 6개 Stage 에셋의 고유 ID, 3/5/7 구성, GUID 존재, 씬 연결 검사 통과.
- 기존 보상/마법 저장 회귀 검사 13개 통과. Unity API 대역 및 임시 파일을 사용했으며 실제 사용자 영구 저장은 수정하지 않았다.
- Unity 에디터의 Play 모드는 직접 실행하지 않았다. UI 배치, 실제 입력/물리 충돌, 조작 복구와 전체 스테이지 플레이는 위 절차로 수동 확인해야 한다. 실제 Play 검증이 남아 있으므로 런타임 오류가 없다고 단정하지 않는다.
