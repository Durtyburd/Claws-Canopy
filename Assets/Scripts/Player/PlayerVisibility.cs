using UnityEngine;

public class PlayerVisibility : MonoBehaviour
{
    [Header("Visibility Modifiers")]
    [SerializeField] private float crouchModifier = 0.5f;
    [SerializeField] private float stillModifier = 0.7f;
    [SerializeField] private float sprintModifier = 1.4f;
    [SerializeField] private float tallGrassCrouchModifier = 0.15f;
    [SerializeField] private float tallGrassStandingModifier = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool showDebug;

    private StarterAssets.FirstPersonController _fpc;
    private int _grassZoneCount;

    public float Visibility { get; private set; } = 1f;
    public bool InTallGrass => _grassZoneCount > 0;
    public float CurrentSpeed => _fpc != null ? _fpc.CurrentSpeed : 0f;

    public void EnterGrass() => _grassZoneCount++;
    public void ExitGrass() => _grassZoneCount = Mathf.Max(0, _grassZoneCount - 1);

    private void Start()
    {
        _fpc = GetComponent<StarterAssets.FirstPersonController>();
        if (_fpc == null)
            Debug.LogWarning("PlayerVisibility: No FirstPersonController found on this GameObject.");
    }

    private void Update()
    {
        if (_fpc == null) return;

        float vis = 1f;

        // Crouch
        if (_fpc.IsCrouching)
            vis *= crouchModifier;

        // Sprint
        if (_fpc.IsSprinting)
            vis *= sprintModifier;

        // Standing still
        if (_fpc.CurrentSpeed < 0.1f)
            vis *= stillModifier;

        // Tall grass
        if (InTallGrass)
            vis *= _fpc.IsCrouching ? tallGrassCrouchModifier : tallGrassStandingModifier;

        Visibility = Mathf.Clamp(vis, 0f, 1.5f);

        if (showDebug)
            Debug.Log($"Visibility: {Visibility:F2} | Crouch:{_fpc.IsCrouching} Sprint:{_fpc.IsSprinting} Speed:{_fpc.CurrentSpeed:F1} Grass:{InTallGrass}");
    }
}
