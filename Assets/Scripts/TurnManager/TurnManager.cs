using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MenuState { None, ChoosingAction, ChoosingAbility, ChoosingItem, ChoosingTarget }

public class QueuedAction
{
    public BattleUnit user;
    public BattleUnit target;
    public AbilityData ability;
    public ItemData item;
}

public class TurnManager : MonoBehaviour
{
    [SerializeField] List<BattleUnit> playerUnits = new List<BattleUnit>();
    [SerializeField] List<BattleUnit> enemyUnits = new List<BattleUnit>();
    [SerializeField] Inventory inventory;

    [SerializeField] float atbFillRate = 3f;
    [SerializeField] bool waitMode = false;
    [SerializeField] float delayBetweenActions = 0.3f;
    [SerializeField] float basicAttackRecovery = 0f;

    [SerializeField] float attackDistance = 1.5f;
    [SerializeField] float hitDelay = 0.4f;
    [SerializeField] float attackRecovery = 0.6f;
    [SerializeField] float statusPause = 0.6f;

    public event System.Action OnUIRefresh;

    readonly Queue<QueuedAction> actionQueue = new Queue<QueuedAction>();
    readonly List<BattleUnit> waitingForCommand = new List<BattleUnit>();
    readonly HashSet<BattleUnit> busyUnits = new HashSet<BattleUnit>();

    BattleUnit commandUnit;
    AbilityData pendingAbility;
    ItemData pendingItem;
    MenuState menuState = MenuState.None;
    bool isExecuting;
    bool battleOver;
    string resultText = "";

    public BattleUnit CurrentUnit => commandUnit;
    public MenuState State => menuState;
    public bool IsBattleOver => battleOver;
    public string ResultText => resultText;
    public Inventory PartyInventory => inventory;

    public bool CurrentUnitHasAbilities =>
        commandUnit != null && commandUnit.abilities.Exists(a => a != null);

    public string PendingActionName =>
        pendingItem != null ? pendingItem.itemName :
        pendingAbility != null ? pendingAbility.abilityName :
        "Atacar";

