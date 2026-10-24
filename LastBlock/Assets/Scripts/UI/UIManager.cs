using System.Collections;
using LastBlock.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBlock.UI
{
    /// <summary>
    /// HUD (level, score, blocks left, hold countdown, toasts) and the Victory / Game Over panels.
    /// Button listeners are added in code, so the scene needs no persistent UnityEvent wiring.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text removeText;
        [SerializeField] TMP_Text holdText;
        [SerializeField] TMP_Text toastText;
        [SerializeField] Button restartButton;

        [Header("Victory")]
        [SerializeField] GameObject victoryPanel;
        [SerializeField] RectTransform victoryCard;
        [SerializeField] TMP_Text victoryTitleText;
        [SerializeField] TMP_Text victoryScoreText;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text nextButtonLabel;

        [Header("Game Over")]
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] RectTransform gameOverCard;
        [SerializeField] TMP_Text gameOverScoreText;
        [SerializeField] Button tryAgainButton;

        [Header("Animation")]
        [SerializeField] float popDuration = 0.35f;
        [SerializeField] float toastDuration = 1.2f;

        Coroutine toastRoutine;
        Coroutine panelRoutine;

        /// <summary>Connects buttons to the game manager. Safe to call more than once.</summary>
        public void Bind(GameManager gameManager)
        {
            if (gameManager == null) return;
            BindButton(restartButton, gameManager.RestartLevel);
            BindButton(nextButton, gameManager.NextLevel);
            BindButton(tryAgainButton, gameManager.RestartLevel);
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        // ---------------------------------------------------------------- HUD

        public void SetLevel(int level) => SetText(levelText, $"LEVEL {level}");
        public void SetScore(int score) => SetText(scoreText, $"SCORE: {score}");

        public void SetRemaining(int remaining)
        {
            SetText(removeText, remaining > 0
                ? $"REMOVE: {remaining} {(remaining == 1 ? "BLOCK" : "BLOCKS")}"
                : "HOLD IT!");
        }

        public void ShowHold(float secondsLeft)
        {
            if (holdText == null) return;
            holdText.gameObject.SetActive(true);
            holdText.text = $"HOLD STEADY... {Mathf.Max(1, Mathf.CeilToInt(secondsLeft))}";
        }

        public void HideHold()
        {
            if (holdText != null) holdText.gameObject.SetActive(false);
        }

        public void SetRestartVisible(bool visible)
        {
            if (restartButton != null) restartButton.gameObject.SetActive(visible);
        }

        public void ShowToast(string message, Color color)
        {
            if (toastText == null) return;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(message, color));
        }

        IEnumerator ToastRoutine(string message, Color color)
        {
            toastText.gameObject.SetActive(true);
            toastText.text = message;
            RectTransform rect = toastText.rectTransform;
            float time = 0f;
            while (time < toastDuration)
            {
                time += Time.unscaledDeltaTime;
                float k = time / toastDuration;
                Color c = color;
                c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                toastText.color = c;
                rect.localScale = Vector3.one * (1f + 0.25f * Mathf.Max(0f, 1f - k * 6f));
                yield return null;
            }
            toastText.gameObject.SetActive(false);
            toastRoutine = null;
        }

        // ---------------------------------------------------------------- panels

        public void HidePanels()
        {
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = null;
            // Cards are centred by the setup tool; reset in case an animation was interrupted.
            ResetCard(victoryCard);
            ResetCard(gameOverCard);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            HideHold();
            SetRestartVisible(true);
        }

        public void ShowVictory(int score, bool newBest, bool isLastLevel)
        {
            HideHold();
            SetRestartVisible(false);
            SetText(victoryTitleText, isLastLevel ? "ALL LEVELS COMPLETE!" : "LEVEL COMPLETE!");
            SetText(victoryScoreText, newBest ? $"SCORE: {score}\nNEW BEST!" : $"SCORE: {score}");
            SetText(nextButtonLabel, isLastLevel ? "PLAY AGAIN" : "NEXT LEVEL");
            if (victoryPanel == null) return;
            victoryPanel.SetActive(true);
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = StartCoroutine(PopRoutine(victoryCard, false));
        }

        public void ShowGameOver(int score)
        {
            HideHold();
            SetRestartVisible(false);
            SetText(gameOverScoreText, $"SCORE: {score}");
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = StartCoroutine(PopRoutine(gameOverCard, true));
        }

        /// <summary>Scale-in with overshoot; the defeat card also shakes sideways.</summary>
        IEnumerator PopRoutine(RectTransform card, bool shake)
        {
            if (card == null) yield break;
            Vector2 basePosition = card.anchoredPosition;
            float time = 0f;
            while (time < popDuration)
            {
                time += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(time / popDuration);
                card.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }
            card.localScale = Vector3.one;

            if (shake)
            {
                time = 0f;
                const float shakeDuration = 0.4f;
                while (time < shakeDuration)
                {
                    time += Time.unscaledDeltaTime;
                    float damper = 1f - Mathf.Clamp01(time / shakeDuration);
                    card.anchoredPosition = basePosition + new Vector2(Mathf.Sin(time * 60f) * 18f * damper, 0f);
                    yield return null;
                }
                card.anchoredPosition = basePosition;
            }
            panelRoutine = null;
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        static void ResetCard(RectTransform card)
        {
            if (card == null) return;
            card.anchoredPosition = Vector2.zero;
            card.localScale = Vector3.one;
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
