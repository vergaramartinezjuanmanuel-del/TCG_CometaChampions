using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Advertisements;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Services
{
    public enum CoinPackType
    {
        Pack1000,
        Pack5000,
        Pack10000
    }

    [Serializable]
    public struct CoinPackInfo
    {
        public CoinPackType Type;
        public string ProductId;
        public int Coins;
        public string DisplayPrice;
        public string Title;
        public string BadgeText;

        public CoinPackInfo(CoinPackType type, string productId, int coins, string displayPrice, string title, string badgeText = "")
        {
            Type = type;
            ProductId = productId;
            Coins = coins;
            DisplayPrice = displayPrice;
            Title = title;
            BadgeText = badgeText;
        }
    }

    /// <summary>
    /// Service managing In-App Purchases (Unity IAP) and Rewarded Video Ads (Unity Ads).
    /// Connects to Unity IAP StoreController and Unity Advertisement SDK,
    /// loading authoritative settings from Assets/Resources while providing
    /// seamless Editor Simulation fallback for rapid playtesting.
    /// </summary>
    public class MonetizationService : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private static MonetizationService? instance;
        public static MonetizationService Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindAnyObjectByType<MonetizationService>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("MonetizationService");
                        instance = go.AddComponent<MonetizationService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public event Action<int>? OnGoldPurchased;
        public event Action<int>? OnAdRewardGranted;

        private StoreController? storeController;
        private bool isStoreConnected;
        private Action<bool, string>? pendingPurchaseCallback;

        private UnityAdsConfig adsConfig = new UnityAdsConfig();
        private bool isAdsInitialized;
        private bool isAdLoaded;
        private Action<bool, string>? pendingAdRewardCallback;

        public bool IsStoreConnected => isStoreConnected;
        public UnityAdsConfig AdsConfig => adsConfig;
        public bool IsAdsInitialized => isAdsInitialized;
        public bool IsAdLoaded => isAdLoaded;
        public bool IsAdReady => isAdLoaded || adsConfig.enableEditorSimulation || !Advertisement.isSupported;

        public static CoinPackInfo GetPackInfo(CoinPackType type) => type switch
        {
            CoinPackType.Pack1000 => new CoinPackInfo(
                CoinPackType.Pack1000,
                PlayerProfile.IapPack1000Id,
                PlayerProfile.CoinsPerPack1000,
                "$0.99 USD",
                "1,000 Monedas"),
            CoinPackType.Pack5000 => new CoinPackInfo(
                CoinPackType.Pack5000,
                PlayerProfile.IapPack5000Id,
                PlayerProfile.CoinsPerPack5000,
                "$4.99 USD",
                "5,000 Monedas",
                "POPULAR"),
            CoinPackType.Pack10000 => new CoinPackInfo(
                CoinPackType.Pack10000,
                PlayerProfile.IapPack10000Id,
                PlayerProfile.CoinsPerPack10000,
                "$8.99 USD",
                "10,000 Monedas",
                "MEJOR VALOR (+20%)"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeIAP();
            InitializeUnityAds();
        }

        /// <summary>
        /// Initializes Unity IAP StoreController and connects to the store or local mock.
        /// </summary>
        public async void InitializeIAP()
        {
            try
            {
                storeController = UnityIAPServices.StoreController();

                storeController.OnStoreConnected += OnStoreConnected;
                storeController.OnStoreDisconnected += OnStoreDisconnected;
                storeController.OnProductsFetched += OnProductsFetched;
                storeController.OnProductsFetchFailed += OnProductsFetchFailed;
                storeController.OnPurchasePending += OnPurchasePending;
                storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                storeController.OnPurchaseFailed += OnPurchaseFailed;

                Debug.Log("[MonetizationService] Connecting to Unity IAP StoreController...");
                await storeController.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MonetizationService] Unity IAP initialization skipped or offline ({ex.Message}). Operating in Editor simulation mode.");
            }
        }

        private void OnStoreConnected()
        {
            isStoreConnected = true;
            Debug.Log("[MonetizationService] Unity IAP store connected successfully. Fetching catalog products...");
            FetchCatalogProducts();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            isStoreConnected = false;
            Debug.LogWarning($"[MonetizationService] Store disconnected: {description.message}");
        }

        private void FetchCatalogProducts()
        {
            if (storeController == null) return;

            var productDefs = new List<ProductDefinition>();
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog != null && catalog.allValidProducts != null && catalog.allValidProducts.Count > 0)
            {
                foreach (var item in catalog.allValidProducts)
                {
                    productDefs.Add(new ProductDefinition(item.id, item.type));
                }
                Debug.Log($"[MonetizationService] Loaded {productDefs.Count} products from Assets/Resources/IAPProductCatalog.json.");
            }
            else
            {
                productDefs.Add(new ProductDefinition(PlayerProfile.IapPack1000Id, ProductType.Consumable));
                productDefs.Add(new ProductDefinition(PlayerProfile.IapPack5000Id, ProductType.Consumable));
                productDefs.Add(new ProductDefinition(PlayerProfile.IapPack10000Id, ProductType.Consumable));
                Debug.Log("[MonetizationService] Using default fallback product definitions for IAP fetch.");
            }

            storeController.FetchProducts(productDefs);
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[MonetizationService] Successfully fetched {products.Count} products from store.");
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"[MonetizationService] Failed to fetch products: {failure.FailureReason}");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            var cartItems = order.CartOrdered?.Items();
            var product = cartItems?.FirstOrDefault()?.Product;
            string productId = product?.definition?.id ?? "";

            Debug.Log($"[MonetizationService] OnPurchasePending received for product: {productId}");

            int coinsToCredit = productId switch
            {
                PlayerProfile.IapPack1000Id => PlayerProfile.CoinsPerPack1000,
                PlayerProfile.IapPack5000Id => PlayerProfile.CoinsPerPack5000,
                PlayerProfile.IapPack10000Id => PlayerProfile.CoinsPerPack10000,
                _ => 0
            };

            if (coinsToCredit > 0)
            {
                var profile = SaveManager.Instance.CurrentProfile;
                if (profile != null)
                {
                    profile.AddGoldCoins(coinsToCredit);
                    SaveManager.Instance.SaveProfile();
                    OnGoldPurchased?.Invoke(coinsToCredit);
                    Debug.Log($"[MonetizationService] Credited +{coinsToCredit:N0} gold coins. New balance: {profile.GoldCoins:N0}");
                }
            }

            storeController?.ConfirmPurchase(order);

            string message = $"¡Compra confirmada! Se han acreditado +{coinsToCredit:N0} Monedas de Oro.";
            pendingPurchaseCallback?.Invoke(true, message);
            pendingPurchaseCallback = null;
        }

        private void OnPurchaseConfirmed(Order order)
        {
            Debug.Log("[MonetizationService] Purchase confirmation acknowledged.");
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            var cartItems = order.CartOrdered?.Items();
            var product = cartItems?.FirstOrDefault()?.Product;
            string reason = $"Compra no completada: {order.FailureReason}";
            Debug.LogWarning($"[MonetizationService] Purchase failed: {product?.definition?.id} - {order.FailureReason}");
            pendingPurchaseCallback?.Invoke(false, reason);
            pendingPurchaseCallback = null;
        }

        /// <summary>
        /// Purchases a coin pack via Unity IAP (or simulation in Editor).
        /// </summary>
        public void BuyCoinPack(CoinPackType packType, Action<bool, string>? onComplete = null)
        {
            var info = GetPackInfo(packType);
            Debug.Log($"[Monetization] Initiating IAP purchase for {info.ProductId} ({info.Coins} coins)...");

            if (isStoreConnected && storeController != null)
            {
                pendingPurchaseCallback = onComplete;
                storeController.PurchaseProduct(info.ProductId);
                return;
            }

            // Fallback: If running in Editor without live store or store connection is pending,
            // process simulated purchase immediately so gameplay and testing are never blocked.
            var profile = SaveManager.Instance.CurrentProfile;
            if (profile == null)
            {
                onComplete?.Invoke(false, "Error: No se pudo cargar el perfil del jugador.");
                return;
            }

            profile.AddGoldCoins(info.Coins);
            SaveManager.Instance.SaveProfile();

            OnGoldPurchased?.Invoke(info.Coins);
            string msg = $"¡Compra exitosa! Se han acreditado +{info.Coins:N0} Monedas de Oro.";
            Debug.Log($"[Monetization] {msg} Nuevo saldo: {profile.GoldCoins:N0}");
            onComplete?.Invoke(true, msg);
        }

        #region Unity Ads Integration
        /// <summary>
        /// Loads authoritative Unity Ads settings from Assets/Resources/UnityAdsSettings.json.
        /// </summary>
        public void LoadAdsConfig()
        {
            var textAsset = Resources.Load<TextAsset>("UnityAdsSettings");
            if (textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<UnityAdsConfig>(textAsset.text);
                    if (loaded != null)
                    {
                        adsConfig = loaded;
                        Debug.Log($"[MonetizationService] Loaded Unity Ads config (GameId: {adsConfig.ActiveGameId}, AdUnit: {adsConfig.ActiveAdUnitId}, TestMode: {adsConfig.testMode}).");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MonetizationService] Error parsing UnityAdsSettings.json: {ex.Message}");
                }
            }

            Debug.Log("[MonetizationService] Using default Unity Ads settings.");
        }

        /// <summary>
        /// Initializes the Unity Advertisement SDK.
        /// </summary>
        public void InitializeUnityAds()
        {
            LoadAdsConfig();

            if (!Advertisement.isSupported)
            {
                Debug.Log("[MonetizationService] Unity Ads not supported on this platform. Simulation mode active.");
                return;
            }

            if (Advertisement.isInitialized)
            {
                isAdsInitialized = true;
                LoadRewardedVideoAd();
                return;
            }

            Debug.Log($"[MonetizationService] Initializing Unity Ads SDK for GameId: {adsConfig.ActiveGameId} (TestMode: {adsConfig.testMode})...");
            Advertisement.Initialize(adsConfig.ActiveGameId, adsConfig.testMode, this);
        }

        public void LoadRewardedVideoAd()
        {
            if (!Advertisement.isInitialized) return;
            Debug.Log($"[MonetizationService] Pre-loading rewarded ad: {adsConfig.ActiveAdUnitId}...");
            Advertisement.Load(adsConfig.ActiveAdUnitId, this);
        }

        // IUnityAdsInitializationListener
        public void OnInitializationComplete()
        {
            isAdsInitialized = true;
            Debug.Log("[MonetizationService] Unity Ads initialized successfully.");
            LoadRewardedVideoAd();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            isAdsInitialized = false;
            Debug.LogWarning($"[MonetizationService] Unity Ads initialization failed: {error} - {message}. Simulation mode available.");
        }

        // IUnityAdsLoadListener
        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId == adsConfig.ActiveAdUnitId)
            {
                isAdLoaded = true;
                Debug.Log($"[MonetizationService] Unity Ads placement ready: {placementId}");
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            isAdLoaded = false;
            Debug.LogWarning($"[MonetizationService] Failed to load placement {placementId}: {error} - {message}");
        }

        /// <summary>
        /// Shows a rewarded video ad via Unity Ads (or simulation in Editor) and rewards gold coins.
        /// </summary>
        public void ShowRewardedVideo(Action<bool, string>? onRewardResult = null)
        {
            if (!IsAdReady)
            {
                onRewardResult?.Invoke(false, "El anuncio no está listo en este momento. Inténtalo de nuevo más tarde.");
                return;
            }

            // If live SDK is initialized and ad is loaded, show native rewarded video
            if (Advertisement.isInitialized && isAdLoaded)
            {
                pendingAdRewardCallback = onRewardResult;
                Debug.Log($"[MonetizationService] Showing native rewarded video ({adsConfig.ActiveAdUnitId})...");
                Advertisement.Show(adsConfig.ActiveAdUnitId, this);
                return;
            }

            // Fallback: If in Editor simulation or offline/testing, complete reward immediately
            Debug.Log("[MonetizationService] Simulating rewarded video ad completion...");
            GrantAdReward(onRewardResult);
        }

        // IUnityAdsShowListener
        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            isAdLoaded = false;
            LoadRewardedVideoAd(); // Preload next ad

            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log($"[MonetizationService] Rewarded video fully watched on {placementId}. Crediting reward...");
                GrantAdReward(pendingAdRewardCallback);
            }
            else
            {
                string msg = "El anuncio se cerró antes de completarse. No se otorgó recompensa.";
                Debug.LogWarning($"[MonetizationService] Rewarded video incomplete ({showCompletionState}).");
                pendingAdRewardCallback?.Invoke(false, msg);
            }
            pendingAdRewardCallback = null;
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            isAdLoaded = false;
            LoadRewardedVideoAd();
            string msg = $"Error al reproducir el anuncio: {message}";
            Debug.LogWarning($"[MonetizationService] Ad show failure: {error} - {message}");
            pendingAdRewardCallback?.Invoke(false, msg);
            pendingAdRewardCallback = null;
        }

        public void OnUnityAdsShowStart(string placementId)
        {
            Debug.Log($"[MonetizationService] Rewarded video playback started: {placementId}");
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            Debug.Log($"[MonetizationService] Rewarded video clicked: {placementId}");
        }

        private void GrantAdReward(Action<bool, string>? callback)
        {
            var profile = SaveManager.Instance.CurrentProfile;
            if (profile == null)
            {
                callback?.Invoke(false, "Error: Perfil no disponible.");
                return;
            }

            int reward = adsConfig.rewardGoldCoins > 0 ? adsConfig.rewardGoldCoins : PlayerProfile.CoinsPerAdReward;
            profile.AddGoldCoins(reward);
            SaveManager.Instance.SaveProfile();

            OnAdRewardGranted?.Invoke(reward);
            string msg = $"¡Gracias por ver el anuncio! Has recibido +{reward} Monedas de Oro.";
            Debug.Log($"[MonetizationService] {msg} Nuevo saldo: {profile.GoldCoins:N0}");
            callback?.Invoke(true, msg);
        }
        #endregion
    }
}
