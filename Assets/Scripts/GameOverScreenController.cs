using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using YandexGames;

public class GameOverScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string nextSceneName = "Title";

    private VisualElement blackout;
    private VisualElement root;
    private Label lossMessage;
    private VisualElement resultsGroup;
    private Label levelResult;
    private Label expResult;
    private Label waveResult;
    private Label bestWaveResult;
    private Label promptLabel;
    private int bestWave;
    private bool isTransitioning = false;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        blackout = root.Q("blackout");
        lossMessage = root.Q<Label>("loss-message");
        resultsGroup = root.Q("results-group");
        levelResult = root.Q<Label>("level-result");
        expResult = root.Q<Label>("exp-result");
        waveResult = root.Q<Label>("wave-result");
        bestWaveResult = root.Q<Label>("best-wave-result");
        promptLabel = root.Q<Label>("prompt-label");

        // Requirement 2.6: persist a "best" record for this endless/score-based run.
        bestWave = GameProgress.SaveBestWaveIfHigher(GameResults.WavesWon);

        ApplyLocalizedText();
        LocalizationManager.OnLanguageChanged += ApplyLocalizedText;

        // Runtime check: Immediately make it black for the starting fade-in
        blackout?.AddToClassList("blackout--active");

        // Gameplay has ended: let the platform know, and offer an interstitial ad.
        YandexGamesManager.Instance?.GameplayStop();
        YandexGamesManager.Instance?.ShowInterstitialAd();

        // Click to return to title
        root.RegisterCallback<PointerDownEvent>(OnRootClicked);

        // Start Fade-In
        StartCoroutine(FadeInSequence());
    }

    private void ApplyLocalizedText()
    {
        if (lossMessage != null) lossMessage.text = LocalizationManager.T("gameover.title");
        if (levelResult != null) levelResult.text = LocalizationManager.T("gameover.level", GameResults.FinalLevel);
        if (expResult != null) expResult.text = LocalizationManager.T("gameover.exp", GameResults.FinalExperience);
        if (waveResult != null) waveResult.text = LocalizationManager.T("gameover.wave", GameResults.WavesWon);
        if (bestWaveResult != null) bestWaveResult.text = LocalizationManager.T("gameover.best_wave", bestWave);
        if (promptLabel != null) promptLabel.text = LocalizationManager.T("gameover.prompt");
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= ApplyLocalizedText;

        if (root != null)
        {
            root.UnregisterCallback<PointerDownEvent>(OnRootClicked);
        }
    }

    private IEnumerator FadeInSequence()
    {
        // Wait a bit to ensure the UI has been painted at least once in its initial state.
        yield return new WaitForSeconds(0.1f);

        // Start Fade-In (0.8s transition)
        blackout?.RemoveFromClassList("blackout--active");

        // Start showing message earlier (e.g. 0.2s after fade start)
        yield return new WaitForSeconds(0.2f);

        // Show loss message with a scale-up animation
        lossMessage?.AddToClassList("loss-message--visible");
    }

    private void OnRootClicked(PointerDownEvent evt)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.Select);
        }

        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        // Trigger Fade-Out
        blackout?.AddToClassList("blackout--active");

        // Wait for animation (uss duration is 0.8s)
        yield return new WaitForSeconds(1.0f);

        SceneManager.LoadScene(nextSceneName);
    }
}
