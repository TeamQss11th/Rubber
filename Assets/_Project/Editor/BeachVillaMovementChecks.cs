using System;
using System.IO;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEngine;
namespace Rubber.EditorTools
{
    public static class BeachVillaMovementChecks
    {
        [MenuItem("Rubber/Expansion/7 Check Character Movement")]
        public static void Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Run outside Play.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();var body=walk.GetComponent<CharacterController>();var saved=walk.transform.position;
            var log=new StringBuilder("CharacterController.Move, fixed 1/60 step, 2.6m/s, gravity; each route starts independently. Not an exhaustive playtest.\n");
            var routes=new[]{("west spine",new Vector3(-20,.105f,-12),new Vector3(-20,.105f,32)),("east spine",new Vector3(21,.105f,-12),new Vector3(21,.105f,35)),("north return",new Vector3(-20,.105f,36),new Vector3(21,.105f,36)),("south return",new Vector3(-20,.105f,-10),new Vector3(21,.105f,-10)),("main front door",new Vector3(6.8f,1.055f,2),new Vector3(6.8f,1.055f,6)),("main rear door",new Vector3(7.2f,1.055f,20),new Vector3(7.2f,1.055f,24)),("guest entrance",new Vector3(23,.105f,18),new Vector3(26.8f,.105f,18)),("pool return",new Vector3(-20,.105f,19),new Vector3(-16,.105f,19))};
            try
            {
                foreach(var r in routes)
                {
                    body.enabled=false;walk.transform.position=r.Item2;body.enabled=true;Physics.SyncTransforms();
                    int stalled=0;float speedY=0;int steps=0;
                    for(;steps<2400;steps++)
                    {
                        var delta=r.Item3-walk.transform.position;delta.y=0;if(delta.magnitude<.12f)break;
                        if(body.isGrounded && speedY<0)speedY=-2;speedY=Mathf.Max(speedY-9.81f/60,-30);
                        var before=walk.transform.position;body.Move(Vector3.ClampMagnitude(delta,2.6f/60)+Vector3.up*(speedY/60));Physics.SyncTransforms();
                        if(Vector2.Distance(new Vector2(before.x,before.z),new Vector2(walk.transform.position.x,walk.transform.position.z))<.001f)stalled++;else stalled=0;
                        if(stalled>60 || walk.transform.position.y< -6)break;
                    }
                    // Finish inside the guest doorway, before its tea-table collision.
                    var goal=r.Item3;
                    float distance=Vector2.Distance(new Vector2(goal.x,goal.z),new Vector2(walk.transform.position.x,walk.transform.position.z));
                    log.AppendLine($"{r.Item1}: pass={distance<.15f}; remaining={distance:F3}; end={walk.transform.position:F3}; steps={steps}");
                }
            }
            finally{body.enabled=false;walk.transform.position=saved;body.enabled=true;Physics.SyncTransforms();File.WriteAllText("Docs/BeachVillaExpansion/movement.txt",log.ToString());}
        }
    }
}
