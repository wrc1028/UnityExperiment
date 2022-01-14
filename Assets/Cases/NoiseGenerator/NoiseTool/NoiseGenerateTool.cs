using System;
using UnityEngine;
using UnityEditor;

public class NoiseGenerateTool : EditorWindow 
{
    [MenuItem("UnityExperiment/NoiseGenerateTool")]
    private static void ShowWindow() 
    {
        var window = GetWindow<NoiseGenerateTool>();
        window.titleContent = new GUIContent("NoiseGenerateTool");
        window.minSize = new Vector2(720, 480);
        window.maxSize = new Vector2(720, 480);
        window.Show();
    }

    private RenderTexture prevRT;
    private float sliderValue;

    private void OnGUI() 
    {
        
    }

    private void DrawLeftContent()
    {
        
    }

    private void DrawRightContent()
    {

    }
}
