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
        GenerateWorld();
    }

    void GenerateWorld()
    {
        for (int x = -renderDistance; x < renderDistance + 1; x++)
        {
            for (int z = -renderDistance; z < renderDistance + 1; z++)
            {   
                Vector3Int coord = new Vector3Int(x, 0, z);
                float distance = coord.magnitude;
                if (distance > renderDistance) continue; // Ignore les chunks hors de la distance de rendu
                Vector3 pos = new Vector3(x * chunkSize * blockSize, 0, z * chunkSize * blockSize);
                GameObject newChunk = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
                newChunk.name = $"Chunk_{x}_{z}";
                // Si ton script Chunk a besoin de savoir sa taille ou sa position :
                Chunk chunk = newChunk.GetComponent<Chunk>();
                if (chunk != null)
                {
                    chunk.chunkSize = chunkSize; // si tu as ce champ
                }
            }
        }
    }
}
