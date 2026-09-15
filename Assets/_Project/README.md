# Rubber 프로젝트 폴더

게임에서 직접 만드는 에셋은 이 폴더에 넣는다. Unity 패키지, URP 설정, 튜토리얼 에셋은 `_Project` 밖에 그대로 둔다.

## 작업 영역

- `Scripts/Gameplay`: 플레이어 이동, 카메라, 상호작용, 오리 줍기/놓기, 수영장 반환, 진행 및 클리어 판정
- `Scripts/World`: 낮과 밤의 변화, 조명, 환경 동작
- `Scripts/UI`: HUD, 메뉴, 오리 수집 표시, 상호작용 안내
- `Scripts/Audio`: BGM과 효과음 재생 코드
- `Art`, `Audio`, `Prefabs`, `Scenes`: 코드가 아닌 에셋을 종류별로 보관

## 간단한 책임 규칙

`Core`가 전체 수집 개수와 게임 진행 상태를 가진다. Gameplay은 그 상태를 바꾸고 UI는 상태를 화면에 표시한다. 오리별 차이는 `Gameplay/Ducks`에 모으고, 오리마다 별도의 매니저를 만들지는 않는다.

## 씬 규칙

- `Scenes/Main`: 빌드에 포함할 실제 플레이 씬
- `Scenes/Test`: 기능 하나만 빠르게 확인할 테스트 씬

외부 에셋을 가져오게 되면 `_Project` 밖의 `Assets/ThirdParty`에 보관한다.
