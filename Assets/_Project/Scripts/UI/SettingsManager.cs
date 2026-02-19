using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("UI References")]
    public GameObject settingsPanel;
    public Toggle musicToggle;
    public Toggle soundToggle;
    public Toggle vibrationToggle;
    public Button supportButton;
    public Button privacyPolicyButton;
    public Button closeButton;
    public AudioSource musicSource;
    public AudioSource soundSource;
    public GameObject musicOnSprite, musicOffSprite, SoundOnSprite, SoundOffSprite, vibrationOnSprite, vibrationOffSprite;
    
    [Header("URLs")]
    public string supportUrl = "https://example.com/support";
    public string privacyPolicyUrl = "https://example.com/privacy";

    // PlayerPrefs Keys
    private const string MUSIC_KEY = "MusicEnabled";
    private const string SOUND_KEY = "SoundEnabled";
    private const string VIBRATION_KEY = "VibrationEnabled";

    // Static Accessors
    public static bool IsMusicEnabled
    {
        get => PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
        set => PlayerPrefs.SetInt(MUSIC_KEY, value ? 1 : 0);
    }

    public static bool IsSoundEnabled
    {
        get => PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        set => PlayerPrefs.SetInt(SOUND_KEY, value ? 1 : 0);
    }

    public static bool IsVibrationEnabled
    {
        get => PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;
        set => PlayerPrefs.SetInt(VIBRATION_KEY, value ? 1 : 0);
    }

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
        }
    }

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (musicToggle != null)
        {
            musicToggle.isOn = IsMusicEnabled;
            musicSource.mute = !IsMusicEnabled;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        if (soundToggle != null)
        {
            soundToggle.isOn = IsSoundEnabled;
            soundSource.mute = !IsSoundEnabled;
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.isOn = IsVibrationEnabled;
            MOST_HapticFeedback.HapticsEnabled = IsVibrationEnabled;
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
        }

        ApplySettings();
    }


    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    #region Toggles

    // Logic: Toggle represents "MUTE" or "OFF". 
    // IsOn = true => Muted/Off => Enabled = false
    
    private void OnMusicToggleChanged(bool isOn)
    {
        IsMusicEnabled = isOn;
        musicOffSprite.SetActive(isOn);
        musicOnSprite.SetActive(!isOn);
        if (musicSource != null)
            musicSource.mute = !isOn;
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        IsSoundEnabled = isOn;
        SoundOffSprite.SetActive(isOn);
        SoundOnSprite.SetActive(!isOn);
        if (soundSource != null)
            soundSource.mute = !isOn;
    }

    private void OnVibrationToggleChanged(bool isOn)
    {
        IsVibrationEnabled = isOn;
        vibrationOffSprite.SetActive(isOn);
        vibrationOnSprite.SetActive(!isOn);
        MOST_HapticFeedback.HapticsEnabled = isOn;
    }


    #endregion

    #region Buttons

    private void OnSupportClicked()
    {
        Application.OpenURL(supportUrl);
    }

    private void OnPrivacyClicked()
    {
        Application.OpenURL(privacyPolicyUrl);
    }

    #endregion

    private void ApplySettings()
    {
       PlayerPrefs.Save();
    }
}
