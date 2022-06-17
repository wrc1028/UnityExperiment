using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public class DivideTriangle : MonoBehaviour
{
    [Range(1, 12)]
    public int iterations = 1;
    private const float sqrt3 = 1.7320508f;
    private Vector3[] verticesOrigin;
    private List<Vector3> verticesResult;
    private List<int> trianglesResult;
    private Vector3[] normalsResult;
    private Mesh mesh;
    
    private void OnDrawGizmos() 
    {
        // verticesOrigin = new Vector3[3] { new Vector3(0, sqrt3, 0), new Vector3(-1, 0, 0), new Vector3(1, 0, 0) };
        // for (int i = 0; i < verticesOrigin.Length; i++)
        // {
        //     Gizmos.DrawIcon(verticesOrigin[i], "", true, Color.white / (i + 1));
        // }
        // if (verticesResult == null) return;
        // for (int i = 0; i < verticesResult.Count; i++)
        // {
        //     Gizmos.DrawIcon(verticesResult[i], "", true, Color.white / (i + 1));
        // }
    }

    [Button("创建")]
    public void CreateMesh()
    {
        verticesOrigin = new Vector3[3] { new Vector3(0, sqrt3, 0), new Vector3(-1, 0, 0), new Vector3(1, 0, 0) };
        GenerateVertices(verticesOrigin, ref verticesResult);
        GenerateTriangles(ref trianglesResult);
        GenerateNormals(verticesResult.Count, ref normalsResult);
        mesh = new Mesh() { name = "Triangle" };
        mesh.SetVertices(verticesResult);
        mesh.SetTriangles(trianglesResult, 0);
        mesh.normals = normalsResult;
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void GenerateVertices(Vector3[] input, ref List<Vector3> output)
    {
        output = new List<Vector3>();
        Vector3 oneToZeroStep = (input[1] - input[0]) / iterations;
        Vector3 twoToZeroStep = (input[2] - input[0]) / iterations;
        for (int verticesLayer = 0; verticesLayer <= iterations; verticesLayer++)
        {
            if (verticesLayer == 0) output.Add(input[0]);
            else
            {
                Vector3 startPoint = input[0] + oneToZeroStep * verticesLayer;
                Vector3 startToEndStep = twoToZeroStep - oneToZeroStep;
                // start ===> end 进行插值
                for (int i = 0; i < verticesLayer + 1; i++)
                {
                    output.Add(startPoint + startToEndStep * i);
                } 
            }
        }
    }

    private void GenerateTriangles(ref List<int> output)
    {
        output = new List<int>();
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
    }

    private void GenerateNormals(int verticesCount, ref Vector3[] output)
    {
        output = new Vector3[verticesCount];
        for (int i = 0; i < verticesCount; i++)
        {
            output[i] = Vector3.forward;
        }
    }
}
