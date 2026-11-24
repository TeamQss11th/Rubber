using UnityEngine;
using System.Collections.Generic;

namespace Rubber.World
{
    // The footprint is sampled from the actual pool interior, not its rectangular bounds.
    public sealed class VillaPoolWater : MonoBehaviour
    {
        static readonly List<VillaPoolWater> pools=new List<VillaPoolWater>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearRegistry(){pools.Clear();}
        void OnEnable(){if(!pools.Contains(this))pools.Add(this);}
        void OnDisable(){pools.Remove(this);}
        public static VillaPoolWater FindAt(Vector3 point)
        {
            foreach(var pool in pools)if(pool&&pool.isActiveAndEnabled&&pool.Contains(point))return pool;
            return null;
        }
        public Vector2 origin;
        public float cellSize=.1f;
        public int columns,rows;
        public bool[] wetCells;
        public float bottom=-1.9f;
        public Renderer surface;
        MaterialPropertyBlock properties;
        public float Height=>transform.position.y;
        public bool Contains(Vector3 point)
        {
            int x=Mathf.FloorToInt((point.x-origin.x)/cellSize),z=Mathf.FloorToInt((point.z-origin.y)/cellSize);
            return x>=0&&z>=0&&x<columns&&z<rows&&wetCells!=null&&z*columns+x<wetCells.Length&&wetCells[z*columns+x];
        }
        void LateUpdate()
        {
            if(!surface)return;
            if(properties==null)properties=new MaterialPropertyBlock();
            surface.GetPropertyBlock(properties);properties.SetColor("_BaseColor",surface.sharedMaterial.GetColor("_BaseColor"));properties.SetFloat("_ReflectionStrength",Mathf.Clamp01(RenderSettings.reflectionIntensity));surface.SetPropertyBlock(properties);
        }
    }
}
