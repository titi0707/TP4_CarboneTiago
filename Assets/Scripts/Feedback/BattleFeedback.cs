using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleFeedback : MonoBehaviour
{
    public static BattleFeedback Instance { get; private set; }

    [SerializeField] TurnManager turnManager;
    [SerializeField] AudioSource sfxSource;

    [SerializeField] DamagePopup popupPrefab;
    [SerializeField] Vector3 popupOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] Color damageColor = Color.white;
    [SerializeField] Color healColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] Color mpColor = new Color(0.4f, 0.7f, 1f);

    [SerializeField] Color hitFlashColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] Color healFlashColor = new Color(0.5f, 1f, 0.5f);

    [SerializeField] CanvasGroup actionBanner;
    [SerializeField] TMP_Text actionBannerText;
    [SerializeField] float bannerDuration = 1.2f;

    [SerializeField] CameraShake cameraShake;
    [SerializeField] float shakePerDamage = 0.01f;
    [SerializeField] float maxShake = 0.3f;

    [SerializeField] AudioClip basicHitSfx;
    [SerializeField] AudioClip damageSfx;
    [SerializeField] AudioClip healSfx;
    [SerializeField] AudioClip mpSfx;
    [SerializeField] AudioClip statusSfx;
    [SerializeField] AudioClip deathSfx;
    [SerializeField] AudioClip victorySfx;
    [SerializeField] AudioClip defeatSfx;
    [SerializeField] AudioClip menuMoveSfx;
    [SerializeField] AudioClip menuConfirmSfx;
    [SerializeField] AudioClip menuCancelSfx;

    class UnitState
    {
        public int hp;
        public int mp;
        public HashSet<StatusEffectData> statuses = new HashSet<StatusEffectData>();
    }

    readonly Dictionary<BattleUnit, UnitState> states = new Dictionary<BattleUnit, UnitState>();
    Coroutine bannerRoutine;
    bool endPlayed;

    void Awake()
    {
        Instance = this;

        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (cameraShake == null && Camera.main != null) cameraShake = Camera.main.GetComponent<CameraShake>();

        if (actionBanner != null)
        {
            actionBanner.alpha = 0f;
            actionBanner.blocksRaycasts = false;
            actionBanner.interactable = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        foreach (BattleUnit u in FindObjectsByType<BattleUnit>(FindObjectsSortMode.None))
        {
            states[u] = new UnitState { hp = u.currentHP, mp = u.currentMP };

            u.OnHPChanged += HandleHPChanged;
            u.OnMPChanged += HandleMPChanged;
            u.OnModifiersChanged += HandleEffectsChanged;
            u.OnDied += HandleDied;

            if (u.GetComponent<UnitFlash>() == null)
                u.gameObject.AddComponent<UnitFlash>();
        }

        if (turnManager != null)
        {
            turnManager.OnActionStarted += HandleActionStarted;
            turnManager.OnActionImpact += HandleActionImpact;
            turnManager.OnUIRefresh += HandleUIRefresh;
        }
    }

    void HandleHPChanged(BattleUnit u)
    {
        if (!states.TryGetValue(u, out UnitState st)) return;

        int diff = u.currentHP - st.hp;
        st.hp = u.currentHP;

        UnitFlash flash = u.GetComponent<UnitFlash>();

        if (diff < 0)
        {
            SpawnPopup(u, (-diff).ToString(), damageColor);
            if (flash != null) flash.Flash(hitFlashColor);
            Play(damageSfx);
            Shake(-diff);
        }
        else if (diff > 0)
        {
            SpawnPopup(u, $"+{diff}", healColor);
            if (flash != null) flash.Flash(healFlashColor);
            Play(healSfx);
        }
    }

    void HandleMPChanged(BattleUnit u)
    {
        if (!states.TryGetValue(u, out UnitState st)) return;

        int diff = u.currentMP - st.mp;
        st.mp = u.currentMP;

        if (diff > 0)
        {
            SpawnPopup(u, $"+{diff} MP", mpColor);
            if (diff > u.mpRegenPerTurn) Play(mpSfx);
        }
    }

    void HandleEffectsChanged(BattleUnit u)
    {
        if (!states.TryGetValue(u, out UnitState st)) return;

        HashSet<StatusEffectData> current = new HashSet<StatusEffectData>();
        foreach (ActiveStatus s in u.ActiveStatuses) current.Add(s.data);

        foreach (StatusEffectData s in current)
        {
            if (!st.statuses.Contains(s))
            {
                SpawnPopup(u, s.shortLabel, s.color);
                Play(statusSfx);
            }
        }

        st.statuses = current;
    }

    void HandleDied(BattleUnit u)
    {
        Play(deathSfx);
    }

    void HandleActionStarted(BattleUnit user, string label)
    {
        if (string.IsNullOrEmpty(label)) return;
        ShowBanner(label);
    }

    void HandleActionImpact(BattleUnit user, BattleUnit target, AbilityData ability, ItemData item)
    {
        AudioClip clip = basicHitSfx;
        GameObject vfx = null;

        if (ability != null)
        {
            clip = ability.sfx;
            vfx = ability.vfxPrefab;
        }
        else if (item != null)
        {
            clip = item.sfx;
            vfx = item.vfxPrefab;
        }

        Play(clip);

        if (vfx != null && target != null)
        {
            GameObject fx = Instantiate(vfx, target.transform.position + Vector3.up, Quaternion.identity);
            Destroy(fx, 3f);
        }
    }

    void HandleUIRefresh()
    {
        if (endPlayed || turnManager == null || !turnManager.IsBattleOver) return;

        endPlayed = true;
        Play(turnManager.PlayerWon ? victorySfx : defeatSfx);
    }

    public void PlayMenuMove() => Play(menuMoveSfx);
    public void PlayMenuConfirm() => Play(menuConfirmSfx);
    public void PlayMenuCancel() => Play(menuCancelSfx);

    void Play(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    void SpawnPopup(BattleUnit u, string value, Color color)
    {
        if (popupPrefab == null) return;

        Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
        DamagePopup popup = Instantiate(popupPrefab, u.transform.position + popupOffset + jitter, Quaternion.identity);
        popup.Setup(value, color);
    }

    void Shake(int damage)
    {
        if (cameraShake == null) return;
        cameraShake.Shake(Mathf.Min(maxShake, damage * shakePerDamage), 0.2f);
    }

    void ShowBanner(string label)
    {
        if (actionBanner == null || actionBannerText == null) return;

        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(label));
    }

    IEnumerator BannerRoutine(string label)
    {
        actionBannerText.text = label;

        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            actionBanner.alpha = t / 0.15f;
            yield return null;
        }

        actionBanner.alpha = 1f;
        yield return new WaitForSeconds(bannerDuration);

        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            actionBanner.alpha = 1f - t / 0.3f;
            yield return null;
        }

        actionBanner.alpha = 0f;
        bannerRoutine = null;
    }
}