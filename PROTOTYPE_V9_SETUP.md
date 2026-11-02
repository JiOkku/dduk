# 9차 프로토타입: 마법 스크롤 장착과 사용

## 분석 및 기존 기능 재사용

Unity 6000.6.7f1, 기존 Lobby/SampleScene(던전), 새 Input System 우클릭, PlayerShooting2D/Projectile2D, 마나/적 공통 피해, 로비 UI, 원자 교체+체크섬 방식 보상 저장을 확인했다. 스크롤 데이터/보유/선택 기능은 없었다.

기존 우클릭 입력/조준/투사체 프리팹/적 피해를 유지하면서 데이터별 공격 형태로 확장했다. 무기·근접·마나 회복·웨이브·드롭·상자·반출·씬 전환은 같은 경로를 사용한다.

## 마법 데이터

MagicData2D는 ScriptableObject다. 고유 Id, Name, Description, Element, Tier, Mana Cost, Cooldown, Damage, Attack Form, Projectile Speed, Lifetime, Range, Area Radius, Projectile Count, Burst Interval, Effect Color, Projectile Sprite, Effect Material을 Inspector에서 조절한다.

Tier는 1~3 표시 정보이며 실제 피해/소모/쿨타임을 강제하지 않는다. Fire/Water/Grass는 데이터와 색만 구분하며 상성/저항은 없다. 원본 에셋은 런타임에 수정하지 않는다.

Assets/Settings/Magic:

| 한국어 이름 / UI 이름 | ID | 티어 | 마나 | 쿨타임 | 피해 | 형태 |
|---|---|---|---|---|---|---|
| 화염탄 / Fire Bolt | fire_bolt | 1 | 20 | 1초 | 1 | 단일 투사체 |
| 화염분사 / Flame Spray | explosive_fireball | 2 | 40 | 3초 | 2 | 전방 거리 3, 각도 60도 부채꼴 |
| 강화 폭발 화염탄 / Greater Fireball | flame_blast | 3 | 70 | 6초 | 4 | 투사체가 적/벽에 충돌하면 반경 2.5 폭발 |
| 물탄 / Water Bolt | water_bolt | 1 | 20 | 1초 | 1 | 단일 투사체 |
| 관통 물줄기 / Piercing Stream | piercing_stream | 2 | 40 | 3초 | 2 | 직선 관통 투사체 |
| 물의 파동 / Water Wave | water_wave | 3 | 70 | 6초 | 3 | 플레이어 중심 반경 3 |
| 가시탄 / Thorn Bolt | thorn_bolt | 1 | 20 | 1초 | 1 | 단일 투사체 |
| 연속 가시 / Thorn Burst | thorn_burst | 2 | 40 | 3초 | 각 1 | 0.12초 간격 3발 |
| 가시 폭발 / Thorn Nova | thorn_nova | 3 | 70 | 6초 | 각 2 | 8방향 방사형 |

기본 투사체 속도 12, 최대 이동 거리 6, 최대 수명 2초다. 거리/수명 중 먼저 끝나는 조건으로 제거한다. 수량/간격/거리/피해/반경은 에셋에서 변경 가능하다. 위 지정 외 수치는 이번 테스트용 기본값이다.

UI는 기존 내장 폰트를 유지해 영문 이름/설명을 사용한다. 각 에셋의 이름과 한국어 대응은 위 표와 같다.

MagicCatalog.asset은 9개 데이터 참조를 관리한다. 고유 ID 중복/빈 값/유효하지 않은 형태/기본 fire_bolt 누락을 검사한다. ID는 저장 키이므로 기존 ID를 임의로 바꾸지 않는다.

## 최초 보유와 로비 장착

최초 보유는 fire_bolt / water_bolt / thorn_bolt이고 기본 장착은 fire_bolt다. 잠긴 스크롤은 일반 보유 목록에 나타나지 않는다.

Lobby GameUI의 LobbyMagicUI2D가 왼쪽 아래 스크롤 목록을 생성한다. 이름/속성/티어/소모 마나/쿨타임/피해/설명과 Equip 또는 Equipped를 표시한다. 오른쪽 반출 보상 목록과 기존 로비 입장 확인창은 유지한다.

