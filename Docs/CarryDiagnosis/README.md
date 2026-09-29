# Held duck drift diagnosis

The Player test scene creates kinematic placeholder ducks with Rigidbody interpolation left at its default, None. The villa integration creates dynamic floating ducks with Interpolate for smooth water motion. The shared carry code set the duck kinematic and parented it to the camera anchor, but left interpolation enabled. The interpolated physics pose then conflicted with the moving camera hierarchy.

Controlled Play-mode comparison used the same real villa duck, the same carrier, and repeated player movement/camera rotation. Before the fix, held interpolation remained Interpolate, and the maximum local deviation from the anchor was 0.748408 m / 16.873 degrees. Disabling interpolation gave zero measured position and rotation deviation. The anchor offsets and model transforms were unchanged.

`RubberDuckInteractable` now remembers the existing interpolation setting, disables interpolation before parenting to the hold anchor, and restores it on release. Release also synchronizes the Rigidbody pose with the intended drop transform. Original pickup/drop API, model offsets, collision restoration and buoyancy logic are preserved.

After the fix, normal pickup uses None and measured local drift is zero. Forcing Interpolate during the diagnostic reproduces the fault (0.673088 m / 19.2433 degrees); reverting to None again gives zero drift. Release restores Interpolate. See `before.txt` and `checks.txt`.

There is no additional per-frame follow script or physics synchronization scan. This is a state-transition fix; unheld ducks retain their original interpolated physics motion. Diagnostic code lives under Editor and is not attached to the game scene. These measurements verify relative pose stability, not a subjective choice of held model orientation or screen position.
