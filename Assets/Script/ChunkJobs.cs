using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

[BurstCompile]
public struct GetVoxelDataJob : IJobParallelFor
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
        for (int axis = 0; axis < 3; axis++)
        {
            // int u = (axis == 0) ? 1 : 0;
            // int v = (axis == 2) ? 1 : 2;
            // but simpler: we will handle dimension sizes explicitly for each axis below

            if (axis == 0)
            {
                // Slices along X: for xi from 0..chunkSize
                for (int xi = 0; xi <= chunkSize; xi++)
                {
                    // build mask for slice xi (plane yz)
                    int idx = 0;
                    for (int zi = 0; zi < chunkSize; zi++)
                    {
                        for (int yi = 0; yi < chunkSize; yi++)
                        {
                            bool a = VoxelAt(xi - 1, yi, zi); // left voxel
                            bool b = VoxelAt(xi, yi, zi);     // right voxel
                            mask[idx++] = (a != b) ? (a ? 1 : -1) : 0; // store sign: 1 means face pointing +X? we encode later
                        }
                    }
                    // apply greedy on mask w x h

                    GreedyOnMask(mask, chunkSize, xi, axis);
                }
            }
            else if (axis == 1)
            {
                // Slices along Y: xi from 0..chunkSize (plane xz)
                for (int yi = 0; yi <= chunkSize; yi++)
                {
                    int idx = 0;
                    for (int zi = 0; zi < chunkSize; zi++)
                    {
                        for (int xi2 = 0; xi2 < chunkSize; xi2++)
                        {
                            bool a = VoxelAt(xi2, yi - 1, zi);
                            bool b = VoxelAt(xi2, yi, zi);
                            mask[idx++] = (a != b) ? (a ? 1 : -1) : 0;
                        }
                    }
                    GreedyOnMask(mask, chunkSize, yi, axis);
                }
            }
            else // axis == 2
            {
                // Slices along Z: zi from 0..chunkSize (plane xy)
                for (int zi = 0; zi <= chunkSize; zi++)
                {
                    int idx = 0;
                    for (int yi = 0; yi < chunkSize; yi++)
                    {
                        for (int xi2 = 0; xi2 < chunkSize; xi2++)
                        {
                            bool a = VoxelAt(xi2, yi, zi - 1);
                            bool b = VoxelAt(xi2, yi, zi);
                            mask[idx++] = (a != b) ? (a ? 1 : -1) : 0;
                        }
                    }
                    GreedyOnMask(mask, chunkSize, zi, axis);
                }
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
    private void AddQuadForRect(int x, int y, int width, int height, int sliceIndex, int axis, int sign)
    {
        // Déterminer origin, du, dv et la normale désirée selon l'axe (corrigé pour X/Y/Z)
        Vector3 origin;
        Vector3 du;
        Vector3 dv;
        Vector3 desiredNormal;

        if (axis == 0) // slices X (plane YZ)
        {
            // origin: (sliceIndex, y, x) ; étalement du rectangle : width -> Z, height -> Y
            origin = new Vector3(sliceIndex * voxelSize, y * voxelSize, x * voxelSize);
            du = new Vector3(0f, width * voxelSize, 0f);   // le "u" parcourt Z
            dv = new Vector3(0f, 0f, height * voxelSize);  // le "v" parcourt Y
            desiredNormal = (sign == 1) ? Vector3.right : Vector3.left;
        }
        else if (axis == 1) // slices Y (plane XZ)
        {
            // origin: (x, sliceIndex, y); étalement : width -> X, height -> Z
            origin = new Vector3(x * voxelSize, sliceIndex * voxelSize, y * voxelSize);
            du = new Vector3(width * voxelSize, 0f, 0f);   // u -> X
            dv = new Vector3(0f, 0f, height * voxelSize);  // v -> Z
            desiredNormal = (sign == 1) ? Vector3.up : Vector3.down;
        }
        else // axis == 2, slices Z (plane XY)
        {
            // origin: (x, y, sliceIndex); étalement : width -> X, height -> Y
            origin = new Vector3(x * voxelSize, y * voxelSize, sliceIndex * voxelSize);
            du = new Vector3(width * voxelSize, 0f, 0f);   // u -> X
            dv = new Vector3(0f, height * voxelSize, 0f);  // v -> Y
            desiredNormal = (sign == 1) ? Vector3.forward : Vector3.back;
        }

        // coins du quad (dans l'ordre "rectangle")
        Vector3 p0 = origin;
        Vector3 p1 = origin + du;
        Vector3 p2 = origin + du + dv;
        Vector3 p3 = origin + dv;

        // calculer la normale du triangle (p1-p0) x (p2-p0)
        Vector3 faceNormal = new Vector3(
            (p1.y - p0.y) * (p2.z - p0.z) - (p1.z - p0.z) * (p2.y - p0.y),
            (p1.z - p0.z) * (p2.x - p0.x) - (p1.x - p0.x) * (p2.z - p0.z),
            (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x)
        );

        // Si faceNormal et desiredNormal pointent dans des directions opposées -> inverser la winding
        bool needsFlip = Vector3.Dot(faceNormal, desiredNormal) < 0f;

        int vStart = vertices.Length;

        if (!needsFlip)
        {
            // ordre normal
            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p2);
            vertices.Add(p3);

            triangles.Add(vStart + 0);
            triangles.Add(vStart + 1);
            triangles.Add(vStart + 2);

            triangles.Add(vStart + 0);
            triangles.Add(vStart + 2);
            triangles.Add(vStart + 3);
        }
        else
        {
            // ordre inversé
            vertices.Add(p0);
            vertices.Add(p3);
            vertices.Add(p2);
            vertices.Add(p1);

            triangles.Add(vStart + 0);
            triangles.Add(vStart + 1);
            triangles.Add(vStart + 2);

            triangles.Add(vStart + 0);
            triangles.Add(vStart + 2);
            triangles.Add(vStart + 3);
        }

        // UVs simples (tu peux adapter à ton atlas)
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(width, 0));
        uvs.Add(new Vector2(width, height));
        uvs.Add(new Vector2(0, height));

        // Normales uniformes = desiredNormal (même si on a flip, normal reste la même)
        normals.Add(desiredNormal);
        normals.Add(desiredNormal);
        normals.Add(desiredNormal);
        normals.Add(desiredNormal);
    }
}