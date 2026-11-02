# 4차 프로토타입: 지팡이 무기 파츠

5차에서 숫자키 교체가 제거되고 드롭/E키 획득으로 바뀌었다. 현재 장착/획득 방법은 PROTOTYPE_V5_SETUP.md를 참고한다.

후속 무기 역할 조정이 적용되어 아래 초기 수치/판정 일부가 변경되었다. 현재 사양은 WEAPON_ROLES_SETUP.md를 참고한다.

## 분석과 구현 방식

기존 PlayerMelee2D는 하나의 부채꼴 판정, 벽 Raycast, 적별 중복 제외, TryTakeDamage 성공 시 마나 보상을 처리하고 있었다. 이 공통 흐름을 유지하면서 모양과 수치를 WeaponPartData2D 에셋에서 읽도록 확장했다.

PlayerShooting2D는 조준과 기본 마법을 계속 담당한다. EnemyChase2D의 체력/사망 이벤트, 웨이브/골드 집계, PlayerHealth2D의 사망/StageClear 정지와 씬 재시작 구조는 수정하지 않았다. 무기 교체는 PlayerWeaponLoadout2D가 담당하며 체력/마나/마법 타이머를 변경하지 않는다.

## 조작과 기본 수치

- WASD: 이동, 마우스: 조준.
- 좌클릭: 장착한 무기로 근접 공격. 클릭당 한 번, 누르고 있어도 자동 반복하지 않는다.
- 우클릭: 기존 마법. 마나 30, 쿨타임 2초 및 피해 설정 유지.
- 키보드 위쪽 숫자 1 / 2 / 3: Club / Spear / Whip. 숫자패드는 연결하지 않았다.

| 무기 | 판정 | 피해 | 간격 | 거리 | 각도/전체 폭 | 타격 회복 | 처치 추가 |
|---|---|---|---|---|---|---|---|
| Club | 전방 부채꼴 | 1 | 0.4초 | 1.5 | 90도 | 10 | 15 |
| Spear | 전방 직사각형, 다중 관통 | 2 | 0.6초 | 2.5 | 폭 0.5 | 8 | 15 |
| Whip | 전방 부채꼴 | 1 | 0.9초 | 3.0 | 120도 | 5 | 15 |

이번 지시서에 따라 Club 거리는 이전 단일 근접 범위 1.8에서 1.5로 변경했다. 다시 늘리려면 Club.asset의 Range를 조절한다. 플레이어 HP 10, 자동 생성 Enemy HP 2, 웨이브 수/골드/기존 마법 수치는 유지했다.

공격 효과는 Club 노란 부채꼴, Spear 청록색 직사각형, Whip 분홍 부채꼴 윤곽이다. 0.15초 동안 공격 순간의 위치/방향에 표시한다. 벽에 맞춰 효과를 잘라내지는 않지만 실제 피해는 기존 벽 판정으로 차단한다. 무기 모델이나 별도 애니메이션은 없다.

## Unity Inspector와 연결

SampleScene에는 PlayerWeaponLoadout2D와 아래 에셋을 이미 연결했다. 외부 변경 알림이 뜨면 Play를 끄고 Reload한다. 별도로 저장하지 않은 씬 편집이 있으면 먼저 다른 씬으로 보존한다.

Player → Player Weapon Loadout 2D:

| 필드 | 에셋 |
|---|---|
| Club | Assets/Settings/Weapons/Club.asset |
| Spear | Assets/Settings/Weapons/Spear.asset |
| Whip | Assets/Settings/Weapons/Whip.asset |

새 컴포넌트를 중복 추가할 필요가 없다. 세 무기 모두 기본 제공되며 매번 씬을 새로 시작하면 Club으로 초기화한다. Inspector의 에셋 연결 순서를 바꾸기보다는 해당 무기 에셋의 수치를 조절한다.

무기 수치는 Project 창의 Assets → Settings → Weapons에서 해당 에셋을 선택해 조절한다.

- Weapon Name: UI 이름.
- Attack Shape: Sector(부채꼴) / Line(직선 직사각형).
- Damage / Attack Interval / Range: 피해 / 공격 후 대기 시간 / 거리.
- Angle: Sector의 전체 각도. Line에서는 사용하지 않는다.
- Width: Line의 전체 폭. Sector에서는 사용하지 않는다.
- Mana On Hit / Mana On Kill: 실제 피해 시 회복 / 그 피해로 처치했을 때 추가 회복.

