# 돌머리 아이언클래드 수동 테스트 체크리스트

기준 버전: Slay the Spire 2 `v0.107.1`, 모드 `v0.1.0-dev` (외부 모드 의존성 없음)

## 자동 검증 완료

- [x] Release 빌드: 경고 0, 오류 0
- [x] Release Publish 결과에 DLL, PCK, JSON 포함
- [x] Harmony 대상 메서드 전체 패치 성공
- [x] 대상 6종 Rock 태그 및 PrimalForce 제외 확인
- [x] 다섯 카드의 비용, DynamicVar, 강화 규칙 확인
- [x] Rock Slam의 Exhaust 확인
- [x] Rock Form 일반+강화 상태의 Amount 인코딩과 표시 중첩 수 확인
- [x] Rock Form 2중첩 비용 감소, 비용 0 하한, 비-Rock 카드 제외 확인
- [x] Rock Form이 다른 플레이어의 Rock 카드 비용을 변경하지 않음 확인
- [x] Absolute Rock 2중첩의 +12 피해, 비-GiantRock 공격 제외 확인
- [x] Absolute Rock이 다른 플레이어의 피해를 변경하지 않음 확인
- [x] 다섯 교체 카드가 직렬화·복원 후 같은 바닐라 카드 ID/타입으로 유지됨 확인
- [x] 네 Power의 한국어/영문 로컬라이징과 서로 다른 소형·확대 아이콘 리소스 존재 확인
- [x] 모드 DLL과 매니페스트에 BaseLib 참조가 없음을 자동 검증
- [x] 실제 게임에서 모드 DLL/PCK 로드 확인
- [x] BaseLib 매니페스트를 비활성화한 실제 게임에서 전체 통합 셀프 테스트 통과
- [x] 실제 게임에서 네 Power의 Model DB 등록 확인
- [x] 실제 게임에서 한국어/영어 카드·Power 테이블 병합 확인
- [x] 실제 게임 메인 메뉴 진입 확인
- [x] 실제 전투 방 진입 후 다섯 카드의 교체 `OnPlay`를 게임 명령 경로로 실행
- [x] 실제 Hook dispatcher를 통한 턴 시작 Rock Form 및 턴 종료 Rockade 발동 확인
- [x] Power 획득 전 GiantRock 사용 기록을 Rockade가 포함하는지 확인
- [x] 일반+강화 Rock Armor 10, Rockade 5, Absolute Rock 12 중첩 확인
- [x] GiantRock/GiantRock+의 실제 피해 16/20 기반값과 2중첩 피해 28/32 확인
- [x] Rock Slam 일반/강화의 피해 5, 생성, 소멸 및 GiantRock 기록 제외 확인
- [x] 원시의 힘이 실제로 변신시킨 GiantRock의 Rock 태그와 모든 GiantRock 시너지 호환 확인
- [x] 두 플레이어 전투 상태에서 비용·피해·방어도·사용 기록의 소유자 격리 확인
- [x] 전체 전투 네트워크 패킷 직렬화 왕복 후 혼합 Rock Form 상태 보존 확인
- [x] 실제 `NCard`/`NPower` 노드와 Power별 전용 소형·확대 아이콘 렌더 확인
- [x] 다섯 교체 카드의 모드 전용 초상화 경로 및 실제 `NCard` 텍스처 렌더 확인
- [x] 강화 카드 렌더에서 2→3, 4→6, GiantRock+, 비용 2→1/1→0 확인
- [x] Power 확대 아이콘이 전투 에셋 캐시에 사전 로드되어 동기 로드 경고가 없는지 확인

자동 스모크 테스트 실행:

```powershell
& '.\.tools\dotnet\dotnet.exe' run --project tests\ThrowRockIronclad.SmokeTests\ThrowRockIronclad.SmokeTests.csproj -c Release
```

컴파일 타임 전체 게임 통합 테스트는 `THROW_ROCK_SELF_TEST` 상수와 같은 이름의 환경 변수를 사용한다. 일반 Release에는 테스트 코드가 포함되지 않는다. 렌더 결과는 `tests/artifacts/full-game-integration.png`에 저장된다.

## 단일 플레이 전투

### 바위의 형상

