using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MenuState { None, ChoosingAction, ChoosingAbility, ChoosingItem, ChoosingTarget }

public class TurnManager : MonoBehaviour
{
    [SerializeField] List<BattleUnit> playerUnits = new List<BattleUnit>();
    [SerializeField] List<BattleUnit> enemyUnits = new List<BattleUnit>();
    [SerializeField] Inventory inventory;

    [SerializeField] float delayBetweenTurns = 1f;

    [SerializeField] float attackDistance = 1.5f;
    [SerializeField] float hitDelay = 0.4f;
    [SerializeField] float attackRecovery = 0.6f;

    public event System.Action OnUIRefresh;

    BattleUnit currentUnit;
    BattleUnit selectedTarget;
    AbilityData pendingAbility;
    ItemData pendingItem;
    MenuState menuState = MenuState.None;
    bool battleOver;
    string resultText = "";

    public BattleUnit CurrentUnit => currentUnit;
    public MenuState State => menuState;
    public bool IsBattleOver => battleOver;
    public string ResultText => resultText;
    public Inventory PartyInventory => inventory;

    public bool CurrentUnitHasAbilities =>
        currentUnit != null && currentUnit.abilities.Exists(a => a != null);

    public string PendingActionName =>
        pendingItem != null ? pendingItem.itemName :
        pendingAbility != null ? pendingAbility.abilityName :
        "Atacar";

    void Start()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        StartCoroutine(BattleLoop());
    }

    void SetState(MenuState newState)
    {
        menuState = newState;
        OnUIRefresh?.Invoke();
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
                SetState(MenuState.None);
                Debug.Log($"Turno de {unit.unitName}");
                unit.OnTurnStart();

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
        pendingAbility = null;
        pendingItem = null;
        SetState(MenuState.ChoosingAction);

        yield return new WaitUntil(() => selectedTarget != null);

        SetState(MenuState.None);

        if (pendingItem != null)
            yield return StartCoroutine(UseItem(unit, selectedTarget, pendingItem));
        else
            yield return StartCoroutine(PerformAction(unit, selectedTarget, pendingAbility));
    }

    public void ChooseAttack()
    {
        if (menuState != MenuState.ChoosingAction) return;
        pendingAbility = null;
        pendingItem = null;
        SetState(MenuState.ChoosingTarget);
    }

    public void OpenAbilities()
    {
        if (menuState != MenuState.ChoosingAction || !CurrentUnitHasAbilities) return;
        SetState(MenuState.ChoosingAbility);
    }

    public void ChooseAbility(AbilityData ability)
    {
        if (menuState != MenuState.ChoosingAbility || !currentUnit.CanUse(ability)) return;
        pendingAbility = ability;
        pendingItem = null;
        SetState(MenuState.ChoosingTarget);
    }

    public void OpenItems()
    {
        if (menuState != MenuState.ChoosingAction || inventory == null || !inventory.HasAny()) return;
        SetState(MenuState.ChoosingItem);
    }

    public void ChooseItem(ItemData item)
    {
        if (menuState != MenuState.ChoosingItem || !inventory.Has(item)) return;
        pendingItem = item;
        pendingAbility = null;
        SetState(MenuState.ChoosingTarget);
    }

    public void BackToActions()
    {
        if (menuState == MenuState.ChoosingItem || menuState == MenuState.ChoosingAbility)
            SetState(MenuState.ChoosingAction);
    }

    public void CancelTarget()
    {
        if (menuState != MenuState.ChoosingTarget) return;

        if (pendingItem != null)
        {
            pendingItem = null;
            SetState(MenuState.ChoosingItem);
        }
        else if (pendingAbility != null)
        {
            pendingAbility = null;
            SetState(MenuState.ChoosingAbility);
        }
        else
        {
            SetState(MenuState.ChoosingAction);
        }
    }

    public void SelectTarget(BattleUnit target)
    {
        if (menuState != MenuState.ChoosingTarget || target == null || !target.IsAlive) return;
        selectedTarget = target;
    }

    public List<BattleUnit> GetCurrentTargets()
    {
        bool targetsAllies = pendingItem != null || (pendingAbility != null && pendingAbility.targetsAllies);
        return targetsAllies ? playerUnits : enemyUnits;
    }

    public string ItemLabel(ItemStack stack)
    {
        string label = $"{stack.item.itemName} x{stack.quantity}";
        if (stack.item.hpRestore > 0) label += $"  +{stack.item.hpRestore} HP";
        if (stack.item.mpRestore > 0) label += $"  +{stack.item.mpRestore} MP";
        return label;
    }

    IEnumerator EnemyTurn(BattleUnit unit)
    {
        yield return new WaitForSeconds(0.5f);

        BattleUnit target;
        AbilityData ability = null;
        EnemyAI ai = unit.GetComponent<EnemyAI>();

        if (ai != null)
        {
            ability = ai.ChooseAbility();
            target = ai.ChooseTarget(playerUnits);
        }
        else
        {
            List<BattleUnit> alive = playerUnits.Where(u => u.IsAlive).ToList();
            target = alive[Random.Range(0, alive.Count)];
        }

        if (target == null) yield break;

        yield return StartCoroutine(PerformAction(unit, target, ability));
    }

    IEnumerator PerformAction(BattleUnit user, BattleUnit target, AbilityData ability)
    {
        bool move = ability == null || ability.moveToTarget;
        string trigger = ability == null ? "Attack" : ability.animationTrigger;

        if (ability != null)
        {
            user.SpendMP(ability.mpCost);
            Debug.Log($"{user.unitName} usa {ability.abilityName} sobre {target.unitName} (-{ability.mpCost} MP)");
        }
        else
        {
            Debug.Log($"{user.unitName} ataca a {target.unitName}");
        }

        if (move)
        {
            Vector3 dir = (target.transform.position - user.transform.position).normalized;
            Vector3 attackPosition = target.transform.position - dir * attackDistance;
            yield return StartCoroutine(user.MoveTo(attackPosition));
        }

        user.PlayTrigger(trigger);
        yield return new WaitForSeconds(hitDelay);

        ApplyEffect(user, target, ability);
        yield return new WaitForSeconds(attackRecovery);

        if (move)
            yield return StartCoroutine(user.ReturnToStart());
    }

    IEnumerator UseItem(BattleUnit user, BattleUnit target, ItemData item)
    {
        inventory.Consume(item);
        Debug.Log($"{user.unitName} usa {item.itemName} sobre {target.unitName}");

        yield return new WaitForSeconds(hitDelay);

        if (item.hpRestore > 0) target.Heal(item.hpRestore);
        if (item.mpRestore > 0) target.RestoreMP(item.mpRestore);

        yield return new WaitForSeconds(attackRecovery);
    }

    void ApplyEffect(BattleUnit user, BattleUnit target, AbilityData ability)
    {
        if (ability == null)
        {
            target.TakeDamage(Mathf.Max(1, user.attack - target.defense));
            return;
        }

        if (ability.effect == AbilityEffect.Heal)
        {
            target.Heal(ability.power + user.magic);
            return;
        }

        int stat = ability.isMagic ? user.magic : user.attack;
        int def = ability.isMagic ? target.defense / 2 : target.defense;
        target.TakeDamage(Mathf.Max(1, ability.power + stat - def));
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

        if (battleOver)
        {
            Debug.Log(resultText);
            OnUIRefresh?.Invoke();
        }

        return battleOver;
    }
}