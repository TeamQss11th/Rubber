using System;
using System.IO;
using System.Linq;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class VillaTimeBake
    {
        const string Request="Library/VillaTimeBake.request", Report="Docs/VillaTimeOfDay", Root="Assets/_Project/Lighting/ModernVilla";
        static string folder;
        static int phase;
        static bool active;
        static double started;
        static VillaTimeOfDay cycle;
        static ModernVillaDayNight old;
        static ProbeVolumeBakingSet set;
        static ModernVillaDayNight.BakedRenderer[] canonical;
        static ModernVillaDayNight.BakedTerrain[] terrainCanonical;
        static LightmapData[] dayMaps;
        static LightingDataAsset dayData;
        static Material savedSky;
        static AmbientMode savedAmbientMode;
        static Color savedAmbient,savedFog;
        static float savedAmbientIntensity,savedReflection;
        static bool[] lightEnabled;
        static Color[] lightColors;
        static float[] lightIntensity;
        static Quaternion savedSunRotation;
        static VillaTimeOfDay.LightingState[] states;
        static VillaTimeBake(){EditorApplication.delayCall+=RequestBake;}
        static void RequestBake()
        {
            if(!File.Exists(Request))return;File.Delete(Request);
            try{Start();}catch(Exception e){Directory.CreateDirectory(Report);File.WriteAllText(Report+"/error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Rubber/Time Of Day/Bake Four Lighting States")]
        public static void Start()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ModernVillaSetup.ScenePath || Application.isPlaying || Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning)throw new InvalidOperationException("Modern Villa must be open outside Play and bake.");
            Directory.CreateDirectory(Report);
            File.Copy(scene.path,Report+"/BeforeTimeOfDay-disk.unity.backup",true);
            if(scene.isDirty && !EditorSceneManager.SaveScene(scene))throw new IOException("Cannot preserve current scene edits.");
            File.Copy(scene.path,Report+"/BeforeTimeOfDay.unity.backup",true);
            string backup="Library/VillaTimeBackup-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
            foreach(var source in Directory.GetFiles(Root,"*",SearchOption.TopDirectoryOnly))File.Copy(source,backup+"/"+Path.GetFileName(source));
            File.WriteAllText(Report+"/bake.txt","START "+DateTime.UtcNow.ToString("O")+"\nAPV backup="+backup+"\n");
            old=Object.FindAnyObjectByType<ModernVillaDayNight>();
            cycle=old.GetComponent<VillaTimeOfDay>();if(!cycle)cycle=old.gameObject.AddComponent<VillaTimeOfDay>();
            cycle.enabled=false;cycle.sun=old.sceneLights.First(l=>l && l.type==LightType.Directional);
            cycle.blender=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/_Project/Scripts/World/VillaLightmapBlend.compute");
            if(!cycle.blender)throw new InvalidOperationException("Missing blend compute shader");
            folder=AssetDatabase.GenerateUniqueAssetPath(Root+"/TimeOfDay");Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            savedSky=RenderSettings.skybox;savedAmbientMode=RenderSettings.ambientMode;savedAmbient=RenderSettings.ambientLight;savedAmbientIntensity=RenderSettings.ambientIntensity;savedReflection=RenderSettings.reflectionIntensity;savedFog=RenderSettings.fogColor;
            savedSunRotation=cycle.sun.transform.rotation;
            lightEnabled=old.sceneLights.Select(l=>l&&l.enabled).ToArray();lightColors=old.sceneLights.Select(l=>l?l.color:Color.white).ToArray();lightIntensity=old.sceneLights.Select(l=>l?l.intensity:0).ToArray();
            states=new[]{
                Make("Default",new Vector3(65,-30,0),new Color(1,.96f,.88f),1.2f,1,new Color(.45f,.55f,.65f),.65f),
                Make("Morning",new Vector3(18,75,0),new Color(1,.79f,.55f),.9f,.8f,new Color(.5f,.5f,.6f),.45f),
                Make("Sunset",new Vector3(6,-90,0),new Color(1,.38f,.12f),.65f,.5f,new Color(.65f,.36f,.24f),.22f),
                Make("Night",new Vector3(-20,-90,0),new Color(.2f,.3f,.5f),0,.001f,new Color(.15f,.2f,.3f),0)
            };
            set=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Root+"/BakingSet.asset");
            foreach(var state in states)if(!set.lightingScenarios.Contains(state.scenario))set.TryAddScenario(state.scenario);
            var pipeline=new SerializedObject(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"));pipeline.FindProperty("m_SupportProbeVolumeScenarioBlending").boolValue=true;pipeline.ApplyModifiedPropertiesWithoutUndo();
            // All four states share geometry, packing inputs, and probe placement.
            foreach(var d in Object.FindObjectsByType<VillaSlidingDoor>(FindObjectsSortMode.None))foreach(var r in d.GetComponentsInChildren<MeshRenderer>())
            {GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);r.receiveGI=ReceiveGI.LightProbes;r.lightmapIndex=-1;}
            phase=0;active=true;Lightmapping.bakeCompleted+=Completed;EditorApplication.update+=Watch;
            BeginPhase();
        }
        static VillaTimeOfDay.LightingState Make(string name,Vector3 angles,Color color,float power,float exposure,Color tint,float reflection)
        {
            var sky=new Material(savedSky){name="Villa "+name+" Sky"};sky.SetFloat("_Exposure",exposure);sky.SetColor("_SkyTint",tint);
            AssetDatabase.CreateAsset(sky,folder+"/"+name+"Sky.mat");
            return new VillaTimeOfDay.LightingState{scenario=name,sky=sky,sunAngles=angles,sunColor=color,sunIntensity=power,reflection=reflection};
        }
        static void BeginPhase()
        {
            var s=states[phase];var so=new SerializedObject(set);so.FindProperty("lightingScenario").stringValue=s.scenario;so.FindProperty("freezePlacement").boolValue=phase!=0;so.ApplyModifiedPropertiesWithoutUndo();
            cycle.sun.transform.rotation=Quaternion.Euler(s.sunAngles);cycle.sun.color=s.sunColor;cycle.sun.intensity=s.sunIntensity;cycle.sun.enabled=s.sunIntensity>0;
            cycle.sun.lightmapBakeType=LightmapBakeType.Mixed;
            foreach(var l in old.sceneLights)if(l && l!=cycle.sun){l.lightmapBakeType=LightmapBakeType.Baked;l.color=new Color(1,.67f,.37f);}
            RenderSettings.skybox=s.sky;RenderSettings.sun=cycle.sun;RenderSettings.reflectionIntensity=s.reflection;
            RenderSettings.ambientMode=phase==3?AmbientMode.Flat:AmbientMode.Skybox;RenderSettings.ambientIntensity=phase==3?0:1;
            RenderSettings.ambientLight=phase==3?new Color(.0001f,.00015f,.00025f):savedAmbient;RenderSettings.fogColor=phase==3?Color.black:savedFog;
            AssetDatabase.SaveAssets();started=EditorApplication.timeSinceStartup;
            File.AppendAllText(Report+"/bake.txt","BEGIN "+s.scenario+"\n");
            if(!Lightmapping.BakeAsync())Fail(new InvalidOperationException("Bake did not start: "+s.scenario));
        }
        static void Watch()
        {
            if(active && EditorApplication.timeSinceStartup-started>600)Fail(new TimeoutException("Lighting bake exceeded ten minutes; previous bake assets preserved in Library backup."));
        }
        static void Completed(){EditorApplication.delayCall+=Finish;}
        static void Finish()
        {
            if(!active)return;
            if(Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning){EditorApplication.delayCall+=Finish;return;}
            try
            {
                if(LightmapSettings.lightmaps.Length==0 || !set.HasBakedData())throw new InvalidOperationException("Missing baked data");
                string label=states[phase].scenario;Directory.CreateDirectory(folder+"/"+label);AssetDatabase.Refresh();
                var current=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(r=>new ModernVillaDayNight.BakedRenderer{renderer=r,index=r.lightmapIndex,scaleOffset=r.lightmapScaleOffset}).ToArray();
                var terrains=Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(t=>new ModernVillaDayNight.BakedTerrain{terrain=t,index=t.lightmapIndex,scaleOffset=t.lightmapScaleOffset}).ToArray();
                if(phase==0)
                {
                    canonical=current;terrainCanonical=terrains;
                    dayMaps=CopyMaps(label);states[phase].maps=Pack(dayMaps);
                    string dataTarget=folder+"/Default/LightingData.asset";AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset),dataTarget);dayData=AssetDatabase.LoadAssetAtPath<LightingDataAsset>(dataTarget);
                    foreach(var src in Directory.GetFiles("Assets/Modern Villa/Scenes/Modern Villa","ReflectionProbe-*.exr"))AssetDatabase.CopyAsset(src,folder+"/Default/"+Path.GetFileName(src));
                    var data=new SerializedObject(dayData);var it=data.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference && it.objectReferenceValue is Texture tex){var replacement=AssetDatabase.LoadAssetAtPath<Texture>(folder+"/Default/"+Path.GetFileName(AssetDatabase.GetAssetPath(tex)));if(replacement)it.objectReferenceValue=replacement;}data.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    bool same=current.All(c=>{var d=canonical.First(x=>x.renderer==c.renderer);return c.index==d.index && (c.scaleOffset-d.scaleOffset).sqrMagnitude<1e-10f;}) && terrains.All(c=>{var d=terrainCanonical.First(x=>x.terrain==c.terrain);return c.index==d.index && (c.scaleOffset-d.scaleOffset).sqrMagnitude<1e-10f;});
                    states[phase].maps=Pack(same?CopyMaps(label):Normalize(label,current,terrains));
                    File.AppendAllText(Report+"/bake.txt","Packing matched="+same+"; canonical maps="+states[phase].maps.Length+"\n");
                }
                File.AppendAllText(Report+"/bake.txt",$"DONE {label}: {EditorApplication.timeSinceStartup-started:F1}s atlases={LightmapSettings.lightmaps.Length} APV={set.HasBakedData()}\n");
                phase++;
                if(phase<states.Length){BeginPhase();return;}
                Complete();
            }
            catch(Exception e){Fail(e);}
        }
        static ModernVillaDayNight.BakedMap[] Pack(LightmapData[] maps)=>maps.Select(m=>new ModernVillaDayNight.BakedMap{color=m.lightmapColor,direction=m.lightmapDir,mask=m.shadowMask}).ToArray();
        static LightmapData[] CopyMaps(string label)
        {
            Texture2D Copy(Texture2D t){if(!t)return null;string target=folder+"/"+label+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(t));if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(t),target))throw new IOException(target);return AssetDatabase.LoadAssetAtPath<Texture2D>(target);}
            return LightmapSettings.lightmaps.Select(m=>new LightmapData{lightmapColor=Copy(m.lightmapColor),lightmapDir=Copy(m.lightmapDir),shadowMask=Copy(m.shadowMask)}).ToArray();
        }
        static LightmapData[] Normalize(string label,ModernVillaDayNight.BakedRenderer[] current,ModernVillaDayNight.BakedTerrain[] terrains)
        {
            var source=LightmapSettings.lightmaps;var result=new LightmapData[dayMaps.Length];int kernel=cycle.blender.FindKernel("Remap");
            for(int atlas=0;atlas<result.Length;atlas++)
            {
                result[atlas]=new LightmapData();
                for(int channel=0;channel<2;channel++)
                {
                    var reference=channel==0?dayMaps[atlas].lightmapColor:dayMaps[atlas].lightmapDir;if(!reference)continue;
                    var rt=new RenderTexture(reference.width,reference.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear){enableRandomWrite=true};rt.Create();
                    var previous=RenderTexture.active;RenderTexture.active=rt;GL.Clear(false,true,Color.clear);
                    void Draw(int sourceIndex,Vector4 from,Vector4 to)
                    {
                        if(sourceIndex<0 || sourceIndex>=source.Length)throw new InvalidOperationException("Renderer missing in source bake");
                        var texture=channel==0?source[sourceIndex].lightmapColor:source[sourceIndex].lightmapDir;
                        int x=Mathf.RoundToInt(to.z*rt.width),y=Mathf.RoundToInt(to.w*rt.height),w=Mathf.RoundToInt(to.x*rt.width),h=Mathf.RoundToInt(to.y*rt.height);
                        if(w<1||h<1)return;
                        cycle.blender.SetTexture(kernel,"SourceA",texture);cycle.blender.SetTexture(kernel,"Result",rt);cycle.blender.SetVector("SourceRect",from);cycle.blender.SetVector("DestinationRect",new Vector4(w,h,x,y));cycle.blender.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                    }
                    foreach(var d in canonical.Where(d=>d.index==atlas)){var c=current.First(x=>x.renderer==d.renderer);Draw(c.index,c.scaleOffset,d.scaleOffset);}
                    foreach(var d in terrainCanonical.Where(d=>d.index==atlas)){var c=terrains.First(x=>x.terrain==d.terrain);Draw(c.index,c.scaleOffset,d.scaleOffset);}
                    var output=new Texture2D(rt.width,rt.height,TextureFormat.RGBAHalf,true,true);output.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);output.Apply(true,true);
                    RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);AssetDatabase.CreateAsset(output,folder+"/"+label+"/Canonical-"+atlas+"-"+channel+".asset");
                    if(channel==0)result[atlas].lightmapColor=output;else result[atlas].lightmapDir=output;
                }
            }
            return result;
        }
        static void Restore()
        {
            RenderSettings.skybox=savedSky;RenderSettings.ambientMode=savedAmbientMode;RenderSettings.ambientLight=savedAmbient;RenderSettings.ambientIntensity=savedAmbientIntensity;RenderSettings.reflectionIntensity=savedReflection;RenderSettings.fogColor=savedFog;cycle.sun.transform.rotation=savedSunRotation;
            for(int i=0;i<old.sceneLights.Length;i++)if(old.sceneLights[i]){old.sceneLights[i].enabled=lightEnabled[i];old.sceneLights[i].color=lightColors[i];old.sceneLights[i].intensity=lightIntensity[i];}
            var so=new SerializedObject(set);so.FindProperty("lightingScenario").stringValue="Default";so.FindProperty("freezePlacement").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            if(dayData)Lightmapping.lightingDataAsset=dayData;
            if(dayMaps!=null)
            {
                LightmapSettings.lightmaps=dayMaps;
                foreach(var b in canonical)if(b.renderer){b.renderer.lightmapIndex=b.index;b.renderer.lightmapScaleOffset=b.scaleOffset;}
                foreach(var b in terrainCanonical)if(b.terrain){b.terrain.lightmapIndex=b.index;b.terrain.lightmapScaleOffset=b.scaleOffset;}
                foreach(var src in Directory.GetFiles(folder+"/Default")){string name=Path.GetFileName(src);if((!name.StartsWith("Lightmap-")&&!name.StartsWith("ReflectionProbe-"))||name.EndsWith(".meta"))continue;string target="Assets/Modern Villa/Scenes/Modern Villa/"+name;File.Copy(src,target,true);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceUpdate);}
            }
        }
        static void Complete()
        {
            active=false;Lightmapping.bakeCompleted-=Completed;EditorApplication.update-=Watch;Restore();
            cycle.noon=states[0];cycle.morning=states[1];cycle.sunset=states[2];cycle.night=states[3];cycle.hour=7;cycle.running=true;cycle.dayLengthSeconds=600;cycle.enabled=true;old.enabled=false;
            // Keep legacy test callers compatible with the freshly baked canonical layout.
            old.nightMaps=states[3].maps;old.nightRenderers=canonical;old.nightTerrains=terrainCanonical;old.nightProbesReady=true;
            RenderSettings.skybox=cycle.noon.sky;cycle.sun.transform.rotation=Quaternion.Euler(cycle.noon.sunAngles);cycle.sun.intensity=cycle.noon.sunIntensity;cycle.sun.color=cycle.noon.sunColor;
            EditorUtility.SetDirty(cycle);EditorUtility.SetDirty(old);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.AppendAllText(Report+"/bake.txt","COMPLETE "+DateTime.UtcNow.ToString("O")+"\nFolder="+folder+"\n");
            VillaTimeChecks.Run();
        }
        static void Fail(Exception e)
        {
            active=false;Lightmapping.bakeCompleted-=Completed;EditorApplication.update-=Watch;if(Lightmapping.isRunning)Lightmapping.Cancel();
            File.AppendAllText(Report+"/bake.txt","FAILED "+e+"\n");
            if(cycle && old && set)Restore();Debug.LogException(e);
        }
    }
}
