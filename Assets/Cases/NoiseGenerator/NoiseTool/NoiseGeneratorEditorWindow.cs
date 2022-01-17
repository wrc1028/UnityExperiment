using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
namespace Custom.Noise
{
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
        private int previewkernel;

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
        public NoiseBase[] noiseDatas;
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
            noiseDatas = new NoiseBase[4];
            noiseDatas[(int)Channel.R] = new NoiseBase();
            noiseDatas[(int)Channel.R].Layer01.isUsed = true;
            noiseDatas[(int)Channel.G] = new NoiseBase();
            noiseDatas[(int)Channel.B] = new NoiseBase();
            noiseDatas[(int)Channel.A] = new NoiseBase();

            // 加载ComputeShader
            noiseGenerateCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Cases/NoiseGenerator/ComputeShader/NoiseGenerate.compute");
            buffersToRelease = new List<ComputeBuffer>();
            if (noiseGenerateCS != null)
            {
                previewkernel = noiseGenerateCS.FindKernel("Preview");
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
            DrawRightContent(ref noiseDatas[(int)channel]);
            UpdatePreviewTexture(noiseDatas[(int)channel], ref previewRT);
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
            if (GUILayout.Button(new GUIContent("保存预览", "保存预览结果到当前通道"), GUILayout.Width(60)))
            {
                
            }
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
                extension = dimensionality == Dimensionality._2DTexture ? saveType.ToString() : ".asset";
                savePath = EditorUtility.SaveFilePanel("贴图保存", Application.dataPath, "New Textrue", extension);
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
            DrawLayerTextureSetting(1, ref noiseBase.Layer01);
            if (noiseBase.Layer01.isUsed)
            {
                if (noiseBase.previewRT == null)
                    noiseBase.previewRT = CreateRenderTexture(256, 256, RenderTextureFormat.ARGB32);
                DrawLayerTextureSetting(2, ref noiseBase.Layer02);
                if (noiseBase.Layer02.isUsed)
                {
                    DrawLayerTextureSetting(3, ref noiseBase.Layer03);
                    if (noiseBase.Layer03.isUsed)
                        DrawLayerTextureSetting(4, ref noiseBase.Layer04);
                }
            }
            GUILayout.EndArea();
            GUILayout.EndVertical();
        }

        private void DrawLayerTextureSetting(int layerNum, ref NoiseBase.NoiseData noiseData)
        {
            GUILayout.BeginHorizontal();
            noiseData.isUsed = EditorGUILayout.Toggle(string.Format("第{0}层", layerNum), noiseData.isUsed);
            if (!noiseData.isUsed) return;
            noiseData.isInvert = EditorGUILayout.Toggle("颜色反转", noiseData.isInvert);
            EditorGUI.BeginChangeCheck();
            noiseData.noiseType = (NoiseType)EditorGUILayout.EnumPopup("噪声类型", noiseData.noiseType);
            if (EditorGUI.EndChangeCheck())
            {
                NoiseBase.CreateRandomValue(out noiseData.randomPoints, noiseData.subdivideNum, noiseData.noiseType);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Label(String.Empty, lineSytle, GUILayout.Height(1));
            noiseData.mixWeight = EditorGUILayout.Slider("混合权重", noiseData.mixWeight, 0.001f, 1);

            noiseData.subdivideNum = EditorGUILayout.IntSlider("细分数", noiseData.subdivideNum, 1, 32);
            if (noiseData.prevSubdivideNum != noiseData.subdivideNum)
            {
                NoiseBase.CreateRandomValue(out noiseData.randomPoints, noiseData.subdivideNum, noiseData.noiseType);
                noiseData.prevSubdivideNum = noiseData.subdivideNum;
            }
            EditorGUILayout.MinMaxSlider("数据映射", ref noiseData.minValue, ref noiseData.maxValue, -1, 1);
            GUILayout.Space(20);
        }

        private void UpdatePreviewTexture(NoiseBase noiseBase, ref RenderTexture previewRT)
        {
            // 如果计算着色器不存在或者当前通道的第一层未被使用，不渲染
            if (noiseGenerateCS == null || !noiseBase.Layer01.isUsed) 
            {
                previewRT = null;
                return;
            }
            // left prop
            noiseGenerateCS.SetFloat("_SamplerDepth", samplerDepth);
            // right prop
            SingleLayerNoiseSetting[] settings = new SingleLayerNoiseSetting[4];
            settings[0] = SetComputeBuffer(noiseBase.Layer01, 1);
            settings[1] = SetComputeBuffer(noiseBase.Layer02, 2);
            settings[2] = SetComputeBuffer(noiseBase.Layer03, 3);
            settings[3] = SetComputeBuffer(noiseBase.Layer04, 4);
            
            int stride = sizeof(float) * 3 + sizeof(int) * 3;
            CreateComputeBuffer(settings, stride, "_Settings", previewkernel);

            noiseGenerateCS.SetTexture(previewkernel, "PreviewResult", noiseBase.previewRT);
            noiseGenerateCS.Dispatch(previewkernel, 32, 32, 1);
            previewRT = noiseBase.previewRT;
            // 清空buffer缓存
            foreach (var buffer in buffersToRelease)
            {
                buffer.Release();
            }
        }
        private SingleLayerNoiseSetting SetComputeBuffer(NoiseBase.NoiseData noiseData, int indexLayer)
        {
            SingleLayerNoiseSetting setting = new SingleLayerNoiseSetting();
            // 当前层被启用
            if (noiseData.isUsed)
            {
                setting.noiseType = (int)noiseData.noiseType;
                setting.mixWeight = noiseData.mixWeight;
                setting.subdivideNum = noiseData.subdivideNum;
                setting.isInvert = noiseData.isInvert ? 1 : 0;
                setting.minValue = noiseData.minValue;
                setting.maxValue = noiseData.maxValue;
            }
            else setting.noiseType = -1;

            CreateComputeBuffer(noiseData.randomPoints, sizeof(float) * 3, string.Format("_Points0{0}", indexLayer), previewkernel);
            return setting;
        }
        private void CreateComputeBuffer(System.Array data, int stride, string bufferName, int kernelHandle)
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
            buffer.SetData(data);
            buffersToRelease.Add(buffer);
            noiseGenerateCS.SetBuffer(kernelHandle, bufferName, buffer);
        }
    }
}