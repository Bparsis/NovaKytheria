using System.Linq.Expressions;
using UnityEngine;

/// <summary>
/// Générateur de bruit spatialement bouclable ("tilable") basé sur FastNoiseLite.
/// Permet de créer des cartes procédurales planétaires sans couture.
/// </summary>
public class TiledFastNoiseLite
{
    private FastNoiseLite noise;
    private float repeatX;
    private float repeatY;
    private float repeatZ;

    /// <param name="seed">Graine du bruit</param>
    /// <param name="frequency">Fréquence de base</param>
    /// <param name="repeatX">Période en X (taille de répétition spatiale)</param>
    /// <param name="repeatZ">Période en Z (taille de répétition spatiale)</param>
    public TiledFastNoiseLite(int seed, float frequency = 0.01f, float repeatX = 250_000_000f, float repeatY = 250_000_000f, float repeatZ = 250_000_000f, FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2, FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None, float fractalGain = 0.5f, int octaves = 3)
    {
        noise = new FastNoiseLite(seed);
        noise.SetNoiseType(noiseType);
        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalGain(fractalGain);
        noise.SetFrequency(frequency);

        this.repeatX = repeatX;
        this.repeatY = repeatY;
        this.repeatZ = repeatZ;
    }

    /// <summary>
    /// Retourne un bruit parfaitement bouclable dans l’espace (wrap spatial).
    /// </summary>
    public float GetTiledNoise(float x, float z)
    {
        // Projection torique : map (x,z) -> (cos,sin,cos,sin)
        float angleX = (x / repeatX) * Mathf.PI * 2f;
        float angleZ = (z / repeatZ) * Mathf.PI * 2f;

        float nx = Mathf.Cos(angleX), ny = Mathf.Sin(angleX);
        float nz = Mathf.Cos(angleZ), nw = Mathf.Sin(angleZ);
        // On utilise 3D pour approcher le 4D torique (FastNoiseLite n’a pas 4D)
        float n = noise.GetNoise(nx + nw * 0.5f, ny + nz * 0.5f, nw);
        // float n = noise.GetNoise(x, z);
        return n;
    }
    public float GetTiledNoise(float x, float y, float z)
    {
        // Angles pour chaque axe
        float angleX = (x / repeatX) * Mathf.PI * 2f;
        float angleY = (y / repeatX) * Mathf.PI * 2f; // même période que X, modifie si besoin
        float angleZ = (z / repeatZ) * Mathf.PI * 2f;

        // Coordonnées toriques
        float nx = Mathf.Cos(angleX);
        float ny = Mathf.Sin(angleX);
        float nz = Mathf.Cos(angleY);
        float nw = Mathf.Sin(angleY);
        float nu = Mathf.Cos(angleZ);
        float nv = Mathf.Sin(angleZ);

        // Projection 6D → 3D (approximation compacte)
        // On "mélange" les 6 composantes dans 3 dimensions du bruit
        float px = nx + 0.5f * nu;
        float py = ny + 0.5f * nz;
        float pz = nw + 0.5f * nv;

        // Bruit 3D sur ces coordonnées
        return noise.GetNoise(px, py, pz);
        // return noise.GetNoise(nx + nw * 0.5f, ny + nz * 0.5f, y);
    }

}
