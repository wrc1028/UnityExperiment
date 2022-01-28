using UnityEngine;
using Sirenix.OdinInspector;
namespace Custom.Terrain
{
    [ExecuteInEditMode]
    public class MeshGenerator : MonoBehaviour
    {
        public Texture2D perlinNoiseTex;
        public ComputeShader generateMesh;
        public float xLength = 20f;
        // Index buffer can either be 16 bit (supports up to 65535 vertices in a mesh), or 32 bit (supports up to 4 billion vertices). 
        // Default index format is 16 bit, since that takes less memory and bandwidth.
        [Range(0, 255)]
        public int xSubdivision = 40;
        public float yLength = 20f;
        [Range(0, 255)]
        public int ySubdivision = 40;

        private Vector3[] vertexes;  
        private Vector3[] normals;
        private Vector2[] uv;
        private int[] triangles;

        [Button("Update")]
        public void UpdateMesh()
        {
            vertexes = GenerateVertexes(new Vector2(xLength, yLength), new Vector2Int(xSubdivision, ySubdivision));
            triangles = GenerateTriangles(new Vector2Int(xSubdivision, ySubdivision));
            uv = GenerateUV(new Vector2Int(xSubdivision, ySubdivision));
            normals = GenerateNormals(new Vector2Int(xSubdivision, ySubdivision));
            // 重新计算Vertex、Normal
            CalculateVertexes(ref vertexes, perlinNoiseTex, 1);
            CalculateNormals(ref normals, perlinNoiseTex, 1);
            Mesh mesh = new Mesh();
            mesh.vertices = vertexes;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateNormals();
            mesh.uv = uv;
            GetComponent<MeshFilter>().mesh = mesh;
        }

        private Vector3[] GenerateVertexes(Vector2 size, Vector2Int subdivision)
        {
            Vector3[] vertexes = new Vector3[(subdivision.x + 1) * (subdivision.y + 1)];
            float xCellSize = size.x / (float)subdivision.x;
            float zCellSize = size.y / (float)subdivision.y;
            for (int z = 0; z < subdivision.y + 1; z++)
            {
                for (int x = 0; x < subdivision.x + 1; x++)
                {
                    int index = x + z * (subdivision.x + 1);
                    vertexes[index] = new Vector3(x * xCellSize, 0, z * zCellSize);
                }
            }
            return vertexes;
        }
        private RenderTexture GetRenderTexture(int width, int height, RenderTextureFormat format)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, format);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }
        private void CalculateVertexes(ref Vector3[] flatVertexes, Texture2D perlinNoise, float heightMultiplier)
        {
            RenderTexture noiseRT = GetRenderTexture(perlinNoise.width, perlinNoise.height, RenderTextureFormat.Default);
            Graphics.Blit(perlinNoise, noiseRT);

            ComputeBuffer buffer = new ComputeBuffer(flatVertexes.Length, sizeof(float) * 3);
            buffer.SetData(flatVertexes);
            generateMesh.SetBuffer(0, "Vertexes", buffer);
            generateMesh.SetInts("_Subdivision", xSubdivision, ySubdivision);
            generateMesh.SetTexture(0, "_TerrainNoiseTexture", noiseRT);
            generateMesh.Dispatch(0, Mathf.CeilToInt(flatVertexes.Length / 1024 + 0.0001f), 1, 1);
            buffer.GetData(flatVertexes);
            buffer.Dispose();
            buffer.Release();
        }
        private void CalculateNormals(ref Vector3[] normals, Texture2D perlinNoise, float heightMultiplier)
        {
            RenderTexture noiseRT = GetRenderTexture(perlinNoise.width, perlinNoise.height, RenderTextureFormat.Default);
            Graphics.Blit(perlinNoise, noiseRT);

            ComputeBuffer buffer = new ComputeBuffer(normals.Length, sizeof(float) * 3);
            buffer.SetData(normals);
            generateMesh.SetBuffer(1, "Normals", buffer);
            generateMesh.SetInts("_Subdivision", xSubdivision, ySubdivision);
            generateMesh.SetTexture(1, "_TerrainNoiseTexture", noiseRT);
            generateMesh.Dispatch(1, Mathf.CeilToInt(normals.Length / 1024 + 0.0001f), 1, 1);
            buffer.GetData(normals);
            buffer.Dispose();
            buffer.Release();
        }

        private Vector2[] GenerateUV(Vector2Int subdivision)
        {
            Vector2[] uv = new Vector2[(subdivision.x + 1) * (subdivision.y + 1)];
            float xCellSize = 1.0f / subdivision.x;
            float zCellSize = 1.0f / subdivision.y;
            for (int v = 0; v < subdivision.y + 1; v++)
            {
                for (int u = 0; u < subdivision.x + 1; u++)
                {
                    int index = u + v * (subdivision.x + 1);
                    uv[index] = new Vector2(u * xCellSize, v * zCellSize);
                }
            }
            return uv;
        }

        private int[] GenerateTriangles(Vector2Int subdivision)
        {
            int[] triangles = new int[subdivision.x * subdivision.y * 6];
            int index = 0;
            for (int z = 0; z < subdivision.y; z++)
            {
                for (int x = 0; x < subdivision.x; x++)
                {
                    // vertex index of quad(clockwise)
                    int vertexIndex01 = x + z * (subdivision.x + 1);
                    int vertexIndex02 = x + (z + 1) * (subdivision.x + 1);
                    int vertexIndex03 = (x + 1) + (z + 1) *(subdivision.x + 1);
                    int vertexIndex04 = (x + 1) + z * (subdivision.x + 1);

                    triangles[index] = vertexIndex01;
                    triangles[index + 1] = vertexIndex02;
                    triangles[index + 2] = vertexIndex03;

                    triangles[index + 3] = vertexIndex01;
                    triangles[index + 4] = vertexIndex03;
                    triangles[index + 5] = vertexIndex04;
                    index += 6;
                }
            }
            return triangles;
        }
        private Vector3[] GenerateNormals(Vector2Int subdivision)
        {
            Vector3[] normals = new Vector3[(subdivision.x + 1) * (subdivision.y + 1)];
            for (int v = 0; v < subdivision.y + 1; v++)
            {
                for (int u = 0; u < subdivision.x + 1; u++)
                {
                    int index = u + v * (subdivision.x + 1);
                    normals[index] = Vector3.up;
                }
            }
            return normals;
        }

        private void OnDrawGizmos()
        {
            // for (int i = 0; i < vertexes.Length; i++)
            // {
            //     // Gizmos.DrawSphere(vertexes[i], 0.1f);
            //     Gizmos.DrawIcon(vertexes[i], i.ToString(), false);
            // }
        }
    }
}