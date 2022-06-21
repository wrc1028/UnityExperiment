using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class Planet : MonoBehaviour
{
    private const float k_GoldenRatio = 0.6180339887f;
    public float planetRadius = 10;
    [Range(1, 128)]
    public int iterations = 1;
    public Material planetMat;

    /// <summary>
    /// 基础的12个顶点位置
    /// </summary>
    private Vector3[] baseVertices;
    [Button("Create Meshes")]
    private void CreateMeshes() 
    {
        baseVertices = new Vector3[12];
        // 南北极
        baseVertices[0] = new Vector3(0, 1, 0);
        CalculateOtherBaseVertices(0, ref baseVertices);
        baseVertices[6] = new Vector3(0, -1, 0);
        CalculateOtherBaseVertices(6, ref baseVertices);
        // 根据这12个点创建20个物体并赋予网格
        CreateRegion(baseVertices);
    }

    /// <summary>
    /// 根据极点算出其他10个顶点的位置
    /// </summary>
    /// <param name="poleIndex">极点的索引</param>
    /// <param name="vertices">顶点</param>
    private void CalculateOtherBaseVertices(int poleIndex, ref Vector3[] vertices)
    {
        Vector3 poleDir = vertices[poleIndex].normalized;
        float sign = vertices[poleIndex].y > 0 ? 1 : -1;
        for (int i = 1; i < 6; i++)
        {
            int vertexIndex = i + poleIndex;
            if (i == 1)
            {
                float cosAlpha = 1 / (Mathf.Sqrt(1 + k_GoldenRatio * k_GoldenRatio));
                float sinAlpha = Mathf.Sqrt(1 - cosAlpha * cosAlpha);
                float sinLength = 2 * sinAlpha;
                float positionX = sinLength * cosAlpha;
                float positionY = 1 - sinLength * sinAlpha;
                vertices[vertexIndex] = new Vector3(positionX, positionY, 0) * sign;
            }
            else
            {
                float baseLength = Mathf.Abs(vertices[poleIndex + 1].x);
                Vector3 priorDir = new Vector3(vertices[vertexIndex - 1].x, 0 , vertices[vertexIndex - 1].z).normalized;
                Vector3 crossDir = Vector3.Cross(priorDir, poleDir).normalized;
                float cos72 = Mathf.Cos(0.4f * Mathf.PI);
                float sin72 = Mathf.Sin(0.4f * Mathf.PI);
                vertices[vertexIndex] = baseLength * (cos72 * priorDir + sin72 * crossDir) + new Vector3(0, vertices[poleIndex + 1].y, 0);
            }
        }
    }
    
    private void CreateRegion(Vector3[] vertices)
    {
        int childCount = transform.childCount;
        while (childCount-- > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        List<int> triangles = new List<int>();
        CalculateIcosahedron(ref triangles, new int[6] { 0, 1, 2, 3, 4, 5}, new int[6] { 6, 7, 8, 9, 10, 11});
        CalculateIcosahedron(ref triangles, new int[6] { 6, 7, 8, 9, 10, 11}, new int[6] { 0, 1, 2, 3, 4, 5});
        int index = 0;
        for (int i = 0; i < triangles.Count; i += 3)
        {
            index ++;
            CreateTriangleMesh(new Vector3[3]
            {
                vertices[triangles[i]],
                vertices[triangles[i + 1]],
                vertices[triangles[i + 2]],
            }, index);
        }
    }

    private void CalculateIcosahedron(ref List<int> triangles, int[] pentagon01, int[] pentagon02)
    {
        for (int i = 1; i < 6; i++)
        {
            triangles.Add(pentagon01[0]);
            triangles.Add(pentagon01[i % 5 + 1]);
            triangles.Add(pentagon01[i]);
        }
        for(int i = 0; i < 5; i++)
        {
            triangles.Add(pentagon01[6 - i == 6 ? 1 : 6 - i]);
            triangles.Add(pentagon02[(i + 2) % 5] + 1);
            triangles.Add(pentagon02[(i + 3) % 5] + 1);
        }
    }
    
    private void CreateTriangleMesh(Vector3[] vertices, int index)
    {
        Mesh mesh = new Mesh() { name = string.Format("Mesh_{0}", index) };
        mesh.SetVertices(GenerateVertices(vertices));
        mesh.SetTriangles(GenerateTriangles(), 0);
        mesh.SetNormals(mesh.vertices);
        
        GameObject go = new GameObject("Region_" + index);
        go.transform.SetParent(transform);
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = planetMat;
    }

    private List<Vector3> GenerateVertices(Vector3[] input)
    {
        List<Vector3> output = new List<Vector3>();
        Vector3 oneToZeroStep = (input[1] - input[0]) / iterations;
        Vector3 twoToZeroStep = (input[2] - input[0]) / iterations;
        for (int verticesLayer = 0; verticesLayer <= iterations; verticesLayer++)
        {
            if (verticesLayer == 0) output.Add(input[0] * planetRadius);
            else
            {
                Vector3 startPoint = input[0] + oneToZeroStep * verticesLayer;
                Vector3 startToEndStep = twoToZeroStep - oneToZeroStep;
                // start ===> end 进行插值
                for (int i = 0; i < verticesLayer + 1; i++)
                {
                    output.Add((startPoint + startToEndStep * i).normalized * planetRadius);
                } 
            }
        }
        return output;
    }

    private List<int> GenerateTriangles()
    {
        List<int> output = new List<int>();
        int startIndex = 0;
        for (int trianglesLayer = 0; trianglesLayer < iterations; trianglesLayer++)
        {
            startIndex += trianglesLayer;
            for (int verticesIndex = 0; verticesIndex < trianglesLayer + 1; verticesIndex++)
            {
                int triangleStartIndex = startIndex + verticesIndex;
                int offset = trianglesLayer + 1;
                if (verticesIndex != 0)
                {
                    output.Add(triangleStartIndex);
                    output.Add(triangleStartIndex - 1);
                    output.Add(triangleStartIndex + offset);
                }
                output.Add(triangleStartIndex);
                output.Add(triangleStartIndex + offset);
                output.Add(triangleStartIndex + offset + 1);
            }
        }
        return output;
    }
}
