using UnityEngine;

public enum AbilityEffect { Damage, Heal, Support }

[CreateAssetMenu(fileName = "NewAbility", menuName = "Combate/Habilidad")]
public class AbilityData : ScriptableObject
{
    public string abilityName = "Habilidad";
    public int mpCost = 5;
    public int power = 10;
    public AbilityEffect effect = AbilityEffect.Damage;
    public bool isMagic = true;
    public bool targetsAllies = false;
    public bool moveToTarget = false;
    public string animationTrigger = "Attack";

    public float recoveryCost = 0f;
    public float atbChangeOnTarget = 0f;

    public StatusEffectData statusToApply;
    [Range(0f, 1f)] public float statusChance = 1f;
}