using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, ISelectHandler, ICancelHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    public BattleMenuUI menu;
    public BattleUnit target;

    public void OnSelect(BaseEventData eventData)
    {
        if (menu != null) menu.OnButtonSelected(this);
        if (BattleFeedback.Instance != null) BattleFeedback.Instance.PlayMenuMove();
    }

    public void OnCancel(BaseEventData eventData)
    {
        if (BattleFeedback.Instance != null) BattleFeedback.Instance.PlayMenuCancel();
        if (menu != null) menu.Cancel();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayConfirm();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayConfirm();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();
        if (button != null && button.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    void PlayConfirm()
    {
        Button button = GetComponent<Button>();
        if (button != null && button.interactable && BattleFeedback.Instance != null)
            BattleFeedback.Instance.PlayMenuConfirm();
    }
}