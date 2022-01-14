using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PerlinNoise : MonoBehaviour
{
    public ComputeShader perlinShader;
    [Range(1, 32)]
    public int subdivideNum = 8;
    [Range(1, 1024)]
    public int resolution = 64;
    [Range(0, 1)]
    public float depthSampler = 0;
    private int kernelHandle;
    private MeshRenderer meshRenderer;
    private RenderTexture previewRT;
    private List<ComputeBuffer> buffersToRelease;
    private Vector3[] randomVector3s;
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }
    private void OnValidate()
    {
        UpdatePreviewTexture();
    }
    [Button("更新")]
    public void UpdatePreviewTexture()
    {
        if (perlinShader == null) return;
        if (randomVector3s == null || subdivideNum * subdivideNum * subdivideNum != randomVector3s.Length) randomVector3s = CreateRandomVector3(subdivideNum);
        InitProp();
        // 传值
        perlinShader.SetTexture(kernelHandle, "TextureResult", previewRT);
        CreateComputeBuffer(CreateRandomVector(subdivideNum), sizeof(float) * 2, "_RandomVector", kernelHandle);
        CreateComputeBuffer(randomVector3s, sizeof(float) * 3, "_RandomVector3", kernelHandle);
        perlinShader.SetInt("_CellNums", subdivideNum);
        perlinShader.SetFloat("_Depth", depthSampler);
        int threadGroupsX = Mathf.CeilToInt(resolution / 8);
        perlinShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsX, 1);
        // 显示结果
        meshRenderer.sharedMaterial.mainTexture = previewRT;
        foreach (var buffer in buffersToRelease)
        {
            buffer.Release();
        }
        buffersToRelease.Clear();
    }

    private Vector2[] CreateRandomVector(int cellNums)
    {
        Vector2[] vector2s = new Vector2[cellNums * cellNums];
        for (int y = 0; y < cellNums; y++)
        {
            for (int x = 0; x < cellNums; x++)
            {
                int index = x + y * cellNums;
                vector2s[index] = new Vector2(UnityEngine.Random.value * 2f - 1f, UnityEngine.Random.value * 2f - 1f);
                vector2s[index] = vector2s[index].normalized;
            }
        }
        return vector2s;
    }
    private Vector3[] CreateRandomVector3(int cellNums)
    {
        Vector3[] vector3s = new Vector3[cellNums * cellNums * cellNums];
        for (int z = 0; z < cellNums; z++)
        {
            for (int y = 0; y < cellNums; y++)
            {
                for (int x = 0; x < cellNums; x++)
                {
                    int index = x + cellNums * (y + z * cellNums);
                    vector3s[index] = new Vector3(UnityEngine.Random.value * 2f - 1f, UnityEngine.Random.value * 2f - 1f, UnityEngine.Random.value * 2f - 1f);
                    vector3s[index] = vector3s[index].normalized;
                }
            }
        }
        return vector3s;
    }

    private void CreateComputeBuffer(System.Array data, int stride, string bufferName, int kernelHandle)
    {
        ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
        buffer.SetData(data);
        buffersToRelease.Add(buffer);
        perlinShader.SetBuffer(kernelHandle, bufferName, buffer);
    }

    private void InitProp()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (previewRT == null || resolution != previewRT.width)
        {
            previewRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            previewRT.filterMode = FilterMode.Point;
            previewRT.enableRandomWrite = true;
            previewRT.Create();
        }
        if (perlinShader != null) kernelHandle = perlinShader.FindKernel("PreviewResult");
        if (buffersToRelease == null) buffersToRelease = new List<ComputeBuffer>();
    }
}
