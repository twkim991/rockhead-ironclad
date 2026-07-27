# 돌머리 아이언클래드 상세 구현 계획

> 이 문서는 `v0.2.1`까지 사용한 “아이언클래드 카드 직접 교체” 구조의 기록이다. 현재 미출시 프로토타입은 원본 아이언클래드를 보존하고, 같은 시각·음향 리소스를 사용하는 별도 캐릭터 `Rockclad`와 전용 카드 모델 9개를 등록하는 구조로 전환되었다.

## 1. 문서 목적

이 문서는 *Slay the Spire 2* 아이언클래드의 기존 카드 5장을 `거대한 바위(Giant Rock)` 중심의 덱 타입으로 재설계하는 모드 **돌머리 아이언클래드**의 구현 명세다.

이 문서에서 확정하는 범위는 다음과 같다.

- 기존 카드 ID를 유지한 채 카드명, 설명, 효과, 강화 효과를 교체한다.
- 타격 카드와 같은 방식으로 화면에 노출되지 않는 `CardTag.Rock`을 추가한다.
- `거대한 바위` 생성, 비용 감소, 피해 증가, 사용 시 방어도, 누적 사용량 기반 방어도를 구현한다.
- 한국어와 영문 로컬라이징을 제공한다.
- 싱글플레이와 멀티플레이에서 결정론적으로 동작하도록 구현한다.
- 카드 일러스트 5종과 Power 소형·확대 아이콘 4쌍은 제공된 이미지를 적용한다.

현재 범위에서 제외하는 항목은 다음과 같다.

- 신규 캐릭터 또는 신규 카드 풀 추가
- 원시의 힘 자체의 효과 변경
- 카드 초상화의 추가 수정·대체
- 전용 VFX, SFX, 애니메이션 제작
- Steam Workshop 배포

## 2. 목표 플레이 경험

모드의 핵심 플레이 경험은 다음 한 문장으로 정의한다.

> 바위를 만들고, 싸게 만들고, 계속 던지면 공격과 방어가 함께 커지는 단순한 아이언클래드 덱.

각 카드의 역할은 다음과 같다.

```text
바위 강타 / 바위의 형상
        ↓
거대한 바위 공급
        ↓
바위의 형상: 비용 감소
절대적인 바위: 피해 증가
바위 갑옷: 사용할 때 방어도
바위케이드: 누적 사용량에 비례한 턴 종료 방어도
```

복잡한 조건, 선택지, 자원 변환은 추가하지 않는다. 덱의 최적 행동이 의도적으로 “바위를 더 많이 던진다”에 수렴하도록 한다.

## 3. 기준 환경과 의존성

최초 구현 기준은 현재 로컬에 설치된 게임 버전으로 고정한다.

| 항목 | 기준 |
|---|---|
| 게임 | Slay the Spire 2 |
| 최초 지원 버전 | `v0.107.1` |
| 런타임 | .NET 9 |
| 엔진/패키징 | MegaDot 또는 정확히 호환되는 Godot .NET |
| 모드 기반 | 게임 기본 Mod API와 자체 Harmony 패치 |
| 런타임 패치 | Harmony |
| 프로젝트 템플릿 | Alchyr StS2 Content Mod Template |
| 모드 ID | `ThrowRockIronclad` |
| 표시명 | `돌머리 아이언클래드` |

매니페스트는 최소한 다음 조건을 만족해야 한다.

```json
{
  "id": "ThrowRockIronclad",
  "name": "돌머리 아이언클래드",
  "has_pck": true,
  "has_dll": true,
  "min_game_version": "0.107.1",
  "dependencies": [],
  "affects_gameplay": true
}
```

외부 라이브러리 모드에 의존하지 않는다. Harmony와 게임 어셈블리는 게임이 제공하는 파일을 참조하며 배포물에 복사하지 않는다.

## 4. 확인된 바닐라 동작

현재 `v0.107.1` 기준으로 확인된 관련 바닐라 데이터는 다음과 같다.

### 4.1 거대한 바위

| 속성 | 일반 | 강화 |
|---|---:|---:|
| 비용 | 1 | 1 |
| 피해 | 16 | 20 |
| 타입 | 공격 | 공격 |
| 희귀도 | 토큰 | 토큰 |

바닐라 클래스는 `MegaCrit.Sts2.Core.Models.Cards.GiantRock`이다.

### 4.2 교체 대상

| 기존 카드 | 클래스 | 기존 기본 비용 | 변경 기본 비용 |
|---|---|---:|---:|
| 바리케이드 | `Barricade` | 3 | 3 |
| 악마의 형상 | `DemonForm` | 3 | 3 |
| 돌 갑옷 | `StoneArmor` | 1 | 1 |
| 절대적인 힘 | `Juggernaut` | 2 | 2 |
| 몸통 박치기 | `BodySlam` | 1 | 1 |

모든 카드의 기본 비용이 새 설계와 이미 일치한다. 따라서 생성자나 기본 비용을 패치하지 않는다. 각 카드의 `OnPlay`, 동적 변수, 강화 처리, 툴팁, 로컬라이징만 교체한다.

### 4.3 타격 시너지 구현 방식

바닐라의 지옥검무와 완벽한 타격은 로컬라이징상 “이름에 타격이 포함된 카드”라고 설명하지만, 실제 판정은 다음 숨은 태그를 사용한다.

```csharp
card.Tags.Contains(CardTag.Strike)
```

바위 시너지 역시 동일한 구조를 사용한다. 표시 이름은 플레이어가 이해하는 계약이고, 실제 판정은 언어에 독립적인 숨은 태그를 사용한다.

