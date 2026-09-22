# 플레이어 테스트

`Assets/_Project/Scenes/Test/PlayerTestScene.unity`를 열고 Play를 실행합니다.

- WASD: 이동 / Space: 점프 / 마우스: 시점 회전 / E: 바라보는 대상과 상호작용
- Esc: 커서 해제 및 조작 중지 / 게임 화면 좌클릭: 조작 재개
- 플레이어는 Renderer 없이 CapsuleCollider와 Rigidbody만 사용합니다.

## 입력 흐름

`PlayerControls.inputactions` → `PlayerInputReader` → `PlayerMovement.Move/Jump`,
`PlayerCamera.Look`, `PlayerInteractionDetector.TryInteract`

입력은 New Input System만 사용합니다. InputReader가 입력 에셋의 복제본을 활성화하고,
Move 이벤트와 현재 입력값을 이동 스크립트에 전달합니다. 이동 스크립트는 입력 장치를
직접 읽지 않으며 FixedUpdate에서 물리 이동을 처리합니다. 카메라는 프레임별 마우스
이동량을 사용합니다.
E를 누르면 감지기가 그 순간 카메라 중앙 레이를 다시 확인한 뒤 대상의
`IInteractable.TryInteract(player)`를 한 번 호출합니다. 커서가 풀렸거나 게임 창의 입력
포커스가 없을 때는 실행하지 않습니다. 테스트 큐브는 실행할 때마다 색이 바뀌고 Console에 기록됩니다.
실제 오리와 수영장은 나중에 같은 인터페이스를 구현하면 되므로 이 테스트 씬을 수정할 필요가 없습니다.
실제 플레이 씬의 플레이어에도 `PlayerInteractionDetector`를 붙이고 Camera와 PlayerStats를
연결해야 합니다. 같은 플레이어 오브젝트에 있으면 PlayerInputReader가 자동으로 찾습니다.

## Inspector 설정

- PlayerMovement: View, Player Stats 참조
- PlayerStats: Move Speed, Acceleration, Deceleration, Jump Height, Coyote Time,
  Rising/Falling Gravity Multiplier, Step Height, Max Slope Angle, Ground Mask,
  Interaction Distance
- PlayerCamera: Sensitivity, Pitch Limit, Step Smooth Time
- PlayerInputReader: Input Actions, Movement, Player Camera, Interaction Detector 참조

캡슐은 높이 1.8, 반지름 0.3, 중심 Y 0.9이며 카메라 높이는 1.6입니다.
플레이어 루트는 스케일 1, Y축 캡슐을 전제로 합니다. 플레이어의 Ignore Raycast 레이어는
Ground Mask에서 제외되어 자신의 캡슐을 지면으로 감지하지 않습니다.
기본 최대 계단 높이는 0.3이며 테스트 계단은 0.2와 0.28 높이입니다.
계단을 오를 때 충돌체는 기존처럼 단층 위로 이동하고, 카메라 높이는 Step Smooth Time을 기준으로
부드럽게 따라옵니다. 기본값은 0.1초입니다.
점프 높이는 Jump Height로 정하고, 상승·하강 속도는 각각의 Gravity Multiplier로 조절합니다.
기본값은 상승 2.2배, 하강 2.7배로 설정되어 있어 점프 높이를 유지하면서 오르내리는 시간이 짧아집니다.
이동 설정은 `ScriptableObjects/Player/PlayerStats.asset`에서 공통으로 관리합니다.
기본 가속도는 20, 감속도는 50입니다. 지면에서 이동 키를 놓으면 수평 속도를 바로 멈춰
관성으로 단층 가장자리를 벗어나지 않도록 합니다.
발판에서 벗어난 직후에도 Coyote Time 동안 점프할 수 있습니다. 기본값은 0.12초입니다.
상호작용 감지 거리는 같은 PlayerStats 에셋의 Interaction Distance에서 관리하며 기본값은 4.5입니다.
화면 중앙 레이가 `IInteractable`을 감지하면 대상과 모든 자식 Renderer를 하나의 마스크로
그린 뒤 PC에서는 14픽셀, Mobile에서는 8픽셀 너비의 화면 공간 외곽선을 합성합니다.
대상의 원본 Material 배열은 변경하지 않습니다.
외곽선 색상과 픽셀 너비는 PC/Mobile Renderer Data의 `Interaction Outline` Feature에서 조절합니다.

## 에디터 도구

- Rubber > Setup Player Test Scene: 테스트 코스가 없을 때만 생성합니다.
- Rubber > Check Player Test Scene: 현재 테스트 씬에서 Play 모드 물리/입력 검증을
  수행하고 종료합니다. 결과는 Console과 `Temp/RubberPlayerCheck.txt`에 기록합니다.
  시뮬레이션 위치는 씬에 저장하지 않습니다.
