using UnityEngine;
using UnityEngine.AI;

public enum RaptorState { Wandering, Stalking, Circling, Striking, Retreating, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class RaptorAI : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 60f;
    [HideInInspector] public float _currentHealth;

    [Header("Speed")]
    [SerializeField] private float wanderSpeed = 2.5f;
    [SerializeField] private float stalkSpeed = 3f;
    [SerializeField] private float circleSpeed = 5f;
    [SerializeField] private float strikeSpeed = 12f;
    [SerializeField] private float retreatSpeed = 9f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float retreatDistance = 20f;

    [Header("Timing")]
    [SerializeField] private float stalkArrivalThreshold = 3f;
    [SerializeField] private float circleArrivalThreshold = 4f;
    [SerializeField] private float retreatArrivalThreshold = 5f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderWaitMin = 2f;
    [SerializeField] private float wanderWaitMax = 5f;

    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 6f;
    [SerializeField] private float dodgeDuration = 0.8f;
    [SerializeField] private float dodgeCooldown = 2f;
    [SerializeField] private float aimDetectionAngle = 5f;
    [SerializeField] private float aimCheckInterval = 0.2f;

    [Header("Feint (Distractor)")]
    [SerializeField] private float feintDistance = 4f;
    [SerializeField] private float feintDuration = 0.6f;
    [SerializeField] private float feintCooldownMin = 3f;
    [SerializeField] private float feintCooldownMax = 6f;

    [Header("Harvest")]
    [SerializeField] private int harvestReward = 40;
    [SerializeField] private float despawnTime = 30f;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParam = "isWalking";
    [SerializeField] private string runParam = "isRunning";
    [SerializeField] private string attackParam = "isAttacking";
    [SerializeField] private string deathParam = "isDead";

    [Header("Sounds")]
    [SerializeField] private AudioClip movementSound;
    [SerializeField] private float soundVolume = 0.3f;
    [SerializeField] private float maxHearingDistance = 15f;

    [Header("References")]
    [SerializeField] private Animator anim;

    // IDamageable
    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;

    // State
    private RaptorState _state = RaptorState.Wandering;
    private RaptorPack _pack;
    private int _memberIndex;
    private NavMeshAgent _agent;
    private Camera _cam;
    private AudioSource _audioSource;
    private bool _hasAttackedThisStrike;
    private float _wanderTimer;
    private Vector3 _spawnPosition;

    // Dodge state
    private float _dodgeTimer;
    private float _dodgeCooldownTimer;
    private bool _isDodging;
    private float _aimCheckTimer;

    // Feint state (distractor)
    private bool _isFeinting;
    private float _feintTimer;
    private float _feintCooldownTimer;
    private Vector3 _feintReturnPos;

    public RaptorState State => _state;

    public void InitPack(RaptorPack pack, int index)
    {
        _pack = pack;
        _memberIndex = index;
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        _agent = GetComponent<NavMeshAgent>();
        _spawnPosition = transform.position;
        _wanderTimer = Random.Range(0f, wanderWaitMax);
        _feintCooldownTimer = Random.Range(feintCooldownMin, feintCooldownMax);

        _cam = Camera.main;

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
        if (_state == RaptorState.Dead) return;

        switch (_state)
        {
            case RaptorState.Wandering:
                WanderUpdate();
                break;
            case RaptorState.Stalking:
                StalkUpdate();
                break;
            case RaptorState.Circling:
                CircleUpdate();
                break;
            case RaptorState.Striking:
                StrikeUpdate();
                break;
            case RaptorState.Retreating:
                RetreatUpdate();
                break;
        }

        UpdateAnimator();
    }

    // --- State Updates ---

