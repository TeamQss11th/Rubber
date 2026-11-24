# Modern Villa scene relocation

Moved the existing scene with Unity AssetDatabase.MoveAsset to `Assets/_Project/Scenes/Modern Villa.unity`. The scene GUID remains `c5c5438aec093734ca0f431d36c22f40`; no scene duplication or rebake was performed.

- Updated Build Settings and editor tools that used the original scene path.
- Reopened the scene and verified the complete asset dependency list was unchanged, the LightingData asset reference was unchanged, and two lightmaps remained assigned. The APV BakingSet still references the same scene GUID.
- Re-ran the Player integration checks from the relocated scene: grounding/jump, eight doors, pickup/drop, both pools, all five returns, duplicate prevention and zero runtime errors passed. Results are copied to `play-checks.txt`.
- Two pre-existing Unknown/missing-script warnings recur when entering Play mode, as they did before the move. No missing components were found on the scene or loaded GameObjects. Their source remains unresolved; the relocation did not eliminate them. No new missing asset/path errors were observed.
- Restored the APV BakingSet's edit-time scenario to its pre-test `Default` value after the runtime check changed it to Morning.

This is a folder/reference maintenance change; rendering features and runtime performance settings are unchanged. Historical audit reports retain the paths valid when those reports were produced.
