# 호환성 및 개발 기준

## 지원 기준

- Slay the Spire 2: `v0.107.1`
- 외부 모드 의존성: 없음
- .NET SDK: `9.0`
- Godot SDK: `4.5.1`

모드는 시작 시 실행 중인 게임 버전과 지원 버전을 로그에 남긴다. `ModelDb.AllCharacters`, 캐릭터 에셋 경로, 카드 태그와 시대 진행 메서드가 사라지면 초기화 단계에서 명시적으로 실패한다. Model DB 초기화 후에는 바위클래드 등록, 68장 카드 선택 풀, 시작 전용 바위 강타, 유물 1개의 풀 격리, 불타는 혈액 시작 유물, 아이언클래드 에셋 재사용, Power·로컬라이징·저장 왕복을 검사한다.

## 알려진 충돌 범위

다음 대상을 변경하는 다른 모드와는 패치 순서에 따라 충돌할 수 있다.

- `ModelDb.AllCharacters` 캐릭터 목록
- `CharacterModel`의 에셋·음향 경로 getter
- `ProgressSaveManager`의 캐릭터 시대 진행 메서드
- CardModel.Tags getter

바위클래드 카드 13장은 별도 모델 ID를 사용하므로 바닐라 카드의 효과·이름·아트를 변경하는 모드와 직접 충돌하지 않는다. 일곱 Power는 모드 전용 PCK의 소형·확대 아이콘을 사용한다. 기존 다섯 Power는 서로 다른 아이콘을 사용하고, 신규 Power 둘은 프로토타입 단계에서 기존 바위 아이콘을 재사용한다. 전용 파일이 누락된 경우에만 공용 placeholder로 fallback한다.

## 바위클래드 프로토타입 제한

- 별도 캐릭터 시대가 없으므로 바위클래드의 액트·엘리트·보스 기반 시대 진행은 임시로 건너뛴다.
- 승리 업적은 아이언클래드 승리 업적을 재사용한다.
- 통계 화면과 카드 도감의 바위클래드 전용 분류는 아직 추가하지 않았다.
- 캐릭터 선택, 전투, 상점, 휴식처와 멀티플레이 표현은 아이언클래드 리소스를 참조한다.
- `v0.2.1` 이하의 진행 중 아이언클래드 저장은 교체 카드에 바닐라 ID를 사용하므로, 프로토타입에서 불러오면 해당 카드가 원래 아이언클래드 효과로 돌아간다. 저장 마이그레이션은 아직 제공하지 않는다.

## 결정론 및 상태 보존

- Rock 판정은 표시 이름이 아닌 사용자 정의 CardTag를 사용한다.
- Rockade는 `CombatHistory.CardPlaysFinished`를 원본으로 삼고 Power 소유자의 GiantRock만 센다.
- Rock Form의 일반/강화 생성원 수는 네트워크 전체 상태가 동기화하는 Power Amount 하나에 인코딩한다.
- 카드 생성은 `CreateCard<GiantRock>`과 `AddGeneratedCardToCombat`을 사용한다.
- Absolute Rock은 바닐라 additive 훅 계약대로 추가량만 반환한다.

## 배포 파일

정식·공개 베타를 모두 지원하는 번들은 다음 명령으로 생성한다.

```powershell
.\scripts\build\build-compat-bundle.ps1
```

이 명령은 `.workspace/game-references/`에 미리 저장한 정식 `0.107.x`와 공개 베타 `0.109.x` 참조를 자동으로 찾는다. Steam 브랜치를 각 버전으로 전환했을 때 한 번씩 다음 명령을 실행해 참조를 저장한다.

```powershell
.\scripts\setup\cache-game-reference.ps1 `
    -GamePath "C:\path\to\Slay the Spire 2"
```

필요하면 `-StableGamePath`와 `-BetaGamePath`로 캐시 대신 사용할 경로를 명시할 수 있다.

결과는 `.artifacts/packages/ThrowRockIronclad-vX.Y.Z/`와 같은 이름의 ZIP에 생성된다.

```text
ThrowRockIronclad-vX.Y.Z/
├─ ThrowRockIronclad.dll
├─ ThrowRockIronclad.pck
├─ ThrowRockIronclad.json
├─ throw-rock-ironclad-variants.manifest
└─ lib/
   ├─ 0.107.1/
   │  └─ ThrowRockIronclad.dll
   └─ 0.109.0/
      └─ ThrowRockIronclad.dll
```

최상위 DLL은 실행 중인 게임 버전을 감지하는 로더이고, `lib` 아래의 DLL은 각 게임 버전에 맞춰 컴파일된 실제 모드다. ZIP의 폴더 구조를 유지해 `mods/ThrowRockIronclad/`에 설치한다. 별도로 설치해야 하는 라이브러리 모드는 없다.

## 검증 범위와 남은 한계

자동 스모크 테스트는 바위클래드 전용 카드 풀, 시작 덱, 불타는 혈액, 전용 유물 풀, 아이언클래드 에셋 재사용과 원본 카드 격리를 검증한다. 컴파일 타임 전체 게임 통합 하네스도 바위클래드 전용 카드 타입을 사용하도록 전환했으며, 실제 게임 실행 검증은 새 프로토타입 배포 전에 다시 수행해야 한다.

이전 아이언클래드 교체 방식은 BaseLib 매니페스트를 비활성화한 실제 게임에서 전체 통합 하네스를 통과했다. 독립 바위클래드 프로토타입도 외부 모드 의존성은 없지만 실제 게임 검증 결과는 별도로 기록해야 한다.

Power 아이콘 경로는 모드 자체 Harmony 패치로 교체한다. 확대 아이콘은 경로 교체만으로 게임의 사전 로딩 목록에 들어가지 않으므로, Rock 카드의 `RunAssetPaths`에 Power가 사용하는 전용 소형·확대 아이콘을 추가한다. 실제 전투에서 `PreloadManager.Cache` 적재 여부까지 확인하며, 모드 아이콘에 대한 `Asset not cached` 경고가 없어야 한다.

아직 별도 수동 환경이 필요한 항목은 실제 저장 파일을 종료 후 다시 불러오는 전투 복구, 두 게임 클라이언트의 로비 접속과 장시간 desync 관찰, 모드 버전 불일치 차단, 손이 가득 찬 상태의 시각 확인, 힘·약화·취약 조합의 카드 미리보기다. 자세한 상태는 [MANUAL_TEST_CHECKLIST.md](MANUAL_TEST_CHECKLIST.md)에 남긴다.
