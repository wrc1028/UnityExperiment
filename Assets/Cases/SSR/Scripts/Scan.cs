using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

public class Scan : MonoBehaviour
{
    public Camera scanCamera;
    public GameObject greenGackground;
    public Vector3 volumeScale;
    public Vector3Int resolution;
    private float stepSize;
    private int index;
    
    public Texture2D texture;
    public Texture2D texture1;
    public Texture2D texture2;
    public Texture2D texture3;
    private void OnEnable() 
    {
        ResetProp();
    }
    [Button("重置")]
    public void ResetProp()
    {
        index = 0;
        stepSize = volumeScale.y / resolution.y;
        scanCamera.nearClipPlane = 0.01f;
        greenGackground.transform.position = new Vector3(0, 100, 0);
    }
    [Button("下一个")]
    public void Next()
    {
        scanCamera.nearClipPlane = Mathf.Max(0.01f, index * stepSize);
        greenGackground.transform.position = new Vector3(0, 100 - (index + 1) * stepSize, 0);
        scanCamera.Render();
        SaveRT2Texture(string.Format(@"Assets\Cases\SSR\Scripts\Scan_{0}.png", index), scanCamera.targetTexture);
        index ++;
    }

    private void SaveRT2Texture(string savePath, RenderTexture rt)
    {
        Texture2D tex2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2D.Apply();
        RenderTexture.active = prev;

        Byte[] texBytes = tex2D.EncodeToPNG();
        FileStream texFile = File.Open(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        BinaryWriter texWriter = new BinaryWriter(texFile);
        texWriter.Write(texBytes);
        texFile.Close();
        Texture2D.DestroyImmediate(tex2D);
        tex2D = null;
        AssetDatabase.Refresh();
    }
    [Button("创建3D贴图")]
    public void CreateVolumeTexture()
    {
        Texture3D volume = new Texture3D(64, 64, 64, TextureFormat.R8, 0);
        volume.filterMode = FilterMode.Point;
        Color[] volumeColors = new Color[64 * 64 * 64];
        for (int i = 0; i < 64; i++)
        {
            switch (i)
            {
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                    GetColor(texture1, i, ref volumeColors);
                    break;
                default:
                    GetColor(texture, i, ref volumeColors);
                    break;
            }
        }
        volume.SetPixels(volumeColors);
        AssetDatabase.CreateAsset(volume, "Assets/Volume_1.asset");
        AssetDatabase.Refresh();
    }

    private void GetColor(Texture2D tex, int yIndex, ref Color[] results)
    {
        Color[] colors = tex.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            int xIndex = i % 64;
            int zIndex = i / 64;
            results[xIndex + 64 * (zIndex + yIndex * 64)] = colors[i];
        }
    }
    public Texture3D volume;
    private List<int> blackID;
    private List<int> whiteID;
    [Button("计算SDF")]
    public void CalculateSDF()
    {
        Color[] colors = volume.GetPixels();
        blackID = new List<int>();
        whiteID = new List<int>();
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i].r < 0.1f) blackID.Add(i);
            else whiteID.Add(i);
        }
        
        for (int x = 0; x < whiteID.Count; x++)
        {
            float dst = 10000000;
            Vector3Int whitePos = GetIDFromIndex(whiteID[x]);
            for (int y = 0; y < blackID.Count; y++)
            {
                Vector3Int blackPos = GetIDFromIndex(blackID[y]);
                dst = Mathf.Min(dst, Vector3Int.Distance(whitePos, blackPos));
            }
            colors[whiteID[x]] = new Color(dst / 64, 0, 0, 1);
        }
        
        Texture3D volumeOut = new Texture3D(64, 64, 64, TextureFormat.R16, 0);
        // volumeOut.filterMode = FilterMode.Point;
        volumeOut.SetPixels(colors);
        AssetDatabase.CreateAsset(volumeOut, "Assets/Volume_1_SDF.asset");
        AssetDatabase.Refresh();
    }
    private Vector3Int GetIDFromIndex(int index)
    {
        int x = (index % 64) % 64;
        int y = index / (64 * 64);
        int z = (index / 64) % 64;
        return new Vector3Int(x, y, z);
    }
}
