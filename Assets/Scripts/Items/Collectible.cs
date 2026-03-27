using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [SerializeField] private float healAmount = 25f;
    [SerializeField] private float interactRange = 3f;

    private Transform _player;
    private PlayerHealth _playerHealth;
    private bool _playerInRange;

    private void Start()
    {
        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        // Make sure collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);
        _playerInRange = distance <= interactRange;

        if (_playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Consume();
        }
    }

    private void OnGUI()
    {
        if (!_playerInRange) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        float width = 250f;
        float height = 40f;
        Rect rect = new Rect(
            (Screen.width - width) / 2f,
            Screen.height * 0.6f,
            width,
            height
        );

        GUI.Label(rect, "Press E to Eat", style);
    }

    private void Consume()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Heal(healAmount);
        }

        Destroy(gameObject);
    }
}
