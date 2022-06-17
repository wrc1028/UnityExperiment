using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    private const float k_GoldenRatio = 0.6180339887f;
    public float planetRadius = 10;

    /// <summary>
    /// 基础的12个顶点位置
    /// </summary>
    private Vector3[] baseVertices;

    private void OnValidate() 
    {
        baseVertices = new Vector3[12];
        // 南北极
        baseVertices[0] = new Vector3(0, planetRadius, 0);
        CalculateOtherBaseVertices(0, ref baseVertices);
        baseVertices[6] = new Vector3(0, -planetRadius, 0);
        CalculateOtherBaseVertices(6, ref baseVertices);
    }

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
                float length = planetRadius * 2 * sinAlpha;
                float positionX = length * cosAlpha;
                float positionY = planetRadius - length * sinAlpha;
                vertices[vertexIndex] = new Vector3(positionX, positionY, 0) * sign;
            }
            else
            {
                float lastLength = Mathf.Abs(vertices[poleIndex + 1].x);
                Vector3 lastDir = new Vector3(vertices[vertexIndex - 1].x, 0 , vertices[vertexIndex - 1].z).normalized;
                Vector3 crossDir = Vector3.Cross(lastDir, poleDir).normalized;
                float cos72 = Mathf.Cos(0.4f * Mathf.PI);
                float sin72 = Mathf.Sin(0.4f * Mathf.PI);
                vertices[vertexIndex] = lastLength * cos72 * lastDir + lastLength * sin72 * crossDir + new Vector3(0, vertices[poleIndex + 1].y, 0);
            }
        }
    }
    private void OnDrawGizmos() 
    {
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Gizmos.DrawIcon(baseVertices[i], "1", true, Color.white / (i + 1));
        }    
    }
}
