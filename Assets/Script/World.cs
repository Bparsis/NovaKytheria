using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class World : MonoBehaviour
{
    [Header("Chunk Settings")]
    public GameObject chunkPrefab;  // Le prefab du chunk
    public int renderDistance = 12; // Distance de rendu en chunks (optionnel, si tu veux gérer le LOD)
    public int simultaneousChunks = 1; // Nombre de chunks à générer par frame pour éviter de bloquer
    public int chunkSize = 160;      // Taille d'un chunk (optionnel, si ton script Chunk en a besoin)
    public float blockSize = 0.2f;  // Taille d'une cellule (optionnel, si ton script Chunk en a besoin)
    [Range(0, 4)]
    public int nbrOfLODLevels = 3; // Nombre de niveaux de LOD que tu veux (0, 1, 2, 3, 4)
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
                chunk.LOD = GetLOD(coord.magnitude); // si tu as ce champ
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

    int GetLOD(float distance)
    {
        switch (nbrOfLODLevels)
        {
            case 0:
                return 0; // Toujours LOD 0
            case 1:
                if (distance < renderDistance * 0.5) return 0; // LOD 0 pour les chunks proches
                else return 1; // LOD 1 pour les chunks éloignés
            case 2:
                if (distance < renderDistance * 0.33) return 0; // LOD 0 pour les chunks proches
                else if (distance < renderDistance * 0.66) return 1; // LOD 1 pour les chunks moyens
                else return 2; // LOD 2 pour les chunks éloignés
            case 3:
                if (distance < renderDistance * 0.25) return 0; // LOD 0 pour les chunks proches
                else if (distance < renderDistance * 0.50) return 1; // LOD 1 pour les chunks moyens
                else if (distance < renderDistance * 0.75) return 2; // LOD 2 pour les chunks éloignés
                else return 3; // LOD 3 pour les chunks lointains
            case 4:
                if (distance < renderDistance * 0.20) return 0; // LOD 0 pour les chunks proches
                else if (distance < renderDistance * 0.40) return 1; // LOD 1 pour les chunks moyens
                else if (distance < renderDistance * 0.60) return 2; // LOD 2 pour les chunks éloignés
                else if (distance < renderDistance * 0.80) return 3; // LOD 3 pour les chunks lointains
                else return 4; // LOD 4 pour les chunks distants
            default:
                return 0; // Toujours LOD 0
        }
    }
}
