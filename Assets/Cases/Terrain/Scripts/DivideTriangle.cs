using System.Collections;
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
    private List<ushort> trianglesResult;
    private Vector3[] normalsResult;
    
    private void OnDrawGizmos() 
    {
        verticesOrigin = new Vector3[3] { new Vector3(0, sqrt3, 0), new Vector3(-1, 0, 0), new Vector3(1, 0, 0) };
        // for (int i = 0; i < verticesOrigin.Length; i++)
        // {
        //     Gizmos.DrawIcon(verticesOrigin[i], "", true, Color.white / (i + 1));
        // }
        if (verticesResult == null) return;
        for (int i = 0; i < verticesResult.Count; i++)
        {
            Gizmos.DrawIcon(verticesResult[i], "", true, Color.white / (i + 1));
        }
    }

    [Button("创建")]
    public void CreateMesh()
    {
        verticesOrigin = new Vector3[3] { new Vector3(0, sqrt3, 0), new Vector3(-1, 0, 0), new Vector3(1, 0, 0) };
        Divide(verticesOrigin, ref verticesResult);
        
    }

    private void Divide(Vector3[] input, ref List<Vector3> output)
    {
        output = new List<Vector3>();
        Vector3 oneToZeroStep = (input[1] - input[0]) / iterations;
        Vector3 twoToZeroStep = (input[2] - input[0]) / iterations;
        for (int layer = 0; layer <= iterations; layer++)
        {
            if (layer == 0) output.Add(input[0]);
            else
            {
                Vector3 startPoint = input[0] + oneToZeroStep * layer;
                Vector3 startToEndStep = twoToZeroStep - oneToZeroStep;
                // start ===> end 进行插值
                for (int i = 0; i < layer + 1; i++)
                {
                    output.Add(startPoint + startToEndStep * i);
                } 
            }
        }
    }
}
