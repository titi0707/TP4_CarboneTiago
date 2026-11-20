using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, ISelectHandler, ICancelHandler, IPointerEnterHandler
{
    public BattleMenuUI menu;
    public BattleUnit target;

    public void OnSelect(BaseEventData eventData)
    {
        if (menu != null) menu.OnButtonSelected(this);
    }

    public void OnCancel(BaseEventData eventData)
    {
        if (menu != null) menu.Cancel();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();
        if (button != null && button.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }
}