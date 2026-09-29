using System;
using UnityEngine;
namespace Rubber.World
{
    public sealed class BeachVillaHideLocations : MonoBehaviour
    {
        [Serializable] public struct Spot { public string label; public Vector3 position; public Vector3 approach; }
        public Spot[] spots;
        public Vector3 poolReturnPoint;
        void OnDrawGizmosSelected()
        {
            Gizmos.color=Color.yellow;
            if(spots!=null)foreach(var spot in spots){Gizmos.DrawWireSphere(spot.position,.13f);Gizmos.DrawLine(spot.position,spot.approach+Vector3.up);}
            Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(poolReturnPoint,.5f);
        }
    }
}
