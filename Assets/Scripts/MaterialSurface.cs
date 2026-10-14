using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class MaterialSurface : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField, Min(1f)] private float size = 10f;
    [SerializeField, Range(16, 256)] private int resolution = 128;

    [Header("Material")]
    [SerializeField, Min(0.001f)] private float accumulationSpeed = 0.5f;
    [SerializeField, Min(0.1f)] private float maxHeight = 5f;

    private float[] heights;
    private Vector3[] vertices;
    private int[] triangles;
    private Mesh mesh;

    private int vertexCount;
    private float cellSize;
    private bool meshDirty;

    public float CellSize => cellSize;
    public float Size => size;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        vertexCount = resolution + 1;
        cellSize = size / resolution;

        int totalVertices = vertexCount * vertexCount;

        heights = new float[totalVertices];
        vertices = new Vector3[totalVertices];
        triangles = new int[resolution * resolution * 6];

        for (int z = 0; z < vertexCount; z++)
        {
            for (int x = 0; x < vertexCount; x++)
            {
                vertices[Index(x, z)] = new Vector3(
                    -size * 0.5f + x * cellSize,
                    0f,
                    -size * 0.5f + z * cellSize
                );
            }
        }

        int triangleIndex = 0;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bottomLeft = Index(x, z);
                int topLeft = Index(x, z + 1);
                int bottomRight = Index(x + 1, z);
                int topRight = Index(x + 1, z + 1);

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomRight;
            }
        }
        mesh = new Mesh
        {
            name = "Accumulated Material Mesh",
            indexFormat = totalVertices > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        meshDirty = false;
    }
    private int Index(int x, int z)
    {
        return z * vertexCount + x;
    }
    public void AddMaterialAt(Vector2 center, float radius, float deltaTime)
    {
        if (radius <= 0f || deltaTime <= 0f)
            return;
        float half = size * 0.5f;
        int minX = Mathf.Max(0, Mathf.FloorToInt((center.x + half - radius) / cellSize));
        int maxX = Mathf.Min(resolution, Mathf.CeilToInt((center.x + half + radius) / cellSize));
        int minZ = Mathf.Max(0, Mathf.FloorToInt((center.y + half - radius) / cellSize));
        int maxZ = Mathf.Min(resolution, Mathf.CeilToInt((center.y + half + radius) / cellSize));
        float radiusSquared = radius * radius;
        float heightDelta = accumulationSpeed * deltaTime;

        for (int z = minZ; z <= maxZ; z++)
        {
            float dz = -half + z * cellSize - center.y;

            for (int x = minX; x <= maxX; x++)
            {
                float dx = -half + x * cellSize - center.x;
                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared >= radiusSquared)
                    continue;

                float profile = Mathf.Sqrt(radiusSquared - distanceSquared) / radius;

                int index = Index(x, z);
                float oldHeight = heights[index];
                float newHeight = Mathf.Min(oldHeight + heightDelta * profile, maxHeight);

                if (newHeight <= oldHeight)
                    continue;

                heights[index] = newHeight;

                Vector3 vertex = vertices[index];
                vertex.y = newHeight;
                vertices[index] = vertex;

                meshDirty = true;
            }
        }
    }
    public void ApplyMeshChanges()
    {
        if (!meshDirty)
            return;

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshDirty = false;
    }
    public void ResetSurface()
    {
        for (int i = 0; i < heights.Length; i++)
        {
            heights[i] = 0f;

            Vector3 vertex = vertices[i];
            vertex.y = 0f;
            vertices[i] = vertex;
        }

        meshDirty = true;
        ApplyMeshChanges();
    }
    private void OnDestroy()
    {
        if (mesh == null)
            return;

        if (Application.isPlaying)
            Destroy(mesh);
        else
            DestroyImmediate(mesh);
    }
}