# Beach Villa 전체 오브젝트·조명 조사

> 후속 변경: 2026-09-22 카메라 정리 완료. 현재는 Main Camera만 활성화되어 있다. 아래의 카메라 12개 활성 설명은 정리 전 조사 결과다. `inventory-before-camera-cleanup.txt`는 이전 조사, `inventory.txt`는 정리 후 조사, `camera-cleanup.txt`는 변경 내역이다.

조사일: 2026-09-22. 대상: `Assets/Modern Villa/Scenes/Beach Villa.unity`. Unity 6000.4.0f1 / 설치된 URP 소스와 실제 에디터 로드 상태 기준. 조사 시 씬은 저장 상태였고, 씬·조명·재질·품질 설정을 수정하거나 베이크하지 않았다. 읽기 전용 Editor 조사 도구와 이 보고서만 추가했다.

## 판단

LOD, 정적 배칭 플래그, 압축 텍스처와 기존 라이트맵 등 기반 최적화는 있다. 그러나 게임 실행을 위한 마무리는 필요하다. 가장 먼저 확인할 문제는 같은 전체 화면으로 출력하는 활성 카메라 12개다. 현재 상태만으로 1080p 60FPS를 보장할 수 없다. 이번 결과는 구성 조사이며 Profiler/Frame Debugger/빌드 실측 결과가 아니다.

## 오브젝트 구성

| 항목 | 확인 수치 |
|---|---:|
| 전체 GameObject | 2,872 |
| 활성 계층의 GameObject | 2,858 |
| 활성 계층의 enabled Renderer | 2,351 |
| 고유 Mesh | 336 |
| Renderer에서 사용하는 고유 재질 | 75 |
| 재질에서 참조하는 고유 텍스처 | 170 |
| LODGroup | 341 |
| BoxCollider / MeshCollider / TerrainCollider | 792 / 828 / 1 |
| Rigidbody / Animator / ParticleSystem | 0 / 0 / 0 |
| 누락된 스크립트 | 0 |

Renderer 수에는 같은 오브젝트의 여러 LOD 단계가 들어 있다. 실제 화면에 동시에 그려지는 수나 드로우콜 수가 아니다. 모든 Mesh 인스턴스 삼각형 합계 4,300,770개도 여러 LOD 단계의 합이며 Terrain과 시야 판정을 제외한 자료량이다.

| 주요 계층 | Renderer 수 | 역할 |
|---|---:|---|
| BuildingD | 1,166 | 본관 구조와 1·2층 가구·식기·커튼·실내등 |
| PoolLevel | 624 | 수영장 높이의 시설·주변 소품 |
| Floor | 132 | 바닥·기단 |
| FloorLevelTerrace | 122 | 테라스 |
| Front Area | 94 | 앞쪽 공간 |
| Foliage | 72 | 식재 |
| LoungeArea | 64 | 휴식 공간·정원등 |
| PavillonC | 42 | 파빌리온과 소품 |
| Plants | 15 | 별도 식물 배치 |

Terrain은 200×200m, 높이 범위 설정 600m, 높이맵 해상도 513, Pixel Error 5다. 높이 범위는 실제 지형 높낮이 차이를 뜻하지 않는다. Terrain 나무 인스턴스와 디테일 원형은 0이므로 식물 비용은 주로 별도 Mesh에 있다. Terrain Draw Instanced는 꺼져 있다.

## 카메라 — 우선순위가 가장 높은 확인 사항

카메라 13개 중 12개가 active+enabled다. 모두 Display 0, 화면 전체 Viewport, RenderTexture 없음, 모든 레이어, Skybox clear, near 0.3 / far 1,000m이다. Main Camera depth는 -1, 나머지는 0이다. 카메라 전환 스크립트나 URP Additional Camera Data는 발견되지 않았다.

현재 구성은 같은 화면에 여러 Base Camera를 렌더링하는 조건이다. 마지막 결과만 보인다고 앞선 카메라 비용이 없다고 볼 수 없다. 실제 카메라별 패스·시간은 Frame Debugger/Profiler로 확인해야 하며, 12배 비용이라고 단정하지 않는다. 1인칭 게임에서는 플레이용 1개를 선택하고 촬영용은 필요할 때만 활성화하는 방식을 우선 검토한다. 이번에는 끄지 않았다.

