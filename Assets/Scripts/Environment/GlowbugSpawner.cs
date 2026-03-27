using UnityEngine;

public class GlowbugSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private int spawnCount = 20;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private float maxHeight = 4f;

    [Header("Appearance")]
    [SerializeField] private Color glowColor = new Color(0.5f, 1f, 0.3f);
    [SerializeField] private float bugSize = 0.05f;
    [SerializeField] private float emissionIntensity = 3f;

    [Header("Movement")]
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobAmount = 0.3f;
    [SerializeField] private float driftSpeed = 0.3f;
    [SerializeField] private float driftRadius = 2f;

    private Transform[] _bugs;
    private Vector3[] _origins;
    private float[] _timeOffsets;
    private Vector2[] _driftDirs;

    private void Start()
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = glowColor * emissionIntensity;
        mat.enableInstancing = true;

        _bugs = new Transform[spawnCount];
        _origins = new Vector3[spawnCount];
        _timeOffsets = new float[spawnCount];
        _driftDirs = new Vector2[spawnCount];

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = GetRandomPosition();
            if (pos == Vector3.zero) continue;

            GameObject bug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bug.name = "Glowbug";
            bug.transform.position = pos;
            bug.transform.localScale = Vector3.one * bugSize;
            bug.transform.SetParent(transform);

            Destroy(bug.GetComponent<Collider>());
            bug.GetComponent<Renderer>().sharedMaterial = mat;
            bug.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bug.GetComponent<Renderer>().receiveShadows = false;

            _bugs[spawned] = bug.transform;
            _origins[spawned] = pos;
            _timeOffsets[spawned] = Random.Range(0f, 100f);
            _driftDirs[spawned] = Random.insideUnitCircle.normalized;
            spawned++;
        }

        // Trim arrays if some failed to spawn
        if (spawned < spawnCount)
        {
            System.Array.Resize(ref _bugs, spawned);
            System.Array.Resize(ref _origins, spawned);
            System.Array.Resize(ref _timeOffsets, spawned);
            System.Array.Resize(ref _driftDirs, spawned);
        }
    }

    // Single Update moves all bugs — no per-bug scripts
    private void Update()
    {
        float time = Time.time;

        for (int i = 0; i < _bugs.Length; i++)
        {
            if (_bugs[i] == null) continue;

            float t = time + _timeOffsets[i];
            float bobY = Mathf.Sin(t * bobSpeed) * bobAmount;
            float dX = Mathf.Sin(t * driftSpeed * 0.7f) * driftRadius;
            float dZ = Mathf.Cos(t * driftSpeed) * driftRadius;

            _bugs[i].position = _origins[i] + new Vector3(
                dX * _driftDirs[i].x,
                bobY,
                dZ * _driftDirs[i].y
            );
        }
    }

    private Vector3 GetRandomPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 randomDir = Random.insideUnitSphere * spawnRadius;
            randomDir.y = 0f;
            Vector3 randomPoint = transform.position + randomDir;

            if (Physics.Raycast(randomPoint + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            {
                float height = Random.Range(minHeight, maxHeight);
                return hit.point + Vector3.up * height;
            }
        }

        return Vector3.zero;
    }
}
