using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Rubber.UI;

namespace Rubber.EditorTools
{
    public static class VillaMenuSetup
    {
        public static void Frost()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            EditorSceneManager.SaveOpenScenes();var scene=EditorSceneManager.OpenScene(VillaFlowSetup.Menu);
            var bg=UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>();
            const string path="Assets/_Project/Art/Shaders/MenuFrostFilter.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Hidden/Rubber/Menu Frost Filter"));AssetDatabase.CreateAsset(mat,path);}
            bg.diffusionMaterial=mat;bg.blurMaterial.SetFloat("_Frost",.36f);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void Refine()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            EditorSceneManager.SaveOpenScenes();var scene=EditorSceneManager.OpenScene(VillaFlowSetup.Menu);
            var bg=UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>();
            bg.lookAt=new Vector3(-1.5f,1.5f,9);bg.arcDegrees=26;bg.height=12;bg.PositionCamera();
            foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))light.enabled=false;
            foreach(var canvas in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)))
            {canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void Install()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            EditorSceneManager.SaveOpenScenes();
            var scene=EditorSceneManager.OpenScene(VillaFlowSetup.Menu);
            var previous=UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>();
            if(previous) throw new InvalidOperationException("Menu background already installed.");
            var shader=Shader.Find("Rubber/UI/Menu Villa Blur");
            if(!shader) throw new InvalidOperationException("Background shader not imported.");
            const string path="Assets/_Project/Art/Shaders/MenuVillaBlur.mat";
            var material=new Material(shader);AssetDatabase.CreateAsset(material,path);
            var root=new GameObject("Villa Menu Background");
            var bg=root.AddComponent<VillaMenuBackground>(); bg.blurMaterial=material;
            var cam=new GameObject("Menu Orbit Camera",typeof(Camera));cam.transform.SetParent(root.transform);
            bg.menuCamera=cam.GetComponent<Camera>();bg.menuCamera.clearFlags=CameraClearFlags.Skybox;bg.menuCamera.enabled=false;
            var canvasObject=new GameObject("Background Canvas",typeof(RectTransform),typeof(Canvas));canvasObject.transform.SetParent(root.transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-100;
            var imageObject=new GameObject("Blurred Villa",typeof(RectTransform),typeof(RawImage));imageObject.transform.SetParent(canvasObject.transform,false);
            var rect=imageObject.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            bg.background=imageObject.GetComponent<RawImage>();bg.background.raycastTarget=false;bg.background.color=new Color(1,1,1,0);
            foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))
                if(camera!=bg.menuCamera){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.05f);camera.cullingMask=0;}
            bg.PositionCamera();
            foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))light.enabled=false;
            foreach(var ui in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)))
            {ui.renderMode=RenderMode.ScreenSpaceOverlay;ui.worldCamera=null;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Frost();
            Directory.CreateDirectory("Docs/MenuBackground");File.WriteAllText("Docs/MenuBackground/setup.txt","Existing villa loaded additively for menu only; player, collection, physics and HUD disabled. Shared lighting, constrained orbit, 1080p source, reusable half-resolution Gaussian blur buffers and white frost. Preview unloaded before fresh gameplay.\n");
        }
    }
}
