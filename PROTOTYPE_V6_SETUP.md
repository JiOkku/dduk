# 6차 프로토타입: 무기 개체 능력치와 비교

7차에서 로비 입출입과 던전 결과 화면이 추가되었다. 현재 시작/복귀/재시작 설정은 PROTOTYPE_V7_SETUP.md를 참고한다.

## 기존 기능 확인과 구현 범위

기존 무기 ScriptableObject, 단일 장착/E키 교체, 적의 일회성 사망 이벤트, 상대 가중치 드롭 테이블, 바닥 아이템과 웨이브/자원/재시작 코드를 확인했다. 몬스터 프리팹별 테이블 지정은 이미 가능했으므로 재사용했다.

확인 시 무기 개체별 능력치, 랜덤 설정 에셋, 근접형/장거리형 테스트 테이블과 비교 UI는 없었다. 이를 이번에 추가했다. 새로운 몬스터 AI는 구현하지 않았다.

## 기본 데이터와 개체 데이터

- WeaponPartData2D: 기존 Club/Spear/Whip 에셋. 무기 이름/형태/기본 수치/특수 공격 정보를 유지한다.
- WeaponInstance2D: 고유 Instance Id, Base Data 참조, 확정된 Damage / Attack Interval / Range / Mana On Hit, 특수 공격용 공격력·거리 배율을 가진 직렬화 가능한 일반 C# 객체다.
- WeaponRollSettings2D: 랜덤 배율의 최솟값/최댓값을 관리하는 별도 ScriptableObject다.

드롭 성공 후 기본 무기를 정하고 개체를 한 번 생성한다. 장착/바닥 생성/재획득은 **같은 개체 객체와 ID를 전달**하며 재추첨하거나 ScriptableObject를 수정하지 않는다. 교체 실패 시 기존 개체와 바닥 개체를 그대로 보존하는 기존 취소 처리를 유지한다.

Player → Player Weapon Loadout 2D → Current Weapon에서 실행 중 개체 ID/실제 수치를 볼 수 있다. 바닥 무기 → Weapon Pickup 2D → Instance에서도 같은 정보를 볼 수 있다. 이 런타임 필드는 확인용이며 밸런스 변경은 기본 무기/랜덤 설정 에셋에서 한다.

시작/Restart는 기본 Club 에셋의 수치 그대로 새 개체를 만든다. 시작 무기를 랜덤 추첨하지 않는다. ID는 새로 생성되고 이전 플레이의 드롭 개체는 유지하지 않는다.

## 랜덤 범위 설정

Project → Assets/Settings/Weapons/DefaultWeaponRolls.asset:

| 필드 | X 최솟값 / Y 최댓값 |
|---|---|
| Damage Multipliers | 0.8 / 1.2 |
| Interval Multipliers | 0.85 / 1.15 |
| Range Multipliers | 0.9 / 1.1 |
| Mana Multipliers | 0.8 / 1.2 |

0.8은 기본값의 80%다. 네 값은 서로 독립적으로 균등 추첨한다. 역순으로 입력하면 작은 값/큰 값 순서로 해석한다. 공격력과 마나 회복량은 Mathf.RoundToInt로 반올림하며 공격력 최소 1, 마나 회복 최소 0, 공격 간격/거리 최소 0.01을 적용한다.

현재 세 무기의 기본 일반 공격력이 1이므로 0.8~1.2를 반올림하면 일반 공격력은 모두 1이다. 이는 지정 범위와 정수 시스템의 결과다. 차이를 검증하려면 테스트 중 기본 피해를 5 등으로 조절하거나 배율 범위를 넓힐 수 있다. 원래 값을 의도 없이 바꾸지는 않았다.

기존 무기 특성을 유지하기 위해:

- 공격 각도와 직선 공격 폭은 기본값 그대로다.
- 몽둥이 부분/풀 충전 피해, 충격파 피해와 창끝 피해에도 같은 공격력 배율을 적용해 반올림한다. 충격파의 기존 마나 보상 없음은 유지한다.
- 사거리 변화에 맞춰 채찍 안쪽 빈틈, 창끝 구간 길이, 충격파 거리도 같은 비율로 조절한다. 충전 시간과 공격 각도/폭은 유지한다.
- 처치 추가 마나 회복은 기본값 15 그대로다.
- 우클릭 마법 피해/마나/쿨타임은 무기 랜덤 옵션과 독립적이다.

기본 데이터 에셋을 변경해도 이미 생성한 개체의 네 주 능력치는 재계산하지 않는다. 공격 형태/각도/폭/특수 공격 설정 등 비추첨 정보는 기본 데이터에서 계속 읽는다. 랜덤 설정 변경도 이후 생성하는 드롭부터 적용한다.

## 몬스터별 테이블

