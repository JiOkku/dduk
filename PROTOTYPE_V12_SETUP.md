# 12차 프로토타입: 마석 테크트리와 영구 성장

기존 마석 잔액, 보스 최초 처치 기록, 안전한 JSON 저장, 로비 마법 장착과 스테이지 선택을 재사용했다. 새 마법이나 보스 패턴은 추가하지 않았다.

## 사용 방법

로비 오른쪽 아래 **Magic Tech Tree / Permanent Upgrades** 버튼으로 메뉴를 연다. Fire / Water / Grass 탭에서 노드를 선택해 상세 정보와 선행 조건을 보고 해금한다. Unlocked 노드는 기존 마법 장착 기능을 통해 Equip Scroll로 장착할 수 있다. Stat Upgrades 탭에서 체력·마나·근접 피해를 강화한다. Close로 닫고 기존 입구에서 던전에 입장한다.

메뉴를 연 동안 로비 이동과 E키 입장/스테이지 선택을 잠시 막고, 닫으면 이전 이동 활성 상태를 복구한다. 구매는 로비에서만 가능하다. 던전의 무기 교체/마법/마나 회복/웨이브/보스/반출 규칙은 유지한다.

기존 테스트에서 전체 마법을 이미 받은 저장은 그대로 유지한다. 이 경우 해당 노드는 Unlocked이며 재구매할 수 없다. LobbyMagicUI의 **Unlock All For Testing** 자동 실행 옵션만 0으로 껐다. 기존 마법을 삭제하거나 저장을 초기화하지 않았다. 신규 저장에서는 1티어 3개만 기본 보유한다.

## 생성한 파일

- `Assets/Scripts/Rewards/PermanentGrowthData2D.cs`: 테크트리 노드, 강화 정의, 공통 성장 SO, 검증/호환 규칙, 별도 영구 보너스 계산. 노드 해금 상태는 SO를 수정하지 않고 영구 프로필에서 판정한다.
- `Assets/Scripts/Rewards/LobbyGrowthUI2D.cs`: 로비 성장 메뉴, 속성 탭/선행 관계/노드 상세/해금/장착, 영구 강화와 구매 결과 표시.
- `Assets/Scripts/Player/PlayerPermanentStats2D.cs`: 새 던전의 Awake에서 저장된 강화 레벨을 읽어 이번 플레이의 보너스를 한 번 계산한다.
- `Assets/Settings/PermanentGrowth.asset`: 기존 마법 ID 9개와 강화 3종의 비용/증가량.
- 위 Unity 파일들의 `.meta`.
- `Tests/Standalone/PermanentGrowthTests.cs`: 실제 구매/저장/성장 코드의 핵심 검사 8개. 파일 I/O는 임시 경로만 사용한다.
- `Backups/Lobby.before-v12.unity.txt`, `Backups/SampleScene.before-v12.unity.txt`: 연결 수정 전 씬 백업.
- 이 문서.

## 수정한 기존 파일

| 파일 | 변경 내용 |
|---|---|
| `Assets/Scripts/Rewards/RewardData2D.cs` | 해금 노드 ID 목록과 강화 레벨 3개를 영구 프로필에 추가 |
| `Assets/Scripts/Rewards/RewardSaveStore2D.cs` | 최신 저장을 읽어 잔액/선행/중복/최대 레벨을 검사한 뒤, 비용과 구매 상태를 한 번에 저장. 기존 보유 마법과 노드 기록 보완 |
| `Assets/Scripts/Rewards/RewardBank2D.cs` | 기존 마석으로 구매하는 API와 저장 성공 후 UI 갱신 이벤트. 로비 밖 구매 차단 |
| `Assets/Scripts/Flow/SceneFlow2D.cs` | 현재 로비에서 조작 가능한지 확인하는 InLobby 속성 추가. 시작 스테이지 전달/재시작 로직 유지 |
| `Assets/Scripts/Flow/LobbyController2D.cs` | 성장 메뉴와 던전 입장 확인창의 입력 충돌 방지, 이동 복구 |
| `Assets/Scripts/Magic/LobbyMagicUI2D.cs` | 구매 후 보유 스크롤 목록 갱신, 기존 보유/노드 기록 일치, 기존 장착 경로 재사용 |
| `Assets/Scripts/Rewards/LobbyRewardsUI2D.cs` | 구매 성공 후 마석 잔액 갱신. 성장 메뉴 진입 버튼 공간에 맞춰 보상 목록 높이 조정 |
| `Assets/Scripts/Player/PlayerHealth2D.cs` | 기본 HP와 별도 영구 보너스를 합친 최대 HP로 초기화 |
| `Assets/Scripts/Player/PlayerMana2D.cs` | 기본 최대 마나와 별도 영구 보너스를 합산. 기존 DungeonSession의 시작 시 최대치 충전 유지 |
| `Assets/Scripts/Player/PlayerMelee2D.cs` | 실제 최종 근접 피해에 영구 피해 보너스를 추가. 무기 데이터/랜덤 옵션/충전 시간은 수정하지 않음 |
| `Assets/Scripts/UI/GameHUD2D.cs` | 장착 무기 표시 옆에 영구 Melee +보너스 표시 |
| `Assets/Editor/PrototypeCombatAudit.cs` | 기존 구성 검사 메뉴에서 성장 데이터 검증과 강화 최대 레벨/증가량 확인 추가 |
| `Assets/Scenes/Lobby.unity` | 성장 UI와 기존 마법 UI에 같은 성장 데이터 연결, 자동 전체 해금 해제 |
| `Assets/Scenes/SampleScene.unity` | 기존 Player에 영구 보너스 컴포넌트와 성장 데이터 연결 |
| `Tests/Standalone/WeaponPartTests.cs` | 저장 검사에서 로비 여부를 제어하는 공통 SceneFlow 대역 추가 |
| `PROTOTYPE_V11_SETUP.md` | 후속 성장 단계 문서 안내 |