## 조명

| 종류·위치 | 개수 | 현재 모드 | 밝기 / 범위 | 그림자 |
|---|---:|---|---|---|
| Directional Light | 1 | Realtime | 1 / 방향광 | Soft |
| BuildingD/Level0 Objects/RoofLamp 계열 | 6 | Realtime Spot | 3.06 / 5m | None |
| LoungeArea/FloorLightB 계열 | 4 | Realtime Point | 1 / 3m | None |

총 11개가 모두 켜져 있고 Baked 또는 Mixed 모드 Light는 없다. 태양은 약간 따뜻한 색 RGB(1, 0.957, 0.839), 국소등은 흰색이다. 국소등 10개의 그림자를 꺼 비용을 줄였고 범위도 짧다. 다만 거리별 on/off 관리나 시간대별 조명 전환 스크립트는 없으며 낮에도 enabled다. 엔진의 기본 시야·광원 컬링과 별도의 거리 관리 기능은 구분해야 한다.

RenderSettings는 안개 OFF, Skybox 기반 환경광, 환경광 강도 1, 반사 강도 1, 기본 반사 해상도 128이다. 전용 Sun 참조는 비어 있으나 실제 Directional Light가 없다는 뜻은 아니다. Skybox는 이 씬에서 Unity 기본 재질 참조다.

## 베이크된 빛과 현재 실시간 빛의 관계

- Baked GI ON, Realtime GI OFF.
- 실제 로드된 라이트맵 16세트: 컬러 EXR + 방향 PNG. 컬러는 1024×1024이며 Shadowmask 텍스처는 없다.
- Renderer 2,147개에 유효 범위 라이트맵 인덱스가 할당되어 있다.
- Lighting Settings: 40 texels/unit, atlas 최대 1024, padding 2, 베이크 AO OFF, 바운스 2, Direct samples 32, Indirect 512, Environment 256.
- 설정의 Mixed Mode는 Shadowmask이지만 현재 Light들은 모두 Realtime이고 실제 Shadowmask 텍스처도 없다. 따라서 ‘Shadowmask 혼합 조명이 작동 중’이라고 해석하면 안 된다.
- 현재 Light의 bakingOutput.isBaked는 모두 false다. 기존 라이트맵은 로드돼 있지만 지금 광원 배치·설정과 일치하는 시점의 결과인지는 확인되지 않았다. 파일 존재나 인덱스만으로 최신 베이크라고 판단할 수 없다.

새 정적 가구를 추가하면 기존 베이크에 그 가구의 차폐와 간접광이 반영되지 않는다. 배치가 확정된 뒤 재베이크 검토가 필요하다. 낮밤 전환은 태양 색·각도만 바꿔도 베이크된 빛이 자동으로 밤에 맞춰 바뀌지는 않으므로 현재 라이트맵 내용을 먼저 확인해야 한다.

## 오리·반사·후처리

- LightProbeGroup 0개, 로드된 Light Probe 샘플 0개. 이동·집기 가능한 러버덕에 주변의 베이크 간접광을 전달할 기반이 없다. 실시간 직접광은 받을 수 있으나 정적 가구와 밝기 차이가 날 수 있다.
- ReflectionProbe 컴포넌트 0개. 폴더에 `ReflectionProbe-0.exr`가 존재하지만 현재 씬에 배치된 반사 프로브로 사용 중인 증거는 없다. 파일 존재와 실제 씬 사용을 구분한다.
- 별도 평면 반사 스크립트는 발견되지 않았다. 재질 29개에서 환경 반사 OFF 키워드를 확인했다. 반사 비용과 금속·유리의 품질에 함께 영향을 주는 설정이다.
- 재질 4개가 Transparent queue다. 유리 등이 겹쳐 보이는 구간은 픽셀 중복 처리 점검 대상이다. 렌더러 4개라는 뜻은 아니다.
- 씬에는 Volume 및 사용자 MonoBehaviour가 없고 URP Additional Light Data 11개만 발견됐다. 카메라의 URP Additional Camera Data도 없다. 설치된 URP 코드의 해당 기본 경로는 Base camera / postProcessEnabled=false다. 기본 Volume Profile에 Bloom 등이 존재한다는 이유만으로 이 게임 카메라에서 효과가 켜졌다고 볼 수 없다.
- SSAO는 Renderer Feature이므로 위의 일반 후처리 OFF와 별개로 켜져 있다.

