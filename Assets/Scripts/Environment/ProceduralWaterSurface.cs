using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralWaterSurface : MonoBehaviour
{
    [SerializeField, Min(1)] private int segmentsX = 96;
    [SerializeField, Min(1)] private int segmentsZ = 96;
    [SerializeField, Min(1.0f)] private float sizeX = 96.0f;
    [SerializeField, Min(1.0f)] private float sizeZ = 96.0f;
    [SerializeField] private float waterLevel = 0.0f;
    [SerializeField] private bool generateOnEnable = true;

    private Mesh _mesh;

    private void OnEnable()
    {
        if (generateOnEnable)
        {
            Generate();
        }
    }

    private void OnValidate()
    {
        segmentsX = Mathf.Max(1, segmentsX);
        segmentsZ = Mathf.Max(1, segmentsZ);
        sizeX = Mathf.Max(1.0f, sizeX);
        sizeZ = Mathf.Max(1.0f, sizeZ);

        if (isActiveAndEnabled && generateOnEnable)
        {
            Generate();
        }
    }

    [ContextMenu("Generate Water Surface")]
    public void Generate()
    {
        int vertexCount = (segmentsX + 1) * (segmentsZ + 1);
        var vertices = new List<Vector3>(vertexCount);
        var uvs = new List<Vector2>(vertexCount);
        var triangles = new List<int>(segmentsX * segmentsZ * 6);

        for (int z = 0; z <= segmentsZ; z++)
        {
            float v = z / (float)segmentsZ;
            for (int x = 0; x <= segmentsX; x++)
            {
                float u = x / (float)segmentsX;
                vertices.Add(new Vector3((u - 0.5f) * sizeX, waterLevel, (v - 0.5f) * sizeZ));
                uvs.Add(new Vector2(u, v));
            }
        }

        for (int z = 0; z < segmentsZ; z++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int i = z * (segmentsX + 1) + x;
                triangles.Add(i);
                triangles.Add(i + segmentsX + 1);
                triangles.Add(i + 1);
                triangles.Add(i + 1);
                triangles.Add(i + segmentsX + 1);
                triangles.Add(i + segmentsX + 2);
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "Generated Water Surface" };
        }

        _mesh.Clear();
        _mesh.indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        _mesh.SetVertices(vertices);
        _mesh.SetUVs(0, uvs);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }
}
