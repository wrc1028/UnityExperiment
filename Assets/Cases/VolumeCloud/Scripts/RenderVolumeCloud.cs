using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RenderVolumeCloud : MonoBehaviour
{
    [TitleGroup("基础设置")]
    [LabelText("体积云着色器")]
    public Shader volumeCloudShader;
    [TitleGroup("基础设置")]
    [LabelText("体积云容器")]
    public Transform cloudContainer;
    [TitleGroup("基础设置")]
    [LabelText("密度步进数")]
    [Range(0, 200)]
    public int stepNums = 2;
    [TitleGroup("基础设置")]
    [LabelText("光线步进数")]
    [Range(0,20)]
    public int lightStepNums = 8;
    // --------
    [TitleGroup("贴图资源")]
    [LabelText("高度渐变贴图")]
    public Texture2D heightGradientTexture;
    [TitleGroup("贴图资源")]
    [LabelText("流体噪声贴图")]
    public Texture2D curlNoiseTexture;
    //-------------------------------
    [TitleGroup("基础噪声设置")]
    [LabelText("基础噪声")]
    public Texture3D shapeNoiseTexture;
    [TitleGroup("基础噪声设置")]
    [LabelText("基础噪声通道权重")]
    public Vector4 shapeNoiseWeight = new Vector4(1, 0.5f, 0.25f, 0.125f);
    [TitleGroup("基础噪声设置")]
    [LabelText("体积云大小")]
    public float cloudScale = 2;
    [TitleGroup("基础噪声设置")]
    [LabelText("体积云位移")]
    public Vector4 cloudOffset = new Vector4(0, 0, 0, 0.01f);
    [TitleGroup("基础噪声设置")]
    [LabelText("体积云移动方向及速度")]
    public Vector4 cloudMoveDirAndSpeed = new Vector4(1, 0, 0, 0.01f);
    // 体积云的密集程度
    [TitleGroup("基础噪声设置")]
    [LabelText("密度阈值")]
    [Range(-20, 20)]
    // 体积云的密度
    public float densityThreshold;
    [TitleGroup("基础噪声设置")]
    [LabelText("密度乘区")]
    [Range(0, 40)]
    public float densityMultiplier;
    //------------------------------
    [TitleGroup("细节噪声设置")]
    [LabelText("细节噪声")]
    public Texture3D detailNoiseTexture;
    [TitleGroup("细节噪声设置")]
    [LabelText("细节噪声权重")]
    public float detailWeight = 2;
    [TitleGroup("细节噪声设置")]
    [LabelText("细节噪声通道权重")]
    public Vector4 detailNoiseWeight = new Vector4(1, 0.5f, 0.25f, 0.125f);
    [TitleGroup("细节噪声设置")]
    [LabelText("细节噪声大小")]
    public float detailNoiseScale = 10;
    [TitleGroup("细节噪声设置")]
    [LabelText("细节噪声位移")]
    public Vector4 detailNoiseOffset = new Vector4(0, 0, 0, 0.01f);
    [TitleGroup("细节噪声设置")]
    [LabelText("细节噪声的移动方向及速度")]
    public Vector4 detailMoveDirAndSpeed = new Vector4(1, 0, 0, 0.01f);
    // 
    [TitleGroup("透光率")]
    [LabelText("取样点到光源间的光吸收系数")]
    [Range(0.001f, 0.5f)]
    public float lightAbsorptionTowardSun;
    [TitleGroup("透光率")]
    [LabelText("云层间的光吸收系数")]
    [Range(0.001f, 0.5f)]
    public float lightAbsorptionTowardCloud;
    [TitleGroup("透光率")]
    [LabelText("Phase数值")]
    public Vector4 darknessThreshold;

    private Material volumeCloudMaterial;
    private Camera currentCamera;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (volumeCloudMaterial == null)
            volumeCloudMaterial = new Material(volumeCloudShader);
        // 包围盒信息
        volumeCloudMaterial.SetVector("BoundMin", cloudContainer.position - cloudContainer.localScale / 2);
        volumeCloudMaterial.SetVector("BoundMax", cloudContainer.position + cloudContainer.localScale / 2);
        volumeCloudMaterial.SetInt("StepNums", Mathf.Max(0, stepNums));
        volumeCloudMaterial.SetInt("LightStepNums", Mathf.Max(0, lightStepNums));
        // 贴图
        volumeCloudMaterial.SetTexture("HeightGradientTexture", heightGradientTexture);
        volumeCloudMaterial.SetTexture("CurlNoiseTexture", curlNoiseTexture);

        // 基础贴图
        volumeCloudMaterial.SetTexture("ShapeNoise", shapeNoiseTexture);
        volumeCloudMaterial.SetVector("ShapeNoiseWeight", shapeNoiseWeight);
        volumeCloudMaterial.SetFloat("CloudScale", cloudScale);
        volumeCloudMaterial.SetVector("CloudOffset", cloudOffset);
        volumeCloudMaterial.SetFloat("DensityThreshold", densityThreshold);
        volumeCloudMaterial.SetFloat("DensityMultiplier", densityMultiplier);
        volumeCloudMaterial.SetVector("CloudMoveDirAndSpeed", cloudMoveDirAndSpeed);

        // 细节贴图
        volumeCloudMaterial.SetTexture("DetailNoise", detailNoiseTexture);
        volumeCloudMaterial.SetFloat("DetailWeight", detailWeight);
        volumeCloudMaterial.SetVector("DetailNoiseWeight", detailNoiseWeight);
        volumeCloudMaterial.SetFloat("DetailNoiseScale", detailNoiseScale);
        volumeCloudMaterial.SetVector("DetailNoiseOffset", detailNoiseOffset);
        volumeCloudMaterial.SetVector("DetailMoveDirAndSpeed", detailMoveDirAndSpeed);

        // 光照
        volumeCloudMaterial.SetFloat("LightAbsorptionTowardSun", lightAbsorptionTowardSun);
        volumeCloudMaterial.SetFloat("LightAbsorptionTowardCloud", lightAbsorptionTowardCloud);
        volumeCloudMaterial.SetVector("DarknessThreshold", darknessThreshold);

        volumeCloudMaterial.SetMatrix("_CameraInverseProjection", currentCamera.projectionMatrix.inverse);
        
        Graphics.Blit(src, dest, volumeCloudMaterial);
    }

    private void OnEnable()
    {
        currentCamera = GetComponent<Camera>();
    }
    
}
