using UnityEditor;
namespace Custom.Cloud
{
    [CustomEditor(typeof(NoiseSetting))]
    public class NoiseSettingEditor : Editor
    {
        NoiseSetting noiseSetting;
        public override void OnInspectorGUI()
        {
            
            EditorGUI.BeginChangeCheck();
            noiseSetting.layer01DivisionNum = EditorGUILayout.IntSlider("第一层细分数", noiseSetting.layer01DivisionNum, 1, 32);
            if (EditorGUI.EndChangeCheck())
            {
                noiseSetting.layer01Points = noiseSetting.GetRandomPoint(noiseSetting.layer01DivisionNum);
            }

            EditorGUI.BeginChangeCheck();
            noiseSetting.layer02DivisionNum = EditorGUILayout.IntSlider("第二层细分数", noiseSetting.layer02DivisionNum, 1, 32);
            if (EditorGUI.EndChangeCheck())
            {
                noiseSetting.layer02Points = noiseSetting.GetRandomPoint(noiseSetting.layer02DivisionNum);
            }

            EditorGUI.BeginChangeCheck();
            noiseSetting.layer03DivisionNum = EditorGUILayout.IntSlider("第三层细分数", noiseSetting.layer03DivisionNum, 1, 32);
            if (EditorGUI.EndChangeCheck())
            {
                noiseSetting.layer03Points = noiseSetting.GetRandomPoint(noiseSetting.layer03DivisionNum);
            }
            
            noiseSetting.persistence = EditorGUILayout.Slider("叠加程度", noiseSetting.persistence, 0, 1);
            noiseSetting.sliderValue = EditorGUILayout.Slider("采样高度", noiseSetting.sliderValue, 0, 1);
            noiseSetting.isInvert = EditorGUILayout.Toggle("反转颜色", noiseSetting.isInvert);

            EditorUtility.SetDirty(target);
        }

        private void OnEnable()
        {
            noiseSetting = (NoiseSetting)target;
        }
    }
}
