using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Combate/Enemigo")]
public class EnemyData : CharacterData
{
    public TargetStrategy strategy = TargetStrategy.Random;
    [Range(0f, 1f)] public float smartness = 0.75f;
    [Range(0f, 1f)] public float abilityChance = 0.4f;
}