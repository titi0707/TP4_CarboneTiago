using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    
    public string unitName = "Unidad";
    public bool isPlayer = true;
    public int maxHP = 100;
    public int attack = 15;
    public int defense = 5;
    public int speed = 10;

    
    public int currentHP;

    public bool IsAlive => currentHP > 0;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"{unitName} recibe {amount} de daño ({currentHP}/{maxHP})");

        if (!IsAlive)
        {
            Debug.Log($"{unitName} fue derrotado");
            gameObject.SetActive(false); 
        }
    }
}