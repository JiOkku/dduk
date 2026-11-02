# 8차 프로토타입: 보상 경로·반출·로컬 저장

9차에서 보유/장착 마법 ID가 같은 저장 파일에 추가되었다. 현재 스크롤/시전/구형 저장 호환 설명은 PROTOTYPE_V9_SETUP.md를 참고한다.

## 기존 구조와 유지한 기능

기존 로비/던전 전환, 결과 확정, 무기 개체, 몬스터 드롭, E 획득, 웨이브/골드 및 무기 특성을 확인했다. 상자/보상 경로/영구 저장은 없었으므로 필수 범위로 추가했다.

기존 몽둥이 충전/충격파, 창끝 피해, 채찍 띠, 마나/마법/드롭 확률/일반 무기 교체/웨이브는 유지했다. 새로운 AI, 반출 무기 장착, 일반 인벤토리는 구현하지 않았다.

## 보상 데이터

- RewardRecord2D: rewardId, name, kind, quantity, source, exportable, 선택적 무기 스냅샷.
- RewardKind2D: Currency / Weapon / TemporaryGold.
- RewardSource2D: StartingEquipment / Monster / Chest / Boss.
- WeaponSaveData2D: 원래 instanceId, 안정적인 definitionId, 표시 이름, 네 실제 능력치, 특수 공격용 배율, 최초 source/exportable.
- RewardProfile2D: 보유 반출 재화, 반출 무기 목록, 이미 정산한 던전 ID 목록, 스키마 버전.

무기 원본에는 저장용 weaponId(club/spear/whip)를 추가했다. 무기의 Source는 생성 시 결정되며 장착/버리기/재획득에서 수정하지 않는다. 실제 개체의 ID/랜덤 수치는 스냅샷에 보존하며 복원 API도 같은 ID/수치를 사용한다. 공유 ScriptableObject를 런타임에 수정하지 않는다.

반출은 exportable 플래그만 믿지 않고 RewardRules2D에서 **종류와 획득 경로를 다시 검사**한다. Monster 무기에 exportable=true를 임의로 붙여도 반출하지 않는다. StartingEquipment도 반출하지 않는다. Chest/Boss의 Currency/Weapon만 대상이고 TemporaryGold는 경로와 무관하게 제외한다.

## 상자와 Inspector

SampleScene의 (-4, 2, 0)에 TreasureChest.prefab 인스턴스 1개를 배치했다. 갈색 사각형이며 열면 회색으로 바뀐다. SpriteRenderer와 기존 방식의 단단한 BoxCollider2D를 사용한다. 몬스터는 이 상자 안에 생성되지 않고, 벽처럼 근접 시야를 가릴 수 있다.

Assets/Prefabs/TreasureChest.prefab → Treasure Chest 2D:

| 항목 | 기본값 |
|---|---|
| Interaction Range | 1.5 |
| Currency Amount | 10 |
| Weapon Count | 1 (0~10) |
| Weapon | 기존 Spear.asset |
| Roll Settings | DefaultWeaponRolls.asset |
| Opened | 꺼짐 |

상자 무기는 기존 무기 데이터/랜덤 설정으로 한 번 생성하고 Chest 경로로 보상 기록에 등록한다. 자동 장착하거나 바닥에 놓지 않는다. 현재 장착 무기/마나/체력은 바뀌지 않는다. 위 수치는 프리팹 Inspector에서 바꿀 수 있다.

E 입력은 기존 PlayerWeaponPickup2D 하나가 담당한다. 가까운 미개봉 상자가 우선이고, 해당 입력에서는 무기 획득을 함께 실행하지 않는다. 상자가 없거나 이미 열렸으면 기존 무기 획득을 처리한다. 상자도 벽에 가려져 있으면 열 수 없다.

같은 상자는 한 번만 보상을 등록한다. 사망 시 상자 외형/열림 상태를 초기화하지만 사망 상태라 다시 상호작용할 수 없다. 재시작/재입장은 새 씬 인스턴스라 상자와 던전 보상 기록이 함께 초기화된다.

## 던전 보상 기록과 정산

SampleScene의 GameUI에 DungeonRewards2D를 추가하고 Player Health / Loadout / RewardSettlementPolicy를 연결했다. DungeonSession2D와 PlayerWeaponPickup2D도 같은 보상 기록을 참조한다.

