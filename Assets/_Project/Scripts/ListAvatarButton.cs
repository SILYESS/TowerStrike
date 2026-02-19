using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ListAvatarButton : MonoBehaviour
{
    [Header("Images")]
    public Image unlockediImage; // The avatar image when unlocked
    public Image lockedImage;    // The avatar image when locked

    [Header("Buttons")]
    public Button purchaseButton;
    public Button selectButton;

    [Header("Texts")]
    public TextMeshProUGUI priceText;

    [Header("Overlays")]
    public GameObject selectedOverlay;

    private string avatarId;
    private Action<string> onPurchaseCallback;
    private Action<string> onSelectCallback;

    public void Setup(PlayerProfile.AvatarData data, bool isUnlocked, bool isSelected, Action<string> onPurchase, Action<string> onSelect)
    {
        avatarId = data.id;
        onPurchaseCallback = onPurchase;
        onSelectCallback = onSelect;

        // Setup Images
        if (unlockediImage != null)
        {
            unlockediImage.sprite = data.icon;
            unlockediImage.gameObject.SetActive(isUnlocked);
        }

        if (lockedImage != null)
        {
            lockedImage.sprite = data.lockedIcon;
            lockedImage.gameObject.SetActive(!isUnlocked);
        }

        // Setup Buttons
        if (purchaseButton != null)
        {
            purchaseButton.gameObject.SetActive(!isUnlocked);
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(isUnlocked);
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }

        // Setup Price
        if (priceText != null)
        {
            priceText.gameObject.SetActive(!isUnlocked);
            priceText.text = data.price.ToString();

            // Preserve user's child coin icon logic if it exists
            if (priceText.transform.childCount > 0)
            {
                priceText.transform.GetChild(0).gameObject.SetActive(!isUnlocked);
            }
        }

        // Setup Selection Overlay
        if (selectedOverlay != null)
        {
            selectedOverlay.SetActive(isSelected);
        }
    }

    private void OnPurchaseClicked()
    {
        onPurchaseCallback?.Invoke(avatarId);
    }

    private void OnSelectClicked()
    {
        onSelectCallback?.Invoke(avatarId);
    }
}
