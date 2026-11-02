# 11차 프로토타입: 보스와 마석

후속 12차에서 기존 마석을 사용하는 마법 해금/영구 성장을 추가했다. 성장 설정과 저장 확장은 [PROTOTYPE_V12_SETUP.md](PROTOTYPE_V12_SETUP.md)를 참고한다.

후속 확인: 몬스터 미생성 제보 후 Unity에서 스크립트/에셋을 다시 불러오고 SampleScene Play를 실행해 1웨이브 3마리 생성과 추적을 확인했다. 최초 실패 로그에는 검증 실패 항목이 없어서 원인을 특정하지는 못했다. WaveManager의 오류 메시지에 실패 항목을 추가했고, `Tools/Prototype/Audit Combat Configuration`으로 현재 Unity 참조를 읽어 `Logs/combat-config-audit.txt`에 기록할 수 있다. 보스 전투와 전체 반출의 실제 Play 검증까지 완료한 것은 아니다. 확인 후 Play를 종료하고 기본 로비 시작 설정을 복구했다.

상자 안내는 실제 무기 이름과 함께 `lobby reward, not equipped`를 표시한다. 상자 무기는 현재 장착이 아니라 최종 클리어 시 로비 목록에 반출되는 보상이며 사망/중간 포기 시 소실된다. 기존 지급 규칙은 바꾸지 않았다.

10차의 로비 시작 스테이지 선택, 공통 카탈로그, 선택 스테이지 재시작을 유지했다. Unity 6000.6.7f1 / Input System 1.20.1을 그대로 사용한다.

## 구현한 흐름

로비에서 선택한 Stage ID로 입장 → 일반 3웨이브 → Boss Approaching → 보스 전투 → 보스 처치 → 마석 임시 등록 → Stage Clear → 다음 스테이지 또는 보상을 포기하고 로비 복귀.

마지막 스테이지 보스까지 처치하면 Dungeon Clear와 기존 최종 정산을 실행한다. 중간 클리어의 `Leave (Lose Rewards)`는 미정산 상자/보스 보상을 포기하고 로비로 돌아간다. 마석을 반출하려면 마지막 스테이지까지 클리어해야 한다.

| 스테이지 | 보스 / ID | 패턴 순서 | 실제 HP | 보스 피해 배율 |
|---|---|---|---|---|
| 1 | Flame Boss / flame_boss | 돌진 → 투사체 | 20 | 1 |
| 2 | Grass Boss / grass_boss | 범위 → 투사체 | 25 | 1 |
| 3 | Water Boss / water_boss | 돌진 → 범위 | 30 | 1 |
| 4 | Flame Boss / flame_boss | 돌진 → 투사체 | 30 | 1 |
| 5 | Grass Boss / grass_boss | 범위 → 투사체 | 38 | 1 |
| 6 | Water Boss / water_boss | 돌진 → 범위 | 45 | 1 |

강화형은 같은 보스 데이터/ID/프리팹을 재사용하고 Stage의 Boss Health Multiplier만 1.5로 설정했다. 체력은 기존처럼 RoundToInt 후 최소 1이므로 25 × 1.5는 38이다. 보스에는 일반 몬스터 Health / Damage / Speed Multiplier를 추가로 곱하지 않는다. 기본 접촉 피해와 각 패턴 피해는 현재 모두 1이다.

## 생성한 파일

- `Assets/Scripts/Enemy/BossData2D.cs`: 보스 ScriptableObject와 직렬화 가능한 공통 패턴 설정.
- `Assets/Scripts/Enemy/BossController2D.cs`: 추적, 패턴 순환, 예고 표시, 물리 돌진, 적대 투사체, 범위 공격, 피격 시 흰색 표시 및 사망/정리.
- `Assets/Prefabs/Boss.prefab`: 기존 삼각형 Enemy를 바탕으로 만든 공통 보스 프리팹. 크기 1.4배, 기존 Rigidbody2D/PolygonCollider2D/EnemyChase2D를 사용한다. 일반 무기 드롭 컴포넌트 대신 BossController2D를 연결했다.
- `Assets/Settings/Bosses/FlameBoss.asset`, `GrassBoss.asset`, `WaterBoss.asset`.
- 위 파일과 새 Bosses 폴더의 `.meta`.
- `Backups/before-v11-stages/Stage01_Fire.asset.txt`부터 `Stage06_Water.asset.txt`까지: 수정 전 스테이지 데이터 백업 6개.
- 이 문서.