- 상자: Export Tokens 10과 무기 1개를 별도 보관.
- 일반 몬스터 무기: E로 실제 획득한 개체를 임시 무기로 기록. 같은 ID 재획득은 기록을 중복하지 않는다. 바닥에 버린 무기도 반출로 바뀌지 않는다.
- 던전 골드: 기존 EarnedGold를 종료 시 임시 보상으로 기록하며 반출 재화로 변환하지 않는다.
- 클리어: 반출 대상만 영구 프로필에 지급/저장한다.
- 사망: 기본 규칙에서는 반출 보상을 모두 소실 처리하고 저장하지 않는다.
- 미정산 상태에서 재시작: 해당 던전의 보상 기록을 폐기한다.
- 이미 클리어로 저장 완료한 보상은 이후 재시작해도 취소되지 않는다. 새 던전은 새 ID를 사용한다.

Assets/Settings/RewardSettlementPolicy.asset의 Export On Clear=true / Export On Death=false로 종료 규칙을 분리했다. 향후 사망 반출 정책은 이 에셋에서 변경할 수 있다. 이 정책이 Monster/TemporaryGold를 반출 대상으로 바꾸지는 않는다.

결과 확정은 DungeonSession에서 한 번, 정산은 DungeonRewards의 Finished 가드로 한 번 처리한다. 성공한 던전 ID를 저장 프로필에도 함께 기록하므로 저장 후 같은 ID가 다시 들어와도 지급하지 않는다. 무기 ID 중복도 거부한다.

## 개발용 보스 보상

실제 보스/AI는 만들지 않았다.

Play 중 던전 GameUI → Dungeon Rewards 2D 컴포넌트의 컨텍스트 메뉴 → **Development/Grant Boss Reward Once**를 선택하면 Boss 경로의 Export Tokens 20을 등록한다. 같은 던전에서는 한 번만 지급하고 종료 후에는 사용할 수 없다. Development Boss Currency는 테스트 수량이다.

메서드/필드는 UNITY_EDITOR 또는 DEVELOPMENT_BUILD에서만 컴파일한다. 일반 출시 빌드에는 개발용 지급 메서드가 없다. 개발 빌드에도 일반 플레이용 버튼/키는 추가하지 않았다.

## 결과 UI와 저장 실패

기존 결과 통계/버튼을 유지하고 다음을 추가했다.

- Exported: 반출에 성공한 재화/무기 수.
- Lost export rewards: 사망 등으로 소실한 반출 대상 보상.
- Discarded temporary: 던전 골드/획득한 일반 몬스터 무기 수.
- Reward weapons: 반출 대상 무기 이름과 경로(최대 3개 이름, 나머지는 수량).

저장 실패 시 NOT SAVED와 Retry Save를 표시한다. 이때 보상을 성공으로 표시하지 않고, Return to Lobby를 비활성화해 미저장 보상이 조용히 사라지는 것을 막는다. 실패 원인을 해결한 뒤 Retry Save를 누르면 같은 던전 ID로 재시도한다. Restart Dungeon은 가능하지만 **미저장 보상을 폐기한다**고 화면에 안내한다.

## 로컬 저장 경로와 형식

실제 경로는 RewardBank2D.SavePath = Application.persistentDataPath/prototype-rewards-v1.json이다.

현재 Windows 프로젝트 설정(DefaultCompany / dduk) 기준 예상 경로:

`%USERPROFILE%\AppData\LocalLow\DefaultCompany\dduk\prototype-rewards-v1.json`

프로젝트의 회사/제품 이름이나 실행 플랫폼을 바꾸면 Unity의 영속 경로도 달라질 수 있다. 저장용 외부 플러그인/PlayerPrefs는 사용하지 않는다.

형식은 버전 1 JSON envelope다. envelope에는 payload JSON과 SHA-256을 저장한다. payload에는 누적 재화(long), 무기 DTO 목록, 정산한 던전 ID 목록이 들어 있다. 무기 데이터는 Unity 객체 참조나 에셋 instanceID가 아니라 definitionId와 개체 수치/원래 ID/경로로 저장한다.

저장 순서:

1. 기존 정상 파일을 읽고 중복 던전 ID/무기 ID와 출처 규칙을 검사.
2. 원본 메모리와 분리한 후보 프로필을 작성.
3. 같은 디렉터리의 고유 .tmp 파일에 쓰고 Flush(true).
4. 임시 파일을 다시 읽어 스키마/무기 수치/체크섬 검증.
5. 기존 파일이 있으면 File.Replace로 교체하고 이전 파일을 .bak으로 보존. 첫 저장은 File.Move.
6. 파일 저장 성공 후에만 메모리의 영구 프로필을 갱신.

