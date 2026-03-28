using UnityEngine;

public enum ItemType { Weapon, Consumable }
public enum UseAction { None, Shoot, Heal }

[CreateAssetMenu(fileName = "NewItem", menuName = "Claw & Canopy/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    public UseAction useAction;
    public GameObject heldModelPrefab;
    public float healAmount;
    public bool stackable = true;
    public int maxStack = 5;
}
