using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TargetStrategy
{
    Random,
    LowestHP,
    LowestHPPercent,
    HighestAttack,
    LowestDefense
}

[RequireComponent(typeof(BattleUnit))]
public class EnemyAI : MonoBehaviour
{
    public TargetStrategy strategy = TargetStrategy.LowestHPPercent;
    [Range(0f, 1f)] public float smartness = 0.75f;
    [Range(0f, 1f)] public float abilityChance = 0.4f;

    BattleUnit self;

    void Awake()
    {
        self = GetComponent<BattleUnit>();
    }

    public AbilityData ChooseAbility()
    {
        List<AbilityData> usable = self.abilities
            .Where(a => a != null && !a.targetsAllies && self.CanUse(a))
            .ToList();

        if (usable.Count == 0 || Random.value > abilityChance) return null;

        AbilityData chosen = usable[Random.Range(0, usable.Count)];
        Debug.Log($"[IA] {self.unitName} decide usar {chosen.abilityName}");
        return chosen;
    }

    public BattleUnit ChooseTarget(List<BattleUnit> possibleTargets)
    {
        List<BattleUnit> alive = possibleTargets.Where(t => t.IsAlive).ToList();
        if (alive.Count == 0) return null;

        if (strategy == TargetStrategy.Random || Random.value > smartness)
        {
            BattleUnit randomTarget = alive[Random.Range(0, alive.Count)];
            Debug.Log($"[IA] {self.unitName} elige al azar → {randomTarget.unitName}");
            return randomTarget;
        }

        BattleUnit target;
        switch (strategy)
        {
            case TargetStrategy.LowestHP:
                target = alive.OrderBy(t => t.currentHP).First();
                break;
            case TargetStrategy.LowestHPPercent:
                target = alive.OrderBy(t => (float)t.currentHP / t.maxHP).First();
                break;
            case TargetStrategy.HighestAttack:
                target = alive.OrderByDescending(t => t.attack).First();
                break;
            case TargetStrategy.LowestDefense:
                target = alive.OrderBy(t => t.defense).First();
                break;
            default:
                target = alive[Random.Range(0, alive.Count)];
                break;
        }

        Debug.Log($"[IA] {self.unitName} usa estrategia {strategy} → {target.unitName}");
        return target;
    }
}