실패 시 원본 메모리/기존 저장에 보상을 지급하지 않고, 작성 중이던 전용 임시 파일만 정리한다. 실패 후 빈 데이터로 기존 파일을 덮어쓰거나 원본을 삭제하지 않는다.

시작 시 원본이 손상되면 .bak을 읽어 표시할 수 있지만 **쓰기 금지 상태**로 둔다. 손상 원본과 백업을 자동으로 덮어쓰지 않는다. 복구가 필요하면 게임을 종료하고 두 파일을 먼저 따로 보존한 뒤 정상 백업을 원본으로 복원해야 한다. 저장 오류의 전체 내용은 Console에서도 확인한다.

파일 무결성 체크섬은 손상 검출용이며 암호화/부정 조작 방지 시스템은 아니다. 던전 진행 자체나 미정산 보상은 저장하지 않으므로 플레이 도중 종료하면 해당 던전의 보상은 남지 않는다.

## 로비 보상 UI

Lobby GameUI에 LobbyRewardsUI2D를 연결했다. 별도 Text/Canvas/데이터 연결은 필요 없다.

- 보유 Export Tokens.
- 반출 무기 전체 목록(스크롤).
- 이름/획득 경로/피해/공격 간격/거리/타격 마나/원래 개체 ID.
- 저장 오류 상태.

로비 입장 때 프로필을 다시 표시하고, 게임 최초 실행 때는 디스크에서 불러온다. 보상 UI는 로비 입장 확인창보다 뒤에 그려져 취소/입장 버튼을 가리지 않는다. 반출 무기 장착/사용/판매/강화 기능은 없다. 새 던전은 계속 기본 Club으로 시작한다.

## Unity에서 수동 확인할 핵심 테스트

1. **개봉**: 로비에서 던전 입장 후 왼쪽 위 (-4,2)의 갈색 상자에 접근해 E. 보상 안내, 회색 열린 상자, 현재 장비 유지 확인.
2. **중복/우선순위**: E를 반복해도 상자 보상이 늘지 않는지 확인. 바닥 무기가 가까이 있어도 같은 E로 상자와 무기를 동시에 획득하면 안 된다.
3. **일반 무기 제한**: 몬스터 무기를 여러 번 주워/버려도 Instance의 Source=Monster, Exportable=false가 유지되고 결과의 임시 무기 집계가 중복되지 않아야 한다.
4. **클리어 반출**: 상자를 열고 3웨이브 클리어. 기본 테스트 보상은 Tokens 10 / Weapon 1을 반출하고, 던전 골드/몬스터 무기는 폐기해야 한다.
5. **사망 소실**: 상자를 열고 사망. Lost export rewards에 10/1이 표시되고 기존 영구 잔액/무기 목록은 늘지 않아야 한다. 상자는 닫힌 상태로 돌아가지만 사망 후 E로 열 수 없어야 한다.
6. **로비 확인**: 클리어 후 Return to Lobby. 재화/무기 주요 수치/ID/경로가 표시되고, 시작 장비 UI는 여전히 Club인지 확인.
7. **재입장/재시작**: 새 상자·새 던전 기록·새 몬스터로 시작하고 이전 보상이 다시 지급되지 않아야 한다. 미정산 상태에서 재시작한 보상은 폐기되어야 한다.
8. **게임 재실행**: 저장 성공 후 Play 종료/재실행 또는 게임 종료/재실행. 영구 잔액과 무기 ID/수치가 유지되어야 한다.
9. **중복 정산**: 결과 버튼/재시도 버튼 반복 입력과 씬 왕복으로 같은 던전 보상이 두 번 지급되지 않아야 한다.
10. **보스 테스트**: 개발 컨텍스트 메뉴를 두 번 사용한 뒤 클리어해도 추가 재화는 20만 지급되어야 한다. 사망이면 기본 정책에 따라 소실한다.
11. **저장 오류**: 실제 저장을 건드리기 전 반드시 원본/백업을 별도 복사한다. 파일 잠금 등으로 저장 실패 시 NOT SAVED/Retry Save, 기존 잔액 유지, 성공으로 표시하지 않음을 확인한다. 강제 손상 검증은 실제 사용자 저장 대신 테스트 사본에서 하는 것이 안전하다.

