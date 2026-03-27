using UnityEngine;

public class MushroomSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject[] mushroomPrefabs;
    [SerializeField] private int spawnCount = 20;
    [SerializeField] private float spawnRadius = 40f;

    [Header("Healing")]
    [SerializeField] private float healAmount = 25f;

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

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomPosition();
            if (spawnPos == Vector3.zero) continue;

            GameObject prefab = mushroomPrefabs[Random.Range(0, mushroomPrefabs.Length)];
            GameObject mushroom = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            // Add collectible if it doesn't already have one
            if (mushroom.GetComponent<Collectible>() == null)
            {
                var collectible = mushroom.AddComponent<Collectible>();
            }

            // Add a collider if it doesn't have one
            if (mushroom.GetComponent<Collider>() == null && mushroom.GetComponentInChildren<Collider>() == null)
            {
                var col = mushroom.AddComponent<SphereCollider>();
                col.radius = 0.5f;
                col.isTrigger = true;
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

            // Raycast down to find the terrain surface
            if (Physics.Raycast(randomPoint + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            {
                return hit.point;
            }
        }

        return Vector3.zero;
    }
}