Equip은 로비 씬에서만 동작하고 저장 성공 후 선택 상태를 갱신한다. 실패하면 이전 선택과 파일을 유지한다. 던전에는 이 UI를 배치하지 않으며 PlayerShooting2D는 입장 시 선택한 마법만 캐시한다. 무기 교체는 마법을 변경하거나 쿨타임을 초기화하지 않는다.

개발 테스트 해금:

1. 로비 Play 중 GameUI → Lobby Magic UI 2D 컨텍스트 메뉴.
2. **Development/Unlock All Test Magic Scrolls** 선택.
3. 보유 9종을 저장하고 목록을 갱신한다. 자동으로 장착을 바꾸지는 않는다.

일반 화면에는 해금 버튼/키가 없다. UnlockAll 진입 메서드는 UNITY_EDITOR/DEVELOPMENT_BUILD에만 컴파일하며 일반 출시 빌드에는 없다. 이 해금은 저장되므로 이후 실행에도 9종이 보일 수 있다. 저장 삭제/초기화 UI는 추가하지 않았다.

## 시전·공격·충돌

- 우클릭 새 입력, 던전 씬, 생존/전투 가능, Playing 또는 WaveTransition, 충분한 마나, 쿨타임 0을 확인한다.
- 설정/마법/조준이 유효하지 않거나 마나 부족/쿨타임 중이면 소비와 타이머 변경이 없다.
- 성공 시 마나를 한 번 소비하고 쿨타임을 시작한 뒤 해당 공격을 생성한다.
- Burst는 총 40마나를 한 번만 소비한다. 첫 조준 방향을 유지해 각 발을 발사하며 사망/클리어/씬 전환 시 남은 발사를 중단한다. 이미 발사한 투사체는 기존 수명 동안 동작한다.
- Radial은 같은 위치에서 여러 발을 생성한다. 투사체끼리는 충돌을 무시해 생성 직후 서로 소멸하지 않는다. 플레이어 충돌 제외도 유지한다.
- Piercing은 적마다 한 번 피해를 주고 해당 적의 Collider를 무시해 계속 이동한다. 벽/상자 같은 고체 장애물에는 막힌다.
- 강화 폭발 화염탄은 직접 피해에 폭발 피해를 더하지 않고, 한 번의 범위 판정으로 피해를 준다.
- 불 2티어는 우클릭 한 번에 전방 부채꼴을 한 번 타격한다. 지속 피해 방식은 아니다. 벽이 피해를 차단하고 간단한 부채꼴 외곽선이 표시된다. `ExplosiveFireball.asset`의 Range와 Cone Angle로 범위를 조절한다.
- 불 3티어는 커서 위치에 즉시 폭발하지 않고 플레이어에서 발사되어 충돌 시 폭발한다. `FlameBlast.asset`의 Projectile Scale(기본 1.8), Projectile Speed, Area Radius로 크기·속도·폭발 반경을 조절한다.
- 저장 호환성을 위해 불 2·3티어의 기존 마법 ID, 에셋 파일명과 GUID는 유지했다. 씬 및 UI 재연결은 필요 없다. Play 모드에서 각 마법을 장착하고 전방 범위, 벽 차단, 투사체 충돌, 마나 및 쿨타임을 직접 확인한다. 컴파일과 독립 데이터 검사만 수행했으며 Unity Play 실행은 확인하지 않았다.
- 범위 공격은 적 HashSet으로 중복 피해를 제외하고 장애물 시야를 검사한다. 한 적에 여러 Collider가 있어도 범위당 한 번이다. 서로 다른 연속/방사 투사체는 개별 피해를 줄 수 있다.
- TargetArea는 마우스 위치를 Range 이내로 제한하고, 벽이 있으면 벽 앞 위치로 제한한다. 그 위치에 시전하므로 벽 너머를 클릭해도 앞쪽에 효과가 생기며 마나는 소비된다. 실제 피해도 벽을 통과하지 않는다.
- SelfArea/Radial은 마우스를 플레이어 중심에 두어도 사용할 수 있다. 방향이 필요한 투사체는 유효한 조준 방향이 필요하다.
- 마법 처치는 공통 적 피해/사망/골드/드롭으로 처리하고 마나를 회복하지 않는다. 근접의 타격/처치 회복은 수정하지 않았다.

기존 Projectile2D.Launch 메서드는 유지하고 데이터용 LaunchMagic을 추가했다. 기존 프리팹의 Rigidbody/Collider/모양도 유지했다. 색과 선택적 스프라이트는 생성된 투사체에만 적용한다.

