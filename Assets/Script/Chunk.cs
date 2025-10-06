using System.Collections;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    [Header("Chunk settings")]
    public int chunkSize = 160;
    public float voxelSize = 0.2f;
    public bool parallelVoxelGet = false;
    public Vector3Int chunkCoords = Vector3Int.zero; // position en chunks dans le monde
    public int LOD = 0; // niveau de détail, si tu veux gérer ça
    public int step = 1;
    public int effectiveChunkSize = 0; // chunkSize / step

    [Header("Visual")]
    public Material material;

    // voxel data: 1 = solid, 0 = empty
    // ici simple demo : remplissage plein au debut, mais tu peux remplir via Perlin/etc.
    private NativeArray<byte> voxels; // length = sizeX*sizeY*sizeZ
    private JobHandle handle;
    private MeshFilter mf;
    private MeshCollider mc;
    private MeshRenderer mr;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
        mr = GetComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;
    }

    void Start()
    {
        step = (int)Mathf.Pow(2, LOD); // si tu veux gérer le LOD
        effectiveChunkSize = chunkSize / step;
        int sizeWithBorder = effectiveChunkSize + 2;
        int totalVoxels = sizeWithBorder * sizeWithBorder * sizeWithBorder;

        voxels = new NativeArray<byte>(totalVoxels, Allocator.Persistent);
        // Schedule avec batchSize (par ex. 64)
        if (parallelVoxelGet)
        {
            var job = new GetVoxelDataParallelJob
            {
                chunkSize = effectiveChunkSize,
                chunkCoords = chunkCoords,
                voxels = voxels
            };
            handle = job.Schedule(totalVoxels, sizeWithBorder);
        }
        else
        {
            var job = new GetVoxelDataJob
            {
                chunkSize = effectiveChunkSize,
                step = step,
                chunkCoords = chunkCoords,
                voxels = voxels
            };
            handle = job.Schedule();
        }
        // while (!handle.IsCompleted) yield return null;

        // voxels = new NativeArray<byte>(new byte[] {
        //                 0,0,0,0,
        //                 0,0,0,0,
        //                 0,0,0,0,
        //                 0,0,0,0,

        //                 0,0,0,0,
        //                 0,1,1,0,
        //                 0,0,0,0,
        //                 0,0,0,0,

        //                 0,0,0,0,
        //                 0,1,1,0,
        //                 0,0,0,0,
        //                 0,0,0,0,

        //                 0,0,0,0,
        //                 0,0,0,0,
        //                 0,0,0,0,
        //                 0,0,0,0,
        //             }, Allocator.Persistent);
        // Lance job de génération de mesh
        StartCoroutine(GenerateMeshRoutine());
    }

    void OnDestroy()
    {
        if (voxels.IsCreated) voxels.Dispose();
    }

    IEnumerator GenerateMeshRoutine()
    {


        var vertices = new NativeList<Vector3>(Allocator.Persistent);
        var triangles = new NativeList<int>(Allocator.Persistent);
        var uvs = new NativeList<Vector2>(Allocator.Persistent);
        var normals = new NativeList<Vector3>(Allocator.Persistent);
        var job = new GreedyMesherJob
        {
            chunkSize = effectiveChunkSize,
            voxelSize = voxelSize * step,
            voxels = voxels,

            vertices = vertices,
            triangles = triangles,
            uvs = uvs,
            normals = normals

        };
        handle = job.Schedule(handle);

        while (!handle.IsCompleted) yield return null;
        handle.Complete();

        Vector3[] verts = job.vertices.AsArray().ToArray();
        int[] tris = job.triangles.AsArray().ToArray();
        Vector2[] uvArr = job.uvs.AsArray().ToArray();
        Vector3[] norms = job.normals.AsArray().ToArray();

        // Construire le mesh (doit se faire sur main thread)
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvArr;
        mesh.normals = norms;
        mesh.RecalculateBounds(); // (normales déjà fournies, mais ok)

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;

        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
        normals.Dispose();
        voxels.Dispose();
        yield break;
    }
}