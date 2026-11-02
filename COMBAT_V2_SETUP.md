# 2차 전투 프로토타입

## 분석 및 유지한 구조

- Unity 6000.6.7f1, Input System 1.20.1, Active Input Handling=Input System.
- SampleScene에는 Player, Enemy, 벽 4개, 바닥, 카메라, GameUI가 있다.
- PlayerShooting2D의 기존 Input Actions / Aim Camera / Projectile Prefab 필드와 GUID를 유지했다.
- Assets/Prefabs/Projectile.prefab은 그대로 사용하며 파일을 수정하지 않았다.
- 이동, 카메라, 적 추적, 플레이어 체력·무적, 벽 충돌, Game Over와 씬 재시작 구조를 유지했다.
- 공유 InputSystem_Actions.inputactions는 변경하지 않았다. 근접은 기존 Player/Attack의 좌클릭 바인딩만 사용하고, 마법은 우클릭 전용 런타임 InputAction을 사용한다. 둘 다 현재 새 Input System으로 동작한다.

## 이번에 구현한 전투 흐름

WASD 이동 → 마우스로 조준 → 좌클릭 근접 타격으로 마나 회복 → 우클릭 마법에 마나 소비 → 접촉 피해/사망 → Restart.

- 근접은 클릭당 한 번의 즉시 판정이며 0.4초 간격이다. 누르고 있다고 자동 반복하지 않는다.
- 부채꼴 판정은 플레이어 중심에서 적의 Collider 중심까지 거리 1.5, 전방 전체 각도 90도이다. 적의 일부만 범위에 걸친 경우 중심이 범위 밖이면 맞지 않는다.
- 적에게 실제 피해를 준 경우만 +10, 그 피해로 처치하면 추가 +15. 기본 HP 1인 적을 근접 처치하면 총 +25다. 여러 적이면 적마다 지급하며 최대 100을 넘지 않는다.
- 벽에 가려진 적은 맞지 않는다. 같은 적에 Collider가 여러 개 있어도 한 번의 공격에서는 한 번만 피해/보상을 받는다.
- 노란 부채꼴 윤곽을 0.15초 표시한다. 공격 순간 위치에 고정된 임시 표시이며 벽에 맞춰 잘라내지는 않는다. 벽 너머 표시가 보이더라도 실제 피해는 차단된다.
- 마법은 클릭당 한 발, 피해 1, 마나 30, 쿨타임 2초. 부족하거나 쿨타임 중이면 발사/마나 소비/쿨타임 갱신을 하지 않는다.
- 마법으로 적을 처치해도 마나를 회복하지 않는다. 자연 회복도 없다.

## 이미 연결한 씬 설정

Assets/Scenes/SampleScene.unity에 다음을 직접 반영했다.

- 기존 Player에 PlayerMana2D, PlayerMelee2D 추가 및 기본 수치 저장.
- 기존 PlayerShooting2D에 Magic 설정 저장. 카메라/입력/투사체 참조는 유지.
- 기존 Enemy의 Max Health=1 저장. 추적 대상·속도·Stop Distance는 유지.
- 기존 GameUI는 Player Health 참조에서 같은 Player의 Mana/마법 컴포넌트를 자동으로 찾는다. 새 UI 참조를 연결할 필요가 없다.

Unity로 돌아가 외부 씬 변경을 다시 불러오라는 안내가 나오면 변경된 씬을 불러온다. 에디터에 이전 씬 상태가 열린 채로 남아 있다면 SampleScene을 다시 연다. **새 컴포넌트를 중복해서 추가하지 않는다.** 아직 저장하지 않은 별도 편집 내용이 있다면 먼저 다른 씬으로 보존하고 변경된 SampleScene과 비교한다.

## Inspector 수치

