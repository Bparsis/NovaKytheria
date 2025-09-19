using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SimpleChunk : MonoBehaviour
{
    [Header("Dimensions")]
    public int width = 16;        // nombre de cellules en X
    public int depth = 16;        // nombre de cellules en Z
    public float blockSize = 1f;  // taille d'une cellule

    [Header("Material (optional)")]
    public Material material;

    void Start()
    {
        GenerateChunk();
    }

    void GenerateChunk()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        var mc = GetComponent<MeshCollider>();

        if (material != null) mr.material = material;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int vertCount = 0;

        // On crée uniquement la face "top" de chaque bloc (surface plate).
        // Pour tester le déplacement c'est largement suffisant et rapide.
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // position de base de la cellule (coin bas gauche)
                Vector3 basePos = new Vector3(x * blockSize, 0f, z * blockSize);

                // sommets de la face supérieure (y = blockSize)
                Vector3 v0 = basePos + new Vector3(0f, blockSize, 0f);             // (0,1,0)
                Vector3 v1 = basePos + new Vector3(blockSize, blockSize, 0f);     // (1,1,0)
                Vector3 v2 = basePos + new Vector3(blockSize, blockSize, blockSize); // (1,1,1)
                Vector3 v3 = basePos + new Vector3(0f, blockSize, blockSize);     // (0,1,1)

                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                // Deux triangles (ordre pour normal vers le haut)
                triangles.Add(vertCount + 0);
                triangles.Add(vertCount + 2);
                triangles.Add(vertCount + 1);

                triangles.Add(vertCount + 0);
                triangles.Add(vertCount + 3);
                triangles.Add(vertCount + 2);

                // UVs simples (pour pouvoir texturer si besoin)
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                vertCount += 4;
            }
        }
        
        foreach (var v in uvs)
        {
            Debug.DrawLine(new Vector3(v.x, 0, v.y), new Vector3(v.x, 10, v.y), Color.red, 100f);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // sûr pour grands meshes
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Assigner au MeshFilter et au MeshCollider
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }
}
