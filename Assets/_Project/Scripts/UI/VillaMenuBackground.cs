using System.Collections;
using Rubber.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Rubber.UI
{
    // The actual villa is loaded only for presentation. Start always loads a fresh game.
    public sealed class VillaMenuBackground : MonoBehaviour
    {
        public Camera menuCamera;
        public RawImage background;
        public Material blurMaterial;
        public Material diffusionMaterial;
        public Vector3 lookAt = new Vector3(-1.5f, 1.5f, 9f);
        public float radius = 20f, height = 12f, fieldOfView = 48f;
        public float centerAngle = 175f, arcDegrees = 26f, orbitSeconds = 100f;
        public float daySeconds = 240f;
        public bool Ready { get; private set; }
        public float LoadProgress => Ready ? 1 : loading != null ? Mathf.Clamp01(loading.progress / .9f) * .9f : .95f;
        public VillaTimeOfDay Lighting { get; private set; }
        public float OrbitTime { get; set; }
        public bool AnimateCamera = true;
        Scene menuScene, villaScene;
        AsyncOperation loading;
        RenderTexture target;
        RenderTexture blurA, blurB;
        Material filter;
        bool configured, rendered;
        Material material;
        bool leaving;
        float reveal;

        IEnumerator Start()
        {
            menuScene = gameObject.scene;
            foreach(var root in menuScene.GetRootGameObjects())
                foreach(var light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;
            background.color = new Color(1, 1, 1, 0);
            material = new Material(blurMaterial);
            filter = new Material(diffusionMaterial);
            background.material = material;
            StartCoroutine(BlurFrames());
            SceneManager.sceneLoaded += ConfigureVilla;
            loading = SceneManager.LoadSceneAsync("Modern Villa", LoadSceneMode.Additive);
            yield return loading;
            loading = null;
        }

        void ConfigureVilla(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive || scene.name != "Modern Villa") return;
            villaScene = scene;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var audio in root.GetComponentsInChildren<AudioListener>(true)) audio.enabled = false;
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
                foreach (var events in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)) events.gameObject.SetActive(false);
                foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!script) continue;
                    var ns = script.GetType().Namespace ?? "";
                    if (ns.StartsWith("Rubber.Gameplay") || script is Rubber.Core.VillaGameFlow || script is GameSettingsUI)
                        script.enabled = false;
                }
                foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                var lights = root.GetComponentInChildren<VillaTimeOfDay>(true);
                if (lights) Lighting = lights;
            }
            // Scene RenderSettings must belong to the villa before its lighting initializes.
            if (Lighting) Lighting.enabled = false;
            SceneManager.SetActiveScene(scene);
            if (Lighting)
            {
                Lighting.hour = 9; Lighting.dayLengthSeconds = daySeconds;
                Lighting.lightmapUpdatesPerSecond = 5; Lighting.running = true; Lighting.enabled = true;
            }
            menuCamera.enabled = true;
            menuCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            // The pool shader needs depth and scene color even in the menu preview.
            menuCamera.GetUniversalAdditionalCameraData().requiresColorOption = CameraOverrideOption.On;
            menuCamera.GetUniversalAdditionalCameraData().requiresDepthOption = CameraOverrideOption.On;
            menuCamera.nearClipPlane = .15f; menuCamera.farClipPlane = 100;
            configured = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ResizeTarget(); PositionCamera();
        }

        void ResizeTarget()
        {
            float scale = Mathf.Min(1f, 1080f / Mathf.Max(1, Screen.height));
            int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * scale));
            int h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * scale));
            if (target && target.width == w && target.height == h) return;
            if (target) { menuCamera.targetTexture = null; Release(target); Release(blurA); Release(blurB); }
            target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            { name = "Menu villa source", antiAliasing = 1, filterMode = FilterMode.Bilinear };
            target.Create();
            blurA = new RenderTexture(w/2,h/2,0,RenderTextureFormat.ARGB32) { name="Menu frost horizontal", filterMode=FilterMode.Bilinear };
            blurB = new RenderTexture(w/2,h/2,0,RenderTextureFormat.ARGB32) { name="Menu frost vertical", filterMode=FilterMode.Bilinear };
            blurA.Create();blurB.Create();
            menuCamera.targetTexture = target; background.texture = blurB;
        }

        IEnumerator BlurFrames()
        {
            var endOfFrame=new WaitForEndOfFrame();
            while(true)
            {
                yield return endOfFrame;
                if(!configured || leaving || !target)continue;
                // Process only after URP has submitted the completed camera texture.
                var previous=RenderTexture.active;
                Graphics.Blit(target,blurA,filter,0);
                Graphics.Blit(blurA,blurB,filter,1);
                Graphics.Blit(blurB,blurA,filter,0);
                Graphics.Blit(blurA,blurB,filter,1);
                RenderTexture.active=previous;
                rendered=true;
            }
        }
        void Release(RenderTexture texture){if(texture){texture.Release();Destroy(texture);}}

        public void PositionCamera()
        {
            // A gentle return arc keeps the unfinished perimeter out of the shot.
            float angle = (centerAngle + Mathf.Sin(OrbitTime * Mathf.PI * 2 / Mathf.Max(10, orbitSeconds)) * arcDegrees * .5f) * Mathf.Deg2Rad;
            menuCamera.transform.position = lookAt + new Vector3(Mathf.Sin(angle) * radius, height - lookAt.y, Mathf.Cos(angle) * radius);
            menuCamera.transform.LookAt(lookAt);
            menuCamera.fieldOfView = fieldOfView;
        }

        void LateUpdate()
        {
            if (!configured || leaving) return;
            ResizeTarget();
            if (AnimateCamera) OrbitTime += Time.unscaledDeltaTime;
            PositionCamera();
            reveal = Mathf.MoveTowards(reveal, 1, Time.unscaledDeltaTime / 1.5f);
            background.color = new Color(1, 1, 1, reveal);
            Ready=rendered && reveal>=1;
        }

        public IEnumerator PrepareForGame()
        {
            leaving = true;
            if (loading != null) yield return loading;
            Ready = false;
            menuCamera.enabled = false;
            if (Lighting) Lighting.enabled = false;
            if (menuScene.IsValid() && menuScene.isLoaded) SceneManager.SetActiveScene(menuScene);
            if (villaScene.IsValid() && villaScene.isLoaded) yield return SceneManager.UnloadSceneAsync(villaScene);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= ConfigureVilla;
            if (menuCamera) menuCamera.targetTexture = null;
            Release(target);Release(blurA);Release(blurB);
            if (material) Destroy(material);
            if (filter) Destroy(filter);
        }
    }
}
