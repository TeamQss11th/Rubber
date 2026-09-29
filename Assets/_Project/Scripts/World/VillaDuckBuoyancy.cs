using UnityEngine;
using UnityEngine.Events;

namespace Rubber.World
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VillaDuckBuoyancy : MonoBehaviour
    {
        public VillaPoolWater water;
        public Vector3[] floatPoints;
        public float equilibriumDepth=.045f;
        public float waterResistance=3;
        public UnityEvent onSettled=new UnityEvent();
        public bool IsFloating{get;private set;}
        public bool IsSettled{get;private set;}
        Rigidbody body;float stableTime;bool notified;
        void Awake(){body=GetComponent<Rigidbody>();}
        void FixedUpdate()
        {
            IsFloating=false;
            if(body.isKinematic||floatPoints==null||floatPoints.Length==0){ResetState();return;}
            var currentPool=VillaPoolWater.FindAt(body.worldCenterOfMass);
            if(currentPool&&currentPool!=water){water=currentPool;ResetState();}
            if(!water||!water.isActiveAndEnabled){ResetState();return;}
            int submerged=0;
            foreach(var local in floatPoints)
            {
                var p=transform.TransformPoint(local);float depth=water.Height-p.y;
                if(depth<=0||p.y<water.bottom||!water.Contains(p))continue;
                submerged++;
                float lift=body.mass*Physics.gravity.magnitude/floatPoints.Length*Mathf.Clamp(depth/Mathf.Max(.005f,equilibriumDepth),0,4);
                var resistance=-body.GetPointVelocity(p)*body.mass*waterResistance/floatPoints.Length;
                body.AddForceAtPosition(Vector3.up*lift+resistance,p,ForceMode.Force);
            }
            IsFloating=submerged>0;
            if(!IsFloating){ResetState();return;}
            bool stable=water.Contains(body.worldCenterOfMass)&&body.linearVelocity.sqrMagnitude<.01f&&body.angularVelocity.sqrMagnitude<.04f&&Vector3.Dot(transform.up,Vector3.up)>.85f;
            stableTime=stable?stableTime+Time.fixedDeltaTime:0;IsSettled=stableTime>1;
            if(IsSettled&&!notified){notified=true;onSettled.Invoke();}
        }
        public void ResetState(){stableTime=0;notified=false;IsFloating=IsSettled=false;}
    }
}
