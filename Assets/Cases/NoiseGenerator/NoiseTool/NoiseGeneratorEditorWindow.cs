using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
namespace Custom.Noise
{
    // 新的GUI
    public class NoiseGeneratorEditorWindow : EditorWindow 
    {
        [MenuItem("UnityExperiment/NoiseGenerator")]
        private static void ShowWindow() 
        {
            var window = GetWindow<NoiseGeneratorEditorWindow>();
            window.titleContent = new GUIContent("NoiseGenerateTool");
            window.minSize = new Vector2(720, 480);
            window.maxSize = new Vector2(720, 480);
            window.Show();
        }

        // GUIStyle
        private GUIStyle lineSytle;
        // compute shader
        private ComputeShader noiseGenerateCS;
        private int previewKernel;
        private int outputTexture2DKernel;
        private int combineChannelKernel;
        private int outputResultKernel;
        private List<ComputeBuffer> buffersToRelease;
        // left
        public Channel channel;
        public RenderTexture previewRT;
        public Dimensionality dimensionality;
        public SaveType saveType;
        public Resolution2D resolution2D;
        public float samplerDepth;
        public Resolution3D resolution3D;
        // right
        public NoiseBase[] noiseBases;
        public float weightAdjust;
        // save data
        private int resolution;
        private string extension;
        private string savePath;
        
        private void OnEnable()
        {
            // 初始化设置
            channel = Channel.R;
            previewRT = CreateRenderTexture(256, 256, RenderTextureFormat.ARGB32);
            dimensionality = Dimensionality._2DTexture;
            saveType = SaveType.PNG;
            resolution2D = Resolution2D._256x256;
            samplerDepth = 0;
            resolution3D = Resolution3D._64x64;

            // 白线
            lineSytle = new GUIStyle();
            lineSytle.normal.background = Texture2D.whiteTexture;

            // 初始化数据
            weightAdjust = 1;
            noiseBases = new NoiseBase[4];
            noiseBases[(int)Channel.R] = new NoiseBase();
            noiseBases[(int)Channel.R].noiseLayerCount = 1;
            noiseBases[(int)Channel.G] = new NoiseBase();
            noiseBases[(int)Channel.B] = new NoiseBase();
            noiseBases[(int)Channel.A] = new NoiseBase();

            // 加载ComputeShader
            noiseGenerateCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Cases/NoiseGenerator/ComputeShader/NoiseGenerate.compute");
            buffersToRelease = new List<ComputeBuffer>();
            if (noiseGenerateCS != null)
            {
                previewKernel = noiseGenerateCS.FindKernel("Preview");
                outputTexture2DKernel = noiseGenerateCS.FindKernel("OutputTexture2D");
                combineChannelKernel = noiseGenerateCS.FindKernel("CombineChannel");
                outputResultKernel = noiseGenerateCS.FindKernel("OutputResult");
            }
        }

        private void OnDestroy()
        {
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
        }

        private RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, format);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        private void OnGUI() 
        {
            GUILayout.BeginHorizontal();
            DrawLeftContent();
            DrawRightContent(ref noiseBases[(int)channel]);
            CalculateTexture2D(noiseBases[(int)channel], ref previewRT, "PreviewResult", previewKernel);
            GUILayout.EndHorizontal();
        }

