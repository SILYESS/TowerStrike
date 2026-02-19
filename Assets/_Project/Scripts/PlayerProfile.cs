using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile Instance;

    [Header("Profile Data")]
    public string playerName = "Player";
    public Sprite playerAvatar;
    public int coins = 1000; // Starting coins for testing
    public int gems = 0;
    public bool adsRemoved = false;

    [Header("HUD References")]
    public Image hudProfileImage;
    public Text hudPlayerNameText;
    public TextMeshProUGUI hudPlayerNameTMP;
    public TextMeshProUGUI coinsText; // UI to show coins
    public TextMeshProUGUI gemsText; // UI to show coins


    [Header("Profile Panel References")]
    public Image profilePanelImage;
    public Text profilePanelNameText;
    public TextMeshProUGUI profilePanelNameTMP;
    public Button openAvatarSelectorButton; // Button to open the selector

    [Header("External References")]
    public ProfileAvatarSelector avatarSelector; // Reference to the selector script

    [System.Serializable]
    public class AvatarData
    {
        public string id;
        public Sprite icon;
        public Sprite lockedIcon;
        public int price;
        public bool isDefaultUnlocked;
    }

    [Header("Avatar Database")]
    public List<AvatarData> avatarDatabase;

    private int currentAvatarIndex = 0;
    private List<string> unlockedAvatarIds = new List<string>();

    private const string PLAYER_NAME_KEY = "PlayerName";
    private const string PLAYER_AVATAR_KEY = "PlayerAvatarIndex";
    private const string PLAYER_COINS_KEY = "PlayerCoins";
    private const string UNLOCKED_AVATARS_KEY = "UnlockedAvatars";

    public const string PLAYER_GEMS_KEY = "PlayerGems";
    public const string ADS_REMOVED_KEY = "AdsRemoved";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadPlayerData();
    }

    private void Start()
    {
        UpdateAllUI();

        if (openAvatarSelectorButton != null)
        {
            openAvatarSelectorButton.onClick.AddListener(OnOpenAvatarSelectorClicked);
        }
    }

    private void OnOpenAvatarSelectorClicked()
    {
        if (avatarSelector != null)
        {
            avatarSelector.OpenAvatarSelector();
        }
        else
        {
            Debug.LogWarning("Avatar Selector reference is missing in PlayerProfile!");
        }
    }

    #region Save/Load

    private void LoadPlayerData()
    {
        playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
        currentAvatarIndex = PlayerPrefs.GetInt(PLAYER_AVATAR_KEY, 0);
        coins = PlayerPrefs.GetInt(PLAYER_COINS_KEY, 1000);
        gems = PlayerPrefs.GetInt(PLAYER_GEMS_KEY, 0);
        adsRemoved = PlayerPrefs.GetInt(ADS_REMOVED_KEY, 0) == 1;

        // Load unlocked avatars
        string unlockedData = PlayerPrefs.GetString(UNLOCKED_AVATARS_KEY, "");
        if (!string.IsNullOrEmpty(unlockedData))
        {
            unlockedAvatarIds = unlockedData.Split(',').ToList();
        }

        // Ensure defaults are unlocked
        foreach (var avatar in avatarDatabase)
        {
            if (avatar.isDefaultUnlocked && !unlockedAvatarIds.Contains(avatar.id))
            {
                unlockedAvatarIds.Add(avatar.id);
            }
        }

        // Set current avatar
        if (avatarDatabase != null && avatarDatabase.Count > 0)
        {
            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, avatarDatabase.Count - 1);
            playerAvatar = avatarDatabase[currentAvatarIndex].icon;
        }
    }

    private void SavePlayerData()
    {
        PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
        PlayerPrefs.SetInt(PLAYER_AVATAR_KEY, currentAvatarIndex);
        PlayerPrefs.SetInt(PLAYER_COINS_KEY, coins);
        PlayerPrefs.SetInt(PLAYER_GEMS_KEY, gems);
        PlayerPrefs.SetInt(ADS_REMOVED_KEY, adsRemoved ? 1 : 0);

        string unlockedData = string.Join(",", unlockedAvatarIds);
        PlayerPrefs.SetString(UNLOCKED_AVATARS_KEY, unlockedData);

        PlayerPrefs.Save();
    }

    #endregion

    #region Public Methods

    public void ChangePlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        playerName = newName;
        SavePlayerData();
        UpdateAllUI();
    }

    public bool IsAvatarUnlocked(string id)
    {
        return unlockedAvatarIds.Contains(id);
    }

    public bool TryPurchaseAvatar(string id)
    {
        AvatarData avatar = avatarDatabase.Find(a => a.id == id);
        if (avatar == null) return false;

        if (IsAvatarUnlocked(id)) return true; // Already owned

        if (coins >= avatar.price)
        {
            coins -= avatar.price;
            unlockedAvatarIds.Add(id);
            SavePlayerData();
            UpdateAllUI();
            return true;
        }

        return false;
    }

    public void SelectAvatar(string id)
    {
        int index = avatarDatabase.FindIndex(a => a.id == id);
        if (index != -1 && IsAvatarUnlocked(id))
        {
            currentAvatarIndex = index;
            playerAvatar = avatarDatabase[index].icon;
            SavePlayerData();
            UpdateAllUI();
        }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        SavePlayerData();
        UpdateAllUI();
    }

    public void AddGems(int amount)
    {
        if (amount <= 0) return;
        gems += amount;
        SavePlayerData();
        UpdateAllUI();
    }

    public void RemoveAds()
    {
        adsRemoved = true;
        SavePlayerData();
        // Notify AdsManager if needed, or check getter
    }

    public void UpdateAllUI()
    {
        UpdateHUD();
        UpdateProfilePanel();
    }

    #endregion

    #region UI Updates

    private void UpdateHUD()
    {
        if (hudProfileImage != null) hudProfileImage.sprite = playerAvatar;
        if (hudPlayerNameText != null) hudPlayerNameText.text = playerName;
        if (hudPlayerNameTMP != null) hudPlayerNameTMP.text = playerName;
        if (coinsText != null) coinsText.text = coins.ToString();
        if (gemsText != null) gemsText.text = gems.ToString();

    }

    private void UpdateProfilePanel()
    {
        if (profilePanelImage != null) profilePanelImage.sprite = playerAvatar;
        if (profilePanelNameText != null) profilePanelNameText.text = playerName;
        if (profilePanelNameTMP != null) profilePanelNameTMP.text = playerName;
    }

    #endregion

    #region Getters

    public string GetPlayerName() => playerName;
    public Sprite GetPlayerAvatar() => playerAvatar;
    public int GetCoins() => coins;
    public int GetGems() => gems;
    public bool HasRemovedAds() => adsRemoved;

    public string GetCurrentAvatarId()
    {
        if (currentAvatarIndex >= 0 && currentAvatarIndex < avatarDatabase.Count)
        {
            return avatarDatabase[currentAvatarIndex].id;
        }
        return "";
    }

    #endregion
}
