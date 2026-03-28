using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float MaxHealth = 100f;
    [HideInInspector] public float CurrentHealth;

    [Header("Respawn")]
    [SerializeField] private float invulnerabilityDuration = 2f;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private float _invulnerabilityTimer;

    public bool IsInvulnerable => _invulnerabilityTimer > 0f;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (_invulnerabilityTimer > 0f)
            _invulnerabilityTimer -= Time.deltaTime;
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsInvulnerable) return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0f);

        if (CurrentHealth <= 0f)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        var controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        transform.position = _spawnPosition;
        transform.rotation = _spawnRotation;

        if (controller != null)
            controller.enabled = true;

        CurrentHealth = MaxHealth;
        _invulnerabilityTimer = invulnerabilityDuration;

        var fpc = GetComponent<StarterAssets.FirstPersonController>();
        if (fpc != null)
            fpc.CurrentStamina = fpc.MaxStamina;
    }
}
