using UnityEngine;
using UnityEngine.AI;

public enum TRexState { Patrolling, Alert, Roaring, Charging, Attacking, Cooldown, Enraged, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class TRexAI : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 500f;
    [HideInInspector] public float _currentHealth;

    [Header("Territory")]
    [SerializeField] private float territoryRadius = 40f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolWaitMin = 3f;
    [SerializeField] private float patrolWaitMax = 8f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 40f;
    [SerializeField] private float disengageRange = 60f;
    [SerializeField] private float visibilityMultiplier = 1.3f;
    [SerializeField] private float movementDetectionRange = 25f;

    [Header("Alert Phase")]
    [SerializeField] private float alertDuration = 2f;

    [Header("Roar")]
    [SerializeField] private float roarDuration = 2.5f;
    [SerializeField] private float roarShakeMagnitude = 0.15f;
    [SerializeField] private float roarScareRadius = 30f;
    [SerializeField] private AudioClip roarSound;

    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float chargeAcceleration = 20f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float biteDamage = 35f;
    [SerializeField] private float stompDamage = 15f;
    [SerializeField] private float stompRadius = 6f;
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float knockbackForce = 15f;

    [Header("Enrage (below 50% HP)")]
    [SerializeField] private float enrageHealthThreshold = 0.5f;
    [SerializeField] private float enrageSpeedMultiplier = 1.4f;
    [SerializeField] private float enrageDamageMultiplier = 1.5f;
    [SerializeField] private float enrageCooldownMultiplier = 0.5f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.6f;
    [SerializeField] private float footstepShakeMagnitude = 0.03f;
    [SerializeField] private float footstepHearingRange = 50f;

    [Header("Harvest")]
    [SerializeField] private int harvestReward = 200;
    [SerializeField] private float despawnTime = 60f;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParam = "isWalking";
    [SerializeField] private string runParam = "isRunning";
    [SerializeField] private string attackParam = "isAttacking";
    [SerializeField] private string roarParam = "isRoaring";
    [SerializeField] private string deathParam = "isDead";

    [Header("References")]
    [SerializeField] private Animator anim;

    // IDamageable
    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;

    // State
    private TRexState _state = TRexState.Patrolling;
    private NavMeshAgent _agent;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private PlayerVisibility _visibility;
    private StarterAssets.FirstPersonController _playerController;
    private AudioSource _audioSource;

    private Vector3 _spawnPosition;
    private float _stateTimer;
    private float _attackTimer;
    private float _footstepTimer;
    private float _patrolWaitTimer;
    private bool _isEnraged;
    private bool _hasRoaredThisEncounter;

    public TRexState State => _state;

    private void Start()
    {
        _currentHealth = maxHealth;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = patrolSpeed;
        _agent.acceleration = 8f;
        _spawnPosition = transform.position;
        _patrolWaitTimer = Random.Range(0f, patrolWaitMax);

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
            Debug.LogWarning($"TRexAI ({name}): No FirstPersonController found in scene.");
        }

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.maxDistance = footstepHearingRange;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (_state == TRexState.Dead || _player == null) return;

        switch (_state)
        {
            case TRexState.Patrolling:
                PatrolUpdate();
                break;
            case TRexState.Alert:
                AlertUpdate();
                break;
            case TRexState.Roaring:
                RoarUpdate();
                break;
            case TRexState.Charging:
                ChargeUpdate();
                break;
            case TRexState.Attacking:
                AttackUpdate();
                break;
            case TRexState.Cooldown:
                CooldownUpdate();
                break;
        }

