# 5차 프로토타입: 무기 드롭과 E키 즉시 교체

6차에서는 무기 개체별 랜덤 수치와 비교 UI가 추가되었다. 현재 개체/랜덤/몬스터별 테이블 설정은 PROTOTYPE_V6_SETUP.md를 참고한다.

## 기존 구조 확인과 유지 범위

확인 시 무기 데이터, 몽둥이 충전/충격파, 창끝 피해, 채찍 띠 판정, 장착 관리, 적의 일회성 사망 이벤트, 웨이브/골드, 마나/마법, 사망/클리어/재시작 코드가 있었다. 드롭 테이블/바닥 아이템/E키 획득은 없어서 새로 구현했다.

무기 판정은 최근 조정된 형태를 유지했다. 몽둥이는 자동 충전 및 풀 충전 충격파, 창은 창끝 보너스, 채찍은 거리 1.8~2.3의 띠다. 무기 수치나 기존 전투/마나/골드/웨이브 코드는 변경하지 않았다.

## 플레이 흐름

- 시작 무기는 Club 하나. 숫자키 1/2/3 자유 교체는 제거했다.
- 적 사망 시 40% 확률로 무기 하나를 생성한다.
- 드롭 성공 안에서 Club 40%, Spear 30%, Whip 30%로 선택한다.
- 갈색/회색/노란 원형 아이템이 적 사망 위치에 나타난다.
- 플레이어 중심에서 1.5 이내이고 벽에 가려지지 않은 아이템 중 가장 가까운 하나에 E 안내를 표시한다.
- E를 새로 누르면 장착 무기를 바꾸고 이전 무기를 플레이어 주변에 남긴다. 인벤토리는 없다.
- 이전 무기도 다음 E 입력으로 다시 획득할 수 있다. 한 번 눌렀을 때 다시 주워지는 일은 막았다.
- 같은 종류끼리 바꿔도 기존 무기 한 개를 바닥에 남기므로 아이템 수가 늘거나 줄지 않는다. 이 경우에도 장착 변경으로 처리해 충전은 초기화한다.

## Unity 연결 (반영 완료)

Play를 끈 상태에서 외부 씬 변경 알림에 Reload를 선택하거나 SampleScene을 다시 연다. 저장하지 않은 별도 편집은 먼저 다른 씬에 보존한다.

| 대상 | 컴포넌트 / 필드 | 연결 |
|---|---|---|
| Player | Player Weapon Loadout 2D → Club | 기존 Club.asset |
| Player | Player Weapon Pickup 2D → Pickup Prefab | Assets/Prefabs/WeaponPickup.prefab |
| Player | Pickup Range | 1.5 |
| Player | Room Bounds | X=-7.5, Y=-4.5, Width=15, Height=9 |
| Player | Drop Distance / Item Clearance | 0.8 / 0.25 |
| Player | Placement Attempts | 24 (코드 상한 128) |
| Player | Blocking Layers | Everything |
| Assets/Prefabs/Enemy.prefab | Enemy Weapon Drop 2D → Drop Table | DefaultWeaponDrops.asset |
| 동일 | Pickup Prefab | WeaponPickup.prefab |

Player의 획득 컴포넌트와 적 프리팹의 드롭 컴포넌트를 이미 연결했다. 기존 적/투사체 프리팹의 GUID와 공격·추적 참조는 유지했다. GameUI는 기존 Player Health 참조를 이용해 획득 컴포넌트를 자동으로 찾으므로 별도 UI 연결은 없다.

씬에 비활성으로 보존된 수동 Enemy는 이전과 마찬가지로 사용하지 않는다. 웨이브가 사용하는 Enemy 프리팹에 드롭을 붙였으며, 수동 Enemy에는 드롭을 추가하지 않았다.

## 드롭 확률과 가중치 조절

Project 창 → Assets/Settings/Weapons/DefaultWeaponDrops.asset을 선택한다.

- Drop Chance: 0.4가 40%, 0은 드롭 없음, 1은 항상 드롭이다.
- Entries: 무기 데이터와 Weight를 지정한다. 기본값은 Club 40 / Spear 30 / Whip 30.
- Weight는 양수들의 상대 비율이다. 4/3/3도 40/30/30과 같다. 무기 미지정 또는 0 이하 가중치는 제외한다. 유효한 항목이 없으면 드롭하지 않는다.
- 따라서 적 한 마리 기준 전체 확률은 Club 16%, Spear 12%, Whip 12%, 드롭 없음 60%다. 소수의 처치에서 반드시 비율대로 나오지는 않는다.

몬스터 종류별로 다르게 하려면 이 테이블을 복제해 수치를 바꾸고, 해당 몬스터 프리팹의 Enemy Weapon Drop 2D → Drop Table에 연결한다. 드롭 코드는 공통으로 쓴다.

