using UnityEngine;
using UnityEngine.InputSystem;

public class Collectible : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private float interactRange = 3f;

    public void SetItemData(ItemData data) => itemData = data;
    public void SetInteractRange(float range) => interactRange = range;

    private Transform _player;
    private PlayerInventory _inventory;
    private bool _playerInRange;

    private void Start()
    {
        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _inventory = playerObj.GetComponent<PlayerInventory>();
        }
        else
        {
            Debug.LogWarning($"Collectible ({name}): No FirstPersonController found in scene.");
        }

    }

    private void Update()
    {
        if (_player == null || _inventory == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);
        bool hasRoom = _inventory.HasRoomFor(itemData);
        _playerInRange = distance <= interactRange && hasRoom;

        if (_playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Pickup();
    }

    private void OnGUI()
    {
        if (!_playerInRange) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        float width = 300f;
        float height = 40f;
        Rect rect = new Rect(
            (Screen.width - width) / 2f,
            Screen.height * 0.6f,
            width,
            height
        );

        string label = itemData != null ? $"Press E to Pick Up {itemData.itemName}" : "Press E to Pick Up";
        GUI.Label(rect, label, style);
    }

    private void Pickup()
    {
        if (_inventory.TryPickup(itemData))
            Destroy(gameObject);
    }
}