## 테크트리 데이터 / Inspector

**Assets/Settings/PermanentGrowth.asset**에서 관리한다. 노드 ID와 실제 마법 ID는 별개다.

| 속성 | 노드 ID | 실제 마법 ID | 현재 마법 | 마석 비용 | 선행 노드 |
|---|---|---|---|---|---|
| Fire | node_fire_1 | fire_bolt | Fire Bolt | 0, 기본 보유 | 없음 |
| Fire | node_fire_2 | explosive_fireball | Flame Spray | 5 | node_fire_1 |
| Fire | node_fire_3 | flame_blast | Greater Fireball | 10 | node_fire_2 |
| Water | node_water_1 | water_bolt | Water Bolt | 0, 기본 보유 | 없음 |
| Water | node_water_2 | piercing_stream | Piercing Stream | 5 | node_water_1 |
| Water | node_water_3 | water_wave | Water Wave | 10 | node_water_2 |
| Grass | node_grass_1 | thorn_bolt | Thorn Bolt | 0, 기본 보유 | 없음 |
| Grass | node_grass_2 | thorn_burst | Thorn Burst | 5 | node_grass_1 |
| Grass | node_grass_3 | thorn_nova | Thorn Nova | 10 | node_grass_2 |

불 2·3티어는 최근 합의한 화염분사/강화 폭발 화염탄을 유지한다. 역사적인 마법 ID와 기존 에셋 GUID는 바꾸지 않았다.

각 노드의 **Node Id / Magic Id / Display Name / Description / Cost / Prerequisites**를 설정할 수 있다. UI는 실제 마법 에셋의 이름/티어/소모 마나/쿨타임/공격 형태/설명을 함께 표시한다. Locked는 선행 조건 미충족, Available은 선행 조건 충족, Unlocked는 보유/해금 완료다. Available이어도 잔액이 부족하면 해금 버튼은 비활성화된다.

현재 3속성×3티어 UI를 사용하며, 신규 마법/속성 추가 화면은 이번 범위가 아니다. 노드/마법 ID 중복, 없는 마법·선행 노드, 순환 참조, 음수 비용은 성장 데이터 검증에서 차단한다.

## 강화 데이터와 전투 적용

같은 성장 에셋의 **Upgrades**에서 Kind, Display Name, Bonus Per Level, Costs를 조절한다. 비용 배열의 길이가 최대 레벨이다.

| 강화 | 레벨당 보너스 | 기본 최대 레벨 | 레벨별 비용 |
|---|---|---|---|
| Health | 최대 HP +1 | 5 | 2 / 4 / 6 / 8 / 10 |
| Mana | 최대 마나 +10 | 5 | 2 / 4 / 6 / 8 / 10 |
| MeleeDamage | 최종 근접 피해 +1 | 3 | 5 / 10 / 15 |

기존 기본값 HP 10 / 마나 100을 유지했다. 모두 최대 강화하면 HP 15 / 마나 150 / 근접 추가 피해 +3이다. 새 던전의 PlayerPermanentStats2D가 먼저 초기화되고 PlayerHealth/PlayerMana는 기본값과 보너스의 합을 최대치로 사용한다. 던전 시작 시 HP와 마나는 강화된 최대치로 시작한다.

