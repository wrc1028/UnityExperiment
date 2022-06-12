using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SphereMesh : MonoBehaviour
{
    private const float goldenRatio = 0.6180339887f;
    private Vector3[] vertices;
    private List<ushort> triangles;
    private Vector3[] normals;
    private Mesh mesh;
    private Vector3[] redVertices;
    private Vector3[] greenVertices;
    private Vector3[] blueVertices;

    private void InitOriginVertexes()
    {
        redVertices = new Vector3[4]
        {
            new Vector3(0, 1, goldenRatio), // 0
            new Vector3(0, 1, -goldenRatio), // 1
            new Vector3(0, -1, -goldenRatio), // 2
            new Vector3(0, -1, goldenRatio), // 3
        };
        greenVertices = new Vector3[4]
        {
            new Vector3(goldenRatio, 0, 1), // 4
            new Vector3(-goldenRatio, 0, 1), // 5
            new Vector3(-goldenRatio, 0, -1), // 6
            new Vector3(goldenRatio, 0, -1), // 7
        };
        blueVertices = new Vector3[4]
        {
            new Vector3(1, goldenRatio, 0), // 8
            new Vector3(-1, goldenRatio, 0), // 9
            new Vector3(-1, -goldenRatio, 0), // 10
            new Vector3(1, -goldenRatio, 0), // 11
        };
    }
    [Button("生成")]
    private void GenerateMesh()
    {
        vertices = new Vector3[12] 
        {
            // R
            new Vector3(0, 1, goldenRatio), // 0
            new Vector3(0, 1, -goldenRatio), // 1
            new Vector3(0, -1, -goldenRatio), // 2
            new Vector3(0, -1, goldenRatio), // 3
            // G
            new Vector3(goldenRatio, 0, 1), // 4
            new Vector3(-goldenRatio, 0, 1), // 5
            new Vector3(-goldenRatio, 0, -1), // 6
            new Vector3(goldenRatio, 0, -1), // 7
            // B
            new Vector3(1, goldenRatio, 0), // 8
            new Vector3(-1, goldenRatio, 0), // 9
            new Vector3(-1, -goldenRatio, 0), // 10
            new Vector3(1, -goldenRatio, 0), // 11
        };
        triangles = new List<ushort>();
        normals = new Vector3[12];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }
        GenerateTriangles(ref triangles, new ushort[4] { 4, 5, 6, 7, }, new ushort[4]{ 8, 0, 9, 1, });
        GenerateTriangles(ref triangles, new ushort[4] { 7, 6, 5, 4, }, new ushort[4]{ 11, 2, 10, 3, });
        GenerateTriangles(ref triangles, new ushort[4] { 0, 8, 1, 9, });
        GenerateTriangles(ref triangles, new ushort[4] { 2, 11, 3, 10, });
        GenerateTriangles(ref triangles, new ushort[4] { 6, 1, 7, 2, });
        GenerateTriangles(ref triangles, new ushort[4] { 4, 0, 5, 3, });
        GenerateTriangles(ref triangles, new ushort[4] { 8, 4, 11, 7, });
        GenerateTriangles(ref triangles, new ushort[4] { 9, 6, 10, 5, });
        mesh = new Mesh() { name = "Custom Mesh" };
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0);
        mesh.normals = normals;
        mesh.RecalculateNormals();
        // 上半部分三角形
        GetComponent<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// 计算第一种情况的三角形
    /// </summary>
    /// <param name="triangles">返回值</param>
    /// <param name="vertexIndexs">所需三角形下标</param>
    private void GenerateTriangles(ref List<ushort> triangles, ushort[] baseVertexIndexs, ushort[] refVertexIndexs)
    {
        for (int i = 0; i < 4; i++)
        {
            triangles.Add(baseVertexIndexs[i]);
            triangles.Add(refVertexIndexs[i]);
            triangles.Add(refVertexIndexs[i + 1 == 4 ? 0 : i + 1]);
        }
    }

    private void GenerateTriangles(ref List<ushort> triangles, ushort[] vertexIndexs)
    {
        triangles.Add(vertexIndexs[0]);
        triangles.Add(vertexIndexs[1]);
        triangles.Add(vertexIndexs[2]);

        triangles.Add(vertexIndexs[0]);
        triangles.Add(vertexIndexs[2]);
        triangles.Add(vertexIndexs[3]);
    }

    private void OnDrawGizmos() 
    {
        InitOriginVertexes();
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawIcon(redVertices[i], i.ToString(), true, Color.red / (i + 1) / (i + 1));
            Gizmos.DrawIcon(greenVertices[i], i.ToString(), true, Color.green / (i + 1) / (i + 1));
            Gizmos.DrawIcon(blueVertices[i], i.ToString(), true, Color.blue / (i + 1) / (i + 1));
        }
    }
}
