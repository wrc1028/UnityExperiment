using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class SSPR : MonoBehaviour
{
    public enum TextureSize { Full = 1, Half = 2, Quarter = 4, Eighth = 8, }
    public TextureSize textureSize = TextureSize.Half;
    public float stretchIntensity = 82.0f;
    public float stretchThreshold = 0;
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
    private RenderTexture cameraColorTexture;
    private RenderTexture SSPRResult;
    private ComputeBuffer SSPRBuffer;
    private uint[] SSPRData;
    
    private static int inverseViewProjectionMatrixId = Shader.PropertyToID("_InverseViewProjectionMatrix");
    private static int reflectionTextureId = Shader.PropertyToID("_SSReflectionTexture");
    private static int reflectionPropId = Shader.PropertyToID("_SSPRParam");
    private static int reflectionProp2Id = Shader.PropertyToID("_SSPRParam2");
    private CommandBuffer SSPRCMD;
    private CommandBuffer CopyCameraColor;
    private void Start()
    {
        mainCamera = Camera.main;
        mainCamera.depthTextureMode |= DepthTextureMode.Depth;
        
        stretchIntensity = 81.0f;
        stretchThreshold = 0;
        reflectParam = new Vector4(Screen.width / (int)textureSize, Screen.height / (int)textureSize, transform.position.y, (float)textureSize);
        

        SSPRCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(@"Assets\Cases\SSPR\Shaders\SSPR.compute");
        if (SSPRCS != null)
        {
            ClearKernel = SSPRCS.FindKernel("Clear");
            SSPRKernel = SSPRCS.FindKernel("SSPR");
            FillHoleKernel = SSPRCS.FindKernel("FillHole");
            
            cameraColorTexture = CreateRenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            SSPRResult = CreateRenderTexture((int)reflectParam.x, (int)reflectParam.y, 0, RenderTextureFormat.ARGB32);
            SSPRData = new uint[(int)(reflectParam.x * reflectParam.y)];
            SSPRBuffer = new ComputeBuffer(SSPRData.Length, sizeof(uint));
            SSPRBuffer.SetData(SSPRData);

            CopyCameraColor = CommandBufferPool.Get("CopyCameraColor");
            CopyCameraColor.Blit(mainCamera.targetTexture, cameraColorTexture);
            mainCamera.AddCommandBuffer(CameraEvent.AfterSkybox - 1, CopyCameraColor);

            SSPRCMD = CommandBufferPool.Get("SSPR");
            int threadGroupsX = Mathf.CeilToInt(reflectParam.x / 8 + 0.0001f);
            int threadGroupsY = Mathf.CeilToInt(reflectParam.y / 8 + 0.0001f);
            Clear(SSPRCMD, threadGroupsX, threadGroupsY);
            CalculateSSPR(SSPRCMD, threadGroupsX, threadGroupsY);
            FillHole(SSPRCMD, threadGroupsX, threadGroupsY);
            mainCamera.AddCommandBuffer(CameraEvent.AfterSkybox, SSPRCMD);

            SSPRCMD.SetGlobalTexture(reflectionTextureId, SSPRResult);
        }
        else Debug.LogError("计算着色器丢失或路径错误");


    }
    private void OnDestroy() 
    {
        SSPRBuffer.Release();
        CopyCameraColor.Release();
        SSPRCMD.Release();
    }
    private void OnWillRenderObject() 
    {
        if (SSPRCS == null) return;
        // 设置矩阵
        viewProjectionMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(inverseViewProjectionMatrixId, viewProjectionMatrix.inverse);
        float cameraDirX = mainCamera.transform.eulerAngles.x;
        cameraDirX = cameraDirX > 180 ? cameraDirX - 360 : cameraDirX;
        cameraDirX *= 0.00001f;
        reflectParam2 = new Vector4(stretchIntensity, stretchThreshold, cameraDirX, 1);
        Shader.SetGlobalVector(reflectionProp2Id, reflectParam2);
    }
    // 清楚数据
    private void Clear(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {
        cmd.SetComputeTextureParam(SSPRCS, ClearKernel, "SSPRResult", SSPRResult);
        cmd.SetComputeBufferParam(SSPRCS, ClearKernel, "SSPRBuffer", SSPRBuffer);
        cmd.DispatchCompute(SSPRCS, ClearKernel, threadGroupsX, threadGroupsY, 1);
    }
    // SSPR计算
    private void CalculateSSPR(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {
        // cmd.SetComputeTextureParam(SSPRCS, SSPRKernel, "_CameraColorTexture", cameraColorTexture);
        cmd.SetComputeVectorParam(SSPRCS, "_Param", reflectParam);
        // cmd.SetComputeVectorParam(SSPRCS, "_Param2", reflectParam2);
        // cmd.SetComputeTextureParam(SSPRCS, SSPRKernel, "SSPRResult", SSPRResult);
        cmd.SetComputeBufferParam(SSPRCS, SSPRKernel, "SSPRBuffer", SSPRBuffer);
        cmd.DispatchCompute(SSPRCS, SSPRKernel, threadGroupsX, threadGroupsY, 1);
    }
    // 填洞
    private void FillHole(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {
        cmd.SetComputeTextureParam(SSPRCS, FillHoleKernel, "_CameraColorTexture", cameraColorTexture);
        // cmd.SetComputeVectorParam(SSPRCS, "_Param", reflectParam);
        cmd.SetComputeTextureParam(SSPRCS, FillHoleKernel, "SSPRResult", SSPRResult);
        cmd.SetComputeBufferParam(SSPRCS, FillHoleKernel, "SSPRBuffer", SSPRBuffer);
        cmd.DispatchCompute(SSPRCS, FillHoleKernel, threadGroupsX, threadGroupsY, 1);
    }
    // 高斯模糊
    private void GaussianBlur(CommandBuffer cmd, int threadGroupsX, int threadGroupsY)
    {

    }
    private RenderTexture CreateRenderTexture(int width, int height, int depth,RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture(width, height, depth, format);
        rt.filterMode = FilterMode.Point;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
}
