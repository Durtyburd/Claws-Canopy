using UnityEngine;
using UnityEngine.AI;

public class DinoSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject[] dinoPrefabs;
    [SerializeField] private int spawnCount = 5;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private float minDistanceFromPlayer = 15f;

    private Transform _player;

    private void Start()
    {
        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
            _player = playerObj.transform;

        SpawnDinos();
    }

    private void SpawnDinos()
    {
        if (dinoPrefabs == null || dinoPrefabs.Length == 0)
        {
            Debug.LogWarning("DinoSpawner: No prefabs assigned!");
            return;
        }

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomNavMeshPosition();
            if (spawnPos == Vector3.zero) continue;

            GameObject prefab = dinoPrefabs[Random.Range(0, dinoPrefabs.Length)];
            Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            spawned++;
        }

        Debug.Log($"DinoSpawner: Spawned {spawned}/{spawnCount} dinos");
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
}
