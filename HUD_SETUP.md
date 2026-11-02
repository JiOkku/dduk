# 화면 체력 표시와 재시작

## Unity 설정

1. Play 모드를 끈다.
2. Hierarchy 빈 곳 우클릭 → Create Empty. 이름을 GameUI로 지정한다. Player나 Enemy의 자식이 아닌 씬 최상위 오브젝트로 만든다.
3. GameUI에 Add Component → GameHUD2D를 추가한다.
4. Game HUD 2D의 Player Health 칸으로 Hierarchy의 Player를 끌어 넣는다. Player에는 기존 PlayerHealth2D가 있어야 한다.
5. GameHUD2D는 씬에 하나만 추가한다. 직접 Canvas, Text, Button을 만들 필요는 없다.
6. 씬을 Ctrl+S로 저장한다. 실행 중 생성되는 HUD Canvas와 HUD EventSystem은 Play 종료 시 자동 제거된다.

현재 씬 SampleScene은 EditorBuildSettings의 활성 씬 목록에 등록되어 있다. 다른 씬에서 사용한다면 File → Build Profiles → Scene List에서 현재 씬이 포함되어 있고 체크되어 있는지 확인한다. 사용 중인 Build Profile이 별도 Scene List를 덮어쓰는 경우 그 목록에도 포함해야 한다.

## UI 입력

씬에 EventSystem이 없으면 새 Input System용 InputSystemUIInputModule을 포함한 EventSystem을 실행 시 자동 생성한다. 기존 EventSystem이 있으면 재사용하며 임의 변경하지 않는다.

기존 EventSystem이 있는데 버튼이 클릭되지 않으면:

1. Play를 끄고 Hierarchy의 EventSystem을 선택한다.
2. Standalone Input Module(기존 입력 시스템용)이 있다면 제거한다.
3. Input System UI Input Module을 추가한다.
4. 액션이 비어 있다면 Inspector의 Assign Default Actions를 누른다.
5. 활성 EventSystem이 하나인지, Canvas와 EventSystem을 직접 중복 생성하지 않았는지 확인한다.

## 화면과 확인 절차

- 왼쪽 위: HP: 3 / 3. 피격에 맞춰 감소한다.
- 체력 0: 반투명 배경 위 Game Over와 Restart 버튼 표시. 기존 이동/공격 정지는 유지된다.
- Restart 클릭: 현재 씬을 다시 로드한다. 체력, 플레이어 위치, 적과 투사체가 저장된 씬 초기 상태로 돌아온다. Play 모드는 유지된다.
- 씬 재로드는 저장된 상태를 사용하므로 에디터에서 배치한 변경 사항을 Play 전에 저장한다.
- 재시작 후 HP: 3 / 3으로 복구되고 적이 다시 추적하며 이동/공격이 가능한지 확인한다. 두 번 이상 사망/재시작하여 UI가 중복 생성되지 않는지도 확인한다.
- 가로/세로 Game 창 크기를 바꿔 체력 표시와 중앙 버튼이 보이는지 확인한다.

기본 내장 폰트로 바로 표시할 수 있도록 UI 문구는 영어를 사용한다. TMP 폰트 임포트는 필요 없다. 전체 게임 시간은 멈추지 않으므로 사망 화면 뒤에서도 적과 기존 투사체는 계속 동작한다.

## 파일

- 추가: Assets/Scripts/UI/GameHUD2D.cs
- 수정: Assets/Scripts/Player/PlayerHealth2D.cs (MaxHealth 읽기 속성, 사망 로그 갱신)
- 추가: HUD_SETUP.md

프로젝트 설정, 입력 액션, 씬 파일은 수정하지 않았다. Unity 컴파일 및 Play 모드 실행을 직접 확인하지 않았으므로 위 절차로 확인해야 한다.
