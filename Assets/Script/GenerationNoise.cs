using System;
using Unity.Mathematics;
using UnityEngine;

public class GenerationNoise
{
    readonly private TiledFastNoiseLite continentalNoise;
    readonly private float continentFrequency = 0.3f;
    readonly private TiledFastNoiseLite mountainNoise;
    readonly private float mountainFrequency = 1f;
    readonly private TiledFastNoiseLite oceanNoise;
    readonly private float oceanFrequency = 0.7f;
    readonly private TiledFastNoiseLite riverNoise;
    readonly private float riverFrequency = 0.9f;
    readonly private TiledFastNoiseLite caveNoise;
    readonly private float caveFrequency = 0.3f;

    public GenerationNoise(int seed)

    {
        // // Continentalité : très basse fréquence (grandes masses)
        continentalNoise = new TiledFastNoiseLite(seed, continentFrequency, 250_000_000f, 250_000_000f, 250_000_000f, FastNoiseLite.NoiseType.OpenSimplex2, FastNoiseLite.FractalType.FBm, 0.5f, 6);

        // Montagnes : ridged multi pour pics
        mountainNoise = new TiledFastNoiseLite(seed, mountainFrequency, 250_000_000f, 250_000_000f, 250_000_000f, FastNoiseLite.NoiseType.OpenSimplex2, FastNoiseLite.FractalType.Ridged, 0.5f, 4);

        // Océans : FBM classique pour fonds irréguliers
        oceanNoise = new TiledFastNoiseLite(seed, oceanFrequency, 250_000_000f, 250_000_000f, 250_000_000f, FastNoiseLite.NoiseType.OpenSimplex2, FastNoiseLite.FractalType.FBm, 0.5f, 6);

        // Rivières : ridged pour vallées
        riverNoise = new TiledFastNoiseLite(seed, riverFrequency, 250_000_000f, 250_000_000f, 250_000_000f, FastNoiseLite.NoiseType.OpenSimplex2, FastNoiseLite.FractalType.Ridged, 0.5f, 3);

        caveNoise = new TiledFastNoiseLite(seed, caveFrequency, 250_000_000f, 250_000_000f, 250_000_000f, FastNoiseLite.NoiseType.OpenSimplex2);
    }

    #region Generator
    #region Continent
    /// <summary>
    /// -1 = océan profond, +1 = continent
    /// </summary>
    public float GetContinent(float x, float z)
    {
        float n = continentalNoise.GetTiledNoise(x, z);
        n += 0.14f;// léger biais vers la terre
        n = (n + 1) / 2;
        n = Mathf.SmoothStep(-1.2f, 1f, n);
        return Mathf.Clamp(n, -1f, 1f);
    }
    #endregion Continent
    #region Mountain
    public float GetMountain(float x, float z)
    {
        float mNoise = mountainNoise.GetTiledNoise(x, z);
        float mountainHeight = Mathf.SmoothStep(0, 1, mNoise);
        float mountainMask = GetMountainMask(x, z);
        mountainHeight *= mountainMask;
        return mountainHeight;
    }
    public float GetMountainMask(float x, float z)
    {
        float continental = GetContinent(x, z);
        float mountainMask = Mathf.SmoothStep(0.1f, 0.9f, continental);
        return mountainMask;
    }
    #endregion Mountain
    #region Ocean
    public float GetOcean(float x, float z)
    {
        float oNoise = oceanNoise.GetTiledNoise(x, z);
        float oceanDepth = Mathf.SmoothStep(0f, 1f, oNoise);
        oceanDepth = (oceanDepth * -1f) + 1f;
        float oceanMask = GetOceanMask(x, z);
        oceanDepth *= oceanMask;
        oceanDepth = Mathf.Lerp(-1f, 1f, oceanDepth);
        oceanDepth = Mathf.Clamp01(oceanDepth);
        return oceanDepth;
    }
    public float GetOceanMask(float x, float z)
    {
        float continental = GetContinent(x, z);
        float oceanMask = Mathf.Pow(Mathf.Abs(continental), 1.3f) * Mathf.Sign(continental);
        oceanMask += 0.5f;
        oceanMask = (oceanMask * Mathf.Sign(-1)) + 1f;
        return oceanMask;
    }
    #endregion Ocean
    #region River
    public float GetRiver(float x, float z)
    {
        float riverDepth = riverNoise.GetTiledNoise(x, z);
        float riverMask = GetRiverMask(x, z);
        riverDepth *= riverMask;
        riverDepth = Mathf.Clamp01(riverDepth);
        return riverDepth;
    }
    public float GetRiverMask(float x, float z)
    {
        float riverMask = GetContinent(x, z);
        if (riverMask < -0.2f)
            riverMask = -1;
        else if (-0.2f < riverMask && riverMask < 0.2f)
            riverMask *= 2.5f;
        else
            riverMask = 1;
        return riverMask;
    }
    #endregion River
    #region Cave
    public float GetCave(float x, float y, float z)
    {
        float caveDensity = caveNoise.GetTiledNoise(x, y, z);
        return caveDensity;
    }
    #endregion Cave
    #endregion Generator

    /// <summary>
    /// Génère la hauteur finale du terrain (en mètres)
    /// </summary>
    public float GetHeight(float x, float z)
    {
        float baseHeight = GetContinent(x, z) * 3000;
        float mountainHeight = GetMountain(x, z) * 7000;
        float oceanDepth = GetOcean(x, z) * 7000 * -1;
        float riverDepth = GetRiver(x, z) * 100 * -1;
        float finalHeight = baseHeight + mountainHeight + oceanDepth + riverDepth;
        return Mathf.Clamp(finalHeight, -10000f, 10000f);
    }
}