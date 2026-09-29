using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Rubber.Gameplay.Player;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Interaction;
namespace Rubber.EditorTools
{
    public static class VillaFlowSetup
    {
        public const string Menu="Assets/_Project/Scenes/Main/MainMenu.unity";
        public static void Polish()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
            EditorSceneManager.SaveOpenScenes();var game=EditorSceneManager.OpenScene(ModernVillaSetup.ScenePath);
            var icon=GameObject.Find("Game HUD").transform.Find("Count/Duck").GetComponent<Image>();icon.color=new Color(.65f,.37f,.04f);
            var flow=UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>();
            var settingsData=new SerializedObject(flow.settings);var panel=(GameObject)settingsData.FindProperty("settingsPanel").objectReferenceValue;
            var source=panel.GetComponentsInChildren<Image>(true).First(i=>i.name=="Setting");
            foreach(var root in new[]{flow.pausePanel,flow.completePanel}){var card=root.transform.Find("Card").GetComponent<Image>();card.sprite=source.sprite;card.type=Image.Type.Sliced;card.color=Color.white;}
            EditorSceneManager.MarkSceneDirty(game);EditorSceneManager.SaveScene(game);
            EditorSceneManager.OpenScene(Menu);
        }
        public static void FixCanvasReferences()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;EditorUtility.SetDirty(canvas);}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static void Survey()
        {
            Directory.CreateDirectory("Docs/GameFlow");
            var scene=EditorSceneManager.OpenScene(Menu,OpenSceneMode.Additive);
            try
            {
                var lines=new List<string>();foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string path=ModernVillaSurfaceAudit.PathOf(t);
                    if(path.Split('/').Length<=4)lines.Add(path+" active="+t.gameObject.activeSelf+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))+(t.TryGetComponent<TMP_Text>(out var text)?" text="+text.text+" font="+AssetDatabase.GetAssetPath(text.font):""));
                }
                File.WriteAllLines("Docs/GameFlow/menu-survey.txt",lines);
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var g=new GameObject(name,typeof(RectTransform));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        static TMP_Text Text(string name,Transform parent,string value,TMP_FontAsset font,float size,Vector2 dimensions,Vector2 position)
        {
            var r=Rect(name,parent,dimensions,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.18f,.15f,.12f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
        }
        static void Button(Transform parent,string label,float y,TMP_FontAsset font,Sprite sprite,UnityAction action)
        {
            var r=Rect(label,parent,new Vector2(340,62),new Vector2(0,y));var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;image.color=new Color(1,.86f,.39f);
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,action);
            Text("Label",r,label,font,27,new Vector2(325,55),Vector2.zero);
        }
        static GameObject Panel(Transform canvas,string title,TMP_FontAsset font,out Transform card)
        {
            var root=Rect(title,canvas,Vector2.zero,Vector2.zero);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            root.gameObject.AddComponent<Image>().color=new Color(.04f,.08f,.1f,.72f);
            card=Rect("Card",root,new Vector2(580,600),Vector2.zero);card.gameObject.AddComponent<Image>().color=new Color(.98f,.97f,.90f);
            Text("Title",card,title,font,44,new Vector2(540,80),new Vector2(0,225));return root.gameObject;
        }
        public static void Install()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
            var game=SceneManager.GetActiveScene();if(game.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Open Modern Villa.");
            if(UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>())throw new InvalidOperationException("Flow already installed.");
            Directory.CreateDirectory("Docs/GameFlow");EditorSceneManager.SaveScene(game);File.Copy(game.path,"Docs/GameFlow/BeforeFlow.unity.backup",true);
            var menu=EditorSceneManager.OpenScene(Menu,OpenSceneMode.Additive);
            try
            {
                var roots=menu.GetRootGameObjects();var menuCanvas=roots.First(g=>g.GetComponent<Canvas>()&&g.transform.Find("Title"));
                var prototype=roots.First(g=>g.GetComponent<PauseSettingsUI>());
                var settings=roots.Select(g=>g.GetComponent<GameSettingsUI>()).First(g=>g);
                var transition=settings.GetComponent<MainMenuTransition>();var transitionData=new SerializedObject(transition);transitionData.FindProperty("nextSceneName").stringValue="Modern Villa";transitionData.ApplyModifiedPropertiesWithoutUndo();
                prototype.SetActive(false);menuCanvas.SetActive(true);menuCanvas.name="Main Menu Canvas";
                var font=menuCanvas.GetComponentInChildren<TMP_Text>(true).font;
                var sprite=((Button)new SerializedObject(transition).FindProperty("startButton").objectReferenceValue).GetComponent<Image>().sprite;
                var bundle=new GameObject("UI Bundle");SceneManager.MoveGameObjectToScene(bundle,menu);menuCanvas.transform.SetParent(bundle.transform,true);settings.transform.SetParent(bundle.transform,true);
                var copy=UnityEngine.Object.Instantiate(bundle);SceneManager.MoveGameObjectToScene(copy,game);copy.name="Game UI - Shared Menu Settings";
                menuCanvas.transform.SetParent(null,true);settings.transform.SetParent(null,true);UnityEngine.Object.DestroyImmediate(bundle);
                var gameSettings=copy.GetComponentInChildren<GameSettingsUI>(true);gameSettings.managedByGameFlow=true;
                var gameTransition=gameSettings.GetComponent<MainMenuTransition>();var tr=new SerializedObject(gameTransition);
                var loading=(GameObject)tr.FindProperty("loadingPanel").objectReferenceValue;var tips=(LoadingTips)tr.FindProperty("loadingTips").objectReferenceValue;
                UnityEngine.Object.DestroyImmediate(gameTransition);
                var optionsCanvas=copy.GetComponentInChildren<Canvas>();optionsCanvas.sortingOrder=200;optionsCanvas.renderMode=RenderMode.ScreenSpaceOverlay;optionsCanvas.worldCamera=null;
                foreach(Transform child in optionsCanvas.transform)child.gameObject.SetActive(false);
                var hud=UnityEngine.Object.Instantiate(prototype);SceneManager.MoveGameObjectToScene(hud,game);hud.name="Game HUD";hud.SetActive(true);
                UnityEngine.Object.DestroyImmediate(hud.GetComponent<PauseSettingsUI>());
                foreach(Transform child in hud.transform.Cast<Transform>().ToArray())if(child.name!="Count")UnityEngine.Object.DestroyImmediate(child.gameObject);
                hud.GetComponent<Canvas>().sortingOrder=100;hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;hud.GetComponent<Canvas>().worldCamera=null;
                var count=(RectTransform)hud.transform.Find("Count");count.anchorMin=count.anchorMax=new Vector2(1,1);count.pivot=new Vector2(1,1);count.anchoredPosition=new Vector2(-30,-24);count.sizeDelta=new Vector2(255,80);
                var backdrop=count.gameObject.AddComponent<Image>();backdrop.color=new Color(.98f,.97f,.90f,.94f);backdrop.raycastTarget=false;
                var counter=count.GetComponentInChildren<TMP_Text>();var cr=counter.rectTransform;cr.anchorMin=cr.anchorMax=new Vector2(.5f,.5f);cr.pivot=new Vector2(.5f,.5f);cr.anchoredPosition=new Vector2(25,0);cr.sizeDelta=new Vector2(170,60);counter.fontSize=32;counter.color=new Color(.18f,.15f,.12f);counter.alignment=TextAlignmentOptions.Center;counter.text="00 / 25";
                var icon=count.Find("Duck") as RectTransform;if(icon){icon.anchorMin=icon.anchorMax=new Vector2(.5f,.5f);icon.pivot=new Vector2(.5f,.5f);icon.anchoredPosition=new Vector2(-85,0);icon.sizeDelta=new Vector2(52,52);icon.GetComponent<Image>().preserveAspect=true;icon.GetComponent<Image>().raycastTarget=false;}
                var objective=Text("Objective",hud.transform,"Find the ducks. Bring them back to either pool.",font,25,new Vector2(920,48),Vector2.zero);objective.color=Color.white;objective.rectTransform.anchorMin=objective.rectTransform.anchorMax=new Vector2(.5f,1);objective.rectTransform.anchoredPosition=new Vector2(0,-42);
                var hint=Text("Controls",hud.transform,"WASD  Move     SPACE  Jump     CLICK  Interact     G  Drop     ESC  Pause",font,21,new Vector2(1050,40),Vector2.zero);hint.color=Color.white;hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);hint.rectTransform.anchoredPosition=new Vector2(0,28);
                SceneManager.SetActiveScene(game);var flow=new GameObject("Game Flow").AddComponent<Rubber.Core.VillaGameFlow>();
                flow.registry=UnityEngine.Object.FindAnyObjectByType<RubberDuckReturnRegistry>();flow.input=UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();flow.movement=flow.input.GetComponent<PlayerMovement>();flow.detector=flow.input.GetComponent<PlayerInteractionDetector>();flow.reticle=UnityEngine.Object.FindAnyObjectByType<InteractionReticle>().gameObject;flow.counter=counter;flow.settings=gameSettings;flow.loadingPanel=loading;flow.tips=tips;
                flow.pausePanel=Panel(hud.transform,"Take a break",font,out var pause);
                Button(pause,"Resume",105,font,sprite,flow.Resume);Button(pause,"Settings",25,font,sprite,flow.OpenSettings);Button(pause,"Restart",-55,font,sprite,flow.Restart);Button(pause,"Main menu",-135,font,sprite,flow.ReturnToMenu);
                flow.completePanel=Panel(hud.transform,"Everyone is home!",font,out var complete);flow.completionText=Text("Result",complete,"All 25 ducks are home!",font,30,new Vector2(520,110),new Vector2(0,85));
                Button(complete,"Play again",-55,font,sprite,flow.Restart);Button(complete,"Main menu",-135,font,sprite,flow.ReturnToMenu);
                flow.pausePanel.SetActive(false);flow.completePanel.SetActive(false);
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);EditorSceneManager.MarkSceneDirty(game);EditorSceneManager.SaveScene(game);AssetDatabase.SaveAssets();
                File.WriteAllText("Docs/GameFlow/setup.txt","MainMenu -> Modern Villa; original menu/loading/settings UI preserved. HUD, pause, settings, complete, restart and return wired.\n");
            }
            finally{EditorSceneManager.CloseScene(menu,true);SceneManager.SetActiveScene(game);}
        }
    }
}