## 바닥 아이템 프리팹

Assets/Prefabs/WeaponPickup.prefab은 기존 프로젝트의 원형 스프라이트/머티리얼을 재사용한다. 구성은 Transform, SpriteRenderer, WeaponPickup2D다.

- Scale=(0.35, 0.35, 1), Sorting Order=0. 바닥(-1) 위에 표시한다.
- Collider2D와 Rigidbody2D가 없어서 플레이어/적/투사체와 물리 접촉하지 않는다. 공격 후보/적 생성 장애물에도 포함되지 않는다.
- Weapon 필드는 기본 비워 둔다. 드롭/교체 시 생성 코드가 무기 데이터를 넣는다.
- 색은 각 무기 데이터의 Pickup Color다: Club 갈색, Spear 회색, Whip 노랑.
- 직접 배치해서 테스트하려면 프리팹을 씬에 끌어 놓고 Weapon에 원하는 무기 데이터 에셋을 연결한다. Play 시작 시 해당 색을 적용한다. 테스트 후 임시 배치를 제거한다.

## 안전한 교체 처리

1. 가장 가까운 아이템이 획득 가능한 상태인지 다시 확인하고 예약한다.
2. 플레이어 주변에 기존 무기를 놓을 위치를 최대 24곳 검사한다. 방 경계/벽/장애물과 겹치거나 벽 건너편이면 제외한다.
3. 유효 위치에 이전 무기를 생성한다. 생성 실패나 장착 불가이면 생성물을 정리하고 원래 아이템 예약을 해제한다.
4. 새 무기 장착에 성공하면 대상 아이템을 즉시 비활성화하고 제거한다.
5. 같은 프레임의 추가 획득 호출은 거절한다. E는 wasPressedThisFrame으로 읽어서 누르고 있는 동안 반복하지 않는다.

유효 위치가 없으면 장착과 아이템을 그대로 두고 No room to drop equipped weapon을 잠깐 표시한다. 다른 위치로 이동 후 E를 다시 누르면 된다. 바닥 아이템은 물리적으로 서로 막지 않으므로, 같은 위치에 여러 아이템이 겹쳐 있을 수 있지만 가장 가까운 하나씩 교체된다.

Room Bounds는 기존 방의 벽 안쪽 월드 좌표다. 맵을 바꾸면 스포너와 함께 이 값도 맞춘다. 바닥에는 Collider를 붙이지 않는다. Blocking Layers에는 벽/장애물을 반드시 포함한다. 플레이어 자신/적/투사체/Trigger는 획득 경로와 기존 무기 배치 검사에서 제외한다.

## 공격·자원·게임 상태 연동

- 교체는 장착 데이터/장착 버전/장착 시각만 갱신한다. 현재 HP/마나, 근접 공격의 nextAttackTime, 마법 타이머를 건드리지 않는다.
- 다른 무기를 주워도 직전에 공격한 무기의 남은 공격 대기 시간은 유지된다.
- 몽둥이 충전은 새 무기를 획득할 때 초기화한다. 바닥의 Club을 새로 주워도 충전은 이전 장착에서 이어받지 않는다.
- 기존 적의 Died 이벤트에 드롭 컴포넌트가 구독한다. 적의 기존 일회성 사망 보호와 드롭 컴포넌트의 handled 보호를 모두 적용한다. 외부 Destroy나 비활성화는 드롭으로 인정하지 않는다.
- 근접/마법/충격파 처치 모두 드롭 판정 대상이다. 각 공격의 기존 마나 규칙과 처치당 골드 5는 변경하지 않았다.
- 사망/StageClear 후에는 획득/장착을 거부하고 안내를 숨긴다. GameOver 뒤 이미 발사된 투사체가 적을 처치하면 드롭 자체는 생길 수 있지만 획득은 할 수 없다.
- 아이템은 현재 씬의 일반 오브젝트이며 영구 저장/DontDestroyOnLoad가 없다. Restart 시 모두 제거되고 시작 Club/체력/마나/웨이브/골드로 초기화된다.

## Unity Play 테스트

