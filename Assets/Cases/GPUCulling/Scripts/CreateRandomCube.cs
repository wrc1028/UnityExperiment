using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CreateRandomCube : MonoBehaviour
{
    public Mesh cubeMesh;
    public Material cubeMat;
    public Vector3Int boundsMin;
    public Vector3Int boundsMax;

    [Button("创建")]
    public void CreateCube()
    {
        Vector3 bounds = boundsMax - boundsMin;
        float width = bounds.x;
        float height = bounds.y;
        float depth = bounds.z;

        for (int x = boundsMin.x; x < boundsMax.x; x += 8)
            for (int y = boundsMin.y; y < boundsMax.y; y += 8)
                for (int z = boundsMin.z; z < boundsMax.z; z += 8)
                {
                    GameObject newCube = new GameObject(string.Format("Cube({0}, {1}, {2})", x, y, z));
                    newCube.transform.parent = this.transform;
                    newCube.transform.localPosition = new Vector3(x + Random.Range(-2, 2), y + Random.Range(-2, 2), z + Random.Range(-2, 2));
                    // newCube.transform.localScale = new Vector3(Random.Range(0.5f, 1), Random.Range(0.5f, 1), Random.Range(0.5f, 1));
                    newCube.AddComponent<MeshFilter>();
                    newCube.GetComponent<MeshFilter>().mesh = cubeMesh;
                    newCube.AddComponent<MeshRenderer>();
                    newCube.GetComponent<MeshRenderer>().material = cubeMat;
                }
    }
}
