using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] float lifetime = 1f;
    [SerializeField] float riseSpeed = 1.5f;
    [SerializeField] float popScale = 1.4f;

    float timer;
    Color baseColor;
    Vector3 baseScale;
    Camera cam;

    void Awake()
    {
        if (text == null) text = GetComponentInChildren<TMP_Text>();
        baseScale = transform.localScale;
        cam = Camera.main;
    }

    public void Setup(string value, Color color)
    {
        text.text = value;
        text.color = color;
        baseColor = color;
        transform.localScale = baseScale * popScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;
        transform.localScale = baseScale * Mathf.Lerp(popScale, 1f, Mathf.Clamp01(t * 4f));

        Color c = baseColor;
        c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
        text.color = c;

        if (cam != null) transform.rotation = cam.transform.rotation;

        if (timer >= lifetime) Destroy(gameObject);
    }
}