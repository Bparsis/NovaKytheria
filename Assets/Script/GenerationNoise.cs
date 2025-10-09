using UnityEngine;

public class GenerationNoise
{
    readonly private FastNoiseLite continentalNoise;
    readonly private float continentFrequency = 1e-9f;
    readonly private FastNoiseLite mountainNoise;
    readonly private float mountainFrequency = 2e-9f;
    readonly private FastNoiseLite oceanNoise;
    readonly private float oceanFrequency = 3e-9f;
    readonly private FastNoiseLite riverNoise;
    readonly private float riverFrequency = 1e-8f;

    public GenerationNoise(int seed)

    {
        // Continentalité : très basse fréquence (grandes masses)
        continentalNoise = new FastNoiseLite(seed);
        continentalNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        continentalNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        continentalNoise.SetFractalOctaves(5);
        continentalNoise.SetFractalLacunarity(2.0f);
        continentalNoise.SetFractalGain(0.5f);
        continentalNoise.SetFrequency(continentFrequency);

        // Montagnes : ridged multi pour pics
        mountainNoise = new FastNoiseLite(seed);
        mountainNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        mountainNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        mountainNoise.SetFractalOctaves(6);
        mountainNoise.SetFrequency(mountainFrequency);

        // Océans : FBM classique pour fonds irréguliers
        oceanNoise = new FastNoiseLite(seed);
        oceanNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        oceanNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        oceanNoise.SetFractalOctaves(4);
        oceanNoise.SetFractalGain(0.5f);
        oceanNoise.SetFrequency(oceanFrequency);

        // Rivières : ridged pour vallées
        riverNoise = new FastNoiseLite(seed + 42);
        riverNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        riverNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        riverNoise.SetFrequency(1e-8f);
    }

    /// <summary>
    /// -1 = océan profond, +1 = continent
    /// </summary>
    public float GetContinent(float x, float z)
    {
        float n = continentalNoise.GetNoise(x, z);
        n = Mathf.Pow(Mathf.Abs(n), 1.3f) * Mathf.Sign(n);
        n += 0.1f; // léger biais vers la terre
        return Mathf.Clamp(n, -1f, 1f);
    }
    public float GetMountain(float x, float z)
    {
        float n = mountainNoise.GetNoise(x, z) * 2.5f;
        n = Mathf.Pow(Mathf.Abs(n), 1.3f) * Mathf.Sign(n);
        return Mathf.Clamp(n, -1f, 1f);
    }
    public float GetOcean(float x, float z)
    {
        float n = oceanNoise.GetNoise(x, z) * 2.5f;
        n = Mathf.Pow(Mathf.Abs(n), 1.3f) * Mathf.Sign(n);
        return Mathf.Clamp(n, -1f, 1f);
    }
    public float GetRiver(float x, float z)
    {
        float n = riverNoise.GetNoise(x, z);
        return n;
    }

    /// <summary>
    /// Génère la hauteur finale du terrain (en mètres)
    /// </summary>
    public float GetHeight(float x, float z)
    {
        float continental = GetContinent(x, z);
        // Debug.Log($"Continentalness at ({x}, {z}) = {continental}  --  GenerationNoise.cs L54");
        // Masques pour savoir où appliquer montagnes / fosses
        float mountainMask = Mathf.SmoothStep(0.45f, 0.75f, continental);
        float oceanMask = 1f + Mathf.SmoothStep(-0.2f, -0.1f, continental);
        // Debug.Log($"MountainMask at ({x}, {z}) = {mountainMask}, OceanMask = {oceanMask}  --  GenerationNoise.cs L58");
        // Base (océan profond → haut plateau)
        float baseHeight = Mathf.Lerp(-3000f, +3000f, (continental + 1f) / 2f);
        // Debug.Log($"BaseHeight at ({x}, {z}) = {baseHeight}m  --  GenerationNoise.cs L61");
        // Reliefs
        float mNoise = mountainNoise.GetNoise(x, z) * 2.5f;
        float oNoise = oceanNoise.GetNoise(x, z) * 2.5f;
        // Debug.Log($"MountainNoise at ({x}, {z}) = {mNoise}, OceanNoise = {oNoise}  --  GenerationNoise.cs L65");
        // Montagnes (pics)
        float mountainHeight = Mathf.Pow(Mathf.Abs(mNoise), 1.8f) * 4500f * mountainMask;
        // Fosses
        float oceanDepth = -Mathf.Pow(Mathf.Abs(oNoise), 1.5f) * 4000f * oceanMask;
        // Debug.Log($"MountainHeight at ({x}, {z}) = {mountainHeight}m, OceanDepth = {oceanDepth}m  --  GenerationNoise.cs L70");
        float finalHeight = baseHeight + mountainHeight + oceanDepth;
        // Debug.Log($"FinalHeight at ({x}, {z}) = {finalHeight}m  --  GenerationNoise.cs L72");
        // return Mathf.Clamp(baseHeight, -10000f, 9000f);
        return Mathf.Clamp(finalHeight, -10000f, 9000f);
    }
}