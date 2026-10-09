using System.Collections.Generic;
using UnityEngine;

namespace FruitNinjaVR
{
    public static class FruitMeshUtility
    {
        private static Mesh s_HemispherePositive;
        private static Mesh s_HemisphereNegative;

        public static Mesh HemispherePositive
        {
            get
            {
                if (s_HemispherePositive == null) s_HemispherePositive = GenerateHemisphere(true);
                return s_HemispherePositive;
            }
        }

        public static Mesh HemisphereNegative
        {
            get
            {
                if (s_HemisphereNegative == null) s_HemisphereNegative = GenerateHemisphere(false);
                return s_HemisphereNegative;
            }
        }

        private static Mesh GenerateHemisphere(bool positiveSide)
        {
            Mesh mesh = new Mesh();
            mesh.name = positiveSide ? "Hemisphere_Positive" : "Hemisphere_Negative";

            int latSegments = 16;
            int lonSegments = 24;
            float radius = 0.5f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> domeTriangles = new List<int>();
            List<int> capTriangles = new List<int>();

            // --- 1. Generate Dome Vertices & Triangles ---
            // We parameterize the dome: phi from 0 to PI/2 (0 at cap, PI/2 at pole)
            // positiveSide: x = radius * sin(phi), y = radius * cos(phi)*cos(theta), z = radius * cos(phi)*sin(theta)
            // if negativeSide, x = -radius * sin(phi)
            float sign = positiveSide ? 1f : -1f;

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float phi = (Mathf.PI * 0.5f) * ((float)lat / latSegments); // 0 at equator, PI/2 at pole
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float theta = (2f * Mathf.PI) * ((float)lon / lonSegments);
                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    float x = sign * radius * sinPhi;
                    float y = radius * cosPhi * cosTheta;
                    float z = radius * cosPhi * sinTheta;

                    Vector3 pos = new Vector3(x, y, z);
                    Vector3 norm = pos.normalized;

                    vertices.Add(pos);
                    normals.Add(norm);
                    uvs.Add(new Vector2((float)lon / lonSegments, (float)lat / latSegments));
                }
            }

            // Dome triangles
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * (lonSegments + 1) + lon;
                    int next = current + lonSegments + 1;

                    if (positiveSide)
                    {
                        domeTriangles.Add(current);
                        domeTriangles.Add(current + 1);
                        domeTriangles.Add(next);

                        domeTriangles.Add(next);
                        domeTriangles.Add(current + 1);
                        domeTriangles.Add(next + 1);
                    }
                    else
                    {
                        domeTriangles.Add(current);
                        domeTriangles.Add(next);
                        domeTriangles.Add(current + 1);

                        domeTriangles.Add(next);
                        domeTriangles.Add(next + 1);
                        domeTriangles.Add(current + 1);
                    }
                }
            }

            // --- 2. Generate Flat Circular Cap at x = 0 ---
            int capCenterIndex = vertices.Count;
            Vector3 capNormal = new Vector3(-sign, 0f, 0f);
            vertices.Add(Vector3.zero);
            normals.Add(capNormal);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int capRingStart = vertices.Count;
            for (int lon = 0; lon <= lonSegments; lon++)
            {
                float theta = (2f * Mathf.PI) * ((float)lon / lonSegments);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta);

                vertices.Add(new Vector3(0f, y, z));
                normals.Add(capNormal);
                uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(theta), 0.5f + 0.5f * Mathf.Sin(theta)));
            }

            for (int lon = 0; lon < lonSegments; lon++)
            {
                int current = capRingStart + lon;
                int next = current + 1;

                if (positiveSide)
                {
                    capTriangles.Add(capCenterIndex);
                    capTriangles.Add(next);
                    capTriangles.Add(current);
                }
                else
                {
                    capTriangles.Add(capCenterIndex);
                    capTriangles.Add(current);
                    capTriangles.Add(next);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            mesh.subMeshCount = 2;
            mesh.SetTriangles(domeTriangles, 0); // Submesh 0: Rind
            mesh.SetTriangles(capTriangles, 1);  // Submesh 1: Flesh inner cap

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
