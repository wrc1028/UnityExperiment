using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereMesh : MonoBehaviour
{
    private const float goldenRatio = 0.6180339887f;
    private Vector3[] redVertexes;
    private Vector3[] greenVertexes;
    private Vector3[] blueVertexes;

    private void InitOriginVertexes()
    {
        redVertexes = new Vector3[4]
        {
            new Vector3(0, 1, goldenRatio),
            new Vector3(0, -1, goldenRatio),
            new Vector3(0, -1, -goldenRatio),
            new Vector3(0, 1, -goldenRatio),
        };
        greenVertexes = new Vector3[4]
        {
            new Vector3(goldenRatio, 0, 1),
            new Vector3(goldenRatio, 0, -1),
            new Vector3(-goldenRatio, 0, -1),
            new Vector3(-goldenRatio, 0, 1),
        };
        blueVertexes = new Vector3[4]
        {
            new Vector3(1, goldenRatio, 0),
            new Vector3(-1, goldenRatio, 0),
            new Vector3(-1, -goldenRatio, 0),
            new Vector3(1, -goldenRatio, 0),
        };
    }

    private void OnDrawGizmos() 
    {
        if (redVertexes.Length == 0 || greenVertexes.Length == 0 || blueVertexes.Length == 0) InitOriginVertexes();
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawSphere(redVertexes[i], 0.05f);
            Gizmos.DrawSphere(greenVertexes[i], 0.05f);
            Gizmos.DrawSphere(blueVertexes[i], 0.05f);
        }
    }
}