## 수정한 파일

| 파일 | 변경 역할 |
|---|---|
| `Assets/Scripts/Enemy/EnemyChase2D.cs` | 보스의 실제 HP/속성/접촉 피해 설정, 보스 이동 제어 연결, 기존 피격 반응 조회 및 피격 시점 제공. 기존 피해/사망 메서드 재사용 |
| `Assets/Scripts/Player/PlayerHealth2D.cs` | 접촉·보스 투사체·범위 공격이 같은 TryTakeDamage와 무적 시간을 사용하도록 통합. 일반 적 접촉 피해 규칙 유지 |
| `Assets/Scripts/Combat/Projectile2D.cs` | 기존 이동/벽 충돌/수명과 충돌 무시 구조에 적대 투사체 발사 추가. 플레이어 마법 분기는 유지 |
| `Assets/Scripts/Player/PlayerShooting2D.cs` | BossIntro / BossFight에서도 기존 우클릭 마법 허용 |
| `Assets/Scripts/Waves/DungeonStageData2D.cs` | Boss, Boss Health/Damage Multiplier, Boss Intro Duration 추가 |
| `Assets/Scripts/Waves/DungeonStageProgress2D.cs` | BossFighting 상태와 보스 생존 중 완료 차단. 보스 사망 후에만 완료 가능 |
| `Assets/Scripts/Waves/WaveManager2D.cs` | 마지막 웨이브 후 보스 생성/재시도, 보스 사망 확인, 마석 등록 후 완료, 중복 보스/보상 방지 |
| `Assets/Settings/Stages/Stage01_Fire.asset` ~ `Stage06_Water.asset` | 속성별 보스 데이터와 전용 배율 연결 |
| `Assets/Scripts/Rewards/RewardData2D.cs` | MagicStone 보상 종류, 재화 ID, 보스 보상 기록 및 영구 프로필 필드 추가 |
| `Assets/Scripts/Rewards/RewardSaveStore2D.cs` | 마석과 최초 처치 ID를 기존 원자적 저장 작업에 함께 반영. 구버전 선택 필드 보완 |
| `Assets/Scripts/Rewards/DungeonRewards2D.cs` | 보스 이벤트별 임시 보상 등록, 최초/반복 예상 수량, 결과의 반출/소실 표시 |
| `Assets/Scripts/Rewards/LobbyRewardsUI2D.cs` | 마석 보유량과 최근 클리어 획득량 표시 |
| `Assets/Scripts/UI/GameHUD2D.cs` | 보스 이름/실제 체력바, 중간 클리어 마석 수량, 최종 결과와 중간 포기 복귀 버튼 |
| `Tests/Standalone/DungeonStageProgressTests.cs` | 보스 생존 중 완료 차단, 중복 보스 단계 및 동시 사망 우선 검사 |
| `Tests/Standalone/RewardPersistenceTests.cs` | 최초/반복/다른 보스 ID, 냉간 재로드, 중복 정산, 실패 후 재시도, 사망/재시작 폐기, 구버전 저장 호환 검사 |
| `PROTOTYPE_V10_SETUP.md` | 최신 보스 단계 문서 안내 추가 |

`LobbyController2D.cs`, `SceneFlow2D.cs`, `DungeonStageCatalog2D.cs`, `DungeonStages.asset`, `Lobby.unity`, `SampleScene.unity`의 기존 스테이지 선택 및 씬 연결은 변경하지 않았다. 별도 씬이나 DontDestroyOnLoad 개체를 추가하지 않았다. 최근 추가한 평타 충전 발밑 원도 유지했다.

## 보스 데이터 / Inspector

`Assets/Settings/Bosses`의 각 에셋에서 다음을 조절한다.

