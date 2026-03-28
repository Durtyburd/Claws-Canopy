using UnityEngine;
using UnityEngine.AI;

public enum PachyState { Grazing, Alert, Defending, Attacking, Fleeing, ReturningToNest, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class PachyAI : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 120f;
    [HideInInspector] public float _currentHealth;

    [Header("Nest Defense")]
    [SerializeField] private float nestDefenseRadius = 15f;
    [SerializeField] private float nestAlertRadius = 25f;
    [SerializeField] private float maxChaseDistance = 30f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private float fleeDetectionRange = 20f;

    [Header("Speed")]
    [SerializeField] private float grazeSpeed = 1.5f;
    [SerializeField] private float chargeSpeed = 7f;
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private float returnSpeed = 3f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float knockbackForce = 8f;

    [Header("Alert")]
    [SerializeField] private float alertDuration = 1.5f;

    [Header("Graze")]
    [SerializeField] private float grazeRadius = 10f;
    [SerializeField] private float grazeWaitMin = 3f;
    [SerializeField] private float grazeWaitMax = 8f;

    [Header("Flee")]
    [SerializeField] private float fleeDistance = 20f;

    [Header("Harvest")]
    [SerializeField] private int harvestReward = 60;
    [SerializeField] private float despawnTime = 30f;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParam = "isWalking";
    [SerializeField] private string runParam = "isRunning";
    [SerializeField] private string attackParam = "isAttacking";
    [SerializeField] private string deathParam = "isDead";

    [Header("Sounds")]
    [SerializeField] private AudioClip movementSound;
    [SerializeField] private float soundVolume = 0.5f;
    [SerializeField] private float maxHearingDistance = 20f;

    [Header("References")]
    [SerializeField] private Animator anim;

    // IDamageable
    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;

    // State
    private PachyState _state = PachyState.Grazing;
    private NavMeshAgent _agent;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private PlayerVisibility _visibility;
    private StarterAssets.FirstPersonController _playerController;
    private AudioSource _audioSource;
    private DinoNest _nest;

    private float _stateTimer;
    private float _attackTimer;
    private float _grazeTimer;
    private bool _isGrazing;

    public PachyState State => _state;

    public void SetNest(DinoNest nest)
    {
        _nest = nest;
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = grazeSpeed;
        _grazeTimer = Random.Range(0f, grazeWaitMax);

        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
            _playerController = playerObj;
            _visibility = playerObj.GetComponent<PlayerVisibility>();
        }
        else
        {
            Debug.LogWarning($"PachyAI ({name}): No FirstPersonController found in scene.");
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
        if (_state == PachyState.Dead || _player == null) return;

        switch (_state)
        {
            case PachyState.Grazing:
                GrazingUpdate();
                break;
            case PachyState.Alert:
                AlertUpdate();
                break;
            case PachyState.Defending:
                DefendingUpdate();
                break;
            case PachyState.Attacking:
                AttackingUpdate();
                break;
            case PachyState.Fleeing:
                FleeingUpdate();
                break;
            case PachyState.ReturningToNest:
                ReturningUpdate();
                break;
        }

        _attackTimer -= Time.deltaTime;
        UpdateAnimator();
    }

    // --- States ---

    private void GrazingUpdate()
    {
        _agent.speed = grazeSpeed;

        // Check if player is near the nest — switch to defense
        if (_nest != null && _nest.IsPlayerNearNest(_player, nestAlertRadius))
        {
            bool detected = DetectionUtils.CanDetectPlayer(transform.position, detectionRange, _visibility, _player);
            if (detected)
            {
                _state = PachyState.Alert;
                _stateTimer = alertDuration;
                _agent.isStopped = true;
                return;
            }
        }

        // Away from nest — flee if player gets close
        if (_nest == null || !IsNearNest())
        {
            bool detected = DetectionUtils.CanDetectPlayer(transform.position, fleeDetectionRange, _visibility, _player);
            if (detected)
            {
                _state = PachyState.Fleeing;
                return;
            }
        }

        // Graze near nest
        Graze();
    }

