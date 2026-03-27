using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float MaxHealth = 100f;
    [HideInInspector] public float CurrentHealth;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    public void TakeDamage(float damage)
    {
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

        var fpc = GetComponent<StarterAssets.FirstPersonController>();
        if (fpc != null)
            fpc.CurrentStamina = fpc.MaxStamina;
    }
}
