using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public int chunkSize = 100;
    public float blockSize = 0.2f;


    [Header("Material (optional)")]
    public Material material;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (material != null) meshRenderer.material = material;
        StartCoroutine(GenerateMeshAsync());
    }

    IEnumerator GenerateMeshAsync()
    {
        int vertCount = (chunkSize + 1) * (chunkSize + 1);

        NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertCount, Allocator.TempJob);
        NativeArray<int> triangles = new NativeArray<int>(chunkSize * chunkSize * 6, Allocator.TempJob);

        // Lancer le job
        GeneratePlaneJob job = new GeneratePlaneJob
        {
            chunkSize = chunkSize,
            blockSize = blockSize,
            vertices = vertices,
            triangles = triangles
        };

        JobHandle handle = job.Schedule();

        // Attendre la fin sans bloquer
        yield return new WaitUntil(() => handle.IsCompleted);
        handle.Complete();

        // Convertir les données en Mesh Unity
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        // Libérer la mémoire native
        vertices.Dispose();
        triangles.Dispose();
    }
}

[BurstCompile]
public struct GeneratePlaneJob : IJob
{
    public int chunkSize;
    public float blockSize;

    public NativeArray<Vector3> vertices;
    public NativeArray<int> triangles;

    public void Execute()
    {
        // Génération des vertices
        int v = 0;
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                vertices[v++] = new Vector3(x * blockSize, 0, z * blockSize);
            }
        }

        // Génération des triangles
        int t = 0;
        int vertPerRow = chunkSize + 1;
        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int start = z * vertPerRow + x;

                triangles[t++] = start;
                triangles[t++] = start + vertPerRow + 1;
                triangles[t++] = start + 1;

                triangles[t++] = start;
                triangles[t++] = start + vertPerRow;
                triangles[t++] = start + vertPerRow + 1;
            }
        }
    }
}
