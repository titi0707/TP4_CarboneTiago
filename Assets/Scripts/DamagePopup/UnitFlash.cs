using System.Collections;
using UnityEngine;

public class UnitFlash : MonoBehaviour
{
    [SerializeField] float duration = 0.15f;

    Renderer[] renderers;
    MaterialPropertyBlock block;
    Coroutine routine;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    public void Flash(Color tint)
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FlashRoutine(tint));
    }

    IEnumerator FlashRoutine(Color tint)
    {
        Apply(tint);
        yield return new WaitForSeconds(duration);
        Clear();
        routine = null;
    }

    void Apply(Color tint)
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            Material[] mats = r.sharedMaterials;

            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                block.Clear();
                if (m.HasProperty("_BaseColor")) block.SetColor("_BaseColor", m.GetColor("_BaseColor") * tint);
                if (m.HasProperty("_Color")) block.SetColor("_Color", m.GetColor("_Color") * tint);
                r.SetPropertyBlock(block, i);
            }
        }
    }

    void Clear()
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                block.Clear();
                r.SetPropertyBlock(block, i);
            }
        }
    }
}