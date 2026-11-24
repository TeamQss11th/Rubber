using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEngine;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class ModernVillaDoorChecks
    {
        static VillaSlidingDoor[] doors;
        static Vector3[] starts, knobs;
        static Quaternion[] rotations;
        static int[] opened, closed;
        static int stage;
        static double next;
        static StringBuilder report;
        static ModernVillaDoorChecks() { EditorApplication.playModeStateChanged += Changed; }
        [MenuItem("Rubber/Modern Doors/Run Play Checks %&e")]
        public static void Run()
        {
            if (Application.isPlaying) return;
            SessionState.SetBool("VillaDoorChecks", true); EditorApplication.isPlaying = true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VillaDoorChecks", false))
            { SessionState.SetBool("VillaDoorChecks", false); stage = 0; next = EditorApplication.timeSinceStartup + 1; report = new StringBuilder(); EditorApplication.update += Tick; }
            if (state == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Tick;
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (stage == 0)
                {
                    doors = Object.FindAnyObjectByType<VillaDoorTestControls>().doors;
                    starts = doors.Select(d => d.transform.position).ToArray();
                    rotations = doors.Select(d => d.transform.rotation).ToArray();
                    knobs = doors.Select(d => d.companion ? d.companion.position : Vector3.zero).ToArray();
                    opened = new int[doors.Length]; closed = new int[doors.Length];
                    for (int i = 0; i < doors.Length; i++) { int index = i; doors[i].onOpened.AddListener(() => opened[index]++); doors[i].onClosed.AddListener(() => closed[index]++); doors[i].Open(); doors[i].Open(); }
                    next = EditorApplication.timeSinceStartup + 2.6;
                }
                else if (stage == 1)
                {
                    Physics.SyncTransforms();
                    for (int i = 0; i < doors.Length; i++)
                    {
                        var d = doors[i]; var displacement = d.transform.position - starts[i];
                        bool hinged = d.openingStyle == VillaSlidingDoor.OpeningStyle.Hinged;
                        var expected = hinged ? Vector3.zero : -(rotations[i] * Vector3.right) * d.width * d.travelRatio;
                        var expectedRotation = hinged ? rotations[i] * Quaternion.Euler(0,d.openingAngle,0) : rotations[i];
                        report.AppendLine($"{d.transform.root.name}/{d.name} {d.openingStyle}: open error={Vector3.Distance(displacement,expected):F6}, rotation error={Quaternion.Angle(d.transform.rotation,expectedRotation):F6}, event={opened[i]}, idle={!d.IsMoving}, knob error={(d.companion ? Vector3.Distance(d.companion.position-knobs[i], displacement) : 0):F6}");
                        var mf = d.GetComponent<MeshFilter>();
                        var center = d.ClosedCenter + (hinged ? Vector3.zero : rotations[i] * Vector3.right * (d.width * (1-d.travelRatio)*.5f));
                        var forward = rotations[i] * Vector3.forward;
                        var feet = new Vector3(center.x, starts[i].y + .33f, center.z) + forward * .5f;
                        var hits = Physics.CapsuleCastAll(feet, feet + Vector3.up * 1.14f, .28f, -forward, 1, ~0, QueryTriggerInteraction.Ignore);
                        report.AppendLine("  passage capsule hits=" + string.Join(",", hits.Select(h => h.collider.name)));
                    }
                    Object.FindAnyObjectByType<ModernVillaDayNight>().SetNight(true);
                    foreach (var d in doors) d.Close();
                    next = EditorApplication.timeSinceStartup + .6;
                }
                else if (stage == 2) { foreach (var d in doors) d.Open(); next = EditorApplication.timeSinceStartup + 1; }
                else if (stage == 3)
                {
                    foreach (var d in doors)
                    {
                        report.AppendLine($"{d.name}: reversed={d.Progress == 1}, night map={d.GetComponent<Renderer>().lightmapIndex}, batched={d.GetComponent<Renderer>().isPartOfStaticBatch}");
                        d.Close(); d.Close();
                    }
                    next = EditorApplication.timeSinceStartup + 2.6;
                }
                else
                {
                    for (int i=0;i<doors.Length;i++) report.AppendLine($"{doors[i].name}: close error={Vector3.Distance(doors[i].transform.position,starts[i]):F6}, rotation error={Quaternion.Angle(doors[i].transform.rotation,rotations[i]):F6}, closed events={closed[i]}, opened events={opened[i]}, idle={!doors[i].IsMoving}");
                    Object.FindAnyObjectByType<ModernVillaDayNight>().SetNight(false);
                    File.WriteAllText("Docs/ModernVillaDoors/playchecks.txt", report.ToString());
                    EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
                }
                stage++;
            }
            catch (System.Exception e) { File.WriteAllText("Docs/ModernVillaDoors/playchecks.txt", report + "\nFAILED " + e); EditorApplication.update -= Tick; EditorApplication.isPlaying = false; }
        }
    }
}
