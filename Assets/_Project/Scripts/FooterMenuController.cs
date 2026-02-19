using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class FooterMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button[] buttons;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite highlightedSprite;

    [Header("Heights")]
    [SerializeField] private float normalHeight = 80f;
    [SerializeField] private float selectedHeight = 110f;

    [Header("Icon Animation")]
    [SerializeField] private float iconNormalScale = 1f;
    [SerializeField] private float iconSelectedScale = 2f;
    [SerializeField] private float iconMoveUpY = 20f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private int currentIndex = -1;

    private void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonClicked(index));
        }

        // Optional default selection
        OnButtonClicked(1);
    }

    public void OnButtonClicked(int selectedIndex)
    {
        if (currentIndex == selectedIndex)
            return;

        currentIndex = selectedIndex;

        for (int i = 0; i < buttons.Length; i++)
        {
            UpdateButton(buttons[i], i == selectedIndex);
        }
    }

    private void UpdateButton(Button button, bool selected)
    {
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        Image buttonImage = button.GetComponent<Image>();

        Image icon = button.transform.GetChild(0).GetComponent<Image>();
        TextMeshProUGUI label = button.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        RectTransform iconRect = icon.rectTransform;

        // Kill previous tweens
        buttonRect.DOKill();
        iconRect.DOKill();
        label.transform.DOKill();

        // Background sprite
        buttonImage.sprite = selected ? highlightedSprite : normalSprite;

        // Height animation
        float targetHeight = selected ? selectedHeight : normalHeight;
        buttonRect
            .DOSizeDelta(new Vector2(buttonRect.sizeDelta.x, targetHeight), duration)
            .SetEase(ease);

        // Icon scale animation
        float targetScale = selected ? iconSelectedScale : iconNormalScale;
        iconRect
            .DOScale(Vector3.one * targetScale, duration)
            .SetEase(ease);

        // Icon move animation (anchored position Y)
        float targetY = selected ? iconMoveUpY : 0f;
        iconRect
            .DOAnchorPosY(targetY, duration)
            .SetEase(ease);

        // Text scale animation
        Vector3 textScale = selected ? Vector3.one : Vector3.zero;
        label.transform
            .DOScale(textScale, duration)
            .SetEase(ease);
    }
}
