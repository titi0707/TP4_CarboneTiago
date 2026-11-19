using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemStack
{
    public ItemData item;
    public int quantity = 1;
}

public class Inventory : MonoBehaviour
{
    public List<ItemStack> items = new List<ItemStack>();

    public bool Has(ItemData item)
    {
        return items.Exists(s => s.item == item && s.quantity > 0);
    }

    public bool HasAny()
    {
        return items.Exists(s => s.item != null && s.quantity > 0);
    }

    public bool Consume(ItemData item)
    {
        ItemStack stack = items.Find(s => s.item == item);
        if (stack == null || stack.quantity <= 0) return false;

        stack.quantity--;
        return true;
    }
}