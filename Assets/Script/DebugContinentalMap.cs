using UnityEngine;

public class DebugContinentalMap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int size = 1000;
        Texture2D tex = new Texture2D(size, size);
        ContinentalNoise cont = new ContinentalNoise(12345);

        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
            {
                float val = cont.GetContinentalness(x * 10000, z * 10000);
                Color c = Color.Lerp(Color.blue, Color.green, (val + 1f) / 2f);
                tex.SetPixel(x, z, c);
            }
        tex.Apply();

        // Crée un sprite pour debug
        GetComponent<MeshRenderer>().material.mainTexture = tex;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
