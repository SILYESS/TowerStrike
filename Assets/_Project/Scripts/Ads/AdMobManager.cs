using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance;

    [Header("Ad Unit IDs")]
    public string androidInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    public string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";



    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private Action onRewardEarnedCallback;

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
    }

    private void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob Initialized");
            LoadInterstitialAd();
            LoadRewardedAd();
            LoadBannerAd();
            HideBannerAd();
        });
    }

    #region Interstitial Ad

    public void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");

        var adRequest = new AdRequest();

        InterstitialAd.Load(androidInterstitialAdUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("interstitial ad failed to load an ad " + error);
                    return;
                }

                Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());

                interstitialAd = ad;
                RegisterInterstitialAdEvents(interstitialAd);
            });
    }

    private void RegisterInterstitialAdEvents(InterstitialAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");
            LoadInterstitialAd();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);
            LoadInterstitialAd();
        };
    }

    public void ShowInterstitialAd()
    {
        if (PlayerProfile.Instance != null && PlayerProfile.Instance.HasRemovedAds())
        {
            Debug.Log("Ads are removed. Skipping Interstitial.");
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogError("Interstitial ad is not ready yet.");
            if (interstitialAd == null) LoadInterstitialAd();
        }
    }

    #endregion

    #region Rewarded Ad

    public void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");

        var adRequest = new AdRequest();

        RewardedAd.Load(androidRewardedAdUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " + error);
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());

                rewardedAd = ad;
                RegisterRewardedAdEvents(rewardedAd);
            });
    }

    private void RegisterRewardedAdEvents(RewardedAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
            LoadRewardedAd();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
            LoadRewardedAd();
        };
    }

    public void ShowRewardedAd(Action onReward)
    {
        onRewardEarnedCallback = onReward;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log(String.Format("Rewarded ad rewarded the user. Type: {0}, Amount: {1}",
                    reward.Type,
                    reward.Amount));

                if (onRewardEarnedCallback != null)
                {
                    onRewardEarnedCallback.Invoke();
                    onRewardEarnedCallback = null;
                }
            });
        }
        else
        {
            Debug.LogError("Rewarded ad is not ready yet.");
            if (rewardedAd == null) LoadRewardedAd();
        }
    }

    #endregion

    #region Banner Ad

    private BannerView bannerView;
    public string androidBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // Test ID

    public void LoadBannerAd()
    {
        if (bannerView == null)
        {
            CreateBannerView();
        }

    }

    private void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        // If we already have a banner, destroy the old one.
        if (bannerView != null)
        {
            DestroyBannerAd();
        }

        bannerView = new BannerView(androidBannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

        AdRequest request = new AdRequest();

        bannerView.LoadAd(request);
    }
    
    public void ShowBannerAd()
    {
        if (PlayerProfile.Instance != null && PlayerProfile.Instance.HasRemovedAds())
        {
            return;
        }

        if (bannerView == null)
        {
            CreateBannerView();
        }
        
        bannerView.Show();
    }

    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            bannerView.Destroy();
            bannerView = null;
        }
    }

    #endregion
}
