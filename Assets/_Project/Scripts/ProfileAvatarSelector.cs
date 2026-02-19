using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProfileAvatarSelector : MonoBehaviour
{
    [Header("UI References")]
    public Transform avatarListContainer;
    public ListAvatarButton avatarButtonPrefab;

    [Header("Confirmation Buttons")]
    public Button closeButton;

    [Header("Panel")]
    public GameObject avatarSelectionPanel;

    private List<ListAvatarButton> spawnedButtons = new List<ListAvatarButton>();

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (avatarSelectionPanel != null)
            avatarSelectionPanel.SetActive(false);
    }

    public void OpenAvatarSelector()
    {
        if (avatarSelectionPanel != null)
            avatarSelectionPanel.SetActive(true);

        RefreshAvatarList();
    }

    private void RefreshAvatarList()
    {
        // Clear old buttons
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();

        if (PlayerProfile.Instance == null) return;

        string currentId = PlayerProfile.Instance.GetCurrentAvatarId();

        foreach (var avatarData in PlayerProfile.Instance.avatarDatabase)
        {
            ListAvatarButton btn = Instantiate(avatarButtonPrefab, avatarListContainer);
            bool isUnlocked = PlayerProfile.Instance.IsAvatarUnlocked(avatarData.id);
            bool isSelected = (avatarData.id == currentId);

            btn.Setup(avatarData, isUnlocked, isSelected, OnPurchaseAvatar, OnSelectAvatar);
            spawnedButtons.Add(btn);
        }
    }

    private void OnPurchaseAvatar(string avatarId)
    {
        if (PlayerProfile.Instance == null) return;

        if (PlayerProfile.Instance.TryPurchaseAvatar(avatarId))
        {
            // If purchase successful, refresh list to unlock it
            RefreshAvatarList();
        }
        else
        {
            Debug.Log("Not enough coins to purchase avatar!");
        }
    }

    private void OnSelectAvatar(string avatarId)
    {
        if (PlayerProfile.Instance == null) return;

        PlayerProfile.Instance.SelectAvatar(avatarId);
        RefreshAvatarList();
    }

    private void ClosePanel()
    {
        if (avatarSelectionPanel != null)
            avatarSelectionPanel.SetActive(false);
    }
}
