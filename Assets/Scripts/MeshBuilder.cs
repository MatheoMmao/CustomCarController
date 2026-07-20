using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder
{
    private List<Vector3> m_Vertices = new List<Vector3>();
    public List<Vector3> Vertices { get { return m_Vertices; } }

    private List<Vector3> m_Normals = new List<Vector3>();
    public List<Vector3> Normals { get { return m_Normals; } }

    private List<Vector2> m_UVs = new List<Vector2>();
    public List<Vector2> UVs { get { return m_UVs; } }

    private List<int> m_Indices = new List<int>();

    public void AddTriangle(int index0, int index1, int index2)
    {
        m_Indices.Add(index0);
        m_Indices.Add(index1);
        m_Indices.Add(index2);
    }

    public void AddPoint(Vector3 vertices, Vector3 normals, Vector2 uv)
    {
        Vertices.Add(vertices);
        Normals.Add(normals);
        UVs.Add(uv);
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = m_Vertices.ToArray();
        mesh.triangles = m_Indices.ToArray();

        //Normals are optional. Only use them if we have the correct amount:
        if (m_Normals.Count == m_Vertices.Count)
            mesh.normals = m_Normals.ToArray();

        //UVs are optional. Only use them if we have the correct amount:
        if (m_UVs.Count == m_Vertices.Count)
            mesh.uv = m_UVs.ToArray();

        mesh.RecalculateBounds();

        return mesh;
    }

    public void BuildQuad(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4, Vector3 normal)
    {
        AddPoint(point1, normal, Vector2.zero);
        AddPoint(point2, normal, new Vector2(0, 1));
        AddPoint(point3, normal, new Vector2(1, 1));
        AddPoint(point4, normal, new Vector2(1, 0));

        int baseIndex = Vertices.Count - 4;

        AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
        AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
    }
}