    private void WanderUpdate()
    {
        _agent.speed = wanderSpeed;

        if (!_agent.pathPending && _agent.remainingDistance <= 0.5f)
        {
            _agent.isStopped = true;
            _wanderTimer -= Time.deltaTime;

            if (_wanderTimer <= 0f)
            {
                Vector3 center = _pack != null ? _pack.transform.position : _spawnPosition;
                Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
                randomDir.y = 0f;

                if (NavMesh.SamplePosition(center + randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                }

                _wanderTimer = Random.Range(wanderWaitMin, wanderWaitMax);
            }
        }
    }

    private void StalkUpdate()
    {
        if (_pack == null || _pack.Player == null) return;

        _agent.speed = stalkSpeed;
        _agent.isStopped = false;

        Vector3 orbitPos = _pack.GetOrbitPosition(_memberIndex);
        _agent.SetDestination(orbitPos);

        float distToOrbit = Vector3.Distance(transform.position, orbitPos);
        if (distToOrbit <= stalkArrivalThreshold)
            _state = RaptorState.Circling;
    }

    private void CircleUpdate()
    {
        if (_pack == null || _pack.Player == null || !_pack.IsHunting)
        {
            _state = RaptorState.Wandering;
            return;
        }

        // Handle active dodge
        if (_isDodging)
        {
            _dodgeTimer -= Time.deltaTime;
            if (_dodgeTimer <= 0f)
                _isDodging = false;
            return; // let the agent run to dodge destination
        }

        _dodgeCooldownTimer -= Time.deltaTime;

        // Check if player is aiming at us — dodge if so
        if (_dodgeCooldownTimer <= 0f)
        {
            _aimCheckTimer -= Time.deltaTime;
            if (_aimCheckTimer <= 0f)
            {
                _aimCheckTimer = aimCheckInterval;
                if (IsPlayerAimingAtMe())
                {
                    StartDodge();
                    return;
                }
            }
        }

        // Distractor feint behavior
        if (_pack.IsDistractor(_memberIndex))
        {
            DistractorUpdate();
            return;
        }

        // Normal circling — follow orbit position
        _agent.speed = circleSpeed;
        _agent.isStopped = false;

        Vector3 orbitPos = _pack.GetOrbitPosition(_memberIndex);
        _agent.SetDestination(orbitPos);

        FaceTarget(_pack.Player.position);
    }

    private void DistractorUpdate()
    {
        if (_pack == null || _pack.Player == null) return;

        // Handle feint return
        if (_isFeinting)
        {
            _feintTimer -= Time.deltaTime;
            if (_feintTimer <= 0f)
            {
                _isFeinting = false;
                _agent.SetDestination(_feintReturnPos);
            }
            return;
        }

        _agent.speed = circleSpeed * 0.8f; // slightly slower, taunting
        _agent.isStopped = false;

        Vector3 orbitPos = _pack.GetOrbitPosition(_memberIndex);
        _agent.SetDestination(orbitPos);
        FaceTarget(_pack.Player.position);

        // Feint: occasionally dart toward player then back off
        _feintCooldownTimer -= Time.deltaTime;
        if (_feintCooldownTimer <= 0f)
        {
            _feintCooldownTimer = Random.Range(feintCooldownMin, feintCooldownMax);
            StartFeint();
        }
    }

    private void StartFeint()
    {
        if (_pack == null || _pack.Player == null) return;

        _isFeinting = true;
        _feintTimer = feintDuration;
        _feintReturnPos = transform.position;

        // Dart toward player
        Vector3 toPlayer = (_pack.Player.position - transform.position).normalized;
        Vector3 feintTarget = transform.position + toPlayer * feintDistance;

        if (NavMesh.SamplePosition(feintTarget, out NavMeshHit hit, feintDistance, NavMesh.AllAreas))
        {
            _agent.speed = strikeSpeed;
            _agent.SetDestination(hit.position);
        }
    }

    private void StrikeUpdate()
    {
        if (_pack == null || _pack.Player == null) return;

        _agent.speed = strikeSpeed;
        _agent.isStopped = false;
        _agent.SetDestination(_pack.Player.position);

        float distToPlayer = Vector3.Distance(transform.position, _pack.Player.position);

        if (distToPlayer <= attackRange && !_hasAttackedThisStrike)
        {
            _hasAttackedThisStrike = true;

            if (anim != null)
                anim.SetBool(attackParam, true);

            if (_pack.PlayerHealth != null)
                _pack.PlayerHealth.TakeDamage(attackDamage);

            _state = RaptorState.Retreating;
        }
    }

    private void RetreatUpdate()
    {
        if (_pack == null || _pack.Player == null)
        {
            _state = RaptorState.Wandering;
            return;
        }

        _agent.speed = retreatSpeed;
        _agent.isStopped = false;

        Vector3 awayDir = (transform.position - _pack.Player.position).normalized;
        Vector3 retreatTarget = _pack.Player.position + awayDir * retreatDistance;

        if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        float distToPlayer = Vector3.Distance(transform.position, _pack.Player.position);
        if (distToPlayer >= retreatArrivalThreshold && distToPlayer >= _pack.OrbitRadius * 0.7f)
            _state = _pack.IsHunting ? RaptorState.Circling : RaptorState.Wandering;
    }

    // --- Dodge ---

    private bool IsPlayerAimingAtMe()
    {
        if (_cam == null) return false;

        Vector3 toRaptor = (transform.position - _cam.transform.position);
        float angle = Vector3.Angle(_cam.transform.forward, toRaptor);
        return angle < aimDetectionAngle;
    }

    private void StartDodge()
    {
        _isDodging = true;
        _dodgeTimer = dodgeDuration;
        _dodgeCooldownTimer = dodgeCooldown;

        // Dodge perpendicular to the aim direction
        Vector3 aimDir = _cam.transform.forward;
        aimDir.y = 0f;
        aimDir.Normalize();

        Vector3 perpendicular = Vector3.Cross(aimDir, Vector3.up);

        // Randomly pick left or right
        if (Random.value > 0.5f)
            perpendicular = -perpendicular;

        Vector3 dodgeTarget = transform.position + perpendicular * dodgeDistance;

        if (NavMesh.SamplePosition(dodgeTarget, out NavMeshHit hit, dodgeDistance, NavMesh.AllAreas))
        {
            _agent.speed = strikeSpeed; // dodge at full sprint
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }
    }

    // --- Pack Commands ---

    public void OnPackHuntStart()
    {
        if (_state == RaptorState.Dead) return;
        _state = RaptorState.Stalking;
    }

    public void OnPackDisengage()
    {
        if (_state == RaptorState.Dead) return;
        _state = RaptorState.Wandering;
        _isDodging = false;
        _isFeinting = false;
        _agent.isStopped = true;
        _wanderTimer = Random.Range(0f, wanderWaitMin);
    }

    public void OnOrderedStrike()
    {
        if (_state == RaptorState.Dead) return;
        _hasAttackedThisStrike = false;
        _isDodging = false;
        _isFeinting = false;
        _state = RaptorState.Striking;
    }

    public bool CanStrike()
    {
        return _state == RaptorState.Circling && !_isDodging && !_isFeinting;
    }

    // --- Damage ---

    public void TakeDamage(float damage)
    {
        if (_state == RaptorState.Dead) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        // Getting shot while circling or stalking — retaliate immediately
        if (_state == RaptorState.Circling || _state == RaptorState.Stalking)
        {
            _hasAttackedThisStrike = false;
            _isDodging = false;
            _isFeinting = false;
            _state = RaptorState.Striking;
        }

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _state = RaptorState.Dead;
        _agent.isStopped = true;
        _agent.enabled = false;

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();

        if (anim != null)
        {
            anim.SetBool(runParam, false);
            anim.SetBool(walkParam, false);
            anim.SetBool(attackParam, false);
            anim.SetBool(deathParam, true);
        }

        if (_pack != null)
            _pack.OnMemberDied(this);

        var harvestable = gameObject.AddComponent<Harvestable>();
        harvestable.SetReward(harvestReward);

        Destroy(gameObject, despawnTime);
    }

    // --- Helpers ---

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
        bool isSprinting = _state == RaptorState.Striking || _state == RaptorState.Retreating
            || _state == RaptorState.Circling || _isDodging;
        bool isAttacking = _state == RaptorState.Striking && _hasAttackedThisStrike;

        anim.SetBool(walkParam, isMoving && !isSprinting && !isAttacking);
        anim.SetBool(runParam, isMoving && isSprinting && !isAttacking);
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