ScriptableObject 데이터 에셋은 컴포넌트가 아니므로 Player에 직접 붙이지 않는다. Create → Prototype → Weapon Part 2D 메뉴로 같은 형식의 데이터를 생성할 수 있다. 현재 시스템의 장착 슬롯은 요청한 세 개로 고정되어 있다.

기존 Player Melee 2D에는 Obstruction Layers와 Effect Duration만 남는다. 무기 전투 수치의 편집 위치가 데이터 에셋으로 이동했다. Obstruction Layers에는 벽 레이어를 계속 포함해야 한다.

기존 Input Actions / Aim Camera / Projectile Prefab 참조는 유지했다. 적/투사체 프리팹 수정이나 추가 연결은 없다. GameHUD2D는 기존 Player Health에서 같은 Player의 Loadout을 찾아 Weapon UI를 자동 생성한다.

ScriptableObject 에셋을 Play 중 수정하면 변경 사항이 종료 후에도 남을 수 있다. 테스트 후 세 에셋의 수치를 확인한다.

## 판정·교체·보상 규칙

- 이전과 동일하게 Collider의 중심으로 거리/각도를 판단한다. 적의 외곽만 걸치고 중심이 밖이면 맞지 않는다.
- Spear는 플레이어 중심에서 조준 방향으로 0~Range, 좌우 Width/2 안의 적 중심을 판정한다. 직선 위에 여러 적이 있어도 서로 차단하지 않는다.
- 두 판정 모두 OverlapCircle/OverlapBox로 후보를 찾은 뒤, 적마다 HashSet으로 중복을 제외하고 플레이어에서 적까지 벽 검사를 수행한다. Trigger는 제외한다.
- 실제 TryTakeDamage 성공 시에만 타격 마나를 지급하고, killed 결과가 true일 때 추가 보상을 준다. 골드는 기존 Enemy 사망 이벤트가 한 번 처리한다.
- 무기 교체는 공격 대기 시간을 초기화하거나 단축하지 않는다. 다음 공격은 **직전에 공격한 무기의 간격**이 끝난 후 가능하다. 예: Whip 공격 직후 Club 교체 → 0.9초 이후 공격 가능.
- 교체 자체는 대기 시간 중에도 즉시 가능하다. 화면 이름은 새 무기로 바뀌지만 이미 표시된 공격 효과는 이전 공격의 형태를 잠깐 유지한다.
- Playing과 WaveTransition에서는 교체 가능하다. PlayerHealth.CombatStopped가 true인 사망/StageClear에서는 교체를 거부한다.
- PlayerMelee2D는 기존 사망/클리어 처리에서 비활성화되고, 마법의 별도 타이머와 마나는 유지된다. 무기 교체 스크립트는 PlayerHealth의 공통 상태를 검사한다.

## Unity Play 모드 확인 절차

아래는 실제 실행해야 할 절차다.

1. 시작 시 Weapon: Club, HP 10 / 10, Mana 100 / 100, Wave 1 / 3인지 확인한다.
2. 2를 누르면 Weapon: Spear, 3은 Whip, 1은 Club으로 즉시 변경되는지 확인한다. HP/마나는 변하지 않아야 한다.
3. Club으로 전방 1.5/90도 안의 여러 적을 타격하고, 뒤/범위 밖의 적은 맞지 않는지 확인한다.
4. Spear로 전방 2.5/폭 0.5의 좁고 긴 영역을 확인한다. 직선상의 여러 적은 맞고 옆으로 벗어난 적은 맞지 않아야 한다.
5. Whip의 3.0/120도가 Club보다 길고 넓으며, 0.9초 간격으로 느리게 공격하는지 확인한다.
6. 자동 생성 적 HP 2에서 Club/Whip은 두 번, Spear는 한 번의 유효 공격에 처치되는지 확인한다.
7. 마나를 먼저 소비한 뒤 보상을 확인한다. Club 첫 타격 +10, 처치 타격 +25. Spear 한 번 처치 +23. Whip 첫 타격 +5, 처치 타격 +20. 여러 적이면 적마다 지급되고 상한 100을 넘지 않아야 한다.
8. 벽 뒤 적은 모든 무기로 피해/마나 보상을 받지 않아야 한다. 임시 적의 Move Speed를 0으로 두면 범위 비교가 쉽다. 테스트 후 원래 수치로 되돌린다.
9. 적 한 마리에 Collider를 임시 추가해도 한 번 공격당 피해/회복/골드가 중복되지 않아야 한다.
10. Whip 공격 직후 1/2/3을 반복하며 클릭해도 0.9초 이전에 다음 공격이 나가지 않아야 한다. Club/Spear도 각각 마지막 공격 간격을 유지하는지 확인한다.
11. 우클릭 마법을 쓴 직후 무기를 교체해도 마법 쿨타임이 줄거나 초기화되지 않아야 한다. 교체만으로 마나 소비/회복이 발생해서도 안 된다.
12. 사망/Stage Clear 뒤 1/2/3과 좌우 클릭을 눌러도 장착 무기나 공격이 바뀌지 않아야 한다. Restart 버튼은 기존대로 동작한다.
13. Whip/Spear를 장착한 상태에서 Restart 후 Club/체력 10/마나 100/웨이브 1/골드 0/UI 초기화인지 확인한다.
14. 기존 벽 충돌/카메라/접촉 피해와 무적/웨이브 3·5·7/골드 5/최종 클리어가 유지되는지 확인한다.

