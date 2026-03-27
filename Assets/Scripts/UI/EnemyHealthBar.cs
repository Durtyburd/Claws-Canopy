using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float extraPadding = 0.5f;
    [SerializeField] private bool showHealthBar = true;

    private EnemyAI _enemy;
    private Camera _cam;
    private Canvas _canvas;
    private float _heightOffset;

    private void Start()
    {
        _enemy = GetComponentInParent<EnemyAI>();
        _cam = Camera.main;
        _canvas = GetComponent<Canvas>();

        if (_canvas != null)
        {
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = _cam;
        }

        if (fillImage != null)
            fillImage.fillAmount = 1f;

        CalculateHeight();
        _canvas.enabled = showHealthBar;
    }

    private void CalculateHeight()
    {
        if (_enemy == null) return;

        // Find the tallest point of all renderers on the dino
        Renderer[] renderers = _enemy.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            _heightOffset = 2f;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        _heightOffset = bounds.max.y - _enemy.transform.position.y + extraPadding;
    }

    private void LateUpdate()
    {
        if (_enemy == null || _cam == null) return;

        if (!showHealthBar)
        {
            if (_canvas.enabled) _canvas.enabled = false;
            return;
        }

        if (!_canvas.enabled) _canvas.enabled = true;

        transform.position = _enemy.transform.position + new Vector3(0f, _heightOffset, 0f);
        transform.forward = _cam.transform.forward;

        if (fillImage != null)
            fillImage.fillAmount = _enemy.CurrentHealth / _enemy.MaxHealth;
    }
}
