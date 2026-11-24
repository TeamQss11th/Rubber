# Input capture recovery

The game scene's PlayerInputReader is enabled and its Player action map is enabled. An isolated Play-mode reproduction (without running door/collection checks) demonstrated a cursor-state mismatch: after an external unlock, `captured` stayed true while `Cursor.lockState` was None. Update rejected movement, but its click-to-capture branch required `captured == false`, so clicking could not recover input.

Before: injected W reached the Move action as (0, 1), but travel was 0.000 m. Click did not recapture. Escape followed by a click did recover, because Escape cleared the stale flag.

Fix: reconcile a lost cursor lock with the internal capture state at the start of Update. The next click then captures the cursor and is consumed without triggering an interaction. Move callbacks also require an actual locked cursor, matching the Update and jump gates.

After: unexpected unlock clears captured; click recaptures; W produces 0.729 m of movement over the short check. Escape releases and another click recaptures. This confirms a reproducible code defect; it does not prove which editor/OS action released the user's cursor originally.

Reports: `before.txt` and `checks.txt`. The editor-only check uses temporary Input System devices and removes them on completion. No test input handler was attached to the game scene. The change adds a constant-time state comparison, not an additional input system or scene scan.
