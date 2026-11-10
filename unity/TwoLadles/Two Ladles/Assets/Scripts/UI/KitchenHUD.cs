using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Kitchen;
using TwoLadles.Player;
using TwoLadles.Networking;

namespace TwoLadles.UI
{
    public class KitchenHUD : MonoBehaviour
    {
        /// <summary>Mobile joystick centre, anchored bottom-left (moved up/right from 92,92 in v1.4.1).</summary>
        public static readonly Vector2 MOBILE_JOYSTICK_POS = new Vector2(150f, 140f);

        [Header("Top Header & Wave/Reputation Bars")]
        public Text shiftTimerText;
        public Text scoreText;
        public Slider flowSlider;
        public Text flowText;
        public GameObject rushBannerObj;
        public Button returnToLobbyButton;
        public Button switchChefButton;
        public Text switchChefButtonText;
        public Button recipeBookButton;
        public Button touchControlsToggleButton;
        public Button audioMuteButton;
        public Text audioMuteButtonText;

        [Header("Reputation Health Bar (Fighting-Game Trailing Damage)")]
        public Text reputationText;
        public RectTransform reputationMainFill;
        public Image reputationMainImage;
        public RectTransform reputationTrailingFill;
        public float displayedReputationTarget = 100.0f;
        public float displayedReputationTrailing = 100.0f;
        private float reputationTrailHoldTimer = 0.0f;
        private const float REPUTATION_BAR_WIDTH = 156.0f;

        [Header("Wave Progress Bar")]
        public RectTransform waveProgressFill;
        private const float WAVE_BAR_WIDTH = 108.0f;

        [Header("Tutorial Guidance")]
        public GameObject tutorialPanelObj;
        public Text tutorialTitleText;
        public Text tutorialInstructionText;

        [Header("Visual Order Queue (Progress Bars)")]
        public Text orderListText;
        private GameObject[] orderCardObjs = new GameObject[4];
        private Image[] orderCardIcons = new Image[4];
        private Text[] orderCardLabels = new Text[4];
        private RectTransform[] orderCardBarFills = new RectTransform[4];
        private Image[] orderCardBarImages = new Image[4];

        [Header("Plate Stack")]
        public Text cleanPlatesText;
        public Text dirtyPlatesText;

        [Header("Jiwon Crew Bar")]
        public GameObject jiwonCrewBar;
        public Button hirePrepButton;
        public Button hireDishwasherButton;
        public Text hirePrepCooldownText;
        public Text hireDishwasherCooldownText;

        [Header("Mobile Virtual Joystick & Action Buttons")]
        public GameObject mobileControlsContainer;
        public RectTransform joystickBackgroundRect;
        public RectTransform joystickKnobRect;
        public Button primaryActionButton;
        public Button secondaryActionButton;
        public Button throwActionButton;
        public Button dpadUpButton;
        public Button dpadDownButton;
        public Button dpadLeftButton;
        public Button dpadRightButton;
        public Text promptText;

        [Header("Feedback Banner")]
        public Text feedbackBannerText;
        private float feedbackTimer = 0.0f;

        [Header("Recipe Book Modal")]
        public GameObject recipeBookModal;
        public Button closeRecipeBookButton;
        public Text recipeBookTitleText;
        public Text[] recipeRowTitleTexts;

        [Header("End of Shift / Level Results Modal")]
        public GameObject endShiftModal;
        public Text endShiftTitleText;
        public Text endShiftDetailsText;
        public Button openShopFromResultsButton;
        public Button retryShiftButton;
        public Text retryShiftButtonText;
        public Button resultsToLobbyButton;

        [Header("Between-Level Upgrade Shop Modal")]
        public GameObject upgradeShopModal;
        public Text shopGoldText;
        public Text shopFeedbackText;
        public Button shopNextLevelButton;
        public Button shopCloseButton;
        private Dictionary<string, Button> shopBuyButtons = new Dictionary<string, Button>();
        private Dictionary<string, Text> shopBuyLabels = new Dictionary<string, Text>();
        private Dictionary<string, Text> shopLevelLabels = new Dictionary<string, Text>();

        [Header("Two-Bot QA Testing Report Modal")]
        public GameObject qaReportModal;
        public Text qaReportTitleText;
        public Text qaReportDetailsText;
        public Button qaReportCloseButton;
        public Button qaReportRerunButton;

        [Header("QA Mode Active Indicator Banner")]
        public GameObject qaModeBannerObj;
        public Text qaModeBannerText;

        public static KitchenHUD Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (shiftTimerText == null)
            {
                AutoBuildUI();
            }
        }

        private static Font _cachedPixelFont;
        private static Sprite _cachedCircleSprite;

