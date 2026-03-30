# Spine 기본 구동 세팅

## 1) 현재 프로젝트 상태
- `Packages/manifest.json`에 `spine-csharp`, `spine-unity` 패키지가 이미 포함되어 있습니다.
- 즉, Spine 에셋(`.skel.bytes`/`.json`, `.atlas.txt`, `.png`)만 넣으면 바로 임포트 가능합니다.

## 2) 권장 폴더
- `Assets/Spine/Characters/<캐릭터명>/` : Spine 원본 export 파일 보관
- `Assets/Prefabs/Characters/` : Spine 프리팹 저장
- `Assets/Scripts/Spine/` : Spine 제어 스크립트

## 3) 씬에 배치하는 기본 순서
1. Spine export 파일을 프로젝트에 추가합니다.
2. 생성된 `SkeletonDataAsset`을 씬으로 드래그해 오브젝트를 생성합니다.
3. 생성된 오브젝트에 `SpineAnimationDriver`를 붙입니다.
4. 입력 테스트가 필요하면 `SpineInputController`를 같이 붙입니다.
5. `Skeleton Animation` 필드에 같은 오브젝트의 `SkeletonAnimation`을 연결합니다.
6. 애니메이션 이름(`idle`, `run`, `attack`)을 실제 Spine 데이터 이름과 맞춥니다.

## 4) 코드 연동 예시
```csharp
// 이동 상태 반영
driver.SetMoving(isMoving);

// 공격 입력 시
if (attackPressed)
    driver.PlayAttack();
```

`SpineInputController` 기본 바인딩:
- 이동: `WASD`, 방향키, 게임패드 Left Stick
- 공격: `J`, 마우스 Left Click, 게임패드 A(buttonSouth)

## 5) 점검 포인트
- 애니메이션이 재생되지 않으면 이름 오타(`idle`, `run`, `attack`)를 먼저 확인하세요.
- 핑크색으로 보이면 머티리얼/셰이더 참조를 확인하세요.
- 애니 전환이 너무 급하면 `Default Mix Duration` 값을 올리세요.