## 5. 확정 용어와 카드 사양

### 5.1 카드명

| 바닐라 ID | 한국어 | 영문 |
|---|---|---|
| `BARRICADE` | 바위케이드 | Rockade |
| `DEMON_FORM` | 바위의 형상 | Rock Form |
| `STONE_ARMOR` | 바위 갑옷 | Rock Armor |
| `JUGGERNAUT` | 절대적인 바위 | Absolute Rock |
| `BODY_SLAM` | 바위 강타 | Rock Slam |
| `GIANT_ROCK` | 거대한 바위 | Giant Rock |

영문명은 모두 `Rock`을 포함한다. 한국어 이름은 모두 `바위`를 포함한다.

### 5.2 바위케이드 / Rockade

| 속성 | 값 |
|---|---|
| 카드 타입 | Power |
| 희귀도 | Rare |
| 비용 | 3 |
| 강화 | 방어도 계수 2 → 3 |

한국어 일반:

> 내 턴 종료 시, 이번 전투에서 사용한 거대한 바위 1장당 방어도를 2 얻습니다.

한국어 강화:

> 내 턴 종료 시, 이번 전투에서 사용한 거대한 바위 1장당 방어도를 3 얻습니다.

영문 일반:

> At the end of your turn, gain 2 Block for each Giant Rock played this combat.

영문 강화:

> At the end of your turn, gain 3 Block for each Giant Rock played this combat.

판정 규칙:

- Power를 사용하기 전에 던진 거대한 바위도 계산한다.
- `CardPlayFinishedEntry`가 생성된 완료된 카드 사용만 계산한다.
- 자동 사용과 추가 사용은 각각 별도 사용으로 계산한다.
- 취소되거나 완료되지 않은 카드 사용은 계산하지 않는다.
- Power 소유자 본인이 사용한 거대한 바위만 계산한다.
- 다른 `Rock` 카드 사용은 계산하지 않는다.
- 여러 장 사용하면 계수가 합산된다.
  - 일반 2장: 바위 1장당 방어도 4
  - 일반 1장 + 강화 1장: 바위 1장당 방어도 5

### 5.3 바위의 형상 / Rock Form

| 속성 | 값 |
|---|---|
| 카드 타입 | Power |
| 희귀도 | Rare |
| 비용 | 3 |
| 강화 | 매 턴 생성하는 카드가 거대한 바위 → 거대한 바위+ |

한국어 일반:

> 내 턴 시작 시, 거대한 바위 1장을 손에 추가합니다. 바위 카드의 비용이 1 감소합니다.

한국어 강화:

> 내 턴 시작 시, 거대한 바위+ 1장을 손에 추가합니다. 바위 카드의 비용이 1 감소합니다.

영문 일반:

> At the start of your turn, add a Giant Rock into your Hand. Rock cards cost 1 less.

영문 강화:

> At the start of your turn, add a Giant Rock+ into your Hand. Rock cards cost 1 less.

판정 규칙:

- 비용 감소는 매 턴 누적되지 않는다.
- Power가 유지되는 동안 적용되는 지속 비용 보정이다.
- Power 한 장당 모든 `Rock` 태그 카드의 비용이 1 감소한다.
- 여러 장 사용하면 비용 감소가 중첩된다.
- 최종 비용은 0 미만이 되지 않는다.
- 비용 감소는 Power를 사용한 직후부터 적용된다.
- 생성 효과는 다음 플레이어 턴 시작부터 발동한다.
- Power 소유자의 카드에만 적용한다.
- 일반판과 강화판을 함께 사용하면 매 턴 일반 거대한 바위와 거대한 바위+를 각각 생성한다.
- 손이 가득 찬 경우 게임의 표준 카드 생성/손 초과 처리 규칙을 따른다.

### 5.4 바위 갑옷 / Rock Armor

| 속성 | 값 |
|---|---|
| 카드 타입 | Power |
| 희귀도 | Uncommon |
| 비용 | 1 |
| 강화 | 방어도 4 → 6 |

한국어 일반:

> 거대한 바위를 사용할 때마다, 방어도를 4 얻습니다.

한국어 강화:

> 거대한 바위를 사용할 때마다, 방어도를 6 얻습니다.

영문 일반:

> Whenever you play a Giant Rock, gain 4 Block.

영문 강화:

> Whenever you play a Giant Rock, gain 6 Block.

판정 규칙:

- `AfterCardPlayed`에서 완료된 `GiantRock` 사용에만 발동한다.
- 자동 사용과 추가 사용에도 발동한다.
- `Rock` 태그만 있고 `GiantRock`이 아닌 카드에는 발동하지 않는다.
- Power 소유자가 사용한 거대한 바위에만 발동한다.
- 여러 장 사용하면 방어도 수치가 합산된다.

### 5.5 절대적인 바위 / Absolute Rock

| 속성 | 값 |
|---|---|
| 카드 타입 | Power |
| 희귀도 | Rare |
| 비용 | 2 |
| 강화 | 비용 2 → 1 |

한국어:

> 거대한 바위가 주는 피해가 6 증가합니다.

영문:

> Giant Rocks deal 6 additional damage.

판정 규칙:

- `ModifyDamageAdditive`에서 `cardSource`가 `GiantRock`일 때 +6을 적용한다.
- Power 소유자가 사용한 거대한 바위 피해에만 적용한다.
- 힘, 약화, 취약 등 기존 피해 계산 체계와 함께 적용한다.
- 여러 장 사용하면 +6씩 합산된다.
- 거대한 바위 이외의 `Rock` 카드 피해에는 적용하지 않는다.
- 카드 미리보기의 예상 피해량에도 반영되는지 통합 테스트한다.

