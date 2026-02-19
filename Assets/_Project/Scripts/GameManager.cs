using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum Mode
{
    Single,
    Multiplayer
}

public class GameManager : MonoBehaviour
{
    
    public GameObject firstText,secondText,thirdText,pointerMove,pointerCut;
    public static GameManager instance;
    public bool gameplayEnabled = false;
    public Mode gameMode = Mode.Single;

    [Header("UI Elements")]
    public Transform tournamentIcon;
    public GameObject playButton;
    
    public GameObject tournamentPanel;
    public Button tourMenuButton; // Old tournament button (will be disabled)
    public Button newTournamentButton; // New unlocked tournament button
    public Button closeTourMenuButton;
    public List<Image> lockedTournament;
    public Image tournamentImage;
    public GameObject text;
    public GameObject inTournament;
    private CanvasGroup playGroup;
    private CanvasGroup gradientGroup;
    private CanvasGroup menuGroup;

    // Touch/Drag variables
    private Vector2 dragStartPos;
    private bool isDragging = false;
    private int dragFingerID = -1;
    
    [Header("GameSpeed")]
    public float solderSpeed=3f;
    public float solderSpawnRate=1f;
    public List<ParticleSystem> confettiList = new List<ParticleSystem>();
    public RectTransform levelHolderTrans;
    public Vector2 levelHolder_StartPos, levelHolder_GamePos;

    void Awake()
    {
        instance = this;
        tournamentIcon.transform.DOScale(transform.localScale * 0.9f, 0.45f).SetEase(Ease.OutBounce).SetLoops(-1, LoopType.Yoyo);
    }

    void Start()
    {
        // Setup new tournament button (initially hidden, will be shown when unlocked)
        if (newTournamentButton != null)
        {
            newTournamentButton.onClick.AddListener(OpenTournamentPanel);
        }
        
        // Setup old tournament button (visible initially)
        if (tourMenuButton != null)
        {
            tourMenuButton.onClick.AddListener(OpenTournamentPanel);
        }
        
        closeTourMenuButton.onClick.AddListener(() =>
        {
            tournamentPanel.SetActive(false);
            inTournament.SetActive(true);
            
        });
        playGroup = playButton.GetComponent<CanvasGroup>();
        if (playGroup == null) playGroup = playButton.AddComponent<CanvasGroup>();
        if(levelHolderTrans != null)levelHolder_StartPos = levelHolderTrans.anchoredPosition;

    }

    public void setLevelHolderPosition_Game ()
    {
        if (levelHolderTrans != null)
        {
            levelHolderTrans.anchoredPosition = levelHolder_GamePos;
            levelHolderTrans.transform.GetChild(0).gameObject.SetActive(false);
            levelHolderTrans.transform.GetChild(1).gameObject.SetActive(true);
        } 

    }
    public void setLevelHolderPosition_Start()
    {
        if (levelHolderTrans != null)
        {
            levelHolderTrans.anchoredPosition = levelHolder_StartPos;
            levelHolderTrans.transform.GetChild(1).gameObject.SetActive(false);
            levelHolderTrans.transform.GetChild(0).gameObject.SetActive(true);
        } 

    }

    /// <summary>
    /// Open the tournament panel with smooth animation
    /// </summary>
    public void OpenTournamentPanel()
    {
        tournamentPanel.SetActive(true);
        
        // Add fade effect
        CanvasGroup canvasGroup = inTournament.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = inTournament.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
        inTournament.transform.localScale = Vector3.zero;
        
        // Fade in and scale up from center (same as win panel)
        canvasGroup.DOFade(1f, 0.5f).SetUpdate(true);
        inTournament.transform
            .DOScale(1f, 0.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
    
 
 
    public void EnableTournamentBtn()
    {
        // Disable the old tournament button
        if (tourMenuButton != null)
        {
            tourMenuButton.gameObject.SetActive(false);
        }
        
        // Enable the new tournament button
        if (newTournamentButton != null)
        {
            newTournamentButton.gameObject.SetActive(true);
        }
        
        tournamentImage.gameObject.SetActive(true);
        text.SetActive(false);
        foreach (var i in lockedTournament)
        {
            i.gameObject.SetActive(false);
        }
    }

    public void setToMultiplayerMode ()
    {
        gameMode = Mode.Multiplayer;
    }
    public void reloadLevel ()
    {
        SceneManager.LoadScene("Main");
    }
}