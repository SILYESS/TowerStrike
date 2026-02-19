using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Intro
{
    public class IntroManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject introPanel;
        public Image loadingImage;
        public Text loadingText;
        public Button signInPanelBtn;
        public Button skipBtn;
        public GameObject signUpPanel;
        
        [Header("Settings")]
        public float loadingRotationSpeed = 1f;
        public float waitDuration = 4f;
        public string mainSceneName = "Main";
        private bool _canActivateScene = false;

        private void Awake()
        {
            StartCoroutine(PrepareAndCloseIntro());
            skipBtn.onClick.AddListener(() => SceneManager.LoadScene(mainSceneName));
            signInPanelBtn.onClick.AddListener(() => signUpPanel.SetActive(true));
        }

        private IEnumerator PrepareAndCloseIntro()
        {
            // 1️⃣ Start rotating loading spinner
            loadingImage.transform
                .DORotate(new Vector3(0, 0, -360f), loadingRotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1);

            float timer = 0f;
            
            // 2️⃣ First, show the timer-based percentage for exactly 4 seconds
            while (timer < waitDuration)
            {
                float timeProgress = timer / waitDuration;
                int percentage = Mathf.RoundToInt(timeProgress * 100f +1);
                loadingText.text = $"{percentage}%";
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            loadingText.text = "100%";
            yield return new WaitForSeconds(0.2f);
            skipBtn.gameObject.SetActive(true);
            signInPanelBtn.gameObject.SetActive(true);
            loadingText.gameObject.SetActive(false);
            loadingImage.gameObject.SetActive(false);
            //SceneManager.LoadScene(mainSceneName);
        }
    }
}