## 생성 파일

- Assets/Scripts/Rewards/RewardData2D.cs — 종류/경로/규칙/보상·무기·프로필 DTO.
- RewardSettlementPolicy2D.cs — 종료 정책 에셋 형식.
- RewardSaveStore2D.cs — JSON 검증/원자 교체/백업/중복 정산.
- RewardBank2D.cs — 씬과 무관한 순수 영구 프로필 서비스.
- DungeonRewards2D.cs — 던전 보상 기록/정산/임시 무기 집계/개발 보스 지급.
- TreasureChest2D.cs — 상자 거리/벽/일회 개봉/보상 생성/사망 초기화.
- LobbyRewardsUI2D.cs — 로비 잔액/무기 스크롤 표시.
- 위 스크립트와 Rewards 폴더 .meta.
- Assets/Settings/RewardSettlementPolicy.asset 및 .meta.
- Assets/Prefabs/TreasureChest.prefab 및 .meta.
- Tests/Standalone/RewardPersistenceTests.cs.
- PROTOTYPE_V8_SETUP.md.

## 수정 파일

- WeaponPartData2D.cs / Club.asset / Spear.asset / Whip.asset — 안정적인 무기 정의 ID.
- WeaponInstance2D.cs — 불변 획득 경로/반출 규칙과 수치 보존 DTO/복원.
- WeaponRollSettings2D.cs / WeaponDropTable2D.cs — 생성 경로 전달. 몬스터 드롭은 항상 Monster.
- PlayerWeaponLoadout2D.cs — 장착 성공 이벤트(기존 장착 데이터/쿨타임 규칙 유지).
- PlayerWeaponPickup2D.cs — 상자 우선 E 입력 및 같은 프레임 중복 처리 차단.
- DungeonSession2D.cs — 기존 결과 확정 후 보상 정산 호출.
- GameHUD2D.cs — 보상 안내/성공·소실·미저장/재시도 UI.
- SampleScene.unity — 상자 프리팹 인스턴스와 보상 기록 참조.
- Lobby.unity — 로비 보상 UI 컴포넌트.
- Tests/Standalone/WeaponPartTests.cs / SceneFlowTests.cs — 새 데이터와 정산 연결에 맞춘 테스트 의존성.
- PROTOTYPE_V7_SETUP.md — 현재 단계 안내 링크.

## 검증 결과와 한계

- 런타임/에디터 C# 코드를 설치된 Unity 참조 어셈블리로 별도 컴파일 통과.
- 릴리스 정의로 컴파일한 DLL에 개발용 GrantBossReward 메서드가 없는 것을 확인.
- 보상/저장 핵심 테스트 9개 통과: 몬스터 출처 보존/반출 거부, 수치·ID 저장 복원, 재로드 후 중복 정산 방지, 실제 임시 파일 잠금 실패/재시도, 손상 원본/백업 보존, 체크섬, 사망 소실, 클리어 일회 정산, 보스 한 번 지급.
- 이 테스트는 실제 RewardSaveStore 코드와 임시 디렉터리의 파일 I/O를 사용하지만, Unity JSON API는 .NET JSON 테스트 대역으로 대체했다. 실제 Unity JsonUtility 포맷/상자/물리/E 입력/UI/씬 전환 테스트가 아니다.
- 무기/개체/드롭 로직 22개 및 씬 흐름/결과 6개도 통과했다. 결과 확정에서 정산 호출이 한 번인 것을 포함한다.
- 실제 Application.persistentDataPath에는 도구로 테스트 데이터를 쓰지 않았다. 임시 테스트 파일은 OS 임시 디렉터리만 사용했다.
- Unity 에디터 임포트/실행/콜라이더/프리팹 인스턴스/E키/로비 스크롤/실제 게임 재실행 저장 복원은 직접 확인하지 않았다.
- 별도 게임 프로세스들이 동시에 같은 저장을 쓰는 다중 실행 동기화나 저장 마이그레이션 UI는 이번 단일 프로토타입 범위에 포함하지 않았다. 스키마/체크섬이 다른 파일은 덮어쓰지 않고 오류로 보존한다.
- 실제 보스 AI/인벤토리/영구 장비 장착/상점/보상 사용은 미구현이며 이번 제외 범위다.
