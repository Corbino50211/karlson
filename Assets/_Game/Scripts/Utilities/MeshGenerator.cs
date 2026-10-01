using System.Collections.Generic;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Procedural mesh builders for shapes Unity has no primitive for (ramps, rings, cones).
    /// Used by the editor generators (saved as assets) and at runtime as a fallback.
    /// </summary>
    public static class MeshGenerator
    {
        class Builder
        {
            public readonly List<Vector3> verts = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<int> tris = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
                {
                    var tmp = b;
                    b = c;
                    c = tmp;
                }
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                normals.Add(normal); normals.Add(normal); normals.Add(normal);
                uvs.Add(Project(a, normal)); uvs.Add(Project(b, normal)); uvs.Add(Project(c, normal));
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                Tri(a, b, c, normal);
                Tri(a, c, d, normal);
            }

            static Vector2 Project(Vector3 p, Vector3 n)
            {
                var an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                if (an.y >= an.x && an.y >= an.z) return new Vector2(p.x, p.z);
                if (an.x >= an.z) return new Vector2(p.z, p.y);
                return new Vector2(p.x, p.y);
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(verts);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }

        /// <summary>
        /// Unit ramp: 1x1 footprint centered on X/Z, bottom at y = 0, rising from 0 at z = -0.5 to 1 at z = +0.5.
        /// </summary>
        public static Mesh CreateWedge()
        {
            var b = new Builder();
            var p0 = new Vector3(-0.5f, 0f, -0.5f);
            var p1 = new Vector3(0.5f, 0f, -0.5f);
            var p2 = new Vector3(0.5f, 0f, 0.5f);
            var p3 = new Vector3(-0.5f, 0f, 0.5f);
            var p4 = new Vector3(-0.5f, 1f, 0.5f);
            var p5 = new Vector3(0.5f, 1f, 0.5f);

            b.Quad(p0, p1, p2, p3, Vector3.down);
            b.Quad(p3, p2, p5, p4, Vector3.forward);
            b.Quad(p0, p1, p5, p4, new Vector3(0f, 1f, -1f).normalized);
            b.Tri(p0, p3, p4, Vector3.left);
            b.Tri(p1, p2, p5, Vector3.right);
            return b.Build("Wedge");
        }

        /// <summary>Torus lying in the XZ plane (axis = Y).</summary>
        public static Mesh CreateTorus(float majorRadius = 1f, float minorRadius = 0.05f, int segments = 48, int sides = 8)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(u) * majorRadius, 0f, Mathf.Sin(u) * majorRadius);
                var outward = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                for (int j = 0; j <= sides; j++)
                {
                    float v = (float)j / sides * Mathf.PI * 2f;
                    var normal = outward * Mathf.Cos(v) + Vector3.up * Mathf.Sin(v);
                    verts.Add(center + normal * minorRadius);
                    normals.Add(normal);
                    uvs.Add(new Vector2((float)i / segments, (float)j / sides));
                }
            }
            int ring = sides + 1;
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int a = i * ring + j;
                    int b = (i + 1) * ring + j;
                    int c = (i + 1) * ring + j + 1;
                    int d = i * ring + j + 1;
                    tris.Add(a); tris.Add(d); tris.Add(b);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }
            var mesh = new Mesh { name = "Torus" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            FixWinding(mesh);
            return mesh;
        }

        /// <summary>Cone with its base centered at the origin pointing up the +Y axis.</summary>
        public static Mesh CreateCone(float radius = 0.5f, float height = 1f, int segments = 24)
        {
            var b = new Builder();
            var tip = new Vector3(0f, height, 0f);
            for (int i = 0; i < segments; i++)
            {
                float a0 = (float)i / segments * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
                var v0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var v1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                var mid = (v0 + v1) * 0.5f;
                var sideNormal = new Vector3(mid.x, radius * radius / height, mid.z).normalized;
                b.Tri(v0, v1, tip, sideNormal);
                b.Tri(Vector3.zero, v0, v1, Vector3.down);
            }
            return b.Build("Cone");
        }

        /// <summary>
        /// Open cylindrical band (ring wall) of radius 1 and height 1 with the given wall thickness.
        /// Used for shockwave rings: scale X/Z to grow the ring.
        /// </summary>
        public static Mesh CreateBand(int segments = 48, float thickness = 0.06f)
        {
            var b = new Builder();
            float inner = 1f - thickness;
            for (int i = 0; i < segments; i++)
            {
                float a0 = (float)i / segments * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                var dm = ((d0 + d1) * 0.5f).normalized;
                // outer wall
                b.Quad(d0, d1, d1 + Vector3.up, d0 + Vector3.up, dm);
                // inner wall
                b.Quad(d0 * inner, d0 * inner + Vector3.up, d1 * inner + Vector3.up, d1 * inner, -dm);
                // top
                b.Quad(d0 * inner + Vector3.up, d0 + Vector3.up, d1 + Vector3.up, d1 * inner + Vector3.up, Vector3.up);
            }
            return b.Build("Band");
        }

        /// <summary>Flat chevron arrow in the XZ plane pointing toward +Z (used on launch/speed pads).</summary>
        public static Mesh CreateChevron(float width = 0.8f, float depth = 0.35f, float thickness = 0.18f)
        {
            var b = new Builder();
            float hw = width * 0.5f;
            var tip = new Vector3(0f, 0f, depth * 0.5f);
            var tipInner = tip - new Vector3(0f, 0f, thickness);
            var left = new Vector3(-hw, 0f, -depth * 0.5f);
            var right = new Vector3(hw, 0f, -depth * 0.5f);
            var leftInner = left - new Vector3(0f, 0f, thickness);
            var rightInner = right - new Vector3(0f, 0f, thickness);
            b.Quad(left, tip, tipInner, leftInner, Vector3.up);
            b.Quad(tip, right, rightInner, tipInner, Vector3.up);
            return b.Build("Chevron");
        }

        /// <summary>Ensures triangles face their stored vertex normals.</summary>
        static void FixWinding(Mesh mesh)
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = v[t[i]];
                var b = v[t[i + 1]];
                var c = v[t[i + 2]];
                var avgN = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), avgN) < 0f)
                {
                    int tmp = t[i + 1];
                    t[i + 1] = t[i + 2];
                    t[i + 2] = tmp;
                }
            }
            mesh.triangles = t;
        }

        static Mesh cachedWedge, cachedTorus, cachedCone, cachedBand, cachedChevron;

        public static Mesh Wedge => cachedWedge != null ? cachedWedge : (cachedWedge = CreateWedge());
        public static Mesh Torus => cachedTorus != null ? cachedTorus : (cachedTorus = CreateTorus());
        public static Mesh Cone => cachedCone != null ? cachedCone : (cachedCone = CreateCone());
        public static Mesh Band => cachedBand != null ? cachedBand : (cachedBand = CreateBand());
        public static Mesh Chevron => cachedChevron != null ? cachedChevron : (cachedChevron = CreateChevron());
    }
}
