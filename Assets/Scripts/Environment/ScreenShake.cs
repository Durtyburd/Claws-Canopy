using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    private static ScreenShake _instance;

    private Vector3 _originalLocalPos;
    private float _shakeTimer;
    private float _shakeMagnitude;
    private float _shakeDuration;

    private void Awake()
    {
        _instance = this;
        _originalLocalPos = transform.localPosition;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void LateUpdate()
    {
        if (_shakeTimer <= 0f)
        {
            transform.localPosition = _originalLocalPos;
            return;
        }

        _shakeTimer -= Time.deltaTime;

        // Fade out as the shake progresses
        float fade = _shakeTimer / _shakeDuration;
        float magnitude = _shakeMagnitude * fade;

        Vector3 offset = new Vector3(
            Random.Range(-1f, 1f) * magnitude,
            Random.Range(-1f, 1f) * magnitude,
            0f
        );

        transform.localPosition = _originalLocalPos + offset;
    }

    /// <summary>Trigger a screen shake. Call from anywhere.</summary>
    public static void Shake(float duration, float magnitude)
    {
        if (_instance == null)
        {
            // Auto-attach to main camera if not manually placed
            var cam = Camera.main;
            if (cam != null)
                _instance = cam.gameObject.AddComponent<ScreenShake>();
            else
                return;
        }

        // Only override if this shake is stronger than the current one
        if (_instance._shakeTimer > 0f && magnitude < _instance._shakeMagnitude)
            return;

        _instance._shakeDuration = duration;
        _instance._shakeMagnitude = magnitude;
        _instance._shakeTimer = duration;
    }
}
