using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SearchingOpponentPanel : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Player 1 (Local)")]
    public Image p1Avatar;
    public Text p1Name;
    public TextMeshProUGUI p1NameTMP;

    [Header("Player 2 (Opponent)")]
    public Image p2Avatar;
    public Text p2Name;
    public TextMeshProUGUI p2NameTMP;
    public GameObject p2LoadingSpinner; // Optional spinner while searching

    [Header("Status")]
    public TextMeshProUGUI statusText;

    private void Start()
    {
        // Ensure panel is hidden on start if needed, or handled by manager
        // panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        LoadLocalPlayer();
        ResetOpponent();
        UpdateStatus("Searching for Opponent...");
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    private void LoadLocalPlayer()
    {
        if (PlayerProfile.Instance != null)
        {
            if (p1Avatar != null) p1Avatar.sprite = PlayerProfile.Instance.GetPlayerAvatar();
            
            string playerName = PlayerProfile.Instance.GetPlayerName();
            if (p1Name != null) p1Name.text = playerName;
            if (p1NameTMP != null) p1NameTMP.text = playerName;
        }
    }

    private void ResetOpponent()
    {
        // Set P2 to unknown/searching state
        if (p2Avatar != null) p2Avatar.sprite = null; // or a question mark sprite
        
        if (p2Name != null) p2Name.text = "Searching...";
        if (p2NameTMP != null) p2NameTMP.text = "Searching...";
        
        if (p2LoadingSpinner != null) p2LoadingSpinner.SetActive(true);
    }

    public void SetOpponentData(string name, Sprite avatar)
    {
        if (p2LoadingSpinner != null) p2LoadingSpinner.SetActive(false);

        if (p2Avatar != null && avatar != null) p2Avatar.sprite = avatar;
        
        if (p2Name != null) p2Name.text = name;
        if (p2NameTMP != null) p2NameTMP.text = name;
    }
}