        // 绘制左边的内容:包括预览图，2D/3D切换，显示通道，保存设置等
        private void DrawLeftContent()
        {
            GUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = 60;
            GUILayout.BeginArea(new Rect(10, 10, 256, 460));
            GUILayout.BeginHorizontal();
            channel = (Channel)EditorGUILayout.EnumPopup("显示通道", channel);
            GUILayout.EndHorizontal();
            GUILayout.Label(previewRT, GUILayout.Width(256), GUILayout.Height(256));
            GUILayout.Space(5);
            dimensionality = (Dimensionality)EditorGUILayout.EnumPopup("2D&3D", dimensionality);
            if (dimensionality == Dimensionality._3DTexture)
            {
                samplerDepth = EditorGUILayout.Slider("采样深度", samplerDepth, 0, 1);
                resolution3D = (Resolution3D)EditorGUILayout.EnumPopup("分辨率", resolution3D);
            }
            else
            {
                saveType = (SaveType)EditorGUILayout.EnumPopup("保存类型", saveType);
                resolution2D = (Resolution2D)EditorGUILayout.EnumPopup("分辨率", resolution2D);
            }
            GUILayout.Space(5);
            if (GUILayout.Button("保存贴图"))
            {
                // TODO: 保存当前设置
                resolution = dimensionality == Dimensionality._2DTexture ? (int)resolution2D : (int)resolution3D;
                extension = dimensionality == Dimensionality._2DTexture ? saveType.ToString() : "asset";
                savePath = EditorUtility.SaveFilePanel("贴图保存", Application.dataPath, "New Textrue", extension);
                if (dimensionality == Dimensionality._2DTexture) OutputTexture2D(savePath, resolution, saveType);
                else
                {
                    savePath = savePath.Substring(Application.dataPath.Length - 6);
                    OutputTexture3D(savePath, resolution);
                }
            }
            GUILayout.EndArea();
            GUILayout.EndVertical();
        }

        // 绘制右边的内容:噪声图类型选择，多层噪声图叠加设置等
        private void DrawRightContent(ref NoiseBase noiseBase)
        {
            GUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = 60;
            GUILayout.BeginArea(new Rect(286, 10, 424, 460));
            GUILayout.BeginHorizontal();
            noiseBase.noiseLayerCount = EditorGUILayout.IntSlider("噪点层数", noiseBase.noiseLayerCount, 0, 4);
            noiseBase.mixType = (MixType)EditorGUILayout.EnumPopup("混合类型", noiseBase.mixType, GUILayout.Width(180));
            GUILayout.EndHorizontal();
            weightAdjust = EditorGUILayout.Slider("权重调整", weightAdjust, 0, 2);

            for (int i = 0; i < 4; i++)
            {
                if (i < noiseBase.noiseLayerCount) DrawLayerTextureSetting(i, ref noiseBase.layers[i], noiseBase.mixType == MixType.Custom);
            }
            GUILayout.EndArea();
            GUILayout.EndVertical();
        }

