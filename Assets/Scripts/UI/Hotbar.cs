using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Hotbar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private int slotCount = 5;
    [SerializeField] private float slotSize = 70f;
    [SerializeField] private float slotSpacing = 8f;
    [SerializeField] private float bottomOffset = 40f;

    [Header("Colors")]
    [SerializeField] private Color slotColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
    [SerializeField] private Color borderColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    [SerializeField] private Color selectedGlowMin = new Color(0.8f, 0.65f, 0.2f, 0.6f);
    [SerializeField] private Color selectedGlowMax = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private float glowSpeed = 3f;

    [Header("Number Labels")]
    [SerializeField] private int fontSize = 14;
    [SerializeField] private Color numberColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

    private int _selectedIndex;
    private float _scrollCooldown;
    private Image[] _borderImages;
    private Image[] _iconImages;
    private RectTransform _panelRect;

    // Public API for future item system
    public int SelectedIndex => _selectedIndex;
    public int SlotCount => slotCount;

    private void Start()
    {
        BuildUI();
        Select(0);
    }

    private void Update()
    {
        HandleInput();
        AnimateGlow();
    }

    private void HandleInput()
    {
        // Number keys 1-5
        if (Keyboard.current != null)
        {
            for (int i = 0; i < slotCount; i++)
            {
                Key key = Key.Digit1 + i;
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    Select(i);
                    return;
                }
            }
        }

        // Scroll wheel
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            _scrollCooldown -= Time.unscaledDeltaTime;
            if (_scrollCooldown > 0f) return;

            if (scroll > 0f)
            {
                Select((_selectedIndex - 1 + slotCount) % slotCount);
                _scrollCooldown = 0.1f;
            }
            else if (scroll < 0f)
            {
                Select((_selectedIndex + 1) % slotCount);
                _scrollCooldown = 0.1f;
            }
        }
    }

    public void Select(int index)
    {
        if (index < 0 || index >= slotCount) return;

        _selectedIndex = index;

        for (int i = 0; i < slotCount; i++)
            _borderImages[i].color = (i == _selectedIndex) ? selectedGlowMax : borderColor;
    }

    private void AnimateGlow()
    {
        float t = (Mathf.Sin(Time.unscaledTime * glowSpeed) + 1f) * 0.5f;
        _borderImages[_selectedIndex].color = Color.Lerp(selectedGlowMin, selectedGlowMax, t);
    }

    // --- Public API for future item system ---

    /// <summary>Set the icon sprite for a slot. Pass null to clear.</summary>
    public void SetSlotIcon(int index, Sprite icon)
    {
        if (index < 0 || index >= slotCount) return;

        _iconImages[index].sprite = icon;
        _iconImages[index].color = icon != null ? Color.white : Color.clear;
    }

    /// <summary>Clear all slot icons.</summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < slotCount; i++)
            SetSlotIcon(i, null);
    }

    // --- UI Construction ---

    private void BuildUI()
    {
        _borderImages = new Image[slotCount];
        _iconImages = new Image[slotCount];

        // Panel container — anchored bottom-center
        GameObject panelObj = new GameObject("HotbarPanel");
        panelObj.transform.SetParent(transform, false);

        _panelRect = panelObj.AddComponent<RectTransform>();
        float totalWidth = slotCount * slotSize + (slotCount - 1) * slotSpacing;
        _panelRect.sizeDelta = new Vector2(totalWidth, slotSize);
        _panelRect.anchorMin = new Vector2(0.5f, 0f);
        _panelRect.anchorMax = new Vector2(0.5f, 0f);
        _panelRect.pivot = new Vector2(0.5f, 0f);
        _panelRect.anchoredPosition = new Vector2(0f, bottomOffset);

        HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < slotCount; i++)
            BuildSlot(panelObj.transform, i);
    }

    private void BuildSlot(Transform parent, int index)
    {
        float borderWidth = 3f;

        // Slot root
        GameObject slotObj = new GameObject($"Slot_{index + 1}");
        slotObj.transform.SetParent(parent, false);

        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(slotSize, slotSize);

        // Background fill
        Image bg = slotObj.AddComponent<Image>();
        bg.color = slotColor;

        // Border (child layered on top, slightly larger via negative padding trick)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slotObj.transform, false);

        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = borderColor;
        borderImg.type = Image.Type.Sliced;
        borderImg.fillCenter = false;
        _borderImages[index] = borderImg;

        // Icon image (for future items)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        float iconPadding = borderWidth + 6f;
        iconRect.offsetMin = new Vector2(iconPadding, iconPadding);
        iconRect.offsetMax = new Vector2(-iconPadding, -iconPadding);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = Color.clear; // hidden until item assigned
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        _iconImages[index] = iconImg;

        // Number label
        GameObject numObj = new GameObject("Number");
        numObj.transform.SetParent(slotObj.transform, false);

        RectTransform numRect = numObj.AddComponent<RectTransform>();
        numRect.anchorMin = new Vector2(0f, 1f);
        numRect.anchorMax = new Vector2(0f, 1f);
        numRect.pivot = new Vector2(0f, 1f);
        numRect.anchoredPosition = new Vector2(4f, -2f);
        numRect.sizeDelta = new Vector2(20f, 20f);

        Text numText = numObj.AddComponent<Text>();
        numText.text = (index + 1).ToString();
        numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        numText.fontSize = fontSize;
        numText.color = numberColor;
        numText.alignment = TextAnchor.UpperLeft;
        numText.raycastTarget = false;
    }
}
