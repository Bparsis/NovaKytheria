using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
[BurstCompile]
public struct GetVoxelDataParallelJob : IJobParallelFor
{
    public int chunkSize;
    public Vector3Int chunkCoords;
    public NativeArray<byte> voxels;

    public void Execute(int index)
    {
        int sizeWithBorder = chunkSize + 2;

        // Conversion index linéaire → x, y, z
        int x = index % sizeWithBorder;
        int y = (index / sizeWithBorder) % sizeWithBorder;
        int z = index / (sizeWithBorder * sizeWithBorder);

        int worldX = chunkCoords.x * chunkSize + (x - 1);
        int worldY = chunkCoords.y * chunkSize + (y - 1);
        int worldZ = chunkCoords.z * chunkSize + (z - 1);

        voxels[index] = (worldY < 0) ? (byte)1 : (byte)0;
    }
}

[BurstCompile]
public struct GetVoxelDataJob : IJob
{
    public GenerationNoise genMotor;
    public int chunkSize;
    public int step;
    public Vector3Int chunkCoords;
    public NativeArray<byte> voxels; // length = sizeX*sizeY*sizeZ

    public void Execute()
    {
        float scale = 0.008f;
        for (int z = -1; z < chunkSize + 1; z++)
            for (int y = -1; y < chunkSize + 1; y++)
                for (int x = -1; x < chunkSize + 1; x++)
                {
                    int worldY = chunkCoords.y * chunkSize * step + y * step + (step / 2);
                    int worldX = chunkCoords.x * chunkSize * step + x * step + (step / 2);
                    int worldZ = chunkCoords.z * chunkSize * step + z * step + (step / 2);


                    int i = Index(x + 1, y + 1, z + 1); // +1 pour bordure

                    //?Platform basic
                    // voxels[i] = (worldY < 0) ? (byte)1 : (byte)0;

                    //?Pyramid
                    // int cx = chunkSize / 2;
                    // int cz = chunkSize / 2;

                    // int relX = x - cx;
                    // int relZ = z - cz;

                    // float height = 64f; // hauteur de la pyramide
                    // float r = height - worldY;

                    // voxels[i] = (worldY >= 0 && worldY <= height &&
                    //              Math.Abs(relX) <= r && Math.Abs(relZ) <= r)
                    //             ? (byte)1 : (byte)0;

                    // //?genMotor Noise height
                    float height = genMotor.GetHeight(x, z);
                    bool isCaveAir = genMotor.GetCave(x, y, z) < -0.2f && worldY < height ? true : false;
                    voxels[i] = (worldY < height && !isCaveAir) ? (byte)1 : (byte)0;
                }
    }

    private int Index(int x, int y, int z)
    {
        int sizeWithBorder = chunkSize + 2;
        return x + sizeWithBorder * (y + sizeWithBorder * z);
    }
}


[BurstCompile]
public struct GreedyMesherJob : IJob
{
    public int chunkSize;
    public float voxelSize;

    [ReadOnly] public NativeArray<byte> voxels; // 0 = empty, 1 = solid

    // Outputs
    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Vector2> uvs;
    public NativeList<Vector3> normals;

    // Helper to test voxel (safely returns 0 if out of bounds)
    private bool VoxelAt(int x, int y, int z)
    {
        int sizeWithBorder = chunkSize + 2;
        x += 1; y += 1; z += 1; // border offset
        if (x < 0 || x >= sizeWithBorder || y < 0 || y >= sizeWithBorder || z < 0 || z >= sizeWithBorder) return false;
        int idx = x + sizeWithBorder * (y + sizeWithBorder * z);
        return voxels[idx] != 0;
    }

    public void Execute()
    {
        // Greedy per direction: 0=X,1=Y,2=Z, and for each both positive faces and negative.
        // We'll implement for 6 face directions by reusing same mask logic.

        // Temporary mask max size = (sizeX*sizeY) when slicing on Z, etc.
        int maxMaskSize = chunkSize * chunkSize;
        NativeArray<int> mask = new NativeArray<int>(maxMaskSize, Allocator.Temp); // 0 = empty, 1 = filled

        // For each axis
        // axis = 0 -> x, iterate d from 0..sizeX (slices between voxel columns)
        // axis = 1 -> y, axis = 2 -> z
        // === Remplace la boucle "for (axis...)" dans Execute() par ceci ===
        for (int axis = 0; axis < 3; axis++)
        {
            int maxMaskSizeLocal = chunkSize * chunkSize;
            for (int slice = 0; slice <= chunkSize; slice++)
            {
                // On remplit le mask en s'assurant que "u" (width) est la dimension qui varie le plus vite
                int idx = 0;
                if (axis == 0) // slices X -> plane YZ  (u = z, v = y)
                {
                    for (int v = 0; v < chunkSize; v++)        // v = y
                        for (int u = 0; u < chunkSize; u++)    // u = z (u inner)
                        {
                            bool a = VoxelAt(slice - 1, v, u);
                            bool b = VoxelAt(slice, v, u);
                            mask[idx++] = (a != b) ? (a ? 1 : -1) : 0;
                        }
                }
                else if (axis == 1) // slices Y -> plane XZ (u = x, v = z)
                {
                    for (int v = 0; v < chunkSize; v++)        // v = z
                        for (int u = 0; u < chunkSize; u++)    // u = x
                        {
                            bool a = VoxelAt(u, slice - 1, v);
                            bool b = VoxelAt(u, slice, v);
                            mask[idx++] = (a != b) ? (a ? 1 : -1) : 0;
                        }
                }
                else // axis == 2, slices Z -> plane XY (u = x, v = y)
                {
                    for (int v = 0; v < chunkSize; v++)        // v = y
                        for (int u = 0; u < chunkSize; u++)    // u = x
                        {
                            bool a = VoxelAt(u, v, slice - 1);
                            bool b = VoxelAt(u, v, slice);
                            mask[idx++] = (a != b) ? (a ? 1 : -1) : 0;
                        }
                }

                GreedyOnMask(mask, chunkSize, slice, axis);
            }
        }



        mask.Dispose();
    }