- [ ] 일반 1장: 다음 턴부터 일반 GiantRock 1장 생성
- [ ] 강화 1장: 다음 턴부터 GiantRock+ 1장 생성
- [x] 일반+강화: 실제 턴 시작 Hook에서 각각 1장 생성
- [x] 2중첩: Rock 카드 비용 2 감소 (객체 수준 자동 검증)
- [x] 비용이 0 아래로 내려가지 않음 (객체 수준 자동 검증)
- [x] Rock 이외 카드는 비용 변화 없음 (객체 수준 자동 검증)
- [ ] 손이 가득 찼을 때 게임 표준 생성 동작과 일치

### 절대적인 바위

- [x] Power 없음: GiantRock 피해 16 및 바닐라 GiantRock+ 기반 피해 20 확인
- [ ] 1중첩: 피해 22/26
- [x] 2중첩: 실제 GiantRock/GiantRock+ 피해 28/32 및 추가 피해 +12 확인
- [x] 다른 공격 카드의 피해는 변하지 않음 (객체 수준 자동 검증)
- [ ] 힘, 약화, 취약과 함께 미리보기와 실제 피해가 일치

### 바위 갑옷

- [ ] 일반 1장: GiantRock 사용마다 방어도 4
- [ ] 강화 1장: GiantRock 사용마다 방어도 6
- [x] 일반+강화: GiantRock 및 GiantRock+ 사용마다 방어도 10
- [x] Rock Slam에는 발동하지 않음
- [x] 자동 사용된 GiantRock에는 발동

### 바위케이드

- [x] Power 획득 전에 사용한 GiantRock도 계산
- [ ] 일반: 완료 사용 2회일 때 턴 종료 방어도 4
- [ ] 강화: 완료 사용 3회일 때 턴 종료 방어도 9
- [x] 일반+강화: 완료 사용 3회일 때 방어도 15
- [x] 다음 턴 종료 Hook에서도 증가한 전체 전투 누적 사용량을 계산
- [x] 생성만 된 GiantRock은 사용 횟수에 포함하지 않음

### 바위 강타

- [x] 일반: 비용 1, 피해 5, 일반 GiantRock을 버린 카드 더미에 생성, 소멸
- [x] 강화: 비용 0, 나머지 효과 동일
- [x] 현재 방어도와 피해량이 무관
- [x] 생성된 GiantRock은 강화되지 않음

## UI와 저장

- [ ] 카드 도감·보상·덱에서 새 이름과 설명 표시
- [x] 실제 전투 카드 5종에서 제공된 전용 초상화 표시
- [ ] 다섯 카드의 바닐라 효과 툴팁이 남지 않음
- [x] Power 네 종에 서로 다른 전용 소형/확대 아이콘 표시
- [ ] 한국어와 영어 전환 후 모두 정상 표시
- [x] 강화 렌더에서 2→3, 4→6, 비용 2→1/1→0 표시
- [x] Rock Form+ 강화 렌더에서 GiantRock+ 표시
- [ ] 전투 저장 후 재실행해 카드와 Power 상태가 보존됨
- [ ] 일반+강화 Rock Form 혼합 상태가 저장 후에도 유지됨

카드 자체의 저장 형식은 자동 직렬화 왕복으로 검증했다. 위 두 항목은 실제 전투 복구 흐름과 Power Amount 복구를 화면에서 확인하는 절차다.

## 멀티플레이

실제 두 클라이언트 로비 테스트는 별도 수동 환경이 필요하다. 현재 자동 통합 테스트는 두 플레이어가 들어 있는 실제 전투 상태에서 소유자 격리를 검증하고, `NetFullCombatState` 패킷 직렬화 왕복으로 혼합 Rock Form 상태가 보존됨을 확인한다.

- [ ] 같은 모드 버전의 2인 로비 접속
- [ ] 모드 버전 불일치 시 참가 차단
- [x] 각 플레이어의 GiantRock 사용량이 섞이지 않음 (두 플레이어 전투 상태 자동 검증)
- [x] 다른 플레이어의 Rock 비용·피해·방어도에 영향을 주지 않음 (자동 검증)
- [x] 일반+강화 Rock Form 상태와 생성 순서가 결정론적임 (자동 검증 및 패킷 왕복)
- [ ] 자동/복제 사용 후 desync 없음
