using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class DebugWorldMap : MonoBehaviour
{
    public int size = 1024;
    public float worldScale = 500000f; // distance entre deux pixels (mètres)
    public int seed = 5;

    private Texture2D tex;
    private Material mat;

    void Start()
    {
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        mat = GetComponent<MeshRenderer>().material;
 
        StartCoroutine(Generate()); 

        tex.Apply();
        mat.mainTexture = tex;
    }
    
    IEnumerator Generate()
    {
        GenerationNoise gen = new GenerationNoise(seed);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float h = gen.GetHeight(worldX, worldZ);
                // Debug.Log($"Height at ({worldX}, {worldZ}) = {h}m  --  DebugWorldMap.cs L36");
                Color c = ColorFromHeight(h);
                tex.SetPixel(x, z, c);
            }
        }
        yield return null;
    }

    Color ColorFromHeight(float h)
    {
        // dégradé simple selon altitude
        if (h < -8000) return new Color(0, 0, 0.2f);               // fosses abyssales
        if (h < -4000) return new Color(0, 0.1f, 0.4f);
        if (h < -1000) return new Color(0, 0.2f, 0.7f);
        if (h < 0) return new Color(0.2f, 0.4f, 0.8f);              // mer peu profonde
        if (h < 200) return new Color(0.9f, 0.85f, 0.6f);           // plages
        if (h < 1000) return new Color(0.1f, 0.6f, 0.1f);           // plaine
        if (h < 3000) return new Color(0.3f, 0.4f, 0.2f);           // colline
        if (h < 5000) return new Color(0.5f, 0.45f, 0.35f);         // montagne
        if (h < 7000) return new Color(0.7f, 0.7f, 0.7f);           // haute montagne
        return Color.white;                                         // sommet enneigé
    }
}