예상 기본 피해:

| 상태 | 거대한 바위 | 거대한 바위+ |
|---|---:|---:|
| Power 없음 | 16 | 20 |
| 절대적인 바위 1장 | 22 | 26 |
| 절대적인 바위 2장 | 28 | 32 |

### 5.6 바위 강타 / Rock Slam

| 속성 | 값 |
|---|---|
| 카드 타입 | Attack |
| 희귀도 | Common |
| 비용 | 1 |
| 피해 | 5 |
| 키워드 | Exhaust |
| 강화 | 비용 1 → 0 |

한국어:

> 피해를 5 줍니다. 거대한 바위 1장을 버린 카드 더미에 추가합니다. 소멸.

영문:

> Deal 5 damage. Add a Giant Rock into your Discard Pile. Exhaust.

판정 규칙:

- 먼저 지정한 적에게 피해를 5 준다.
- 이후 강화되지 않은 거대한 바위 1장을 소유자의 버린 카드 더미에 생성한다.
- 생성된 카드는 표준 전투 생성 카드로 취급한다.
- 바위 강타 자신은 `Rock` 태그를 가지므로 바위의 형상의 비용 감소를 받는다.
- 바위 강타 사용은 바위 갑옷과 바위케이드의 트리거로 계산하지 않는다.
- 카드 사용 후 바위 강타는 소멸한다.

## 6. 바위 분류 모델

### 6.1 숨은 CardTag

화면에 노출되는 `CardKeyword`는 만들지 않는다. 숨은 `CardTag` 값을 모드에 고정하고 대상 카드의 `Tags` getter에 추가한다.

```csharp
public static class RockTags
{
    public const int RockValue = 1_059_034_496;
    public static readonly CardTag Rock = (CardTag)RockValue;
}
```

`RockValue`는 이전 버전이 생성하던 결정론적 값을 그대로 유지하여 기존 멀티플레이 데이터와의 호환성을 보존한다.

### 6.2 태그 대상

다음 카드 타입에 `Rock` 태그를 부여한다.

```text
Barricade
DemonForm
StoneArmor
Juggernaut
BodySlam
GiantRock
```

`PrimalForce`는 이름에 바위가 포함되지 않고 바위를 생성하는 카드이므로 `Rock` 태그를 부여하지 않는다.

### 6.3 이름과 태그 계약

사용자에게는 타격 카드와 동일하게 “이름에 바위가 포함된 카드”로 설명한다. 내부 판정은 항상 다음과 같이 수행한다.

```csharp
card.Tags.Contains(RockTags.Rock)
```

다음 구현은 금지한다.

```csharp
card.Title.Contains("바위")
card.Title.Contains("Rock")
```

금지 이유:

- 현재 언어에 따라 이름이 달라진다.
- 다른 모드가 표시 이름을 변경할 수 있다.
- 로컬라이징 로드 시점에 따라 결과가 달라질 수 있다.
- 멀티플레이 클라이언트의 언어가 다르면 동기화 문제가 생길 수 있다.

### 6.4 태그 주입 전략

한 곳에서 대상 타입을 관리한다.

```csharp
public static class RockCardRegistry
{
    private static readonly HashSet<Type> RockCardTypes =
    [
        typeof(Barricade),
        typeof(DemonForm),
        typeof(StoneArmor),
        typeof(Juggernaut),
        typeof(BodySlam),
        typeof(GiantRock),
    ];

    public static bool ShouldHaveRockTag(CardModel card)
        => RockCardTypes.Contains(card.GetType());
}
```

`CardModel.Tags` getter에 Harmony Postfix를 적용한다. 기존 태그 집합은 보존하고 `Rock`만 추가한다. 반환값이 `HashSet<CardTag>`이면 캐시된 집합에 직접 추가하고, 다른 enumerable이면 새로운 집합으로 변환한다.

패치는 다음 조건을 만족해야 한다.

- 기존 `Strike`, `Defend`, 기타 태그를 삭제하지 않는다.
- 동일 태그를 중복 추가하지 않는다.
- 대상 카드가 아닌 카드에는 할당과 변환을 수행하지 않는다.
- 다른 모드의 Postfix 결과를 보존한다.

## 7. 구현 아키텍처

권장 프로젝트 구조는 다음과 같다.

```text
throw_rock_ironclad/
├─ ThrowRockIronclad.sln
├─ Directory.Build.props
├─ src/
│  ├─ ThrowRockIronclad.GameMod/
│  │  ├─ ThrowRockIronclad.csproj
│  │  ├─ ThrowRockIronclad.json
│  │  ├─ project.godot
│  │  ├─ export_presets.cfg
│  │  ├─ Code/
│  │  │  ├─ Cards/
│  │  │  ├─ Compatibility/
│  │  │  ├─ Core/
│  │  │  ├─ Patches/
│  │  │  ├─ Powers/
│  │  │  ├─ Relics/
│  │  │  └─ Utilities/
│  │  └─ ThrowRockIronclad/
│  │     ├─ images/
│  │     └─ localization/
│  └─ ThrowRockIronclad.Loader/
│     ├─ ThrowRockIronclad.Loader.csproj
│     └─ LoaderBootstrap.cs
├─ tests/
│  └─ ThrowRockIronclad.SmokeTests/
├─ docs/
├─ scripts/
│  ├─ setup/
│  ├─ build/
│  └─ release/
├─ build/
│  └─ msbuild/
├─ .artifacts/                 # 로컬 패키지와 Workshop 작업 폴더, Git 제외
└─ .workspace/                 # SDK·게임 버전별 참조·로그·임시 파일, Git 제외
```

