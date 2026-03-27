using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float smoothSpeed = 5f;

    private PlayerHealth _player;
    private float _displayedHealth;

    private void Start()
    {
        fillImage.fillAmount = 1f;
        _displayedHealth = 1f;
        _player = FindAnyObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (_player == null) return;

        float target = _player.CurrentHealth / _player.MaxHealth;
        _displayedHealth = Mathf.Lerp(_displayedHealth, target, Time.deltaTime * smoothSpeed);
        fillImage.fillAmount = _displayedHealth;
    }
}