## 적용되어 있는 최적화

1. **LOD 341개:** 원거리에서 낮은 상세도의 Mesh로 바꾸는 기반이다. 2단계 149개, 3단계 127개, 4단계 65개. 모든 그룹의 fadeMode는 None이다. PC LOD Bias=2여서 Bias=1보다 높은 상세도를 오래 유지하는 방향이다.
2. **정적 배칭 준비:** Renderer 2,147개에 BatchingStatic, ContributeGI, OccluderStatic, OccludeeStatic 플래그. Standalone Static Batching도 ON. 실제 합쳐진 배치와 드로우콜은 빌드 검증이 필요하다.
3. **SRP Batcher ON:** 재질·셰이더 렌더 상태 설정의 CPU 비용을 줄이는 기능. 오브젝트를 모두 한 드로우콜로 합친다는 뜻은 아니다.
4. **국소등 그림자 OFF:** Spot 6개, Point 4개에서 그림자 맵 생성을 하지 않는다.
5. **텍스처 압축·밉맵:** 조사한 재질 텍스처 170개 모두 mipmap ON, Read/Write OFF, compression=Compressed. 2K 115개, 1K 53개, 128×128 2개. 4K 이상은 없다.
6. **기존 라이트맵 사용 / Realtime GI OFF:** 베이크 간접광을 사용하는 기반이며, 실시간 GI 계산은 꺼져 있다. 현재 직접광 11개를 무료로 만드는 것은 아니다.
7. **MSAA OFF / Render Scale 1:** URP MSAA=1은 멀티샘플링 없음이다. 카메라 allowMSAA=true만 보고 MSAA가 켜졌다고 해석하면 안 된다.

## 아직 미적용이거나 점검할 사항

| 항목 | 실제 상태 | 의미 |
|---|---|---|
| 베이크 Occlusion Culling | 씬 데이터 참조 없음 | 정적 가림 플래그와 카메라 체크만으로 벽 뒤 오브젝트 생략이 준비된 것은 아님 |
| GPU Occlusion / GPU Resident Drawer | OFF / OFF | 이 경로의 GPU 가림 처리·인스턴싱은 미사용 |
| GPU Instancing | 재질 75개 중 1개 ON | mv_Fruits만 ON. SRP Batcher·정적 배칭과의 관계 때문에 일괄 ON을 권하지 않음 |
| Dynamic Batching | OFF | 반드시 켜야 하는 결함은 아님. 실제 CPU 비용을 비교할 대상 |
| 텍스처 Streaming | PC 품질 OFF, 재질 텍스처 170개도 OFF | 메모리 여유가 작으면 점검할 후보. 라이트맵 일부의 importer streaming ON만으로 전체 streaming이 동작하지 않음 |
| MeshCollider | 828개 모두 non-convex | 정적 환경에는 사용 가능. 작은 장식까지 필요한지 개별 검사 필요 |
| Renderer 그림자 | 2,351개 모두 cast/receive ON | LOD 단계 포함. 작은 식기·장식의 그림자 비용 점검 후보 |
| LOD 임계값 | 18개 그룹에 0.233 → 0.009 → 0.010 | 순서가 역전된 구간이 있어 전환 검사 필요. 이번에는 수정하지 않음 |

MeshCollider 원본 삼각형 합은 1,506,273개다. 물리엔진의 매 프레임 충돌 검사량을 뜻하지 않는다. Rigidbody가 없어서 모두가 동적 물리 시뮬레이션 중인 것도 아니다.