## 8. Power 구현 상세

기존 바닐라 Power를 변경하지 않는다. 카드의 `OnPlay`를 패치하여 모드 전용 Power를 적용한다.

| 카드 | 적용 Power | 기존 Power 사용 여부 |
|---|---|---|
| 바위케이드 | `RockadePower` | 사용하지 않음 |
| 바위의 형상 | `RockFormPower` | 사용하지 않음 |
| 바위 갑옷 | `RockArmorPower` | 사용하지 않음 |
| 절대적인 바위 | `AbsoluteRockPower` | 사용하지 않음 |
| 바위 강타 | 없음 | 해당 없음 |

### 8.1 공통 Power 기반 클래스

`ThrowRockIroncladPower`는 게임의 `PowerModel`을 직접 상속한다. 모드 자체 Harmony 패치가 기존과 같은 `THROWROCKIRONCLAD-` ID 접두사와 아이콘 경로를 공급한다. 공통 책임은 다음과 같다.

- 모드 전용 로컬라이징 ID 구성
- Power 아이콘 경로 구성
- Power 타입과 중첩 정책 명시
- 누락된 아이콘에 대한 공용 fallback 제공

각 Power는 다음 전용 파일을 사용한다. 원본 1254×1254 정사각형 이미지는 구도 변경 없이 각각 64×64와 256×256으로 축소한다. 공용 `power.png`는 전용 파일이 누락됐을 때만 fallback으로 사용한다.

```text
images/powers/rockade_power.png              64x64
images/powers/rock_form_power.png            64x64
images/powers/rock_armor_power.png           64x64
images/powers/absolute_rock_power.png         64x64
images/powers/big/rockade_power.png          256x256
images/powers/big/rock_form_power.png        256x256
images/powers/big/rock_armor_power.png       256x256
images/powers/big/absolute_rock_power.png    256x256
```

### 8.2 RockadePower

권장 데이터:

```text
Type: Buff
StackType: Counter
Amount: 바위 1장당 얻는 방어도
```

카드는 일반일 때 Amount 2, 강화일 때 Amount 3을 적용한다. Power 중첩은 Amount 합산으로 처리한다.

턴 종료 훅은 `BeforeSideTurnEnd` 또는 동등한 플레이어 턴 종료 훅을 사용한다. 다음을 순서대로 처리한다.

1. 현재 종료되는 side/participants에 Power 소유자가 포함되는지 검사한다.
2. `GiantRockHistory.CountFinishedPlaysThisCombat(owner)`를 호출한다.
3. `count * Amount`가 0보다 크면 방어도를 얻는다.
4. Power Flash와 표준 방어도 명령을 실행한다.

별도의 누적 카운터를 Power 안에 저장하지 않는다. 전투 도중 Power를 얻어도 과거 사용량을 포함해야 하므로 전투 기록을 원본 데이터로 사용한다.

### 8.3 RockFormPower

권장 데이터:

```text
Type: Buff
StackType: Counter
Amount: 비용 감소량이자 Rock Form 적용 횟수
InternalData.normalSources: 일반판 생성원 수
InternalData.upgradedSources: 강화판 생성원 수
```

카드는 일반/강화 모두 Power Amount 1을 적용한다. `AfterApplied`에서 `cardSource.IsUpgraded`를 확인하여 내부 생성원 수를 갱신한다.

턴 시작 훅은 `AfterSideTurnStart`를 사용한다.

1. participants에 Power 소유자가 포함되는지 검사한다.
2. `normalSources`만큼 일반 `GiantRock`을 손에 생성한다.
3. `upgradedSources`만큼 강화 `GiantRock`을 손에 생성한다.
4. 생성 순서는 모든 클라이언트에서 동일해야 한다.

비용 수정은 `TryModifyEnergyCostInCombat`을 사용한다.

```csharp
if (card.Owner == Owner.Player && card.Tags.Contains(RockTags.Rock))
{
    modifiedCost = Math.Max(0, originalCost - Amount);
    return true;
}
```

실제 API의 Owner 타입에 맞추어 비교식을 조정하되, 반드시 Power 소유자의 카드에만 적용한다.

내부 데이터 복제와 네트워크 동기화가 게임의 Power cloning 규칙에서 정상적으로 처리되는지 기술 스파이크에서 검증한다. 문제가 있으면 일반 생성원과 강화 생성원을 서로 다른 숨은 Power로 분리하는 대안을 사용한다.

### 8.4 RockArmorPower

권장 데이터:

```text
Type: Buff
StackType: Counter
Amount: 바위 사용 시 얻는 방어도
```

카드는 일반일 때 Amount 4, 강화일 때 Amount 6을 적용한다.

`AfterCardPlayed`에서 다음 조건을 모두 만족할 때 방어도를 얻는다.

```text
cardPlay.Card is GiantRock
cardPlay.Card.Owner.Creature == Power.Owner
```

태그만 검사하지 않는다. 이 효과는 모든 Rock 카드가 아니라 `GiantRock` 전용이다.

### 8.5 AbsoluteRockPower

권장 데이터:

```text
Type: Buff
StackType: Counter
Amount: 거대한 바위 추가 피해
```

카드는 Amount 6을 적용한다. 강화 여부는 Power 수치가 아니라 카드 비용에만 영향을 준다.

`ModifyDamageAdditive`에서 다음을 확인한다.

```text
cardSource is GiantRock
dealer == Power.Owner
```

