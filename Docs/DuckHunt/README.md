# Villa duck hunt — 25 collectibles

Scene: `Assets/_Project/Scenes/Modern Villa.unity`.

The five existing duck models each appear five times. All 25 have separate RubberDuckData assets/IDs (1001–1025), so collecting another copy of the same model counts independently. The shared return registry now expects 25 ducks. Existing pickup offsets, held interpolation fix, trait hooks and buoyancy/return events remain.

## Placement

19 ducks are spread through the main lounge, dining room, study, reading room, annex living/breakfast rooms, upper studio and retreat. Six are near the front-pool lounge and garden seats/benches. They use table edges, books, plants and furniture corners as visual cover, with a mix of floor and tabletop heights. No ducks start in the water or beyond the boundary. The existing 32 cm model sizing is retained for consistent visibility and pickup.

Every position was selected around a named environment feature, then checked for flat support across its footprint, collider clearance, a clear standing capsule and an unobstructed pickup ray within the player's interaction range. Seats whose physical collision surface did not match the visible cushion and the unsupported pavilion location were rejected. The exact positions and validated approach points are in `placement.txt` (spoilers).

## Boundary

`Villa Boundary - Invisible Colliders` contains four overlapping, non-trigger static BoxColliders. The inner limits are x=-19..13 and z=-4..32.5; vertical coverage is y=-10..20. These enclose the surveyed buildings, terraces, both pools and garden/pavilion. No Renderer, Rigidbody, Update loop, or new visual wall is added. Existing entrances and interior paths remain within the boundary.

## Verification

An editor-only Play-mode check confirmed 25 unique registry entries, all 25 initial positions stable after physics settling, and an actual pickup ray hitting each duck from a clear standing approach. None was automatically returned at startup. Capsule casts against all four barriers passed 132/132 samples at three heights. See `checks.txt`.

This checks local accessibility and boundary coverage; it is not a complete human walkthrough of every route or a final difficulty/playtime balance test. Existing missing-script warnings on Play-mode scene load remain separately tracked; no new missing components were added.

## Performance and preservation

Models/materials are reused; there are no new rendering effects or lights. Ducks retain simple box collision and normal Rigidbody sleeping; the barriers use four static primitive colliders. No lightmap or APV rebake is needed for these dynamic collectibles and invisible colliders. No FPS improvement is claimed. The scene before placement is preserved in the ignored `BeforeHunt.unity.backup` file.