| 대상 / 컴포넌트 | 항목 | 기본값 |
|---|---|---|
| Player / Player Mana 2D | Max Mana / Starting Mana | 100 / 100 |
| Player / Player Melee 2D | Damage | 1 |
| Player / Player Melee 2D | Attack Interval | 0.4초 |
| Player / Player Melee 2D | Range / Angle | 1.5 / 90도 |
| Player / Player Melee 2D | Mana On Hit / Mana On Kill | 10 / 15 |
| Player / Player Melee 2D | Obstruction Layers | Everything |
| Player / Player Melee 2D | Effect Duration | 0.15초 |
| Player / Player Shooting 2D → Magic | Damage / Mana Cost / Cooldown | 1 / 30 / 2초 |
| Enemy / Enemy Chase 2D | Max Health | 1 |

Current Mana와 Current Health는 런타임 확인용이다. 시작값은 Starting Mana / Max Health에서 초기화한다. Play 중 조절한 수치는 종료하면 원래대로 돌아간다.

벽은 현재 Default 레이어를 포함하는 Everything으로 판정하므로 기존 레이어 설정 변경이 필요 없다. 추후 Obstruction Layers를 좁히면 반드시 벽 레이어를 포함해야 한다. Trigger, 플레이어 자신, 적, 투사체는 벽 차단물에서 제외한다. 바닥에는 기존처럼 Collider를 붙이지 않는다.

다른 씬에 적용할 경우 Player에 PlayerMana2D와 PlayerMelee2D를 추가하고 기존 PlayerShooting2D의 Input Actions, Aim Camera, Projectile Prefab을 연결한다. 마나 스크립트를 먼저 추가한다. GameHUD2D의 Player Health를 같은 Player로 연결한다. MagicAttackSettings2D는 컴포넌트나 ScriptableObject 에셋이 아니라 PlayerShooting2D 내부에 직렬화되는 수치 묶음이므로 따로 오브젝트에 붙이지 않는다.

## UI

기존 체력 아래 Mana: 현재 / 최대를 표시한다. 그 아래 마법 상태를 표시한다.

- Magic: Ready — 쿨타임 종료 및 마나 충분.
- Magic: 1.5s — 남은 쿨타임.
- Magic: Not enough mana — 쿨타임은 끝났으나 마나 부족.
- Magic: Disabled — 사망.
- Magic: Unavailable — 컴포넌트 누락 또는 비활성화.

## Play 모드 수동 테스트

아래는 직접 실행해야 하는 검증 절차이며 실행 결과가 아니다. 적 한 마리는 기본 HP가 1이라 바로 사라지므로, 필요할 때 기존 Enemy를 Ctrl+D로 복제하고 서로 겹치지 않게 배치한다. 검사 후 임시 복제와 수치를 되돌린다.

1. **기존 기능**: WASD 대각선, 카메라, 벽 충돌, 접촉 피해 1/무적 1초, 체력 UI가 유지되는지 확인한다.
2. **입력 분리**: 좌클릭은 노란 부채꼴만 나타나고 투사체는 발사하지 않아야 한다. 우클릭은 투사체를 발사한다. 버튼을 누르고 있기만 해서는 반복 발사하지 않는다.
3. **마법 수치**: 100에서 우클릭 후 70, 2초 동안 클릭해도 70, 쿨타임 이후 40, 다음 10. 10에서는 발사하지 않고 마나가 그대로여야 한다. 실패한 클릭으로 쿨타임이 다시 시작되면 안 된다.
4. **회복 없음**: 기다려도 마나가 자연 회복하지 않아야 한다. 마법으로 적을 처치해도 마나 추가 회복이 없어야 한다.
5. **근접 보상**: 마나가 10일 때 기본 HP 1 적을 근접 처치하면 35가 된다. 공중에 헛스윙하면 회복하지 않는다. 마나가 90이면 처치 후 100에서 멈춘다.
6. **비치명타**: Play 전에 적 Max Health를 2로 설정한다. 마나 70에서 첫 근접 피해는 적 HP 1/마나 80, 다음 유효 공격은 적 사망/마나 100이어야 한다. 두 공격 사이에는 0.4초 이상 둔다.
7. **다중 타격**: 마나가 10일 때 전방 부채꼴 안에 기본 적 두 마리를 두고 한 번에 맞히면 60이 된다. 적 하나에 Collider를 임시 추가하더라도 피해와 +25 보상이 적당 한 번만 발생하는지 확인한다.
8. **범위/각도/간격**: 전방 1.5 이내는 맞고 뒤쪽/범위 밖은 맞지 않아야 한다. 필요하면 적 Move Speed를 0으로 두어 배치한다. 0.4초 이내의 추가 클릭은 피해도 효과도 새로 만들지 않아야 한다.
9. **벽 차단**: 얇은 임시 Square + BoxCollider2D를 플레이어와 적 사이에 놓는다. 적 중심이 1.5 이내/전방에 있어도 피해와 회복이 없어야 한다. 벽 옆에서 시야가 열리는 방향으로 이동하면 공격 가능해야 한다. 임시 벽은 삭제한다.
10. **사망**: 체력 0에서 이동·근접·마법이 멈추고 마나도 회복되지 않아야 한다. Game Over/Restart는 정상 표시된다. 기존에 발사된 마법 투사체는 남은 수명 동안 계속 동작하지만 마나 보상은 주지 않는다.
11. **재시작**: Restart 후 HP 3, Mana 100, Magic Ready, 원래 플레이어/적 위치·적 HP로 복구되는지 확인한다. 여러 번 재시작해 입력·UI·효과가 중복되지 않는지도 확인한다.
12. **Console**: 컴파일/실행 오류가 없는지 확인한다. Inspector에서 Current Mana가 0~Max Mana 범위인지 확인한다.