조건을 만족하면 `amount + Amount`가 아니라 hook 계약에 맞는 additive 값 `Amount`를 반환한다. 실제 메서드의 반환 계약은 구현 직전 바닐라 예제를 다시 확인한다.

## 9. 전투 기록 계산

`바위케이드`의 원본 데이터는 게임의 `CombatHistory.CardPlaysFinished`다.

`GiantRockHistory`는 다음 단일 책임을 가진다.

```csharp
public static int CountFinishedPlaysThisCombat(Creature owner, ICombatState combatState)
```

개념적 필터는 다음과 같다.

```csharp
combatState.History.CardPlaysFinished.Count(entry =>
    entry.CardPlay.Card is GiantRock &&
    entry.CardPlay.Card.Owner.Creature == owner);
```

구현 시 확인할 항목:

- 실제 `ICombatState`의 CombatHistory 접근 프로퍼티 이름
- `CardPlay.Card.Owner`와 Power 소유자 비교 방법
- 플레이어가 사망하거나 교체되는 특수 상황
- 자동 사용/복제 사용이 각각 `CardPlayFinishedEntry`를 생성하는지
- 리플레이 또는 네트워크 복구 시 history가 동일하게 복원되는지

전투 기록을 사용할 수 없는 테스트 환경에서는 순수 함수로 필터 로직을 분리하여 단위 테스트한다.

## 10. 카드 패치 상세

### 10.1 공통 원칙

- 카드 클래스는 sealed이므로 상속으로 교체하지 않는다.
- 기존 카드 ID와 타입은 유지한다.
- Harmony Prefix로 기존 `OnPlay`를 건너뛰고 새 Task를 반환한다.
- 기존 강화 로직을 건너뛰고 새 강화 로직만 실행한다.
- 기존 동적 변수와 새 설명이 일치하도록 CanonicalVars를 교체한다.
- 원본 효과의 잘못된 HoverTip을 반드시 제거한다.
- 생성자는 기본 비용이 이미 일치하므로 패치하지 않는다.

### 10.2 BarricadePatch

교체 대상:

- `CanonicalVars`: 방어도 계수 2인 Block 관련 DynamicVar
- `OnPlay`: `RockadePower`를 DynamicVar 값만큼 적용
- `OnUpgrade`: 계수 +1
- `ExtraHoverTips`: Block만 유지
- 기존 강화의 비용 −1은 실행하지 않음

검증:

- 일반 비용 3 유지
- 강화 비용도 3 유지
- 카드 미리보기에 2 → 3 차이가 표시됨
- 기존 “방어도가 사라지지 않음” 효과가 완전히 제거됨

### 10.3 DemonFormPatch

교체 대상:

- `CanonicalVars`: 기존 StrengthPower 변수 제거
- `OnPlay`: `RockFormPower` Amount 1 적용, 카드 강화 상태 전달
- `OnUpgrade`: 기존 힘 +1 로직을 실행하지 않음
- `ExtraHoverTips`: Strength Power 팁 제거, GiantRock 카드 팁 추가

검증:

- 일반/강화 비용 모두 3
- 강화 미리보기에서 생성 카드만 `GiantRock+`로 변경
- 기존 힘 증가가 발생하지 않음

### 10.4 StoneArmorPatch

교체 대상:

- `CanonicalVars`: Block 값 4
- `OnPlay`: `RockArmorPower`를 Block 값만큼 적용
- `OnUpgrade`: Block +2
- `ExtraHoverTips`: Plating 제거, Block 유지, 필요하면 GiantRock 카드 팁 추가

검증:

- 기존 판금 Power가 적용되지 않음
- 일반/강화가 각각 Power Amount 4/6 적용

### 10.5 JuggernautPatch

교체 대상:

- `CanonicalVars`: 추가 피해 6
- `OnPlay`: `AbsoluteRockPower` Amount 6 적용
- `OnUpgrade`: 비용 −1
- `ExtraHoverTips`: 기존 Block 팁 제거, GiantRock 카드 팁 추가

검증:

- 일반 비용 2, 강화 비용 1
- 강화해도 추가 피해는 6으로 유지
- 기존 방어도 획득 시 무작위 피해 효과가 발생하지 않음

### 10.6 BodySlamPatch

교체 대상:

- `CanonicalVars`: Damage 5
- `CanonicalKeywords`: Exhaust 추가
- `OnPlay`:
  1. 대상에게 Damage 5 공격
  2. 일반 GiantRock을 소유자의 버린 카드 더미에 생성
- `OnUpgrade`: 비용 −1
- `ExtraHoverTips`: 기존 Block 팁 제거, GiantRock 카드 팁 추가

검증:

- 일반 비용 1, 강화 비용 0
- 현재 방어도와 무관하게 피해 5
- 생성 바위는 강화되지 않음
- 생성 후 바위 강타는 소멸
- 생성 자체는 바위 사용으로 계산되지 않음

## 11. 로컬라이징 계획

지원 언어는 초기 구현에서 한국어와 영어로 제한한다.

```text
ThrowRockIronclad/localization/kor/cards.json
ThrowRockIronclad/localization/kor/powers.json
ThrowRockIronclad/localization/eng/cards.json
ThrowRockIronclad/localization/eng/powers.json
```

`card_keywords.json`은 만들지 않는다. Rock은 표시 키워드가 아닌 숨은 CardTag다.

바닐라 로컬라이징 키를 PCK에서 직접 덮어쓰지 않는다. 대상 카드의 Title/Description이 모드 전용 namespaced 키를 가리키도록 패치한다. 이를 통해 다른 모드와의 전역 로컬라이징 충돌을 줄인다.

권장 논리 키:

```text
THROW_ROCK_IRONCLAD_CARD_ROCKADE.title
THROW_ROCK_IRONCLAD_CARD_ROCKADE.description
THROW_ROCK_IRONCLAD_CARD_ROCK_FORM.title
THROW_ROCK_IRONCLAD_CARD_ROCK_FORM.description
THROW_ROCK_IRONCLAD_CARD_ROCK_ARMOR.title
THROW_ROCK_IRONCLAD_CARD_ROCK_ARMOR.description
THROW_ROCK_IRONCLAD_CARD_ABSOLUTE_ROCK.title
THROW_ROCK_IRONCLAD_CARD_ABSOLUTE_ROCK.description
THROW_ROCK_IRONCLAD_CARD_ROCK_SLAM.title
THROW_ROCK_IRONCLAD_CARD_ROCK_SLAM.description
```

Power 키도 같은 방식으로 namespaced 처리한다.

동적 변수 표기는 최종 코드에서 사용하는 변수 이름과 정확히 일치해야 한다. 예:

```text
{Block:diff()}
{Damage:diff()}
{ExtraDamage:diff()}
```

일반 모드 분석기는 유지하되, 외부 라이브러리의 ID 접두사를 전제로 하는 Power 진단은 비활성화한다. 네 Power의 ID·로컬라이징·아이콘은 스모크 테스트와 Model DB 초기화 후 런타임 진단으로 검증한다.

## 12. 이미지 연기 전략

교체 카드 5종은 모드 전용 초상화를 사용한다. 제공된 1448×1086 원본을 내용 변경 없이 카드 초상화 비율로 중앙 크롭하고 1000×760 PNG로 저장한다.

| 카드 | 파일 |
|---|---|
| 바위 강타 | `images/card_portraits/rock_slam.png` |
| 절대적인 바위 | `images/card_portraits/absolute_rock.png` |
| 바위의 형상 | `images/card_portraits/rock_form.png` |
| 바위케이드 | `images/card_portraits/rockade.png` |
| 바위 갑옷 | `images/card_portraits/rock_armor.png` |

기존 카드 ID를 유지하므로 `CardModel.PortraitPath`와 `PortraitPngPath`를 Harmony로 교체한다. 다른 카드의 초상화 경로는 변경하지 않는다.

각 Power는 `IconFileName`으로 전용 소형·확대 아이콘 한 쌍을 선택한다. 네 Power의 파일명이 서로 다른지도 런타임 진단으로 검증한다.

모드 자체 Harmony 패치가 커스텀 Power의 아이콘 경로를 교체한다. 경로 교체만으로 확대 아이콘이 게임의 run asset set에 자동 추가되지는 않으므로, Rock 카드의 `RunAssetPaths`에 전용 아이콘 8개를 모두 추가하여 `PreloadManager.Cache`가 전투 전에 로드하도록 한다. 런타임 검증은 리소스 존재 여부뿐 아니라 확대 아이콘의 캐시 적재 여부도 확인한다.

## 13. 구현 단계

### 단계 0. 프로젝트 골격과 기준 실행

작업:

- Content Mod 템플릿으로 프로젝트 생성
- 프로젝트와 solution을 같은 디렉터리에 배치
- 게임 및 MegaDot 경로 설정
- 외부 모드 의존성이 없는 매니페스트 구성
- 매니페스트 작성
- `.workspace/references/`, 빌드 결과물, 로컬 경로 설정 파일을 `.gitignore`에 추가
- 빈 DLL/PCK를 Publish하여 게임 Mod Settings에서 로드 확인

완료 조건:

- 모드가 오류 없이 목록에 표시된다.
- 모드 활성화 후 메인 메뉴 진입이 가능하다.
- 로그에 assembly와 PCK 로드 성공이 남는다.

### 단계 1. Rock 태그 기반 구축

작업:

- `RockTags.Rock` 정의
- `RockCardRegistry` 정의
- `CardModel.Tags` Postfix 구현
- 개발용 콘솔/로그로 대상 카드 태그 확인
- 이름과 태그 계약 단위 테스트 작성

완료 조건:

- 대상 6종만 Rock 태그를 가진다.
- 기존 태그는 보존된다.
- 언어를 한국어/영어로 바꾸어도 태그 결과가 같다.

### 단계 2. 바위 강타 수직 기능 구현

첫 번째 카드로 바위 강타를 구현한다. 공격, 생성, 버린 카드 더미, 소멸, 강화, 로컬라이징을 한 번에 검증할 수 있기 때문이다.

작업:

- DynamicVar 교체
- OnPlay 교체
- Exhaust 추가
- OnUpgrade 교체
- 툴팁 교체
- 한국어/영문 카드명과 설명 연결

완료 조건:

- 일반/강화 비용과 피해가 정확하다.
- 거대한 바위가 버린 카드 더미에 생성된다.
- 바위 강타가 소멸한다.
- 도감, 덱, 전투에서 새 이름과 설명이 표시된다.

### 단계 3. 절대적인 바위 구현

작업:

- `AbsoluteRockPower` 구현
- Juggernaut OnPlay/OnUpgrade/Vars/Tip 교체
- GiantRock 피해 additive hook 구현
- 중첩과 피해 계산 테스트

완료 조건:

- 거대한 바위만 +6 피해를 받는다.
- 일반 공격과 다른 Rock 카드 피해는 바뀌지 않는다.
- 여러 Power가 정상 중첩된다.

### 단계 4. 바위 갑옷 구현

작업:

- `RockArmorPower` 구현
- StoneArmor OnPlay/OnUpgrade/Vars/Tip 교체
- GiantRock 사용 후 방어도 획득 훅 구현

