// Yandex Games SDK bridge.
// Talks to the `YaGames` global that is loaded via <script src="/sdk.js"> in the
// WebGL template (Assets/WebGLTemplates/YandexGames/index.html), and forwards
// results back into Unity via SendMessage.
//
// Docs: https://yandex.ru/dev/games/doc/ru/sdk/sdk-about

mergeInto(LibraryManager.library, {

  YandexSDK_Init: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);

    function sendToUnity(method, message) {
      var instance = window.unityInstance;
      if (instance) {
        instance.SendMessage(goName, method, message || '');
      } else {
        console.warn('[YandexGamesSDK] unityInstance not found, cannot deliver ' + method);
      }
    }

    if (typeof YaGames === 'undefined') {
      console.warn('[YandexGamesSDK] YaGames script not found. Are you running outside Yandex Games?');
      sendToUnity('OnSDKInitFailed', 'YaGames script not present');
      return;
    }

    YaGames.init().then(function (ysdk) {
      window.__ysdk = ysdk;
      console.log('[YandexGamesSDK] Initialized. environment:', ysdk.environment);
      sendToUnity('OnSDKInitialized', '');
    }).catch(function (err) {
      console.error('[YandexGamesSDK] Init failed', err);
      sendToUnity('OnSDKInitFailed', String(err));
    });
  },

  // Tells the platform the game finished loading and the loading screen can be hidden.
  YandexSDK_GameReady: function () {
    if (window.__ysdk && window.__ysdk.features && window.__ysdk.features.LoadingAPI) {
      window.__ysdk.features.LoadingAPI.ready();
    }
  },

  // Tells the platform active gameplay has begun (pauses ads/rotations while playing).
  YandexSDK_GameplayStart: function () {
    if (window.__ysdk && window.__ysdk.features && window.__ysdk.features.GameplayAPI) {
      window.__ysdk.features.GameplayAPI.start();
    }
  },

  // Tells the platform active gameplay has stopped (e.g. game over / menu).
  YandexSDK_GameplayStop: function () {
    if (window.__ysdk && window.__ysdk.features && window.__ysdk.features.GameplayAPI) {
      window.__ysdk.features.GameplayAPI.stop();
    }
  },

  YandexSDK_ShowInterstitial: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);

    if (!window.__ysdk) return;

    window.__ysdk.adv.showFullscreenAdv({
      callbacks: {
        onClose: function (wasShown) {
          var instance = window.unityInstance;
          if (instance) instance.SendMessage(goName, 'OnInterstitialClosed', wasShown ? '1' : '0');
        },
        onError: function (err) {
          console.error('[YandexGamesSDK] Interstitial error', err);
          var instance = window.unityInstance;
          if (instance) instance.SendMessage(goName, 'OnInterstitialClosed', '0');
        }
      }
    });
  },

  YandexSDK_ShowRewarded: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);

    if (!window.__ysdk) return;

    var rewarded = false;

    window.__ysdk.adv.showRewardedVideo({
      callbacks: {
        onRewarded: function () {
          rewarded = true;
        },
        onClose: function () {
          var instance = window.unityInstance;
          if (instance) instance.SendMessage(goName, 'OnRewardedClosed', rewarded ? '1' : '0');
        },
        onError: function (err) {
          console.error('[YandexGamesSDK] Rewarded error', err);
          var instance = window.unityInstance;
          if (instance) instance.SendMessage(goName, 'OnRewardedClosed', '0');
        }
      }
    });
  },

  YandexSDK_GetLang: function () {
    var lang = 'en';
    if (window.__ysdk && window.__ysdk.environment && window.__ysdk.environment.i18n) {
      lang = window.__ysdk.environment.i18n.lang || 'en';
    } else {
      console.warn('[YandexGamesSDK] environment.i18n not available, defaulting to en. ysdk:', window.__ysdk);
    }
    console.log('[YandexGamesSDK] Detected language:', lang);
    var bufferSize = lengthBytesUTF8(lang) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(lang, buffer, bufferSize);
    return buffer;
  },

  // Requirement 1.3: sound must stop when the page/tab is hidden (minimized, switched away, etc).
  // Registers a document.visibilitychange listener that forwards state to Unity.
  YandexSDK_RegisterVisibilityHandler: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);

    function notify(isVisible) {
      var instance = window.unityInstance;
      if (instance) {
        instance.SendMessage(goName, 'OnPageVisibilityChanged', isVisible ? '1' : '0');
      }
    }

    document.addEventListener('visibilitychange', function () {
      notify(document.visibilityState === 'visible');
    });

    // Also react to the Yandex SDK's own pause/resume events (e.g. when an ad or the
    // platform overlay takes focus), per requirement 1.19.4.
    if (window.__ysdk && window.__ysdk.on) {
      window.__ysdk.on('game_api_pause', function () { notify(false); });
      window.__ysdk.on('game_api_resume', function () { notify(true); });
    }
  }

});
