using UnityEngine;
using UnityEngine.EventSystems;

public class CloseOnOutsideClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform content;

    public void OnPointerClick(PointerEventData eventData)
    {
        // If click is NOT inside content, close
        if (!RectTransformUtility.RectangleContainsScreenPoint(
                content,
                eventData.position,
                eventData.pressEventCamera))
        {
            ClosePanel();
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
