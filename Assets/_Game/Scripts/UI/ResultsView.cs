using System.Text;
using Momentum.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>End-of-level screen: time, rank, personal best comparison, splits and navigation.</summary>
    public class ResultsView : MonoBehaviour
    {
        [SerializeField] Text levelNameText;
        [SerializeField] Text timeText;
        [SerializeField] Text rankText;
        [SerializeField] Text bestText;
        [SerializeField] Text newBestText;
        [SerializeField] Text statsText;
        [SerializeField] Text splitsText;
        [SerializeField] Text rankTableText;
        [SerializeField] Button retryButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button levelSelectButton;
        [SerializeField] Button mainMenuButton;

        float revealTimer;
        LevelResult current;

        public Button RetryButton => retryButton;
        public Button NextButton => nextButton;
        public Button LevelSelectButton => levelSelectButton;
        public Button MainMenuButton => mainMenuButton;

        public void Show(LevelResult result)
        {
            current = result;
            gameObject.SetActive(true);
            revealTimer = 0f;

            if (levelNameText != null) levelNameText.text = (result.isEditorTest ? "TEST RUN - " : "") + result.levelName.ToUpperInvariant() + "  COMPLETE";
            if (timeText != null) timeText.text = TimeFormat.Format(result.time);
            if (rankText != null)
            {
                rankText.text = result.rank.ToString();
                rankText.color = RankCalculator.GetColor(result.rank);
                rankText.transform.localScale = Vector3.one * 2.5f;
            }
            if (newBestText != null)
            {
                newBestText.gameObject.SetActive(result.isNewBest);
                newBestText.text = result.firstCompletion ? "FIRST CLEAR!" : "NEW PERSONAL BEST!";
            }
            if (bestText != null)
            {
                if (result.isEditorTest) bestText.text = "Editor test runs are not recorded";
                else if (result.previousBest > 0f)
                {
                    float delta = result.time - result.previousBest;
                    string color = delta <= 0f ? "5CFF7A" : "FF5050";
                    bestText.text = "PREVIOUS BEST  " + TimeFormat.Format(result.previousBest) + "   <color=#" + color + ">" + TimeFormat.FormatDelta(delta) + "</color>";
                }
                else bestText.text = "PERSONAL BEST  " + TimeFormat.Format(result.time);
            }
            if (statsText != null)
            {
                statsText.text = "DEATHS  " + result.deaths + "     KILLS  " + result.kills +
                                 (result.secretsTotal > 0 ? "     SECRETS  " + result.secretsFound + "/" + result.secretsTotal : "");
            }
            if (splitsText != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < result.splits.Count; i++)
                {
                    sb.Append("CP ").Append(i + 1).Append("   ");
                    float s = result.splits[i];
                    sb.Append(s >= 0f ? TimeFormat.Format(s) : "--:--.--");
                    float d = i < result.splitDeltas.Count ? result.splitDeltas[i] : float.NaN;
                    if (!float.IsNaN(d))
                    {
                        sb.Append("   <color=#").Append(d <= 0f ? "5CFF7A" : "FF5050").Append('>').Append(TimeFormat.FormatDelta(d)).Append("</color>");
                    }
                    sb.Append('\n');
                }
                splitsText.text = result.splits.Count > 0 ? sb.ToString() : "NO CHECKPOINTS";
            }
            if (rankTableText != null)
            {
                var t = result.thresholds;
                rankTableText.text = "S " + TimeFormat.Format(t.s) + "   A " + TimeFormat.Format(t.a) + "   B " + TimeFormat.Format(t.b) + "   C " + TimeFormat.Format(t.c);
            }

            if (nextButton != null) nextButton.gameObject.SetActive(result.hasNextLevel);
            if (levelSelectButton != null) levelSelectButton.gameObject.SetActive(!result.isEditorTest);
            var menuLabel = UIFactory.GetLabel(mainMenuButton);
            if (menuLabel != null) menuLabel.text = result.isEditorTest ? "BACK TO EDITOR" : "MAIN MENU";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (current == null || rankText == null) return;
            revealTimer += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(revealTimer / 0.35f);
            rankText.transform.localScale = Vector3.one * Mathf.Lerp(2.5f, 1f, 1f - (1f - k) * (1f - k));
        }

        public static ResultsView Build(Transform parent)
        {
            var canvas = UIFactory.CreateCanvas("Results", 60, parent);
            var view = canvas.gameObject.AddComponent<ResultsView>();
            var overlay = UIFactory.CreatePanel("Overlay", canvas.transform, new Color(0f, 0f, 0f, 0.7f));
            UIFactory.Stretch(overlay.rectTransform);

            var panel = UIFactory.CreatePanel("Panel", canvas.transform, UITheme.Background);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 820f));
            var p = panel.rectTransform;

            view.levelNameText = UIFactory.CreateText("Level", p, "LEVEL COMPLETE", 40, TextAnchor.UpperCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(view.levelNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(1100f, 56f));

            view.rankText = UIFactory.CreateText("Rank", p, "S", 220, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold);
            UIFactory.Place(view.rankText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(250f, -270f), new Vector2(360f, 300f));

            view.timeText = UIFactory.CreateText("Time", p, "00:00.00", 96, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(view.timeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(470f, -200f), new Vector2(680f, 110f));
            view.newBestText = UIFactory.CreateText("NewBest", p, "NEW PERSONAL BEST!", 32, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold);
            UIFactory.Place(view.newBestText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(474f, -280f), new Vector2(680f, 44f));
            view.bestText = UIFactory.CreateText("Best", p, "", 26, TextAnchor.MiddleLeft, UITheme.TextDim);
            UIFactory.Place(view.bestText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(474f, -330f), new Vector2(680f, 40f));
            view.statsText = UIFactory.CreateText("Stats", p, "", 24, TextAnchor.MiddleLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(view.statsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(474f, -375f), new Vector2(680f, 36f));

            var splitsHeader = UIFactory.CreateText("SplitsHeader", p, "SPLITS", 24, TextAnchor.UpperLeft, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(splitsHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -440f), new Vector2(500f, 30f));
            view.splitsText = UIFactory.CreateText("Splits", p, "", 22, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Place(view.splitsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -476f), new Vector2(520f, 200f));
            view.splitsText.verticalOverflow = VerticalWrapMode.Truncate;

            var ranksHeader = UIFactory.CreateText("RanksHeader", p, "RANK TIMES", 24, TextAnchor.UpperLeft, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(ranksHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, -440f), new Vector2(500f, 30f));
            view.rankTableText = UIFactory.CreateText("RankTable", p, "", 20, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Place(view.rankTableText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, -476f), new Vector2(520f, 60f));
            view.rankTableText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var buttons = UIFactory.CreateRect("Buttons", p);
            buttons.anchorMin = new Vector2(0f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(0.5f, 0f);
            buttons.anchoredPosition = new Vector2(0f, 36f);
            buttons.sizeDelta = new Vector2(-80f, 70f);
            UIFactory.AddHorizontal(buttons.gameObject, 16f, null, TextAnchor.MiddleCenter, true);
            view.retryButton = UIFactory.CreateButton("Retry", buttons, "RETRY", 28);
            view.nextButton = UIFactory.CreateButton("Next", buttons, "NEXT LEVEL", 28);
            view.levelSelectButton = UIFactory.CreateButton("LevelSelect", buttons, "LEVEL SELECT", 28);
            view.mainMenuButton = UIFactory.CreateButton("MainMenu", buttons, "MAIN MENU", 28);
            canvas.gameObject.SetActive(false);
            return view;
        }
    }
}
