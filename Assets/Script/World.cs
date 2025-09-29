using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class World : MonoBehaviour
{
    [Header("Chunk Settings")]
    public GameObject chunkPrefab;  // Le prefab du chunk
    public int renderDistance = 20; // Distance de rendu en chunks (optionnel, si tu veux gérer le LOD)
    public int simultaneousChunks = 3; // Nombre de chunks à générer par frame pour éviter de bloquer
    public int chunkSize = 100;      // Taille d'un chunk (optionnel, si ton script Chunk en a besoin)
    public float blockSize = 0.2f;  // Taille d'une cellule (optionnel, si ton script Chunk en a besoin)
    private float DebugTime = 0f;
    private void Start()
    {
        // GenerateWorld();
        DebugTime = Time.time;
        StartCoroutine(GenerateWorld());
    }

    IEnumerator GenerateWorld()
    {
        var coords = new List<Vector3Int>();
        for (int x = -renderDistance; x < renderDistance + 1; x++)
            for (int y = -renderDistance; y < renderDistance + 1; y++)
                for (int z = -renderDistance; z < renderDistance + 1; z++)
                {
                    Vector3Int coord = new(x, y, z);
                    if (coord.magnitude < renderDistance) coords.Add(coord); // Ignore les chunks hors de la distance de rendu
                }
        coords.Sort((a, b) => a.magnitude.CompareTo(b.magnitude)); // Tri par distance décroissante

        int chunksThisFrame = 0;
        int generatedChunks = 0;

        foreach (var coord in coords)
        {
            Vector3 pos = new(coord.x * chunkSize * blockSize, coord.y * chunkSize * blockSize, coord.z * chunkSize * blockSize);
            GameObject newChunk = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
            newChunk.name = $"Chunk_{coord.x}_{coord.y}_{coord.z}";
            // Si ton script Chunk a besoin de savoir sa taille ou sa position :
            Chunk chunk = newChunk.GetComponent<Chunk>();
            if (chunk != null)
            {
                chunk.chunkSize = chunkSize; // si tu as ce champ
                chunk.voxelSize = blockSize; // si tu as ce champ
                chunk.chunkCoords = coord; // si tu as ce champ
                chunk.LOD = getLod(coord.magnitude); // si tu as ce champ
            }
            generatedChunks++;
            if (++chunksThisFrame >= simultaneousChunks)
            {
                chunksThisFrame = 0;
                Debug.Log($"Generated {generatedChunks}/{coords.Count} chunks... {generatedChunks * 100 / coords.Count}%");
                yield return null; // Attendre la fin de la frame pour ne pas bloquer
                // yield return new WaitForSeconds(1); // Attendre la fin de la frame pour ne pas bloquer
            }
        }
        Debug.Log($"World generated in {Time.time - DebugTime} seconds.");
    }

    int getLod(float distance)
    {
        if (distance < renderDistance * 0.25f) return 0; // LOD 0 pour les chunks proches
        else if (distance < renderDistance * 0.50f) return 1; // LOD 1 pour les chunks moyens
        else if (distance < renderDistance * 0.75f) return 2; // LOD 2 pour les chunks éloignés
        else return 3; // LOD 3 pour les chunks lointains
    }
}
