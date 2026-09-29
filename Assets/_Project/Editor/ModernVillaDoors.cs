using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubber.World;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace Rubber.EditorTools
{
    public static class ModernVillaDoors
    {
        const string Report = "Docs/ModernVillaDoors";
        [MenuItem("Rubber/Modern Doors/Install Sliding Doors %&d")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || Lightmapping.isRunning || scene.path != ModernVillaSetup.ScenePath) throw new System.InvalidOperationException("Open Modern Villa in Edit mode first.");
            if (Object.FindAnyObjectByType<VillaDoorTestControls>()) return;
            Directory.CreateDirectory(Report);
            File.Copy(scene.path, Report + "/BeforeDoors.unity.backup", true);
            var leaves = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m => m.name == "Window door door" || m.sharedMesh && (m.sharedMesh.name.ToLowerInvariant().Contains("interior") || m.sharedMesh.name.ToLowerInvariant().Contains("exterior")) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m).EndsWith("Door.prefab")).ToArray();
            var doors = new System.Collections.Generic.List<VillaSlidingDoor>();
            var moving = new System.Collections.Generic.HashSet<Renderer>();
            var report = new StringBuilder();
            foreach (var leaf in leaves)
            {
                var t = leaf.transform;
                if (t.GetComponent<VillaSlidingDoor>()) continue;
                Undo.RecordObject(t.gameObject, "Configure sliding door");
                if (leaf.name == "Window door door")
                {
                    // Replace the solid wall-sized box with the actual frame geometry.
                    var frame = t.parent;
                    foreach (var c in frame.GetComponents<Collider>()) { Undo.RecordObject(c, "Door frame collision"); c.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
                    var fc = Undo.AddComponent<MeshCollider>(frame.gameObject); fc.sharedMesh = frame.GetComponent<MeshFilter>().sharedMesh;
                    foreach (var m in frame.GetComponentsInChildren<MeshFilter>().Where(m => m.name == "Window door window" || m.name == "Window door windows side"))
                    { var c = Undo.AddComponent<MeshCollider>(m.gameObject); c.sharedMesh = m.sharedMesh; }
                }
                if (!t.GetComponent<Collider>())
                {
                    var c = Undo.AddComponent<BoxCollider>(t.gameObject); c.center = leaf.sharedMesh.bounds.center;
                    var size = leaf.sharedMesh.bounds.size; size.z = Mathf.Max(size.z, .04f); c.size = size;
                }
                var body = Undo.AddComponent<Rigidbody>(t.gameObject); body.isKinematic = true; body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                var d = Undo.AddComponent<VillaSlidingDoor>(t.gameObject);
                d.width = leaf.sharedMesh.bounds.size.x * Mathf.Abs(t.localScale.x); d.travelRatio = 2f / 3f; d.duration = 2;
                if (leaf.name != "Window door door")
                {
                    d.openingStyle = VillaSlidingDoor.OpeningStyle.Hinged;
                    d.openingAngle = leaf.name == "interior door" || t.root.name == "Main Building" ? -90 : 90;
                }
                var center = t.TransformPoint(leaf.sharedMesh.bounds.center);
                d.closedCenter = t.parent ? t.parent.InverseTransformPoint(center) : center;
                if (leaf.name == "Window door door") d.companion = t.parent.Find("Window door knob");
                foreach (var r in t.GetComponentsInChildren<Renderer>()) moving.Add(r);
                if (d.companion) foreach (var r in d.companion.GetComponentsInChildren<Renderer>()) moving.Add(r);
                doors.Add(d);
                report.AppendLine($"{PathOf(t)} width={d.width:F4} travel={d.width*d.travelRatio:F4} duration={d.duration} local-left; narrow={d.width*d.travelRatio < .56f}");
            }
            if (doors.Count == 0) throw new System.InvalidOperationException("No supported door leaves found");
            foreach (var r in moving)
            {
                Undo.RecordObject(r, "Moving door probe lighting"); Undo.RecordObject(r.gameObject, "Moving door static flags");
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, 0);
                r.lightmapIndex = -1; r.lightProbeUsage = LightProbeUsage.BlendProbes;
                if (r is MeshRenderer mr) mr.receiveGI = ReceiveGI.LightProbes;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r); PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
            }
            var lighting = Object.FindAnyObjectByType<ModernVillaDayNight>();
            if (lighting) { Undo.RecordObject(lighting, "Exclude moving doors from night lightmaps"); lighting.nightRenderers = lighting.nightRenderers.Where(b => !moving.Contains(b.renderer)).ToArray(); }
            var walk = Object.FindAnyObjectByType<ModernVillaWalkthrough>();
            var test = Undo.AddComponent<VillaDoorTestControls>(walk.gameObject); test.view = walk.view;
            test.doors = doors.OrderBy(d => PathOf(d.transform)).ToArray();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Report + "/installed.txt", report + $"\nDoors={doors.Count}; moving renderers={moving.Count}; baked lighting NOT regenerated.\n");
        }
        [MenuItem("Rubber/Modern Doors/Inspect")]
        public static void Survey()
        {
            if (SceneManager.GetActiveScene().path != ModernVillaSetup.ScenePath) return;
            var b = new StringBuilder();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.ToLowerInvariant().Contains("door")))
            {
                b.AppendLine($"{t.GetInstanceID()} {PathOf(t)} pos={t.position} rot={t.eulerAngles} scale={t.lossyScale} prefab={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t)}");
                foreach (var m in t.GetComponentsInChildren<MeshFilter>()) b.AppendLine($"  MESH {PathOf(m.transform)} local={m.sharedMesh.bounds} world={m.GetComponent<Renderer>()?.bounds}");
                foreach (var c in t.GetComponentsInChildren<Collider>()) b.AppendLine($"  COL {PathOf(c.transform)} {c.GetType().Name} {c.bounds}");
            }
            Directory.CreateDirectory(Report); File.WriteAllText(Report + "/survey.txt", b.ToString());
        }
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    }
}
