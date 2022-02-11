using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Custom.SDF
{
    public enum TextureType { 黑白图, 多段灰阶, }
    public enum Resolution { 原图分辨率, _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, }
    public enum SaveType { PNG, JPG, TGA, }
    public class SDFGenerator : EditorWindow
    {
        [MenuItem("UnityExperiment/SDFGenerator")]
        private static void ShowWindow()
        {
            var window = GetWindow<SDFGenerator>();
            window.titleContent = new GUIContent("SDFGenerator");
            window.minSize = new Vector2(276, 424);
            window.maxSize = new Vector2(276, 424);
            window.Show();
        }
        private GUIStyle lineSytle;
        private ComputeShader SDFShader;
        private int multiGraykernel;
        private int blackWhitekernel;
        private Material SDFMat;
        private string savePath;
        // left
        private Texture originTexture;
        private RenderTexture previewRT;
        private RenderTexture inputRT;
        private RenderTexture outputRT;
        private float cilpValue;
        private SaveType saveType;
        private Resolution resolution;
        private TextureType textureType;
        private bool isInvert;
        private bool isAssociateTex;
        private bool isUpdate;
        // right
        private float minMapValue;
        private float maxMapValue;
        private string minMaxSliderTitle;
        
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = 60;
            DrawContent();
            GUILayout.EndVertical();
        }
        
        private void OnEnable()
        {
            lineSytle = new GUIStyle();
            lineSytle.normal.background = Texture2D.whiteTexture;
            previewRT = CreateRenderTexture(256, 256, RenderTextureFormat.ARGB32, false);
            Graphics.Blit(Texture2D.grayTexture, previewRT);
            cilpValue = 0;
            saveType = SaveType.PNG;
            resolution = Resolution.原图分辨率;
            textureType = TextureType.黑白图;
            minMaxSliderTitle = "范围限制";
            isInvert = false;
            isAssociateTex = true;
            isUpdate = false;
            minMapValue = 0;
            maxMapValue = 1;
            // Load Compute Shader
            SDFShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(@"Assets\Cases\SDF\ComputeShaders\MulitGraySDF.compute");
            if (SDFShader != null)
            {
                multiGraykernel = SDFShader.FindKernel("MulitGray");
                blackWhitekernel = SDFShader.FindKernel("BlackWhite");
            }
            SDFMat = new Material(Shader.Find("Unlit/SDFCutout"));
        }

        private void OnDestroy()
        {

        }
        private RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format, bool isPoint)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, format);
            if (isPoint) rt.filterMode = FilterMode.Point;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        private void DrawContent()
        {
            GUILayout.BeginArea(new Rect(10, 10, 256, 600));
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("初始贴图", GUILayout.Width(60));
                originTexture = EditorGUILayout.ObjectField(originTexture, typeof(Texture), true) as Texture;
                textureType = (TextureType)EditorGUILayout.EnumPopup(textureType, GUILayout.Width(70));
            }
            GUILayout.EndHorizontal();
            // 预览图
            GUILayout.Label(previewRT, GUILayout.Width(256), GUILayout.Height(256));
            // 贴图设置
            minMaxSliderTitle = textureType == TextureType.黑白图 ? "范围限制" : "结果映射";
            EditorGUILayout.MinMaxSlider(minMaxSliderTitle, ref minMapValue, ref maxMapValue, 0, 1);
            GUILayout.BeginHorizontal();
            {    
                isInvert = EditorGUILayout.Toggle("颜色反转", isInvert);
                GUILayout.Space(20);
                isAssociateTex = EditorGUILayout.Toggle("关联原图", isAssociateTex);
                isUpdate = GUILayout.Button("更新", GUILayout.Width(50));
            }
            GUILayout.EndHorizontal();
            if (isUpdate && originTexture != null)
            {
                previewRT.Release();
                int kernelHandle = textureType == TextureType.黑白图 ? blackWhitekernel : multiGraykernel;
                cilpValue = textureType == TextureType.多段灰阶 && cilpValue <= 0.01f ? 0.01f : cilpValue;
                SDFMat.SetFloat("_CilpValue", cilpValue);
                Graphics.Blit(CalculateSDF(originTexture, kernelHandle), previewRT, SDFMat);
            }
            EditorGUI.BeginChangeCheck();
            cilpValue = EditorGUILayout.Slider("裁切值", cilpValue, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                previewRT.Release();
                cilpValue = textureType == TextureType.多段灰阶 && cilpValue <= 0.01f ? 0.01f : cilpValue;
                SDFMat.SetFloat("_CilpValue", cilpValue);
                Graphics.Blit(outputRT, previewRT, SDFMat);
            }
            // 输出GUI
            GUILayout.Space(10);
            resolution = (Resolution)EditorGUILayout.EnumPopup("分辨率", resolution);
            saveType = (SaveType)EditorGUILayout.EnumPopup("输出类型", saveType);
            if (GUILayout.Button("保存贴图") && outputRT != null)
            {
                savePath = EditorUtility.SaveFilePanel("贴图保存", Application.dataPath, "New Textrue", saveType.ToString());
                if (string.IsNullOrEmpty(savePath)) return;
                if (resolution != Resolution.原图分辨率)
                {
                    RenderTexture tempRT = outputRT;
                    outputRT = CreateRenderTexture((int)resolution, (int)resolution, RenderTextureFormat.R8, false);
                    Graphics.Blit(tempRT, outputRT);
                    tempRT.Release();
                }
                SaveRT2Texture(savePath, outputRT, saveType);
            }
            GUILayout.EndArea();
        }


        private RenderTexture CalculateSDF(Texture originTexture, int kernelHandle)
        {
            if (SDFShader == null) return null;
            if (originTexture.width != originTexture.height)
            {
                EditorUtility.DisplayDialog("长宽不一致", "暂不支持处理长宽尺寸不一致的贴图!", "确定");
                return null;
            }
            if (originTexture.width > 1024)
            {
                EditorUtility.DisplayDialog("尺寸过大", "暂不支持处理1024X1024以上尺寸的贴图!", "确定");
                return null;
            }
            if (inputRT != null) inputRT.Release();
                inputRT = CreateRenderTexture(originTexture.width, originTexture.height, RenderTextureFormat.R8, true);
            if (outputRT != null) outputRT.Release();
                outputRT = CreateRenderTexture(originTexture.width, originTexture.height, RenderTextureFormat.R8, false);
            originTexture.filterMode = FilterMode.Point;
            Graphics.Blit(originTexture, inputRT);

            SDFShader.SetTexture(kernelHandle, "_OriginTexture", inputRT);
            SDFShader.SetTexture(kernelHandle, "SDFResult", outputRT);
            SDFShader.SetBool("_IsInvert", isInvert);
            SDFShader.SetBool("_IsAssociateTex", isAssociateTex);
            SDFShader.SetFloats("_MapValue", minMapValue, maxMapValue);
            

            int threadGroupsX = Mathf.CeilToInt(inputRT.width / 32 + 0.001f);
            int threadGroupsY = Mathf.CeilToInt(inputRT.height / 32 + 0.001f);
            SDFShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);
            return outputRT;
        }
        
        private void SaveRT2Texture(string savePath, RenderTexture rt, SaveType saveType)
        {
            Texture2D tex2D = new Texture2D(rt.width, rt.height, TextureFormat.R8, false);
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
    }
}