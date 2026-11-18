using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    
    [SerializeField] List<BattleUnit> playerUnits = new List<BattleUnit>();
    [SerializeField] List<BattleUnit> enemyUnits = new List<BattleUnit>();

    [SerializeField] float delayBetweenTurns = 1f;

    BattleUnit currentUnit;
    BattleUnit selectedTarget;
    bool waitingForPlayer;
    bool battleOver;
    string resultText = "";

    void Start()
    {
        StartCoroutine(BattleLoop());
    }

    
    IEnumerator BattleLoop()
    {
        int round = 1;

        while (!battleOver)
        {
            Debug.Log($"===== RONDA {round} =====");

            
            List<BattleUnit> turnOrder = playerUnits.Concat(enemyUnits)
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.speed)
                .ToList();

            foreach (BattleUnit unit in turnOrder)
            {
                if (!unit.IsAlive) continue; 

                currentUnit = unit;
                Debug.Log($"Turno de {unit.unitName}");

                if (unit.isPlayer)
                    yield return StartCoroutine(PlayerTurn(unit));
                else
                    yield return StartCoroutine(EnemyTurn(unit));

                if (CheckBattleEnd()) yield break;

                yield return new WaitForSeconds(delayBetweenTurns);
            }

            round++;
        }
    }

  
    IEnumerator PlayerTurn(BattleUnit unit)
    {
        selectedTarget = null;
        waitingForPlayer = true;

        yield return new WaitUntil(() => selectedTarget != null);

        waitingForPlayer = false;
        PerformAttack(unit, selectedTarget);
    }

    
    public void SelectTarget(BattleUnit target)
    {
        if (!waitingForPlayer || target == null || !target.IsAlive) return;
        selectedTarget = target;
    }

    
    IEnumerator EnemyTurn(BattleUnit unit)
    {
        yield return new WaitForSeconds(0.5f); 

        List<BattleUnit> targets = playerUnits.Where(u => u.IsAlive).ToList();
        BattleUnit target = targets[Random.Range(0, targets.Count)];

        PerformAttack(unit, target);
    }

    
    
    
    void PerformAttack(BattleUnit attacker, BattleUnit target)
    {
        int damage = Mathf.Max(1, attacker.attack - target.defense);
        Debug.Log($"{attacker.unitName} ataca a {target.unitName}");
        target.TakeDamage(damage);
    }

   
    bool CheckBattleEnd()
    {
        if (enemyUnits.All(u => !u.IsAlive))
        {
            battleOver = true;
            resultText = "¡VICTORIA!";
        }
        else if (playerUnits.All(u => !u.IsAlive))
        {
            battleOver = true;
            resultText = "DERROTA";
        }

        if (battleOver) Debug.Log(resultText);
        return battleOver;
    }

   
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 500));

        
        foreach (var u in playerUnits)
            GUILayout.Label($"[Aliado] {u.unitName}: {u.currentHP}/{u.maxHP}");
        foreach (var u in enemyUnits)
            GUILayout.Label($"[Enemigo] {u.unitName}: {u.currentHP}/{u.maxHP}");

        GUILayout.Space(10);

        if (battleOver)
        {
            GUILayout.Label(resultText);
        }
        else if (waitingForPlayer)
        {
            GUILayout.Label($"Turno de {currentUnit.unitName}. Elegí a quién atacar:");
            foreach (var enemy in enemyUnits)
            {
                if (enemy.IsAlive && GUILayout.Button($"Atacar a {enemy.unitName}"))
                    SelectTarget(enemy);
            }
        }
        else if (currentUnit != null)
        {
            GUILayout.Label($"Turno de {currentUnit.unitName}...");
        }

        GUILayout.EndArea();
    }
}