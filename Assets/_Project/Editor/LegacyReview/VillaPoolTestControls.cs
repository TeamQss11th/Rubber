using UnityEngine;
using UnityEngine.InputSystem;
namespace Rubber.World
{
    public sealed class VillaPoolTestControls:MonoBehaviour
    {
        public VillaDuckBuoyancy[] ducks;
        public VillaPoolWater water;
        public ModernVillaWalkthrough walkthrough;
        public int poolCheckpoint;
        Vector3[] starts;Quaternion[] rotations;
        void Awake(){starts=new Vector3[ducks.Length];rotations=new Quaternion[ducks.Length];for(int i=0;i<ducks.Length;i++){starts[i]=ducks[i].transform.position;rotations[i]=ducks[i].transform.rotation;}}
        public void ResetDucks(){for(int i=0;i<ducks.Length;i++){if(!ducks[i])continue;var rb=ducks[i].GetComponent<Rigidbody>();rb.position=starts[i];rb.rotation=rotations[i];rb.linearVelocity=rb.angularVelocity=Vector3.zero;ducks[i].ResetState();rb.WakeUp();}}
        void Update(){if(!Application.isFocused||Keyboard.current==null)return;if(Keyboard.current.bKey.wasPressedThisFrame)ResetDucks();if(Keyboard.current.jKey.wasPressedThisFrame&&ducks.Length>0&&ducks[0])ducks[0].GetComponent<Rigidbody>().AddForce(Vector3.left*.1f,ForceMode.Impulse);if(Keyboard.current.f12Key.wasPressedThisFrame&&walkthrough)walkthrough.GoTo(poolCheckpoint);}
        void OnGUI(){int floating=0,settled=0;foreach(var d in ducks){if(d&&d.IsFloating)floating++;if(d&&d.IsSettled)settled++;}GUI.Box(new Rect(16,200,660,27),$"POOL TEST | F12: pool | B: drop | J: push | floating {floating}/{ducks.Length} | settled {settled}/{ducks.Length}");}
    }
}