    // mask: NativeArray<int> with size w*h. Values: 0 = none, 1 = face where 'a' is solid and b empty (face on one side), -1 opposite
    // dims: width = dimA, height = dimB (order used above)
    private void GreedyOnMask(NativeArray<int> mask, int chunkSize, int sliceIndex, int axis)
    {
        int w = chunkSize;
        int h = chunkSize;

        int i = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w;)
            {
                int m = mask[i];
                if (m != 0)
                {
                    // find width
                    int width = 1;
                    while (x + width < w && mask[i + width] == m) width++;

                    // find height
                    int height = 1;
                    bool done = false;
                    while (y + height < h && !done)
                    {
                        for (int k = 0; k < width; k++)
                        {
                            if (mask[i + k + height * w] != m)
                            {
                                done = true;
                                break;
                            }
                        }
                        if (!done) height++;
                    }

                    // create quad for rectangle (x,y)-(x+width, y+height) on the plane
                    AddQuadForRect(x, y, width, height, sliceIndex, axis, m);

                    // zero out mask area
                    for (int dy = 0; dy < height; dy++)
                        for (int dx = 0; dx < width; dx++)
                            mask[i + dx + dy * w] = 0;

                    x += width;
                    i += width;
                }
                else
                {
                    x++;
                    i++;
                }
            }
        }
    }

    // AddQuadForRect corrigé : calcule la winding en fonction de la normale désirée
    // === Remplace ta AddQuadForRect par cette version ===
    private void AddQuadForRect(int u, int v, int width, int height, int sliceIndex, int axis, int sign)
    {
        Vector3 origin;
        Vector3 du;
        Vector3 dv;
        Vector3 desiredNormal;

        if (axis == 0) // plane YZ, u = z, v = y
        {
            origin = new Vector3(sliceIndex * voxelSize, v * voxelSize, u * voxelSize);
            du = new Vector3(0f, 0f, width * voxelSize); // u -> +Z
            dv = new Vector3(0f, height * voxelSize, 0f); // v -> +Y
            desiredNormal = (sign == 1) ? Vector3.right : Vector3.left;
        }
        else if (axis == 1) // plane XZ, u = x, v = z
        {
            origin = new Vector3(u * voxelSize, sliceIndex * voxelSize, v * voxelSize);
            du = new Vector3(width * voxelSize, 0f, 0f); // u -> +X
            dv = new Vector3(0f, 0f, height * voxelSize); // v -> +Z
            desiredNormal = (sign == 1) ? Vector3.up : Vector3.down;
        }
        else // axis == 2, plane XY, u = x, v = y
        {
            origin = new Vector3(u * voxelSize, v * voxelSize, sliceIndex * voxelSize);
            du = new Vector3(width * voxelSize, 0f, 0f); // u -> +X
            dv = new Vector3(0f, height * voxelSize, 0f); // v -> +Y
            desiredNormal = (sign == 1) ? Vector3.forward : Vector3.back;
        }

        Vector3 p0 = origin;
        Vector3 p1 = origin + du;
        Vector3 p2 = origin + du + dv;
        Vector3 p3 = origin + dv;

        // On calcule la normale de (du x dv) et on compare au désiré pour savoir si on doit inverser le winding
        Vector3 faceNormal = Vector3.Cross(du, dv);
        bool needsFlip = Vector3.Dot(faceNormal, desiredNormal) < 0f;

        int vStart = vertices.Length;
        if (!needsFlip)
        {
            vertices.Add(p0); vertices.Add(p1); vertices.Add(p2); vertices.Add(p3);

            triangles.Add(vStart + 0); triangles.Add(vStart + 1); triangles.Add(vStart + 2);
            triangles.Add(vStart + 0); triangles.Add(vStart + 2); triangles.Add(vStart + 3);
        }
        else
        {
            vertices.Add(p0); vertices.Add(p3); vertices.Add(p2); vertices.Add(p1);

            triangles.Add(vStart + 0); triangles.Add(vStart + 1); triangles.Add(vStart + 2);
            triangles.Add(vStart + 0); triangles.Add(vStart + 2); triangles.Add(vStart + 3);
        }

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(width, 0));
        uvs.Add(new Vector2(width, height));
        uvs.Add(new Vector2(0, height));

        normals.Add(desiredNormal); normals.Add(desiredNormal); normals.Add(desiredNormal); normals.Add(desiredNormal);
    }

}