## 파일과 역할

추가:

- Assets/Scripts/Player/PlayerMana2D.cs — 마나 초기화, 상한/하한, 소비/회복, 사망 후 차단.
- Assets/Scripts/Player/PlayerMelee2D.cs — 좌클릭, 공격 간격, 부채꼴/벽 판정, 중복 제외, 근접 보상, 임시 표시.
- Assets/Scripts/Combat/MagicAttackSettings2D.cs — 마법별 피해/소모량/쿨타임 수치 묶음.
- 위 스크립트의 .meta 파일 — 고정 GUID로 씬 연결.
- COMBAT_V2_SETUP.md — 이 보고서.

수정:

- PlayerShooting2D.cs — 기존 조준과 참조 유지, 우클릭·마나 소비·쿨타임·마법 피해.
- Projectile2D.cs — 기존 이동/충돌/발사자 제외 유지, 공통 피해 메서드 호출.
- EnemyChase2D.cs — 기존 AI 유지, HP/실제 피해 결과/일회성 사망 처리.
- PlayerHealth2D.cs — 기존 사망 처리에서 근접 컴포넌트도 비활성화.
- GameHUD2D.cs — 기존 UI 유지, 마나/마법 상태 추가.
- Assets/Scenes/SampleScene.unity — 기존 오브젝트·참조 유지, 새 전투 컴포넌트와 수치 연결.

## 검증 범위와 영향

- 설치된 Unity 참조 어셈블리와 Roslyn C# 컴파일러로 Assets/Scripts의 런타임 코드를 별도 DLL로 컴파일했다. 오류 없이 통과했다. 이것은 Unity 에디터 컴파일/임포트나 실제 플레이 검증을 대신하지 않는다.
- 컴파일러는 Inspector로 주입하는 기존 필드에 대해 CS0649 경고를 출력한다. 기존 HUD의 FindFirstObjectByType에도 사용 중단 경고가 있다. 컴파일 오류는 아니다.
- Unity 에디터 Play 모드, 실제 입력, 물리 판정, 임시 효과 렌더링, UI 및 씬 재시작은 직접 실행하지 않았다.
- 기존 좌클릭 투사체 조작은 의도적으로 없어지고 좌클릭 근접/우클릭 마법으로 바뀐다. 우클릭 투사체에는 자원과 쿨타임 제한이 생긴다.
- 기존 투사체 프리팹은 그대로 사용한다. 입력 설정/패키지/물리 레이어/프로젝트 설정은 수정하지 않았다.
- 이번 범위 밖인 적 생성/웨이브, 보스, 성장, 보상, 장착 시스템은 추가하지 않았다.
