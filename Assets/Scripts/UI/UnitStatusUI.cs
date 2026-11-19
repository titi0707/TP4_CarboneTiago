using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatusUI : MonoBehaviour
{
    [SerializeField] BattleUnit unit;
    [SerializeField] TurnManager turnManager;

    [SerializeField] TMP_Text nameText;
    [SerializeField] Image hpFill;
    [SerializeField] TMP_Text hpText;
    [SerializeField] Image mpFill;
    [SerializeField] TMP_Text mpText;
    [SerializeField] Image atbFill;
    [SerializeField] Color atbChargingColor = new Color(0.9f, 0.75f, 0.2f);
    [SerializeField] Color atbReadyColor = Color.white;
    [SerializeField] GameObject turnIndicator;
    [SerializeField] CanvasGroup canvasGroup;

    void OnEnable()
    {
        if (unit != null)
        {
            unit.OnHPChanged += UpdateHP;
            unit.OnMPChanged += UpdateMP;
            unit.OnDied += HandleDeath;
        }
        if (turnManager != null) turnManager.OnUIRefresh += UpdateTurnIndicator;
    }

    void OnDisable()
    {
        if (unit != null)
        {
            unit.OnHPChanged -= UpdateHP;
            unit.OnMPChanged -= UpdateMP;
            unit.OnDied -= HandleDeath;
        }
        if (turnManager != null) turnManager.OnUIRefresh -= UpdateTurnIndicator;
    }

    void Start()
    {
        nameText.text = unit.unitName;

        if (mpFill != null)
            mpFill.transform.parent.gameObject.SetActive(unit.maxMP > 0);

        UpdateHP(unit);
        UpdateMP(unit);
        UpdateTurnIndicator();
    }

    void Update()
    {
        if (atbFill == null || unit == null) return;

        SetFill(atbFill, unit.ATBPercent);
        atbFill.color = unit.IsReady ? atbReadyColor : atbChargingColor;
    }

    void UpdateHP(BattleUnit u)
    {
        SetFill(hpFill, u.HPPercent);
        if (hpText != null) hpText.text = $"{u.currentHP}/{u.maxHP}";
    }

    void UpdateMP(BattleUnit u)
    {
        if (mpFill != null) SetFill(mpFill, u.MPPercent);
        if (mpText != null) mpText.text = $"{u.currentMP}/{u.maxMP}";
    }

    void SetFill(Image fill, float percent)
    {
        RectTransform rt = fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void HandleDeath(BattleUnit u)
    {
        if (canvasGroup != null) canvasGroup.alpha = 0.4f;
    }

    void UpdateTurnIndicator()
    {
        if (turnIndicator == null || turnManager == null) return;
        turnIndicator.SetActive(turnManager.CurrentUnit == unit && !turnManager.IsBattleOver);
    }
}