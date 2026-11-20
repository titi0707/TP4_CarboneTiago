using UnityEngine;

public class TargetCursor : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] float bobAmplitude = 0.15f;
    [SerializeField] float bobSpeed = 4f;
    [SerializeField] float spinSpeed = 90f;

    BattleUnit target;

    public void Show(BattleUnit unit)
    {
        target = unit;
        gameObject.SetActive(unit != null);
    }

    public void Hide()
    {
        target = null;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = target.transform.position + offset + Vector3.up * bob;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}