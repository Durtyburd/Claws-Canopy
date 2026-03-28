using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RaptorPack : MonoBehaviour
{
    [Header("Pack Setup")]
    [SerializeField] private GameObject raptorPrefab;
    [SerializeField] private int packSize = 3;
    [SerializeField] private float spawnSpread = 5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 35f;
    [SerializeField] private float disengageRange = 55f;
    [SerializeField] private float visibilityMultiplier = 0.8f;

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 18f;
    [SerializeField] private float orbitRadiusVariance = 3f;
    [SerializeField] private float orbitDrift = 8f;

    [Header("Flanking")]
    [SerializeField] private float flankArc = 270f;

    [Header("Strike Coordination")]
    [SerializeField] private float strikeCooldown = 5f;
    [SerializeField] private float firstStrikeDelay = 3f;
    [SerializeField] private float aggressionRampPerDeath = 0.7f;
    [SerializeField] private float doubleStrikeChance = 0.25f;

    [Header("Pack Calls")]
    [SerializeField] private float callRange = 60f;

    // Static registry of all active packs for inter-pack communication
    private static readonly List<RaptorPack> AllPacks = new List<RaptorPack>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AllPacks.Clear();

    private readonly List<RaptorAI> _members = new List<RaptorAI>();
    private Transform _player;
    private PlayerHealth _playerHealth;
    private PlayerVisibility _visibility;
    private Camera _cam;
    private bool _hunting;
    private float _baseAngleDrift;
    private float _strikeTimer;
    private int _nextStrikerIndex;
    private float _currentStrikeCooldown;
    private int _distractorIndex = -1;

    public bool IsHunting => _hunting;
    public Transform Player => _player;
    public PlayerHealth PlayerHealth => _playerHealth;
    public float OrbitRadius => orbitRadius;
    public float DetectionRange => detectionRange;

    /// <summary>Called by DinoSpawner to configure before Start runs.</summary>
    public void SetSpawnSettings(GameObject prefab, int size)
    {
        raptorPrefab = prefab;
        packSize = size;
    }

    public bool IsDistractor(int memberIndex)
    {
        return memberIndex == _distractorIndex;
    }

    private void Start()
    {
        AllPacks.Add(this);

        _cam = Camera.main;

        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
            _visibility = playerObj.GetComponent<PlayerVisibility>();
        }
        else
        {
            Debug.LogWarning("RaptorPack: No FirstPersonController found in scene.");
            return;
        }

        _currentStrikeCooldown = strikeCooldown;
        _baseAngleDrift = Random.Range(0f, 360f);

        SpawnPack();
    }

    private void OnDestroy()
    {
        AllPacks.Remove(this);
    }

    private void SpawnPack()
    {
        if (raptorPrefab == null)
        {
            Debug.LogWarning("RaptorPack: No raptor prefab assigned!");
            return;
        }

        for (int i = 0; i < packSize; i++)
        {
            Vector3 offset = Random.insideUnitSphere * spawnSpread;
            offset.y = 0f;
            Vector3 spawnPos = transform.position + offset;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, spawnSpread * 2f, NavMesh.AllAreas))
            {
                GameObject obj = Instantiate(raptorPrefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                var raptor = obj.GetComponent<RaptorAI>();
                if (raptor == null)
                    raptor = obj.AddComponent<RaptorAI>();

                raptor.InitPack(this, i);
                _members.Add(raptor);
            }
        }

        Debug.Log($"RaptorPack: Spawned {_members.Count}/{packSize} raptors");
    }

    private void Update()
    {
        if (_player == null) return;

        CleanDeadMembers();
        if (_members.Count == 0) return;

        if (_hunting)
        {
            Transform closestMember = GetClosestMember();
            Vector3 closestPos = closestMember != null ? closestMember.position : transform.position;
            float distToPlayer = Vector3.Distance(closestPos, _player.position);

            if (distToPlayer > disengageRange)
            {
                Disengage();
                return;
            }

            // Slow organic drift added on top of flanking angles
            _baseAngleDrift += orbitDrift * Time.deltaTime;
            if (_baseAngleDrift > 360f) _baseAngleDrift -= 360f;

            _strikeTimer -= Time.deltaTime;
            if (_strikeTimer <= 0f)
                OrderStrike();
        }
        else
        {
            if (AnyMemberCanDetectPlayer())
                EngageHunt();
        }
    }

    private void EngageHunt()
    {
        _hunting = true;
        _strikeTimer = firstStrikeDelay;

        // Pick a distractor — one raptor that will taunt from the front
        _distractorIndex = Random.Range(0, _members.Count);

        for (int i = 0; i < _members.Count; i++)
            _members[i].OnPackHuntStart();

        AlertNearbyPacks();
    }

    private void Disengage()
    {
        _hunting = false;
        _distractorIndex = -1;

        for (int i = 0; i < _members.Count; i++)
            _members[i].OnPackDisengage();
    }

    private void OrderStrike()
    {
        if (_members.Count == 0) return;

        // Find first available striker — prefer non-distractors
        int firstStriker = -1;
        for (int attempt = 0; attempt < _members.Count; attempt++)
        {
            _nextStrikerIndex = (_nextStrikerIndex + 1) % _members.Count;
            if (_members[_nextStrikerIndex].CanStrike() && _nextStrikerIndex != _distractorIndex)
            {
                firstStriker = _nextStrikerIndex;
                break;
            }
        }

        // Fallback: let distractor strike if it's the only one available
        if (firstStriker == -1)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].CanStrike())
                {
                    firstStriker = i;
                    break;
                }
            }
        }

        if (firstStriker == -1)
        {
            _strikeTimer = 0.5f;
            return;
        }

        _members[firstStriker].OnOrderedStrike();

        // Chance for a synchronized double strike from the opposite side
        if (_members.Count >= 2 && Random.value < doubleStrikeChance)
        {
            int secondStriker = FindOppositeStriker(firstStriker);
            if (secondStriker != -1)
                _members[secondStriker].OnOrderedStrike();
        }

        _strikeTimer = _currentStrikeCooldown;
    }

    private int FindOppositeStriker(int firstIndex)
    {
        if (_player == null) return -1;

        Vector3 firstDir = (_members[firstIndex].transform.position - _player.position).normalized;
        float bestDot = 0f; // looking for most negative dot (opposite direction)
        int bestIndex = -1;

        for (int i = 0; i < _members.Count; i++)
        {
            if (i == firstIndex || !_members[i].CanStrike()) continue;

            Vector3 candidateDir = (_members[i].transform.position - _player.position).normalized;
            float dot = Vector3.Dot(firstDir, candidateDir);

            // More negative = more opposite
            if (dot < bestDot || bestIndex == -1)
            {
                bestDot = dot;
                bestIndex = i;
            }
        }

        // Only accept if reasonably opposite (dot < -0.3 ≈ > 107 degrees apart)
        return bestDot < -0.3f ? bestIndex : -1;
    }

    /// <summary>
    /// Get the orbit position for a pack member.
    /// Positions are biased toward the player's flanks and rear.
    /// The distractor gets a position in front of the player instead.
    /// </summary>
    public Vector3 GetOrbitPosition(int memberIndex)
    {
        if (_player == null || _members.Count == 0) return transform.position;

        // Get player's facing direction (flattened)
        Vector3 playerForward = _cam != null ? _cam.transform.forward : _player.forward;
        playerForward.y = 0f;
        playerForward.Normalize();
        float playerAngle = Mathf.Atan2(playerForward.z, playerForward.x) * Mathf.Rad2Deg;

        float angle;
        float radius;

        if (memberIndex == _distractorIndex)
        {
            // Distractor: position in front of the player, slightly closer
            angle = playerAngle;
            radius = orbitRadius * 0.65f;
        }
        else
        {
            // Flankers: distribute across a wide arc centered on the player's back
            float rearAngle = playerAngle + 180f;
            float halfArc = flankArc / 2f;

            // Count non-distractor members for spacing
            int flankerCount = _distractorIndex >= 0 ? _members.Count - 1 : _members.Count;
            int flankerIndex = memberIndex > _distractorIndex && _distractorIndex >= 0 ? memberIndex - 1 : memberIndex;

            float step = flankerCount > 1 ? flankArc / (flankerCount - 1) : 0f;
            angle = rearAngle - halfArc + step * flankerIndex;

            // Add slow organic drift so they don't feel robotic
            angle += Mathf.Sin(_baseAngleDrift * Mathf.Deg2Rad + memberIndex * 2f) * 15f;

            // Per-raptor radius variation
            float radiusNoise = Mathf.PerlinNoise(memberIndex * 100f, Time.time * 0.3f) * orbitRadiusVariance * 2f - orbitRadiusVariance;
            radius = orbitRadius + radiusNoise;
        }

        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
        Vector3 target = _player.position + offset;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return hit.position;

        return target;
    }

    public void OnMemberDied(RaptorAI raptor)
    {
        _currentStrikeCooldown = Mathf.Max(1.5f, _currentStrikeCooldown * aggressionRampPerDeath);

        // If distractor died, reassign
        int deadIndex = _members.IndexOf(raptor);
        if (deadIndex == _distractorIndex)
        {
            _distractorIndex = -1;
            // Pick a new distractor from surviving circling members
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i] != null && _members[i] != raptor && _members[i].State != RaptorState.Dead)
                {
                    _distractorIndex = i;
                    break;
                }
            }
        }
    }

    private void AlertNearbyPacks()
    {
        for (int i = 0; i < AllPacks.Count; i++)
        {
            var pack = AllPacks[i];
            if (pack == this || pack._hunting) continue;

            float dist = Vector3.Distance(GetPackCenter(), pack.GetPackCenter());
            if (dist <= callRange)
                pack.OnAlertedByPack();
        }
    }

    private void OnAlertedByPack()
    {
        if (!_hunting && _player != null)
            EngageHunt();
    }

    private void CleanDeadMembers()
    {
        for (int i = _members.Count - 1; i >= 0; i--)
        {
            if (_members[i] == null)
                _members.RemoveAt(i);
        }

        // Fix distractor index after removal
        if (_distractorIndex >= _members.Count)
            _distractorIndex = _members.Count > 0 ? 0 : -1;
    }

    private bool AnyMemberCanDetectPlayer()
    {
        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i] == null) continue;
            bool detected = DetectionUtils.CanDetectPlayer(
                _members[i].transform.position, detectionRange,
                _visibility, _player, visibilityMultiplier);
            if (detected) return true;
        }
        return false;
    }

    private Transform GetClosestMember()
    {
        if (_player == null || _members.Count == 0) return null;

        float closestDist = float.MaxValue;
        Transform closest = null;

        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i] == null) continue;
            float dist = Vector3.Distance(_members[i].transform.position, _player.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = _members[i].transform;
            }
        }

        return closest;
    }

    private Vector3 GetPackCenter()
    {
        if (_members.Count == 0) return transform.position;

        Vector3 sum = Vector3.zero;
        int alive = 0;
        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i] != null)
            {
                sum += _members[i].transform.position;
                alive++;
            }
        }

        return alive > 0 ? sum / alive : transform.position;
    }
}
