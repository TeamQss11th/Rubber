using System;
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
    public static class BeachVillaWalkthroughSetup
    {
        const string Report = "Docs/BeachVillaWalkthrough";
        [MenuItem("Rubber/Walkthrough/3 Play Mode Smoke Test")]
        public static void PlayModeSmokeTest()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play mode first.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            var body=walk.GetComponent<CharacterController>();
            var log=new StringBuilder($"Play mode=True; focused={Application.isFocused}; keyboard={UnityEngine.InputSystem.Keyboard.current != null}; mouse={UnityEngine.InputSystem.Mouse.current != null}; timeScale={Time.timeScale}\n");
            for(int i=0;i<walk.checkpoints.Length;i++)
            {
                walk.GoTo(i);Physics.SyncTransforms();
                for(int f=0;f<60;f++)body.Move(Vector3.down*.04f);
                log.AppendLine($"{walk.checkpoints[i].label}: grounded={body.isGrounded}; feet={walk.transform.position:F3}");
                if(!body.isGrounded)throw new InvalidOperationException("Runtime grounding failed");
            }
            walk.GoTo(0);Physics.SyncTransforms();
            var start=walk.transform.position;
            for(int f=0;f<30;f++)body.Move((walk.transform.forward*2.6f+Vector3.down*2)/60);
            log.AppendLine($"Forward movement distance={Vector3.Distance(start,walk.transform.position):F3}m (0.5 second simulation)");
            log.AppendLine($"Enabled cameras={UnityEngine.Object.FindObjectsByType<Camera>().Count(c=>c.isActiveAndEnabled)}");
            File.WriteAllText(Report+"/playmode-validation.txt",log.ToString());
            walk.GoTo(4);
        }
        [MenuItem("Rubber/Walkthrough/2 Validate Checkpoints")]
        public static void ValidateCheckpoints()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            if(!walk)throw new InvalidOperationException("Configure walkthrough first.");
            var body=walk.GetComponent<CharacterController>();
            var log=new StringBuilder();
            for(int i=0;i<walk.checkpoints.Length;i++)
            {
                var point=walk.checkpoints[i];
                body.enabled=false;
                try
                {
                    if(!FindFloor(point.feet+Vector3.up*.8f,out var feet,out var floor))throw new InvalidOperationException("No safe floor: "+point.label);
                    point.feet=feet;walk.checkpoints[i]=point;
                    log.AppendLine($"{point.label}: feet={feet:F3}; floor={floor}; capsule clear=True");
                }
                finally {body.enabled=true;}
                walk.GoTo(i);Physics.SyncTransforms();
                for(int f=0;f<90;f++)body.Move(Vector3.down*.04f);
                float drift=Mathf.Abs(walk.transform.position.y-point.feet.y);
                log.AppendLine($"Grounded={body.isGrounded}; drift={drift:F3}m");
                if(!body.isGrounded||drift>.15f)throw new InvalidOperationException("Grounding failed");
            }
            walk.GoTo(0);EditorUtility.SetDirty(walk);
            EditorSceneManager.MarkSceneDirty(walk.gameObject.scene);EditorSceneManager.SaveScene(walk.gameObject.scene);
            File.WriteAllText(Report+"/checkpoint-validation.txt",log.ToString());
        }
        [MenuItem("Rubber/Walkthrough/1 Configure and Validate")]
        public static void Configure()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Modern Villa/Scenes/Beach Villa.unity" || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open saved Beach Villa outside Play mode.");
            if (UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>()) throw new InvalidOperationException("Walkthrough already configured.");
            var camera = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single(c=>c.isActiveAndEnabled && c.CompareTag("MainCamera"));
            Physics.SyncTransforms();
            var hints = new[] { new Vector3(-4,1,-6), new Vector3(5,2,0), new Vector3(6,2,10), new Vector3(1,2,18), new Vector3(6,5,13), new Vector3(6,5,3) };
            var labels = new[] { "F1 / Pool terrace", "F2 / Entrance", "F3 / Interior", "F4 / Rear interior", "F5 / Upper floor", "F6 / Upper front" };
            var points = new BeachVillaWalkthrough.Checkpoint[hints.Length];
            var log = new StringBuilder("Checkpoint clearance checks (not proof of connected walking routes)\n");
            for (int i=0;i<hints.Length;i++)
            {
                if (!FindFloor(hints[i], out var feet, out var floor)) throw new InvalidOperationException("No safe floor near " + labels[i]);
                points[i] = new BeachVillaWalkthrough.Checkpoint { label=labels[i], feet=feet, yaw=i==0?30:0 };
                log.AppendLine($"{labels[i]}: feet={feet:F3}; floor={floor}; capsule clear=True");
            }
            Directory.CreateDirectory(Report);
            log.AppendLine($"Original camera: position={camera.transform.position:F3}; rotation={camera.transform.eulerAngles:F3}; near={camera.nearClipPlane}; FOV={camera.fieldOfView}");
            var rig = new GameObject("Beach Villa - Walkthrough"); Undo.RegisterCreatedObjectUndo(rig,"Configure walkthrough");
            rig.transform.SetPositionAndRotation(points[0].feet,Quaternion.Euler(0,points[0].yaw,0));
            var body = rig.AddComponent<CharacterController>(); body.height=1.8f;body.radius=.28f;body.center=new Vector3(0,.9f,0);body.stepOffset=.3f;body.slopeLimit=45;body.skinWidth=.03f;body.minMoveDistance=0;
            Undo.SetTransformParent(camera.transform,rig.transform,"Reuse main camera");Undo.RecordObject(camera.transform,"Set eye height");Undo.RecordObject(camera,"Set near clip");
            camera.transform.localPosition=new Vector3(0,1.65f,0);camera.transform.localRotation=Quaternion.identity;camera.nearClipPlane=.08f;
            var walk=rig.AddComponent<BeachVillaWalkthrough>();walk.view=camera.transform;walk.checkpoints=points;
            Physics.SyncTransforms();
            for(int i=0;i<points.Length;i++)
            {
                walk.GoTo(i);Physics.SyncTransforms();
                for(int frame=0;frame<90;frame++)body.Move(Vector3.down * .04f);
                bool grounded=body.isGrounded;float drift=Mathf.Abs(rig.transform.position.y-points[i].feet.y);
                log.AppendLine($"{labels[i]} gravity test: grounded={grounded}; vertical drift={drift:F3}m");
                if(!grounded || drift>.15f)throw new InvalidOperationException("Grounding validation failed: "+labels[i]);
            }
            walk.GoTo(0);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Report+"/setup-validation.txt",log.ToString());
        }
        static bool FindFloor(Vector3 hint,out Vector3 feet,out string floor)
        {
            for(int ring=0;ring<=5;ring++) for(int x=-ring;x<=ring;x++) for(int z=-ring;z<=ring;z++)
            {
                if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring)continue;
                var origin=hint+new Vector3(x*.5f,1,z*.5f);
                if(!Physics.Raycast(origin,Vector3.down,out var hit,2.5f,~0,QueryTriggerInteraction.Ignore) || hit.normal.y<.8f)continue;
                string surface=hit.collider.name.ToLowerInvariant();
                if(!(surface.Contains("floor")||surface.Contains("roof")||surface.Contains("carpet")||surface.Contains("terrain")||surface.Contains("stair")))continue;
                var p=hit.point+Vector3.up*.05f;
                if(Physics.CheckCapsule(p+Vector3.up*.28f,p+Vector3.up*1.52f,.28f,~0,QueryTriggerInteraction.Ignore))continue;
                feet=p;floor=hit.collider.name;return true;
            }
            feet=default;floor=null;return false;
        }
    }
}
