using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Custom.Cloud
{
    [CustomEditor(typeof(TextureSetting))]
    public class TextureSettingEditor : Editor
    {
        TextureSetting textureSetting;
        SerializedProperty rSetting;
        SerializedProperty gSetting;
        SerializedProperty bSetting;
        SerializedProperty aSetting;
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            textureSetting.textureSize = (TextureSize)EditorGUILayout.EnumPopup("贴图分辨率", textureSetting.textureSize);
            if (EditorGUI.EndChangeCheck()) textureSetting.resolution = (int)textureSetting.textureSize;

            EditorGUI.BeginChangeCheck();
            textureSetting.channel = (Channel)EditorGUILayout.EnumPopup("通道选择", textureSetting.channel);
            if (EditorGUI.EndChangeCheck()) textureSetting.UpdateCurrentNoiseSetting(textureSetting.channel);
            
            EditorUtility.SetDirty(target);
            bool isSettingAssetEmpty = textureSetting.rSetting == null || textureSetting.gSetting == null || textureSetting.bSetting == null || textureSetting.aSetting == null;
            if (isSettingAssetEmpty)
            {
                textureSetting.isFoldoutSetting = EditorGUILayout.Foldout(textureSetting.isFoldoutSetting, "噪声设置资源");
                if (textureSetting.isFoldoutSetting)
                {
                    EditorGUILayout.PropertyField(rSetting, new GUIContent("R通道噪点设置"));
                    EditorGUILayout.PropertyField(gSetting, new GUIContent("G通道噪点设置"));
                    EditorGUILayout.PropertyField(bSetting, new GUIContent("B通道噪点设置"));
                    EditorGUILayout.PropertyField(aSetting, new GUIContent("A通道噪点设置"));
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        private void OnEnable()
        {
            textureSetting = (TextureSetting)target;
            rSetting = serializedObject.FindProperty("rSetting");
            gSetting = serializedObject.FindProperty("gSetting");
            bSetting = serializedObject.FindProperty("bSetting");
            aSetting = serializedObject.FindProperty("aSetting");
        }
    }
}
