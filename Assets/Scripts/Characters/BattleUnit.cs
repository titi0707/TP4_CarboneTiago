using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveModifier
{
    public StatModifier source;
    public int turnsLeft;
    public bool skipNextTick;
}

public class ActiveStatus
{
    public StatusEffectData data;
    public int turnsLeft;
}

public class BattleUnit : MonoBehaviour
{
    public const float MaxATB = 100f;

    public CharacterData data;

    public Animator animator;
    public float moveSpeed = 6f;

    public int currentHP;
    public int currentMP;
    public float atb;

    public event Action<BattleUnit> OnHPChanged;
    public event Action<BattleUnit> OnMPChanged;
    public event Action<BattleUnit> OnDied;
    public event Action<BattleUnit> OnModifiersChanged;

    readonly List<ActiveModifier> activeModifiers = new List<ActiveModifier>();
    readonly List<ActiveStatus> activeStatuses = new List<ActiveStatus>();
    public IReadOnlyList<ActiveModifier> ActiveModifiers => activeModifiers;
    public IReadOnlyList<ActiveStatus> ActiveStatuses => activeStatuses;

    public string unitName => data != null ? data.unitName : name;
    public bool isPlayer => !(data is EnemyData);
    public int maxHP => data.maxHP;
    public int maxMP => data.maxMP;
    public int mpRegenPerTurn => data.mpRegenPerTurn;
    public List<AbilityData> abilities => data.abilities;

    public bool IsAlive => currentHP > 0;
    public bool IsReady => IsAlive && atb >= MaxATB;
    public bool IsRecovering => atb < 0f;
    public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public float MPPercent => maxMP > 0 ? (float)currentMP / maxMP : 0f;
    public float ATBPercent => Mathf.Clamp01(atb / MaxATB);

