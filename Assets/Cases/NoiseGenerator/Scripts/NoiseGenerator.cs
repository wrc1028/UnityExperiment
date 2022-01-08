using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
namespace Custom.Cloud
{
    [ExecuteInEditMode]
    public class NoiseGenerator : MonoBehaviour
    {
        [TitleGroup("基础设置")]
        [HorizontalGroup("基础设置/01")]
        [LabelText("计算着色器")]
        public ComputeShader noiseCompute;
        [HorizontalGroup("基础设置/02")]
        [LabelText("贴图设置")]
        public TextureSetting textureSetting;

        [HorizontalGroup("基础设置/03")]
        [LabelText("预览大小")]
        [Range(0, 1)]
        public float previewTextureSize;
        
        [TitleGroup("输出结果")]
        [HorizontalGroup("输出结果/01")]
        [Button("输出3D Texture")]
        public void Create3DTexture()
        {
            float[] result01 = Output3DNoiseTexture(textureSetting.resolution, textureSetting.rSetting);
            float[] result02 = Output3DNoiseTexture(textureSetting.resolution, textureSetting.gSetting);
            float[] result03 = Output3DNoiseTexture(textureSetting.resolution, textureSetting.bSetting);
            float[] result04 = Output3DNoiseTexture(textureSetting.resolution, textureSetting.aSetting);
            Color[] resultColor = new Color[result01.Length];
            for (int i = 0; i < resultColor.Length; i++)
            {
                resultColor[i] = new Color(result01[i], result02[i], result03[i], result04[i]);
            }
            Texture3D result3Dtexture = new Texture3D(textureSetting.resolution, textureSetting.resolution, textureSetting.resolution, TextureFormat.RGBA32, false);
            result3Dtexture.SetPixels(resultColor);
            result3Dtexture.Apply();
            string savePath = EditorUtility.SaveFilePanel("输出3D贴图结果", Application.dataPath, "New 3D Noise Texture", "asset");
            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    savePath = savePath.Substring(Application.dataPath.Length - 6);
                    AssetDatabase.CreateAsset(result3Dtexture, savePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("创建失败，原因" + e.Message);
                    Debug.Log("已改为在Assets目录下创建");
                }
            }
        }

        // 用于缓存资源
        private List<ComputeBuffer> buffersToRelease;
        // 用于预览的贴图
        [HideInInspector]
        public RenderTexture previewRT;

        [HideInInspector]
        public bool isNoiseSettingFoldout;
        [HideInInspector]
        public bool isTextureSettingFoldout;


        public float[] Output3DNoiseTexture(int resolution, NoiseSetting setting)
        {
            if (noiseCompute == null || textureSetting == null) return null;
            buffersToRelease = new List<ComputeBuffer>();
            int kernelHandle = noiseCompute.FindKernel("Create3DTexture");
            // 参数
            float[] result = new float[resolution * resolution * resolution];
            ComputeBuffer resultBuffer = CreateAndGetBuffer(result, sizeof(float), "NoiseResult", kernelHandle);
            SetNoiseValue(setting, kernelHandle);
            noiseCompute.SetInt("_Resolution", resolution);

            int threadGroupsX = Mathf.CeilToInt(result.Length / 512);
            noiseCompute.Dispatch(kernelHandle, threadGroupsX, 1, 1);

            resultBuffer.GetData(result);
            resultBuffer.Dispose();
            // 清空缓存
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
            return result;
        }
        
        // 预览噪点结果，输出一张RenderTexture
        public void PreviewNoiseResult(int resolution, NoiseSetting setting)
        {
            if (noiseCompute == null || textureSetting == null) return;

            buffersToRelease = new List<ComputeBuffer>();
            int kernelHandle = noiseCompute.FindKernel("Preview");
            // 预览贴图
            previewRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            previewRT.enableRandomWrite = true;
            previewRT.Create();
            noiseCompute.SetTexture(kernelHandle, "Result", previewRT);
            // 设置参数
            SetNoiseValue(setting, kernelHandle);
            // 调用
            int threadGroupsX = Mathf.CeilToInt(resolution / 8);
            int threadGroupsY = Mathf.CeilToInt(resolution / 8);
            noiseCompute.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);
            // 清空缓存
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
        }

        private void SetNoiseValue(NoiseSetting setting, int kernelHandle)
        {
            // 设置位置buffer
            CreateBuffer(setting.layer01Points, sizeof(float) * 3, "_Points01", kernelHandle);
            CreateBuffer(setting.layer02Points, sizeof(float) * 3, "_Points02", kernelHandle);
            CreateBuffer(setting.layer03Points, sizeof(float) * 3, "_Points03", kernelHandle);
            // 设置其他参数
            noiseCompute.SetInts("_CullNums", setting.layer01DivisionNum, setting.layer02DivisionNum, setting.layer03DivisionNum);
            noiseCompute.SetFloat("_SlideValue", setting.sliderValue);
            noiseCompute.SetFloat("_Persistence", setting.persistence);
            noiseCompute.SetBool("_Invert", setting.isInvert);

        }
        // 创建Buffer
        private void CreateBuffer(System.Array data, int stride, string buffName, int kernelHandle)
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Raw);
            buffersToRelease.Add(buffer);
            buffer.SetData(data);
            noiseCompute.SetBuffer(kernelHandle, buffName, buffer);
        }
        // 创建并返回Buffer，用来获取最终值
        private ComputeBuffer CreateAndGetBuffer(System.Array data, int stride, string buffName, int kernelHandle)
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Raw);
            buffersToRelease.Add(buffer);
            buffer.SetData(data);
            noiseCompute.SetBuffer(kernelHandle, buffName, buffer);
            return buffer;
        }
    }
}