근접 피해 보너스는 무기의 랜덤 공격력, 충전 피해, 창 끝 피해를 판정한 **이후** 더한다. 몽둥이 풀충전의 별도 충격파에도 근접 보너스를 더하지만 충격파의 마나 회복 없음 규칙은 유지한다. 마법 공격력·마나 소비·쿨타임·마나 회복량은 바꾸지 않았다. 비교 UI의 무기 수치는 개체 자체의 수치이며 영구 보너스로 원본을 덮어쓰지 않는다. HUD의 Melee +표시가 별도 보너스다.

무기를 교체하거나 버렸다 주워도 플레이어 보너스는 그대로다. 같은 씬의 스테이지 이동은 기존 HP/마나를 유지하고 회복시키지 않는다. 던전 재시작은 새 개체에서 기본값+저장 보너스를 다시 계산하므로 누적 적용하지 않는다. 향후 비용 배열을 줄이면 기존 저장 레벨은 삭제하지 않고 실제 적용 보너스만 현재 최대 레벨로 제한한다.

## 구매·저장·기존 데이터 호환

기존 `Application.persistentDataPath/prototype-rewards-v1.json`의 version 1 / SHA256 envelope / 임시 파일 검증 / 원자적 교체 / `.bak` / 손상 시 쓰기 중지 방식을 재사용한다. 이번 필드는 다음과 같다.

- `unlockedTechNodeIds`: 해금 노드 ID 목록.
- `healthUpgradeLevel`, `manaUpgradeLevel`, `meleeUpgradeLevel`: 강화 레벨.

이전 저장에 필드가 없으면 빈 목록/레벨 0으로 읽는다. 기존 보유 스크롤은 성장 화면에서 대응 노드가 해금된 것으로 보완하고, 이미 알려진 노드 기록이 있으면 보유 마법도 보완한다. 읽기만으로 기존 파일을 덮어쓰지 않으며 다음 구매/장착 저장 때 일치한 기록을 함께 쓴다. 알 수 없는 저장 노드 ID는 지우지 않고 보관하되 마법이나 구매 권한을 부여하지 않는다. 음수 강화 레벨, 중복/빈 노드 ID 등 잘못된 저장은 기존 손상 보존 정책에 따라 구매를 막는다.

구매는 최신 저장을 읽어 만든 사본에만 적용한다. 잔액 차감과 마법/노드 해금 또는 강화 레벨 증가를 한 파일로 저장하고 성공한 뒤에만 메모리 상태와 UI를 갱신한다. 실패하면 잔액·보유 마법·레벨과 기존 파일은 그대로다. 기존 마석 보유량, 최근 획득량, 보스 최초 처치 기록, 토큰, 반출 무기 개체 수치, 장착 마법, 정산 run ID를 보존한다.

이미 해금한 마법은 재구매를 막는다. 강화 요청은 화면에 표시했던 레벨과 최신 저장의 레벨을 비교해 오래된 중복 요청을 거부한다. UI도 연속 클릭에 0.35초 간격을 둔다. 성공한 구매마다 추가로 다음 레벨을 사려면 갱신된 화면과 비용을 확인해야 한다. 마석 부족과 MAX에서는 버튼을 비활성화한다.

사망, 중간 포기, 클리어 및 재시작은 이미 구매한 성장을 삭제하지 않는다. 보스 마석 반출 규칙은 기존 11차 그대로다. 성장 메뉴는 새 마석을 생성하는 기능이 아니며 구매 테스트를 위해 사용자 재화를 임의로 지급하거나 차감하지 않았다.

## 씬 연결 / 수동 설정

필수 수동 연결은 없다. 기존 씬 이름과 GUID, 플레이어/카메라/스테이지/보스 참조를 유지하며 다음만 연결했다.

1. Lobby의 GameUI → **LobbyGrowthUI2D.Growth Data**와 기존 **LobbyMagicUI2D.Growth Data**: PermanentGrowth.asset.
2. SampleScene의 Player → **PlayerPermanentStats2D.Growth Data**: 같은 PermanentGrowth.asset.
3. 기존 **LobbyMagicUI2D.Unlock All For Testing**: 꺼짐. 개발용 수동 해금 메뉴는 기존처럼 남아 있으나 사용자 저장을 초기화하지 않는다.

