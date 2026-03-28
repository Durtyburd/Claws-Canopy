using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Hotbar hotbar;
    [SerializeField] private Transform handHolder;
    [SerializeField] private FireArm fireArm;

    [Header("Starting Items")]
    [SerializeField] private ItemData gunItem;

    private ItemData[] _slotItems;
    private int[] _slotCounts;
    private GameObject _currentHeldModel;
    private int _lastSelectedIndex = -1;
    private PlayerHealth _playerHealth;
    private Text[] _countTexts;

    private bool _initialized;

    private void Start()
    {
        _playerHealth = GetComponent<PlayerHealth>();

        int count = hotbar != null ? hotbar.SlotCount : 5;
        _slotItems = new ItemData[count];
        _slotCounts = new int[count];
        _countTexts = new Text[count];

        // Gun goes in slot 0
        if (gunItem != null)
        {
            _slotItems[0] = gunItem;
            _slotCounts[0] = 1;
        }

    }

    private void Update()
    {
        if (hotbar == null) return;

        // Defer first refresh to Update so Hotbar.Start() has built its UI
        if (!_initialized)
        {
            _initialized = true;
            _lastSelectedIndex = hotbar.SelectedIndex;
            BuildCountLabels();
            for (int i = 0; i < _slotItems.Length; i++)
                UpdateSlotUI(i);
            RefreshHeldItem();
            return;
        }

        // Detect slot change
        if (hotbar.SelectedIndex != _lastSelectedIndex)
        {
            _lastSelectedIndex = hotbar.SelectedIndex;
            RefreshHeldItem();
        }

        // Use item on left click — only for non-weapon items (FireArm handles its own clicks)
        ItemData selected = _slotItems[hotbar.SelectedIndex];
        if (selected != null && selected.useAction != UseAction.Shoot)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryUseSelectedItem();
        }
    }

    // --- Pickup ---

    /// <summary>Try to add an item to the inventory. Returns true if successful.</summary>
    public bool TryPickup(ItemData item, int amount = 1)
    {
        if (item == null) return false;

        // Try to stack into existing slot first
        if (item.stackable)
        {
            for (int i = 0; i < _slotItems.Length; i++)
            {
                if (_slotItems[i] == item && _slotCounts[i] < item.maxStack)
                {
                    int canAdd = Mathf.Min(amount, item.maxStack - _slotCounts[i]);
                    _slotCounts[i] += canAdd;
                    UpdateSlotUI(i);
                    return true;
                }
            }
        }

        // Find an empty slot
        for (int i = 0; i < _slotItems.Length; i++)
        {
            if (_slotItems[i] == null)
            {
                _slotItems[i] = item;
                _slotCounts[i] = amount;
                UpdateSlotUI(i);
                return true;
            }
        }

        return false; // Inventory full
    }

    /// <summary>Check if inventory has room for this item.</summary>
    public bool HasRoomFor(ItemData item)
    {
        if (item == null) return false;

        if (item.stackable)
        {
            for (int i = 0; i < _slotItems.Length; i++)
            {
                if (_slotItems[i] == item && _slotCounts[i] < item.maxStack)
                    return true;
            }
        }

        for (int i = 0; i < _slotItems.Length; i++)
        {
            if (_slotItems[i] == null)
                return true;
        }

        return false;
    }

    // --- Use ---

    private void TryUseSelectedItem()
    {
        int index = hotbar.SelectedIndex;
        ItemData item = _slotItems[index];
        if (item == null) return;

        switch (item.useAction)
        {
            case UseAction.Shoot:
                // FireArm handles its own shooting — we just need to make sure it's enabled
                break;

            case UseAction.Heal:
                if (_playerHealth == null) break;
                if (_playerHealth.CurrentHealth >= _playerHealth.MaxHealth) break;

                _playerHealth.Heal(item.healAmount);
                ConsumeFromSlot(index);
                break;
        }
    }

    private void ConsumeFromSlot(int index)
    {
        _slotCounts[index]--;
        if (_slotCounts[index] <= 0)
        {
            _slotItems[index] = null;
            _slotCounts[index] = 0;
        }
        UpdateSlotUI(index);
        RefreshHeldItem();
    }

    // --- Held Model ---

    private void RefreshHeldItem()
    {
        // Destroy old held model
        if (_currentHeldModel != null)
            Destroy(_currentHeldModel);

        // Disable gun by default
        if (fireArm != null)
            fireArm.enabled = false;

        int index = hotbar != null ? hotbar.SelectedIndex : 0;
        ItemData item = _slotItems[index];

        if (item == null) return;

        // Weapon slot — enable the existing FireArm instead of spawning a model
        if (item.useAction == UseAction.Shoot)
        {
            if (fireArm != null)
            {
                fireArm.enabled = true;
                // Make sure the gun model is visible
                fireArm.gameObject.SetActive(true);
            }
            return;
        }

        // Other items — spawn held model in hand
        if (item.heldModelPrefab != null && handHolder != null)
        {
            _currentHeldModel = Instantiate(item.heldModelPrefab, handHolder);
            _currentHeldModel.transform.localPosition = Vector3.zero;
            _currentHeldModel.transform.localRotation = Quaternion.identity;
        }

        // Hide the gun model when holding something else
        if (fireArm != null)
            fireArm.gameObject.SetActive(false);
    }

    // --- UI ---

    private void UpdateSlotUI(int index)
    {
        if (hotbar == null) return;

        ItemData item = _slotItems[index];
        hotbar.SetSlotIcon(index, item != null ? item.icon : null);

        if (_countTexts[index] != null)
        {
            if (item != null && item.stackable && _slotCounts[index] > 0)
                _countTexts[index].text = _slotCounts[index].ToString();
            else
                _countTexts[index].text = "";
        }
    }

    private void BuildCountLabels()
    {
        if (hotbar == null) return;

        // Find slot objects built by Hotbar and add count labels
        var panel = hotbar.GetComponentInChildren<HorizontalLayoutGroup>();
        if (panel == null) return;

        for (int i = 0; i < _countTexts.Length && i < panel.transform.childCount; i++)
        {
            Transform slotTransform = panel.transform.GetChild(i);

            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(slotTransform, false);

            RectTransform rect = countObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-4f, 2f);
            rect.sizeDelta = new Vector2(20f, 20f);

            Text countText = countObj.AddComponent<Text>();
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 14;
            countText.color = Color.white;
            countText.alignment = TextAnchor.LowerRight;
            countText.raycastTarget = false;
            _countTexts[i] = countText;
        }
    }

    // --- Query ---

    public ItemData GetSelectedItem()
    {
        if (hotbar == null) return null;
        return _slotItems[hotbar.SelectedIndex];
    }
}
