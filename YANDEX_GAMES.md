# Yandex Games integration

This adds Yandex Games SDK support, RU/EN localization, and several platform
technical-requirement fixes to Dungeon Match Heroes for WebGL builds.
Based on the official requirements page (last updated 2026-07-01):
https://yandex.ru/dev/games/doc/ru/concepts/requirements

## What was added / changed

**SDK core**
- `Assets/Plugins/WebGL/YandexGamesSDK.jslib` — JS bridge to `YaGames` (init,
  `LoadingAPI.ready`, `GameplayAPI.start/stop`, interstitial & rewarded ads,
  language detection, page-visibility forwarding).
- `Assets/Scripts/YandexGames/YandexGamesManager.cs` — C# singleton wrapping
  the bridge; mocks a successful init on Editor/non-WebGL so gameplay code
  can call it unconditionally. Automatically calls `GameplayAPI.stop()`/`start()`
  around ad playback and browser tab switches while a run is active, per the
  official docs' list of start/stop triggers.
- `Assets/WebGLTemplates/YandexGames/index.html` — WebGL template that loads
  the SDK from `/sdk.js` (the relative path required for games uploaded via
  the developer Console — see note below if you host on your own domain).
- Hooks: `TitleScreenController` -> `GameplayStart()`; `GameOverScreenController`
  -> `GameplayStop()` + `ShowInterstitialAd()`.

**Localization (requirement 2.10 - RU for ru/be/kk/uk/uz, EN otherwise; 2.14 -
auto language detection via SDK)**
- `Assets/Scripts/YandexGames/LocalizationManager.cs` - RU/EN string table,
  language resolved automatically from `ysdk.environment.i18n.lang`
  (`YandexSDK_GetLang` in the jslib), with an `Application.systemLanguage`
  fallback outside Yandex Games.
- Wired into: `TitleScreenController` (start message, best-wave label),
  `GameOverScreenController` (all result labels), `CombatManager` (HP/SHIELD/EXP
  labels, KEY state, victory/monsters-approach banners, chest-interaction
  messages), `StartHintController` (gameplay hint).
- **Not yet translated:** the `TIPS` / `TIPS_JP` gameplay-tip content array in
  `CombatManager.cs` only has English + Japanese variants. These tips show
  between waves; if you want full RU coverage you'll need to add a `TIPS_RU`
  array and branch on `LocalizationManager.CurrentLanguage` in `ShowTipRoutine`.

**Technical requirements fixes**
- **1.3** (sound must stop when the tab/page is hidden): jslib listens to
  `document.visibilitychange` and the SDK's `game_api_pause`/`game_api_resume`
  events, forwarded to `YandexGamesManager.OnPageVisibilityChanged`, which
  toggles `AudioListener.pause`.
- **1.6.1.8 / 1.6.2.7** (no text selection / context menu from interacting with
  the game field): WebGL template disables `contextmenu`, `selectstart`, and
  page scroll/zoom gestures.
- **1.10.2** (no browser scroll/swipe-to-refresh): `touch-action: none` +
  `overscroll-behavior: none` + wheel/touchmove prevention in the template.
- **1.19.2** (`LoadingAPI.ready()` called once the player can start playing):
  called right after SDK init completes in `YandexGamesManager`.
- **1.9 / 2.6** (persist progress immediately; endless/score-based games need a
  best-result record): `Assets/Scripts/YandexGames/GameProgress.cs` saves the
  best wave reached via `PlayerPrefs` immediately on game over, shown on both
  the Game Over and Title screens.

## Still needs attention before submitting (not code - Console/config side)

These are real requirements from the checklist that this patch can't satisfy
by editing files - they depend on your build settings, Console draft, and
account setup:

- **1.1 / 1.19.1** - build with the **YandexGames** WebGL template selected in
  Player Settings (see setup steps below).
- **1.6.2.1-1.6.2.2** - on desktop, the active field must stretch to fill the
  available area (long side <= 2x short side). The template here is 100%/100%
  and will stretch; verify your camera/UI scales acceptably at very wide or
  very tall aspect ratios.
- **1.12** - Yandex Ad Network (RSYA) monetization must be connected in the
  developer Console if you want banner/video ad revenue; this is a Console
  setting, not a code change.
- **2.7** - set an accurate age-rating tag for the content in the Console
  draft.
- **2.9** - main content should take **more than 10 minutes** to complete;
  confirm wave/level pacing supports this.
- **5.x** - icon, cover, screenshots, and store text fields are filled out
  correctly in the Console draft (see full checklist for exact specs).
- **6.9** (recommended) - if you ever add a manual language switcher, it must
  use a globe/gear icon and label each language in itself (Русский, English),
  not rely on the current UI language.

## One-time Editor setup

1. **Switch build target to WebGL**: `File > Build Settings > WebGL > Switch Platform`.
2. **Select the Yandex Games WebGL template**: `Project Settings > Player >
   Resolution and Presentation > WebGL Template` -> **YandexGames**.
3. **Publishing Settings**: `Compression Format` -> **Disabled** (simplest), or
   Gzip/Brotli with `Decompression Fallback` enabled.
4. **Build**: `File > Build Settings > Build`, e.g. to `Build/WebGL`.

**Hosting note:** the template loads the SDK from the relative path `/sdk.js`,
which is correct (and required to pass moderation) when you upload the build
as a zip through the Console — Yandex serves your game from a path under its
own domain, so `/sdk.js` resolves there automatically. If instead you embed
the game via iframe from your own domain, change the script tag in
`Assets/WebGLTemplates/YandexGames/index.html` to the absolute URL
`https://sdk.games.s3.yandex.net/sdk.js`.

## Publishing to Yandex Games

1. Zip the **contents** of the build output folder so `index.html` sits at the
   zip root.
2. Upload the zip as a draft in the developer console: https://games.yandex.ru/
3. Use the console's test link and its debug panel to check for console errors
   and confirm the SDK initializes (outside Yandex Games, `YaGames` won't
   exist and `YandexGamesManager` logs a warning but the game still runs).

## Extending

- `YandexGamesManager.Instance.ShowRewardedAd(granted => { ... })` is wired up
  for e.g. a future "revive"/"continue" mechanic.
- To add a language, add cases to `LocalizationManager.ResolveLanguage` and
  new entries to each key in the `Table` dictionary.
- Leaderboards, cloud saves, and in-game purchases follow the same pattern:
  add a `YandexSDK_*` function to the `.jslib`, a matching `[DllImport]` +
  wrapper in `YandexGamesManager`, and call it from gameplay code.
