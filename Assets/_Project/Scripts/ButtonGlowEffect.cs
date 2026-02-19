using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonGlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    public Image buttonImage; // The button's background image
    public float glowDuration = 1f; // Duration for one glow cycle (higher = slower)
    public float glowIntensity = 1.8f; // How bright the glow gets (1.0 = normal, 2.0 = twice as bright)
    public bool animateOnStart = true; // Start glowing automatically
    
    [Header("Scale Pulse Effect")]
    public bool enableScalePulse = true; // Add a subtle scale animation
    public float scaleAmount = 1.1f; // How much to scale (1.1 = 10% bigger)
    
    [Header("Optional - Text Glow")]
    public Text buttonText; // Optional: also animate the text
    public TMPro.TextMeshProUGUI buttonTextTMP; // Optional: TextMeshPro text
    public bool glowText = true; // Whether to glow the text as well

    private Color originalImageColor;
    private Color originalTextColor;
    private Vector3 originalScale;
    private Tween glowTween;
    private Tween textGlowTween;
    private Tween scaleTween;

    private void Start()
    {
        // Store original colors and scale
        if (buttonImage != null)
            originalImageColor = buttonImage.color;
        
        if (buttonText != null)
            originalTextColor = buttonText.color;
        else if (buttonTextTMP != null)
            originalTextColor = buttonTextTMP.color;

        originalScale = transform.localScale;

        // Start glow animation
        if (animateOnStart)
            StartGlowAnimation();
    }

    /// <summary>
    /// Start the glow animation
    /// </summary>
    public void StartGlowAnimation()
    {
        StopGlowAnimation(); // Stop any existing animation

        // Animate button image with stronger glow
        if (buttonImage != null)
        {
            Color brightColor = originalImageColor * glowIntensity;
            brightColor.a = originalImageColor.a; // Keep original alpha
            
            glowTween = buttonImage.DOColor(brightColor, glowDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // Animate scale pulse
        if (enableScalePulse)
        {
            scaleTween = transform.DOScale(originalScale * scaleAmount, glowDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // Animate text if enabled
        if (glowText)
        {
            Color brightTextColor = originalTextColor * glowIntensity;
            brightTextColor.a = originalTextColor.a; // Keep original alpha

            if (buttonText != null)
            {
                textGlowTween = buttonText.DOColor(brightTextColor, glowDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else if (buttonTextTMP != null)
            {
                textGlowTween = buttonTextTMP.DOColor(brightTextColor, glowDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
    }

    /// <summary>
    /// Stop the glow animation and reset to original colors
    /// </summary>
    public void StopGlowAnimation()
    {
        if (glowTween != null)
        {
            glowTween.Kill();
            if (buttonImage != null)
                buttonImage.color = originalImageColor;
        }

        if (scaleTween != null)
        {
            scaleTween.Kill();
            transform.localScale = originalScale;
        }

        if (textGlowTween != null)
        {
            textGlowTween.Kill();
            if (buttonText != null)
                buttonText.color = originalTextColor;
            else if (buttonTextTMP != null)
                buttonTextTMP.color = originalTextColor;
        }
    }

    private void OnDestroy()
    {
        // Clean up tweens
        if (glowTween != null)
            glowTween.Kill();
        if (scaleTween != null)
            scaleTween.Kill();
        if (textGlowTween != null)
            textGlowTween.Kill();
    }

    private void OnDisable()
    {
        // Stop animation when disabled
        StopGlowAnimation();
    }

    private void OnEnable()
    {
        // Restart animation when enabled (if it was set to auto-start)
        if (animateOnStart && buttonImage != null)
            StartGlowAnimation();
    }
}
