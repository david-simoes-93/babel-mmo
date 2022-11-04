using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class MakeSphereHemisphere : MonoBehaviour
{
    [Header("Sphere Options")]
    internal float radius = 1f;

    [Range(1, 3)]
    internal int meshSmoothness = 1;

    [Space]
    [Header("Hemisphere Options")]
    [Tooltip("Create north hemisphere only")]
    internal bool hemisphere = false;

    [Tooltip("UV covers full sphere")]
    internal bool fullUvRange = true;

    // Use this for initialization
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Make(mf);
        FlipNormals(mf);
    }

    void Make(MeshFilter mf)
    {
        Mesh mesh = mf.mesh;
        mesh.Clear();

        // Longitude |||
        int nbLong = 24 * meshSmoothness;
        // Latitude ---
        int nbLat = 16 * meshSmoothness;

        #region Vertices
        Vector3[] vertices = new Vector3[(nbLong + 1) * ((hemisphere) ? nbLat / 2 : nbLat) + 2];
        float _pi = Mathf.PI;
        float _2pi = _pi * 2f;

        vertices[0] = Vector3.up * radius;
        //for (int lat = 0; lat < nbLat; lat++)
        for (int lat = 0; lat < ((hemisphere) ? nbLat / 2 : nbLat); lat++)
        {
            float a1 = _pi * (float)(lat + 1) / (nbLat + 1);
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);

            for (int lon = 0; lon <= nbLong; lon++)
            {
                float a2 = _2pi * (float)(lon == nbLong ? 0 : lon) / nbLong;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);

                vertices[lon + lat * (nbLong + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
            }
        }
        vertices[vertices.Length - 1] = Vector3.up * -radius;
        #endregion

        #region Normals
        Vector3[] normals = new Vector3[vertices.Length];
        for (int n = 0; n < vertices.Length; n++)
            normals[n] = vertices[n].normalized;
        #endregion

        #region UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = Vector2.up;
        uvs[uvs.Length - 1] = Vector2.zero;
        //for (int lat = 0; lat < nbLat; lat++)
        for (int lat = 0; lat < ((hemisphere) ? nbLat / 2 : nbLat); lat++)
            for (int lon = 0; lon <= nbLong; lon++)
            {
                //uvs[lon + lat * (nbLong + 1) + 1] = new Vector2((float)lon / nbLong, 1f - (float)(lat + 1) / (nbLat + 1));
                if (!hemisphere || fullUvRange)
                    uvs[lon + lat * (nbLong + 1) + 1] = new Vector2((float)lon / nbLong, 1f - (float)(lat + 1) / (nbLat + 1));
                else
                    uvs[lon + lat * (nbLong + 1) + 1] = new Vector2((float)lon / nbLong, 1f - (float)(lat + 1) / (nbLat / 2));
            }
        #endregion

        #region Triangles
        int nbFaces = vertices.Length;
        int nbTriangles = nbFaces * 2;
        int nbIndexes = nbTriangles * 3;
        int[] triangles = new int[nbIndexes];

        //Top Cap
        int i = 0;
        for (int lon = 0; lon < nbLong; lon++)
        {
            triangles[i++] = lon + 2;
            triangles[i++] = lon + 1;
            triangles[i++] = 0;
        }

        //Middle
        for (int lat = 0; lat < ((hemisphere) ? nbLat / 2 : nbLat) - 1; lat++)
        {
            for (int lon = 0; lon < nbLong; lon++)
            {
                int current = lon + lat * (nbLong + 1) + 1;
                int next = current + nbLong + 1;

                triangles[i++] = current;
                triangles[i++] = current + 1;
                triangles[i++] = next + 1;

                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = next;
            }
        }

        if (!hemisphere)
        {
            //Bottom Cap
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }
        }
        #endregion

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
    }

    void FlipNormals(MeshFilter filter)
    {
        if (filter != null)
        {
            Mesh mesh = filter.mesh;

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] = -normals[i];
            mesh.normals = normals;

            for (int m = 0; m < mesh.subMeshCount; m++)
            {
                int[] triangles = mesh.GetTriangles(m);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i + 0];
                    triangles[i + 0] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                mesh.SetTriangles(triangles, m);
            }
        }
    }
}
