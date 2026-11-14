using System;
using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class TrainingGroundBakedLighting : MonoBehaviour
{
    [Serializable] public struct Binding { public Renderer renderer; public int index; public Vector4 scaleOffset; }
    public Texture2D[] lightmaps;
    public Binding[] bindings;
    void OnEnable(){Apply();}
    public void Apply()
    {
        if(lightmaps==null||lightmaps.Length!=2||lightmaps[0]==null||lightmaps[1]==null||bindings==null)return;
        LightmapSettings.lightmapsMode=LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps=new[]{new LightmapData{lightmapColor=lightmaps[0]}, new LightmapData{lightmapColor=lightmaps[1]}};
        foreach(var b in bindings)if(b.renderer!=null){b.renderer.lightmapIndex=b.index;b.renderer.lightmapScaleOffset=b.scaleOffset;}
    }
    void OnDisable()
    {
        var active=LightmapSettings.lightmaps;
        if(lightmaps!=null&&lightmaps.Length==2&&active.Length==2&&active[0].lightmapColor==lightmaps[0]&&active[1].lightmapColor==lightmaps[1])LightmapSettings.lightmaps=new LightmapData[0];
    }
}