범위 시각 효과는 색이 다른 원 윤곽이다. MagicEffect.mat을 데이터에 명시적으로 연결해 해당 URP Unlit 셰이더 참조를 포함했다. 고급 애니메이션/상성/마법 강화는 없다.

## 저장 확장과 기존 보상 보존

동일한 prototype-rewards-v1.json 및 기존 envelope/체크섬/백업/원자 교체를 사용한다. payload 버전 1에 다음 선택 필드를 추가했다.

- ownedMagicIds: 보유 ID 목록.
- equippedMagicId: 장착 ID.

8차 파일에서 필드가 없으면 메모리에 기본 3종/화염탄을 보완한다. 읽기만으로 원본 파일을 덮어쓰지 않는다. 다음 장착/해금/보상 저장 때 새 필드를 포함하며 재화/무기/정산 던전 ID는 보존한다.

보유 목록의 알 수 없는 ID는 향후 복원을 위해 지우지 않지만 UI/시전에 사용하지 않는다. 알 수 없거나 미보유인 장착 ID는 카탈로그의 화염탄으로 대체한다. 잘못된 카탈로그는 별도로 연결한 Fallback Magic을 사용한다. 기본 마법조차 없으면 오류를 표시하고 시전을 중지해 마나 소비를 막는다.

장착 변경은 재화를 소비하지 않는다. 보상 정산이 기존 장착/보유 정보를 덮어쓰지 않도록 같은 최신 프로필에서 변경한다. 사망/클리어는 장착 ID를 변경하지 않는다. 별도 저장 슬롯/장비 인벤토리/스크롤 드롭은 추가하지 않았다.

## Inspector 연결 (반영 완료)

| 위치 | 필드 | 연결 |
|---|---|---|
| Lobby → GameUI → Lobby Magic UI 2D | Catalog | Assets/Settings/Magic/MagicCatalog.asset |
| Dungeon(SampleScene) → Player Shooting 2D | Magic Catalog | 동일 카탈로그 |
| 동일 | Fallback Magic | FireBolt.asset |
| 동일 | Input Actions / Aim Camera / Projectile Prefab | 기존 참조 유지 |
| 마법 데이터 9개 | Projectile Sprite | 기존 원형 투사체 스프라이트 |
| 동일 | Effect Material | MagicEffect.mat |

수동 연결은 필요 없다. 외부 씬 변경 알림에는 Play를 끄고 Reload한다. 마법 수치는 해당 에셋을 선택해 조절한다. 기존 MagicAttackSettings2D 파일은 남겨 두었지만 현재 시전 수치는 더 이상 그 인라인 설정에서 읽지 않는다.

기존 기본 마법 30마나/2초는 이번 지시서에 맞춰 초기 화염탄 20마나/1초로 바뀐다. 속성과 티어가 자동으로 수치를 결정하지 않는다.

## Unity Play 확인

1. 로비에서 기본 보유 3종과 Fire Bolt 장착을 확인한다. 이미 개발 해금한 저장이라면 보유가 더 많을 수 있다.
2. Water Bolt/Thorn Bolt Equip을 눌러 장착 표시와 입장 전 장비 정보가 갱신되는지 확인한다.
3. 던전 입장 후 선택한 이름/속성/티어/Cost/마나/쿨타임을 확인하고 우클릭한다.
4. 쿨타임 중 추가 클릭은 투사체/추가 소비가 없어야 한다. 마나가 소모량 미만일 때도 타이머/마나가 바뀌면 안 된다.
5. 근접으로 마나를 회복하고 다시 시전되는지 확인한다. 무기 E 교체로 마법 쿨타임이 바뀌지 않아야 한다.
6. 개발 메뉴로 나머지 해금 후 각각 장착/입장해 위 표의 형태를 확인한다. 특히 폭발 중복 피해, 벽 차단, 관통 다중 적중, 가시 3발의 단일 소비, 방사형 8발의 상호 충돌 제외를 확인한다.
7. 사망/클리어 후 시전이 멈추고 연속 발사의 남은 발이 취소되는지 확인한다. 로비 복귀 후 선택은 유지되고 로비에서만 바꿀 수 있어야 한다.
8. 게임 종료/재실행 후 보유/장착, 기존 반출 재화/무기 능력치가 함께 복원되는지 확인한다.
9. 기존 8차 저장 사본으로 실행해 기본 마법 보완과 재화/무기/정산 ID 보존을 확인한다. 실제 사용자 원본은 별도 복사한 후 검증한다.
10. 없는 장착 ID를 가진 테스트 저장에서는 화염탄을 사용하고 오류/마나 소실이 없어야 한다. 체크섬이 있는 저장을 수동 편집하면 손상으로 처리되므로 테스트는 정상 envelope를 생성하는 도구/사본에서 한다.
11. 로비 이동/입장 확인/취소, 무기/웨이브/상자/보상 반출/재시작이 유지되고 Console 오류가 없는지 확인한다. Play 중 ScriptableObject 변경은 종료 후에도 남을 수 있으므로 테스트 수치를 되돌린다.

