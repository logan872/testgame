using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using YandexGames;

public class TitleScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string nextSceneName = "Main";

    private Label startMessage;
    private Label bestWaveLabel;
    private VisualElement blackout;
    private VisualElement root;
    private bool isStarting = false;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        startMessage = root.Q<Label>("start-message");
        bestWaveLabel = root.Q<Label>("best-wave-label");
        blackout = root.Q("blackout");

        // Set version number from Player Settings
        Label versionLabel = root.Q<Label>("version-label");
        if (versionLabel != null)
        {
            versionLabel.text = "v" + Application.version;
        }

        ApplyLocalizedText();
        LocalizationManager.OnLanguageChanged += ApplyLocalizedText;

        // Make root clickable to start the adventure
        root.RegisterCallback<PointerDownEvent>(OnRootClicked);

        // Start blinking effect
        StartCoroutine(BlinkMessage());

        // Trigger intro animation
        StartCoroutine(TriggerIntro());

        // Play Title SE after a short delay to ensure AudioManager is loaded
        StartCoroutine(PlayTitleSEWithDelay());

        // Show a launch ad (once per session) once the title screen is up.
        StartCoroutine(ShowLaunchAdRoutine());
    }

    private IEnumerator ShowLaunchAdRoutine()
    {
        // Let the intro animation play out first so the ad doesn't cut it off instantly.
        yield return new WaitForSeconds(1.0f);

        // Wait briefly for the SDK to finish initializing (it usually already has by now).
        float timeout = 4f;
        while (YandexGamesManager.Instance != null && !YandexGamesManager.Instance.IsInitialized && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        YandexGamesManager.Instance?.ShowLaunchAdIfNeeded();
    }

    private void ApplyLocalizedText()
    {
        if (startMessage != null)
        {
            startMessage.text = LocalizationManager.T("title.start_message");
            LocalizationManager.ApplyFont(startMessage);
        }

        if (bestWaveLabel != null)
        {
            int best = GameProgress.GetBestWave();
            bestWaveLabel.text = best > 0 ? LocalizationManager.T("title.best_wave", best) : "";
            LocalizationManager.ApplyFont(bestWaveLabel);
        }
    }

    private IEnumerator PlayTitleSEWithDelay()
    {
        // Wait until AudioManager is available
        while (AudioManager.Instance == null)
        {
            yield return null;
        }

        // Additional small delay to be safe
        yield return new WaitForSeconds(0.2f);

        AudioManager.Instance.PlaySE(SEType.Title);
    }

    private IEnumerator TriggerIntro()
    {
        // Wait a bit to ensure the UI has been painted at least once in its intro state
        yield return new WaitForSeconds(0.1f);

        root.Q("background")?.RemoveFromClassList("background--intro");
        root.Q("heroes")?.RemoveFromClassList("heroes--intro");
        root.Q("heroes-shadow")?.RemoveFromClassList("heroes--intro");
        root.Q("logo")?.RemoveFromClassList("logo--intro");
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= ApplyLocalizedText;

        if (root != null)
        {
            root.UnregisterCallback<PointerDownEvent>(OnRootClicked);
        }
    }

    private IEnumerator BlinkMessage()
    {
        if (startMessage == null) yield break;

        while (true)
        {
            startMessage.ToggleInClassList("start-message--hidden");
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void OnRootClicked(PointerDownEvent evt)
    {
        if (isStarting) return;
        isStarting = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.Select);
        }

        StartCoroutine(StartAdventureSequence());
    }

    private IEnumerator StartAdventureSequence()
    {
        // Trigger blackout animation
        blackout?.AddToClassList("blackout--active");

        // Wait for the animation to complete (0.6s in USS)
        yield return new WaitForSeconds(0.7f);

        YandexGames.YandexGamesManager.Instance?.GameplayStart();

        SceneManager.LoadScene(nextSceneName);
    }
}
