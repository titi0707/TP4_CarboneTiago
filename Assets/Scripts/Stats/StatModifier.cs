using UnityEngine;

public enum StatType { Attack, Magic, Defense, Speed }

[System.Serializable]
public class StatModifier
{
    public StatType stat = StatType.Attack;
    public float multiplier = 1.5f;
    public int durationTurns = 3;

    public bool IsBuff => multiplier >= 1f;

    public string ShortLabel()
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        return $"{StatName(stat)} {percent:+0;-0}%";
    }

    public static string StatName(StatType type)
    {
        switch (type)
        {
            case StatType.Attack: return "ATK";
            case StatType.Magic: return "MAG";
            case StatType.Defense: return "DEF";
            case StatType.Speed: return "VEL";
            default: return "";
        }
    }
}