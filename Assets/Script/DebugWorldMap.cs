using System.Collections;
using UnityEngine;

// [RequireComponent(typeof(MeshRenderer))]
public class DebugWorldMap : MonoBehaviour
{
    public GameObject DebugWorldMapPlane;
    public int size = 1000;
    public float worldScale = 250_000_000f; // distance entre deux pixels (0.2 mètres)
    public int seed = 987654321;
    private GenerationNoise genMotor;

    void Start()
    {
        genMotor = new GenerationNoise(seed);
        StartCoroutine(ContinentalRenderGen());
        StartCoroutine(MountainRenderGen());
        StartCoroutine(MountainMaskRenderGen());
        StartCoroutine(OceanRenderGen());
        StartCoroutine(OceanMaskRenderGen());
        StartCoroutine(RiverRenderGen());
        StartCoroutine(RiverMaskRenderGen());
        StartCoroutine(FinalRenderGen());
    }
    IEnumerator RiverRenderGen()
    {
        GameObject RiverRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(350, 0, 50), Quaternion.identity, transform);
        RiverRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        RiverRenderPlane.name = "RiverRenderGen";
        Texture2D RiverRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material RiverRenderMat = RiverRenderPlane.GetComponent<Renderer>().material;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de RiverRenderGen en cours... {x * 100 / size}%");
            RiverRenderTex.Apply();
            RiverRenderMat.mainTexture = RiverRenderTex;
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float r = genMotor.GetRiver(worldX, worldZ);
                Color col = new Color((r + 1f) / 2f, (r + 1f) / 2f, (r + 1f) / 2f);
                RiverRenderTex.SetPixel(x, z, col);
            }
        }
        yield return null;
    }

    IEnumerator RiverMaskRenderGen()
    {
        GameObject RiverMaskRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(350, 0, -50), Quaternion.identity, transform);
        RiverMaskRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        RiverMaskRenderPlane.name = "RiverMaskRenderGen";
        Texture2D RiverMaskRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material RiverMaskRenderMat = RiverMaskRenderPlane.GetComponent<Renderer>().material;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de RiverMaskRenderGen en cours... {x * 100 / size}%");
            RiverMaskRenderTex.Apply();
            RiverMaskRenderMat.mainTexture = RiverMaskRenderTex;
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float m = genMotor.GetRiverMask(worldX, worldZ);
                Color col = new Color((m + 1f) / 2f, (m + 1f) / 2f, (m + 1f) / 2f);
                RiverMaskRenderTex.SetPixel(x, z, col);
            }
        }
        yield return null;
    }
    IEnumerator OceanRenderGen()
    {
        GameObject OceanRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(250, 0, 50), Quaternion.identity, transform);
        OceanRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        OceanRenderPlane.name = "OceanRenderGen";
        Texture2D OceanRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material OceanRenderMat = OceanRenderPlane.GetComponent<Renderer>().material;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de OceanRenderGen en cours... {x * 100 / size}%");
            OceanRenderTex.Apply();
            OceanRenderMat.mainTexture = OceanRenderTex;
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float o = genMotor.GetOcean(worldX, worldZ);
                Color col = new Color((o + 1f) / 2f, (o + 1f) / 2f, (o + 1f) / 2f);
                OceanRenderTex.SetPixel(x, z, col);
            }
        }
        yield return null;
    }
    IEnumerator OceanMaskRenderGen()
    {
        GameObject OceanMaskRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(250, 0, -50), Quaternion.identity, transform);
        OceanMaskRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        OceanMaskRenderPlane.name = "OceanMaskRenderGen";
        Texture2D OceanMaskRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material OceanMaskRenderMat = OceanMaskRenderPlane.GetComponent<Renderer>().material;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de OceanMaskRenderGen en cours... {x * 100 / size}%");
            OceanMaskRenderTex.Apply();
            OceanMaskRenderMat.mainTexture = OceanMaskRenderTex;
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float m = genMotor.GetOceanMask(worldX, worldZ);
                Color col = new Color((m + 1f) / 2f, (m + 1f) / 2f, (m + 1f) / 2f);
                OceanMaskRenderTex.SetPixel(x, z, col);
            }
        }
        yield return null;
    }
    IEnumerator MountainMaskRenderGen()
    {
        GameObject MountainMaskRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(150, 0, -50), Quaternion.identity, transform);
        MountainMaskRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        MountainMaskRenderPlane.name = "MountainMaskRenderGen";
        Texture2D MountainMaskRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material MountainMaskRenderMat = MountainMaskRenderPlane.GetComponent<Renderer>().material;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de MountainMaskRenderGen en cours... {x * 100 / size}%");
            MountainMaskRenderTex.Apply();
            MountainMaskRenderMat.mainTexture = MountainMaskRenderTex;
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float m = genMotor.GetMountainMask(worldX, worldZ);
                Color col = new Color((m + 1f) / 2f, (m + 1f) / 2f, (m + 1f) / 2f);
                MountainMaskRenderTex.SetPixel(x, z, col);
            }
        }
        yield return null;
    }
    IEnumerator MountainRenderGen()
    {
        GameObject MountainRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(150, 0, 50), Quaternion.identity, transform);
        MountainRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        MountainRenderPlane.name = "MountainRenderGen";
        Texture2D MountainRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material MountainRenderMat = MountainRenderPlane.GetComponent<Renderer>().material;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de MountainRenderGen en cours... {x * 100 / size}%");
            MountainRenderTex.Apply();
            MountainRenderMat.mainTexture = MountainRenderTex;
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float m = genMotor.GetMountain(worldX, worldZ);
                Color col = new Color((m + 1f) / 2f, (m + 1f) / 2f, (m + 1f) / 2f);
                MountainRenderTex.SetPixel(x, z, col);
            }
        }
        yield return null;
    }
    IEnumerator ContinentalRenderGen()
    {
        GameObject ContinentalRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(-50, 0, 150), Quaternion.identity, transform);
        ContinentalRenderPlane.transform.localScale = new Vector3(10, 1, 10);
        ContinentalRenderPlane.name = "ContinentalRenderGen";
        Texture2D ContinentalRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material ContinentalRenderMat = ContinentalRenderPlane.GetComponent<Renderer>().material;
        float nbsup = 0;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            // Debug.Log($"Génération de ContinentalRenderGen en cours... {x * 100 / size}%");
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float c = genMotor.GetContinent(worldX, worldZ);
                nbsup += c < 0 ? -1 : 1;
                Color col = new Color((c + 1f) / 2f, (c + 1f) / 2f, (c + 1f) / 2f);
                ContinentalRenderTex.SetPixel(x, z, col);
            }
            ContinentalRenderTex.Apply();
            ContinentalRenderMat.mainTexture = ContinentalRenderTex;
        }
        Debug.Log("Continental render gen nbsup"+nbsup);
        yield return null;
    }
    IEnumerator FinalRenderGen()
    {
        GameObject FinalRenderPlane = Instantiate(DebugWorldMapPlane, new Vector3(0, 0, 0), Quaternion.identity, transform);
        FinalRenderPlane.transform.localScale = new Vector3(20, 1, 20);
        FinalRenderPlane.name = "FinalRenderGen";
        Texture2D FinalRenderTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Material FinalRenderMat = FinalRenderPlane.GetComponent<Renderer>().material;
        float min = 0, max = 0;
        for (int x = 0; x < size; x++)
        {
            yield return null; // laisse une frame pour l'instanciation
            Debug.Log($"Génération de FinalRenderGen en cours... {x * 100 / size}%");
            for (int z = 0; z < size; z++)
            {
                float worldX = x * worldScale;
                float worldZ = z * worldScale;

                float h = genMotor.GetHeight(worldX, worldZ);
                min = h < min ? h : min;
                max = h > max ? h : max;
                Color c = ColorFromHeight(h);
                FinalRenderTex.SetPixel(x, z, c);
            }
            FinalRenderTex.Apply();
            FinalRenderMat.mainTexture = FinalRenderTex;
        }
        Debug.Log("Continental render gen min :"+min+"max :"+max);

        static Color ColorFromHeight(float h)
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
        yield return null;
    }

}