    void Start()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
    }

    void SetState(MenuState newState)
    {
        menuState = newState;
        OnUIRefresh?.Invoke();
    }

    IEnumerable<BattleUnit> AllUnits()
    {
        return playerUnits.Concat(enemyUnits);
    }

    static bool TargetsAllies(AbilityData ability, ItemData item)
    {
        if (item != null) return !item.targetsEnemies;
        return ability != null && ability.targetsAllies;
    }

    void Update()
    {
        if (battleOver) return;

        if (commandUnit != null && !commandUnit.IsAlive)
        {
            busyUnits.Remove(commandUnit);
            commandUnit = null;
            StartNextCommand();
        }

        bool paused = waitMode && (isExecuting || commandUnit != null);
        if (!paused)
        {
            foreach (BattleUnit u in AllUnits())
            {
                if (!busyUnits.Contains(u))
                    u.TickATB(atbFillRate * Time.deltaTime);
            }
        }

        List<BattleUnit> newlyReady = AllUnits()
            .Where(u => u.IsReady && !busyUnits.Contains(u))
            .OrderByDescending(u => u.Speed)
            .ToList();

        foreach (BattleUnit u in newlyReady)
        {
            busyUnits.Add(u);
            u.OnTurnStart();

            if (u.isPlayer)
            {
                waitingForCommand.Add(u);
                if (commandUnit == null) StartNextCommand();
            }
            else
            {
                actionQueue.Enqueue(new QueuedAction { user = u });
            }
        }

        if (!isExecuting && actionQueue.Count > 0)
            StartCoroutine(ExecuteNext());
    }

    void StartNextCommand()
    {
        waitingForCommand.RemoveAll(u => !u.IsAlive);

        if (battleOver || waitingForCommand.Count == 0)
        {
            commandUnit = null;
            SetState(MenuState.None);
            return;
        }

        commandUnit = waitingForCommand[0];
        waitingForCommand.RemoveAt(0);
        pendingAbility = null;
        pendingItem = null;
        Debug.Log($"Turno de {commandUnit.unitName}");
        SetState(MenuState.ChoosingAction);
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
        if (menuState != MenuState.ChoosingAbility || !commandUnit.CanUse(ability)) return;
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
        if (menuState != MenuState.ChoosingTarget || commandUnit == null || target == null || !target.IsAlive) return;

        actionQueue.Enqueue(new QueuedAction
        {
            user = commandUnit,
            target = target,
            ability = pendingAbility,
            item = pendingItem
        });

        commandUnit = null;
        StartNextCommand();
    }

    public List<BattleUnit> GetCurrentTargets()
    {
        return TargetsAllies(pendingAbility, pendingItem) ? playerUnits : enemyUnits;
    }

    public string ItemLabel(ItemStack stack)
    {
        ItemData item = stack.item;
        string label = $"{item.itemName} x{stack.quantity}";

        if (item.hpRestore > 0) label += $"  +{item.hpRestore} HP";
        if (item.mpRestore > 0) label += $"  +{item.mpRestore} MP";

        foreach (StatModifier mod in item.modifiers)
            label += $"  {mod.ShortLabel()}";

        if (item.statusToApply != null)
            label += $"  {item.statusToApply.shortLabel}";

        if (item.curesStatuses.Count > 0)
            label += "  Cura " + string.Join("/", item.curesStatuses.Where(s => s != null).Select(s => s.shortLabel));

        return label;
    }

    IEnumerator ExecuteNext()
    {
        isExecuting = true;

        QueuedAction action = actionQueue.Dequeue();
        BattleUnit user = action.user;

        if (user.IsAlive)
        {
            float recovery = basicAttackRecovery;
            bool canAct = user.ProcessStatuses();

            if (!user.IsAlive || !canAct)
            {
                yield return new WaitForSeconds(statusPause);
            }
            else
            {
                if (!user.isPlayer) DecideEnemyAction(action);
                ValidateAction(action);

                if (action.target != null)
                {
                    if (action.item != null)
                    {
                        recovery = action.item.recoveryCost;
                        yield return StartCoroutine(UseItem(user, action.target, action.item));
                    }
                    else
                    {
                        if (action.ability != null) recovery = action.ability.recoveryCost;
                        yield return StartCoroutine(PerformAction(user, action.target, action.ability));
                    }
                }
                else
                {
                    Debug.Log($"{user.unitName} no pudo actuar");
                }
            }

            if (user.IsAlive)
            {
                user.TickModifiers();
                user.ResetATB(recovery);
                if (recovery > 0f)
                    Debug.Log($"{user.unitName} queda en recuperación (-{recovery} ATB)");
            }
        }

        busyUnits.Remove(user);

        if (!CheckBattleEnd())
            yield return new WaitForSeconds(delayBetweenActions);

        isExecuting = false;
    }

    void DecideEnemyAction(QueuedAction action)
    {
        EnemyAI ai = action.user.GetComponent<EnemyAI>();

        if (ai != null)
        {
            action.ability = ai.ChooseAbility();
            action.target = ai.ChooseTarget(playerUnits);
        }
        else
        {
            action.target = RandomAlive(playerUnits);
        }
    }

    void ValidateAction(QueuedAction action)
    {
        if (action.item != null && !inventory.Has(action.item))
        {
            action.target = null;
            return;
        }

        if (action.ability != null && !action.user.CanUse(action.ability))
        {
            action.ability = null;
            if (action.target != null && action.target.isPlayer == action.user.isPlayer)
                action.target = null;
        }

        if (action.target == null || !action.target.IsAlive)
        {
            bool targetsAllies = TargetsAllies(action.ability, action.item);
            List<BattleUnit> side = (action.user.isPlayer == targetsAllies) ? playerUnits : enemyUnits;
            action.target = RandomAlive(side);
        }
    }

    BattleUnit RandomAlive(List<BattleUnit> list)
    {
        List<BattleUnit> alive = list.Where(u => u.IsAlive).ToList();
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
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

        if (ability != null)
        {
            ApplyATBChange(target, ability.atbChangeOnTarget);
            TryApplyStatus(target, ability.statusToApply, ability.statusChance);
        }

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

        foreach (StatusEffectData cure in item.curesStatuses)
            target.RemoveStatus(cure);

        foreach (StatModifier mod in item.modifiers)
            target.AddModifier(mod, target == user);

        TryApplyStatus(target, item.statusToApply, 1f);
        ApplyATBChange(target, item.atbChangeOnTarget);

        yield return new WaitForSeconds(attackRecovery);
    }

    void TryApplyStatus(BattleUnit target, StatusEffectData status, float chance)
    {
        if (status == null || !target.IsAlive) return;

        if (Random.value <= chance)
            target.AddStatus(status);
        else
            Debug.Log($"{target.unitName} resiste {status.statusName}");
    }

    void ApplyEffect(BattleUnit user, BattleUnit target, AbilityData ability)
    {
        if (ability == null)
        {
            target.TakeDamage(Mathf.Max(1, user.Attack - target.Defense));
            return;
        }

        switch (ability.effect)
        {
            case AbilityEffect.Heal:
                target.Heal(ability.power + user.Magic);
                break;

            case AbilityEffect.Damage:
                int stat = ability.isMagic ? user.Magic : user.Attack;
                int def = ability.isMagic ? target.Defense / 2 : target.Defense;
                target.TakeDamage(Mathf.Max(1, ability.power + stat - def));
                break;

            case AbilityEffect.Support:
                break;
        }
    }

    void ApplyATBChange(BattleUnit target, float amount)
    {
        if (amount == 0f || !target.IsAlive) return;

        target.ChangeATB(amount);
        string verb = amount > 0f ? "se acelera" : "se retrasa";
        Debug.Log($"{target.unitName} {verb} ({amount:+0;-0} ATB)");
    }

    bool CheckBattleEnd()
    {
        if (battleOver) return true;

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
            commandUnit = null;
            menuState = MenuState.None;
            Debug.Log(resultText);
            OnUIRefresh?.Invoke();
        }

        return battleOver;
    }
}