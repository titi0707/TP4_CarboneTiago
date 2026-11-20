using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Combate/Objeto")]
public class ItemData : ScriptableObject
{
    public string itemName = "Objeto";
    public bool targetsEnemies = false;

    public int hpRestore;
    public int mpRestore;
    public List<StatModifier> modifiers = new List<StatModifier>();

    public StatusEffectData statusToApply;
    public List<StatusEffectData> curesStatuses = new List<StatusEffectData>();

    public float recoveryCost = 0f;
    public float atbChangeOnTarget = 0f;
}