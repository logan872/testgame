using UnityEngine;

namespace YandexGames
{
    /// <summary>
    /// Persists the player's best result locally via PlayerPrefs, saved immediately
    /// (requirement 1.9: progress must be saved right after the relevant action, and
    /// survive a page refresh) and used to satisfy requirement 2.6 (an endless/score-based
    /// game must keep a best-result record).
    /// </summary>
    public static class GameProgress
    {
        private const string BestWaveKey = "DMH_BestWave";

        /// <summary>Saves the wave count if it's a new best, and returns the (possibly updated) best.</summary>
        public static int SaveBestWaveIfHigher(int wave)
        {
            int best = PlayerPrefs.GetInt(BestWaveKey, 0);
            if (wave > best)
            {
                best = wave;
                PlayerPrefs.SetInt(BestWaveKey, best);
                PlayerPrefs.Save();
            }
            return best;
        }

        public static int GetBestWave() => PlayerPrefs.GetInt(BestWaveKey, 0);
    }
}
