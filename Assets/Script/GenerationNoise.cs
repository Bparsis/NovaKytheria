using System;
using Unity.Mathematics;
using UnityEngine;

public class GenerationNoise
{
    readonly private FastNoiseLite continentalNoise;
    readonly private float continentFrequency = 1e-8f;
    readonly private FastNoiseLite mountainNoise;
    readonly private float mountainFrequency = 5e-9f;
    readonly private FastNoiseLite oceanNoise;
    readonly private float oceanFrequency = 2e-8f;
    readonly private FastNoiseLite riverNoise;
    readonly private float riverFrequency = 1e-8f;
    readonly private FastNoiseLite caveNoise;
    readonly private float caveFrequency = 2e-6f;

    public GenerationNoise(int seed)

    {
        // Continentalité : très basse fréquence (grandes masses)
        continentalNoise = new FastNoiseLite(seed);
        continentalNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        continentalNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        continentalNoise.SetFractalOctaves(8);
        continentalNoise.SetFractalLacunarity(2.0f);
        continentalNoise.SetFractalGain(0.5f);
        continentalNoise.SetFrequency(continentFrequency);

        // Montagnes : ridged multi pour pics
        mountainNoise = new FastNoiseLite(seed);
        mountainNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        mountainNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        mountainNoise.SetFractalOctaves(4);
        mountainNoise.SetFrequency(mountainFrequency);

        // Océans : FBM classique pour fonds irréguliers
        oceanNoise = new FastNoiseLite(seed);
        oceanNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        oceanNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        oceanNoise.SetFractalOctaves(6);
        oceanNoise.SetFractalGain(0.5f);
        oceanNoise.SetFrequency(oceanFrequency);

        // Rivières : ridged pour vallées
        riverNoise = new FastNoiseLite(seed);
        riverNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        riverNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        riverNoise.SetFrequency(riverFrequency);

        caveNoise = new FastNoiseLite(seed);
        caveNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        caveNoise.SetFrequency(caveFrequency);
    }

    #region Generator
    #region Continent
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
    #endregion Continent
    #region Mountain
    public float GetMountain(float x, float z)
    {
        float mNoise = mountainNoise.GetNoise(x, z);
        float mountainMask = GetMountainMask(x, z);
        float mountainHeight = Mathf.SmoothStep(0, 2, mNoise);
        mountainHeight *= mountainMask;
        mountainHeight = Mathf.Lerp(0f, 1f, mountainHeight);
        return mountainHeight;
    }
    public float GetMountainMask(float x, float z)
    {
        float continental = GetContinent(x, z);
        float mountainMask = Mathf.SmoothStep(0.1f, 1.8f, continental);
        return mountainMask;
    }
    #endregion Mountain
    #region Ocean
    public float GetOcean(float x, float z)
    {
        float oNoise = oceanNoise.GetNoise(x, z);
        float oceanMask = GetOceanMask(x, z);
        float oceanDepth = Mathf.SmoothStep(0f, 1f, oNoise);
        oceanDepth = (oceanDepth * -1f) + 1f;
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
        float riverDepth = riverNoise.GetNoise(x, z);
        riverDepth += 1;
        riverDepth /= 2;
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
        float caveDensity = caveNoise.GetNoise(x, y, z);
        return caveDensity;
    }
    #endregion Cave
    #endregion Generator

    /// <summary>
    /// Génère la hauteur finale du terrain (en mètres)
    /// </summary>
    public float GetHeight(float x, float z)
    {
        float continental = GetContinent(x, z);
        float baseHeight = Mathf.Lerp(-2000f, +2000f, (continental + 1f) / 2f);
        float mountainHeight = GetMountain(x, z) * 8000;
        float oceanDepth = GetOcean(x, z) * 8000 * -1;
        float riverDepth = GetRiver(x, z) * 100000 * -1;
        float finalHeight = baseHeight + mountainHeight + oceanDepth;
        return Mathf.Clamp(finalHeight, -10000f, 10000f);
    }
}