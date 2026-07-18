using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace YandexGames
{
    /// <summary>
    /// Bootstraps and wraps the Yandex Games SDK for WebGL builds.
    /// On non-WebGL platforms (Editor, standalone) all calls are safely no-ops / mocked,
    /// so the rest of the game can call this API unconditionally.
    ///
    /// Setup:
    ///  1. Build target must be WebGL, using the "YandexGames" WebGL template
    ///     (Project Settings > Player > Resolution and Presentation > WebGL Template).
    ///  2. Player Settings > Publishing Settings:
    ///       - Compression Format: Disabled (or Gzip/Brotli with "Decompression Fallback" ON).
    ///       - Data caching: leave default; Yandex Games hosts the build as static files.
    ///  3. Zip the WebGL build output (index.html at the zip root) and upload it in the
    ///     Yandex Games developer console: https://games.yandex.ru/
    /// </summary>
    public class YandexGamesManager : MonoBehaviour
    {
        public static YandexGamesManager Instance { get; private set; }

        /// <summary>Raised once the SDK has finished initializing (success or mocked).</summary>
        public static event Action OnInitialized;

        /// <summary>Raised if SDK init failed (e.g. running outside Yandex Games).</summary>
        public static event Action<string> OnInitFailed;

        public bool IsInitialized { get; private set; }

        /// <summary>True only on WebGL when actually running inside Yandex Games and SDK init succeeded.</summary>
        public bool IsAvailable { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void YandexSDK_Init(string gameObjectName);
        [DllImport("__Internal")] private static extern void YandexSDK_GameReady();
        [DllImport("__Internal")] private static extern void YandexSDK_GameplayStart();
        [DllImport("__Internal")] private static extern void YandexSDK_GameplayStop();
        [DllImport("__Internal")] private static extern void YandexSDK_ShowInterstitial(string gameObjectName);
        [DllImport("__Internal")] private static extern void YandexSDK_ShowRewarded(string gameObjectName);
        [DllImport("__Internal")] private static extern string YandexSDK_GetLang();
        [DllImport("__Internal")] private static extern void YandexSDK_RegisterVisibilityHandler(string gameObjectName);
#endif

        private Action<bool> pendingRewardedCallback;

        /// <summary>True while the player is in an active run (between GameplayStart/GameplayStop).</summary>
        private bool isGameplayActive;

        /// <summary>Whether an ad interrupted an active run and should resume GameplayAPI on close.</summary>
        private bool resumeGameplayAfterAd;

        /// <summary>Whether the once-per-session launch ad has already been shown.</summary>
        private bool launchAdShown;

        // Creates the manager automatically before the first scene loads, mirroring
        // the pattern used by PersistentSystemsLoader.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var go = new GameObject(nameof(YandexGamesManager));
            go.AddComponent<YandexGamesManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK_Init(gameObject.name);
#else
            // Editor / non-WebGL builds: mock a successful init so calling code
            // doesn't need platform checks everywhere.
            IsInitialized = true;
            IsAvailable = false;
            LocalizationManager.SetLanguageFromCode(SystemLanguageToCode(Application.systemLanguage));
            OnInitialized?.Invoke();
#endif
        }

        // ----- Callbacks invoked from JS via SendMessage -----

        public void OnSDKInitialized(string _)
        {
            IsInitialized = true;
            IsAvailable = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Requirement 2.14: auto language detection through the SDK.
            LocalizationManager.SetLanguageFromCode(YandexSDK_GetLang());
            // Requirement 1.3 / 1.19.4: stop sound on tab hide, react to platform pause/resume.
            YandexSDK_RegisterVisibilityHandler(gameObject.name);
#endif

            OnInitialized?.Invoke();

            // Let the platform know the game is playable and hide its loading screen.
            GameReady();
        }

        public void OnSDKInitFailed(string error)
        {
            IsInitialized = true;
            IsAvailable = false;
            LocalizationManager.SetLanguageFromCode(SystemLanguageToCode(Application.systemLanguage));
            Debug.LogWarning($"[YandexGames] SDK init failed: {error}");
            OnInitFailed?.Invoke(error);
        }

        /// <summary>Called from JS when the page becomes hidden/visible (tab switch, minimize, ad overlay, etc).</summary>
        public void OnPageVisibilityChanged(string isVisible)
        {
            bool visible = isVisible == "1";
            AudioListener.pause = !visible;

            // Docs explicitly list "leaving/returning to the browser tab" as a GameplayAPI
            // start/stop trigger. Only re-signal if a run is actually in progress, so we
            // don't send a spurious start() while sitting on the title/game-over screen.
            if (isGameplayActive)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (visible) YandexSDK_GameplayStart();
                else YandexSDK_GameplayStop();
#endif
            }
        }

        private static string SystemLanguageToCode(SystemLanguage language) => language switch
        {
            SystemLanguage.Russian => "ru",
            SystemLanguage.Ukrainian => "uk",
            SystemLanguage.Belarusian => "be",
            _ => "en",
        };

        public void OnInterstitialClosed(string wasShown)
        {
            if (resumeGameplayAfterAd)
            {
                resumeGameplayAfterAd = false;
                GameplayStart();
            }
        }

        public void OnRewardedClosed(string rewarded)
        {
            if (resumeGameplayAfterAd)
            {
                resumeGameplayAfterAd = false;
                GameplayStart();
            }

            bool granted = rewarded == "1";
            var callback = pendingRewardedCallback;
            pendingRewardedCallback = null;
            callback?.Invoke(granted);
        }

        // ----- Public API -----

        /// <summary>Tells the platform loading is complete and it can hide its loading UI.</summary>
        public void GameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK_GameReady();
#endif
        }

        /// <summary>Call when the player enters active gameplay.</summary>
        public void GameplayStart()
        {
            isGameplayActive = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK_GameplayStart();
#endif
        }

        /// <summary>Call when the player leaves active gameplay (game over, pause, menu).</summary>
        public void GameplayStop()
        {
            isGameplayActive = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK_GameplayStop();
#endif
        }

        /// <summary>Shows a fullscreen interstitial ad once per session, meant to be called
        /// when the title screen first appears. Safe to call repeatedly - only fires once.</summary>
        public void ShowLaunchAdIfNeeded()
        {
            if (launchAdShown) return;
            launchAdShown = true;
            ShowInterstitialAd();
        }

        /// <summary>Shows a fullscreen interstitial ad, e.g. between runs on the Game Over screen.</summary>
        public void ShowInterstitialAd()
        {
            // Docs list "showing a fullscreen/rewarded ad" as a GameplayAPI.stop() trigger;
            // resume automatically once it closes if a run was in progress.
            resumeGameplayAfterAd = isGameplayActive;
            if (isGameplayActive) GameplayStop();

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK_ShowInterstitial(gameObject.name);
#endif
        }

        /// <summary>Shows a rewarded video ad. onComplete receives true only if the reward was granted.</summary>
        public void ShowRewardedAd(Action<bool> onComplete)
        {
            pendingRewardedCallback = onComplete;

            resumeGameplayAfterAd = isGameplayActive;
            if (isGameplayActive) GameplayStop();

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK_ShowRewarded(gameObject.name);
#else
            // No ad platform available outside WebGL builds.
            var callback = pendingRewardedCallback;
            pendingRewardedCallback = null;
            if (resumeGameplayAfterAd) GameplayStart();
            callback?.Invoke(false);
#endif
        }
    }
}
