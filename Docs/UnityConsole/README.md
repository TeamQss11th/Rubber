# Unity Console cleanup — 2026-09-29

- Updated project calls to deprecated `FindObjectsByType` overloads to the Unity 6000.4 overloads without sorting parameters. All previous calls used `None`, so no sorted ordering was required. Replaced the door survey's deprecated instance identifier with `GetEntityId`.
- Removed one unresolved script component from the existing `StaticLightingSky` root. Its script GUID does not resolve in Assets or installed packages; the serialized component had no custom fields. The root and all functioning lighting systems remain.
- Created six terrain-only plant prefab copies in `Assets/_Project/Prefabs/TerrainPlants`. Removed MeshColliders unsupported by TerrainCollider and reassigned the existing terrain prototype indices. Original vendor prefabs and their colliders remain available for individually placed objects.
- Verified all 1,614 tree instance records were unchanged. These decorative terrain plants remain non-solid: Unity previously ignored their unsupported colliders. Terrain ground collision was not disabled.

## Verification

Unity recompiled the scripts and executed the repair successfully in Edit mode. The subsequent Console inspection reported zero entries, and the scene inspection found no missing scripts or MeshColliders in assigned tree prototypes. No Console clear command was used. See `repair.txt`, `scene.txt`, and `console.txt` for results. A new Play-mode performance or traversal test was not run.

The scene and TerrainData file were backed up before repair in the timestamped backup directory alongside this report. These backups preserve the pre-repair water work as well.

Earlier editor logs also contained Unity Services token-exchange/HTTP 401 failures. Those concern the editor's account connection; no account, cloud-project, or authentication settings were changed. They were not present in the post-repair Console snapshot and may recur independently of project code.

## Performance scope

No additional runtime effects or collision bodies were added. The API migration preserves unsorted searches; terrain copies reuse the original rendering assets. This is warning cleanup, not a measured FPS improvement.
