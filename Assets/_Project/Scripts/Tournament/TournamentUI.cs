using System;
using System.Collections;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentUI : MonoBehaviour
{
    [Header("Manager")]
    public TournamentManager manager;

    [Header("UI References")]
    public GameObject tournamentPanel;
    public Button playButton;
    public TextMeshProUGUI playButtonText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    [Header("Bracket (Visualization Only)")]
    // Assign these in inspector to visualize progress
    public GameObject[] roundHighlights; // 0=R16, 1=QF, 2=SF, 3=Final

    [Header("Overlays")]
    public GameObject eliminatedOverlay;
    public GameObject winnerOverlay;

    private void Start()
    {
        if (manager == null) manager = TournamentManager.Instance;
        
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        InvokeRepeating(nameof(UpdateTimer), 0f, 1f);
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (manager == null) return;

        // Reset overlays
        if (eliminatedOverlay != null) eliminatedOverlay.SetActive(false);
        if (winnerOverlay != null) winnerOverlay.SetActive(false);

        // Check Status
        if (manager.IsEliminated)
        {
            if (eliminatedOverlay != null) eliminatedOverlay.SetActive(true);
            playButton.interactable = false;
            if (statusText != null) statusText.text = "ELIMINATED";
        }
        else if (manager.IsWinner)
        {
            if (winnerOverlay != null) winnerOverlay.SetActive(true);
            playButton.interactable = false;
            if (statusText != null) statusText.text = "CHAMPION!";
        }
        else
        {
            playButton.interactable = true;
            if (playButtonText != null) playButtonText.text = "PLAY ROUND " + (manager.CurrentRound + 1);
            if (statusText != null) statusText.text = GetRoundName(manager.CurrentRound);
        }

        // Bracket Highlights
        if (roundHighlights != null)
        {
            for (int i = 0; i < roundHighlights.Length; i++)
            {
                if (roundHighlights[i] != null)
                {
                    // Simple logic: Highlight if this is the current round
                    roundHighlights[i].SetActive(i == manager.CurrentRound);
                }
            }
        }
    }

    private string GetRoundName(int round)
    {
        switch (round)
        {
            case 0: return "Round of 16";
            case 1: return "Quarter-Finals";
            case 2: return "Semi-Finals";
            case 3: return "Finals";
            default: return "Complete";
        }
    }

    private void OnPlayClicked()
    {
        if (manager.TryStartMatch())
        {
            // Start Multiplayer Matchmaking
            // We assume MultiplayerManager is available or handled by GameManager integration
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.StartMatchmaking();
                // Close tournament panel if needed? Or let matchmaking UI overlay it.
                // gameObject.SetActive(false); 
            }
            else
            {
                Debug.LogError("MultiplayerManager not found!");
            }
        }
    }

    private void UpdateTimer()
    {
        if (timerText != null)
        {
            DateTime now = DateTime.Now;
            DateTime tomorrow = now.Date.AddDays(1);
            TimeSpan timeUntilReset = tomorrow - now;
            timerText.text = $"Start in: {timeUntilReset.Hours:D2}:{timeUntilReset.Minutes:D2}:{timeUntilReset.Seconds:D2}";
        }
    }
}
