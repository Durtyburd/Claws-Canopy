using UnityEngine;

public class StarGenerator : MonoBehaviour
{
    [Header("Stars")]
    [SerializeField] private int starCount = 400;
    [SerializeField] private float skyRadius = 300f;
    [SerializeField] private float minStarSize = 0.2f;
    [SerializeField] private float maxStarSize = 0.6f;

    [Header("Appearance")]
    [SerializeField] private Color starColor = Color.white;

    private void Start()
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = starColor;
        material.enableInstancing = true;

        // Build one shared mesh for all stars (single draw call)
        var combine = new CombineInstance[starCount];

        var quadMesh = CreateQuadMesh();

        for (int i = 0; i < starCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            if (dir.y < 0.05f) dir.y = Mathf.Abs(dir.y) + 0.05f;
            dir.Normalize();

            Vector3 pos = dir * skyRadius;
            float size = Random.Range(minStarSize, maxStarSize);

            Quaternion rot = Quaternion.LookRotation(dir);

            combine[i].mesh = quadMesh;
            combine[i].transform = Matrix4x4.TRS(pos, rot, Vector3.one * size);
        }

        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine, true, true);

        var starsObj = new GameObject("Stars");
        starsObj.transform.SetParent(transform);
        starsObj.transform.localPosition = Vector3.zero;

        var filter = starsObj.AddComponent<MeshFilter>();
        filter.mesh = combinedMesh;

        var renderer = starsObj.AddComponent<MeshRenderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        float h = 0.5f;
        mesh.vertices = new Vector3[]
        {
            new Vector3(-h, -h, 0),
            new Vector3(h, -h, 0),
            new Vector3(h, h, 0),
            new Vector3(-h, h, 0)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.normals = new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
        return mesh;
    }
}