| 프리팹 | 드롭 테이블 | 확률 | Club / Spear / Whip 가중치 |
|---|---|---|---|
| Enemy.prefab | DefaultWeaponDrops.asset | 0.4 | 40 / 30 / 30 |
| Enemy_MeleeDrops.prefab | MeleeWeaponDrops.asset | 0.5 | 60 / 30 / 10 |
| Enemy_RangedDrops.prefab | RangedWeaponDrops.asset | 0.5 | 10 / 60 / 30 |

세 프리팹은 동일한 기존 추적 AI/체력 2/삼각형/Collider를 사용한다. MeleeDrops/RangedDrops는 테이블 테스트용 이름이며 새로운 근접/원거리 AI가 아니다.

각 프리팹의 Enemy Weapon Drop 2D → Drop Table과 Pickup Prefab은 연결 완료했다. 세 테이블의 Roll Settings도 DefaultWeaponRolls.asset에 연결했다. 가중치는 합계 100일 필요 없이 양수들의 상대 비율이다. Drop Chance=0이면 드롭하지 않는다.

현재 SampleScene의 Enemy Spawner 2D는 기존 Enemy.prefab을 유지한다. 다른 테이블을 실제 웨이브에서 테스트하려면 Play를 끄고 WaveSystem → Enemy Spawner 2D → Enemy Prefab에 원하는 테스트 프리팹을 연결한다. 이번 단계에서 웨이브를 여러 종류 혼합 생성하도록 바꾸지는 않았다.

새로운 몬스터별 설정은 테이블을 복제해 Drop Chance/Entries/Roll Settings를 조절하고 해당 적 프리팹에 지정한다. Roll Settings를 비워 둔 사용자 테이블은 안전하게 기본 수치 개체를 만든다. 랜덤 추첨을 원하면 반드시 연결한다.

## 비교 UI

기존 HUD 생성 방식을 재사용해 화면 오른쪽에 비교 패널을 자동 생성한다. 별도 Canvas/Text/Inspector 참조 연결은 필요 없다.

획득 가능한 가장 가까운 바닥 무기 하나에 대해:

- Equipped / Ground 무기 이름.
- 공격력, 공격 간격(초), 사거리, 타격 마나 회복량의 현재 → 바닥 수치.
- 초록 better / 빨강 worse / 회색 =.
- 공격 간격은 낮을수록 better이고, 나머지는 높을수록 better다.
- 공격 방식이 다르므로 수치만으로 전체 성능을 평가하지 않는다는 문구.
- 기존 E키 안내도 함께 유지한다.

근처에 무기가 없거나 사망/StageClear면 비교 패널을 숨긴다. UI는 Raycast를 막지 않아 이동/조준/공격을 가로채지 않는다.

## 연결과 마이그레이션

- 기존 씬/적/아이템/투사체 GUID와 참조는 유지했다. Scene 파일은 이번 단계에서 수정하지 않았다.
- 기존 DefaultWeaponDrops.asset에 Roll Settings 연결을 추가했다.
- 바닥 프리팹의 기존 Weapon 필드는 유지했다. 직접 배치한 구형 아이템에 기본 에셋이 지정되어 있으면 Awake에서 기본 개체를 한 번 생성한다. 드롭은 명시적으로 추첨한 개체를 전달한다.
- WeaponPickup2D.Weapon / Loadout.CurrentWeapon / TryEquipWeapon / Spawn의 데이터 형식을 기본 에셋에서 WeaponInstance2D로 변경하고 프로젝트 내 호출부와 테스트를 함께 갱신했다. 장착 시 새 개체를 만드는 호환 오버로드는 두지 않아 재추첨 경로를 방지했다.
- 무기를 교체해도 HP/마나/공격 대기/마법 쿨타임은 초기화하지 않는다. 기존 몽둥이 충전 초기화 규칙은 유지한다.

## Play 모드 테스트 절차

