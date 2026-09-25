# 러버덕 줍기와 운반

오리 프리팹의 루트에 `RubberDuckInteractable`, `Rigidbody`, 충돌체를 붙입니다.
Renderer와 충돌체가 자식에 있어도 됩니다. 플레이어 루트에는 `PlayerDuckCarrier`를 붙이고
View에 플레이어 카메라를 연결합니다. 플레이어의 `PlayerInputReader`와
`PlayerInteractionDetector`도 같은 루트에 있어야 합니다.

- E로 바라보는 오리를 들면 카메라 아래의 Hold Anchor에 붙어 화면에 보입니다.
- 들고 있는 동안 오리의 충돌체를 끄고 Rigidbody를 kinematic으로 바꿉니다.
- 한 마리만 들 수 있습니다. 다른 오리는 계속 감지되고 외곽선이 보이지만 획득은 실패합니다.
- 다른 종류의 `IInteractable`은 오리를 든 상태에서도 그대로 실행됩니다.
- 감지된 상호작용 대상이 없을 때 E를 누르면 플레이어 앞 공중에서 놓아 중력으로 떨어집니다.
  앞이 벽으로 막혀 충분한 공간이 없으면 오리를 계속 들고 있습니다.

`PlayerDuckCarrier`의 Held Local Position/Rotation과 Drop Distance로 화면 위치와
내려놓는 거리를 조절할 수 있습니다. 오리별 이름, 특성, 능력 데이터는 아직 만들지 않았습니다.
나중에 ScriptableObject를 추가하더라도 이 공통 입력과 운반 흐름을 유지할 수 있습니다.

`PlayerTestScene`에는 각각 한 마리의 오리를 대신하는 작은 테스트 큐브 5개가 모여 있습니다.
`Rubber > Add Duck Test Objects` 메뉴는 누락된 테스트 오리만 이 씬에 추가합니다.
