# RubberDuck Crown

- `RubberDuck_Crown.blend`: editable Blender scene, original duck meshes and materials, separate `Crown_Gold` accessory, packed crown texture, and preview stage.
- `RubberDuck_Crown.fbx`: only `RubberDuck` and `Crown_Gold` meshes, including materials and embedded crown base-color texture. Studio objects and hidden editable duplicate meshes are excluded.
- `Crown_Gold_BaseColor.png`: external copy of the crown's baked 1024 x 1024 color texture.
- `RubberDuck_Crown_preview.png` / `RubberDuck_Crown_front.png`: rendered previews.
- `geometry_validation.json`: original geometry comparison and FBX reimport results.

The original duck's shape, topology, transforms, eyes, beak, and yellow material are preserved. Only the crown accessory is new. It has five curved points, round finials, and a rolled gold base inspired by the supplied image. It sits 1.2 cm to the duck's right of the centerline and tilts 16 degrees sideways and 5 degrees forward/backward, on the original 20 cm-tall model.

Blender version: 5.2.2 LTS. FBX orientation: -Z forward, Y up. The crown material uses a baked gold base color, metallic 0.48, and roughness 0.29. Material rendering can vary by FBX importer; the PNG is provided for manual material assignment if needed.

The source `C:\Users\user\Downloads\RubberDuck.blend` was read without enabling embedded scripts and was not overwritten.
