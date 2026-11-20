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
    BattleUnit self;

    EnemyData Settings => self.data as EnemyData;
    TargetStrategy Strategy => Settings != null ? Settings.strategy : TargetStrategy.Random;
    float Smartness => Settings != null ? Settings.smartness : 0f;
    float AbilityChance => Settings != null ? Settings.abilityChance : 0f;

    void Awake()
    {
        self = GetComponent<BattleUnit>();
    }

    public AbilityData ChooseAbility()
    {
        List<AbilityData> usable = self.abilities
            .Where(a => a != null && !a.targetsAllies && self.CanUse(a))
            .ToList();

        if (usable.Count == 0 || Random.value > AbilityChance) return null;

        AbilityData chosen = usable[Random.Range(0, usable.Count)];
        Debug.Log($"[IA] {self.unitName} decide usar {chosen.abilityName}");
        return chosen;
    }

    public BattleUnit ChooseTarget(List<BattleUnit> possibleTargets)
    {
        List<BattleUnit> alive = possibleTargets.Where(t => t.IsAlive).ToList();
        if (alive.Count == 0) return null;

        if (Strategy == TargetStrategy.Random || Random.value > Smartness)
        {
            BattleUnit randomTarget = alive[Random.Range(0, alive.Count)];
            Debug.Log($"[IA] {self.unitName} elige al azar → {randomTarget.unitName}");
            return randomTarget;
        }

        BattleUnit target;
        switch (Strategy)
        {
            case TargetStrategy.LowestHP:
                target = alive.OrderBy(t => t.currentHP).First();
                break;
            case TargetStrategy.LowestHPPercent:
                target = alive.OrderBy(t => t.HPPercent).First();
                break;
            case TargetStrategy.HighestAttack:
                target = alive.OrderByDescending(t => t.Attack).First();
                break;
            case TargetStrategy.LowestDefense:
                target = alive.OrderBy(t => t.Defense).First();
                break;
            default:
                target = alive[Random.Range(0, alive.Count)];
                break;
        }

        Debug.Log($"[IA] {self.unitName} usa estrategia {Strategy} → {target.unitName}");
        return target;
    }
}