1. 시작 Club의 수치가 기본값이고 체력 10/마나 100/웨이브 1/골드 0인지 확인한다.
2. 테스트할 테이블의 Drop Chance를 임시 1로 두고 같은 종류의 적을 여러 번 처치한다. 같은 무기 종류라도 공격 간격/거리/타격 마나 값이 달라지는지 확인한다. 이후 확률을 원래대로 돌린다.
3. 가까운 아이템의 비교 패널에 실제 개체 수치가 표시되는지 확인한다. 느린 공격 간격은 빨강, 빠른 간격은 초록이어야 한다.
4. E 획득 전 바닥 아이템 Instance의 ID/네 수치를 기록한다. 획득 후 Player Loadout의 Current Weapon이 같은 ID/수치인지 확인한다.
5. 다른 무기를 획득해 기존 무기를 버린 다음 다시 주워도 ID/능력치가 동일해야 한다. 반복 교체로 바닥 아이템 수가 증가하거나 수치가 재추첨되면 안 된다.
6. 안전한 버리기 위치가 없는 상황을 강제해 교체 실패 시 장착/바닥 양쪽 개체가 유지되는지 확인한다. 기존 방식대로 Room Bounds를 임시로 너무 작게 하면 실패 경로를 검사할 수 있다. 이후 복구한다.
7. 기본/근접형/장거리형 테스트 프리팹으로 Enemy Spawner의 연결을 바꿔 해당 테이블을 사용하는지 확인한다. 확률은 적은 표본에서 정확한 비율을 보장하지 않는다. 단일 항목만 양수로 설정하면 종류 선택을 확실하게 검사할 수 있다.
8. DefaultWeaponRolls의 네 범위를 임시로 X=Y=1로 설정하면 이후 드롭이 기본 수치로 나와야 한다. 기존 드롭 수치는 바뀌지 않아야 한다. 이후 기본 범위를 복구한다.
9. 몽둥이 충전/충격파, 창끝 구간, 채찍 띠, 실제 공격 간격/사거리와 타격 마나가 장착 개체에 맞는지 확인한다. 각도/폭과 처치 추가 회복 15는 유지되어야 한다.
10. 우클릭 마법의 소모 마나 30/쿨타임 2초와 기존 설정이 유지되는지 확인한다. 무기 교체로 근접/마법 쿨타임을 우회할 수 없어야 한다.
11. 사망/StageClear 이후 E 획득과 장착을 거부하고 패널을 숨기는지 확인한다. Restart 후 바닥 아이템과 이전 개체를 제거하고 새 기본 Club/웨이브/자원을 초기화해야 한다.
12. Console 오류가 없는지 확인하고, Play 종료 후 테스트용으로 바꾼 에셋/스포너 수치를 되돌린다. ScriptableObject Inspector 변경은 Play 종료 뒤에도 남을 수 있다.

## 생성 파일

- Assets/Scripts/Combat/WeaponInstance2D.cs 및 .meta — 런타임 고유 ID/능력치/특수 판정 연결.
- Assets/Scripts/Combat/WeaponRollSettings2D.cs 및 .meta — Inspector 배율 범위와 단일 추첨.
- Assets/Settings/Weapons/DefaultWeaponRolls.asset 및 .meta.
- Assets/Settings/Weapons/MeleeWeaponDrops.asset, RangedWeaponDrops.asset 및 .meta.
- Assets/Prefabs/Enemy_MeleeDrops.prefab, Enemy_RangedDrops.prefab 및 .meta.
- PROTOTYPE_V6_SETUP.md — 이 보고서.

## 수정 파일

- WeaponPartData2D.cs — 원본의 수치 변경 없이 유효 사거리/안쪽 거리로 판정하는 공통 메서드 확장.
- PlayerWeaponLoadout2D.cs — 개체 하나 장착, 시작 기본 개체, Inspector 확인 필드.
- PlayerMelee2D.cs — 실제 개체의 피해/간격/거리/마나 및 특수 판정 사용.
- WeaponDropTable2D.cs / EnemyWeaponDrop2D.cs — 테이블별 랜덤 설정과 최초 개체 생성.
- WeaponPickup2D.cs / PlayerWeaponPickup2D.cs — 개체 그대로 전달, 기존 수동 배치 호환.
- GameHUD2D.cs — 비교 UI와 개체 기반 기존 이름/충전 표시.
- DefaultWeaponDrops.asset — 기본 랜덤 설정 연결.
- Tests/Standalone/WeaponPartTests.cs — 개체와 랜덤/원본 보존 테스트 추가.

## 검증 결과와 한계

- 전체 런타임 스크립트: 설치된 Unity 참조 어셈블리와 Roslyn으로 별도 C# 컴파일 오류 0.
- 독립 무기/개체/드롭 로직 테스트 22개 통과. 배율 경계와 반올림/최소값, 원본 미수정, 동일 종류의 다른 ID, 재장착 시 ID/수치 유지, 네 능력치 스냅샷 유지, 채찍/창끝 사거리 연동, 새 개체 생성 포함.
- 기존 웨이브/골드 상태 로직 테스트 8개 재실행 통과.
- 씬/프리팹 fileID 참조, GUID 중복, 세 테이블 확률/가중치와 랜덤 설정 연결 검사 통과.
- 독립 테스트는 Unity API 의존성을 가짜로 대체한다. 실제 랜덤 통계 분포, 물리 판정, 아이템 교체 트랜잭션, 비교 UI 색/배치, SceneManager 재시작은 실행한 테스트가 아니다.
- Unity 에디터 임포트/컴파일 및 Play 모드는 직접 확인하지 않았다. 위 Play 절차가 필요하다.
- 기본 공격력 1의 정수 반올림 한계와 특수 피해 배율 적용은 밸런스 검증 시 고려한다. 무기별 모양 차이 때문에 초록 수치가 많아도 전체적으로 더 좋은 무기라고 단정하지 않는다.

새 AI/인벤토리/등급/강화/오브 조합/영구 저장은 추가하지 않았다.
