using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RaySpere : MonoBehaviour
{
    [TitleGroup("基础设置")]
    [LabelText("体积云容器")]
    public Transform cloudContainer;
    [TitleGroup("基础设置")]
    [LabelText("着色器")]
    public Shader raySphereShader;
    [TitleGroup("基础设置")]
    [LabelText("球体半径")]
    public float radius = 50;
    [TitleGroup("基础设置")]
    [LabelText("高度")]
    public float height = 40;
    // 每次步进距离相等，步进长度为:球体直径/直径步进数
    [TitleGroup("基础设置")]
    [LabelText("直径步进数")]
    public int radiusStepNums = 100;
    [TitleGroup("贴图资源")]
    [LabelText("高度渐变贴图")]
    public Texture2D heightGradientTexture;
    

    [TitleGroup("基础噪声设置")]
    [LabelText("基础噪声贴图")]
    public Texture3D shapeTexture;
    [TitleGroup("基础噪声设置")]
    [LabelText("体积云大小")]
    public float cloudScale;
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



    private Material raySphereMat;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (raySphereMat == null) raySphereMat = new Material(raySphereShader);

        raySphereMat.SetMatrix("_CameraInverseProjection", GetComponent<Camera>().projectionMatrix.inverse);
        raySphereMat.SetVector("_RaySphereCenter", cloudContainer.position);
        raySphereMat.SetFloat("_SphereRadius", Mathf.Max(0, radius));
        raySphereMat.SetFloat("_SphereHeight", Mathf.Max(0, height));
        raySphereMat.SetFloat("_RadiusStepNums", Mathf.Max(1, radiusStepNums));
        raySphereMat.SetTexture("_HeightGradientTexture", heightGradientTexture);

        raySphereMat.SetTexture("_ShapeTexture", shapeTexture);
        raySphereMat.SetFloat("_CloudScale", Mathf.Max(0.01f, cloudScale));
        raySphereMat.SetFloat("_DensityThreshold", densityThreshold);
        raySphereMat.SetFloat("_DensityMultiplier", densityMultiplier);

        Graphics.Blit(src, dest, raySphereMat);
    }
}
