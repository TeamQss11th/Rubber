# Player + Map integration

Merged `origin/Player` at `31ffdce` into `Map`. The playable scene is `Assets/_Project/Scenes/Modern Villa.unity`; it is now the enabled first scene in Build Settings. The teammate's separate PlayerTestScene is preserved as a development asset and is not loaded by the game scene.

## Controls

WASD: move; mouse: look; Space: jump; left click: interact with a door or pick up a duck; G: drop; Escape: release cursor; left click again: capture cursor.

## Connections

- One Player uses the Player branch's Rigidbody movement, input actions, camera, interaction detector, duck carrier, and center reticle. Its camera enables the existing URP postprocessing. Interaction outlines from Player are preserved.
- Eight existing sliding/hinged doors use `VillaDoorInteractable` to expose the existing animations through `IInteractable`. An idle door intentionally disables its movement component; the adapter accepts interaction in that state.
- Five existing duck models have unique production data IDs 1001–1005, pickup, trait-controller compatibility, Rigidbody collision, and buoyancy. They are initially distributed over accessible floor locations in the main house and annex. This is an initial playable distribution, not a final hiding-difficulty pass. No placeholder test cubes or debug HUD were added.
- `VillaDuckReturn` subscribes to the existing buoyancy settled event and calls the teammate's `RubberDuckReturnRegistry`. Both pools share one collection of five ducks. Returning requires an unheld, stable floating duck inside the actual sampled water footprint. No broad rectangular return trigger is used.
- The registry retains progress/completion events for the UI/game flow owner. This integration does not implement the final collection screen, victory presentation, or duck-specific production traits.
- Existing APV, baked lightmaps, automatic day/night cycle, water shader, and repaired geometry are preserved. A gameplay flashlight has not been added; the old test flashlight stays removed.

## Validation and performance

The editor-only `VillaIntegrationChecks` exercises spawn grounding/jump, all doors opening and closing, pickup/drop, suspended buoyancy while held, delayed pool registration, both pools, completion and duplicate rejection. Results are in `checks.txt`. These checks are not attached to the scene and do not run during ordinary play.

Final Play-mode checks passed, including all five ducks floating and registered across the two pools, with zero runtime errors. Two `The referenced script (Unknown) on this Behaviour is missing!` warnings still occur during Play-mode scene loading. Both active-scene traversal (including inactive objects) and all loaded GameObjects report no missing MonoBehaviour components; the source is not yet established. These warnings were not suppressed or treated as resolved. Earlier failed pool test runs used edge/shallow-water placements and teleported a duck in the same physics step as release; the final test waits for release physics and selects spaced, deep interior locations.

Return integration adds event subscriptions rather than a new per-frame scan of the scene. Existing water footprint lookup, four-point buoyancy and lighting update throttling remain. The interaction outline renders only when a target is selected. No new performance benchmark or standalone player build was run; 1080p/60 FPS remains a target, not a newly measured result.
