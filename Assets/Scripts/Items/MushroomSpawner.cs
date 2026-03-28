using UnityEngine;

public class MushroomSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Just visual mushroom models — no components needed on them")]
    [SerializeField] private GameObject[] mushroomPrefabs;
    [SerializeField] private int spawnCount = 20;
    [SerializeField] private float spawnRadius = 40f;

    [Header("Item")]
    [Tooltip("The MushroomItem ScriptableObject — handles icon, healing, stacking")]
    [SerializeField] private ItemData mushroomItemData;

    [Header("Pickup")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float colliderRadius = 0.5f;

    private void Start()
    {
        SpawnMushrooms();
    }

    private void SpawnMushrooms()
    {
        if (mushroomPrefabs == null || mushroomPrefabs.Length == 0)
        {
            Debug.LogWarning("MushroomSpawner: No prefabs assigned!");
            return;
        }

        if (mushroomItemData == null)
            Debug.LogWarning("MushroomSpawner: No ItemData assigned! Mushrooms won't be pickable.");

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomPosition();
            if (spawnPos == Vector3.zero) continue;

            GameObject prefab = mushroomPrefabs[Random.Range(0, mushroomPrefabs.Length)];
            GameObject mushroom = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            // Add Collectible if missing, or configure the existing one
            var collectible = mushroom.GetComponent<Collectible>();
            if (collectible == null)
                collectible = mushroom.AddComponent<Collectible>();
            if (mushroomItemData != null)
                collectible.SetItemData(mushroomItemData);
            collectible.SetInteractRange(interactRange);

            // Ensure collider exists
            if (mushroom.GetComponent<Collider>() == null && mushroom.GetComponentInChildren<Collider>() == null)
            {
                var col = mushroom.AddComponent<SphereCollider>();
                col.radius = colliderRadius;
                col.isTrigger = true;
            }
            else
            {
                // Make sure existing collider is a trigger
                var col = mushroom.GetComponent<Collider>();
                if (col == null) col = mushroom.GetComponentInChildren<Collider>();
                if (col != null) col.isTrigger = true;
            }

            spawned++;
        }

        Debug.Log($"MushroomSpawner: Spawned {spawned}/{spawnCount} mushrooms");
    }

    private Vector3 GetRandomPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 randomDir = Random.insideUnitSphere * spawnRadius;
            randomDir.y = 0f;
            Vector3 randomPoint = transform.position + randomDir;

            if (Physics.Raycast(randomPoint + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                return hit.point;
        }

        return Vector3.zero;
    }
}
