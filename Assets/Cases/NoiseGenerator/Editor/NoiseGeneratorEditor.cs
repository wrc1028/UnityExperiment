using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;

namespace Custom.Cloud
{
    [CustomEditor(typeof(NoiseGenerator))]
    public class NoiseGeneratorEditor : OdinEditor
    {
        Editor textureSettingEditor;
        Editor noiseSettingEditor;
        NoiseGenerator noiseGenerator;
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (noiseGenerator.textureSetting != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("贴图设置");
                DrawSettingEditor(noiseGenerator.textureSetting, ref noiseGenerator.isTextureSettingFoldout, ref textureSettingEditor);
                
                GUILayout.Space(10);
                GUILayout.Label("噪点设置");
                DrawSettingEditor(noiseGenerator.textureSetting.currentNoiseSetting, ref noiseGenerator.isNoiseSettingFoldout, ref noiseSettingEditor);
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (noiseGenerator.textureSetting != null)
                    noiseGenerator.PreviewNoiseResult(noiseGenerator.textureSetting.resolution, noiseGenerator.textureSetting.currentNoiseSetting); 
            }
        }

        private void OnSceneGUI()
        {
            Handles.BeginGUI();
            int previewResolution = Mathf.CeilToInt(1024 * noiseGenerator.previewTextureSize);
            EditorGUI.DrawPreviewTexture(new Rect(0, 0, previewResolution, previewResolution), noiseGenerator.previewRT);
            Handles.EndGUI();
        }
        
        private void DrawSettingEditor(Object setting, ref bool isFoldout, ref Editor settingEditor)
        {
            if (setting != null)
            {
                isFoldout = EditorGUILayout.InspectorTitlebar(isFoldout, setting);
                if (isFoldout)
                {
                    CreateCachedEditor(setting, null, ref settingEditor);
                    settingEditor.OnInspectorGUI();
                }
            }
        }

        private new void OnEnable()
        {
            noiseGenerator = (NoiseGenerator)target;
        }
    }
}
