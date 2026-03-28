using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class DinoSpawnEntry
{
    public GameObject prefab;
    public int count = 1;
}

public class DinoSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private DinoSpawnEntry[] dinoEntries;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private float minDistanceFromPlayer = 15f;

    [Header("Pack Settings (auto-applied to RaptorAI prefabs)")]
    [SerializeField] private int packSize = 3;
    [SerializeField] private float minDistanceBetweenPacks = 25f;

    [Header("Nest Settings (auto-applied to PachyAI prefabs)")]
    [SerializeField] private GameObject nestPrefab;
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private int eggsPerNest = 3;

    private Transform _player;
    private readonly List<Vector3> _packPositions = new List<Vector3>();

    private void Start()
    {
        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("DinoSpawner: No FirstPersonController found in scene. Spawning without player distance check.");

        SpawnDinos();
    }

    private void SpawnDinos()
    {
        if (dinoEntries == null || dinoEntries.Length == 0)
        {
            Debug.LogWarning("DinoSpawner: No entries assigned!");
            return;
        }

        if (!NavMesh.SamplePosition(transform.position, out _, spawnRadius, NavMesh.AllAreas))
        {
            Debug.LogError("DinoSpawner: No NavMesh found! Bake a NavMeshSurface before playing. No dinos will spawn.");
            return;
        }

        int totalRequested = 0;
        int totalSpawned = 0;

        for (int e = 0; e < dinoEntries.Length; e++)
        {
            var entry = dinoEntries[e];
            if (entry.prefab == null)
            {
                Debug.LogWarning($"DinoSpawner: Entry {e} has no prefab assigned, skipping.");
                continue;
            }

            bool isPack = entry.prefab.GetComponent<RaptorAI>() != null;
            bool isNester = entry.prefab.GetComponent<PachyAI>() != null;
            totalRequested += entry.count;

            for (int i = 0; i < entry.count; i++)
            {
                Vector3 spawnPos = isPack ? GetPackSpawnPosition() : GetRandomNavMeshPosition();
                if (spawnPos == Vector3.zero) continue;

                if (isPack)
                {
                    SpawnPackAt(entry.prefab, spawnPos);
                    _packPositions.Add(spawnPos);
                }
                else if (isNester)
                {
                    SpawnWithNest(entry.prefab, spawnPos);
                }
                else
                {
                    Instantiate(entry.prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                }

                totalSpawned++;
            }
        }

        if (totalSpawned < totalRequested)
            Debug.LogWarning($"DinoSpawner: Only spawned {totalSpawned}/{totalRequested} entries. NavMesh may be too small or player too close.");
        else
            Debug.Log($"DinoSpawner: Spawned {totalSpawned}/{totalRequested} entries");
    }

    private void SpawnPackAt(GameObject raptorPrefab, Vector3 position)
    {
        GameObject packObj = new GameObject($"RaptorPack_{_packPositions.Count + 1}");
        packObj.transform.position = position;

        RaptorPack pack = packObj.AddComponent<RaptorPack>();
        pack.SetSpawnSettings(raptorPrefab, packSize);
    }

    private void SpawnWithNest(GameObject pachyPrefab, Vector3 position)
    {
        // Create the nest at the spawn point
        GameObject nestObj;
        if (nestPrefab != null)
        {
            nestObj = Instantiate(nestPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            nestObj.name = $"DinoNest_{_packPositions.Count + 1}";
        }
        else
        {
            nestObj = new GameObject($"DinoNest_{_packPositions.Count + 1}");
            nestObj.transform.position = position;
        }

        DinoNest nest = nestObj.GetComponent<DinoNest>();
        if (nest == null)
            nest = nestObj.AddComponent<DinoNest>();

        nest.SetEggPrefab(eggPrefab, eggsPerNest);
        nest.SpawnEggs();

        // Spawn the pachy nearby
        Vector3 pachyOffset = Random.insideUnitSphere * 3f;
        pachyOffset.y = 0f;
        Vector3 pachyPos = position + pachyOffset;

        if (NavMesh.SamplePosition(pachyPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            pachyPos = hit.position;

        GameObject pachyObj = Instantiate(pachyPrefab, pachyPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        var pachyAI = pachyObj.GetComponent<PachyAI>();
        if (pachyAI != null)
        {
            pachyAI.SetNest(nest);
            nest.SetOwner(pachyAI);
        }
    }

    private Vector3 GetRandomNavMeshPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 randomDir = Random.insideUnitSphere * spawnRadius;
            randomDir.y = 0f;
            Vector3 randomPoint = transform.position + randomDir;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                if (_player == null || Vector3.Distance(hit.position, _player.position) >= minDistanceFromPlayer)
                    return hit.position;
            }
        }

        return Vector3.zero;
    }

    private Vector3 GetPackSpawnPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 candidate = GetRandomNavMeshPosition();
            if (candidate == Vector3.zero) continue;

            bool tooCloseToOtherPack = false;
            for (int j = 0; j < _packPositions.Count; j++)
            {
                if (Vector3.Distance(candidate, _packPositions[j]) < minDistanceBetweenPacks)
                {
                    tooCloseToOtherPack = true;
                    break;
                }
            }

            if (!tooCloseToOtherPack)
                return candidate;
        }

        return Vector3.zero;
    }
}