    private void AlertUpdate()
    {
        // Stare down the player — warning before charge
        FaceTarget(_player.position);
        _agent.isStopped = true;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            // Is the player still near the nest?
            if (_nest != null && _nest.IsPlayerNearNest(_player, nestDefenseRadius))
            {
                _state = PachyState.Defending;
            }
            else if (_nest != null && _nest.IsPlayerNearNest(_player, nestAlertRadius))
            {
                // Still in alert zone but not defense zone — keep watching
                _stateTimer = alertDuration * 0.5f;
            }
            else
            {
                // Player backed off
                _state = PachyState.Grazing;
            }
        }
    }

    private void DefendingUpdate()
    {
        float distToPlayer = Vector3.Distance(transform.position, _player.position);

        if (distToPlayer <= attackRange)
        {
            _agent.isStopped = true;
            FaceTarget(_player.position);
            TryAttack();
            return;
        }

        // Chase player, but don't go too far from nest
        float distToNest = _nest != null ? Vector3.Distance(transform.position, _nest.Position) : 0f;
        if (distToNest > maxChaseDistance)
        {
            // Too far from nest — return
            _state = PachyState.ReturningToNest;
            return;
        }

        // Player left the defense zone — return to nest
        if (_nest != null && !_nest.IsPlayerNearNest(_player, nestDefenseRadius + 5f))
        {
            _state = PachyState.ReturningToNest;
            return;
        }

        // Charge at player
        _agent.isStopped = false;
        _agent.speed = chargeSpeed;
        _agent.SetDestination(_player.position);
        FaceTarget(_player.position);
    }

    private void AttackingUpdate()
    {
        _agent.isStopped = true;
        FaceTarget(_player.position);

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            _state = PachyState.Defending;
    }

    private void FleeingUpdate()
    {
        _agent.speed = fleeSpeed;
        _agent.isStopped = false;

        Vector3 fleeDir = (transform.position - _player.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * fleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        // If we're far enough, go back to grazing
        float distToPlayer = Vector3.Distance(transform.position, _player.position);
        if (distToPlayer > fleeDetectionRange * 1.5f)
        {
            // Head back to nest if we have one
            if (_nest != null)
                _state = PachyState.ReturningToNest;
            else
                _state = PachyState.Grazing;
        }
    }

    private void ReturningUpdate()
    {
        if (_nest == null)
        {
            _state = PachyState.Grazing;
            return;
        }

        _agent.speed = returnSpeed;
        _agent.isStopped = false;
        _agent.SetDestination(_nest.Position);

        // Check if player came back into nest zone while returning
        if (_nest.IsPlayerNearNest(_player, nestDefenseRadius))
        {
            bool detected = DetectionUtils.CanDetectPlayer(transform.position, detectionRange, _visibility, _player);
            if (detected)
            {
                _state = PachyState.Defending;
                return;
            }
        }

        // Arrived back at nest
        if (!_agent.pathPending && _agent.remainingDistance <= 2f)
            _state = PachyState.Grazing;
    }

    // --- Actions ---

    private void Graze()
    {
        Vector3 grazeCenter = _nest != null ? _nest.Position : transform.position;

        if (!_isGrazing || (!_agent.pathPending && _agent.remainingDistance <= 0.5f))
        {
            _agent.isStopped = true;
            _isGrazing = false;

            _grazeTimer -= Time.deltaTime;
            if (_grazeTimer <= 0f)
            {
                Vector3 randomDir = Random.insideUnitSphere * grazeRadius;
                randomDir.y = 0f;

                if (NavMesh.SamplePosition(grazeCenter + randomDir, out NavMeshHit hit, grazeRadius, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                    _isGrazing = true;
                }

                _grazeTimer = Random.Range(grazeWaitMin, grazeWaitMax);
            }
        }
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f) return;

        if (anim != null)
            anim.SetBool(attackParam, true);

        // Headbutt with knockback
        if (_playerHealth != null)
            _playerHealth.TakeDamage(attackDamage);

        if (_playerController != null)
        {
            Vector3 knockDir = (_player.position - transform.position).normalized;
            knockDir.y = 0.2f;
            _playerController.ApplyKnockback(knockDir * knockbackForce);
        }

        _attackTimer = attackCooldown;
        _state = PachyState.Attacking;
        _stateTimer = 0.5f;
    }

    // --- Damage ---

    public void TakeDamage(float damage)
    {
        if (_state == PachyState.Dead) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        // Getting shot always triggers defense mode if near nest
        if (_nest != null && IsNearNest())
        {
            if (_state == PachyState.Grazing || _state == PachyState.Alert || _state == PachyState.Fleeing)
                _state = PachyState.Defending;
        }
        else
        {
            // No nest nearby — flee
            if (_state != PachyState.Defending && _state != PachyState.Attacking)
                _state = PachyState.Fleeing;
        }

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _state = PachyState.Dead;
        _agent.isStopped = true;
        _agent.enabled = false;

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();

        if (anim != null)
        {
            anim.SetBool(walkParam, false);
            anim.SetBool(runParam, false);
            anim.SetBool(attackParam, false);
            anim.SetBool(deathParam, true);
        }

        var harvestable = gameObject.AddComponent<Harvestable>();
        harvestable.SetReward(harvestReward);

        Destroy(gameObject, despawnTime);
    }

    // --- Helpers ---

    private bool IsNearNest()
    {
        if (_nest == null) return false;
        return Vector3.Distance(transform.position, _nest.Position) <= maxChaseDistance;
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        bool isMoving = _agent.enabled && _agent.velocity.magnitude > 0.1f;
        bool isCharging = _state == PachyState.Defending || _state == PachyState.Fleeing;
        bool isAttacking = _state == PachyState.Attacking;

        anim.SetBool(walkParam, isMoving && !isCharging && !isAttacking);
        anim.SetBool(runParam, isMoving && isCharging);
        anim.SetBool(attackParam, isAttacking);

        if (_audioSource != null && movementSound != null)
        {
            if (isMoving && !_audioSource.isPlaying)
                _audioSource.Play();
            else if (!isMoving && _audioSource.isPlaying)
                _audioSource.Stop();
        }
    }
}
