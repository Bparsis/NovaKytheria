using UnityEngine;

public class ContinentalNoise
{
    private FastNoiseLite noise;

    public ContinentalNoise(int seed, float frequency = 2e-7f)
    {
        noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(20);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
        noise.SetFrequency(frequency);
    }

    // Retourne un continent mask -1 (océan) à +1 (continent)
    public float GetContinentalness(float x, float z)
    {
        float n = noise.GetNoise(x, z);
        n = Mathf.Pow(Mathf.Abs(n), 1.3f) * Mathf.Sign(n); // accentuer reliefs
        // n -= 0.1f; // bias océan
        n += 0.2f; // bias océan
        return Mathf.Clamp(n, -1f, 1f);
    }
}
