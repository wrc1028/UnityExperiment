using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class SingleScattering : MonoBehaviour
{
    [TitleGroup("基础参数")]
    [LabelText("渲染着色器")]
    public Shader scatteringShader;
    [TitleGroup("基础参数")]
    [LabelText("散射采样步进数")]
    [Range(1, 20)]
    public int scatteringStepNums = 8;
    [TitleGroup("基础参数")]
    [LabelText("光学距离采样步进数")]
    [Range(1, 20)]
    public int opticalDepthStepNums = 8;
    [TitleGroup("基础参数")]
    [LabelText("波长")]
    public Vector3 waveLength = new Vector3(700, 530, 440);
    [TitleGroup("基础参数")]
    [LabelText("散射强度强度")]
    [Range(0.001f, 0.5f)]
    public float scatteringStrength = 1;

    [TitleGroup("行星数据")]
    [LabelText("行星位置")]
    public Transform earthTransform;
    [TitleGroup("行星数据")]
    [LabelText("行星半径")]
    [Min(0.001f)]
    public float earthRadius = 42.8f;
    [TitleGroup("行星数据")]
    [LabelText("大气层高度")]
    [Min(0.001f)]
    public float atmosphereHeight = 10;
    [TitleGroup("行星数据")]
    [LabelText("大气层密度衰减控制")]
    [Min(0.001f)]
    public float atmosphericDensityCtrl = 1;

    private void Start()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
    private Material scatteringMat;
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (scatteringMat == null) scatteringMat = new Material(scatteringShader);
        float scatterR = Pow4(400 / waveLength.x) * scatteringStrength;
        float scatterG = Pow4(400 / waveLength.y) * scatteringStrength;
        float scatterB = Pow4(400 / waveLength.z) * scatteringStrength;
        Vector3 scatteringValue = new Vector3(scatterR, scatterG, scatterB);

        scatteringMat.SetInt("_ScatteringStepNums", scatteringStepNums);
        scatteringMat.SetInt("_OpticalDepthStepNums", opticalDepthStepNums);
        scatteringMat.SetVector("_ScatteringValue", scatteringValue);

        scatteringMat.SetVector("_EarthCenter", earthTransform.position);
        scatteringMat.SetFloat("_EarthRadius", Mathf.Max(0, earthRadius));
        scatteringMat.SetFloat("_AtmosphereHeight", Mathf.Max(0, atmosphereHeight));
        scatteringMat.SetFloat("_AtmosphericDensityCtrl", Mathf.Max(0, atmosphericDensityCtrl));


        scatteringMat.SetMatrix("_CameraInverseProjection", GetComponent<Camera>().projectionMatrix.inverse);
        Graphics.Blit(src, dest, scatteringMat);
    }

    private float Pow4(float value)
    {
        return value * value * value * value;
    }
}