자료량이 큰 반복 Mesh 예: CurtainD LOD0 22,044 triangles ×20, SoupBowl LOD0 9,472×18, LargeAntipastiPlate LOD0 8,832×18, DiningChairB LOD0 8,240×18. 1인칭 근접 시에는 세부가 필요할 수 있지만 식탁과 커튼이 다수 보이는 구간을 우선 측정할 이유가 된다.

재질 텍스처의 에디터 RuntimeMemorySize 합은 약 1.20GiB다. 에디터 내부 표현을 포함한 참고값이며 플레이어 GPU VRAM 실측이 아니다. Terrain·라이트맵·렌더 타깃·Mesh 전체 메모리를 포괄하지 않는다.

## PC 렌더 설정의 비용

현재 품질은 PC, 파이프라인은 PC_RPAsset, Renderer는 Forward+다. 설치된 URP enum과 확인했다.

| 항목 | 설정 |
|---|---|
| 태양 그림자 atlas | 2048 |
| 그림자 거리 / Cascade | 50m / 4 |
| Soft Shadows | ON, High |
| 추가등 그림자 지원·atlas | ON / 2048 (현재 국소등 자체 그림자는 OFF) |
| SSAO | ON, 전체 해상도, DepthNormals, 8 samples, High/Bilateral blur |
| SSAO intensity / radius / falloff | 0.4 / 0.3 / 100 |
| Depth Texture / Opaque Texture | ON / ON |
| Opaque Texture downsampling | 2× Bilinear |
| HDR / Render Scale | ON / 1.0 |
| VSync | OFF |

QualitySettings에 별도로 보이는 Shadow Distance 40 / Cascade 2 대신 URP의 50 / 4를 기준으로 해석해야 한다. 또한 Additional Lights Per Object Limit=4는 저장돼 있지만 Forward+에서 ‘오브젝트당 네 개만 계산’한다는 보장이 아니다.

## 후속 작업 우선순위 — 이번에는 미실행

1. 플레이용 카메라 한 개를 정하고 나머지 촬영 카메라의 동시 출력을 정리한 뒤 1080p 기준 측정.
2. 기존 라이트맵과 현재 Realtime 광원의 일치 여부, 낮밤 요구에 맞는 간접광 방식을 결정. 움직이는 오리용 Light Probe 검토.
3. 식기·커튼·식물이 많은 뷰에서 GPU/CPU/드로우콜 측정. 작은 소품의 그림자·콜라이더와 LOD 전환을 선별 조정.
4. 본관의 벽·방 구조에 맞는 Occlusion Culling 검토. 열린 해변 구간에는 이득이 적을 수 있으므로 별도 비교.
5. SSAO 전체/절반 해상도, 그림자 4/2 cascade, 필터 품질을 같은 뷰로 A/B 비교.
6. 플레이어 빌드에서 텍스처·라이트맵 메모리와 16.67ms 프레임 예산 확인.

## 근거

- 전체 조사 값: [inventory.txt](inventory.txt). 실제 사용 중인 재질·텍스처 경로와 오브젝트 계층별 기록 포함.
- 씬 원본, `Beach Villa Lighting Settings.lighting`, `ProjectSettings/QualitySettings.asset`, `Assets/Settings/PC_RPAsset.asset`, `PC_Renderer.asset`, 설치된 URP 소스를 대조했다.
- [Unity Forward+ 설명](https://docs.unity.cn/6000.0/Documentation/Manual/urp/rendering/forward-plus-rendering-path.html): per-object light limit 해석.
- [Unity 카메라 렌더 순서](https://docs.unity3d.com/cn/6000.0/Manual/urp/cameras-advanced.html): 복수 카메라의 렌더링.
- [Unity 베이크 조명](https://docs.unity3d.com/ja/current/ScriptReference/LightmapBakeType.Baked.html): 정적 라이트맵과 동적 물체용 프로브의 구분.

수정 보고: 읽기 전용 조사 코드와 문서만 추가. 씬 오브젝트·빛·품질 변경 없음. 새 최적화 적용 없음. FPS/GPU 시간 실측 없음.