완료 조건:

- 일반/강화가 4/6 방어도를 제공한다.
- 자동 사용과 추가 사용에도 1회씩 발동한다.
- 바위 강타에는 발동하지 않는다.

### 단계 5. 바위의 형상 구현

작업:

- `RockFormPower`와 내부 생성원 데이터 구현
- DemonForm OnPlay/OnUpgrade/Vars/Tip 교체
- 턴 시작 카드 생성 구현
- Rock 카드 비용 감소 구현
- 일반/강화 혼합 중첩 테스트

완료 조건:

- 한 장당 비용이 1 감소하며 0 아래로 내려가지 않는다.
- 일반/강화 생성원이 구분된다.
- 멀티플레이에서 각 플레이어 카드에만 적용된다.

### 단계 6. 바위케이드 구현

작업:

- `GiantRockHistory` 구현
- `RockadePower` 구현
- Barricade OnPlay/OnUpgrade/Vars/Tip 교체
- 과거 사용량, 자동 사용, 중첩 테스트

완료 조건:

- Power 사용 전의 바위 사용량까지 계산한다.
- Power 소유자의 완료된 GiantRock 플레이만 계산한다.
- 일반/강화 혼합 중첩이 정확하다.

### 단계 7. 프레젠테이션 정리

작업:

- 다섯 카드의 한국어/영문 텍스트 확정
- 네 Power의 한국어/영문 텍스트 작성
- 잘못 남은 바닐라 HoverTip 제거
- Power별 전용 소형·확대 아이콘 연결
- 카드 초상화 5종을 모드 전용 경로로 교체

완료 조건:

- 힘, 판금, 기존 바리케이드 등 삭제된 효과의 툴팁이 남지 않는다.
- 모든 숫자가 강화 미리보기에 정확히 표시된다.
- 누락된 텍스트 키나 이미지 오류가 로그에 없다.

### 단계 8. 통합 QA와 패키징

작업:

- 전체 수동 테스트 체크리스트 실행
- 2인 멀티플레이 테스트
- 저장 후 재실행 테스트
- Release Publish 실행
- DLL/PCK/JSON 패키지 검증
- 호환 버전과 알려진 충돌 문서화

완료 조건:

- Definition of Done 전 항목 통과
- 치명적 로그 오류 없음
- 같은 모드 버전의 멀티플레이에서 desync 없음

### 완료 단계. 최종 Power 이미지

- 제공된 원본 4종을 소형 64×64와 확대 256×256으로 변환
- Power별 전용 경로 연결
- 전용 아이콘 8개의 run asset 사전 로딩
- 카드 상세 보기와 전투 UI에서 시각 검수

## 14. 테스트 계획

### 14.1 단위 테스트

`RockCardRegistryTests`

- 대상 6종은 true
- 다른 아이언클래드 카드는 false
- `PrimalForce`는 false

`RockRulesTests`

- Rock 태그 보유 여부 판정
- GiantRock 전용 판정과 Rock 일반 판정 구분
- 비용 감소가 0 아래로 내려가지 않음

`RockLocalizationContractTests`

- Rock 태그 카드의 한국어 이름에 `바위` 포함
- Rock 태그 카드의 영문 이름에 `Rock` 포함
- 한국어/영문 카드 및 Power 키 누락 없음
- 설명 DynamicVar 이름과 코드 변수 일치

`PowerCalculationTests`

- Rockade 계수 합산
- RockArmor 계수 합산
- AbsoluteRock 피해 합산
- RockForm 비용 감소와 생성원 분리

### 14.2 수동 전투 테스트

#### 태그와 비용

- 바위의 형상 1장 후 모든 바위 카드 비용 −1
- 바위의 형상 2장 후 모든 바위 카드 비용 −2
- 비용은 0 아래로 표시되지 않음
- 다른 카드는 비용 변화 없음
- Power 소유자가 아닌 플레이어 카드는 비용 변화 없음

#### 바위의 형상 생성

- 일반 1장: 턴마다 GiantRock 1장
- 강화 1장: 턴마다 GiantRock+ 1장
- 일반 1장 + 강화 1장: 턴마다 각각 1장
- 일반 2장: 턴마다 일반 GiantRock 2장
- 손이 가득 찼을 때 표준 동작 확인

#### 절대적인 바위 피해

- Power 없음: 16/20
- Power 1장: 22/26
- Power 2장: 28/32
- 힘, 약화, 취약과 조합 결과 확인
- 카드 미리보기와 실제 피해 일치

#### 바위 갑옷

- 일반 1장: GiantRock 1회당 방어도 4
- 강화 1장: GiantRock 1회당 방어도 6
- 일반+강화: GiantRock 1회당 방어도 10
- 바위 강타에는 발동하지 않음
- 자동 사용 GiantRock에는 발동

#### 바위케이드

- Power 전에 GiantRock 2회, Power 후 턴 종료: 일반은 방어도 4
- 강화 Power 전에 GiantRock 3회: 방어도 9
- 일반+강화 Power, GiantRock 3회: 방어도 15
- 다음 턴 GiantRock을 더 사용하면 전체 전투 누적량 기준으로 증가
- 다른 플레이어의 GiantRock은 계산하지 않음

#### 바위 강타

- 일반: 비용 1, 피해 5, 버린 카드 더미에 일반 GiantRock, 소멸
- 강화: 비용 0, 나머지 동일
- 생성된 GiantRock은 바위케이드 횟수에 즉시 포함되지 않음
- 생성된 GiantRock을 실제로 사용한 뒤부터 포함됨