## 생성 파일

- Assets/Scripts/Magic/MagicData2D.cs — 개별 스크롤 SO.
- MagicCatalog2D.cs — ID 검증/기본 대체/보유 기반 선택 조회.
- MagicCombat2D.cs — 공통 범위 피해/장애물/임시 원 효과.
- LobbyMagicUI2D.cs — 보유 목록/로비 장착/개발 해금.
- 위 스크립트 및 Magic 폴더 .meta.
- Assets/Settings/Magic의 마법 9개 .asset, MagicCatalog.asset, MagicEffect.mat 및 .meta.
- Tests/Standalone/MagicDefinitionTests.cs.
- PROTOTYPE_V9_SETUP.md.

## 수정 파일

- PlayerShooting2D.cs — 기존 우클릭/조준을 데이터 시전으로 연결, 던전/진행 조건, 연속/방사/범위 처리.
- Projectile2D.cs — 기존 이동/충돌 확장, 관통/폭발, 데이터별 속도/거리/수명/색, 투사체 간 충돌 제외.
- GameHUD2D.cs — 현재 마법/소모/상태 표시 및 행 간격 조절.
- LobbyController2D.cs — 선택한 마법 이름 반영, 스크롤 영역과 안내 위치 조절.
- RewardData2D.cs — 선택적 보유/장착 필드 및 구형 기본값 보완.
- RewardSaveStore2D.cs — 동일 안전 저장 경로 재사용, 보유/장착 저장, 기존 보상 보존.
- RewardBank2D.cs — 저장 진입점 추가.
- Lobby.unity / SampleScene.unity — 카탈로그/UI/기본 마법 연결.
- Tests/Standalone/WeaponPartTests.cs / RewardPersistenceTests.cs — 테스트 대역 확장 및 마법 저장 호환 검증.
- PROTOTYPE_V8_SETUP.md — 후속 단계 안내.

## 검증 결과와 제한

- 설치된 Unity 참조 어셈블리/Roslyn으로 전체 런타임 및 에디터 코드 별도 컴파일 통과.
- 씬 로컬 참조, 고유 에셋 GUID, 카탈로그 연결, 마법 ID 9개와 시각 효과 참조 검사 통과.
- 보상/마법 저장 핵심 테스트 13개 통과. 실제 임시 파일 I/O로 구형 envelope 읽기/보상 보존, 기본값 보완, 해금/장착 재실행 복원, 잠긴 ID 거부, 저장 실패 시 이전 선택/파일 유지, 보상 정산 후 선택 유지 등을 검증했다.
- 카탈로그/소유/기본값 테스트 5개 통과. 알 수 없는 ID 기본 대체, 미보유 선택 거부, 중복 ID 검사, 티어별 수치 강제 없음 포함.
- 위 독립 테스트는 Unity JSON API/Unity 객체를 .NET 대역으로 대체한다. 실제 Unity JsonUtility, 시전 자원/물리/코루틴/UI/씬 왕복/게임 재실행을 실행한 테스트가 아니다.
- 실제 Application.persistentDataPath 저장은 도구로 수정하지 않았다. 테스트는 임시 폴더에만 썼다.
- 일반 릴리스 컴파일 결과에 개발 UnlockAll 메서드가 없는 것을 확인했다.
- Unity 에디터 임포트/Play 모드는 직접 실행하지 않았다. 9종의 실제 공격과 UI는 위 수동 확인이 필요하다.
- 상성/저항/테크트리/마석 소비/마법 강화/다중 슬롯/던전 마법 교체/스크롤 드롭/보스는 미구현이며 이번 제외 범위다.
