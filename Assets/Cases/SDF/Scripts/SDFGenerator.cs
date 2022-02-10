using UnityEngine;
using UnityEditor;

namespace Custom.SDF
{
    public enum TextureType { 黑白图, 多段灰阶, }
    public enum Resolution { _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, }
    public enum SaveType { PNG, JPG, TGA, }
    public class SDFGenerator : EditorWindow
    {
        [MenuItem("UnityExperiment/SDFGenerator")]
        private static void ShowWindow()
        {
            var window = GetWindow<SDFGenerator>();
            window.titleContent = new GUIContent("SDFGenerator");
            window.minSize = new Vector2(540, 320);
            window.maxSize = new Vector2(540, 320);
            window.Show();
        }
        private GUIStyle lineSytle;
        private ComputeShader SDFSC;
        // left
        private Texture originTexture;
        private RenderTexture previewRT;
        private RenderTexture inputRT;
        private float cilpValue;
        private SaveType saveType;
        private Resolution resolution;
        private TextureType textureType;
        private bool isInvert;
        private bool isAssociateTex;
        // right
        private float inSpread;
        private float outSpread;
        private float minMapValue;
        private float maxMapValue;
        
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 60;
            DrawLeftContent();
            GUILayout.Label(string.Empty, lineSytle, GUILayout.Width(1), GUILayout.Height(420));
            DrawRightContent(textureType);
            GUILayout.EndHorizontal();
        }
        
        private void OnEnable()
        {
            // 白线
            lineSytle = new GUIStyle();
            lineSytle.normal.background = Texture2D.whiteTexture;
            // left
            previewRT = CreateRenderTexture(256, 256, RenderTextureFormat.ARGB32);
            cilpValue = 0;
            saveType = SaveType.PNG;
            resolution = Resolution._256;
            // right
            textureType = TextureType.黑白图;
            isInvert = false;
            isAssociateTex = false;
            minMapValue = 0;
            maxMapValue = 1;
            // Load Compute Shader
            SDFSC = AssetDatabase.LoadAssetAtPath<ComputeShader>("");
        }

        private void OnDestroy()
        {
            previewRT.Release();
        }
        private RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, format);
            rt.filterMode = FilterMode.Point;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }
        private void DrawLeftContent()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginArea(new Rect(10, 10, 256, 300));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("初始贴图", GUILayout.Width(60));
                    originTexture = EditorGUILayout.ObjectField(originTexture, typeof(Texture), true) as Texture;
                    textureType = (TextureType)EditorGUILayout.EnumPopup(textureType, GUILayout.Width(70));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
                GUILayout.Label(previewRT, GUILayout.Width(256), GUILayout.Height(256));
                GUILayout.Space(2);
                cilpValue = EditorGUILayout.Slider("裁切值", cilpValue, 0, 1);
                
                GUILayout.EndArea();
            }
            GUILayout.EndVertical();
        }
        private void DrawRightContent(TextureType type)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginArea(new Rect(275, 10, 255, 300));
                GUILayout.BeginHorizontal();
                {    
                    isInvert = EditorGUILayout.Toggle("颜色反转", isInvert);
                    GUILayout.Space(90);
                    isAssociateTex = EditorGUILayout.Toggle("关联原图", isAssociateTex);
                }
                GUILayout.EndHorizontal();
                if (type == TextureType.黑白图)
                {
                    inSpread = EditorGUILayout.Slider("内扩散", inSpread, 0, 1);
                    outSpread = EditorGUILayout.Slider("外扩散", outSpread, 0, 1);
                }
                EditorGUILayout.MinMaxSlider("结果映射", ref minMapValue, ref maxMapValue, 0, 1);
                if (GUILayout.Button("更新") && originTexture != null)
                {
                    originTexture.filterMode = FilterMode.Point;
                    inputRT = CreateRenderTexture(originTexture.width, originTexture.height, RenderTextureFormat.R8);
                    Graphics.Blit(originTexture, inputRT);
                    previewRT = CalculateSDF(inputRT);
                }
                GUILayout.EndArea();
                GUILayout.BeginArea(new Rect(275, 226, 255, 300));
                GUILayout.Label(string.Empty, lineSytle, GUILayout.Height(1));
                GUILayout.Space(1);
                resolution = (Resolution)EditorGUILayout.EnumPopup("分辨率", resolution);
                saveType = (SaveType)EditorGUILayout.EnumPopup("保存类型", saveType);
                if (GUILayout.Button("保存贴图"))
                {
                    
                }
                GUILayout.EndArea();
            }
            GUILayout.EndVertical();
        }


        private RenderTexture CalculateSDF(RenderTexture originTexture)
        {
            return originTexture;
        }
    }
}