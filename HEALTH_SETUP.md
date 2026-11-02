# 플레이어 체력과 접촉 피해 설정

## Unity에서 연결

1. Play 모드를 끄고 Hierarchy에서 기존 Player를 선택한다.
2. Add Component에서 PlayerHealth2D를 추가한다.
3. Player Health 2D 컴포넌트의 값을 설정한다.
   - Max Health: 3
   - Contact Damage: 1 (적 접촉 한 번에 받는 피해)
   - Invulnerability Duration: 1 (피격 후 무적 시간, 초)
   - Current Health: 실행 중 확인할 값. Play 시작 시 Max Health로 초기화하므로 직접 설정하지 않는다.
4. Hierarchy의 Enemy를 선택하고 Enemy Chase 2D의 **Stop Distance를 0으로 변경**한다.
   기존 씬에는 이전 값 1이 저장되어 있으므로 코드 기본값을 바꾸어도 자동 반영되지 않는다.
   0이면 적이 플레이어 중심을 향해 이동하다 Collider에 막혀 실제 접촉을 유지한다.
   적이 여러 마리면 각각 변경한다.
5. Player와 Enemy의 Collider 2D에서 Is Trigger가 꺼져 있고 Rigidbody 2D의 Simulated가 켜져 있는지 확인한다.
6. 씬을 저장한다. 기존 입력 액션, 발사 프리팹, 카메라 설정을 바꿀 필요는 없다.

## 직접 확인

1. Player를 선택한 상태에서 Play를 실행한다. Current Health가 3인지 확인한다.
2. Game 창을 클릭하고 적이 닿게 둔다. 체력이 2로 줄고 Console에 Player HP 로그가 표시되는지 확인한다.
3. 계속 접촉해도 물리 프레임마다 피해를 받지 않고, 약 1초 간격으로 체력이 줄어야 한다.
4. 피격 후 1초 안에 다시 닿거나 여러 적이 동시에 닿아도 무적 시간 동안 추가 피해가 없어야 한다.
5. 적을 피하거나 투사체로 처치하면 더 이상 그 적으로부터 피해를 받지 않아야 한다. 벽 접촉은 피해를 주지 않는다.
6. 체력이 0이면 Player Movement 2D와 Player Shooting 2D가 비활성화되고 이동과 새 투사체 발사가 중지되어야 한다. 남은 적에 밀리지 않도록 Rigidbody2D의 위치와 회전도 고정한다.
7. 사망 전에 발사한 투사체는 기존 규칙대로 계속 동작한다. 적 전체를 정지시키거나 씬을 자동 재시작하지 않는다.
8. Play를 종료하고 다시 실행하면 체력과 컴포넌트 활성 상태가 씬에 저장한 초기값으로 돌아오는지 확인한다.

이번 단계에서는 체력 UI나 피격 이펙트 없이 Inspector의 Current Health와 Console로 확인한다.
무적 시간은 Time.time 기준이므로 게임 시간이 멈추면 함께 멈춘다.

## 파일 변경

- 추가: Assets/Scripts/Player/PlayerHealth2D.cs
- 수정: Assets/Scripts/Enemy/EnemyChase2D.cs (처치 상태 공개, 새 컴포넌트의 Stop Distance 기본값 0)
- 추가: HEALTH_SETUP.md

기존 이동/발사/프로젝트 설정 및 씬 파일은 수정하지 않았다.
Unity 컴파일 및 Play 실행을 직접 확인하지 않았다. 위 절차로 실제 동작을 확인해야 한다.
