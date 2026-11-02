# 첫 번째 2D 탑뷰 프로토타입

## 확인한 프로젝트 환경

- Unity: 6000.6.7f1 (`ProjectSettings/ProjectVersion.txt`).
- Input System: 1.20.1. `activeInputHandler: 1`로 새 Input System만 사용.
- `Assets/Settings/InputSystem_Actions.inputactions`에 `Player/Move` Vector2 액션과 WASD 바인딩이 이미 존재. 그대로 재사용하며 액션 파일은 수정하지 않음.
- 기존 폴더: `Assets/Scenes`(SampleScene), `Assets/Settings`(입력, URP 2D 설정과 씬 템플릿), `Assets/Welcome`(템플릿 안내 자료).
- 추가 폴더: `Assets/Scripts/Player`, `Assets/Scripts/Camera`.
- 기존 씬, 패키지, 프로젝트 설정, VS Code 설정은 수정하지 않음.

## Unity 에디터 설정

Play 모드가 아닌 상태에서 진행한다. 스크립트 임포트가 끝나고 Console에 컴파일 오류가 없는지 먼저 확인한다.

1. `Assets/Scenes/SampleScene.unity`를 연다.
2. Hierarchy에서 우클릭 → **2D Object → Sprites → Square**로 사각형을 생성하고 이름을 `Player`로 바꾼다.
   - Transform: Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`.
   - Sprite Renderer의 Color를 밝은 노랑 등 배경과 구분되는 색으로 지정한다. 기존 Global Light 2D는 유지한다.
3. Player에 **Box Collider 2D**를 추가한다. `Is Trigger`는 끈다. 크기는 기본 사각형에 맞는 `(1, 1)`을 사용한다.
4. Player에 **Player Movement 2D** 스크립트를 추가한다. Rigidbody 2D도 자동 추가된다.
   - `Move Speed`: **5** (초당 Unity 월드 단위). Inspector에서 원하는 값으로 조절한다.
   - `Input Actions`: Project 창의 `Assets/Settings/InputSystem_Actions.inputactions`를 끌어 넣는다.
   - 별도 Player Input 컴포넌트나 C# 입력 래퍼 생성은 필요 없다.
5. Player의 **Rigidbody 2D** 설정을 확인한다. 스크립트를 처음 붙이면 Reset에서 아래 값을 설정하지만, 기존 컴포넌트를 재사용했다면 직접 확인한다.
   - Body Type: **Dynamic**, Simulated: 켬.
   - Gravity Scale: **0** (탑뷰이므로 아래로 떨어지지 않음).
   - Linear Damping: **0**.
   - Constraints: **Freeze Rotation Z** 켬, Freeze Position X/Y 끔.
   - Interpolate: **Interpolate**.
   - Collision Detection은 이 속도의 초기 프로토타입에서는 기본 Discrete를 유지한다.
6. 기존 **Main Camera**에 **Camera Follow 2D** 스크립트를 추가한다.
   - `Target`: Hierarchy의 Player를 끌어 넣는다.
   - `Offset`: `(0, 0, -10)`.
   - 카메라 Projection: **Orthographic**, Size: **5**.
   - 카메라 Rotation: `(0, 0, 0)`. 기존 Camera/URP 컴포넌트는 유지한다.
7. 씬을 저장하고 Play를 누른 뒤 **Game 창을 클릭**하여 키보드 포커스를 준다.

## 구현 방식

- PlayerMovement2D는 기존 Player/Move 액션을 복제하여 자체 활성화/비활성화/해제한다. 공유 입력 액션의 활성 상태는 변경하지 않는다. 기존 액션에 포함된 방향키/게임패드 바인딩도 그대로 사용 가능하다.
- Update에서 입력을 읽고 길이를 최대 1로 제한한다. W+D 같은 대각선도 직선과 같은 최대 속도로 이동하며, 아날로그 입력의 작은 크기는 유지한다.
- FixedUpdate에서 Rigidbody2D.linearVelocity를 설정한다. Transform을 직접 움직이지 않아 2D 충돌 계산에 참여한다. 속도에 deltaTime을 곱하지 않는다.
- CameraFollow2D는 LateUpdate에서 보간된 플레이어 위치와 Offset을 사용한다. 플레이어는 XY 평면, 카메라는 Z=-10에 둔다.
- 이동 스크립트를 비활성화하면 입력을 끄고 속도를 0으로 초기화한다. 포커스를 잃으면 입력값도 0으로 처리한다.

## 직접 확인할 항목

아래는 에디터에서 수행할 수동 확인 절차이며, 실행 테스트 완료 보고가 아니다.

1. W/A/S/D 각각과 두 키 조합으로 8방향 이동하는지 확인한다. 키를 떼면 멈춰야 한다.
2. Inspector의 Player Transform으로 월드 좌표 변화를 확인한다. Move Speed=5에서 같은 시간 이동할 때 직선과 대각선의 이동 거리 크기가 같아야 한다. 대각선의 각 축 속도는 약 3.536이다.
3. 플레이어가 화면 중앙에 유지되고 Main Camera의 X/Y 좌표가 Player를 따라가며 Z는 -10을 유지하는지 확인한다. 빈 배경에서는 이동이 눈에 잘 드러나지 않으므로 Transform 좌표로 확인한다.
4. Play 중 Move Speed를 2나 8로 바꾸어 속도가 달라지는지 확인한다. Play 중 변경은 종료하면 되돌아가므로 최종 값은 Play 종료 후 저장한다.
5. Console에 오류가 없는지, 중력으로 떨어지거나 회전하지 않는지 확인한다.
6. 충돌도 확인하려면 임시 Square를 Player 옆에 두고 Box Collider 2D만 붙인다(Rigidbody 2D 없음). 플레이어가 통과하지 않는지 확인한 뒤 임시 오브젝트는 삭제한다.

이 작업에서는 Unity 에디터에서 컴파일 및 Play 모드 실행을 직접 수행하지 않았다. 실제 입력, 렌더링, 카메라 보간과 충돌 동작은 위 절차로 확인해야 한다.