- **Boss Id**: 영구 최초 처치 기록의 키. 이름 변경은 가능하지만 ID 변경은 다른 보스로 취급된다. 서로 다른 보스 에셋에 같은 ID를 중복 등록하면 던전 시작 검증에서 오류를 표시한다.
- **Boss Name / Element / Color**: 표시 이름, 실제 속성 데이터, 임시 외형 색상. 상성이나 저항은 없다.
- **Max Health / Move Speed / Contact Damage**: 기본 전투 수치.
- **Attack Interval**: 패턴 사이의 최소 대기. 최초 공격 전에도 대기한다.
- **Patterns**: 등록 순서대로 반복한다. 랜덤 패턴 선택은 하지 않는다.
- **Prefab**: `Boss.prefab`의 EnemyChase2D 컴포넌트. 보스 데이터 3개는 같은 프리팹을 사용한다.
- **Projectile Prefab**: 기존 `Projectile.prefab`의 Projectile2D 컴포넌트. 플레이어 투사체와 코드를 공유하지만 보스가 발사할 때 적대 투사체로 초기화한다.
- **Effect Material**: 기존 `MagicEffect.mat` 연결. 보스 프리팹의 도형과 예고 선은 단순한 Unlit 표시를 사용한다.
- **First Kill Stones / Repeat Kill Stones**: 최초 5, 반복 2. 0으로 설정해도 클리어 시 최초 처치 기록은 확정된다.

각 패턴 항목에는 Kind, Damage, Warning Time, Recovery Time, Cooldown, Range, Radius, Dash Speed/Duration, Projectile Speed/Lifetime이 있다. 해당 패턴에 필요한 필드만 적용한다.

기본 예고 0.8초, 회복 0.6초, 패턴 쿨타임 1.5초다. 회복 종료 후 `max(보스 Attack Interval, 패턴 Cooldown)`만큼 대기하며, 대기 중에는 추적한다. 회복과 예고 중에는 기본 추적을 멈춘다.

- **Dash**: 플레이어 방향을 미리 고정하고 빨간 선으로 예고한다. 속도 8, 최대 0.6초이며 Range 5를 넘지 않도록 지속시간을 제한한다. Dynamic Rigidbody2D + Continuous 충돌로 이동한다. 실제 피해는 돌진 중 접촉으로 발생하며 기존 피격 무적을 공유한다. 벽에 막혀도 시간이 끝나면 회복 단계로 넘어간다.
- **Projectile**: 방향을 예고한 뒤 기존 Projectile 프리팹 한 발을 발사한다. 속도 6, 사거리 5 또는 수명 3초에 제거한다. 발사 보스와 다른 적/투사체를 무시하고 벽 또는 플레이어에 부딪히면 제거한다. 무적 중 부딪혀도 투사체는 제거한다.
- **Area**: 플레이어 위치를 사거리 안으로 제한해 빨간 원을 예고한다. 반경 1.5 안에 남아 있을 때 한 번 피해를 준다. 예고 중 이동하면 피할 수 있고 벽이 피해 경로를 차단한다. 지속 피해는 없다.

## 스테이지별 연결 / 수동 설정

현재 연결은 완료했으므로 필수 수동 연결은 없다. Unity 임포트를 기다리고 Play를 다시 시작한다.

1. `DungeonStages.asset`의 공통 목록에서 원하는 Stage 에셋을 연다.
2. **Boss**에 원하는 보스 데이터를 지정한다. None이면 기존처럼 일반 웨이브 완료 직후 클리어한다.
3. 보스 난이도는 **Boss Health Multiplier / Boss Damage Multiplier**로만 조절한다. 후자는 접촉 피해와 각 패턴 피해에 적용한다. HP와 피해는 반올림 후 최소 1이다.
4. **Boss Intro Duration**으로 웨이브 종료와 보스 생성 사이의 시간을 조절한다. 기본 1.5초다.
5. WaveSystem의 기존 EnemySpawner2D가 방 내부 유효 위치를 탐색한다. 보스의 확대된 폴리곤 크기를 포함해 벽/플레이어/상자와 겹치지 않는 위치를 찾고, 실패하면 생성 간격마다 제한된 횟수로 재시도한다.

강화형을 같은 최초 처치 대상으로 취급하려면 현재처럼 같은 보스 에셋을 연결하고 스테이지 배율만 올린다. 별도 최초 보상을 주려면 보스 에셋을 복제하고 새 Boss ID를 지정해 해당 스테이지에 연결한다. 프리팹과 패턴 코드는 그대로 재사용한다.

