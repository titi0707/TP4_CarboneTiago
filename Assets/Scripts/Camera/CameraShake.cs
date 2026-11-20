using UnityEngine;

public class CameraShake : MonoBehaviour
{
    Vector3 basePosition;
    float strength;
    float duration;
    float timeLeft;

    void Awake()
    {
        basePosition = transform.localPosition;
    }

    public void Shake(float amount, float time)
    {
        if (timeLeft > 0f && amount <= strength) return;

        strength = amount;
        duration = time;
        timeLeft = time;
    }

    void LateUpdate()
    {
        if (timeLeft <= 0f) return;

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            transform.localPosition = basePosition;
            strength = 0f;
            return;
        }

        float fade = timeLeft / duration;
        transform.localPosition = basePosition + Random.insideUnitSphere * strength * fade;
    }
}