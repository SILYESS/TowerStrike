using Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playButton;
    public Button cancelButton;
    public GameObject matchmakingPanel;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        if (matchmakingPanel != null)
            matchmakingPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnMatchmakingStatus += UpdateStatus;
            MultiplayerManager.Instance.OnOpponentFound += OnOpponentFound;
        }
    }

    private void OnDisable()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnMatchmakingStatus -= UpdateStatus;
            MultiplayerManager.Instance.OnOpponentFound -= OnOpponentFound;
        }
    }

    private void OnPlayClicked()
    {
        matchmakingPanel.SetActive(true);
        playButton.interactable = false;
        if (cancelButton != null) cancelButton.interactable = true;
        GameManager.instance.setToMultiplayerMode();
        MultiplayerManager.Instance.StartMatchmaking();
    }

    private void OnCancelClicked()
    {
        QuitTournamentMatch();
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    private void OnOpponentFound()
    {
        if (statusText != null)
            statusText.text = "BATTLE AGAINST";

        if (cancelButton != null)
            cancelButton.interactable = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            QuitTournamentMatch();
        }
    }

    public void QuitTournamentMatch()
    {
        Debug.Log("Quitting multiplayer match...");

        // Stop the Fusion runner
        if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.runner != null)
        {
            MultiplayerManager.Instance.runner.Shutdown();
        }

        matchmakingPanel.SetActive(false);
        playButton.interactable = true;
        if (cancelButton != null) cancelButton.interactable = true;
        
        // Only load main scene if we are actually in the game scene, otherwise just reset UI
        if (SceneManager.GetActiveScene().name != "Main")
             SceneManager.LoadScene("Main");
    }
}