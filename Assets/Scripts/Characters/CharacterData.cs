using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Combate/Personaje")]
public class CharacterData : ScriptableObject
{
    public string unitName = "Personaje";

    public int maxHP = 100;
    public int maxMP = 20;
    public int mpRegenPerTurn = 2;

    public int attack = 15;
    public int magic = 10;
    public int defense = 5;
    public int speed = 10;

    public List<AbilityData> abilities = new List<AbilityData>();
}