        _attackTimer -= Time.deltaTime;
        UpdateFootsteps();
        UpdateAnimator();
        CheckEnrage();
    }

    // --- State Updates ---

    private void PatrolUpdate()
    {
        _agent.speed = patrolSpeed;

        if (!_agent.pathPending && _agent.remainingDistance <= 0.5f)
        {
            _agent.isStopped = true;
            _patrolWaitTimer -= Time.deltaTime;

            if (_patrolWaitTimer <= 0f)
            {
                Vector3 randomDir = Random.insideUnitSphere * territoryRadius;
                randomDir.y = 0f;

                if (NavMesh.SamplePosition(_spawnPosition + randomDir, out NavMeshHit hit, territoryRadius, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                }

                _patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
            }
        }

        // Dual detection: sight (poor) + vibration (movement-based)
        bool seenPlayer = DetectionUtils.CanDetectPlayer(
            transform.position, detectionRange, _visibility, _player, visibilityMultiplier);

        bool feltPlayer = false;
        if (_visibility != null)
        {
            if (_visibility.CurrentSpeed > 1f)
            {
                float dist = Vector3.Distance(transform.position, _player.position);
                float movementFactor = Mathf.InverseLerp(1f, 6f, _visibility.CurrentSpeed);
                feltPlayer = dist <= movementDetectionRange * movementFactor;
            }
        }
        else if (_playerController != null)
        {
            // Fallback without PlayerVisibility: use raw speed from controller
            float speed = _playerController.CurrentSpeed;
            if (speed > 1f)
            {
                float dist = Vector3.Distance(transform.position, _player.position);
                float movementFactor = Mathf.InverseLerp(1f, 6f, speed);
                feltPlayer = dist <= movementDetectionRange * movementFactor;
            }
        }

        if (seenPlayer || feltPlayer)
        {
            _agent.isStopped = true;
            _state = TRexState.Alert;
            _stateTimer = alertDuration;
        }
    }

    private void AlertUpdate()
    {
        // Stare down the player — slow turn to face
        FaceTarget();
        _agent.isStopped = true;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            if (!_hasRoaredThisEncounter)
            {
                _state = TRexState.Roaring;
                _stateTimer = roarDuration;
                _hasRoaredThisEncounter = true;
                OnRoarStart();
            }
            else
            {
                // Already roared — go straight to charging
                BeginCharge();
            }
        }
    }

    private void RoarUpdate()
    {
        FaceTarget();
        _agent.isStopped = true;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            // Player still in range? Charge. Otherwise, back to patrol.
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= disengageRange)
                BeginCharge();
            else
                Disengage();
        }
    }

    private void ChargeUpdate()
    {
        float speed = chargeSpeed * (_isEnraged ? enrageSpeedMultiplier : 1f);
        _agent.speed = speed;
        _agent.acceleration = chargeAcceleration;
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= attackRange && _attackTimer <= 0f)
        {
            _state = TRexState.Attacking;
            _stateTimer = 0.5f; // brief attack window
            PerformAttack();
        }
        else if (dist > disengageRange)
        {
            Disengage();
        }
    }

    private void AttackUpdate()
    {
        _agent.isStopped = true;
        FaceTarget();

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            _state = TRexState.Cooldown;
            float cd = attackCooldown * (_isEnraged ? enrageCooldownMultiplier : 1f);
            _stateTimer = cd;
        }
    }

    private void CooldownUpdate()
    {
        // Slowly advance toward player during cooldown
        _agent.speed = patrolSpeed;
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
        FaceTarget();

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= disengageRange)
                BeginCharge();
            else
                Disengage();
        }
    }

    // --- Actions ---

    private void OnRoarStart()
    {
        if (anim != null)
            anim.SetBool(roarParam, true);

        // Roar sound
        if (roarSound != null && _audioSource != null)
        {
            _audioSource.clip = roarSound;
            _audioSource.volume = 1f;
            _audioSource.Play();
        }

        // Screen shake
        ScreenShake.Shake(roarDuration, roarShakeMagnitude);

        // Scare nearby dinos
        ScatterNearbyDinos();
    }

    private void ScatterNearbyDinos()
    {
        var allDinos = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        for (int i = 0; i < allDinos.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, allDinos[i].transform.position);
            if (dist <= roarScareRadius)
                allDinos[i].Frighten(transform.position, 6f);
        }
    }

    private void BeginCharge()
    {
        _state = TRexState.Charging;
        _agent.acceleration = chargeAcceleration;

        if (anim != null)
            anim.SetBool(roarParam, false);
    }

    private void PerformAttack()
    {
        float dmgMultiplier = _isEnraged ? enrageDamageMultiplier : 1f;
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= attackRange)
        {
            // Direct bite
            if (_playerHealth != null)
                _playerHealth.TakeDamage(biteDamage * dmgMultiplier);

            // Knockback
            if (_playerController != null)
            {
                Vector3 knockDir = (_player.position - transform.position).normalized;
                knockDir.y = 0.3f; // slight upward launch
                _playerController.ApplyKnockback(knockDir * knockbackForce);
            }
        }

        // Stomp AoE — damages even if the bite missed slightly
        if (dist <= stompRadius && dist > attackRange * 0.5f)
        {
            if (_playerHealth != null)
                _playerHealth.TakeDamage(stompDamage * dmgMultiplier);
        }

        // Ground shake from stomp
        ScreenShake.Shake(0.3f, 0.08f);

        float cd = attackCooldown * (_isEnraged ? enrageCooldownMultiplier : 1f);
        _attackTimer = cd;
    }

    private void Disengage()
    {
        _state = TRexState.Patrolling;
        _hasRoaredThisEncounter = false;
        _agent.acceleration = 8f;
        _agent.isStopped = true;
        _patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);

        if (anim != null)
            anim.SetBool(roarParam, false);
    }

    // --- Damage ---

    public void TakeDamage(float damage)
    {
        if (_state == TRexState.Dead) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        // Getting shot during patrol/alert — skip roar, charge immediately
        if (_state == TRexState.Patrolling || _state == TRexState.Alert)
        {
            _hasRoaredThisEncounter = true; // no warning this time
            BeginCharge();
        }

        if (_currentHealth <= 0f)
            Die();
    }

    private void CheckEnrage()
    {
        if (_isEnraged) return;
        if (_currentHealth <= maxHealth * enrageHealthThreshold)
        {
            _isEnraged = true;

            // Roar on enrage transition
            if (_state != TRexState.Roaring)
            {
                ScreenShake.Shake(1f, roarShakeMagnitude * 1.5f);
                if (roarSound != null && _audioSource != null)
                {
                    _audioSource.clip = roarSound;
                    _audioSource.pitch = 0.85f; // deeper, angrier
                    _audioSource.Play();
                }
                ScatterNearbyDinos();
            }
        }
    }

    private void Die()
    {
        _state = TRexState.Dead;
        _agent.isStopped = true;
        _agent.enabled = false;

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();

        if (anim != null)
        {
            anim.SetBool(walkParam, false);
            anim.SetBool(runParam, false);
            anim.SetBool(attackParam, false);
            anim.SetBool(roarParam, false);
            anim.SetBool(deathParam, true);
        }

        // Earth-shaking collapse
        ScreenShake.Shake(1.5f, 0.2f);

        var harvestable = gameObject.AddComponent<Harvestable>();
        harvestable.SetReward(harvestReward);

        Destroy(gameObject, despawnTime);
    }

    // --- Footsteps ---

    private void UpdateFootsteps()
    {
        if (!_agent.enabled || _agent.velocity.magnitude < 0.5f) return;
        if (_state == TRexState.Roaring || _state == TRexState.Attacking) return;

        // Faster footsteps when charging
        float interval = _state == TRexState.Charging ? footstepInterval * 0.5f : footstepInterval;

        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer <= 0f)
        {
            _footstepTimer = interval;

            if (footstepSound != null)
                AudioSource.PlayClipAtPoint(footstepSound, transform.position, 0.8f);

            // Distant rumble — screen shake only if player is within hearing range
            float distToPlayer = Vector3.Distance(transform.position, _player.position);
            if (distToPlayer <= footstepHearingRange)
            {
                float distFade = 1f - (distToPlayer / footstepHearingRange);
                ScreenShake.Shake(0.15f, footstepShakeMagnitude * distFade);
            }
        }
    }

    // --- Helpers ---

    private void FaceTarget()
    {
        if (_player == null) return;
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0f;
        // Slow turn — T-Rex is huge, shouldn't snap instantly
        float turnSpeed = _isEnraged ? 3f : 2f;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        bool isMoving = _agent.enabled && _agent.velocity.magnitude > 0.1f;
        bool isCharging = _state == TRexState.Charging;
        bool isAttacking = _state == TRexState.Attacking;
        bool isRoaring = _state == TRexState.Roaring;

        anim.SetBool(walkParam, isMoving && !isCharging && !isAttacking && !isRoaring);
        anim.SetBool(runParam, isMoving && isCharging);
        anim.SetBool(attackParam, isAttacking);
        anim.SetBool(roarParam, isRoaring);
    }
}
