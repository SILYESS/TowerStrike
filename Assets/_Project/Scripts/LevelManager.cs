using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using Towers;
using System.Linq;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    [Header("Maps")]
    public GameObject[] maps;
    [Header("Tower Colors")]
    public Color greyColor;
    public Color greenColor;
    public Color redColor;
    public Color yellowColor;
    public Color blueColor;
    [Header("Line Colors")]
    public Color greyLineColor;
    public Color greenLineColor;
    public Color redLineColor;
    public Color yellowLineColor;
    public Color blueLineColor;

    [Header("Level Data")]
    public LevelData[] levels;
    public int whenToReachTournamentLevel = 2; // Level 3 (index 2)
    public int playerTowers, enemyTowers;
    public Transform levelCamera;
    public List<GameObject> mapMultiplayer;

    [Header("UI References")]
    public Transform towersParent;
    public Transform treessParent;
    public Text levelText;
    public GameObject levelCompletePanel;
    public Button nextLevelButton;
    public Button claimButton;
    public GameObject inPanel;
    public GameObject losePanel;
    public Image blueFill;
    public Image redFill;
    public Image greenFill;
    public Image yellowFill;

    [Header("Tournament Unlock")]
    public GameObject tournamentLockButton; // The locked tournament button to disable when unlocked
    public GameObject tournamentUnlockButton; // The unlocked tournament button (with actual button) to enable

    [Header("Win Animation")]
    //public Image[] stars; // Assign the 3 star images
    public Image glowBackground; // Assign the glow/background image behind stars
    public float glowPulseDuration = 2f; // Duration for one glow pulse cycle (higher = slower)
    public float starAnimationDuration = 0.6f; // Duration for each star animation (higher = slower)
    public float starDelay = 0.2f; // Delay between each star appearing
    [Header("Win Star VFX")]
    private int lastStarSpawnIndex = -1;
    private Coroutine starLoopCoroutine;
    private List<GameObject> spawnedStars = new List<GameObject>();
    public GameObject starVfxPrefab;
    public Transform[] starSpawnPoints; // assign in inspector
    public int randomStarsToSpawn = 5;
    public Vector2 randomOffsetRange = new Vector2(-0.5f, 0.5f);
    [Space(5)]

    public int currentLevel = 0;
    public List<GameObject> towerPool = new List<GameObject>();
    private List<GameObject> obstaclesPool = new List<GameObject>();
    public bool levelCompleted = false;
    private AITowerController aiController;
    public Image bluePercentageFilled;
    public float bluePercentage;
    public bool testGame;
    public TowerBehavior starterTower;

    // --- Global Registry ---
    public readonly List<TowerBehavior> allTowers = new List<TowerBehavior>();
    public readonly List<Soldier.SoldierBehavior> allSoldiers = new List<Soldier.SoldierBehavior>();
    public readonly List<bulletBehaviour> allBullets = new List<bulletBehaviour>();
    public readonly List<LrBehavior> allLines = new List<LrBehavior>();
    public readonly List<CuttingLineRenderer> allTrails = new List<CuttingLineRenderer>();
    // -----------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        //currentLevel = PlayerPrefs.GetInt("currentLevel", 0);
        currentLevel = FirebaseManager.Instance.Data.currentLevel;
        if (GameManager.instance.gameMode == Mode.Single)
        {
            if (currentLevel > 30)
            {
                maps[1].SetActive(true);
            }
            else
            {
                maps[0].SetActive(true);
            }
        }
       
        aiController = FindObjectOfType<AITowerController>();
        if (GameManager.instance.gameMode == Mode.Multiplayer)
        {
            if (aiController != null)
            {
                aiController.enabled = false;
                //this.enabled = false;
                return;
            }
            return;
        }
        else
        {
            OnRetryLevelClicked();
        }


        if (aiController == null)
        {
            GameObject aiObj = new GameObject("AIController");
            aiController = aiObj.AddComponent<AITowerController>();
        }

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        // Set initial lock state for tournament
        if (tournamentLockButton != null)
        {
            tournamentLockButton.SetActive(true); // Show locked button initially
        }

        //OnRetryLevelClicked();
        if (currentLevel == 0)
        {
            towerPool[2].SetActive(false);
            towerPool[3].SetActive(false);
        }

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.HideBannerAd();
        }
    }

    private void Update()
    {
        if (!GameManager.instance.gameplayEnabled || levelCompleted)
            return;


        bool anyActive = false;

        bool allBlue = true;
        bool allRed = true;
        bool allGreen = true;

        foreach (var towerObj in towerPool)
        {
            if (!towerObj.activeSelf)
                continue;

            anyActive = true;

            var tower = towerObj.GetComponent<TowerBehavior>();
            if (tower == null)
                continue;

            // Check their color/type
            switch (tower.type)
            {
                case TowerType.PlayerTower:      // Blue
                    allRed = false;
                    allGreen = false;
                    break;

                case TowerType.RedTower:         // Red
                    allBlue = false;
                    allGreen = false;
                    break;

                case TowerType.GreenTower:       // Green
                    allBlue = false;
                    allRed = false;
                    break;

                default:
                    allBlue = false;
                    allRed = false;
                    allGreen = false;
                    break;
            }
        }

        if (!anyActive) return;

        // 🟦 All blue = WIN
        if (allBlue)
        {
            OnLevelCompleted();
            return;
        }

        bool noBlue = true;
        foreach (var towerObj in towerPool)
        {
            if (!towerObj.activeSelf) continue;

            var tower = towerObj.GetComponent<TowerBehavior>();
            if (tower == null) continue;

            if (tower.type == TowerType.PlayerTower)
            {
                noBlue = false;
                break;
            }
        }

        if (noBlue)
        {
            ShowLosePopup("No blue towers left!");
            levelCompleted = true;
            return;
        }

    }

    private void ShowLosePopup(string allTowersBecameGreen)
    {
        losePanel.SetActive(true);
    }


    #region Load Level

    public void LoadLevel()
    {
        ResetLevelState();

        //if (currentLevel < 0 || currentLevel >= levels.Length) return;
        var index = currentLevel % levels.Length;
        Debug.Log("Index : " + index);
        //currentLevel = levelIndex;
        levelCompleted = false;
        levelText.text = "LEVEL " + (currentLevel + 1);

        // Check if tournament should be unlocked based on current level
        if (currentLevel >= whenToReachTournamentLevel)
        {
            UnlockTournament();
        }

        var levelData = levels[index];

        //Set Camera Position And Rotation
        //levelCamera.DOMove(levelData.cameraPosition, 1);
        //levelCamera.DORotate(levelData.cameraRotation, 1);

        // Hide old towers
        foreach (var t in towerPool)
        {
            if (t != null)
            {
                t.SetActive(false);
                StopTowerLines(t);
            }
        }

        foreach (var t in obstaclesPool)
        {
            if (t != null)
            {
                t.SetActive(false);

            }
        }

        // Expand pool if needed
        while (towerPool.Count < levelData.towers.Length)
        {
            int idx = towerPool.Count;
            var prefab = levelData.towers[idx].towerPrefab;
            var newTower = Instantiate(prefab, Vector3.zero, Quaternion.identity, towersParent);
            towerPool.Add(newTower);
        }

        while (obstaclesPool.Count < levelData.trees.Length)
        {
            int idx = obstaclesPool.Count;
            var prefab = levelData.trees[idx].treePrefab;
            var newTower = Instantiate(prefab, Vector3.zero, Quaternion.identity, treessParent);
            obstaclesPool.Add(newTower);
        }

        // Activate towers
        for (int i = 0; i < levelData.towers.Length; i++)
        {
            var towerObj = towerPool[i];
            var data = levelData.towers[i];
            towerObj.transform.position = data.position;
            towerObj.transform.localEulerAngles = data.rotation;
            towerObj.SetActive(true);

            var tower = towerObj.GetComponent<TowerBehavior>();

            if (tower != null)
            {

                tower.maxHealth = data.maxHealth;
                tower.isStarterTower = data.isStarterTower;
                tower.linkedTowers.Clear();
                tower.Initialize(data.type);
                tower.resetTowerConnections();
                tower.InitTower(data.solderSpawnRate, data.startHealth);
                tower.SetAttackEnabled(true);
                tower.spawnCooldown = data.solderSpawnRate;
                if (tower.isStarterTower)
                {
                    starterTower = tower;
                }
                if (tower.isStarterTower && GameManager.instance.gameplayEnabled)
                {
                    tower.PunchRoundedSprite(tower);
                }
            }

            if (currentLevel == 0)
            {
                if (i == 2 || i == 3)
                {
                    towerObj.SetActive(false);
                }
            }
        


        }


        for (int i = 0; i < levelData.trees.Length; i++)
        {
            var treeObj = obstaclesPool[i];
            var data = levelData.trees[i];
            treeObj.transform.position = data.position;
            treeObj.transform.localEulerAngles = data.rotation;
            treeObj.SetActive(true);




        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (aiController != null)
        {
            aiController.StopAI();
            // Don't start AI immediately on load. Wait for PlayGame()
            // aiController.FindAITowers(); 
            // aiController.StartAI(); 
        }

        

        UpdatePercentageUI();
    }

    #endregion

    private void StopTowerLines(GameObject towerObj)
    {
        var lines = towerObj.GetComponentsInChildren<LineRenderer>();
        foreach (var lr in lines)
            lr.enabled = false;
    }

    #region Completion

    public void CheckLevelCompletion()
    {
        if (levelCompleted) return;

        // Condition: all active towers must be PLAYER towers
        bool allBlue = true;

        foreach (var towerObj in towerPool)
        {
            if (towerObj.activeSelf)
            {
                var tower = towerObj.GetComponent<TowerBehavior>();
                if (tower == null || tower.type != TowerType.PlayerTower)
                {
                    allBlue = false;
                    break;
                }
            }
        }

        if (allBlue)
            OnLevelCompleted();
    }

    private void PlayStarVFX()
    {
        // Stop existing loop if any
        if (starLoopCoroutine != null)
            StopCoroutine(starLoopCoroutine);

        starLoopCoroutine = StartCoroutine(StarVFXSequence());
    }

    private IEnumerator StarVFXSequence()
    {
        if (starVfxPrefab == null || starSpawnPoints == null || starSpawnPoints.Length == 0)
            yield break;

        while (true)
        {
            for (int i = 0; i < randomStarsToSpawn; i++)
            {
                int spawnIndex;
                // Pick a random index that is different from the last one
                do
                {
                    spawnIndex = Random.Range(0, starSpawnPoints.Length);
                } while (starSpawnPoints.Length > 1 && spawnIndex == lastStarSpawnIndex);

                lastStarSpawnIndex = spawnIndex;

                Transform spawnPoint = starSpawnPoints[spawnIndex];
                Vector3 randomPos = spawnPoint.position + new Vector3(
                    Random.Range(randomOffsetRange.x, randomOffsetRange.y),
                    Random.Range(randomOffsetRange.x, randomOffsetRange.y),
                    Random.Range(randomOffsetRange.x, randomOffsetRange.y)
                );

                SpawnStar(randomPos);

                yield return new WaitForSeconds(0.4f);
            }

            yield return new WaitForSeconds(1f);
            DestroyAllSpawnedStars();
        }
    }



    private IEnumerator StarVFXLoop()
    {
        while (true)
        {
            yield return StartCoroutine(SpawnRandomStars());
            // Wait for 1 second or whatever matches your star animation duration
            yield return new WaitForSeconds(2.5f);
            DestroyAllSpawnedStars();
        }
    }


    private IEnumerator SpawnRandomStars()
    {
        if (starVfxPrefab == null || starSpawnPoints == null || starSpawnPoints.Length == 0)
            yield break;

        for (int i = 0; i < randomStarsToSpawn; i++)
        {
            Transform spawnPoint = starSpawnPoints[Random.Range(0, starSpawnPoints.Length)];
            Vector3 randomPos = spawnPoint.position + new Vector3(
                Random.Range(randomOffsetRange.x, randomOffsetRange.y),
                Random.Range(randomOffsetRange.x, randomOffsetRange.y),
                Random.Range(randomOffsetRange.x, randomOffsetRange.y)
            );

            SpawnStar(randomPos);
        }
    }

    private void SpawnStar(Vector3 position)
    {
        GameObject star = Instantiate(starVfxPrefab, position, Quaternion.identity);
        star.transform.localScale *= 1.5f;
        spawnedStars.Add(star);

        var ps = star.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.loop = false; // disable automatic loop
            ps.Play();
        }
    }

    private void DestroyAllSpawnedStars()
    {
        foreach (var star in spawnedStars)
        {
            if (star != null) Destroy(star);
        }
        spawnedStars.Clear();
    }



    private void OnLevelCompleted()
    {
        blueFill.DOFillAmount(1, 0.15f);
        GameManager.instance.gameplayEnabled = false;
        levelCompleted = true;

        if (currentLevel == 0 && GameManager.instance.thirdText.activeInHierarchy)
        {
            GameManager.instance.thirdText.SetActive(false);
        }
        // Show Interstitial Ad
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowInterstitialAd();
        }

        // Award Coins
        if (levels != null && levels.Length > 0)
        {
            var levelData = levels[currentLevel % levels.Length];
            if (levelData != null && PlayerProfile.Instance != null)
            {
                PlayerProfile.Instance.AddCoins(levelData.coinsReward);
                Debug.Log($"Level Complete! Awarded {levelData.coinsReward} coins.");
            }
            FirebaseManager.Instance.AddCoins(levelData.coinsReward);
        }

        foreach (var towerObj in towerPool)
        {
            if (towerObj.activeSelf)
            {
                towerObj.GetComponent<TowerBehavior>()?.SetAttackEnabled(false);
                StopTowerLines(towerObj);
            }
        }

        //Time.timeScale = 0f;


        //Maybe to be removed because i added the other VFX
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (claimButton != null) claimButton.gameObject.SetActive(true);
        if (GameManager.instance.confettiList != null)
        {
            foreach (var item in GameManager.instance.confettiList)
            {
                item.Play();
            }
        }

        PlayStarVFX();
        // Center popup animation with fade
        CanvasGroup canvasGroup = inPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = inPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        inPanel.transform.localScale = Vector3.zero;

        // Fade in and scale up from center
        canvasGroup.DOFade(1f, 0.5f).SetUpdate(true);
        inPanel.transform
            .DOScale(1f, 0.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() => AnimateStarsAndGlow());

        ResetLevelState();
    }

    private void OnClaimButtonClicked()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowRewardedAd(() =>
            {
                // Reward: Double the level coins or a fixed amount
                if (levels != null && levels.Length > 0)
                {
                    var levelData = levels[currentLevel % levels.Length];
                    if (levelData != null && PlayerProfile.Instance != null)
                    {
                        PlayerProfile.Instance.AddCoins(levelData.coinsReward); // Give the reward again (effectively doubling it)
                        Debug.Log($"Claimed Bonus! Awarded additional {levelData.coinsReward} coins.");
                        FirebaseManager.Instance.AddCoins(levelData.coinsReward);
                    }
                }

                // Hide button after claim
                if (claimButton != null) claimButton.gameObject.SetActive(false);
            });
        }
    }

    private void AnimateStarsAndGlow()
    {
        // Animate glow background opacity
        if (glowBackground != null)
        {
            Color glowColor = glowBackground.color;
            glowColor.a = 0f;
            glowBackground.color = glowColor;

            glowBackground.DOFade(1f, glowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        /*
        // Animate stars one by one with opacity fade
        if (stars != null && stars.Length > 0)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    // Enable the star GameObject
                    stars[i].gameObject.SetActive(true);
                    
                    // Start transparent
                    Color starColor = stars[i].color;
                    starColor.a = 0f;
                    stars[i].color = starColor;
                    
                    // Also start small
                    stars[i].transform.localScale = Vector3.zero;
                    
                    // Delay each star animation
                    float delay = i * starDelay;
                    
                    // Fade in and scale up
                    stars[i].DOFade(1f, starAnimationDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    
                    stars[i].transform.DOScale(1f, starAnimationDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                }
            }
        }
        */
    }

    private void OnNextLevelClicked()
    {
        // Stop confetti
        if (GameManager.instance.confettiList != null)
        {
            foreach (var item in GameManager.instance.confettiList)
            {
                item.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (starLoopCoroutine != null)
        {
            StopCoroutine(starLoopCoroutine);
            starLoopCoroutine = null;
        }
        /*if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowRewardedAd(() =>
            {
                if (levels != null && levels.Length > 0)
                {
                    var levelData = levels[currentLevel % levels.Length];
                    if (levelData != null && PlayerProfile.Instance != null)
                    {
                        PlayerProfile.Instance.AddCoins(levelData.coinsReward);
                        Debug.Log($"Claimed Bonus! Awarded additional {levelData.coinsReward} coins.");
                    }
                }
            });
        }*/
        DestroyAllSpawnedStars();
        GameManager.instance.gameplayEnabled = false; // Wait for PlayGame()
        Time.timeScale = 1f;
        levelCompletePanel.SetActive(false);
        currentLevel++;

        //PlayerPrefs.SetInt("currentLevel", currentLevel);
        FirebaseManager.Instance.SetLevel(currentLevel);

        if (currentLevel >= whenToReachTournamentLevel)
        {
            UnlockTournament();
        }

        LoadLevel();
    }


    /// <summary>
    /// Unlock the tournament button and hide the lock icon
    /// </summary>
    private void UnlockTournament()
    {
        // Disable the locked tournament button
        if (tournamentLockButton != null)
        {
            tournamentLockButton.SetActive(false);
        }

        // Enable the unlocked tournament button
        if (tournamentUnlockButton != null)
        {
            tournamentUnlockButton.SetActive(true);
        }

        tournamentUnlockButton.GetComponent<Button>().interactable = true;

        Debug.Log("Tournament unlocked!");
    }

    #endregion


    #region Reset & Retry

    public void ResetLevelState()
    {
        // Stop AI
        if (aiController != null)
        {
            aiController.StopAI();
            aiController.ClearConnectionsOnExit();
        }

        // Kill soldiers using registry
        for (int i = allSoldiers.Count - 1; i >= 0; i--)
        {
            var s = allSoldiers[i];
            if (s == null) continue;
            var runner = Network.MultiplayerManager.Instance != null ? Network.MultiplayerManager.Instance.runner : null;
            if (runner != null && s.Object != null && s.Object.IsValid)
                runner.Despawn(s.Object);
            else
                Destroy(s.gameObject);
        }
        allSoldiers.Clear();

        // Kill line renderers using registry
        for (int i = allLines.Count - 1; i >= 0; i--)
        {
            if (allLines[i] != null) Destroy(allLines[i].gameObject);
        }
        allLines.Clear();

        // Kill cut trails
        for (int i = allTrails.Count - 1; i >= 0; i--)
        {
            if (allTrails[i] != null) Destroy(allTrails[i].gameObject);
        }
        allTrails.Clear();

        // Kill bullets
        for (int i = allBullets.Count - 1; i >= 0; i--)
        {
            var b = allBullets[i];
            if (b == null) continue;
            var netObj = b.GetComponent<NetworkObject>();
            var runner = Network.MultiplayerManager.Instance != null ? Network.MultiplayerManager.Instance.runner : null;
            if (netObj != null && runner != null && netObj.IsValid)
                runner.Despawn(netObj);
            else
                Destroy(b.gameObject);
        }
        allBullets.Clear();

        // Clear tower links
        foreach (var towerObj in towerPool)
        {
            if (towerObj == null) continue;
            var tower = towerObj.GetComponent<TowerBehavior>();
            if (tower != null)
                tower.linkedTowers.Clear();
            Destroy(towerObj.gameObject);
        }
        towerPool.Clear();
        Utils.SimplePool.Clear(); // Clear pool to remove stale references

        // Clear registries
        allTowers.Clear();
        // Soldiers, Bullets, Lines, Trails cleared above

        playerTowers = 0;
        enemyTowers = 0;
        blueFill.fillAmount = 0;
        redFill.fillAmount = 0;
        greenFill.fillAmount = 0;
        yellowFill.fillAmount = 0;
        bluePercentageFilled.fillAmount = 0;

        levelCompleted = false;
    }

    public void OnRetryLevelClicked()
    {
        Debug.Log("Retry level");
        Time.timeScale = 1f;
        GameManager.instance.gameplayEnabled = false;
        if (currentLevel == 0)
        {
            GameManager.instance.firstText.SetActive(false);
            GameManager.instance.secondText.SetActive(false);
            GameManager.instance.thirdText.SetActive(false);
            GameManager.instance.pointerCut.SetActive(false);
            GameManager.instance.pointerMove.SetActive(false);
        }
        if (aiController != null)
        {
            aiController.StopAI();
        }

        ResetLevelState();
        LoadLevel();

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.HideBannerAd();
        }
    }

    public void PlayGame()
    {
        // Use registry instead of FindObjectsOfType
        if (allTowers.Count > 0)
        {
            foreach (var tower in allTowers)
            {
                if (tower.attackType == TowerBehavior.TowerAttackType.NormalTower)
                {
                    if (GameManager.instance.gameMode == Mode.Single)
                    {
                        tower.towerHealthText.text = tower.CurrentHealth.ToString();
                        tower.SetDots(tower.CurrentHealth);
                    }

                }
                else if (tower.attackType == TowerBehavior.TowerAttackType.AttackerTower)
                {
                    Debug.Log("Is Attacker Tower");
                    if (GameManager.instance.gameMode == Mode.Single)
                    {
                        Debug.Log("Is Single Tower");
                        tower.towerHealthText.text = tower.CurrentHealth.ToString();
                        tower.setAttackerFloors(tower.CurrentHealth);
                    }

                }
            }
        }

        GameManager.instance.gameplayEnabled = true;

        if (starterTower != null)
        {
            starterTower.PunchRoundedSprite(starterTower);
        }
        if (aiController != null)
        {
            aiController.FindAITowers();
            StartCoroutine(aiController.InitializeAI());
        }
        GameManager.instance.setLevelHolderPosition_Game();

        if (currentLevel == 0 && (GameManager.instance.gameMode == Mode.Single))
        {
            GameManager.instance.firstText.SetActive(true);
            GameManager.instance.pointerMove.SetActive(true);
        }

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowBannerAd();
        }
    }
    public void QuitToMenu()
    {
        if (Network.MultiplayerManager.Instance && Network.MultiplayerManager.Instance.runner)
        {
            Network.MultiplayerManager.Instance.runner.Shutdown();
        }
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.HideBannerAd();
        }
        SceneManager.LoadScene("Main");
    }

    public void UpdatePercentageUI()
    {
        // Use registry
        int blueCount = 0;
        int redCount = 0;
        int greenCount = 0;
        int yellowCount = 0;

        // Count only non-neutral active towers
        foreach (var tower in allTowers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy) continue;
            switch (tower.type)
            {
                case TowerType.PlayerTower:  // BLUE
                    blueCount++;
                    break;

                case TowerType.RedTower:
                    redCount++;
                    break;

                case TowerType.GreenTower:
                    greenCount++;
                    break;

                case TowerType.YellowTower:
                    yellowCount++;
                    break;

                // GREY TOWER → skip (neutral)
                case TowerType.GreyTower:
                    break;
            }
        }

        int totalNonNeutral = blueCount + redCount + greenCount + yellowCount;

        if (totalNonNeutral == 0)
            return; // avoid division by zero

        float bluePercent = (float)blueCount / totalNonNeutral;
        float redPercent = (float)redCount / totalNonNeutral;
        float greenPercent = (float)greenCount / totalNonNeutral;
        float yellowPercent = (float)yellowCount / totalNonNeutral;

        // Compute conditional stacked amounts so colors with 0% do not overlay others
        float blueFillVal = bluePercent;
        float redFillVal = redPercent > 0f ? redPercent + bluePercent : 0f;
        float greenFillVal = greenPercent > 0f ? greenPercent + redPercent + bluePercent : 0f;
        float yellowFillVal = yellowPercent > 0f ? yellowPercent + greenPercent + redPercent + bluePercent : 0f;

        if (GameManager.instance.gameplayEnabled)
        {
            blueFill.DOKill();
            redFill.DOKill();
            greenFill.DOKill();
            yellowFill.DOKill();
            blueFill.DOFillAmount(blueFillVal, 0.35f);
            redFill.DOFillAmount(redFillVal, 0.35f);
            greenFill.DOFillAmount(greenFillVal, 0.35f);
            yellowFill.DOFillAmount(yellowFillVal, 0.35f);
        }
        else
        {
            blueFill.fillAmount = blueFillVal;
            redFill.fillAmount = redFillVal;
            greenFill.fillAmount = greenFillVal;
            yellowFill.fillAmount = yellowFillVal;
        }

    }

    #endregion
}


