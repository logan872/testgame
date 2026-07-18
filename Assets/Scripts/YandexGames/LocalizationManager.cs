using System;
using System.Collections.Generic;
using UnityEngine;

namespace YandexGames
{
    public enum GameLanguage
    {
        English,
        Russian
    }

    /// <summary>
    /// Minimal localization system covering the two languages required for Yandex Games
    /// publication (see requirement 2.10 — RU for ru/be/kk/uk/uz locales, EN otherwise).
    ///
    /// Language is set automatically from <see cref="YandexGamesManager"/> using the
    /// SDK's `environment.i18n.lang` value (requirement 2.14 — auto language detection),
    /// with a system-language fallback when running outside Yandex Games (Editor, itch.io, etc).
    /// </summary>
    public static class LocalizationManager
    {
        public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

        /// <summary>Fired whenever the active language changes, so visible UI can refresh itself.</summary>
        public static event Action OnLanguageChanged;

        private static readonly Dictionary<string, Dictionary<GameLanguage, string>> Table = new()
        {
            ["title.start_message"] = new()
            {
                [GameLanguage.English] = "Click to Start Adventure",
                [GameLanguage.Russian] = "Нажмите, чтобы начать приключение",
            },
            ["title.best_wave"] = new()
            {
                [GameLanguage.English] = "Best Wave: {0}",
                [GameLanguage.Russian] = "Лучшая волна: {0}",
            },
            ["hud.hint"] = new()
            {
                [GameLanguage.English] = "Click bottom row to shatter blocks!",
                [GameLanguage.Russian] = "Нажмите на нижний ряд, чтобы разбить блоки!",
            },
            ["hud.treasure_found"] = new()
            {
                [GameLanguage.English] = "You found a treasure chest!",
                [GameLanguage.Russian] = "Вы нашли сундук с сокровищами!",
            },
            ["hud.chest_found"] = new()
            {
                [GameLanguage.English] = "You found a chest!",
                [GameLanguage.Russian] = "Вы нашли сундук!",
            },
            ["hud.chest_unlocked"] = new()
            {
                [GameLanguage.English] = "Unlocked with the key!\nBonus EXP obtained!",
                [GameLanguage.Russian] = "Открыт ключом!\nПолучен бонусный опыт!",
            },
            ["hud.chest_no_key"] = new()
            {
                [GameLanguage.English] = "No key to open it...",
                [GameLanguage.Russian] = "Нет ключа, чтобы его открыть...",
            },
            ["hud.victory"] = new()
            {
                [GameLanguage.English] = "VICTORY!",
                [GameLanguage.Russian] = "ПОБЕДА!",
            },
            ["hud.monsters_approach"] = new()
            {
                [GameLanguage.English] = "MONSTERS APPROACH!",
                [GameLanguage.Russian] = "ПРИБЛИЖАЮТСЯ МОНСТРЫ!",
            },
            ["hud.hp_label"] = new()
            {
                [GameLanguage.English] = "HP:",
                [GameLanguage.Russian] = "ОЗ:",
            },
            ["hud.shield_label"] = new()
            {
                [GameLanguage.English] = "SHIELD:",
                [GameLanguage.Russian] = "ЩИТ:",
            },
            ["hud.exp_label"] = new()
            {
                [GameLanguage.English] = "EXP:",
                [GameLanguage.Russian] = "ОПЫТ:",
            },
            ["hud.key_yes"] = new()
            {
                [GameLanguage.English] = "KEY: YES",
                [GameLanguage.Russian] = "КЛЮЧ: ЕСТЬ",
            },
            ["hud.key_no"] = new()
            {
                [GameLanguage.English] = "KEY: NO",
                [GameLanguage.Russian] = "КЛЮЧ: НЕТ",
            },
            ["gameover.title"] = new()
            {
                [GameLanguage.English] = "Your Adventure Ends Here",
                [GameLanguage.Russian] = "Ваше приключение окончено",
            },
            ["gameover.level"] = new()
            {
                [GameLanguage.English] = "LEVEL: {0}",
                [GameLanguage.Russian] = "УРОВЕНЬ: {0}",
            },
            ["gameover.exp"] = new()
            {
                [GameLanguage.English] = "EXP: {0}",
                [GameLanguage.Russian] = "ОПЫТ: {0}",
            },
            ["gameover.wave"] = new()
            {
                [GameLanguage.English] = "WAVE: {0}",
                [GameLanguage.Russian] = "ВОЛНА: {0}",
            },
            ["gameover.best_wave"] = new()
            {
                [GameLanguage.English] = "BEST WAVE: {0}",
                [GameLanguage.Russian] = "ЛУЧШАЯ ВОЛНА: {0}",
            },
            ["gameover.prompt"] = new()
            {
                [GameLanguage.English] = "Click to Return to Title",
                [GameLanguage.Russian] = "Нажмите, чтобы вернуться в меню",
            },
        };

        /// <summary>Call once with the SDK/browser language code (e.g. "ru", "en", "uk").</summary>
        public static void SetLanguageFromCode(string languageCode)
        {
            var lang = ResolveLanguage(languageCode);
            if (lang == CurrentLanguage) return;

            CurrentLanguage = lang;
            OnLanguageChanged?.Invoke();
        }

        private static GameLanguage ResolveLanguage(string code)
        {
            if (string.IsNullOrEmpty(code)) return GameLanguage.English;

            // Recommended minimal localization per Yandex Games requirement 2.10:
            // Russian for ru/be/kk/uk/uz, English for every other locale.
            switch (code.Trim().ToLowerInvariant())
            {
                case "ru":
                case "be":
                case "kk":
                case "uk":
                case "uz":
                    return GameLanguage.Russian;
                default:
                    return GameLanguage.English;
            }
        }

        /// <summary>Returns the localized string for <paramref name="key"/>, formatted with <paramref name="args"/> if provided.</summary>
        public static string T(string key, params object[] args)
        {
            if (!Table.TryGetValue(key, out var entry))
            {
                Debug.LogWarning($"[Localization] Missing key: {key}");
                return key;
            }

            string raw = entry.TryGetValue(CurrentLanguage, out var value) ? value : entry[GameLanguage.English];
            return args is { Length: > 0 } ? string.Format(raw, args) : raw;
        }
    }
}