        private void DrawLayerTextureSetting(int layerNum, ref NoiseBase.NoiseData noiseData, bool customMixType)
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("第{0}层", layerNum + 1));
            noiseData.isInvert = EditorGUILayout.Toggle("颜色反转", noiseData.isInvert);
            EditorGUI.BeginChangeCheck();
            noiseData.noiseType = (NoiseType)EditorGUILayout.EnumPopup("噪声类型", noiseData.noiseType, GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck())
            {
                NoiseBase.CreateRandomValue(out noiseData.randomPoints, noiseData.cellNums, noiseData.noiseType);
            }
            GUILayout.EndHorizontal();

            noiseData.cellNums = EditorGUILayout.IntSlider("噪点数量", noiseData.cellNums, 1, 32);
            if (noiseData.prevSubdivideNum != noiseData.cellNums)
            {
                NoiseBase.CreateRandomValue(out noiseData.randomPoints, noiseData.cellNums, noiseData.noiseType);
                noiseData.prevSubdivideNum = noiseData.cellNums;
            }
            EditorGUILayout.MinMaxSlider("数据映射", ref noiseData.minValue, ref noiseData.maxValue, 0, 1);
            if (customMixType)
                noiseData.mixWeight = EditorGUILayout.Slider("混合权重", noiseData.mixWeight, 0.001f, 1);
        }

        #region 预览贴图
        private void CalculateTexture2D(NoiseBase noiseBase, ref RenderTexture resultRT, string resultName, int kernelHandle)
        {
            // 如果计算着色器不存在，不渲染
            // TODO: 显示第二层的情况
            if (noiseGenerateCS == null) return;
            // left prop
            noiseGenerateCS.SetBool("_IsTowDimension", dimensionality == Dimensionality._2DTexture);
            noiseGenerateCS.SetFloat("_SamplerDepth", samplerDepth);
            // right prop
            SetNoiseData(noiseBase, kernelHandle);
            noiseGenerateCS.SetTexture(kernelHandle, resultName, resultRT);
            int threadGroups = Mathf.CeilToInt((resultRT.width + 0.001f) / 8);
            noiseGenerateCS.Dispatch(kernelHandle, threadGroups, threadGroups, 1);
            // 清空buffer缓存
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
        }
        private void CalculateTexture3D(NoiseBase noiseBase, ref float[] result, int resolution, string resultName, int kernelHandle)
        {
            // 如果计算着色器不存在，不渲染
            if (noiseGenerateCS == null || noiseBase.noiseLayerCount == 0 || result.Length == 0) return;

            SetNoiseData(noiseBase, kernelHandle);
            noiseGenerateCS.SetInt("_Resolution", resolution);
            ComputeBuffer resultBuffer = new ComputeBuffer(result.Length, sizeof(float));
            resultBuffer.SetData(result);
            buffersToRelease.Add(resultBuffer);
            noiseGenerateCS.SetBuffer(kernelHandle, resultName, resultBuffer);

            int threadGroups = Mathf.CeilToInt(((float)result.Length + 0.0001f) / 256f);
            noiseGenerateCS.Dispatch(kernelHandle, threadGroups, 1, 1);

            resultBuffer.GetData(result);
            resultBuffer.Dispose();
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
        }
        private void SetNoiseData(NoiseBase noiseBase, int kernelHandle)
        {
            SingleLayerNoiseSetting[] settings = new SingleLayerNoiseSetting[4];
            for (int i = 0; i < 4; i++)
            {
                settings[i] = SetComputeBuffer(noiseBase.layers[i], noiseBase.noiseLayerCount, noiseBase.mixType, i, kernelHandle);
            }
            
            int stride = sizeof(float) * 3 + sizeof(int) * 3;
            CreateComputeBuffer(settings, stride, "_Settings", kernelHandle);
        }
        private SingleLayerNoiseSetting SetComputeBuffer(NoiseBase.NoiseData noiseData, int noiseLayerCount, MixType mixType, int indexLayer, int kernelHandle)
        {
            SingleLayerNoiseSetting setting = new SingleLayerNoiseSetting();
            // 当前层被启用
            if (indexLayer < noiseLayerCount)
            {
                setting.noiseType = (int)noiseData.noiseType;
                setting.mixWeight = GetMixWeight(noiseData, noiseLayerCount, mixType, indexLayer); // TODO:
                setting.cellNums = noiseData.cellNums;
                setting.isInvert = noiseData.isInvert ? 1 : 0;
                setting.minValue = noiseData.minValue;
                setting.maxValue = noiseData.maxValue;
            }
            else setting.noiseType = -1;

            CreateComputeBuffer(noiseData.randomPoints, sizeof(float) * 3, string.Format("_Points0{0}", indexLayer + 1), kernelHandle);
            return setting;
        }
        private float GetMixWeight(NoiseBase.NoiseData noiseData, int noiseLayerCount, MixType mixType, int indexLayer)
        {
            float weight = 0;
            switch (mixType)
            {
                case MixType.Uniform:
                    weight = 1.0f / noiseLayerCount;
                    break;
                case MixType.FBM:
                    indexLayer ++;
                    weight = 1.0f / (indexLayer * indexLayer);
                    break;
                case MixType.FBM_Cell:
                    weight = 1.0f / noiseData.cellNums;
                    break;
                default:
                    weight = noiseData.mixWeight;
                    break;
            }
            return weight;
        }
        private void CreateComputeBuffer(System.Array data, int stride, string bufferName, int kernelHandle)
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
            buffer.SetData(data);
            buffersToRelease.Add(buffer);
            noiseGenerateCS.SetBuffer(kernelHandle, bufferName, buffer);
        }
        #endregion
        #region 输出贴图
        private void OutputTexture2D(string savePath, int resolution, SaveType saveType)
        {
            if (string.IsNullOrEmpty(savePath) || noiseGenerateCS == null) return;
            int validDataCount = 0;
            NoiseBase tempNoiseBase = null;
            if (noiseBases[(int)Channel.R].noiseLayerCount > 0) { validDataCount ++; tempNoiseBase = noiseBases[(int)Channel.R]; }
            if (noiseBases[(int)Channel.G].noiseLayerCount > 0) { validDataCount ++; tempNoiseBase = noiseBases[(int)Channel.G]; }
            if (noiseBases[(int)Channel.B].noiseLayerCount > 0) { validDataCount ++; tempNoiseBase = noiseBases[(int)Channel.B]; }
            if (noiseBases[(int)Channel.A].noiseLayerCount > 0) { validDataCount ++; tempNoiseBase = noiseBases[(int)Channel.A]; }
            
            if (validDataCount == 0)
            {
                EditorUtility.DisplayDialog("无输出结果!", "最少需要一个通道有结果!", "确定");
                return;
            }
            else if (validDataCount == 1) 
            {
                RenderTexture grayRT = CreateRenderTexture(resolution, resolution, RenderTextureFormat.R8);
                CalculateTexture2D(tempNoiseBase, ref grayRT, "SingleChannelResult", outputTexture2DKernel);
                SaveRT2Texture(savePath, grayRT, TextureFormat.R8, saveType);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    RenderTexture channel = CreateRenderTexture(resolution, resolution, RenderTextureFormat.R8);
                    CalculateTexture2D(noiseBases[i], ref channel, "SingleChannelResult", outputTexture2DKernel);
                    noiseGenerateCS.SetTexture(combineChannelKernel, string.Format("_{0}Channel", ((Channel)i).ToString()), channel);
                }
                RenderTexture result = CreateRenderTexture(resolution, resolution, RenderTextureFormat.ARGB32);
                noiseGenerateCS.SetTexture(combineChannelKernel, "Texture2DResult", result);
                int threadGroups = Mathf.CeilToInt((resolution + 0.001f) / 8);
                noiseGenerateCS.Dispatch(combineChannelKernel, threadGroups, threadGroups, 1);
                SaveRT2Texture(savePath, result, TextureFormat.RGBA32, saveType);
            }
        }
        private void SaveRT2Texture(string savePath, RenderTexture rt, TextureFormat format, SaveType saveType)
        {
            Texture2D tex2D = new Texture2D(rt.width, rt.height, format, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = prev;

            Byte[] texBytes;
            switch (saveType)
            {
                case SaveType.PNG:
                    texBytes = tex2D.EncodeToPNG();
                    break;
                case SaveType.TGA:
                    texBytes = tex2D.EncodeToTGA();
                    break;
                default:
                    texBytes = tex2D.EncodeToJPG();
                    break;
            }
            FileStream texFile = File.Open(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryWriter texWriter = new BinaryWriter(texFile);
            texWriter.Write(texBytes);
            texFile.Close();
            Texture2D.DestroyImmediate(tex2D);
            tex2D = null;
            AssetDatabase.Refresh();
        }
        #endregion
        #region 输出3D资源
        private void OutputTexture3D(string savePath, int resolution)
        {
            float[] rChannel = new float[resolution * resolution * resolution];
            CalculateTexture3D(noiseBases[0], ref rChannel, resolution, "SingleLayerResult", outputResultKernel);
            float[] gChannel = new float[resolution * resolution * resolution];
            CalculateTexture3D(noiseBases[1], ref gChannel, resolution, "SingleLayerResult", outputResultKernel);
            float[] bChannel = new float[resolution * resolution * resolution];
            CalculateTexture3D(noiseBases[2], ref bChannel, resolution, "SingleLayerResult", outputResultKernel);
            float[] aChannel = new float[resolution * resolution * resolution];
            CalculateTexture3D(noiseBases[3], ref aChannel, resolution, "SingleLayerResult", outputResultKernel);

            UnityEngine.Color[] resultColors = new UnityEngine.Color[rChannel.Length];
            for (int i = 0; i < resultColors.Length; i++)
            {
                resultColors[i] = new UnityEngine.Color(rChannel[i], gChannel[i], bChannel[i], aChannel[i]);
            }
            Texture3D tex3D = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, 0);
            tex3D.SetPixels(resultColors);
            tex3D.Apply();
            AssetDatabase.CreateAsset(tex3D, savePath);
            AssetDatabase.Refresh();
        }
        #endregion
    }
}