        public static Font GetDefaultFont()
        {
            if (_cachedPixelFont != null)
            {
                if (_cachedPixelFont.material != null && _cachedPixelFont.material.mainTexture != null)
                    _cachedPixelFont.material.mainTexture.filterMode = FilterMode.Point;
                return _cachedPixelFont;
            }

            Font font = null;
            try { font = Resources.Load<Font>("Fonts/PixelFont"); } catch { }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            }
            if (font == null)
            {
                try
                {
                    var osFonts = Font.GetOSInstalledFontNames();
                    if (osFonts != null && osFonts.Length > 0)
                    {
                        font = Font.CreateDynamicFontFromOSFont(osFonts[0], 14);
                    }
                }
                catch { }
            }
            if (font != null)
            {
                // Fallback font names across Windows, Android, Mac/iOS for Korean Hangul and extended Unicode glyphs
                font.fontNames = new string[] { 
                    "PixelFont", 
                    "Malgun Gothic", 
                    "NanumGothic", 
                    "Apple SD Gothic Neo", 
                    "Noto Sans CJK KR", 
                    "Noto Sans KR", 
                    "Segoe UI", 
                    "Arial" 
                };

                if (font.material != null && font.material.mainTexture != null)
                {
                    font.material.mainTexture.filterMode = FilterMode.Point;
                }
            }
            _cachedPixelFont = font;
            return font;
        }

        public static Sprite GetCircleSprite()
        {
            if (_cachedCircleSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                float center = (size - 1) * 0.5f;
                float radius = size * 0.47f;
                Color clear = new Color(0f, 0f, 0f, 0f);
                Color border = new Color(0.12f, 0.14f, 0.18f, 1.0f);
                Color rim = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                Color fill = new Color(0.92f, 0.94f, 0.98f, 1.0f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        if (dist > radius)
                            tex.SetPixel(x, y, clear);
                        else if (dist > radius - 2.2f)
                            tex.SetPixel(x, y, border);
                        else if (dist > radius - 4.0f)
                            tex.SetPixel(x, y, rim);
                        else
                            tex.SetPixel(x, y, fill);
                    }
                }
                tex.Apply();
                _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            }
            return _cachedCircleSprite;
        }

        private Text CreateLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, string text, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var txt = go.AddComponent<Text>();
            txt.font = GetDefaultFont();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return txt;
        }

        private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, string labelText, Color bgColor, out Text outLabel, int fontSize = 12)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.15f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            outLabel = CreateLabel(name + "_Label", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -2f), labelText, fontSize, Color.white, TextAnchor.MiddleCenter);
            return btn;
        }

        private Button CreateCircleActionButton(string name, Transform parent, Vector2 anchoredPos, Vector2 sizeDelta, string labelText, Color bgColor, int fontSize = 11)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.sprite = GetCircleSprite();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.15f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            CreateLabel(name + "_Label", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f), labelText, fontSize, Color.white, TextAnchor.MiddleCenter);
            return btn;
        }

        private Slider CreateFlowSlider(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject sliderObj = new GameObject("FlowSlider", typeof(RectTransform));
            sliderObj.transform.SetParent(parent, false);
            var rect = sliderObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fRect = fill.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var fImg = fill.AddComponent<Image>();
            fImg.color = new Color(0.95f, 0.68f, 0.15f, 1.0f);

            slider.fillRect = fRect;
            slider.targetGraphic = bgImg;
            return slider;
        }

        public void AutoBuildUI()
        {
            if (shiftTimerText != null) return;

            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            var root = rootRect.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // =========================================================================
            // 1. TOP BAR PANEL (Visual Bars for Reputation, Wave Progress, and Flow)
            // =========================================================================
            GameObject topBar = new GameObject("TopBarPanel", typeof(RectTransform));
            topBar.transform.SetParent(root, false);
            var tbRect = topBar.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 1f);
            tbRect.anchorMax = new Vector2(1f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = Vector2.zero;
            tbRect.sizeDelta = new Vector2(0f, 38f);
            var tbImg = topBar.AddComponent<Image>();
            tbImg.color = new Color(0.07f, 0.08f, 0.12f, 0.92f);

            // A) Fighting-Game Reputation Health Bar (Left: X = 12..250)
            reputationText = CreateLabel("ReputationLabel", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(76f, 28f), "REP 100", 13, new Color(0.35f, 1.0f, 0.55f), TextAnchor.MiddleLeft);

            GameObject repBarBg = new GameObject("ReputationBarBg", typeof(RectTransform));
            repBarBg.transform.SetParent(topBar.transform, false);
            var rbgRect = repBarBg.GetComponent<RectTransform>();
            rbgRect.anchorMin = new Vector2(0f, 0.5f);
            rbgRect.anchorMax = new Vector2(0f, 0.5f);
            rbgRect.pivot = new Vector2(0f, 0.5f);
            rbgRect.anchoredPosition = new Vector2(82f, 0f);
            rbgRect.sizeDelta = new Vector2(REPUTATION_BAR_WIDTH, 16f);
            var rbgImg = repBarBg.AddComponent<Image>();
            rbgImg.color = new Color(0.22f, 0.08f, 0.08f, 0.95f);

            // White Trailing Damage Bar (purely visual, renders behind main green fill)
            GameObject repTrail = new GameObject("ReputationTrailingFill", typeof(RectTransform));
            repTrail.transform.SetParent(repBarBg.transform, false);
            reputationTrailingFill = repTrail.GetComponent<RectTransform>();
            reputationTrailingFill.anchorMin = new Vector2(0f, 0f);
            reputationTrailingFill.anchorMax = new Vector2(0f, 1f);
            reputationTrailingFill.pivot = new Vector2(0f, 0.5f);
            reputationTrailingFill.anchoredPosition = Vector2.zero;
            reputationTrailingFill.sizeDelta = new Vector2(REPUTATION_BAR_WIDTH, 0f);
            var rtImg = repTrail.AddComponent<Image>();
            rtImg.color = new Color(1.0f, 1.0f, 1.0f, 0.95f);

            // Main Health Bar Fill (decreases immediately on damage)
            GameObject repMain = new GameObject("ReputationMainFill", typeof(RectTransform));
            repMain.transform.SetParent(repBarBg.transform, false);
            reputationMainFill = repMain.GetComponent<RectTransform>();
            reputationMainFill.anchorMin = new Vector2(0f, 0f);
            reputationMainFill.anchorMax = new Vector2(0f, 1f);
            reputationMainFill.pivot = new Vector2(0f, 0.5f);
            reputationMainFill.anchoredPosition = Vector2.zero;
            reputationMainFill.sizeDelta = new Vector2(REPUTATION_BAR_WIDTH, 0f);
            reputationMainImage = repMain.AddComponent<Image>();
            reputationMainImage.color = new Color(0.22f, 0.88f, 0.42f, 1.0f);

            // B) Level & Wave Progress Bar (X = 252..470)
            shiftTimerText = CreateLabel("WaveLabel", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(252f, 0f), new Vector2(105f, 28f), "L1 • WAVE 1/3", 12, Color.white, TextAnchor.MiddleLeft);

            GameObject waveBarBg = new GameObject("WaveBarBg", typeof(RectTransform));
            waveBarBg.transform.SetParent(topBar.transform, false);
            var wbgRect = waveBarBg.GetComponent<RectTransform>();
            wbgRect.anchorMin = new Vector2(0f, 0.5f);
            wbgRect.anchorMax = new Vector2(0f, 0.5f);
            wbgRect.pivot = new Vector2(0f, 0.5f);
            wbgRect.anchoredPosition = new Vector2(354f, 0f);
            wbgRect.sizeDelta = new Vector2(WAVE_BAR_WIDTH, 14f);
            var wbgImg = waveBarBg.AddComponent<Image>();
            wbgImg.color = new Color(0.16f, 0.20f, 0.28f, 0.95f);

            GameObject waveFillObj = new GameObject("WaveBarFill", typeof(RectTransform));
            waveFillObj.transform.SetParent(waveBarBg.transform, false);
            waveProgressFill = waveFillObj.GetComponent<RectTransform>();
            waveProgressFill.anchorMin = new Vector2(0f, 0f);
            waveProgressFill.anchorMax = new Vector2(0f, 1f);
            waveProgressFill.pivot = new Vector2(0f, 0.5f);
            waveProgressFill.anchoredPosition = Vector2.zero;
            waveProgressFill.sizeDelta = new Vector2(0f, 0f);
            var wfImg = waveFillObj.AddComponent<Image>();
            wfImg.color = new Color(0.30f, 0.78f, 1.0f, 1.0f);

            // C) Flow Bar & Plates & Score
            flowText = CreateLabel("FlowText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(474f, 0f), new Vector2(48f, 28f), "FLOW", 11, new Color(1.0f, 0.82f, 0.25f), TextAnchor.MiddleLeft);
            flowSlider = CreateFlowSlider(topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(518f, 0f), new Vector2(82f, 14f));

            cleanPlatesText = CreateLabel("CleanPlatesText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(610f, 0f), new Vector2(72f, 28f), "", 11, new Color(0.75f, 1.0f, 0.75f), TextAnchor.MiddleLeft);
            cleanPlatesText.gameObject.SetActive(false);
            dirtyPlatesText = CreateLabel("DirtyPlatesText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(674f, 0f), new Vector2(65f, 28f), "", 11, new Color(1.0f, 0.65f, 0.65f), TextAnchor.MiddleLeft);
            dirtyPlatesText.gameObject.SetActive(false);
            scoreText = CreateLabel("ScoreText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(616f, 0f), new Vector2(120f, 28f), "0 pts", 13, Color.yellow, TextAnchor.MiddleLeft);

            // D) Top-Right Utility Buttons: Recipes [R], Touch UI, Mute, Switch Chef [C], Lobby
            returnToLobbyButton = CreateButton("ReturnToLobbyBtn", topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(72f, 26f), "Lobby", new Color(0.55f, 0.20f, 0.20f, 0.95f), out _, 11);
            switchChefButton = CreateButton("SwitchChefBtn", topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-88f, 0f), new Vector2(115f, 26f), "LINH [C]", new Color(0.18f, 0.42f, 0.65f, 0.95f), out switchChefButtonText, 11);
            recipeBookButton = CreateButton("RecipeBookBtn", topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-209f, 0f), new Vector2(100f, 26f), "Recipes [R]", new Color(0.65f, 0.42f, 0.14f, 0.95f), out _, 11);
            touchControlsToggleButton = CreateButton("TouchToggleBtn", topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-315f, 0f), new Vector2(72f, 26f), "Joypad", new Color(0.24f, 0.36f, 0.48f, 0.95f), out _, 11);
            audioMuteButton = CreateButton("AudioMuteBtn", topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-393f, 0f), new Vector2(68f, 26f), "BGM: ON", new Color(0.22f, 0.25f, 0.32f, 0.95f), out audioMuteButtonText, 11);

            // =========================================================================
            // 2. TUTORIAL GUIDANCE CARD (Non-Overlapping, Compact Top-Center)
            // =========================================================================
            GameObject tutPanel = new GameObject("TutorialGuidancePanel", typeof(RectTransform));
            tutPanel.transform.SetParent(root, false);
            tutorialPanelObj = tutPanel;
            var tutRect = tutPanel.GetComponent<RectTransform>();
            tutRect.anchorMin = new Vector2(0.5f, 1f);
            tutRect.anchorMax = new Vector2(0.5f, 1f);
            tutRect.pivot = new Vector2(0.5f, 1f);
            tutRect.anchoredPosition = new Vector2(0f, -44f);
            tutRect.sizeDelta = new Vector2(480f, 52f);

            var tutImg = tutPanel.AddComponent<Image>();
            tutImg.color = new Color(0.08f, 0.11f, 0.18f, 0.94f);

            tutorialTitleText = CreateLabel("TutorialTitle", tutPanel.transform, new Vector2(0f, 0.52f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(-16f, 0f), "STEP 1: MOVE", 13, new Color(1.0f, 0.88f, 0.2f), TextAnchor.MiddleCenter);
            tutorialInstructionText = CreateLabel("TutorialInstruction", tutPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(-16f, 0f), "[WASD] or Joystick to walk.", 12, Color.white, TextAnchor.MiddleCenter);
            tutorialPanelObj.SetActive(false);

            // =========================================================================
            // 3. VISUAL ORDER QUEUE CARDS (Upper Left — Dish Icon + Patience Bar!)
            // =========================================================================
            GameObject ordersPanel = new GameObject("OrdersPanel", typeof(RectTransform));
            ordersPanel.transform.SetParent(root, false);
            var opRect = ordersPanel.GetComponent<RectTransform>();
            opRect.anchorMin = new Vector2(0f, 1f);
            opRect.anchorMax = new Vector2(0f, 1f);
            opRect.pivot = new Vector2(0f, 1f);
            opRect.anchoredPosition = new Vector2(12f, -44f);
            opRect.sizeDelta = new Vector2(210f, 148f);

            // Hidden fallback text for legacy reference
            orderListText = CreateLabel("OrderListText", ordersPanel.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, "", 1, Color.clear, TextAnchor.UpperLeft);

            for (int i = 0; i < 4; i++)
            {
                GameObject card = new GameObject($"OrderCard_{i}", typeof(RectTransform));
                card.transform.SetParent(ordersPanel.transform, false);
                orderCardObjs[i] = card;
                var cRect = card.GetComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0f, 1f);
                cRect.anchorMax = new Vector2(1f, 1f);
                cRect.pivot = new Vector2(0f, 1f);
                cRect.anchoredPosition = new Vector2(0f, -(i * 36f));
                cRect.sizeDelta = new Vector2(0f, 32f);

                var cBg = card.AddComponent<Image>();
                cBg.color = new Color(0.08f, 0.09f, 0.14f, 0.86f);

                // Dish Icon
                GameObject iconGo = new GameObject("DishIcon", typeof(RectTransform));
                iconGo.transform.SetParent(card.transform, false);
                var icRect = iconGo.GetComponent<RectTransform>();
                icRect.anchorMin = new Vector2(0f, 0.5f);
                icRect.anchorMax = new Vector2(0f, 0.5f);
                icRect.pivot = new Vector2(0f, 0.5f);
                icRect.anchoredPosition = new Vector2(6f, 0f);
                icRect.sizeDelta = new Vector2(24f, 24f);
                orderCardIcons[i] = iconGo.AddComponent<Image>();

                // Short Dish Label
                orderCardLabels[i] = CreateLabel("DishName", card.transform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(36f, -2f), new Vector2(-42f, 0f), "Bánh Mì", 11, Color.white, TextAnchor.MiddleLeft);

                // Patience Progress Bar Bg
                GameObject pBg = new GameObject("PatienceBarBg", typeof(RectTransform));
                pBg.transform.SetParent(card.transform, false);
                var pbRect = pBg.GetComponent<RectTransform>();
                pbRect.anchorMin = new Vector2(0f, 0f);
                pbRect.anchorMax = new Vector2(0f, 0f);
                pbRect.pivot = new Vector2(0f, 0f);
                pbRect.anchoredPosition = new Vector2(36f, 5f);
                pbRect.sizeDelta = new Vector2(164f, 8f);
                var pbImg = pBg.AddComponent<Image>();
                pbImg.color = new Color(0.20f, 0.20f, 0.24f, 0.95f);

                // Patience Progress Bar Fill
                GameObject pFill = new GameObject("PatienceBarFill", typeof(RectTransform));
                pFill.transform.SetParent(pBg.transform, false);
                orderCardBarFills[i] = pFill.GetComponent<RectTransform>();
                orderCardBarFills[i].anchorMin = new Vector2(0f, 0f);
                orderCardBarFills[i].anchorMax = new Vector2(0f, 1f);
                orderCardBarFills[i].pivot = new Vector2(0f, 0.5f);
                orderCardBarFills[i].anchoredPosition = Vector2.zero;
                orderCardBarFills[i].sizeDelta = new Vector2(164f, 0f);
                orderCardBarImages[i] = pFill.AddComponent<Image>();
                orderCardBarImages[i].color = new Color(0.25f, 0.92f, 0.45f, 1.0f);

                card.SetActive(false);
            }

            // =========================================================================
            // 4. DRAGGABLE VIRTUAL JOYSTICK & TOUCH ACTION BUTTONS
            // =========================================================================
            mobileControlsContainer = new GameObject("MobileControlsContainer", typeof(RectTransform));
            mobileControlsContainer.transform.SetParent(root, false);
            var mcRect = mobileControlsContainer.GetComponent<RectTransform>();
            mcRect.anchorMin = Vector2.zero;
            mcRect.anchorMax = Vector2.one;
            mcRect.sizeDelta = Vector2.zero;

            // A) Circular Draggable Virtual Joystick (Bottom Left)
            GameObject joyBgObj = new GameObject("VirtualJoystickPad", typeof(RectTransform));
            joyBgObj.transform.SetParent(mobileControlsContainer.transform, false);
            joystickBackgroundRect = joyBgObj.GetComponent<RectTransform>();
            joystickBackgroundRect.anchorMin = new Vector2(0f, 0f);
            joystickBackgroundRect.anchorMax = new Vector2(0f, 0f);
            joystickBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
            joystickBackgroundRect.anchoredPosition = MOBILE_JOYSTICK_POS;
            joystickBackgroundRect.sizeDelta = new Vector2(128f, 128f);

            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.color = new Color(0.14f, 0.16f, 0.22f, 0.68f);

            // Directional D-Pad sub-buttons around the joystick rim
            Color dpadCol = new Color(0.22f, 0.25f, 0.32f, 0.70f);
            dpadUpButton = CreateButton("DpadUp", joyBgObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(32f, 28f), "▲", dpadCol, out _, 10);
            dpadDownButton = CreateButton("DpadDown", joyBgObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(32f, 28f), "▼", dpadCol, out _, 10);
            dpadLeftButton = CreateButton("DpadLeft", joyBgObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-44f, 0f), new Vector2(28f, 32f), "◀", dpadCol, out _, 10);
            dpadRightButton = CreateButton("DpadRight", joyBgObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 0f), new Vector2(28f, 32f), "▶", dpadCol, out _, 10);

            // Draggable Center Joystick Knob
            GameObject knobObj = new GameObject("JoystickKnob", typeof(RectTransform));
            knobObj.transform.SetParent(joyBgObj.transform, false);
            joystickKnobRect = knobObj.GetComponent<RectTransform>();
            joystickKnobRect.anchorMin = new Vector2(0.5f, 0.5f);
            joystickKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
            joystickKnobRect.pivot = new Vector2(0.5f, 0.5f);
            joystickKnobRect.anchoredPosition = Vector2.zero;
            joystickKnobRect.sizeDelta = new Vector2(48f, 48f);
            var knobImg = knobObj.AddComponent<Image>();
            knobImg.color = new Color(0.95f, 0.80f, 0.25f, 0.90f);
            knobImg.raycastTarget = false;

            SetupDraggableJoystick(joyBgObj);

            // B) Mobile Action Buttons (Bottom Right: 3 Colored Circular Buttons for INTERACT, CHOP, and THROW!)
            GameObject actionsRoot = new GameObject("MobileActions", typeof(RectTransform));
            actionsRoot.transform.SetParent(mobileControlsContainer.transform, false);
            var actRect = actionsRoot.GetComponent<RectTransform>();
            actRect.anchorMin = new Vector2(1f, 0f);
            actRect.anchorMax = new Vector2(1f, 0f);
            actRect.pivot = new Vector2(1f, 0f);
            actRect.anchoredPosition = new Vector2(-18f, 18f);
            actRect.sizeDelta = new Vector2(210f, 190f);

            primaryActionButton = CreateCircleActionButton("PrimaryActionBtn", actionsRoot.transform, new Vector2(-8f, 88f), new Vector2(84f, 84f), "INTERACT\n[E]", new Color(0.16f, 0.68f, 0.52f, 0.95f), 11);
            secondaryActionButton = CreateCircleActionButton("SecondaryActionBtn", actionsRoot.transform, new Vector2(-104f, 52f), new Vector2(78f, 78f), "CHOP\n[F]", new Color(0.90f, 0.52f, 0.14f, 0.95f), 11);
            throwActionButton = CreateCircleActionButton("ThrowActionBtn", actionsRoot.transform, new Vector2(-18f, 4f), new Vector2(74f, 74f), "THROW\n[Q]", new Color(0.82f, 0.24f, 0.38f, 0.95f), 11);

            // Joysticks & touch buttons only show on mobile/touch platforms (clean screen on desktop/laptop)
            mobileControlsContainer.SetActive(Application.isMobilePlatform);

            // =========================================================================
            // 5. JIWON CREW BAR & COMPACT PROMPT BAR
            // =========================================================================
            jiwonCrewBar = new GameObject("JiwonCrewBar", typeof(RectTransform));
            jiwonCrewBar.transform.SetParent(root, false);
            var cbRect = jiwonCrewBar.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0f);
            cbRect.anchorMax = new Vector2(0.5f, 0f);
            cbRect.pivot = new Vector2(0.5f, 0f);
            cbRect.anchoredPosition = new Vector2(0f, 38f);
            cbRect.sizeDelta = new Vector2(360f, 30f);

            hirePrepButton = CreateButton("HireCrewBtn", jiwonCrewBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(200f, 26f), "Part-Timer [H] (20g)", new Color(0.24f, 0.44f, 0.38f, 0.85f), out hirePrepCooldownText, 11);

            GameObject promptBar = new GameObject("PromptBar", typeof(RectTransform));
            promptBar.transform.SetParent(root, false);
            var pbRectBar = promptBar.GetComponent<RectTransform>();
            pbRectBar.anchorMin = new Vector2(0.5f, 0f);
            pbRectBar.anchorMax = new Vector2(0.5f, 0f);
            pbRectBar.pivot = new Vector2(0.5f, 0f);
            pbRectBar.anchoredPosition = new Vector2(0f, 8f);
            pbRectBar.sizeDelta = new Vector2(460f, 24f);
            var pbImgBar = promptBar.AddComponent<Image>();
            pbImgBar.color = new Color(0.06f, 0.07f, 0.10f, 0.82f);

            promptText = CreateLabel("PromptText", promptBar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, 0f), "[WASD] Move  •  [TAB] Wheel  •  [E] Use  •  [R] Recipes", 11, new Color(0.92f, 0.94f, 0.98f), TextAnchor.MiddleCenter);

            // Feedback Banner positioned below Tutorial Panel (Y = -102f) so they NEVER overlap!
            feedbackBannerText = CreateLabel("FeedbackBanner", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(460f, 28f), "", 15, Color.yellow, TextAnchor.MiddleCenter);
            feedbackBannerText.gameObject.SetActive(false);

            // QA Mode Active Indicator Banner (Y = -44f, directly below top bar)
            qaModeBannerObj = new GameObject("QAModeBanner", typeof(RectTransform));
            qaModeBannerObj.transform.SetParent(root, false);
            var qbRect = qaModeBannerObj.GetComponent<RectTransform>();
            qbRect.anchorMin = new Vector2(0.5f, 1f);
            qbRect.anchorMax = new Vector2(0.5f, 1f);
            qbRect.pivot = new Vector2(0.5f, 1f);
            qbRect.anchoredPosition = new Vector2(0f, -44f);
            qbRect.sizeDelta = new Vector2(460f, 22f);
            var qbImg = qaModeBannerObj.AddComponent<Image>();
            qbImg.color = new Color(0.10f, 0.35f, 0.50f, 0.92f);
            qaModeBannerText = CreateLabel("QABannerText", qaModeBannerObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "🤖 TWO-BOT QA PLAYTEST ACTIVE", 11, new Color(0.9f, 0.98f, 1.0f), TextAnchor.MiddleCenter);
            qaModeBannerObj.SetActive(false);

            // Quick-Select Radial Ingredient Wheel
            if (GetComponentInChildren<QuickSelectWheelUI>() == null)
            {
                GameObject wheelObj = new GameObject("QuickSelectWheelUI", typeof(RectTransform));
                wheelObj.transform.SetParent(root, false);
                wheelObj.AddComponent<QuickSelectWheelUI>();
            }

            // =========================================================================
            // 6. VISUAL RECIPE BOOK MODAL ([R] Key or Button)
            // =========================================================================
            BuildRecipeBookModal(root);

            // =========================================================================
            // 7. LEVEL RESULTS MODAL (Victory / Defeat, Reputation, Gold Reward)
            // =========================================================================
            BuildLevelResultsModal(root);

            // =========================================================================
            // 8. BETWEEN-LEVEL UPGRADE SHOP MODAL
            // =========================================================================
            BuildUpgradeShopModal(root);

            // =========================================================================
            // 9. TWO-BOT QA REPORT MODAL
            // =========================================================================
            BuildQAReportModal(root);
        }

        private void BuildRecipeBookModal(Transform root)
        {
            recipeBookModal = new GameObject("RecipeBookModal", typeof(RectTransform));
            recipeBookModal.transform.SetParent(root, false);
            var rbRect = recipeBookModal.GetComponent<RectTransform>();
            rbRect.anchorMin = new Vector2(0.5f, 0.5f);
            rbRect.anchorMax = new Vector2(0.5f, 0.5f);
            rbRect.pivot = new Vector2(0.5f, 0.5f);
            rbRect.anchoredPosition = Vector2.zero;
            rbRect.sizeDelta = new Vector2(620f, 370f);
            var rbImg = recipeBookModal.AddComponent<Image>();
            rbImg.color = new Color(0.08f, 0.10f, 0.15f, 0.96f);

            recipeBookTitleText = CreateLabel("RBTitle", recipeBookModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(560f, 26f), "📖 5-DAY RECIPE BOOK — PRESS [E] ON COUNTER/PAN TO SELF-ASSEMBLE!", 14, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter);

            var recipeRows = new (string dayKey, string fallbackTitle, string dishIcon, (string icon, string tag)[] steps)[]
            {
                ("day.1", "Day 1: Tteokbokki", "tteokbokki", new[] { ("tteok_portioned", "Rice Cake Portion"), ("gochujang", "+ Gochujang"), ("tteokbokki", "Cook Together!") }),
                ("day.2", "Day 2: + Bột Chiên", "bot_chien", new[] { ("flour", "Cook Flour"), ("egg_raw", "+ Egg & Cook"), ("bot_chien", "Bột Chiên!") }),
                ("day.3", "Day 3: + K-BBQ Rice", "korean_bbq_rice", new[] { ("unprocessed_raw_meat", "Cut 1x➔Cook"), ("cooked_rice", "Cook Rice"), ("korean_bbq_rice", "Assemble [E]") }),
                ("day.4", "Day 4: + Broken Rice", "viet_broken_rice", new[] { ("rice_portioned", "Cook Rice"), ("bulgogi", "+ BBQ Beef"), ("egg_raw", "+ Egg [E]") }),
                ("day.5", "Day 5: Bulgogi Bánh Mì", "bulgogi_banh_mi", new[] { ("baguette", "Cook Flour+Egg"), ("pickled_veg", "Cut Radish"), ("bulgogi", "Cut 2x➔Cook") })
            };
            recipeRowTitleTexts = new Text[recipeRows.Length];
            recipeRowObjects = new GameObject[recipeRows.Length];
            recipeRowGroups = new CanvasGroup[recipeRows.Length];
            recipeRowNewTags = new Text[recipeRows.Length];

            for (int i = 0; i < recipeRows.Length; i++)
            {
                var row = recipeRows[i];
                float yPos = -48f - (i * 54f);

                GameObject rowGo = new GameObject($"RecipeRow_{i}", typeof(RectTransform));
                rowGo.transform.SetParent(recipeBookModal.transform, false);
                var rRect = rowGo.GetComponent<RectTransform>();
                rRect.anchorMin = new Vector2(0.5f, 1f);
                rRect.anchorMax = new Vector2(0.5f, 1f);
                rRect.pivot = new Vector2(0.5f, 1f);
                rRect.anchoredPosition = new Vector2(0f, yPos);
                rRect.sizeDelta = new Vector2(584f, 46f);
                var rBg = rowGo.AddComponent<Image>();
                rBg.color = new Color(0.13f, 0.16f, 0.22f, 0.92f);

                string rowTitle = (LocalizationManager.Instance != null) ? LocalizationManager.Instance.Get(row.dayKey, row.fallbackTitle) : row.fallbackTitle;
                recipeRowTitleTexts[i] = CreateLabel("DishTitle", rowGo.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(135f, 0f), rowTitle, 11, new Color(1.0f, 0.90f, 0.45f), TextAnchor.MiddleLeft);

                // Final Dish Icon
                CreateRecipeStepBadge(rowGo.transform, 152f, row.dishIcon, "Dish");

                for (int s = 0; s < row.steps.Length; s++)
                {
                    float sx = 248f + (s * 112f);
                    CreateRecipeStepBadge(rowGo.transform, sx, row.steps[s].icon, row.steps[s].tag);
                }

                recipeRowObjects[i] = rowGo;
                recipeRowGroups[i] = rowGo.AddComponent<CanvasGroup>();
                recipeRowNewTags[i] = CreateLabel("NewTag", rowGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -2f), new Vector2(50f, 14f), "NEW!", 10, new Color(1.0f, 0.45f, 0.35f), TextAnchor.MiddleRight);
                recipeRowNewTags[i].gameObject.SetActive(false);
            }

            closeRecipeBookButton = CreateButton("CloseRecipeBookBtn", recipeBookModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(180f, 32f), "CLOSE [R]", new Color(0.25f, 0.55f, 0.35f, 1f), out _, 12);
            closeRecipeBookButton.onClick.AddListener(ToggleRecipeBook);
            recipeBookModal.SetActive(false);
        }

        private void CreateRecipeStepBadge(Transform parent, float xOffset, string spriteName, string label)
        {
            GameObject icoGo = new GameObject("StepIcon", typeof(RectTransform));
            icoGo.transform.SetParent(parent, false);
            var iRect = icoGo.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.5f);
            iRect.anchorMax = new Vector2(0f, 0.5f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.anchoredPosition = new Vector2(xOffset, 0f);
            iRect.sizeDelta = new Vector2(28f, 28f);
            var img = icoGo.AddComponent<Image>();
            Sprite spr = PlayerController.LoadItemSprite(spriteName);
            if (spr != null) img.sprite = spr;

            CreateLabel("StepText", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(xOffset + 32f, 0f), new Vector2(76f, 0f), label, 10, Color.white, TextAnchor.MiddleLeft);
        }

        // ---- Recipe Book discovery: future days are blank, today's recipe fades in ----
        private GameObject[] recipeRowObjects;
        private CanvasGroup[] recipeRowGroups;
        private Text[] recipeRowNewTags;
        private int recipeFadeRow = -1;
        private float recipeFadeT = 0f;
        private const float RECIPE_FADE_SECONDS = 1.2f;

        /// <summary>Campaign Day N shows recipes 1..N (tutorial counts as Day 1); Endless Rush shows all.</summary>
        public static bool IsRecipeUnlocked(int recipeDay, int currentStageId, GameMode mode)
        {
            if (mode == GameMode.ENDLESS_RUSH) return true;
            return recipeDay <= Mathf.Max(1, currentStageId);
        }

        /// <summary>The recipe discovered today (gets the fade-in). Only in campaign days 1-5.</summary>
        public static bool IsNewRecipe(int recipeDay, int currentStageId, GameMode mode)
        {
            return mode == GameMode.CAMPAIGN && currentStageId >= 1 && recipeDay == currentStageId;
        }

        private void RefreshRecipeDiscovery(bool animateNew)
        {
            if (recipeRowObjects == null) return;
            var sm = ShiftManager.Instance;
            int stage = sm != null ? sm.currentStageId : 1;
            GameMode mode = sm != null ? sm.currentMode : GameMode.CAMPAIGN;
            recipeFadeRow = -1;

            for (int i = 0; i < recipeRowObjects.Length; i++)
            {
                var rowGo = recipeRowObjects[i];
                if (rowGo == null) continue;
                bool unlocked = IsRecipeUnlocked(i + 1, stage, mode);
                bool isNew = IsNewRecipe(i + 1, stage, mode);

                // Locked rows stay blank: hide every child (title, icons, steps), keep only a faint empty slot.
                foreach (Transform child in rowGo.transform)
                    child.gameObject.SetActive(unlocked);
                if (recipeRowNewTags[i] != null) recipeRowNewTags[i].gameObject.SetActive(unlocked && isNew);

                var bg = rowGo.GetComponent<Image>();
                if (bg != null) bg.color = unlocked ? new Color(0.13f, 0.16f, 0.22f, 0.92f) : new Color(0.10f, 0.12f, 0.17f, 0.45f);

                if (recipeRowGroups[i] != null) recipeRowGroups[i].alpha = 1f;
                if (unlocked && isNew && animateNew)
                {
                    recipeFadeRow = i;
                    recipeFadeT = 0f;
                    if (recipeRowGroups[i] != null) recipeRowGroups[i].alpha = 0f;
                }
            }
        }

        private void AnimateRecipeFade()
        {
            if (recipeFadeRow < 0 || recipeRowGroups == null || recipeFadeRow >= recipeRowGroups.Length) return;
            var cg = recipeRowGroups[recipeFadeRow];
            if (cg == null || recipeBookModal == null || !recipeBookModal.activeSelf) { if (cg != null) cg.alpha = 1f; recipeFadeRow = -1; return; }
            recipeFadeT += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(recipeFadeT / RECIPE_FADE_SECONDS);
            cg.alpha = p * p * (3f - 2f * p); // smooth ease-in-out
            if (p >= 1f) recipeFadeRow = -1;
        }

        private void BuildLevelResultsModal(Transform root)
        {
            endShiftModal = new GameObject("EndShiftModal", typeof(RectTransform));
            endShiftModal.transform.SetParent(root, false);
            var esmRect = endShiftModal.GetComponent<RectTransform>();
            esmRect.anchorMin = new Vector2(0.5f, 0.5f);
            esmRect.anchorMax = new Vector2(0.5f, 0.5f);
            esmRect.pivot = new Vector2(0.5f, 0.5f);
            esmRect.anchoredPosition = Vector2.zero;
            esmRect.sizeDelta = new Vector2(460f, 300f);
            var esmImg = endShiftModal.AddComponent<Image>();
            esmImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            endShiftTitleText = CreateLabel("EndShiftTitle", endShiftModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(420f, 32f), "★ LEVEL COMPLETE! ★", 18, new Color(0.3f, 1.0f, 0.5f), TextAnchor.MiddleCenter);
            endShiftDetailsText = CreateLabel("EndShiftDetails", endShiftModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(410f, 150f), "Remaining Reputation: 100 / 100\nGold Earned: +200g", 14, Color.white, TextAnchor.MiddleCenter);

            openShopFromResultsButton = CreateButton("OpenShopResultsBtn", endShiftModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-138f, 22f), new Vector2(132f, 38f), "🛒 UPGRADE SHOP", new Color(0.80f, 0.55f, 0.14f, 1f), out _, 11);
            retryShiftButton = CreateButton("RetryShiftBtn", endShiftModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(132f, 38f), "▶ NEXT LEVEL", new Color(0.20f, 0.62f, 0.35f, 1f), out retryShiftButtonText, 11);
            resultsToLobbyButton = CreateButton("ResultsToLobbyBtn", endShiftModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(138f, 22f), new Vector2(132f, 38f), "LEVEL SELECT", new Color(0.24f, 0.38f, 0.56f, 1f), out _, 11);

            endShiftModal.SetActive(false);
        }

        private void BuildUpgradeShopModal(Transform root)
        {
            upgradeShopModal = new GameObject("UpgradeShopModal", typeof(RectTransform));
            upgradeShopModal.transform.SetParent(root, false);
            var usRect = upgradeShopModal.GetComponent<RectTransform>();
            usRect.anchorMin = new Vector2(0.5f, 0.5f);
            usRect.anchorMax = new Vector2(0.5f, 0.5f);
            usRect.pivot = new Vector2(0.5f, 0.5f);
            usRect.anchoredPosition = Vector2.zero;
            usRect.sizeDelta = new Vector2(680f, 410f);
            var usImg = upgradeShopModal.AddComponent<Image>();
            usImg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);

            CreateLabel("ShopTitle", upgradeShopModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(450f, 26f), "🛒 BETWEEN-LEVEL UPGRADE SHOP", 16, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter);
            shopGoldText = CreateLabel("ShopGoldText", upgradeShopModal.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -14f), new Vector2(180f, 26f), "GOLD: 150g", 14, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleRight);
            shopFeedbackText = CreateLabel("ShopFeedbackText", upgradeShopModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(620f, 20f), "Spend Gold on permanent character & kitchen upgrades!", 12, new Color(0.75f, 0.90f, 1.0f), TextAnchor.MiddleCenter);

            string[] upgradeKeys = new string[]
            {
                "chef_speed_boots",
                "sharp_santoku_knives",
                "turbo_wok_burners",
                "specialist_tier_2",
                "specialist_tier_3",
                "insulated_broth_pot"
            };

            string[] defaultTitles = new string[]
            {
                "Non-Slip Kitchen Footwear",
                "Sharp Santoku Knife Set",
                "High-Output Jet Burners",
                "Chef Specialist Tier 2 (Day 3+)",
                "Chef Specialist Tier 3 (Day 5+)",
                "Double-Walled Phở Stockpot"
            };

            string[] defaultDescs = new string[]
            {
                "+18% Movement Speed for both chefs.",
                "+15% Prep speed & cadence window.",
                "+25% Faster Cooking Speed on appliances.",
                "Linh: 3rd Arm (+8% speed). Jiwon: Crew (1 helper, 1.5s/chop).",
                "Linh: Glowing Arm (+15% speed). Jiwon: Crew (2 helpers, 1.0s/chop).",
                "+50% longer OPTIMAL heat window."
            };

            int[] defaultCosts = new int[] { 100, 100, 150, 250, 500, 250 };

            for (int i = 0; i < upgradeKeys.Length; i++)
            {
                string uKey = upgradeKeys[i];
                int col = i % 2;
                int row = i / 2;
                float xPos = (col == 0) ? -162f : 162f;
                float yPos = -68f - (row * 94f);

                GameObject card = new GameObject($"ShopCard_{uKey}", typeof(RectTransform));
                card.transform.SetParent(upgradeShopModal.transform, false);
                var cRect = card.GetComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0.5f, 1f);
                cRect.anchorMax = new Vector2(0.5f, 1f);
                cRect.pivot = new Vector2(0.5f, 1f);
                cRect.anchoredPosition = new Vector2(xPos, yPos);
                cRect.sizeDelta = new Vector2(310f, 84f);
                var cBg = card.AddComponent<Image>();
                cBg.color = new Color(0.13f, 0.16f, 0.22f, 0.96f);

                CreateLabel("UpName", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(-95f, 20f), defaultTitles[i], 12, new Color(1.0f, 0.90f, 0.4f), TextAnchor.MiddleLeft);
                shopLevelLabels[uKey] = CreateLabel("UpLvl", card.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -6f), new Vector2(80f, 20f), "Lv 0/1", 11, Color.cyan, TextAnchor.MiddleRight);
                CreateLabel("UpDesc", card.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -2f), new Vector2(-110f, 34f), defaultDescs[i], 10, new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft);

                Text buyLbl;
                Button buyBtn = CreateButton($"BuyBtn_{uKey}", card.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 10f), new Vector2(92f, 28f), $"{defaultCosts[i]}g BUY", new Color(0.20f, 0.58f, 0.34f, 1f), out buyLbl, 11);
                buyBtn.onClick.AddListener(() => OnBuyUpgradeClicked(uKey));
                shopBuyButtons[uKey] = buyBtn;
                shopBuyLabels[uKey] = buyLbl;
            }

            shopNextLevelButton = CreateButton("ShopNextLevelBtn", upgradeShopModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-115f, 16f), new Vector2(210f, 36f), "▶ CONTINUE TO NEXT LEVEL", new Color(0.20f, 0.62f, 0.35f, 1f), out _, 12);
            shopCloseButton = CreateButton("ShopCloseBtn", upgradeShopModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(115f, 16f), new Vector2(210f, 36f), "LEVEL SELECT / CLOSE", new Color(0.26f, 0.38f, 0.54f, 1f), out _, 12);

            upgradeShopModal.SetActive(false);
        }

        private void BuildQAReportModal(Transform root)
        {
            qaReportModal = new GameObject("QAReportModal", typeof(RectTransform));
            qaReportModal.transform.SetParent(root, false);
            var rect = qaReportModal.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(580f, 380f);
            var img = qaReportModal.AddComponent<Image>();
            img.color = new Color(0.06f, 0.08f, 0.13f, 0.75f); // test UI: see-through so game stays visible

            qaReportTitleText = CreateLabel("QATitle", qaReportModal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(540f, 32f), "🤖 TWO-BOT QA AUTOPLAY REPORT", 17, new Color(0.2f, 0.9f, 1.0f), TextAnchor.MiddleCenter);

            qaReportDetailsText = CreateLabel("QADetails", qaReportModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(540f, 240f), "Running simulation...", 12, Color.white, TextAnchor.MiddleLeft);

            qaReportCloseButton = CreateButton("QACloseBtn", qaReportModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 24f), new Vector2(210f, 38f), "RETURN TO LOBBY", new Color(0.24f, 0.40f, 0.58f, 1f), out _, 12);
            qaReportCloseButton.onClick.AddListener(() =>
            {
                qaReportModal.SetActive(false);
                var lobby = FindObjectOfType<LobbyUI>(true);
                if (lobby != null)
                {
                    lobby.gameObject.SetActive(true);
                }
            });

            qaReportRerunButton = CreateButton("QARerunBtn", qaReportModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120f, 24f), new Vector2(210f, 38f), "▶ RERUN 2-BOT TEST", new Color(0.18f, 0.58f, 0.42f, 1f), out _, 12);
            qaReportRerunButton.onClick.AddListener(() =>
            {
                qaReportModal.SetActive(false);
                if (TwoLadles.AI.TwoBotQAManager.Instance != null)
                {
                    var qaMgr = TwoLadles.AI.TwoBotQAManager.Instance;
                    qaMgr.StartAutoplayRun(qaMgr.startDay, qaMgr.targetDay, UnityEngine.Random.Range(100, 9999), 2.0f);
                }
            });

            qaReportModal.SetActive(false);
        }

        public void ShowQAReport(TwoLadles.AI.QARunReport report)
        {
            if (qaReportModal == null) return;
            qaReportModal.SetActive(true);

            if (qaModeBannerObj != null)
                qaModeBannerObj.SetActive(false);

            if (qaReportTitleText != null)
            {
                bool allCleared = report.daysCleared == report.daysAttempted && report.daysCleared > 0;
                qaReportTitleText.text = allCleared ? "★ 2-BOT QA REPORT: ALL DAYS CLEARED! ★" : "⚠ 2-BOT QA REPORT: TEST INCOMPLETE ⚠";
                qaReportTitleText.color = allCleared ? new Color(0.3f, 1.0f, 0.55f) : new Color(1.0f, 0.45f, 0.45f);
            }

            if (qaReportDetailsText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Timestamp:  {report.timestamp}   |   Seed: #{report.seed}");
                sb.AppendLine($"Result:     {report.daysCleared}/{report.daysAttempted} Days Cleared   |   Total Score: {report.totalScore} pts");
                sb.AppendLine($"Orders:     {report.totalOrdersServed} served / {report.totalOrdersFailed} failed ({report.overallSuccessRate * 100f:F1}% accuracy)\n");
                sb.AppendLine("DAY-BY-DAY PERFORMANCE:");
                foreach (var d in report.dayResults)
                {
                    string tag = d.cleared ? "[CLEARED]" : "[FAILED] ";
                    sb.AppendLine($"  • Day {d.day}: {tag} Score: {d.score,4} pts | {d.ordersServed} served | Rep: {d.remainingReputation:F0}/100 | Time: {d.durationSeconds:F1}s");
                }
                sb.AppendLine("\nAudit logs saved to: qa_reports/latest_results.json & latest_summary.txt");
                qaReportDetailsText.text = sb.ToString();
            }
        }

        public void SetQAModeBanner(bool active, int day, int totalDays, float speed)
        {
            if (qaModeBannerObj != null)
            {
                qaModeBannerObj.SetActive(active);
                if (qaModeBannerText != null && active)
                {
                    qaModeBannerText.text = $"🤖 TWO-BOT QA TEST MODE — DAY {day} OF {totalDays} — {speed:F1}x SPEED";
                }
            }
        }

        private void SetupDraggableJoystick(GameObject joyPadObj)
        {
            var trigger = joyPadObj.AddComponent<EventTrigger>();

            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((data) => OnJoystickDrag((PointerEventData)data));
            trigger.triggers.Add(downEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => OnJoystickDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((_) =>
            {
                if (joystickKnobRect != null) joystickKnobRect.anchoredPosition = Vector2.zero;
                SetJoystick(Vector2.zero);
            });
            trigger.triggers.Add(upEntry);
        }

        private void OnJoystickDrag(PointerEventData eventData)
        {
            if (joystickBackgroundRect == null || eventData == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackgroundRect, eventData.position, eventData.pressEventCamera, out Vector2 localPt))
            {
                float maxRadius = 46.0f;
                Vector2 clamped = Vector2.ClampMagnitude(localPt, maxRadius);
                if (joystickKnobRect != null)
                {
                    joystickKnobRect.anchoredPosition = clamped;
                }
                SetJoystick(clamped / maxRadius);
            }
        }

        private void Start()
        {
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.OnFlowUpdated += OnFlowUpdated;
                ShiftManager.Instance.OnRushRhythmChanged += OnRushRhythmChanged;
                ShiftManager.Instance.OnOrderQueueUpdated += RefreshOrders;
                ShiftManager.Instance.OnFeedbackBanner += ShowFeedback;
                ShiftManager.Instance.OnShiftEnded += ShowEndShiftModal;
                ShiftManager.Instance.OnTutorialStepChanged += UpdateTutorialGuidance;
                ShiftManager.Instance.OnReputationChanged += OnReputationChanged;
                ShiftManager.Instance.OnWaveUpdated += OnWaveUpdated;

                if (ShiftManager.Instance.isTutorial)
                {
                    UpdateTutorialGuidance(ShiftManager.Instance.tutorialStep, ShiftManager.Instance.tutorialStepTitle, ShiftManager.Instance.tutorialStepInstruction);
                }
            }

            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.OnStatusToast += ShowFeedback;
                KitchenController.Instance.OnPromptChanged += UpdatePrompt;
                KitchenController.Instance.OnChefSwitched += UpdateChefSwitchDisplay;
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionUpdated += RefreshUpgradeShopDisplay;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += RefreshLocalizedHUD;
            }
            RefreshLocalizedHUD();

            if (switchChefButton != null)
            {
                switchChefButton.onClick.AddListener(() =>
                {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.SwitchLocalChef();
                });
            }

            if (recipeBookButton != null)
            {
                recipeBookButton.onClick.AddListener(ToggleRecipeBook);
            }

            if (touchControlsToggleButton != null)
            {
                touchControlsToggleButton.onClick.AddListener(() =>
                {
                    if (mobileControlsContainer != null)
                        mobileControlsContainer.SetActive(!mobileControlsContainer.activeSelf);
                });
            }

            if (audioMuteButton != null)
            {
                audioMuteButton.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.ToggleMute();
                        if (audioMuteButtonText != null)
                            audioMuteButtonText.text = AudioManager.Instance.isMuted ? "BGM: OFF" : "BGM: ON";
                    }
                });
            }

            if (hirePrepButton != null)
                hirePrepButton.onClick.AddListener(OnHireCrewClicked);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

            if (resultsToLobbyButton != null)
                resultsToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

            if (openShopFromResultsButton != null)
            {
                openShopFromResultsButton.onClick.AddListener(() =>
                {
                    if (endShiftModal != null) endShiftModal.SetActive(false);
                    OpenUpgradeShopModal();
                });
            }

            if (shopNextLevelButton != null)
                shopNextLevelButton.onClick.AddListener(OnNextOrReplayClicked);

            if (shopCloseButton != null)
            {
                shopCloseButton.onClick.AddListener(() =>
                {
                    if (upgradeShopModal != null) upgradeShopModal.SetActive(false);
                    OnReturnToLobbyClicked();
                });
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnReturnedToMenu += OnReturnedToMenuHandler;
            }

            if (primaryActionButton != null)
                primaryActionButton.onClick.AddListener(() =>
                {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.LocalPlayerInteractPrimary();
                });

            if (secondaryActionButton != null)
                secondaryActionButton.onClick.AddListener(() =>
                {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.LocalPlayerInteractSecondary();
                });

            if (throwActionButton != null)
                throwActionButton.onClick.AddListener(() =>
                {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.LocalPlayerThrow();
                });

            SetupDpadButton(dpadUpButton, Vector2.up);
            SetupDpadButton(dpadDownButton, Vector2.down);
            SetupDpadButton(dpadLeftButton, Vector2.left);
            SetupDpadButton(dpadRightButton, Vector2.right);

            if (retryShiftButton != null)
            {
                retryShiftButton.onClick.AddListener(OnNextOrReplayClicked);
            }
        }

        public void ToggleRecipeBook()
        {
            if (recipeBookModal != null)
            {
                bool willBeActive = !recipeBookModal.activeSelf;
                if (willBeActive) RefreshRecipeDiscovery(false);
                recipeBookModal.SetActive(willBeActive);
                if (!willBeActive && ShiftManager.Instance != null && ShiftManager.Instance.isWaitingForRecipeBook)
                {
                    ShiftManager.Instance.isWaitingForRecipeBook = false;
                    ShowFeedback("Recipe Book Closed! Shift Started!", new Color(0.3f, 1.0f, 0.5f));
                }
            }
        }

        public void OpenUpgradeShopModal()
        {
            if (upgradeShopModal != null)
            {
                upgradeShopModal.SetActive(true);
                RefreshUpgradeShopDisplay();
            }
        }

        public void RefreshUpgradeShopDisplay()
        {
            if (ProgressionManager.Instance == null) return;

            if (shopGoldText != null)
                shopGoldText.text = $"GOLD: {ProgressionManager.Instance.gold}g";

            foreach (var kvp in ProgressionManager.Instance.catalogUpgrades)
            {
                string key = kvp.Key;
                var up = kvp.Value;

                bool isLocked = false;
                string lockReason = "";
                if (key == "specialist_tier_2" && ProgressionManager.Instance.highestUnlockedStage < 3)
                {
                    isLocked = true;
                    lockReason = "DAY 3+";
                }
                else if (key == "specialist_tier_3")
                {
                    if (ProgressionManager.Instance.linhTier < 2 && !ProgressionManager.Instance.IsUpgradeOwned("specialist_tier_2"))
                    {
                        isLocked = true;
                        lockReason = "NEED TIER 2";
                    }
                    else if (ProgressionManager.Instance.highestUnlockedStage < 5)
                    {
                        isLocked = true;
                        lockReason = "DAY 5+";
                    }
                }

                if (shopLevelLabels.TryGetValue(key, out var lvlTxt) && lvlTxt != null)
                {
                    if (up.purchased)
                    {
                        lvlTxt.text = "OWNED ✓";
                        lvlTxt.color = new Color(0.35f, 1.0f, 0.55f);
                    }
                    else if (isLocked)
                    {
                        lvlTxt.text = lockReason;
                        lvlTxt.color = new Color(1.0f, 0.45f, 0.35f);
                    }
                    else
                    {
                        lvlTxt.text = $"Lv {up.level}/{up.maxLevel}";
                        lvlTxt.color = Color.cyan;
                    }
                }

                if (shopBuyButtons.TryGetValue(key, out var btn) && btn != null && shopBuyLabels.TryGetValue(key, out var bTxt) && bTxt != null)
                {
                    bool canAfford = ProgressionManager.Instance.gold >= up.cost;
                    btn.interactable = !up.purchased && !isLocked && canAfford;
                    bTxt.text = up.purchased ? "OWNED" : (isLocked ? lockReason : $"{up.cost}g BUY");
                    btn.GetComponent<Image>().color = up.purchased
                        ? new Color(0.22f, 0.28f, 0.25f, 0.8f)
                        : (isLocked ? new Color(0.35f, 0.25f, 0.25f, 0.85f)
                        : (canAfford ? new Color(0.20f, 0.58f, 0.34f, 1f) : new Color(0.45f, 0.22f, 0.22f, 0.85f)));
                }
            }
        }

        private void OnBuyUpgradeClicked(string upgradeId)
        {
            if (ProgressionManager.Instance == null) return;
            var res = ProgressionManager.Instance.PurchaseUpgrade(upgradeId);
            if (shopFeedbackText != null)
            {
                if (res.ok && ProgressionManager.Instance.catalogUpgrades.TryGetValue(upgradeId, out var up))
                {
                    shopFeedbackText.text = $"★ Purchased {up.displayName}! Stat upgraded immediately. ★";
                    shopFeedbackText.color = new Color(0.35f, 1.0f, 0.55f);
                }
                else
                {
                    shopFeedbackText.text = res.reason;
                    shopFeedbackText.color = new Color(1.0f, 0.45f, 0.45f);
                }
            }
            RefreshUpgradeShopDisplay();
        }

        private void SetupDpadButton(Button btn, Vector2 dir)
        {
            if (btn == null) return;
            var trigger = btn.gameObject.AddComponent<EventTrigger>();

            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((_) => SetJoystick(dir));
            trigger.triggers.Add(entryDown);

            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((_) => SetJoystick(Vector2.zero));
            trigger.triggers.Add(entryUp);
        }

        private void SetJoystick(Vector2 dir)
        {
            int localId = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (KitchenController.Instance != null && KitchenController.Instance.playersByPeer.TryGetValue(localId, out var chef))
            {
                chef.virtualJoystickInput = dir;
            }
        }

        private void UpdatePrompt(string p)
        {
            if (promptText != null)
                promptText.text = p;
        }

        private void OnReputationChanged(float newRep, float prevRep)
        {
            displayedReputationTarget = Mathf.Clamp(newRep, 0.0f, 100.0f);
            if (newRep < prevRep)
            {
                // Keep trailing white bar at the higher value and hold briefly before animating down
                displayedReputationTrailing = Mathf.Max(displayedReputationTrailing, prevRep);
                reputationTrailHoldTimer = 0.30f;
            }
            else
            {
                displayedReputationTrailing = displayedReputationTarget;
            }
            UpdateReputationBarImmediate();
        }

        private void UpdateReputationBarImmediate()
        {
            float targetNorm = Mathf.Clamp01(displayedReputationTarget / ShiftManager.MAX_REPUTATION);
            if (reputationMainFill != null)
            {
                reputationMainFill.sizeDelta = new Vector2(REPUTATION_BAR_WIDTH * targetNorm, 0.0f);
            }
            if (reputationMainImage != null)
            {
                if (displayedReputationTarget > 50.0f)
                    reputationMainImage.color = new Color(0.22f, 0.88f, 0.42f, 1.0f);
                else if (displayedReputationTarget > 25.0f)
                    reputationMainImage.color = new Color(0.95f, 0.72f, 0.18f, 1.0f);
                else
                    reputationMainImage.color = new Color(0.95f, 0.22f, 0.20f, 1.0f);
            }
            if (reputationText != null)
            {
                reputationText.text = $"REP {Mathf.CeilToInt(displayedReputationTarget)}";
                reputationText.color = displayedReputationTarget > 25.0f ? new Color(0.45f, 1.0f, 0.6f) : new Color(1.0f, 0.35f, 0.35f);
            }
        }

        private void AnimateReputationTrailingBar(float dt)
        {
            if (displayedReputationTrailing > displayedReputationTarget)
            {
                if (reputationTrailHoldTimer > 0.0f)
                {
                    reputationTrailHoldTimer -= dt;
                }
                else
                {
                    // Smooth catch-up animation
                    float speed = Mathf.Max(22.0f, (displayedReputationTrailing - displayedReputationTarget) * 3.2f);
                    displayedReputationTrailing = Mathf.MoveTowards(displayedReputationTrailing, displayedReputationTarget, speed * dt);
                }
            }
            else
            {
                displayedReputationTrailing = displayedReputationTarget;
            }

            if (reputationTrailingFill != null)
            {
                float trailNorm = Mathf.Clamp01(displayedReputationTrailing / ShiftManager.MAX_REPUTATION);
                reputationTrailingFill.sizeDelta = new Vector2(REPUTATION_BAR_WIDTH * trailNorm, 0.0f);
            }
        }

        private void OnWaveUpdated(int waveNum, int totalWaves, int completedInWave, int requiredInWave)
        {
            if (shiftTimerText != null && ShiftManager.Instance != null)
            {
                if (ShiftManager.Instance.isTutorial)
                {
                    shiftTimerText.text = "ACADEMY";
                }
                else if (ShiftManager.Instance.currentMode == GameMode.CAMPAIGN && ShiftManager.Instance.currentStageId == 5)
                {
                    int m = Mathf.FloorToInt(ShiftManager.Instance.shiftTimeRemaining / 60.0f);
                    int s = Mathf.FloorToInt(ShiftManager.Instance.shiftTimeRemaining % 60.0f);
                    shiftTimerText.text = $"DAY 5 • {m:D2}:{s:D2}";
                }
                else if (ShiftManager.Instance.currentMode == GameMode.CAMPAIGN)
                {
                    shiftTimerText.text = $"DAY {ShiftManager.Instance.currentStageId} • W{waveNum}/{totalWaves}";
                }
            }

            if (waveProgressFill != null)
            {
                if (ShiftManager.Instance != null && ShiftManager.Instance.currentMode == GameMode.CAMPAIGN && ShiftManager.Instance.currentStageId == 5)
                {
                    float pct = Mathf.Clamp01(ShiftManager.Instance.shiftTimeRemaining / 180.0f);
                    waveProgressFill.sizeDelta = new Vector2(WAVE_BAR_WIDTH * pct, 0.0f);
                }
                else
                {
                    float pct = requiredInWave > 0 ? Mathf.Clamp01((float)completedInWave / requiredInWave) : 0.0f;
                    waveProgressFill.sizeDelta = new Vector2(WAVE_BAR_WIDTH * pct, 0.0f);
                }
            }
        }

        private bool hasAutoOpenedRecipeBook = false;
        private bool storyShownForShift = false;

        private void Update()
        {
            // Start-of-day story phone plays first; the recipe book waits until it closes.
            if (ShiftManager.Instance != null && ShiftManager.Instance.shiftActive && ShiftManager.Instance.isWaitingForRecipeBook
                && !storyShownForShift
                && PhoneStoryUI.ShouldShowStory(ShiftManager.Instance.currentStageId, ShiftManager.Instance.currentMode, ShiftManager.Instance.isAutoplayQAMode))
            {
                storyShownForShift = true;
                PhoneStoryUI.Show(ShiftManager.Instance.currentStageId, null);
            }

            if (Input.GetKeyDown(KeyCode.R) && !PhoneStoryUI.IsShowing)
            {
                ToggleRecipeBook();
            }

            if (Input.GetKeyDown(KeyCode.F3) && mobileControlsContainer != null)
            {
                mobileControlsContainer.SetActive(!mobileControlsContainer.activeSelf);
            }

            if (ShiftManager.Instance != null && ShiftManager.Instance.shiftActive && ShiftManager.Instance.isWaitingForRecipeBook)
            {
                if (recipeBookModal != null && !recipeBookModal.activeSelf && !hasAutoOpenedRecipeBook && !PhoneStoryUI.IsShowing)
                {
                    hasAutoOpenedRecipeBook = true;
                    RefreshRecipeDiscovery(true);
                    recipeBookModal.SetActive(true);
                }
            }
            else if (ShiftManager.Instance != null && !ShiftManager.Instance.isWaitingForRecipeBook)
            {
                hasAutoOpenedRecipeBook = false;
                storyShownForShift = false;
            }

            AnimateRecipeFade();
            AnimateReputationTrailingBar(Time.deltaTime);

            if (ShiftManager.Instance == null || !ShiftManager.Instance.shiftActive) return;

            if (shiftTimerText != null)
            {
                if (ShiftManager.Instance.isTutorial)
                {
                    shiftTimerText.text = "ACADEMY";
                }
                else if (ShiftManager.Instance.currentMode == GameMode.CAMPAIGN && ShiftManager.Instance.currentStageId == 5)
                {
                    int m = Mathf.FloorToInt(ShiftManager.Instance.shiftTimeRemaining / 60.0f);
                    int s = Mathf.FloorToInt(ShiftManager.Instance.shiftTimeRemaining % 60.0f);
                    shiftTimerText.text = $"DAY 5 • {m:D2}:{s:D2}";
                    if (waveProgressFill != null)
                    {
                        float pct = Mathf.Clamp01(ShiftManager.Instance.shiftTimeRemaining / 180.0f);
                        waveProgressFill.sizeDelta = new Vector2(WAVE_BAR_WIDTH * pct, 0.0f);
                    }
                }
                else if (ShiftManager.Instance.currentMode == GameMode.CAMPAIGN)
                {
                    shiftTimerText.text = $"DAY {ShiftManager.Instance.currentStageId} • W{ShiftManager.Instance.currentWaveNumber}/{ShiftManager.Instance.totalWaves}";
                }
                else
                {
                    int m = Mathf.FloorToInt(ShiftManager.Instance.shiftElapsedSeconds / 60.0f);
                    int s = Mathf.FloorToInt(ShiftManager.Instance.shiftElapsedSeconds % 60.0f);
                    shiftTimerText.text = $"∞ {m:D2}:{s:D2}";
                }
            }

            if (scoreText != null)
                scoreText.text = $"{ShiftManager.Instance.totalScore} pts";

            if (cleanPlatesText != null)
                cleanPlatesText.text = $"Clean: {ShiftManager.Instance.cleanPlates}";

            if (dirtyPlatesText != null)
                dirtyPlatesText.text = $"Dirty: {ShiftManager.Instance.dirtyPlatesAtReturn}";

            // Jiwon crew bar only shown when the live Jiwon chef (own save or LAN partner's) is Tier >= 2
            int jiwonLiveTier = ProgressionManager.Instance != null ? ProgressionManager.Instance.jiwonTier : 1;
            if (KitchenController.Instance != null)
            {
                foreach (var pc in KitchenController.Instance.playersByPeer.Values)
                    if (pc != null && pc.characterId == "jiwon") { jiwonLiveTier = pc.characterTier; break; }
            }
            if (jiwonCrewBar != null)
            {
                bool showCrew = jiwonLiveTier >= 2;
                if (jiwonCrewBar.activeSelf != showCrew)
                    jiwonCrewBar.SetActive(showCrew);
            }

            if (KitchenController.Instance != null)
            {
                if (hirePrepCooldownText != null)
                {
                    float cd = KitchenController.Instance.workerCooldowns.TryGetValue("prep_assistant", out float val) ? val : 0.0f;
                    int cost = (GameData.Instance != null && GameData.Instance.workerCatalog.TryGetValue("prep_assistant", out var wspec)) ? wspec.hireCost : 20;
                    hirePrepCooldownText.text = cd > 0.0f ? $"Crew ({cd:F0}s)" : $"Part-Timer [H] (-{cost} pts)";
                }
            }

            RefreshOrders(ShiftManager.Instance.activeOrders);

            if (tutorialPanelObj != null)
            {
                bool shouldShow = ShiftManager.Instance != null && ShiftManager.Instance.isTutorial;
                if (tutorialPanelObj.activeSelf != shouldShow)
                {
                    tutorialPanelObj.SetActive(shouldShow);
                }
            }

            if (switchChefButtonText != null)
            {
                int localId = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
                switchChefButtonText.text = (localId == 1) ? "LINH [C]" : "JIWON [C]";
            }

            if (feedbackTimer > 0.0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0.0f && feedbackBannerText != null)
                {
                    feedbackBannerText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateTutorialGuidance(int step, string title, string instruction)
        {
            if (tutorialPanelObj != null)
            {
                tutorialPanelObj.SetActive(true);
            }
            if (tutorialTitleText != null)
            {
                tutorialTitleText.text = title;
            }
            if (tutorialInstructionText != null)
            {
                tutorialInstructionText.text = instruction;
            }
        }

        private void UpdateChefSwitchDisplay(int peerId)
        {
            if (switchChefButtonText != null)
            {
                switchChefButtonText.text = (peerId == 1) ? "LINH [C]" : "JIWON [C]";
            }
        }

        private void OnFlowUpdated(float flow, bool rushActive, float rushRemaining)
        {
            if (flowSlider != null)
                flowSlider.value = rushActive ? (rushRemaining / ShiftManager.RUSH_RHYTHM_DURATION) : (flow / ShiftManager.MAX_FLOW);

            if (flowText != null)
            {
                flowText.text = rushActive ? "RUSH!" : "FLOW";
            }
        }

        private void OnRushRhythmChanged(bool isActive)
        {
            if (rushBannerObj != null)
                rushBannerObj.SetActive(isActive);
        }

        private void RefreshOrders(List<OrderTicket> orders)
        {
            if (orders == null) return;

            for (int i = 0; i < 4; i++)
            {
                if (orderCardObjs[i] == null) continue;
                if (i < orders.Count)
                {
                    var o = orders[i];
                    orderCardObjs[i].SetActive(true);

                    string rName = o.GetFormattedDisplayLabel();
                    if (orderCardLabels[i] != null)
                    {
                        orderCardLabels[i].text = rName;
                    }
                    if (orderCardIcons[i] != null)
                    {
                        string iconRecipeId = o.GetNextUnfulfilledRecipeId();
                        Sprite spr = PlayerController.LoadItemSprite(iconRecipeId);
                        if (spr != null) orderCardIcons[i].sprite = spr;
                    }
                    if (orderCardBarFills[i] != null)
                    {
                        float pct = o.initialPatience > 0.01f ? Mathf.Clamp01(o.timeRemaining / o.initialPatience) : 1.0f;
                        orderCardBarFills[i].sizeDelta = new Vector2(164.0f * pct, 0.0f);
                        if (orderCardBarImages[i] != null)
                        {
                            orderCardBarImages[i].color = pct > 0.5f
                                ? new Color(0.25f, 0.92f, 0.45f, 1.0f)
                                : (pct > 0.25f ? new Color(0.95f, 0.78f, 0.20f, 1.0f) : new Color(0.95f, 0.25f, 0.20f, 1.0f));
                        }
                    }
                }
                else
                {
                    orderCardObjs[i].SetActive(false);
                }
            }
        }

        private void ShowFeedback(string message, Color col)
        {
            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = message;
                feedbackBannerText.color = col;
                feedbackBannerText.gameObject.SetActive(true);
                feedbackTimer = 2.0f;
            }
        }

        private void ShowEndShiftModal(Dictionary<string, object> summary)
        {
            if (endShiftModal != null)
            {
                endShiftModal.SetActive(true);
                if (summary != null)
                {
                    bool victory = summary.ContainsKey("victory") && Convert.ToBoolean(summary["victory"]);
                    float rep = summary.ContainsKey("reputation") ? Convert.ToSingle(summary["reputation"]) : 0.0f;
                    int gold = summary.ContainsKey("gold_earned") ? Convert.ToInt32(summary["gold_earned"]) : 0;
                    int wavesDone = summary.ContainsKey("waves_completed") ? Convert.ToInt32(summary["waves_completed"]) : 0;
                    int totalWaves = summary.ContainsKey("total_waves") ? Convert.ToInt32(summary["total_waves"]) : 3;
                    int served = summary.ContainsKey("ordersServed") ? Convert.ToInt32(summary["ordersServed"]) : 0;

                    var lm = LocalizationManager.Instance;
                    if (endShiftTitleText != null)
                    {
                        endShiftTitleText.text = victory 
                            ? (lm != null ? lm.Format("results.victory", ShiftManager.Instance != null ? ShiftManager.Instance.currentStageId : 1) : "★ VICTORY! LEVEL COMPLETE ★")
                            : (lm != null ? lm.Get("results.game_over") : "✖ DEFEAT — REPUTATION LOST ✖");
                        endShiftTitleText.color = victory ? new Color(0.30f, 1.0f, 0.50f) : new Color(1.0f, 0.35f, 0.35f);
                    }

                    if (endShiftDetailsText != null)
                    {
                        int totalGold = ProgressionManager.Instance != null ? ProgressionManager.Instance.gold : gold;
                        int stageId = summary.ContainsKey("stage_id") ? Convert.ToInt32(summary["stage_id"]) : 1;
                        int finalScore = summary.ContainsKey("total_score") ? Convert.ToInt32(summary["total_score"]) : 0;
                        int highScore = summary.ContainsKey("high_score") ? Convert.ToInt32(summary["high_score"]) : finalScore;

                        if (lm != null)
                        {
                            string repStr = lm.Format("results.rep_remaining", Mathf.CeilToInt(rep));
                            string waveStr = lm.Format("results.waves_done", wavesDone, totalWaves, served);
                            string goldStr = lm.Format("results.gold_earned", gold);
                            string balStr = lm.Format("results.gold_balance", totalGold);
                            endShiftDetailsText.text = $"{repStr}\n{waveStr}\n{goldStr}\n{balStr}";
                        }
                        else if (stageId == 5)
                        {
                            endShiftDetailsText.text =
                                $"3-MINUTE BULGOGI BÁNH MÌ HIGH SCORE: {finalScore} pts\n" +
                                $"All-Time Best High Score: {highScore} pts  (Served: {served})\n" +
                                $"Remaining Reputation: {Mathf.CeilToInt(rep)} / 100\n" +
                                $"Gold Earned: +{gold}g  (Total Balance: {totalGold}g)";
                        }
                        else
                        {
                            endShiftDetailsText.text =
                                $"Final Reputation: {Mathf.CeilToInt(rep)} / 100\n" +
                                $"Waves Completed: {wavesDone} / {totalWaves}  (Orders Served: {served})\n" +
                                $"Gold Earned: +{gold}g\n" +
                                $"Total Gold Balance: {totalGold}g";
                        }
                    }

                    if (retryShiftButtonText != null)
                    {
                        retryShiftButtonText.text = victory 
                            ? (lm != null ? lm.Get("results.next_level") : "▶ NEXT LEVEL")
                            : (lm != null ? lm.Get("results.retry_level") : "↻ RETRY LEVEL");
                    }
                }
            }
        }

        private void OnNextOrReplayClicked()
        {
            if (endShiftModal != null) endShiftModal.SetActive(false);
            if (upgradeShopModal != null) upgradeShopModal.SetActive(false);

            if (ShiftManager.Instance != null)
            {
                int nextStage = ShiftManager.Instance.currentStageId;
                if (ShiftManager.Instance.levelVictory && nextStage >= 1 && nextStage < 5)
                {
                    nextStage++;
                }
                else if (nextStage == 0)
                {
                    nextStage = 1;
                }
                ShiftManager.Instance.StartShift(nextStage, ShiftManager.Instance.currentMode);
            }
        }

        private void OnHireCrewClicked()
        {
            if (KitchenController.Instance != null)
            {
                int peerId = 2;
                var res = KitchenController.Instance.RequestHireAvailableCrew(peerId);
                if (!res.ok)
                {
                    ShowFeedback(res.reason, Color.red);
                }
            }
        }

        private void OnReturnToLobbyClicked()
        {
            if (endShiftModal != null) endShiftModal.SetActive(false);
            if (upgradeShopModal != null) upgradeShopModal.SetActive(false);
            if (recipeBookModal != null) recipeBookModal.SetActive(false);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.ReturnToMenu("Select a level or upgrade your chefs.");
            }
            if (ShiftManager.Instance != null && ShiftManager.Instance.shiftActive)
            {
                ShiftManager.Instance.shiftActive = false;
            }
            var lobby = FindObjectOfType<LobbyUI>(true);
            if (lobby != null)
            {
                lobby.gameObject.SetActive(true);
                if (lobby.modalPanel != null) lobby.modalPanel.SetActive(true);
                lobby.ShowMainMenu();
                lobby.RefreshProgressionDisplay();
            }
        }

        private void OnReturnedToMenuHandler(string reason)
        {
            if (ShiftManager.Instance != null && ShiftManager.Instance.shiftActive)
            {
                ShiftManager.Instance.shiftActive = false;
            }
        }

        public void RefreshLocalizedHUD()
        {
            if (LocalizationManager.Instance == null) return;
            var lm = LocalizationManager.Instance;

            if (returnToLobbyButton != null)
            {
                var txt = returnToLobbyButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = lm.Get("hud.return_lobby");
            }
            if (recipeBookButton != null)
            {
                var txt = recipeBookButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = lm.Get("hud.recipes");
            }
            if (audioMuteButtonText != null)
            {
                bool isMuted = AudioManager.Instance != null && AudioManager.Instance.isMuted;
                audioMuteButtonText.text = isMuted ? lm.Get("hud.bgm_off") : lm.Get("hud.bgm_on");
            }
            if (touchControlsToggleButton != null)
            {
                var txt = touchControlsToggleButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = lm.Get("hud.joypad");
            }
            if (primaryActionButton != null)
            {
                var txt = primaryActionButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = lm.Get("hud.prompt_use");
            }
            if (secondaryActionButton != null)
            {
                var txt = secondaryActionButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = lm.Get("hud.prompt_chop");
            }
            if (throwActionButton != null)
            {
                var txt = throwActionButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = lm.Get("hud.prompt_throw");
            }
            if (recipeRowTitleTexts != null)
            {
                string[] dayKeys = new[] { "day.1", "day.2", "day.3", "day.4", "day.5" };
                for (int i = 0; i < recipeRowTitleTexts.Length && i < dayKeys.Length; i++)
                {
                    if (recipeRowTitleTexts[i] != null)
                        recipeRowTitleTexts[i].text = lm.Get(dayKeys[i]);
                }
            }

            if (ShiftManager.Instance != null && ShiftManager.Instance.activeOrders != null)
            {
                RefreshOrders(ShiftManager.Instance.activeOrders);
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnReturnedToMenu -= OnReturnedToMenuHandler;
            }
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.OnTutorialStepChanged -= UpdateTutorialGuidance;
                ShiftManager.Instance.OnReputationChanged -= OnReputationChanged;
                ShiftManager.Instance.OnWaveUpdated -= OnWaveUpdated;
            }
            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.OnChefSwitched -= UpdateChefSwitchDisplay;
            }
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionUpdated -= RefreshUpgradeShopDisplay;
            }
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshLocalizedHUD;
            }
        }
    }
}
