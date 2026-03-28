using UnityEngine;
using UnityEngine.UI;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField] private Text moneyText;

    private PlayerWallet _wallet;

    private void Start()
    {
        var playerObj = FindAnyObjectByType<StarterAssets.FirstPersonController>();
        if (playerObj != null)
            _wallet = playerObj.GetComponent<PlayerWallet>();
        else
            Debug.LogWarning("MoneyDisplay: No FirstPersonController found in scene.");
    }

    private void Update()
    {
        if (_wallet == null || moneyText == null) return;
        moneyText.text = $"${_wallet.Money}";
    }
}
