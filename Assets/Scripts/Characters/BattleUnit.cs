using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public string unitName = "Unidad";
    public bool isPlayer = true;
    public int maxHP = 100;
    public int maxMP = 20;
    public int mpRegenPerTurn = 2;
    public int attack = 15;
    public int magic = 10;
    public int defense = 5;
    public int speed = 10;

    public List<AbilityData> abilities = new List<AbilityData>();

    public Animator animator;
    public float moveSpeed = 6f;

    public int currentHP;
    public int currentMP;

    public event Action<BattleUnit> OnHPChanged;
    public event Action<BattleUnit> OnMPChanged;
    public event Action<BattleUnit> OnDied;

    public bool IsAlive => currentHP > 0;
    public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public float MPPercent => maxMP > 0 ? (float)currentMP / maxMP : 0f;

    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (animator == null) animator = GetComponentInChildren<Animator>();
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