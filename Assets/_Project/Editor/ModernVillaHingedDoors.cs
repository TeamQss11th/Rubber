using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class ModernVillaHingedDoors
    {
        const string Request = "Library/ModernVillaHingedDoors.request";
        static ModernVillaHingedDoors() { EditorApplication.delayCall += ProcessRequest; }
        static void ProcessRequest()
        {
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            try { Apply(); ModernVillaDoorChecks.Run(); }
            catch (System.Exception e) { File.WriteAllText("Docs/ModernVillaDoors/hinged-error.txt", e.ToString()); }
        }
        [MenuItem("Rubber/Modern Doors/Configure Inward Hinged Doors")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || Lightmapping.isRunning || scene.isDirty || scene.path != ModernVillaSetup.ScenePath)
                throw new System.InvalidOperationException("Saved Modern Villa must be open in Edit mode, with no bake running.");
            var doors = Object.FindObjectsByType<VillaSlidingDoor>().Where(d => d.name == "interior door" || d.name == "exterior Door").ToArray();
            if (doors.Length != 3) throw new System.InvalidOperationException("Expected exactly three existing hinged door leaves.");
            File.Copy(scene.path, "Docs/ModernVillaDoors/BeforeHingedDoors.unity.backup", true);
            var report = new StringBuilder();
            foreach (var d in doors)
            {
                Undo.RecordObject(d, "Inward hinged door");
                d.openingStyle = VillaSlidingDoor.OpeningStyle.Hinged;
                // Main west facade opens east; annex west facade also opens east.
                // Internal annex door opens north into the upper bedroom.
                d.openingAngle = d.name == "interior door" ? -90 : d.transform.root.name == "Main Building" ? -90 : 90;
                report.AppendLine($"{d.transform.root.name}/{d.name} hinge={d.transform.position} yaw={d.transform.eulerAngles.y} angle={d.openingAngle} duration={d.duration}");
                EditorUtility.SetDirty(d);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Docs/ModernVillaDoors/hinged-installed.txt", report.ToString());
        }
    }
}
