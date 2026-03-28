using UnityEngine;

public class DinoNest : MonoBehaviour
{
    [Header("Nest")]
    [SerializeField] private int eggCount = 3;
    [SerializeField] private float eggSpread = 0.5f;
    [SerializeField] private GameObject eggPrefab;

    [Header("Harvest")]
    [SerializeField] private int eggReward = 30;
    [SerializeField] private float eggInteractRange = 3f;

    private PachyAI _owner;

    public Vector3 Position => transform.position;

    public void SetOwner(PachyAI owner)
    {
        _owner = owner;
    }

    /// <summary>Called by DinoSpawner to pass in prefab references before SpawnEggs.</summary>
    public void SetEggPrefab(GameObject prefab, int count)
    {
        if (prefab != null) eggPrefab = prefab;
        if (count > 0) eggCount = count;
    }

    /// <summary>Called by PachyAI when it detects player near the nest.</summary>
    public bool IsPlayerNearNest(Transform player, float radius)
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= radius;
    }

    /// <summary>Spawn visual eggs around the nest. If no egg prefab, creates simple spheres.</summary>
    public void SpawnEggs()
    {
        for (int i = 0; i < eggCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * eggSpread;
            offset.y = 0f;
            Vector3 eggPos = transform.position + offset;

            GameObject egg;
            if (eggPrefab != null)
            {
                egg = Instantiate(eggPrefab, eggPos, Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f)));
            }
            else
            {
                egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                egg.transform.position = eggPos + Vector3.up * 0.1f;
                egg.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f);
                egg.name = "DinoEgg";

                var renderer = egg.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.9f, 0.85f, 0.7f);
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            egg.transform.SetParent(transform);

            // Make egg harvestable
            var harvestable = egg.AddComponent<Harvestable>();
            harvestable.SetReward(eggReward);
        }
    }
}
