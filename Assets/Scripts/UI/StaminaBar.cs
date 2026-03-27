using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float smoothSpeed = 5f;

    private FirstPersonController _player;
    private float _displayedStamina;

    private void Start()
    {
        fillImage.fillAmount = 1f;
        _displayedStamina = 1f;
        _player = FindAnyObjectByType<FirstPersonController>();
    }

    private void Update()
    {
        if (_player == null) return;

        float target = _player.CurrentStamina / _player.MaxStamina;
        _displayedStamina = Mathf.Lerp(_displayedStamina, target, Time.deltaTime * smoothSpeed);
        fillImage.fillAmount = _displayedStamina;
    }
}
