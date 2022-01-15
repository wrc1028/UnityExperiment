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
        // left
        public Channel channel;
        public RenderTexture prevRT;
        private RenderTexture prevRTR;
        private RenderTexture prevRTG;
        private RenderTexture prevRTB;
        private RenderTexture prevRTA;
        public Dimensionality dimensionality;
        public SaveType saveType;
        public Resolution2D resolution2D;
        public float samplerDepth;
        public Resolution3D resolution3D;
        private int resolution;
        private string extension;
        private string savePath;
        // right
        public NoiseBase[] noiseDatas;
        
        private void OnEnable()
        {
            lineSytle = new GUIStyle();
            lineSytle.normal.background = Texture2D.whiteTexture;

            noiseDatas = new NoiseBase[4];
            noiseDatas[0] = new NoiseBase();
            noiseDatas[1] = new NoiseBase();
            noiseDatas[2] = new NoiseBase();
            noiseDatas[3] = new NoiseBase();

            prevRT = CreateRenderTexture(256, 256, RenderTextureFormat.ARGB32);
            prevRTR = CreateRenderTexture(256, 256, RenderTextureFormat.R8);
            prevRTG = CreateRenderTexture(256, 256, RenderTextureFormat.R8);
            prevRTB = CreateRenderTexture(256, 256, RenderTextureFormat.R8);
            prevRTA = CreateRenderTexture(256, 256, RenderTextureFormat.R8);
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
            switch (channel)
            {
                case Channel.R:
                    DrawRightContent(ref noiseDatas[0]);
                    UpdatePreviewTexture(noiseDatas[0]);
                    break;
                case Channel.G:
                    DrawRightContent(ref noiseDatas[1]);
                    UpdatePreviewTexture(noiseDatas[1]);
                    break;
                case Channel.B:
                    DrawRightContent(ref noiseDatas[2]);
                    UpdatePreviewTexture(noiseDatas[2]);
                    break;
                default:
                    DrawRightContent(ref noiseDatas[3]);
                    UpdatePreviewTexture(noiseDatas[3]);
                    break;
            }
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
            GUILayout.Label(prevRT, GUILayout.Width(256), GUILayout.Height(256));
            GUILayout.Space(5);
            dimensionality = (Dimensionality)EditorGUILayout.EnumPopup("维度选择", dimensionality);
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
        private void DrawRightContent(ref NoiseBase noiseDatas)
        {
            GUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = 60;
            GUILayout.BeginArea(new Rect(286, 10, 424, 460));
            DrawLayerTextureSetting(1, ref noiseDatas.Layer01);
            if (noiseDatas.Layer01.isStartUsing)
            {
                DrawLayerTextureSetting(2, ref noiseDatas.Layer02);
                if (noiseDatas.Layer02.isStartUsing)
                {
                    DrawLayerTextureSetting(3, ref noiseDatas.Layer03);
                    if (noiseDatas.Layer03.isStartUsing)
                        DrawLayerTextureSetting(4, ref noiseDatas.Layer04);
                }
            }
            GUILayout.EndArea();
            GUILayout.EndVertical();
        }

        private void DrawLayerTextureSetting(int layerNum, ref NoiseBase.NoiseData noiseData)
        {
            noiseData.isStartUsing = EditorGUILayout.Toggle(string.Format("第{0}层", layerNum), noiseData.isStartUsing);
            if (!noiseData.isStartUsing) return;
            
            GUILayout.Label(String.Empty, lineSytle, GUILayout.Height(1));
            noiseData.mixWeight = EditorGUILayout.Slider("混合权重", noiseData.mixWeight, 0, 1);
            noiseData.noiseType = (NoiseType)EditorGUILayout.EnumPopup("噪声类型", noiseData.noiseType);

            noiseData.subdivideNum = EditorGUILayout.IntSlider("细分数", noiseData.subdivideNum, 1, 32);
            if (noiseData.prevSubdivideNum != noiseData.subdivideNum)
            {
                NoiseBase.CreateRandomValue(out noiseData.randomValue, noiseData.subdivideNum);
                noiseData.prevSubdivideNum = noiseData.subdivideNum;
            }

            noiseData.isInvert = EditorGUILayout.Toggle("反转颜色", noiseData.isInvert);
            GUILayout.Space(15);
        }

        private void UpdatePreviewTexture(NoiseBase noiseBase)
        {
            
        }
    }
}