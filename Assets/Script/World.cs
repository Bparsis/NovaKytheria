using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    [Header("Chunk Settings")]
    public GameObject chunkPrefab;  // Le prefab du chunk
    public int renderDistance = 20; // Distance de rendu en chunks (optionnel, si tu veux gérer le LOD)
    public int chunkSize = 100;      // Taille d'un chunk (optionnel, si ton script Chunk en a besoin)
    public float blockSize = 0.2f;  // Taille d'une cellule (optionnel, si ton script Chunk en a besoin)
    private void Start()
    {
        // GenerateWorld();
        StartCoroutine(GenerateWorld());
    }

    IEnumerator GenerateWorld()
    {
        var coords = new List<Vector3Int>();
        for (int x = -renderDistance; x < renderDistance + 1; x++)
        {
            for (int z = -renderDistance; z < renderDistance + 1; z++)
            {
                Vector3Int coord = new(x, 0, z);
                if (coord.magnitude < renderDistance) coords.Add(coord); // Ignore les chunks hors de la distance de rendu
            }
        }
        coords.Sort((a, b) => a.magnitude.CompareTo(b.magnitude)); // Tri par distance décroissante

        foreach (var coord in coords)
        {
            Vector3 pos = new(coord.x * chunkSize * blockSize, 0, coord.z * chunkSize * blockSize);
            GameObject newChunk = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
            newChunk.name = $"Chunk_{coord.x}_{coord.z}";
            // Si ton script Chunk a besoin de savoir sa taille ou sa position :
            Chunk chunk = newChunk.GetComponent<Chunk>();
            if (chunk != null)
            {
                chunk.chunkSize = chunkSize; // si tu as ce champ
            }
            yield return null; // Attendre une frame pour ne pas bloquer le thread principal
        }
    }
}
