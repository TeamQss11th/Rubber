using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rubber.EditorTools
{
    public static class VillaPoolSurfaceSettings
    {
        [MenuItem("Rubber/Pool/Apply Refined Surface Settings")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Apply water material settings in Edit mode.");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/PoolPrototype/PoolWater.mat");
            if(!material)throw new InvalidOperationException("Shared pool water material is missing.");
            Undo.RecordObject(material,"Refine water ripples depth and refraction");
            material.SetColor("_DeepColor",new Color(.008f,.085f,.13f,1));
            material.SetFloat("_DepthDistance",1.8f);
            material.SetFloat("_Density",.8f);
            material.SetFloat("_WaveStrength",.035f);
            material.SetFloat("_WaveSpeed",.65f);
            material.SetFloat("_RefractionPixels",3);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            ShaderUtil.CompilePass(material,0,true);
            var lines=new System.Collections.Generic.List<string>{"Shared material saved: "+AssetDatabase.GetAssetPath(material),"Wave scales=2, directions=4; depth distance=1.8m; absorption=0.8; refraction <=3 pixels per axis.","Existing reflection settings unchanged. No additional cameras or render passes.","Shader errors="+ShaderUtil.ShaderHasError(material.shader),"Play mode and performance tests were not run."};
            foreach(var message in ShaderUtil.GetShaderMessages(material.shader))lines.Add(message.severity+": "+message.message);
            Directory.CreateDirectory("Docs/VillaPool");File.WriteAllLines("Docs/VillaPool/surface-settings.txt",lines);
        }
    }
}
