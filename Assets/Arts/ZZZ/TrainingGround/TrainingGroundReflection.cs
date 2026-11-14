using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class TrainingGroundReflection : MonoBehaviour
{
    [Tooltip("Top surface of the recovered floor meshes, in world coordinates.")]
    public float floorHeight = 0.586f;
    [Range(256, 1024)] public int resolution = 768;
    [Range(0, 1)] public float strength = 0.12f;
    public Renderer[] floorRenderers;
    [System.NonSerialized] public int renderedGameFrames;
    Camera reflectionCamera;
    RenderTexture reflectionTexture;
    bool rendering;
    void OnEnable(){RenderPipelineManager.beginCameraRendering+=RenderReflection;}
    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering-=RenderReflection;
        Shader.SetGlobalFloat("_TrainingReflectionReady", 0);
        if(reflectionCamera!=null)Release(reflectionCamera.gameObject);
        if(reflectionTexture!=null){reflectionTexture.Release();Release(reflectionTexture);}
    }
    static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    void RenderReflection(ScriptableRenderContext context, Camera source)
    {
        if(rendering||source==null||source==reflectionCamera||source.cameraType==CameraType.Reflection||source.cameraType==CameraType.Preview)return;
        if(source.gameObject.scene!=gameObject.scene&&source.cameraType!=CameraType.SceneView)return;
        if(source.transform.position.y<=floorHeight+0.03f){Shader.SetGlobalFloat("_TrainingReflectionReady", 0);return;}
        rendering=true;bool inverted=GL.invertCulling;
        bool[] previous=floorRenderers==null?new bool[0]:new bool[floorRenderers.Length];
        try{
            if(reflectionCamera==null){var go=new GameObject("Training Floor Reflection Camera"){hideFlags=HideFlags.HideAndDontSave};reflectionCamera=go.AddComponent<Camera>();reflectionCamera.enabled=false;var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderShadows=false;data.renderPostProcessing=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;}
            int width=Mathf.Clamp(resolution, 256, 1024), height=Mathf.Max(128, Mathf.RoundToInt(width/Mathf.Max(0.5f, source.aspect)));
            if(reflectionTexture==null||reflectionTexture.width!=width||reflectionTexture.height!=height){if(reflectionTexture!=null){reflectionTexture.Release();Release(reflectionTexture);}reflectionTexture=new RenderTexture(width, height, 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear){name="Training Floor Reflection", hideFlags=HideFlags.HideAndDontSave, useMipMap=true, autoGenerateMips=false, filterMode=FilterMode.Trilinear, wrapMode=TextureWrapMode.Clamp};reflectionTexture.Create();}
            reflectionCamera.CopyFrom(source);reflectionCamera.enabled=false;reflectionCamera.cameraType=CameraType.Reflection;reflectionCamera.targetTexture=reflectionTexture;reflectionCamera.allowMSAA=false;reflectionCamera.useOcclusionCulling=false;reflectionCamera.depth=-100;
            var mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=2*floorHeight;
            reflectionCamera.transform.position=mirror.MultiplyPoint(source.transform.position);
            reflectionCamera.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
            Vector3 point=reflectionCamera.worldToCameraMatrix.MultiplyPoint(new Vector3(0, floorHeight+0.02f, 0));
            Vector3 normal=reflectionCamera.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            reflectionCamera.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal)));
            Shader.SetGlobalFloat("_TrainingReflectionReady", 0);
            for(int i=0;i<previous.Length;i++){if(floorRenderers[i]==null)continue;previous[i]=floorRenderers[i].forceRenderingOff;floorRenderers[i].forceRenderingOff=true;}
            GL.invertCulling=!inverted;

            #pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
            #pragma warning restore CS0618
            if(source.cameraType==CameraType.Game)renderedGameFrames++;
            reflectionTexture.GenerateMips();
            Shader.SetGlobalTexture("_TrainingReflectionTex", reflectionTexture);
            Shader.SetGlobalMatrix("_TrainingReflectionVP", GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, true)*reflectionCamera.worldToCameraMatrix);
            Shader.SetGlobalFloat("_TrainingReflectionStrength", strength);
            Shader.SetGlobalFloat("_TrainingReflectionReady", 1);
        }finally{
            GL.invertCulling=inverted;
            for(int i=0;i<previous.Length;i++)if(floorRenderers[i]!=null)floorRenderers[i].forceRenderingOff=previous[i];
            rendering=false;
        }
    }
}