### 14.3 UI 테스트

- 카드 도감에서 이름과 설명 표시
- 카드 보상에서 이름과 설명 표시
- 덱 보기에서 이름과 설명 표시
- 강화 미리보기의 숫자/비용 강조
- Power 툴팁의 이름, 설명, Amount 표시
- 삭제된 바닐라 툴팁이 남지 않음
- 한국어와 영어 전환 후 모두 정상 표시

### 14.4 저장과 멀티플레이

- 교체 대상 카드가 포함된 런 저장 후 재실행
- 카드 ID가 유지되어 Deprecated Card로 바뀌지 않음
- 2인 멀티플레이에서 양쪽이 같은 Power 상태와 비용을 봄
- 각 플레이어의 GiantRock 사용량이 섞이지 않음
- 자동 사용/추가 사용 횟수가 양쪽에서 동일함
- 모드 버전 불일치가 정상적으로 참가를 막는지 확인

## 15. 호환성과 위험 관리

### 15.1 게임 업데이트

얼리 액세스 업데이트로 다음 항목이 바뀌면 패치가 깨질 수 있다.

- 대상 카드 메서드 서명
- `CardModel.Tags` 구현
- Power hook 이름 또는 호출 순서
- CombatHistory 구조
- 로컬라이징 ID와 DynamicVar API

대응:

- `SupportedGameVersion`에서 개발 기준 버전을 로그에 표시한다.
- Harmony 대상 메서드가 없으면 조용히 무시하지 않고 명확한 오류를 기록한다.
- 카드별 패치를 분리하여 깨진 범위를 즉시 찾을 수 있게 한다.
- 게임 업데이트 후 먼저 빈 모드 로드, 고정 태그 값, Power ID, 카드 1장 순서로 회귀 테스트한다.

### 15.2 다른 카드 리워크 모드

같은 카드의 `OnPlay`, DynamicVars, 로컬라이징을 변경하는 모드와는 본질적으로 충돌 가능성이 있다.

대응:

- 시작 로그에 다섯 카드 패치 성공 여부를 출력한다.
- Harmony patch owner 정보를 진단 로그에서 확인 가능하게 한다.
- 원본 카드 리워크 모드와의 완전 호환을 초기 목표로 삼지 않는다.
- 초상화는 namespaced 경로를 사용하지만, 같은 카드의 `PortraitPath`를 바꾸는 아트 모드와는 패치 순서에 따라 충돌할 수 있다.

### 15.3 멀티플레이 결정론

금지 사항:

- 클라이언트 언어에 의존한 카드명 문자열 판정
- 로컬 시간 또는 비결정적 컬렉션 순서 사용
- Power 소유자를 확인하지 않는 전역 카드 카운트
- 네트워크 상태 밖에만 존재하는 전투 카운터를 게임플레이 판정의 원본으로 사용

권장 사항:

- 태그 판정 사용
- CombatHistory 사용
- 게임의 표준 카드 생성 명령 사용
- 게임 RNG가 필요하다면 RunState의 지정된 RNG 사용

### 15.4 원본 리소스

`.workspace/references/`는 분석용으로만 사용한다.

- 빌드에 포함하지 않는다.
- Git에 커밋하지 않는다.
- 배포 파일에 포함하지 않는다.
- 원본 PCK나 게임 설치 파일을 직접 수정하지 않는다.

## 16. Definition of Done

다음 조건을 모두 만족해야 첫 플레이 가능 버전을 완료로 본다.

- 모드가 `v0.107.1`에서 오류 없이 로드된다.
- 기존 다섯 카드 ID가 유지된다.
- 다섯 카드가 새 이름, 설명, 효과, 강화 효과를 사용한다.
- 대상 6종만 숨은 Rock 태그를 가진다.
- 원시의 힘이 만든 GiantRock이 모든 GiantRock 전용 시너지와 정상 상호작용한다.
- 바위의 형상의 비용 감소가 정적이고 중첩 가능하며 0 아래로 내려가지 않는다.
- 바위의 형상 일반/강화 혼합 생성이 정확하다.
- 바위 갑옷은 GiantRock 사용에만 발동한다.
- 절대적인 바위는 GiantRock 피해에만 적용된다.
- 바위케이드는 Power 획득 전을 포함한 전체 전투 완료 사용량을 계산한다.
- 바위 강타가 피해, 생성, 소멸, 강화를 정확히 수행한다.
- 삭제된 바닐라 효과와 툴팁이 남지 않는다.
- 한국어와 영어가 모두 정상 표시된다.
- 다섯 카드가 제공된 모드 전용 초상화를 표시한다.
- Power별 전용 소형·확대 아이콘으로 모든 Power UI를 확인할 수 있다.
- 저장 후 재실행해도 카드가 보존된다.
- 같은 모드 버전의 2인 멀티플레이에서 desync가 없다.
- Publish 결과물에 DLL, PCK, JSON이 포함된다.

## 17. 구현 착수 순서 요약

```text
1. 템플릿과 빌드 환경
2. Rock CardTag
3. 바위 강타
4. 절대적인 바위
5. 바위 갑옷
6. 바위의 형상
7. 바위케이드
8. 로컬라이징/툴팁 정리
9. 통합 및 멀티플레이 QA
10. 후속 이미지 제작
```

이 순서는 단순한 효과에서 시작해 피해 수정, 사용 훅, 비용/생성, 전투 기록 순으로 복잡도를 높인다. 각 단계가 독립적으로 검증 가능하므로 게임 업데이트나 API 차이가 발견되더라도 실패 범위를 작게 유지할 수 있다.
