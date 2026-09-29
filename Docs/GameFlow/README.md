# FinDuck game flow

`MainMenu` is the first enabled build scene; `Modern Villa` is the gameplay scene. Open `Assets/_Project/Scenes/Main/MainMenu.unity` and press Play, then Start.

## Play loop

1. The teammate's original menu and loading screen open Modern Villa asynchronously. Repeated Start clicks do not create duplicate transitions. Gameplay input is blocked until the loading overlay finishes.
2. Find and carry the 25 ducks to either pool. Existing stable-buoyancy returns drive the event-based counter (00 / 25). Duplicate IDs cannot count twice.
3. Esc opens a pause screen. Resume, the shared settings screen, restart, and main-menu navigation are wired. Pause blocks movement/interaction and stops physics and the session timer. Esc belongs to the game flow while this scene is running; the old cursor-release input does not compete with it.
4. The final return opens the completion screen with the total and session duration. The scene is paused and gameplay input is blocked. Play again loads a fresh 0/25 session; Main menu returns through the loading overlay.

The menu's settings art, controls, mixer reference, loading tips, logo and button sprite are retained. Camera sensitivity now uses the settings multiplier. Resolution defaults target 1920×1080, respecting a saved supported preference and choosing the nearest available size instead of silently falling back to the first/lowest resolution.

## Merge fixes

- Kept the existing shader-folder GUID when resolving the folder metadata conflict.
- Resolved Build Settings to menu first, villa second, SampleScene disabled.
- Moved the incoming custom `SceneManager` class into `Rubber.Core` so it cannot shadow Unity's SceneManager in existing map tools.
- Activated the menu canvas and left the incoming unfinished HUD prototype inactive in MainMenu. Reused its duck counter art for the villa HUD.
- Removed cross-scene camera references on copied gameplay canvases by using screen-space overlay canvases.
- Centralized Esc handling and modal input ownership. Settings/control-help back navigation unwinds one panel at a time.

## Validation

`checks.txt` records the Editor Play-mode checks: menu load, duplicate Start guard, HUD/input after loading, injected Esc pause/resume, paused timer, settings/back navigation, 25 progress events, completion, duplicate rejection, restart reset, menu return, and no duplicate EventSystem/scene-manager instances. Screenshots (`menu`, `gameplay`, `pause`, `settings`, `complete`) were inspected for layout and clipping.

The completion test calls the registry to isolate UI/game-flow behavior. Physical pickup, buoyancy, pool return, and all 25 hiding locations have separate previous validation; this is not a manual full-length playthrough of collecting all ducks. No standalone Windows build or new FPS benchmark was run. Existing unknown-script scene-load warnings recur when the villa loads and remain separately unresolved; the flow test checks runtime errors independently.

## Performance and scope

The counter changes only on registry events, and scene transitions are asynchronous. There is no new every-frame scan for ducks. Existing water, APV, lightmap and shadow settings are preserved. Test code is editor-only and not attached to normal scenes.

This is a complete first single-session loop, not a claim that all production features are finished: save/continue, final audio/feedback, duck-specific production traits, final visual polish and difficulty balancing are not newly implemented here. The old test flashlight remains removed; a production nighttime flashlight still needs its own gameplay integration.