UI는 코드로 생성하므로 Canvas/버튼 OnClick을 수동으로 만들 필요가 없다. 성장 메뉴는 가로로 긴 Game 뷰에서도 잘리지 않도록 CanvasScaler Expand를 사용한다. 컴포넌트를 중복 추가하지 않는다.

## Unity에서 수동 확인할 항목

1. 로비에서 성장 메뉴를 열고 잔액, Fire/Water/Grass 탭, 선행 관계와 상세 정보를 확인한다. 기존 전체 해금 저장은 이미 보유한 노드가 Unlocked인 것이 정상이다.
2. 마석이 부족할 때 Need N stones와 비활성 버튼을 확인한다. 마석은 기존 보스/던전 클리어 경로로 획득한다. 기존 저장을 지울 필요는 없다.
3. 아직 미보유인 2티어가 있는 플레이에서는 5마석으로 구매하고 잔액 차감과 보유 목록 추가를 확인한다. 2티어를 사기 전에 같은 속성 3티어가 Locked인지 확인한다. 기존 전체 해금 상태의 저장은 이 경로가 이미 완료된 상태이므로 중복 구매가 차단된다. 신규 상태 구매 검사는 아래 독립 검사에서도 확인했다.
4. 새 마법을 Equip Scroll 또는 기존 보유 목록의 Equip으로 장착한 뒤 던전 우클릭이 해당 마법을 사용하는지 확인한다.
5. HP 강화를 1회 산 뒤 던전에 입장해 최대 HP 11, 현재 HP 11을 확인한다. Mana 1회면 최대/현재 110, Melee 1회면 무기 기본/랜덤 최종 피해에 +1이어야 한다.
6. 적 피격으로 HP를 줄이고 근접·마법을 사용해 본다. 무기 교체 후에도 Melee +1이 유지되고 드롭 개체 자체 수치는 바뀌지 않아야 한다. 몽둥이 충전 원과 마나 회복도 유지되어야 한다.
7. 스테이지 이동에서는 현재 HP/마나 유지, 던전 재시작에서는 강화된 최대치로 초기화되며 보너스가 두 번 더해지지 않는지 확인한다.
8. 사망 후 로비 복귀 시 레벨·마석·마법·보스 기록이 유지되는지 확인한다. 게임을 종료/재실행해도 복원되어야 한다.
9. 연속 클릭으로 동일 구매가 중복 차감되지 않는지, 최대 HP/Mana 레벨 5 및 Melee 레벨 3에서 MAX가 표시되는지 확인한다. 저장 실패 시 성공 안내가 표시되어서는 안 된다.

## 수행한 검사와 한계

- Unity 참조/컴파일러를 사용하는 런타임·에디터 조건부 코드 컴파일 통과.
- 관련 독립 시나리오 **8개** 통과: 선행/잔액/중복 해금, 잘못된 ID/순환/손상 저장 차단, 최대 레벨/오래된 강화 요청, 저장 실패와 재시도, 이전 저장 보완과 무기 수치 보존, 노드/보유 마법 일치, 원본 무기와 별도 보너스/재시작, 로비 전용 구매 및 갱신 이벤트.
- 검사는 실제 구매/저장 코드를 사용하고 파일 I/O는 별도 임시 경로에서 했다. Unity API와 JsonUtility는 검사 대역을 사용했으므로 실제 Unity 입력/물리/직렬화 전체 검증으로 간주하지 않는다. 무관한 전체 회귀/성능 검사는 수행하지 않았다.
- 실제 Unity Play 모드에서 로비 메뉴 열기, 마법 상세/탭 및 강화 레벨·비용 표시를 확인했다. 현재 잔액 부족 상태의 구매 버튼도 확인했다. 사용자 보유 마석으로 구매하거나 테스트 재화를 저장하지 않았고 확인 후 Play를 종료했다.
- UI 확인 중 가로로 긴 Game 뷰에서 진입 버튼이 잘리는 문제를 발견해 화면 하단 고정 위치와 Expand로 수정했다. 기존 보상 목록도 버튼 공간에 맞게 조정했다.
- 실제 구매 뒤 강화 수치가 적용된 던전 전투, 무기 교체/사망, 게임 앱 재실행까지는 직접 완료하지 않았다. 위 수동 절차가 남아 있다. 마지막 버튼 부족 잔액 문구와 불투명 배경 개선은 컴파일 검증 대상이다.
- 마법 자체 강화, 환불/초기화, 성장 소비 외 상점, 새 마법/몬스터/보스, 속성 상성은 구현하지 않았다.
