using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleMenuUI : MonoBehaviour
{
    [SerializeField] TurnManager turnManager;

    [SerializeField] TMP_Text turnText;
    [SerializeField] GameObject menuPanel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] Transform buttonContainer;
    [SerializeField] Button buttonPrefab;

    [SerializeField] GameObject resultPanel;
    [SerializeField] TMP_Text resultText;

    [SerializeField] TargetCursor targetCursor;

    readonly List<Button> spawnedButtons = new List<Button>();

    void Awake()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (turnManager == null)
        {
            Debug.LogError("BattleMenuUI: no hay ningún TurnManager en la escena.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (turnManager != null) turnManager.OnUIRefresh += Refresh;
    }

    void OnDisable()
    {
        if (turnManager != null) turnManager.OnUIRefresh -= Refresh;
    }

    void Start()
    {
        resultPanel.SetActive(false);
        if (targetCursor != null) targetCursor.Hide();
        Refresh();
    }

    void Update()
    {
        if (!menuPanel.activeSelf || spawnedButtons.Count == 0 || EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        bool selectionLost = selected == null
            || !selected.activeInHierarchy
            || !spawnedButtons.Exists(b => b.gameObject == selected);

        if (selectionLost) SelectFirstButton();
    }

    void Refresh()
    {
        ClearButtons();
        if (targetCursor != null) targetCursor.Hide();

        if (turnManager.IsBattleOver)
        {
            menuPanel.SetActive(false);
            turnText.text = "";
            resultPanel.SetActive(true);
            resultText.text = turnManager.ResultText;
            return;
        }

        BattleUnit unit = turnManager.CurrentUnit;
        turnText.text = unit != null ? $"Turno de {unit.unitName}" : "";

        switch (turnManager.State)
        {
            case MenuState.ChoosingAction:
                BuildActionMenu();
                break;
            case MenuState.ChoosingAbility:
                BuildAbilityMenu(unit);
                break;
            case MenuState.ChoosingItem:
                BuildItemMenu();
                break;
            case MenuState.ChoosingTarget:
                BuildTargetMenu();
                break;
            default:
                menuPanel.SetActive(false);
                return;
        }

        menuPanel.SetActive(true);
        SelectFirstButton();
    }

    void BuildActionMenu()
    {
        titleText.text = "Acciones";

        AddButton("Atacar", true, turnManager.ChooseAttack);
        AddButton("Habilidades", turnManager.CurrentUnitHasAbilities, turnManager.OpenAbilities);

        Inventory inv = turnManager.PartyInventory;
        AddButton("Objetos", inv != null && inv.HasAny(), turnManager.OpenItems);
    }

    void BuildAbilityMenu(BattleUnit unit)
    {
        titleText.text = "Habilidades";

        foreach (AbilityData ability in unit.abilities)
        {
            if (ability == null) continue;
            AbilityData a = ability;
            AddButton($"{a.abilityName}  ({a.mpCost} MP)", unit.CanUse(a), () => turnManager.ChooseAbility(a));
        }

        AddButton("Volver", true, turnManager.BackToActions);
    }

    void BuildItemMenu()
    {
        titleText.text = "Objetos";

        foreach (ItemStack stack in turnManager.PartyInventory.items)
        {
            if (stack.item == null || stack.quantity <= 0) continue;
            ItemData item = stack.item;
            AddButton(turnManager.ItemLabel(stack), true, () => turnManager.ChooseItem(item));
        }

        AddButton("Volver", true, turnManager.BackToActions);
    }

    void BuildTargetMenu()
    {
        titleText.text = $"{turnManager.PendingActionName}: elegí objetivo";

        foreach (BattleUnit t in turnManager.GetCurrentTargets())
        {
            if (!t.IsAlive) continue;
            BattleUnit target = t;
            AddButton(target.unitName, true, () => turnManager.SelectTarget(target), target);
        }

        AddButton("Volver", true, turnManager.CancelTarget);
    }

    public void Cancel()
    {
        switch (turnManager.State)
        {
            case MenuState.ChoosingTarget:
                turnManager.CancelTarget();
                break;
            case MenuState.ChoosingAbility:
            case MenuState.ChoosingItem:
                turnManager.BackToActions();
                break;
        }
    }

    public void OnButtonSelected(MenuButton button)
    {
        if (targetCursor == null) return;

        if (button.target != null) targetCursor.Show(button.target);
        else targetCursor.Hide();
    }

    void AddButton(string label, bool interactable, UnityAction onClick, BattleUnit target = null)
    {
        Button button = Instantiate(buttonPrefab, buttonContainer);
        button.GetComponentInChildren<TMP_Text>().text = label;
        button.interactable = interactable;
        button.onClick.AddListener(onClick);

        MenuButton menuButton = button.gameObject.AddComponent<MenuButton>();
        menuButton.menu = this;
        menuButton.target = target;

        spawnedButtons.Add(button);
    }

    void ClearButtons()
    {
        foreach (Button b in spawnedButtons)
        {
            b.gameObject.SetActive(false);
            Destroy(b.gameObject);
        }
        spawnedButtons.Clear();
    }

    void SelectFirstButton()
    {
        Button first = spawnedButtons.Find(b => b.interactable);
        if (first != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
}