1. 시작 시 Equipped: Club인지 확인한다. 1/2/3을 눌러도 바뀌지 않아야 한다.
2. 테스트를 쉽게 하려면 Drop Chance를 임시 1로 설정한다. 적을 처치할 때 하나씩 나타나는지, 근접/마법/충격파 모두에서 나오는지 확인한다.
3. Drop Chance를 0으로 설정해 새 적 처치에서 아무것도 생성되지 않는지 확인한다. 테스트 뒤 0.4로 되돌린다.
4. Entries의 특정 무기만 양수로 두면 해당 무기만 나오는지 확인한다. 테스트 뒤 40/30/30으로 되돌린다.
5. 아이템 1.5 이내에서 E: Pick up 이름이 나오고, 여러 개가 있으면 가장 가까운 하나만 표시되는지 확인한다. 벽 너머 아이템은 획득하지 못해야 한다.
6. E를 한 번 누르면 새 무기가 장착되고 아이템 하나가 제거되며 이전 무기 하나가 주변에 남는지 확인한다. 누르고 있어도 계속 바뀌면 안 된다.
7. E를 떼고 다시 눌러 이전 무기를 다시 주울 수 있는지 확인한다. 같은 종류끼리 교체도 바닥 아이템 수가 변하지 않는지 확인한다.
8. 벽 모서리/좁은 장애물 틈에서 유효 배치가 없으면 교체를 취소하고 양쪽 무기를 보존하는지 확인한다. 원활한 검증을 위해 Player의 Room Bounds를 임시로 너무 작게 설정하면 취소 경로를 강제할 수 있다. 이후 원래대로 돌린다.
9. 교체 전후 HP/마나가 같고, 마법 직후 또는 채찍 공격 직후 교체해도 해당 쿨타임이 우회되지 않는지 확인한다.
10. Club 충전을 쌓은 상태에서 아이템을 획득하면 충전이 초기화되는지 확인한다. 창끝 보너스/채찍 띠/몽둥이 충격파는 유지되어야 한다.
11. 아이템 위로 플레이어/적/투사체가 지나가도 충돌하거나 아이템을 공격하지 않는지 확인한다.
12. 사망/클리어 이후 E로 교체할 수 없어야 한다. Restart 후 모든 바닥 아이템이 사라지고 Club, HP 10, Mana 100, Wave 1, Gold 0으로 돌아오는지 확인한다.
13. 여러 번 재시작해 오래된 아이템 안내/중복 드롭/중복 UI가 남지 않는지, Console에 오류가 없는지 확인한다.

## 생성한 파일

- Assets/Scripts/Items/WeaponDropTable2D.cs — 몬스터별 드롭 확률/가중치 데이터.
- Assets/Scripts/Items/EnemyWeaponDrop2D.cs — 적 사망 구독과 일회성 드롭.
- Assets/Scripts/Items/WeaponPickup2D.cs — 바닥 무기/색/활성 목록/예약·제거.
- Assets/Scripts/Player/PlayerWeaponPickup2D.cs — 근접 탐색/E 입력/안전한 즉시 교체.
- Assets/Settings/Weapons/DefaultWeaponDrops.asset — 기본 40%, 가중치 40/30/30.
- Assets/Prefabs/WeaponPickup.prefab — 비물리 바닥 아이템.
- 위 스크립트/에셋 및 Items 폴더 .meta.
- PROTOTYPE_V5_SETUP.md — 이 안내.

## 수정한 파일

- PlayerWeaponLoadout2D.cs — 숫자키/해금 슬롯 제거, 시작 Club/단일 장착과 데이터 기반 교체 유지.
- WeaponPartData2D.cs — Pickup Color 추가.
- GameHUD2D.cs — Equipped와 E 안내 추가, 충전 표시 유지.
- Club.asset / Spear.asset / Whip.asset — 바닥 표시 색만 추가.
- Assets/Prefabs/Enemy.prefab — 드롭 컴포넌트와 테이블/아이템 프리팹 연결.
- Assets/Scenes/SampleScene.unity — Player 획득 컴포넌트 연결, 자유 교체용 참조 제거.
- Tests/Standalone/WeaponPartTests.cs — 새 장착 API/같은 종류 획득/드롭 테이블 경계 테스트 반영.

## 검증 결과와 한계

- 전체 런타임 스크립트를 설치된 Unity 참조 어셈블리/번들 Roslyn으로 컴파일: 오류 0.
- 가짜 Unity 의존성을 사용한 실제 무기/장착/드롭 테이블 로직 테스트 16개 통과. 0%/100%, 확률 경계, 상대 가중치, 무효 항목, 동일 종류 재장착의 충전 시각 갱신 포함.
- 기존 웨이브/골드 로직 테스트 8개 재실행 통과.
- 위 테스트는 실제 Unity 물리/아이템 생성·예약·교체 트랜잭션/E 입력/표시/SceneManager 재시작을 실행한 테스트가 아니다. 특히 아이템 소실·중복 방지와 유효 바닥 배치는 위 Play 절차로 확인해야 한다.
- Unity 에디터 임포트/컴파일 및 Play 모드 실행은 직접 확인하지 않았다.
- 장비 장착 API를 TryEquipSlot에서 TryEquipWeapon으로 바꿨고 프로젝트 내 호출부/독립 테스트를 함께 갱신했다. 숫자키 사용을 설명하는 과거 단계 문서는 현재 조작을 나타내지 않는다.
- 인벤토리/복수 장착/반출/강화/등급/상점/조합 시스템은 추가하지 않았다.
