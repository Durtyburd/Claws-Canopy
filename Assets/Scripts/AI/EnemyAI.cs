using UnityEngine;
using UnityEngine.AI;

public enum DinoMode { Aggressive, Passive }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private DinoMode mode = DinoMode.Aggressive;
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private float fleeDistance = 20f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float wanderWaitMin = 2f;
    [SerializeField] private float wanderWaitMax = 6f;
    [SerializeField] private string walkParam = "isWalking";

    [Header("Health")]
    public float MaxHealth = 100f;
    [HideInInspector] public float CurrentHealth;
    private bool _isDead;

    [Header("Pursuit")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float detectionRange = 30f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDamage = 20f;
    private float _attackTimer;

    [Header("Harvest")]
    [SerializeField] private int harvestReward = 50;
    [SerializeField] private float despawnTime = 30f;

    [Header("Animator Parameters")]
    [SerializeField] private string idleParam = "isWalking";
    [SerializeField] private string runParam = "isRunning";
    [SerializeField] private string attackParam = "isAttacking";
    [SerializeField] private string deathParam = "isDead";

    [Header("Sounds")]
    [SerializeField] private AudioClip movementSound;
    [SerializeField] private float soundVolume = 0.5f;
    [SerializeField] private float maxHearingDistance = 20f;

    [Header("References")]
    [SerializeField] private Animator anim;

    private NavMeshAgent _agent;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private AudioSource _audioSource;

    private Vector3 _spawnPosition;
    private float _wanderTimer;
    private bool _isWandering;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = chaseSpeed;
        _spawnPosition = transform.position;
        _wanderTimer = Random.Range(0f, wanderWaitMax);

        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = movementSound;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
        _audioSource.maxDistance = maxHearingDistance;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.volume = soundVolume;
    }

    private void Update()
    {
        if (_player == null || _isDead) return;

        if (mode == DinoMode.Aggressive)
            AggressiveUpdate();
        else
            PassiveUpdate();

        _attackTimer -= Time.deltaTime;
        UpdateAnimator();
    }

    private void AggressiveUpdate()
    {
        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= attackRange)
        {
            _isWandering = false;
            _agent.isStopped = true;
            _agent.speed = chaseSpeed;
            FaceTarget();
            TryAttack();
        }
        else if (distance <= detectionRange)
        {
            _isWandering = false;
            _agent.isStopped = false;
            _agent.speed = chaseSpeed;
            _agent.SetDestination(_player.position);
        }
        else
        {
            Wander();
        }
    }

    private void PassiveUpdate()
    {
        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= detectionRange)
        {
            _isWandering = false;
            _agent.isStopped = false;
            _agent.speed = fleeSpeed;

            Vector3 fleeDir = (transform.position - _player.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * fleeDistance;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
        else
        {
            Wander();
        }
    }

    private void Wander()
    {
        _agent.speed = wanderSpeed;

        // If arrived at wander destination or not wandering yet
        if (!_isWandering || (!_agent.pathPending && _agent.remainingDistance <= 0.5f))
        {
            _agent.isStopped = true;
            _isWandering = false;

            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f)
            {
                Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
                randomDir.y = 0f;
                Vector3 wanderTarget = _spawnPosition + randomDir;

                if (NavMesh.SamplePosition(wanderTarget, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                    _isWandering = true;
                }

                _wanderTimer = Random.Range(wanderWaitMin, wanderWaitMax);
            }
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
    }

    private void TryAttack()
    {
        if (_attackTimer <= 0f)
        {
            if (anim != null)
                anim.SetBool(attackParam, true);

            if (_playerHealth != null)
                _playerHealth.TakeDamage(attackDamage);

            _attackTimer = attackCooldown;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0f);

        if (CurrentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _isDead = true;
        _agent.isStopped = true;
        _agent.enabled = false;

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();

        if (anim != null)
        {
            anim.SetBool(runParam, false);
            anim.SetBool(attackParam, false);
            anim.SetBool(deathParam, true);
        }

        // Add harvestable so player can loot
        var harvestable = gameObject.AddComponent<Harvestable>();
        harvestable.SetReward(harvestReward);

        // Despawn if not harvested
        Destroy(gameObject, despawnTime);
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        bool isMoving = _agent.velocity.magnitude > 0.1f;
        bool isAttacking = _attackTimer > attackCooldown - 0.5f;

        anim.SetBool(walkParam, isMoving && _isWandering && !isAttacking);
        anim.SetBool(runParam, isMoving && !_isWandering && !isAttacking);
        anim.SetBool(attackParam, isAttacking);

        // Play/stop movement sound
        if (_audioSource != null && movementSound != null)
        {
            if (isMoving && !_audioSource.isPlaying)
                _audioSource.Play();
            else if (!isMoving && _audioSource.isPlaying)
                _audioSource.Stop();
        }
    }
}
