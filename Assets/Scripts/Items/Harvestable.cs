using UnityEngine;
using UnityEngine.InputSystem;

public class Harvestable : MonoBehaviour
{
    [SerializeField] private int moneyReward = 50;
    [SerializeField] private float interactRange = 4f;

    private Transform _player;
    private PlayerWallet _wallet;
    private bool _playerInRange;

    public void SetReward(int amount)
    {
        moneyReward = amount;
    }

    private void Start()
    {
        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _wallet = playerObj.GetComponent<PlayerWallet>();
        }
    }

    private void Update()
    {
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);
        _playerInRange = distance <= interactRange;

        if (_playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Harvest();
        }
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

        GUI.Label(rect, $"Press E to Harvest (+${moneyReward})", style);
    }

    private void Harvest()
    {
        if (_wallet != null)
            _wallet.AddMoney(moneyReward);

        Destroy(gameObject);
    }
}
