using System.Collections.Generic;
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
    [SerializeField] Color atbRecoveringColor = new Color(0.85f, 0.25f, 0.25f);
    [SerializeField] TMP_Text modifiersText;
    [SerializeField] GameObject turnIndicator;
    [SerializeField] CanvasGroup canvasGroup;

    void OnEnable()
    {
        if (unit != null)
        {
            unit.OnHPChanged += UpdateHP;
            unit.OnMPChanged += UpdateMP;
            unit.OnDied += HandleDeath;
            unit.OnModifiersChanged += UpdateModifiers;
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
            unit.OnModifiersChanged -= UpdateModifiers;
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
        UpdateModifiers(unit);
        UpdateTurnIndicator();
    }

    void Update()
    {
        if (atbFill == null || unit == null) return;

        if (unit.IsRecovering)
        {
            SetFill(atbFill, -unit.atb / BattleUnit.MaxATB);
            atbFill.color = atbRecoveringColor;
        }
        else
        {
            SetFill(atbFill, unit.ATBPercent);
            atbFill.color = unit.IsReady ? atbReadyColor : atbChargingColor;
        }
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

    void UpdateModifiers(BattleUnit u)
    {
        if (modifiersText == null) return;

        List<string> parts = new List<string>();

        foreach (ActiveStatus s in u.ActiveStatuses)
        {
            string hex = ColorUtility.ToHtmlStringRGB(s.data.color);
            parts.Add($"<color=#{hex}>{s.data.shortLabel} ({s.turnsLeft})</color>");
        }

        foreach (ActiveModifier m in u.ActiveModifiers)
        {
            string hex = m.source.IsBuff ? "7CFF7C" : "FF7C7C";
            parts.Add($"<color=#{hex}>{m.source.ShortLabel()} ({m.turnsLeft})</color>");
        }

        modifiersText.text = string.Join("  ", parts);
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