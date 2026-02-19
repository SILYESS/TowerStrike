using Fusion;
using TMPro;
using Towers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Network
{
    public class MultiplayerGameHUD : MonoBehaviour
    {
        public static MultiplayerGameHUD Instance;

        [Header("HP Sliders")]
        public Image blueSlider;
        public Image redSlider;
        public Text blueHpText;
        public Text redHpText;

        [Header("Game Over Panels")]
        public GameObject victoryPanel;
        public GameObject defeatPanel;

        private void Awake()
        {
            Instance = this;
            if (victoryPanel) victoryPanel.SetActive(false);
            if (defeatPanel) defeatPanel.SetActive(false);
        }

        private void Update()
        {
            UpdateHPSliders();
        }
        public void QuitToMenu()
        {
            if (MultiplayerManager.Instance && MultiplayerManager.Instance.runner)
            {
                MultiplayerManager.Instance.runner.Shutdown();
            }
            SceneManager.LoadScene("Main");
        }
        private void UpdateHPSliders()
        {
            if (MultiplayerManager.Instance == null || MultiplayerManager.Instance.runner == null) return;

            int blueCount = 0;
            int redCount = 0;
            int totalCount = 0;

            var towers = FindObjectsOfType<TowerBehavior>();

            // Calculate totals
            foreach (var t in towers)
            {
                // CRITICAL FIX: Skip if NetworkObject is not valid yet
                if (t.Object == null || !t.Object.IsValid) continue;

                if (t.networkedType == TowerType.PlayerTower)
                {
                    blueCount++;
                    totalCount++;
                }
                else if (t.networkedType == TowerType.RedTower)
                {
                    redCount++;
                    totalCount++;
                }
            }

            // Determine which count is "Mine" (Blue Slider) and "Enemy" (Red Slider)
            // Default (Host/Server): Blue Towers = Me, Red Towers = Enemy
            // Client: Red Towers = Me, Blue Towers = Enemy

            float myCount = blueCount;
            float enemyCount = redCount;

            // If we are the client (and not the server/host), swap
            if (!MultiplayerManager.Instance.runner.IsServer)
            {
                myCount = redCount;
                enemyCount = blueCount;
            }

            // Update Sliders
            if (totalCount > 0)
            {
                if (blueSlider)
                {
                    // "Blue" slider represents the Local Player
                    blueSlider.fillAmount = myCount / totalCount;
                }
                if (redSlider)
                {
                    // "Red" slider represents the Enemy
                    redSlider.fillAmount = (enemyCount + myCount) / totalCount;
                }
            }
            else
            {
                if (blueSlider) blueSlider.fillAmount = 0;
                if (redSlider) redSlider.fillAmount = 0;
            }

            //if (blueHpText) blueHpText.text = Mathf.RoundToInt(myCount).ToString();
            //if (redHpText) redHpText.text = Mathf.RoundToInt(enemyCount).ToString();
        }

        public void ShowGameOver(bool isWinner)
        {
            if (isWinner)
            {
                if (victoryPanel) victoryPanel.SetActive(true);
            }
            else
            {
                if (defeatPanel) defeatPanel.SetActive(true);
            }
        }

        public void ReturnToMenu()
        {
            if (MultiplayerManager.Instance)
                MultiplayerManager.Instance.LeaveSession();

            SceneManager.LoadScene("Main");
        }
    }
}
