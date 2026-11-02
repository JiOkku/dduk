# 삼각형 추적 적

## Unity 설정

Play 모드를 끈 상태에서 아래 작업을 진행한다. 기존 Player, 카메라, 벽과 투사체 프리팹은 유지한다.

1. Hierarchy 우클릭 → 2D Object → Sprites → Triangle을 선택한다. 이름을 Enemy로 바꾼다.
2. Transform의 Position을 (3, 2, 0), Rotation을 (0, 0, 0), Scale을 (1, 1, 1)로 설정한다. Player 및 벽과 겹치지 않는 방 안에 둔다.
3. Sprite Renderer의 Color를 빨강, Order in Layer를 0으로 설정한다. 바닥의 Order in Layer는 기존 -1을 유지한다.
4. Add Component → EnemyChase2D를 추가한다. Rigidbody 2D와 Polygon Collider 2D도 자동 추가된다.
5. Enemy Chase 2D 컴포넌트를 설정한다.
   - Target: Hierarchy의 기존 Player를 끌어 넣는다.
   - Move Speed: 2 (Player의 기본 이동 속도 5보다 느림).
   - Stop Distance: 0 (플레이어 접촉 피해를 위해 실제 충돌할 때까지 접근).
6. Polygon Collider 2D의 Is Trigger를 끈다. Edit Collider로 외곽선이 삼각형 스프라이트와 맞는지 확인한다. 기존 Box Collider 2D 등이 있다면 제거하고 Polygon Collider 2D 하나만 사용한다.
7. Rigidbody 2D의 Simulated는 켜 둔다. 실행 시 스크립트가 Dynamic, Gravity Scale 0, Linear Damping 0, Freeze Rotation Z, Interpolate를 적용한다.
8. 씬을 저장한다. 현재는 씬의 적 한 마리면 충분하므로 적 프리팹을 만들 필요는 없다.

## 확인 순서

1. Play 후 적이 Player를 향해 움직이고 Player의 Collider에 막히는지 확인한다.
2. WASD로 자리를 옮기면 적이 새 위치로 따라오는지 확인한다.
3. 적을 향해 왼쪽 클릭한다. 투사체가 적의 Collider에 닿으면 적과 투사체가 함께 사라져야 한다.
4. 적을 빗맞힌 투사체는 기존처럼 벽 충돌이나 수명 종료로 사라져야 한다.
5. Play를 종료하고 다시 실행하면 씬에 저장한 적이 다시 나타나야 한다. 런타임 Destroy는 씬 에셋의 적을 삭제하지 않는다.
6. Console에 컴파일 오류나 실행 오류가 없는지 확인한다.

## 현재 범위

- 삼각형 외형은 Unity 기본 Triangle 스프라이트로 구성한다.
- Rigidbody2D의 속도로 직선 추적하며 벽의 일반 Collider 2D에 막힌다. 장애물을 돌아가는 길찾기는 없다.
- 투사체 한 발에 적이 사라진다. 중복 충돌로 여러 적을 처치하지 않도록 투사체는 최초 충돌 한 번만 처리한다.
- 접촉 피해와 플레이어 체력 설정은 HEALTH_SETUP.md를 참고한다. 적의 별도 공격, 체력 UI, 자동 생성, 보상은 아직 구현하지 않았다.
- 적은 Stop Distance 안에서 멈추지만 플레이어가 직접 접근하면 물리 접촉이 발생할 수 있다.

## 변경 파일

- 추가: Assets/Scripts/Enemy/EnemyChase2D.cs
- 수정: Assets/Scripts/Combat/Projectile2D.cs
- 추가: ENEMY_SETUP.md

기존 입력/이동/카메라/프로젝트 설정 및 씬 파일은 수정하지 않았다.
Unity에서 직접 컴파일하거나 Play 모드로 실행하지 않았으므로 위 항목은 수동 확인 절차이다.
