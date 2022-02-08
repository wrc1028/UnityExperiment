using System;
using System.Reflection.Emit;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

public class SDFCalculate : MonoBehaviour
{
    [LabelText("贴图")]
    public Texture2D tex2D;
    [LabelText("计算着色器")]
    public ComputeShader CS;

    #region CUP 黑白有向距离生成
    // [Button("CPU计算")]
    public void CPUExcute()
    {
        if (tex2D == null || CS == null) return;
        RenderTexture originRT = CreateRenderTexture(tex2D.width, tex2D.height, RenderTextureFormat.ARGB32);
        Graphics.Blit(tex2D, originRT);
        
        Color[] colors = new Color[tex2D.width * tex2D.height];
        for (int x = 0; x < tex2D.width; x++)
        {
            for (int y = 0; y < tex2D.height; y++)
            {
                int index = x + y * tex2D.height;
                float dst = GetDistance(x, y, originRT);
                colors[index] = new Color(1 - dst, 1 - dst, 1 - dst, 1 - dst);
            }
        }
        Texture2D previewTex = new Texture2D(tex2D.width, tex2D.height, TextureFormat.ARGB32, false);
        previewTex.filterMode = FilterMode.Point;
        previewTex.SetPixels(colors);
        previewTex.Apply();
        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = previewTex;
    }
    private RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture(width, height, 0, format);
        rt.filterMode = FilterMode.Point;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
    private float GetDistance(int xIndex, int yIndex, RenderTexture rt)
    {
        RenderTexture resultRT = CreateRenderTexture(tex2D.width, tex2D.height, RenderTextureFormat.ARGB32);
        float[] dstResult = new float[tex2D.width * tex2D.height];
        ComputeBuffer resultBuffer = new ComputeBuffer(dstResult.Length, sizeof(float));
        resultBuffer.SetData(dstResult);
        CS.SetBuffer(0, "DstResult", resultBuffer);
        CS.SetTexture(0, "_OriginTexture", rt);
        CS.SetInts("_PixelID", xIndex, yIndex);
        CS.Dispatch(0, rt.width / 32, rt.height / 32, 1);
        float minValue = 1;
        resultBuffer.GetData(dstResult);
        for (int i = 0; i < dstResult.Length; i++)
        {
            minValue = Mathf.Min(minValue, dstResult[i]);
        }
        resultBuffer.Dispose();
        resultBuffer.Release();
        return minValue;
    }
    #endregion


    #region GUP 黑白有向距离生成
    [Button("GPU计算(黑白图)")]
    public void GPUExcute()
    {
        if (tex2D == null || CS == null) return;
        RenderTexture originRT = CreateRenderTexture(tex2D.width, tex2D.height, RenderTextureFormat.ARGB32);
        RenderTexture resultRT = CreateRenderTexture(tex2D.width, tex2D.height, RenderTextureFormat.ARGB32);
        Graphics.Blit(tex2D, originRT);
        CS.SetTexture(0, "_MulitGrayTexture", originRT);
        CS.SetTexture(0, "SDFResult", resultRT);
        int threadGroupsX = Mathf.CeilToInt(originRT.width / 32 + 0.001f);
        int threadGroupsY = Mathf.CeilToInt(originRT.height / 32 + 0.001f);
        CS.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = resultRT;
    }
    #endregion
    
    
}