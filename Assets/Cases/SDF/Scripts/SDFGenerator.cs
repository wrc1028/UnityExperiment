using UnityEngine;
using UnityEditor;

public class SDFGenerator : EditorWindow
{
    [MenuItem("UnityExperiment/SDFGenerator")]
    private static void ShowWindow()
    {
        var window = GetWindow<SDFGenerator>();
        window.titleContent = new GUIContent("SDFGenerator");
        window.Show();
    }

    private void OnGUI()
    {
        
    }
}
