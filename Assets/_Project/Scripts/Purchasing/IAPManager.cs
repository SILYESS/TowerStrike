using UnityEngine;
using UnityEngine.Purchasing;
using System;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance;

    private static IStoreController m_StoreController;
    private static IExtensionProvider m_StoreExtensionProvider;

    public const string NO_ADS = "com.towerstrike.noads";

    public const string COINS_PACK_1 = "com.towerstrike.coinspack1"; // e.g. 50 coins
    public const string COINS_PACK_2 = "com.towerstrike.coinspack2"; // e.g. 100 coins
    public const string COINS_PACK_3 = "com.towerstrike.coinspack3"; // e.g. 150 coins

    public const string GEMS_PACK_1 = "com.towerstrike.gemspack1"; // e.g. 50 gems
    public const string GEMS_PACK_2 = "com.towerstrike.gemspack2"; // e.g. 150 gems
    public const string GEMS_PACK_3 = "com.towerstrike.gemspack3"; // e.g. 300 gems

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
        if (m_StoreController == null)
        {
            InitializePurchasing();
        }
    }

    public void InitializePurchasing()
    {
        if (IsInitialized())
        {
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(NO_ADS, ProductType.NonConsumable);

        builder.AddProduct(COINS_PACK_1, ProductType.Consumable);
        builder.AddProduct(COINS_PACK_2, ProductType.Consumable);
        builder.AddProduct(COINS_PACK_3, ProductType.Consumable);

        builder.AddProduct(GEMS_PACK_1, ProductType.Consumable);
        builder.AddProduct(GEMS_PACK_2, ProductType.Consumable);
        builder.AddProduct(GEMS_PACK_3, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    #region Buying Methods

    public void BuyNoAds()
    {
        BuyProductID(NO_ADS);
    }

    public void BuyCoinsPack1()
    {
        BuyProductID(COINS_PACK_1);
    }

    public void BuyCoinsPack2()
    {
        BuyProductID(COINS_PACK_2);
    }

    public void BuyCoinsPack3()
    {
        BuyProductID(COINS_PACK_3);
    }

    public void BuyGemsPack1()
    {
        BuyProductID(GEMS_PACK_1);
    }

    public void BuyGemsPack2()
    {
        BuyProductID(GEMS_PACK_2);
    }

    public void BuyGemsPack3()
    {
        BuyProductID(GEMS_PACK_3);
    }

    #endregion

    private void BuyProductID(string productId)
    {
        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        else
        {
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("OnInitialized: PASS");
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error + " " + message);

    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (String.Equals(args.purchasedProduct.definition.id, NO_ADS, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null)
            {
                PlayerProfile.Instance.RemoveAds();
            }
        }
        else if (String.Equals(args.purchasedProduct.definition.id, COINS_PACK_1, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.AddCoins(50);
        }
        else if (String.Equals(args.purchasedProduct.definition.id, COINS_PACK_2, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.AddCoins(150);
        }
        else if (String.Equals(args.purchasedProduct.definition.id, COINS_PACK_3, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.AddCoins(300);
        }
        else if (String.Equals(args.purchasedProduct.definition.id, GEMS_PACK_1, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.AddGems(50);
        }
        else if (String.Equals(args.purchasedProduct.definition.id, GEMS_PACK_2, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.AddGems(150);
        }
        else if (String.Equals(args.purchasedProduct.definition.id, GEMS_PACK_3, StringComparison.Ordinal))
        {
            Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.AddGems(300);
        }
        else
        {
            Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
    }
}
