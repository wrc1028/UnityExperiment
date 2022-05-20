using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
// 挂载到主相机上
public class SSPR : MonoBehaviour
{
    public enum TextureSize { Full = 1, Half = 2, Quarter = 4, }
    public bool EnableSSPR = false;
    private bool isAddSSPRCMD = false;
    public TextureSize textureSize = TextureSize.Half;
    private TextureSize prevTextureSize;
    [Range(-2.0f, 2.0f)]
    public float waterHeightAdjust = 0;
    [Range(0.0f, 0.999f)]
    public float edgeFadeAdjust = 0.9f;
    public float stretchIntensity = 82.0f;
    public float stretchThreshold = 0;

    [HideInInspector]
    public List<Vector2> waterHeightAndDistance2s;
    /// <summary>
    /// 所需参数, xy:屏幕尺寸, z:水面高度 
    /// </summary>
    private Vector4 reflectParam;
    private Vector4 reflectParam2;
    private Camera mainCamera;
    private Matrix4x4 viewProjectionMatrix;
    private ComputeShader SSPRCS;
    private int ClearKernel;
    private int SSPRKernel;
    private int FillHoleKernel;
    // private int GaussianBlurKernel;
    private RenderTexture cameraColorTexture;
    private RenderTexture cameraTempColorTexture;
    private RenderTexture SSPRResult;
    // private RenderTexture SSPRResultBlur;
    private ComputeBuffer SSPRBuffer;
    private uint[] SSPRData;
    // cs input
    private static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
    private static int inverseViewProjectionMatrixId = Shader.PropertyToID("_InverseViewProjectionMatrix");
    private static int reflectionPropId = Shader.PropertyToID("_SSPRParam");
    private static int reflectionProp2Id = Shader.PropertyToID("_SSPRParam2");
    // cs output
    private static int SSPRBufferId = Shader.PropertyToID("SSPRBuffer");
    private static int SSPRResultId = Shader.PropertyToID("SSPRResult");
    private static int reflectionTextureId = Shader.PropertyToID("_SSReflectionTexture");
    private CommandBuffer SSPRCMD;
    private CommandBuffer CopyCameraColor;
    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.depthTextureMode |= DepthTextureMode.Depth;
        waterHeightAndDistance2s = new List<Vector2>();
        prevTextureSize = textureSize;
        SSPRCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(@"Assets\Cases\SSPR\Shaders\SSPR.compute");
        if (SSPRCS != null)
        {
            // 获得内核索引
            ClearKernel = SSPRCS.FindKernel("Clear");
            SSPRKernel = SSPRCS.FindKernel("SSPR");
            FillHoleKernel = SSPRCS.FindKernel("FillHole");
            // GaussianBlurKernel = SSPRCS.FindKernel("GaussianBlur");
        }
        else Debug.LogError("计算着色器丢失或路径错误");
    }
    private void OnDestroy() 
    {
        ClaenupBuffer();
    }
    private void OnDisable() 
    {
        ClaenupBuffer();
    }
    // 在渲染前调用
    public void OnPreRender()
    {
        // 如果场景中没有带有SSPRWater的物体, 则不进行SSPR渲染, 或者将执行渲染的分辨率调成1
        // 如果有, 使用距离相机最近的那个水面高度
        if (!EnableSSPR || SSPRCS == null || waterHeightAndDistance2s.Count == 0)
        {
            if (isAddSSPRCMD) ClaenupBuffer();
            return;
        }
        // 设置常规参数, 每帧设置
        SetFrameParam();
        // 调整反射贴图大小
        if (prevTextureSize != textureSize)
        {
            prevTextureSize = textureSize;
            ClaenupBuffer();
        }
        // 在主相机中添加cmd
        if (!isAddSSPRCMD) AddSSPRCMD();
    }
    // 设置每一帧运行都需要更新的数据
    private void SetFrameParam()
    {
        // 相机投影的逆矩阵
        viewProjectionMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(inverseViewProjectionMatrixId, viewProjectionMatrix.inverse);
        // 反射参数
        float waterHeight = GetMinDistanceWaterHeight(waterHeightAndDistance2s) - waterHeightAdjust;
        reflectParam = new Vector4(Screen.width / (int)textureSize, Screen.height / (int)textureSize, waterHeight, (float)textureSize);
        Shader.SetGlobalVector(reflectionPropId, reflectParam);
        float cameraDirX = mainCamera.transform.eulerAngles.x;
        cameraDirX = cameraDirX > 180 ? cameraDirX - 360 : cameraDirX;
        cameraDirX *= 0.00001f;
        reflectParam2 = new Vector4(stretchIntensity, stretchThreshold, cameraDirX, edgeFadeAdjust);
        Shader.SetGlobalVector(reflectionProp2Id, reflectParam2);
    }
    // 获得距离相机最近的那个水面高度
    private float GetMinDistanceWaterHeight(List<Vector2> waterInfos)
    {
        Vector2 waterInfo = waterInfos[0];
        for (int i = 1; i < waterInfos.Count; i++)
        {
            if (waterInfos[i].y > waterInfo.y) continue;
            waterInfo = waterInfos[i];
        }
        return waterInfo.x;
    }
    // 添加SSPR cmd
    private void AddSSPRCMD()
    {
        cameraColorTexture = CreateRenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        // cameraTempColorTexture = CreateRenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        SSPRResult = CreateRenderTexture((int)reflectParam.x, (int)reflectParam.y, 0, RenderTextureFormat.ARGB32);
        // SSPRResultBlur = CreateRenderTexture((int)reflectParam.x, (int)reflectParam.y, 0, RenderTextureFormat.ARGB32);
        SSPRData = new uint[(int)(reflectParam.x * reflectParam.y)];
        SSPRBuffer = new ComputeBuffer(SSPRData.Length, sizeof(uint));
        SSPRBuffer.SetData(SSPRData);

        CopyCameraColor = CommandBufferPool.Get("CopyCameraColor");
        // CopyCameraColor.Blit(mainCamera.targetTexture, cameraColorTexture);
        CopyCameraColor.Blit(cameraTempColorTexture, cameraColorTexture);
        mainCamera.AddCommandBuffer(CameraEvent.AfterSkybox - 1, CopyCameraColor);

        SSPRCMD = CommandBufferPool.Get("SSPR");
        int threadGroupsX = Mathf.CeilToInt(reflectParam.x / 8 + 0.0001f);
        int threadGroupsY = Mathf.CeilToInt(reflectParam.y / 8 + 0.0001f);
        ClearTexture(SSPRCMD, threadGroupsX, threadGroupsY);
        CalculateSSPR(SSPRCMD, threadGroupsX, threadGroupsY);
        FillHole(SSPRCMD, threadGroupsX, threadGroupsY);
        // GaussianBlur(SSPRCMD, threadGroupsX, threadGroupsY);
        mainCamera.AddCommandBuffer(CameraEvent.AfterSkybox, SSPRCMD);
        SSPRCMD.SetGlobalTexture(reflectionTextureId, SSPRResult);

        isAddSSPRCMD = true;
    }
    // 清楚缓存
    private void ClaenupBuffer()
    {
        cameraColorTexture.Release();
        SSPRResult.Release();
        // SSPRResultBlur.Release();
        SSPRBuffer.Release();

        mainCamera.RemoveCommandBuffer(CameraEvent.AfterSkybox - 1, CopyCameraColor);
        mainCamera.RemoveCommandBuffer(CameraEvent.AfterSkybox, SSPRCMD);

        isAddSSPRCMD = false;
    }
    // 在渲染水面结束后
    private void OnPostRender() 
    {
        waterHeightAndDistance2s.Clear();
    }
    // 清楚数据
    private void ClearTexture(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {
        cmd.SetComputeTextureParam(SSPRCS, ClearKernel, SSPRResultId, SSPRResult);
        cmd.SetComputeBufferParam(SSPRCS, ClearKernel, SSPRBufferId, SSPRBuffer);
        cmd.DispatchCompute(SSPRCS, ClearKernel, threadGroupsX, threadGroupsY, 1);
    }
    // SSPR计算
    private void CalculateSSPR(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {
        // cmd.SetComputeTextureParam(SSPRCS, SSPRKernel, SSPRResultId, SSPRResult);
        cmd.SetComputeBufferParam(SSPRCS, SSPRKernel, SSPRBufferId, SSPRBuffer);
        cmd.DispatchCompute(SSPRCS, SSPRKernel, threadGroupsX, threadGroupsY, 1);
    }
    // 填洞
    private void FillHole(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {
        cmd.SetComputeTextureParam(SSPRCS, FillHoleKernel, cameraColorTextureId, cameraColorTexture);
        cmd.SetComputeTextureParam(SSPRCS, FillHoleKernel, SSPRResultId, SSPRResult);
        cmd.SetComputeBufferParam(SSPRCS, FillHoleKernel, SSPRBufferId, SSPRBuffer);
        cmd.DispatchCompute(SSPRCS, FillHoleKernel, threadGroupsX, threadGroupsY, 1);
    }
    // 高斯模糊
    // private void GaussianBlur(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    // {
    //     cmd.SetComputeTextureParam(SSPRCS, GaussianBlurKernel, "_SSPRResultInput", SSPRResult);
    //     cmd.SetComputeTextureParam(SSPRCS, GaussianBlurKernel, SSPRResultId, SSPRResultBlur);
    //     cmd.DispatchCompute(SSPRCS, GaussianBlurKernel, threadGroupsX, threadGroupsY, 1);
    // }
    private RenderTexture CreateRenderTexture(int width, int height, int depth,RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture(width, height, depth, format);
        rt.filterMode = FilterMode.Bilinear;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
}
