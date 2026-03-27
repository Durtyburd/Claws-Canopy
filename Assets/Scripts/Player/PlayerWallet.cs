using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [HideInInspector] public int Money;

    public void AddMoney(int amount)
    {
        Money += amount;
        Debug.Log($"Money: {Money}");
    }
}