## 파일 목록

추가:

- Assets/Scripts/Combat/WeaponPartData2D.cs — 무기 ScriptableObject, 공통 모양별 중심점 판정.
- Assets/Scripts/Player/PlayerWeaponLoadout2D.cs — 장착 상태, 숫자키, 상태 제한, 시작 무기 초기화.
- Assets/Settings/Weapons/Club.asset, Spear.asset, Whip.asset — 각 무기의 수치.
- 위 스크립트/에셋 및 Weapons 폴더 .meta — 고정 GUID 연결.
- Tests/Standalone/WeaponPartTests.cs — 가짜 의존성을 사용한 데이터/기하/교체 로직 테스트.
- PROTOTYPE_V4_SETUP.md — 이 보고서.

수정:

- Assets/Scripts/Player/PlayerMelee2D.cs — 기존 피해/벽/중복/마나 처리를 재사용하고 데이터별 Sector/Line 후보 및 효과를 적용. 공격 타이머는 계속 이 컴포넌트가 보유한다.
- Assets/Scripts/UI/GameHUD2D.cs — 기존 UI 아래 Weapon 표시 추가.
- Assets/Scenes/SampleScene.unity — Player에 Loadout/세 데이터 연결, 기존 단일 무기 수치는 데이터 에셋으로 이동.
  기존 웨이브 수 저장값 끝의 불필요한 i 문자도 제거하여 3/5/7 정수 배열을 정상화했다.

## 검증 범위

- 모든 Assets/Scripts 런타임 코드: 설치된 Unity 참조 어셈블리와 Roslyn으로 별도 C# 컴파일 통과(오류 0). Inspector 주입 필드의 CS0649 경고는 있다.
- 무기 데이터/기하/선택 로직 테스트 9개 통과: 부채꼴 앞/뒤/거리, Whip 각도/거리 차이, Spear 폭/거리/다중 위치, 회전/이동 좌표, 조준 0 거부, 잘못된 수치 보정, 시작 Club/교체, 종료 상태 차단, 참조 누락 처리.
- 기존 웨이브/골드 로직 테스트 8개 재실행 통과.
- 독립 테스트는 실제 소스를 사용하되 Unity API/물리/입력/라이프사이클을 가짜 의존성으로 대체한다. 실제 충돌·벽·다중 피해·마나 보상·공격 타이머·렌더링·UI·SceneManager 재시작을 실행한 결과가 아니다.
- Unity 에디터 임포트/컴파일 및 Play 모드 실행은 직접 확인하지 않았다. 위 수동 절차로 최종 확인해야 한다.

## 기존 기능에 미치는 변경

- 이전 단일 근접 수치(거리 1.8 포함)는 더 이상 PlayerMelee2D에서 편집하지 않는다. 새 지시서대로 Club은 거리 1.5다.
- 다른 씬에서 PlayerMelee2D를 사용하는 경우 같은 Player에 Loadout을 추가하고 세 데이터를 연결해야 한다. 현재 SampleScene은 연결 완료했다.
- 무기 교체는 숫자키 입력만 추가하며 공유 입력 에셋/마법 코드/기존 프리팹/웨이브/보상 코드에는 변경이 없다.
- 제외 범위인 드롭/조합/제작/강화/등급/상점/영구 성장 등은 추가하지 않았다.
