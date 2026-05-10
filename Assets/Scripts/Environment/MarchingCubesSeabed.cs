using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesSeabed : MonoBehaviour
{
    private static readonly Vector3Int[] CubeCorners =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
    };

    private static readonly int[,] Tetrahedra =
    {
        { 0, 5, 1, 6 },
        { 0, 1, 2, 6 },
        { 0, 2, 3, 6 },
        { 0, 3, 7, 6 },
        { 0, 7, 4, 6 },
        { 0, 4, 5, 6 }
    };

    [Header("Volume")]
    [SerializeField, Min(2)] private int cellsX = 48;
    [SerializeField, Min(2)] private int cellsY = 18;
    [SerializeField, Min(2)] private int cellsZ = 48;
    [SerializeField, Min(0.1f)] private float cellSize = 1.0f;
    [SerializeField] private float isoLevel = 0.0f;

    [Header("Seabed Shape")]
    [SerializeField] private float baseDepth = -12.0f;
    [SerializeField] private float heightAmplitude = 5.5f;
    [SerializeField] private float primaryNoiseScale = 0.055f;
    [SerializeField] private float detailNoiseScale = 0.18f;
    [SerializeField] private float ridgeStrength = 2.0f;
    [SerializeField] private float caveStrength = 0.55f;
    [SerializeField] private Vector2 noiseOffset;

    [Header("Output")]
    [SerializeField] private bool generateOnEnable = true;
    [SerializeField] private bool addMeshCollider;

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
        cellsX = Mathf.Max(2, cellsX);
        cellsY = Mathf.Max(2, cellsY);
        cellsZ = Mathf.Max(2, cellsZ);
        cellSize = Mathf.Max(0.1f, cellSize);
        primaryNoiseScale = Mathf.Max(0.001f, primaryNoiseScale);
        detailNoiseScale = Mathf.Max(0.001f, detailNoiseScale);

        if (isActiveAndEnabled && generateOnEnable)
        {
            Generate();
        }
    }

    [ContextMenu("Generate Seabed")]
    public void Generate()
    {
        var vertices = new List<Vector3>(cellsX * cellsZ * 6);
        var triangles = new List<int>(cellsX * cellsZ * 18);

        Vector3 origin = new Vector3(
            cellsX * cellSize * -0.5f,
            baseDepth - cellsY * cellSize * 0.45f,
            cellsZ * cellSize * -0.5f
        );

        var cornerPositions = new Vector3[8];
        var cornerValues = new float[8];

        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                for (int z = 0; z < cellsZ; z++)
                {
                    Vector3 cellOrigin = origin + new Vector3(x, y, z) * cellSize;

                    for (int corner = 0; corner < CubeCorners.Length; corner++)
                    {
                        Vector3 position = cellOrigin + (Vector3)CubeCorners[corner] * cellSize;
                        cornerPositions[corner] = position;
                        cornerValues[corner] = SampleDensity(position);
                    }

                    PolygonizeCube(cornerPositions, cornerValues, vertices, triangles);
                }
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "Generated Marching Cubes Seabed" };
        }

        _mesh.Clear();
        _mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = _mesh;

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (addMeshCollider)
        {
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = _mesh;
        }
        else if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
        }
    }

    private float SampleDensity(Vector3 position)
    {
        float x = position.x + noiseOffset.x;
        float z = position.z + noiseOffset.y;

        float broad = Mathf.PerlinNoise(x * primaryNoiseScale, z * primaryNoiseScale);
        float detail = Mathf.PerlinNoise(x * detailNoiseScale + 37.1f, z * detailNoiseScale - 19.7f);
        float ridges = 1.0f - Mathf.Abs(detail * 2.0f - 1.0f);
        float seabedHeight = baseDepth + (broad - 0.5f) * heightAmplitude + ridges * ridgeStrength;

        float cave = Mathf.PerlinNoise(
            (x + position.y * 1.7f) * detailNoiseScale,
            (z - position.y * 1.3f) * detailNoiseScale
        );

        return position.y - seabedHeight + (cave - 0.5f) * caveStrength;
    }

    private void PolygonizeCube(
        Vector3[] cubePositions,
        float[] cubeValues,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        var tetraPositions = new Vector3[4];
        var tetraValues = new float[4];

        for (int tetra = 0; tetra < Tetrahedra.GetLength(0); tetra++)
        {
            for (int i = 0; i < 4; i++)
            {
                int cornerIndex = Tetrahedra[tetra, i];
                tetraPositions[i] = cubePositions[cornerIndex];
                tetraValues[i] = cubeValues[cornerIndex];
            }

            PolygonizeTetrahedron(tetraPositions, tetraValues, vertices, triangles);
        }
    }

    private void PolygonizeTetrahedron(
        Vector3[] positions,
        float[] values,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        var inside = new List<int>(4);
        var outside = new List<int>(4);

        for (int i = 0; i < 4; i++)
        {
            if (values[i] <= isoLevel)
            {
                inside.Add(i);
            }
            else
            {
                outside.Add(i);
            }
        }

        if (inside.Count == 0 || inside.Count == 4)
        {
            return;
        }

        if (inside.Count == 1)
        {
            AddTriangle(
                Interpolate(positions[inside[0]], positions[outside[0]], values[inside[0]], values[outside[0]]),
                Interpolate(positions[inside[0]], positions[outside[1]], values[inside[0]], values[outside[1]]),
                Interpolate(positions[inside[0]], positions[outside[2]], values[inside[0]], values[outside[2]]),
                vertices,
                triangles
            );
            return;
        }

        if (inside.Count == 3)
        {
            AddTriangle(
                Interpolate(positions[outside[0]], positions[inside[2]], values[outside[0]], values[inside[2]]),
                Interpolate(positions[outside[0]], positions[inside[1]], values[outside[0]], values[inside[1]]),
                Interpolate(positions[outside[0]], positions[inside[0]], values[outside[0]], values[inside[0]]),
                vertices,
                triangles
            );
            return;
        }

        Vector3 a = Interpolate(positions[inside[0]], positions[outside[0]], values[inside[0]], values[outside[0]]);
        Vector3 b = Interpolate(positions[inside[0]], positions[outside[1]], values[inside[0]], values[outside[1]]);
        Vector3 c = Interpolate(positions[inside[1]], positions[outside[0]], values[inside[1]], values[outside[0]]);
        Vector3 d = Interpolate(positions[inside[1]], positions[outside[1]], values[inside[1]], values[outside[1]]);

        AddTriangle(a, b, c, vertices, triangles);
        AddTriangle(c, b, d, vertices, triangles);
    }

    private Vector3 Interpolate(Vector3 a, Vector3 b, float valueA, float valueB)
    {
        float denominator = valueB - valueA;
        if (Mathf.Abs(denominator) < 0.00001f)
        {
            return (a + b) * 0.5f;
        }

        float t = Mathf.Clamp01((isoLevel - valueA) / denominator);
        return Vector3.Lerp(a, b, t);
    }

    private static void AddTriangle(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }
}