보스는 WaveManager가 생성하고 초기화하므로 씬에 직접 드래그해 추가하지 않는다. HUD와 로비 마석 표시도 기존처럼 코드로 생성하여 Canvas/OnClick 수동 연결은 필요 없다.

## 마석 보상과 최초 기록

- 재화 ID: `magic_stone`, 표시 이름: Magic Stones. 기존 Export Tokens 및 임시 Gold와 별도다.
- 처치 시 던전의 RewardRecord에 보스 ID, 최초/반복 수량과 임시 표시 수량을 기록한다. 이벤트 키는 현재 스테이지 ID를 사용하며 같은 스테이지의 사망 이벤트는 한 번만 등록한다.
- 임시 최초 판정은 저장된 defeatedBossIds와 이번 던전의 보스 보상 목록을 함께 본다. 같은 보스를 Stage 1과 Stage 4에서 처치하면 임시 수량은 5 + 2다.
- 최종 저장 때는 최신 영구 프로필을 기준으로 최초 여부를 다시 판단한다. 마석 증가, defeatedBossIds 갱신, settledRunIds 기록을 하나의 저장 파일 교체로 함께 확정한다.
- 던전 사망, 중간 포기 복귀, 재시작은 미정산 보상을 폐기한다. 보스만 처치하고 나가면 최초 보상을 받은 것으로 기록되지 않는다.
- 저장 실패 시 이전 프로필과 최초 기록은 그대로다. 기존 Retry Save로 다시 시도하고, 성공하기 전 최종 로비 복귀를 막는 동작도 유지한다. 같은 runId는 재시도/중복 버튼으로 재지급되지 않는다.
- 최초 미처치 상태에서 Stage 1~6 전체 클리어 시 마석은 `(5 + 2) × 3 = 21`이다. 이후 동일한 전체 플레이는 `2 × 6 = 12`다. Stage 6만 선택하면 Water Boss 한 번에 대한 5 또는 2만 지급한다.
- 보스 처치도 결과의 Kills에 포함하므로 기본 전체 플레이는 일반 적 90 + 보스 6 = 96이다. 보스에게 별도 일반 골드는 지급하지 않는다. 기존 임시 골드는 일반 처치 450 + 스테이지 클리어 135 = 585로 유지한다.
- 보스는 기존 EnemyChase2D 피해 메서드를 사용하므로 근접 타격/근접 처치 마나 보너스가 유지된다. 마법 처치로는 마나가 회복되지 않는다. 보스는 이번 단계에서 일반 무기를 드롭하지 않는다.

## 저장 구조와 호환

기존 `Application.persistentDataPath/prototype-rewards-v1.json`의 SHA256 envelope / version 1을 유지하고 payload에 다음 선택 필드를 추가했다.

- `magicStones`: 영구 보유 마석(long).
- `lastMagicStones`: 가장 최근에 정산된 클리어의 획득량(long). 사망이나 중간 포기는 이 값을 갱신하지 않는다.
- `defeatedBossIds`: 최초 보상을 정산한 보스 ID 목록.

