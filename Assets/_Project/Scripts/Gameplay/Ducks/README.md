# 러버덕 줍기와 운반

오리 프리팹의 루트에 `RubberDuckInteractable`, `Rigidbody`, 충돌체를 붙입니다.
Renderer와 충돌체가 자식에 있어도 됩니다. 플레이어 루트에는 `PlayerDuckCarrier`를 붙이고
View에 플레이어 카메라를 연결합니다. 플레이어의 `PlayerInputReader`와
`PlayerInteractionDetector`도 같은 루트에 있어야 합니다.

각 오리는 `RubberDuckData`를 참조합니다. 데이터에는 정수 ID, 표시 이름, 설명,
수집 아이콘, 고유 사운드와 운반 포즈 보정값이 들어갑니다. ID는 오리 식별과
수집 목록 정렬에 함께 사용하므로 오리마다 겹치지 않게 부여합니다.

Held Position Offset과 Held Euler Angle Offset은 플레이어의 기본 Hold 포즈에 더해지는 오리별 보정값입니다.
두 값의 기본값은 모두 0이며, 이때 오리는 화면 오른쪽 아래에서 플레이어를 바라봅니다.
오리 모델의 정면은 로컬 +Z축을 기준으로 합니다.

- 좌클릭으로 바라보는 오리를 들면 카메라 아래의 Hold Anchor에 붙어 화면에 보입니다.
- 들고 있는 동안 오리의 충돌체를 끄고 Rigidbody를 kinematic으로 바꿉니다.
- 한 마리만 들 수 있습니다. 다른 오리는 계속 감지되고 외곽선이 보이지만 획득은 실패합니다.
- 다른 종류의 `IInteractable`은 오리를 든 상태에서도 그대로 실행됩니다.
- G를 누르면 감지 중인 상호작용 대상과 관계없이 플레이어 앞 공중에서 놓아 중력으로 떨어집니다.
  앞이 벽으로 막혀 충분한 공간이 없으면 오리를 계속 들고 있습니다.

`PlayerDuckCarrier`의 Held Local Position/Rotation과 Drop Distance로 화면 위치와
내려놓는 거리를 조절할 수 있습니다. 오리별 특성과 능력은 아직 포함하지 않습니다.
발견 여부, 수영장 반환 여부 같은 실행 중 진행 상태는 `RubberDuckData`에 저장하지 않습니다.

`PlayerTestScene`에는 각각 한 마리의 오리를 대신하는 작은 테스트 큐브 5개가 모여 있습니다.
`Rubber > Add Duck Test Objects` 메뉴는 누락된 테스트 오리만 이 씬에 추가합니다.
테스트 오리 5개는 반환 중복 판정을 확인할 수 있도록 서로 다른 데이터 ID를 사용합니다.

`Rubber > Add Duck Return Test Area` 메뉴는 `PlayerTestScene`에 벽과 낮은 바닥으로 된
반환 풀을 추가합니다. 오리가 내부 Trigger로 떨어지면 최초 1회만 반환 수가 올라가며,
화면 왼쪽 위의 테스트 HUD에서 `반환 수 / 전체 수`를 확인할 수 있습니다.
