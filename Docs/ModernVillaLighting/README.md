# Modern Villa 실내 조명

변경 씬: `Assets/Modern Villa/Scenes/Modern Villa.unity`
배치 도구: `Assets/_Project/Editor/ModernVillaLighting.cs`
씬 루트: `Modern Villa - Interior Lighting`

- 실제 Modern Villa의 RoofLamp 프리팹 2개를 두 식탁 중심 위에 배치했다. 케이블 상단을 각 층 천장에 맞췄으며 크기는 원본 그대로다.
- WallLight 프리팹 6개를 식당, 서재, 독서 공간, 거실, 별채 휴식 공간의 벽에 배치했다. 통로와 바닥을 점유하지 않는다.
- 기존 탁상등 4개와 기존 천장등 2개에 맞춘 광원을 새 루트 아래 추가했다. 원본 조명 기구는 이동하거나 덮어쓰지 않았다.
- 총 20개 Spot Light: 천장등 4, 탁상등 4, 벽등 상하향 12. 3600K, Mixed, 그림자 사용. 작은 국소 광원은 그림자 해상도를 낮췄다. 전역 렌더 설정은 변경하지 않았다.

Unity에서 실행·저장하고, 추가 보조광 없는 카메라로 일곱 실내 시점의 before/after 이미지를 촬영하여 확인했다. 과도했던 탁상등 밝기를 낮추고 식당과 독서 공간 벽등을 보완했다. 배치를 다시 실행해도 기존 루트가 있으면 추가하지 않는 것을 확인했다.

`validation.txt`에 최종 광원 설정과 프리팹 크기·경계가 있다. `scope-audit.txt`의 비교 기준은 이번 조명 작업 직전 `Before.unity.backup`이다. 기존 씬 문서는 루트 목록 외에 보존했고 새 문서 100개를 추가했다. Unity 자동 갱신으로 생긴 기존 조명·Terrain 직렬화 변경은 원래대로 유지했다. 외부 프리팹·공유 재질·Beach Villa·플레이어·수집 코드는 변경하지 않았다.

베이크는 실행하지 않았다. 현재 사진은 직접광 검수이며 베이크 후 최종 결과가 아니다. 식당·서재의 그늘과 간접광은 여전히 어둡다. 다음 조명 단계에서 베이크 후 밝기와 반사광을 재조정하고 실제 플레이어 이동 중 광원 전환과 성능을 확인해야 한다. 이번에는 실제 플레이 모드 이동 및 수집 판정 검증을 하지 않았다. 별채 휴식 공간의 기존 액자 방향도 추후 가구 검수 대상으로 남아 있다.

사진: `after-living.png`, `after-dining.png`, `after-study.png`, `after-breakfast.png`, `after-reading.png`, `after-studio.png`, `after-retreat.png`.
