using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ProfileNameChanger : MonoBehaviour
{
    [Header("Input Field References")]
    public TMP_InputField nameInputFieldTMP; // TextMeshPro InputField
    public int maxNameLength = 12; // Maximum characters allowed in name

    [Header("Buttons")]
    public Button editButton; // The edit/pencil icon button
    public Button confirmButton; // OK button to confirm name change
    
    [Header("Animation")]
    public GameObject profilePanel; // The panel to animate

    private bool isEditing = false;

    private void Start()
    {
        // Add button listeners
        if (editButton != null)
            editButton.onClick.AddListener(OnEditButtonClicked);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmNameChange);

        // Set character limit on input field
        if (nameInputFieldTMP != null)
        {
            nameInputFieldTMP.characterLimit = maxNameLength;
        }

        // Initialize with current player name
        LoadCurrentName();
        
        // Make input field non-interactable initially
        if (nameInputFieldTMP != null)
            nameInputFieldTMP.interactable = false;
        
        // Hide confirm button initially
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Load the current player name into the input field
    /// </summary>
    private void LoadCurrentName()
    {
        if (PlayerProfile.Instance != null && nameInputFieldTMP != null)
        {
            Debug.Log("Profile Exist");
            string currentName = PlayerProfile.Instance.GetPlayerName();
            nameInputFieldTMP.text = currentName;
        }
    }

    /// <summary>
    /// Called when edit button is clicked
    /// </summary>
    public void OnEditButtonClicked()
    {
        if (nameInputFieldTMP == null) return;

        isEditing = true;
        
        // Make input field interactable
        nameInputFieldTMP.interactable = true;
        
        // Show confirm button
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        
        // Focus and select all text in the input field
        nameInputFieldTMP.Select();
        nameInputFieldTMP.ActivateInputField();
    }

    /// <summary>
    /// Confirm and apply the name change
    /// </summary>
    public void OnConfirmNameChange()
    {
        if (nameInputFieldTMP == null) return;

        string newName = nameInputFieldTMP.text;

        // Validate and apply
        if (!string.IsNullOrEmpty(newName.Trim()))
        {
            if (PlayerProfile.Instance != null)
            {
                PlayerProfile.Instance.ChangePlayerName(newName.Trim());
                Debug.Log("Player name changed to: " + newName.Trim());
            }

            // Disable editing mode
            isEditing = false;
            nameInputFieldTMP.interactable = false;
            
            // Hide confirm button
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Name cannot be empty!");
            // Reset to current name
            LoadCurrentName();
        }
    }

    /// <summary>
    /// Update the input field when profile panel is opened
    /// </summary>
    private void OnEnable()
    {
        LoadCurrentName();
        PlayOpenAnimation();
    }
    
    /// <summary>
    /// Play smooth popup animation when panel opens
    /// </summary>
    private void PlayOpenAnimation()
    {
        if (profilePanel == null) return;

        // Add fade effect
        CanvasGroup canvasGroup = profilePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = profilePanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        profilePanel.transform.localScale = Vector3.zero;

        // Fade in and scale up from center
        canvasGroup.DOFade(1f, 0.5f).SetUpdate(true);
        profilePanel.transform
            .DOScale(1f, 0.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}

