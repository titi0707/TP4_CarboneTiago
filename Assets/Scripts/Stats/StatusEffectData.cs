using UnityEngine;

[CreateAssetMenu(fileName = "NewStatus", menuName = "Combate/Estado Alterado")]
public class StatusEffectData : ScriptableObject
{
    public string statusName = "Estado";
    public string shortLabel = "EST";
    public Color color = Color.magenta;
    public int durationTurns = 3;

    public float damagePercentPerTurn = 0f;
    [Range(0f, 1f)] public float skipTurnChance = 0f;
    public float atbFillMultiplier = 1f;

    public StatType affectedStat = StatType.Attack;
    public float statMultiplier = 1f;
}