    public int Attack => GetStat(StatType.Attack);
    public int Magic => GetStat(StatType.Magic);
    public int Defense => GetStat(StatType.Defense);
    public int Speed => GetStat(StatType.Speed);

    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{name} no tiene un CharacterData asignado en BattleUnit.");
            return;
        }

        currentHP = maxHP;
        currentMP = maxMP;
        atb = UnityEngine.Random.Range(0f, MaxATB * 0.3f);
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public int GetBaseStat(StatType type)
    {
        switch (type)
        {
            case StatType.Attack: return data.attack;
            case StatType.Magic: return data.magic;
            case StatType.Defense: return data.defense;
            case StatType.Speed: return data.speed;
            default: return 0;
        }
    }

    public int GetStat(StatType type)
    {
        float value = GetBaseStat(type);

        foreach (ActiveModifier m in activeModifiers)
        {
            if (m.source.stat == type)
                value *= m.source.multiplier;
        }

        foreach (ActiveStatus s in activeStatuses)
        {
            if (s.data.affectedStat == type && s.data.statMultiplier != 1f)
                value *= s.data.statMultiplier;
        }

        int min = type == StatType.Speed ? 1 : 0;
        return Mathf.Max(min, Mathf.RoundToInt(value));
    }

    public void AddModifier(StatModifier mod, bool skipNextTick)
    {
        if (!IsAlive || mod == null) return;

        ActiveModifier existing = activeModifiers.Find(m =>
            m.source.stat == mod.stat && m.source.IsBuff == mod.IsBuff);

        if (existing != null)
        {
            existing.source = mod;
            existing.turnsLeft = mod.durationTurns;
            existing.skipNextTick = skipNextTick;
        }
        else
        {
            activeModifiers.Add(new ActiveModifier
            {
                source = mod,
                turnsLeft = mod.durationTurns,
                skipNextTick = skipNextTick
            });
        }

        Debug.Log($"{unitName}: {mod.ShortLabel()} por {mod.durationTurns} turnos");
        OnModifiersChanged?.Invoke(this);
    }

    public void TickModifiers()
    {
        if (activeModifiers.Count == 0) return;

        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            ActiveModifier m = activeModifiers[i];

            if (m.skipNextTick)
            {
                m.skipNextTick = false;
                continue;
            }

            m.turnsLeft--;
            if (m.turnsLeft <= 0)
            {
                Debug.Log($"{unitName}: termina {m.source.ShortLabel()}");
                activeModifiers.RemoveAt(i);
            }
        }

        OnModifiersChanged?.Invoke(this);
    }

    public bool HasStatus(StatusEffectData status)
    {
        return activeStatuses.Exists(s => s.data == status);
    }

    public void AddStatus(StatusEffectData status)
    {
        if (!IsAlive || status == null) return;

        ActiveStatus existing = activeStatuses.Find(s => s.data == status);

        if (existing != null)
            existing.turnsLeft = status.durationTurns;
        else
            activeStatuses.Add(new ActiveStatus { data = status, turnsLeft = status.durationTurns });

        Debug.Log($"{unitName} sufre {status.statusName} ({status.durationTurns} turnos)");
        OnModifiersChanged?.Invoke(this);
    }

    public void RemoveStatus(StatusEffectData status)
    {
        if (status == null) return;

        if (activeStatuses.RemoveAll(s => s.data == status) > 0)
        {
            Debug.Log($"{unitName} se cura de {status.statusName}");
            OnModifiersChanged?.Invoke(this);
        }
    }

    public bool ProcessStatuses()
    {
        if (activeStatuses.Count == 0) return IsAlive;

        bool canAct = true;

        for (int i = activeStatuses.Count - 1; i >= 0; i--)
        {
            ActiveStatus s = activeStatuses[i];
            StatusEffectData d = s.data;

            if (d.damagePercentPerTurn > 0f)
            {
                int damage = Mathf.Max(1, Mathf.RoundToInt(maxHP * d.damagePercentPerTurn / 100f));
                Debug.Log($"{unitName} pierde vida por {d.statusName}");
                TakeDamage(damage);
                if (!IsAlive) return false;
            }

            if (d.skipTurnChance > 0f && UnityEngine.Random.value < d.skipTurnChance)
            {
                Debug.Log($"{unitName} está afectado por {d.statusName} y no puede actuar");
                canAct = false;
            }

            s.turnsLeft--;
            if (s.turnsLeft <= 0)
            {
                Debug.Log($"{unitName}: se termina {d.statusName}");
                activeStatuses.RemoveAt(i);
            }
        }

        OnModifiersChanged?.Invoke(this);
        return canAct && IsAlive;
    }

    float StatusATBMultiplier()
    {
        float mult = 1f;
        foreach (ActiveStatus s in activeStatuses)
            mult *= s.data.atbFillMultiplier;
        return mult;
    }

    public void TickATB(float amount)
    {
        if (!IsAlive || atb >= MaxATB) return;
        atb = Mathf.Min(MaxATB, atb + Speed * amount * StatusATBMultiplier());
    }

    public void ResetATB(float recoveryCost)
    {
        atb = Mathf.Max(-MaxATB, atb - MaxATB - recoveryCost);
    }

    public void ChangeATB(float amount)
    {
        if (!IsAlive) return;
        atb = Mathf.Clamp(atb + amount, -MaxATB, MaxATB);
    }

    public void OnTurnStart()
    {
        if (!IsAlive || mpRegenPerTurn <= 0 || currentMP >= maxMP) return;
        RestoreMP(mpRegenPerTurn);
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"{unitName} recibe {amount} de daño ({currentHP}/{maxHP})");
        OnHPChanged?.Invoke(this);

        if (IsAlive)
        {
            PlayTrigger("Hit");
        }
        else
        {
            atb = 0f;
            activeModifiers.Clear();
            activeStatuses.Clear();
            OnModifiersChanged?.Invoke(this);

            Debug.Log($"{unitName} fue derrotado");
            OnDied?.Invoke(this);
            if (animator != null) PlayTrigger("Die");
            else gameObject.SetActive(false);
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"{unitName} recupera {amount} de vida ({currentHP}/{maxHP})");
        OnHPChanged?.Invoke(this);
    }

    public void RestoreMP(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        currentMP = Mathf.Min(maxMP, currentMP + amount);
        Debug.Log($"{unitName} recupera {amount} de maná ({currentMP}/{maxMP})");
        OnMPChanged?.Invoke(this);
    }

    public bool CanUse(AbilityData ability)
    {
        return ability != null && currentMP >= ability.mpCost;
    }

    public bool SpendMP(int amount)
    {
        if (currentMP < amount) return false;

        currentMP -= amount;
        OnMPChanged?.Invoke(this);
        return true;
    }

    public void PlayTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
    }

    public IEnumerator MoveTo(Vector3 destination)
    {
        if (animator != null) animator.SetBool("IsMoving", true);
        FacePoint(destination);

        while (Vector3.Distance(transform.position, destination) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = destination;
        if (animator != null) animator.SetBool("IsMoving", false);
    }

    public IEnumerator ReturnToStart()
    {
        yield return StartCoroutine(MoveTo(startPosition));
        transform.rotation = startRotation;
    }

    void FacePoint(Vector3 point)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}