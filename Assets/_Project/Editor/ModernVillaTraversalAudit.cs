using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubber.World;

namespace Rubber.EditorTools
{
    public static class ModernVillaTraversalAudit
    {
        static string Folder=>ModernVillaSurfaceAudit.Folder;
        public static bool Ground(Collider c)
        {
            if(c is TerrainCollider)return true;
            var m=c.GetComponent<MeshFilter>();string n=(c.name+" "+(m?m.sharedMesh.name:"")).ToLowerInvariant();
            return !n.Contains("lamp")&&(n.Contains("floor")||n.Contains("roof")||n.Contains("step")||n.Contains("stair")||n.Contains("pool bottom")||n.Contains("pool straight top")||n.Contains("pool corner top"));
        }
        public static void Run()
        {
            if(Application.isPlaying||SceneManager.GetActiveScene().path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa Edit mode required");
            Physics.SyncTransforms();
            var points=new List<Vector3>();var clear=new List<Vector3>();var grid=new StringBuilder("x\tz\ty\tsurface\n");var failures=new StringBuilder();
            var template=UnityEngine.Object.FindAnyObjectByType<ModernVillaWalkthrough>().GetComponent<CharacterController>();
            var temp=new GameObject("Surface validation capsule (temporary)"){hideFlags=HideFlags.HideAndDontSave};var body=temp.AddComponent<CharacterController>();
            body.height=template.height;body.radius=template.radius;body.center=template.center;body.stepOffset=template.stepOffset;body.slopeLimit=template.slopeLimit;body.skinWidth=template.skinWidth;body.minMoveDistance=0;
            body.enabled=false;int ground=0,excluded=0,failed=0;
            try
            {
                for(float x=-17.875f;x<13;x+=.5f)for(float z=-4.875f;z<33;z+=.5f)
                {
                    var hits=Physics.RaycastAll(new Vector3(x,8,z),Vector3.down,12,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.7f&&Ground(h.collider)&&h.point.y<4.8f).OrderByDescending(h=>h.point.y).ToArray();
                    foreach(var hit in hits)
                    {
                        var p=hit.point;if(points.Any(v=>Mathf.Abs(v.x-p.x)<.001f&&Mathf.Abs(v.z-p.z)<.001f&&Mathf.Abs(v.y-p.y)<.04f))continue;
                        points.Add(p);grid.AppendLine($"{x:F3}\t{z:F3}\t{p.y:F3}\t{ModernVillaSurfaceAudit.PathOf(hit.collider.transform)}");ground++;
                        body.enabled=false;
                        if(Physics.CheckCapsule(p+Vector3.up*.34f,p+Vector3.up*1.57f,.27f,~0,QueryTriggerInteraction.Ignore)){excluded++;continue;}
                        clear.Add(p);
                        body.transform.position=p+Vector3.up*.05f;body.enabled=true;Physics.SyncTransforms();
                        float velocity=0;for(int k=0;k<75;k++){if(body.isGrounded&&velocity<0)velocity=-2;velocity+=Physics.gravity.y/60;body.Move(Vector3.up*velocity/60);}
                        if(!body.isGrounded||Mathf.Abs(body.transform.position.y-p.y)>.13f){failed++;failures.AppendLine($"Standing failure {p:F3} final={body.transform.position:F3} grounded={body.isGrounded} surface={ModernVillaSurfaceAudit.PathOf(hit.collider.transform)}");}
                    }
                }
                body.enabled=false;
                int crossings=0,crossFalls=0,blocked=0;var seamReport=new StringBuilder();
                foreach(var start in clear)foreach(var axis in new[]{Vector3.right,Vector3.forward})
                {
                    var wanted=start+axis*.5f;var targets=clear.Where(p=>Mathf.Abs(p.x-wanted.x)<.01f&&Mathf.Abs(p.z-wanted.z)<.01f&&Mathf.Abs(p.y-start.y)<.08f).ToArray();if(targets.Length==0)continue;
                    var end=targets[0];body.enabled=false;body.transform.position=start+Vector3.up*.05f;body.enabled=true;Physics.SyncTransforms();float velocity=0;bool fell=false;
                    for(int k=0;k<50;k++){if(body.isGrounded&&velocity<0)velocity=-2;velocity+=Physics.gravity.y/60;var delta=end-body.transform.position;delta.y=0;body.Move(Vector3.ClampMagnitude(delta,2.6f/60)+Vector3.up*velocity/60);if(body.transform.position.y<Mathf.Min(start.y,end.y)-.2f)fell=true;}
                    crossings++;if(fell){crossFalls++;seamReport.AppendLine($"FALL {start:F3} -> {end:F3} final={body.transform.position:F3}");}else if(Vector2.Distance(new Vector2(body.transform.position.x,body.transform.position.z),new Vector2(end.x,end.z))>.15f)blocked++;
                    body.enabled=false;
                }
                File.WriteAllText(Folder+"/seams.txt",$"Adjacent level supports, 0.5m grid. Crossings={crossings}; falls={crossFalls}; blocked by geometry={blocked}\n"+seamReport);
                var terrainReport=new StringBuilder();foreach(var terrain in UnityEngine.Object.FindObjectsByType<Terrain>()){var d=terrain.terrainData;var holes=d.GetHoles(0,0,d.holesResolution,d.holesResolution);int holesCount=0;foreach(bool solid in holes)if(!solid)holesCount++;terrainReport.AppendLine($"{ModernVillaSurfaceAudit.PathOf(terrain.transform)}: holes={holesCount}/{holes.Length}");}File.WriteAllText(Folder+"/terrain.txt",terrainReport.ToString());
                var routes=new[]{
                    new[]{new Vector3(3.75f,.456f,16.5f),new Vector3(3.75f,3.05f,12.5f)},
                    new[]{new Vector3(3.75f,.456f,16.5f),new Vector3(4.75f,.25f,16.5f)},
                    new[]{new Vector3(7.5f,-.24f,6.2f),new Vector3(7.5f,.25f,7.7f)},
                    new[]{new Vector3(4.15f,.25f,8.748f),new Vector3(2.6f,.75f,8.748f)},
                    new[]{new Vector3(1.57f,.25f,12.2f),new Vector3(1.57f,.75f,10.2f)},
                    new[]{new Vector3(.9f,.25f,26.5f),new Vector3(-3.8f,-1.633f,26.5f)}
                };
                var names=new[]{"annex stairs","annex lower landing turn","pool-front steps","terrace side steps","backyard steps","pool basin steps"};
                var routeReport=new StringBuilder();
                for(int i=0;i<routes.Length;i++)
                {
                    for(int direction=0;direction<2;direction++)
                    {
                        var start=routes[i][direction];var end=routes[i][1-direction];body.enabled=false;body.transform.position=start+Vector3.up*.05f;body.enabled=true;Physics.SyncTransforms();float velocity=0,minY=start.y;
                        var delta=end-start;delta.y=0;int frames=Mathf.CeilToInt(delta.magnitude/1.5f*60)+30;var move=delta.normalized*1.5f/60;
                        for(int k=0;k<frames+120;k++){if(body.isGrounded&&velocity<0)velocity=-2;velocity=Mathf.Max(velocity+Physics.gravity.y/60,-30);var remaining=end-body.transform.position;remaining.y=0;body.Move(Vector3.ClampMagnitude(remaining,1.5f/60)+Vector3.up*velocity/60);minY=Mathf.Min(minY,body.transform.position.y);}
                        var final=body.transform.position;routeReport.AppendLine($"{names[i]} {(direction==0?"forward":"reverse")}: start={start:F3} target={end:F3} final={final:F3} horizontalError={Vector2.Distance(new Vector2(final.x,final.z),new Vector2(end.x,end.z)):F3} grounded={body.isGrounded} minY={minY:F3}");
                        if(Vector2.Distance(new Vector2(final.x,final.z),new Vector2(end.x,end.z))>.15f){var probe=final+(end-start).normalized*.1f;foreach(var obstacle in Physics.OverlapCapsule(probe+Vector3.up*.31f,probe+Vector3.up*1.52f,.28f,~0,QueryTriggerInteraction.Ignore))routeReport.AppendLine("  BLOCKER "+ModernVillaSurfaceAudit.PathOf(obstacle.transform)+" "+obstacle.bounds);}
                        body.enabled=false;
                    }
                    for(int k=0;k<=20;k++)
                    {
                        var p=Vector3.Lerp(routes[i][0],routes[i][1],k/20f);
                        var hs=Physics.RaycastAll(new Vector3(p.x,5,p.z),Vector3.down,8,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.65f&&Ground(h.collider)).OrderByDescending(h=>h.point.y);
                        routeReport.AppendLine($"  PROFILE {names[i]} x={p.x:F3} z={p.z:F3}: "+string.Join(";",hs.Select(h=>$"{h.point.y:F3}:{h.collider.name}")));
                    }
                }
                File.WriteAllText(Folder+"/routes.txt",routeReport.ToString());
                File.WriteAllText(Folder+"/standing.txt",$"Grid 0.5m, x[-17.875,13), z[-4.875,33), all support layers below 4.8m. Controller h={body.height}, r={body.radius}, step={body.stepOffset}, slope={body.slopeLimit}.\nSupport nodes={ground}; obstructed/excluded={excluded}; clear standing tests={ground-excluded}; failures={failed}\n"+failures);
                File.WriteAllText(Folder+"/ground-grid.tsv",grid.ToString());
            }
            finally{UnityEngine.Object.DestroyImmediate(temp);}
        }
    }
}