필드가 없는 예전 저장은 마석 0, 빈 보스 기록으로 보완한다. 읽기만으로 원본을 덮어쓰지 않는다. 기존 재화·무기 개체 수치·보유 마법·장착 마법·정산 ID를 보존한다. 기존 임시 파일 검증, 원자적 교체, `.bak`, 손상 시 쓰기 중지 정책을 유지한다. 실제 경로는 OS와 Unity Company/Product 설정에 따라 달라지며 기본 프로젝트 설정에서는 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\dduk`다.

## Unity Play 모드 테스트

1. 로비에서 Stage 1을 선택해 입장한다. 기존 3/5/7 웨이브가 끝나도 즉시 Stage Clear가 나오지 않고 보스 등장 안내 뒤 Flame Boss가 한 마리만 생성되는지 확인한다.
2. 보스 체력 20과 체력바를 확인하고 좌클릭/우클릭으로 줄여 본다. 피격 흰색 표시, 근접 마나 회복, 기존 충전 원도 확인한다.
3. 돌진 예고 선을 보고 피하거나 벽 쪽으로 유도한다. 벽 통과 여부, 돌진 후 회복 대기, 접촉 무적을 확인한다. 투사체 예고/발사/벽 충돌/수명과 플레이어 피해도 확인한다.
4. 보스를 남겨 둔 동안 Next Stage나 Stage Clear가 나오지 않아야 한다. 보스 처치 시 체력바와 패턴/남은 적대 투사체가 사라지고 Stage Clear에 마석 임시 수량이 표시되어야 한다.
5. 중간 클리어에서 Next Stage로 이동해 HP/마나/무기/골드가 유지되는지 확인한다. Stage 2는 Grass Boss의 원형 위험 지역에서 벗어나면 범위 피해가 발생하지 않아야 한다.
6. 로비에서 Stage 3을 선택해 Water Boss와 돌진/범위 패턴을 확인한다. 전투 중 사망 후 Restart하면 Stage 3부터 다시 시작하고 이전 보스/투사체/예고가 없어야 한다. 로비 복귀 후에도 선택은 유지되어야 한다.
7. Stage 4/5/6을 선택해 HP 30/38/45와 일반 배율 중복 미적용을 확인한다.
8. 마석 보상을 빠르게 확인하려면 Stage 6을 선택해 끝까지 클리어한다. 최초 미정산 Water Boss라면 마석 +5와 Water ID가 함께 저장되어야 한다. 로비에서 보유량과 최근 획득량을 확인하고 다시 같은 경로를 클리어하면 +2여야 한다. 이미 기존 플레이에서 최초 보상을 정산했다면 처음부터 +2가 정상이다.
9. 게임을 종료하고 재실행해 보유 마석과 반복 처치 판정, 기존 보상 무기·마법 장착 상태가 복원되는지 확인한다. 사용자 저장을 지워서 테스트할 필요는 없다.
10. 중간 스테이지 보스만 처치하고 `Leave (Lose Rewards)`로 나가거나, 다음 스테이지에서 사망하는 별도 플레이를 한다. 임시 마석이 지급되지 않는지 확인한다. 아직 최초 보상을 정산하지 않은 보스라면 재도전의 임시 표시가 계속 5여야 한다.
11. 마지막 타격에 여러 투사체를 맞추거나 버튼을 연속 클릭해 처치/정산/보스 생성이 중복되지 않는지 확인한다.
12. 필요하면 Stage 데이터의 Boss를 잠시 None으로 바꾸고 일반 웨이브만으로 기존 클리어가 가능한지 확인한 뒤 복구한다. 테스트 시간을 줄일 때는 웨이브 마릿수를 잠시 낮추고 원래 3/5/7로 복구한다.

## 검증 결과 / 남은 확인

- 설치된 Unity 참조와 컴파일러로 런타임 및 에디터 조건부 코드 컴파일 통과.
- 실제 진행 상태 코드 검사 5개 통과: 기존 선택/재시작, 중복 전환 방지, 보스 생존 중 완료 차단, 보스/플레이어 동시 사망 우선 등.
- 보상/마법/마석 저장 검사 18개 통과: 기존 저장 호환, 최초/반복 및 서로 다른 ID, 같은 던전의 동일 보스, 사망/재시작 폐기, 실패 후 재시도, 중복 정산, 실제 임시 파일 냉간 재로드 등.
- 보스/스테이지 에셋 GUID 및 실제 컴포넌트 fileID, 기존 패키지 Triangle 스프라이트 참조, 보스 ID 고유성, 로비/던전의 공통 카탈로그 연결을 확인했다.
- 저장 검사는 Unity API 대역과 임시 파일을 사용했다. 실제 사용자 영구 저장 파일은 수정하지 않았다.
- Unity Play 모드는 직접 실행하지 않았다. 실제 물리 돌진의 벽 충돌, 패턴 회피, UI 배치, 전체 플레이와 앱 재실행은 위 절차로 확인해야 한다. 검증된 런타임 오류가 없다고 단정하지 않는다.
- 마석 소비/테크트리/상성/영구 강화/새 무기/고급 연출은 구현하지 않았다. 패턴 선택은 데이터 순서대로 반복하며 새로운 복잡한 기믹은 없다.
