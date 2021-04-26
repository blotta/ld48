using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waves : MonoBehaviour
{
    public int Dimension = 30;
    public float UVScale = 30f;
    public Octave[] Octaves;

    public Mesh MeshEdtiorMesh;

    protected MeshFilter MeshFilter;
    protected Mesh Mesh;

    void Start()
    {
        Mesh = new Mesh();
        Mesh.name = gameObject.name;

        Mesh.vertices = GenerateVerts();
        Mesh.triangles = GenerateTries();
        Mesh.uv = GenerateUVs();
        Mesh.RecalculateBounds();
        Mesh.RecalculateNormals();

        MeshFilter = gameObject.AddComponent<MeshFilter>();
        MeshFilter.mesh = Mesh;
        
    }

    public float GetHeight(Vector3 position)
    {
        // Scale factor and position in local space
        var scale = new Vector3(1 / transform.lossyScale.x, 0, 1 / transform.lossyScale.z);
        var localPos = Vector3.Scale((position - transform.position), scale);

        // get edge points
        var p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
        var p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
        var p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
        var p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

        // clamp if the position is outside the plane
        p1.x = Mathf.Clamp(p1.x, 0, Dimension);
        p1.z = Mathf.Clamp(p1.z, 0, Dimension);
        p2.x = Mathf.Clamp(p2.x, 0, Dimension);
        p2.z = Mathf.Clamp(p2.z, 0, Dimension);
        p3.x = Mathf.Clamp(p3.x, 0, Dimension);
        p3.z = Mathf.Clamp(p3.z, 0, Dimension);
        p4.x = Mathf.Clamp(p4.x, 0, Dimension);
        p4.z = Mathf.Clamp(p4.z, 0, Dimension);

        // get the max distance to one of the edges and take that to compute max - dist
        var p1Dist = Vector3.Distance(p1, localPos);
        var p2Dist = Vector3.Distance(p2, localPos);
        var p3Dist = Vector3.Distance(p3, localPos);
        var p4Dist = Vector3.Distance(p4, localPos);

        var max = Mathf.Max(p1Dist, p2Dist, p3Dist, p4Dist + Mathf.Epsilon);
        var dist = (max - p1Dist) + (max - p2Dist) + (max - p3Dist) + (max - p4Dist + Mathf.Epsilon);

        // Weigted sum
        var height = Mesh.vertices[index((int)p1.x, (int)p1.z)].y * (max - p1Dist)
                   + Mesh.vertices[index((int)p2.x, (int)p2.z)].y * (max - p2Dist)
                   + Mesh.vertices[index((int)p3.x, (int)p3.z)].y * (max - p3Dist)
                   + Mesh.vertices[index((int)p4.x, (int)p4.z)].y * (max - p4Dist);

        return height * transform.localScale.y / dist;
    }

    private Vector3[] GenerateVerts()
    {
        var verts = new Vector3[(Dimension + 1) * (Dimension + 1)];

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                verts[index(x, z)] = new Vector3(x, 0, z);
            }
        }

        return verts;
    }

    private int index(int x, int z)
    {
        return x * (Dimension + 1) + z;
    }

    private int[] GenerateTries()
    {
        var tries = new int[Mesh.vertices.Length * 6];

        for (int x = 0; x < Dimension; x++)
        {
            for (int z = 0; z < Dimension; z++)
            {
                tries[index(x, z) * 6 + 0] = index(x, z);
                tries[index(x, z) * 6 + 1] = index(x + 1, z + 1);
                tries[index(x, z) * 6 + 2] = index(x + 1, z);
                tries[index(x, z) * 6 + 3] = index(x, z);
                tries[index(x, z) * 6 + 4] = index(x, z + 1);
                tries[index(x, z) * 6 + 5] = index(x + 1, z + 1);
            }
        }

        return tries;
    }

    private Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[Mesh.vertices.Length];

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                var vec = new Vector2((x / UVScale) % 2, (z / UVScale % 2));
                uvs[index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }
        return uvs;
    }

    void Update()
    {
        var verts = Mesh.vertices;
        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                var y = 0f;
                for (int o = 0; o < Octaves.Length; o++)
                {
                    if (Octaves[o].alternate)
                    {
                        var perl = Mathf.PerlinNoise((x * Octaves[o].scale.x) / Dimension, (z * Octaves[o].scale.y) / Dimension) * Mathf.PI * 2f;
                        y += Mathf.Cos(perl + Octaves[o].speed.magnitude * Time.time) * Octaves[o].height;
                    }
                    else
                    {
                        var perl = Mathf.PerlinNoise(
                            (x * Octaves[o].scale.x + Time.time * Octaves[o].speed.x)/Dimension,
                            (z * Octaves[o].scale.y + Time.time * Octaves[o].speed.y)/ Dimension) - 0.5f;
                        y += perl * Octaves[o].height;
                    }
                }
                verts[index(x, z)] = new Vector3(x, y, z);
            }
        }

        Mesh.vertices = verts;
        Mesh.RecalculateNormals();
    }

    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;

        Gizmos.color = new Color(0, 0, 1f, 0.5f);
        float dimX = transform.localScale.x * Dimension;
        float dimZ = transform.localScale.z * Dimension;
        Vector3 pos = transform.position + new Vector3(dimX/2f, 0, dimZ/2f);
        Gizmos.DrawCube(pos, new Vector3(dimX, 0.1f, dimZ));

    }
}
