# Main menu villa backdrop

Open `Assets/_Project/Scenes/Main/MainMenu.unity` and press Play. The existing loading panel covers startup until the villa has loaded, rendered and completed its background reveal. Returning from gameplay keeps the same transition overlay visible until this readiness check passes. The editor scene itself does not contain a duplicate copy of the villa.

## Presentation

- Camera targets (-1.5, 1.5, 9), with radius 20 m, height 12 m and vertical FOV 48 degrees. A smooth 26-degree return arc takes 100 seconds per cycle. A restricted arc frames the two buildings and pools without circling into unfinished sides of the map. Existing terrain behind the buildings remains visible.
- The background alone receives two horizontal/vertical Gaussian blur iterations, with a cool white translucent frost layer, slightly whiter edges and subtle stationary grain. The old dark vignette is removed. Menu art and buttons render in a separate overlay and stay sharp.
- Existing morning/noon/sunset/night lightmaps and APV scenarios are reused. The menu starts at 09:00 and runs a 240-second day. The original dark night is retained. Gameplay lighting assets and timing are unchanged.
- The menu's old default directional light is disabled so it cannot illuminate the villa at night. Depth and opaque scene-color textures remain enabled for the pool shader.

## Isolation and cost controls

The preview disables villa gameplay scripts, gameplay canvases, player cameras and duplicate audio/event listeners. Rigidbodies become kinematic in the temporary loaded instance. Its collection progress does not run. These changes are runtime-only.

The source background now renders at screen resolution up to 1080 pixels high. Only the two reusable blur buffers use half width/height, with five bilinear samples per directional pass. This costs more than the previous half-resolution world rendering, but replaces the pixelated appearance with deliberate glass diffusion. Buffers are recreated only when resolution changes and released on exit. Blur processing happens after the completed URP frame without a second camera render or CPU readback; the UI displays the prior completed blurred frame. MSAA and camera post-processing remain disabled. Menu lightmap blending updates at 5 Hz; existing APV blending remains shared. The full villa assets are still loaded into memory. No new reflection camera is added.

Starting a game unloads the preview under the loading overlay before loading a fresh Modern Villa. This prevents two copies of the villa and avoids carrying menu time, disabled input or collection state into gameplay. Repeated transition requests while another loading overlay is active are ignored. Render textures and the runtime material are released when leaving the menu.

## Validation

`checks.txt` records Editor Play-mode verification of camera/time movement, disabled preview gameplay, free cursor, one EventSystem, render target size, Start-button raycasts, three game entries, two returns to menu, transition reentry protection and runtime errors. All checks passed. `view-0` through `view-4` capture the center and both arc ends plus sunset and night; screenshots were visually inspected.

Existing missing-script warnings on villa load are outside this change. No standalone build or hardware FPS benchmark was performed; 1080p/60 FPS is still a target, not a measured guarantee.
