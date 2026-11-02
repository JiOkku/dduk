# 마우스 방향 투사체 공격

기존 이동/카메라, 입력 액션, 씬, 물리 레이어 설정은 변경하지 않았다.
기존 InputSystem_Actions의 Player/Attack(마우스 왼쪽 버튼)을 복제하여 사용한다.
Player/Look의 마우스 입력은 화면 좌표가 아닌 이동량이므로 조준 위치에는 새 Input System의 Mouse.position을 사용한다.
한 번 클릭할 때 한 발 발사한다. 버튼을 누르고 있는 동안 자동 연사는 하지 않는다.

## 1. 투사체 프리팹 만들기

Play 모드를 끈 상태에서 진행한다.

1. Hierarchy 우클릭 → 2D Object → Sprites → Circle로 원을 생성하고 이름을 Projectile로 변경한다.
2. Transform의 Scale을 (0.2, 0.2, 1), Rotation을 (0, 0, 0)으로 설정한다.
3. Sprite Renderer의 Color를 밝은 노랑, Order in Layer를 1로 설정한다.
4. Add Component에서 Projectile2D를 추가한다. Rigidbody 2D와 Circle Collider 2D가 자동 추가된다.
5. Projectile 2D 컴포넌트의 Speed는 12, Lifetime은 2로 둔다. 둘 다 Inspector에서 조절할 수 있다.
6. Circle Collider 2D의 Is Trigger를 끄고 Radius를 0.5로 둔다. Scale이 0.2이므로 실제 반지름은 0.1이다.
7. Rigidbody 2D는 Simulated를 켠다. 실행 시 스크립트가 Dynamic, Gravity Scale 0, Linear Damping 0, Freeze Rotation Z, Continuous 충돌, Interpolate를 설정한다.
8. Project 창의 Assets 아래 Prefabs 폴더를 만든다. Hierarchy의 Projectile을 이 폴더로 끌어 넣어 프리팹을 만든다.
9. 프리팹이 생성됐는지 확인한 후 Hierarchy에 남아 있는 Projectile은 삭제한다. 발사할 때 프리팹의 복제본이 생성된다.

## 2. Player에 발사 스크립트 연결하기

1. Hierarchy에서 기존 Player를 선택한다. 이동 스크립트는 유지한다.
2. Add Component → PlayerShooting2D를 추가한다.
3. Input Actions: Assets/Settings/InputSystem_Actions.inputactions를 끌어 넣는다.
4. Aim Camera: Hierarchy의 Main Camera를 끌어 넣는다.
5. Projectile Prefab: Project 창의 Assets/Prefabs/Projectile 프리팹을 끌어 넣는다. 프리팹 루트에 Projectile2D가 있어야 한다.
6. 씬을 저장한다.

## 3. 직접 확인하기

1. Play 후 Game 창을 클릭하고, 플레이어에서 떨어진 위치를 왼쪽 클릭한다. 첫 클릭은 Game 창 포커스만 잡을 수 있으므로 다시 클릭해 본다.
2. 마우스 위치를 향해 원형 투사체가 날아가는지 확인한다.
3. WASD로 이동하면서도 조준과 발사가 가능한지 확인한다.
4. 벽에 닿으면 투사체가 사라지는지 확인한다. 벽의 Collider 2D는 Is Trigger가 꺼져 있어야 한다.
5. 벽에 닿지 않은 투사체도 Lifetime(기본 2초) 뒤 Hierarchy에서 사라지는지 확인한다.
6. 투사체가 자기 플레이어에 부딪혀 즉시 사라지거나 플레이어를 밀지 않는지 확인한다.
7. Console에 오류가 없는지 확인한다.

투사체는 플레이어 중심에서 생성하고 발사자의 Collider만 충돌에서 제외한다. 벽 너머에 생성되는 것을 피하기 위해 발사 위치를 앞쪽으로 이동시키지 않는다.
현재 투사체는 발사자 이외의 일반 Collider 2D에 부딪히면 사라진다. 바닥에는 Collider를 추가하지 않는다.
적/피해/자동 연사/오브젝트 풀은 이번 범위에 포함하지 않았다.

이 안내는 수동 확인 절차이다. Unity 에디터에서 컴파일이나 Play 모드 실행을 직접 수행한 결과는 아니다.
