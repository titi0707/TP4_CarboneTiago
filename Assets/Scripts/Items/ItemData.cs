using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Combate/Objeto")]
public class ItemData : ScriptableObject
{
    public string itemName = "Objeto";
    public int hpRestore;
    public int mpRestore;
}