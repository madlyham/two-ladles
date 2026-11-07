using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Kitchen;
using TwoLadles.Networking;

namespace TwoLadles.UI
{
    public class KitchenHUD : MonoBehaviour
    {
        [Header("Top Header")]
        public Text shiftTimerText;
        public Text scoreText;
        public Slider flowSlider;
        public Text flowText;
        public GameObject rushBannerObj;
        public Button returnToLobbyButton;

        [Header("Order Queue")]
        public Text orderListText;

        [Header("Plate Stack")]
        public Text cleanPlatesText;
        public Text dirtyPlatesText;

        [Header("Jiwon Crew Bar")]
        public GameObject jiwonCrewBar;
        public Button hirePrepButton;
        public Button hireDishwasherButton;
        public Text hirePrepCooldownText;
        public Text hireDishwasherCooldownText;

        [Header("Mobile Action & D-Pad Buttons")]
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

        [Header("End of Shift Modal")]
        public GameObject endShiftModal;
        public Text endShiftDetailsText;
        public Button retryShiftButton;

        private void Awake()
        {
            if (shiftTimerText == null)
            {
                AutoBuildUI();
            }
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                var osFonts = Font.GetOSInstalledFontNames();
                if (osFonts != null && osFonts.Length > 0)
                {
                    font = Font.CreateDynamicFontFromOSFont(osFonts[0], 14);
                }
            }
            return font;
        }

        private Text CreateLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, string text, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
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
            return txt;
        }

        private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, string labelText, Color bgColor, out Text outLabel)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
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

            outLabel = CreateLabel(name + "_Label", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, labelText, 13, Color.white, TextAnchor.MiddleCenter);
            return btn;
        }

        private Slider CreateFlowSlider(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject sliderObj = new GameObject("FlowSlider");
            sliderObj.transform.SetParent(parent, false);
            var rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRect = fill.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var fImg = fill.AddComponent<Image>();
            fImg.color = new Color(0.95f, 0.68f, 0.15f, 1.0f);

            slider.fillRect = fRect;
            slider.targetGraphic = bgImg;
            return slider;
        }

        private void AutoBuildUI()
        {
            var root = transform;

            // 1. Top Bar Panel
            GameObject topBar = new GameObject("TopBarPanel");
            topBar.transform.SetParent(root, false);
            var tbRect = topBar.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 1f);
            tbRect.anchorMax = new Vector2(1f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = Vector2.zero;
            tbRect.sizeDelta = new Vector2(0f, 40f);
            var tbImg = topBar.AddComponent<Image>();
            tbImg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

            scoreText = CreateLabel("ScoreText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(110f, 30f), "SCORE: 0", 15, Color.yellow, TextAnchor.MiddleLeft);
            shiftTimerText = CreateLabel("TimerText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(144f, 0f), new Vector2(120f, 30f), "TIME: 90s", 15, Color.white, TextAnchor.MiddleLeft);
            flowText = CreateLabel("FlowText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(274f, 0f), new Vector2(160f, 30f), "KITCHEN FLOW: 0 / 100", 14, new Color(0.35f, 0.85f, 1.0f), TextAnchor.MiddleLeft);
            flowSlider = CreateFlowSlider(topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(444f, 0f), new Vector2(110f, 16f));
            cleanPlatesText = CreateLabel("CleanPlatesText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(574f, 0f), new Vector2(140f, 30f), "Clean: 4", 14, new Color(0.7f, 1.0f, 0.7f), TextAnchor.MiddleLeft);
            dirtyPlatesText = CreateLabel("DirtyPlatesText", topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(724f, 0f), new Vector2(130f, 30f), "Dirty Return: 0", 14, new Color(1.0f, 0.6f, 0.6f), TextAnchor.MiddleLeft);

            returnToLobbyButton = CreateButton("ReturnToLobbyBtn", topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(130f, 28f), "Return to Lobby", new Color(0.5f, 0.2f, 0.2f, 0.9f), out _);

            // 2. Active Orders Panel (Upper Left)
            GameObject ordersPanel = new GameObject("OrdersPanel");
            ordersPanel.transform.SetParent(root, false);
            var opRect = ordersPanel.AddComponent<RectTransform>();
            opRect.anchorMin = new Vector2(0f, 1f);
            opRect.anchorMax = new Vector2(0f, 1f);
            opRect.pivot = new Vector2(0f, 1f);
            opRect.anchoredPosition = new Vector2(24f, -48f);
            opRect.sizeDelta = new Vector2(480f, 130f);
            var opImg = ordersPanel.AddComponent<Image>();
            opImg.color = new Color(0.06f, 0.06f, 0.09f, 0.75f);

            orderListText = CreateLabel("OrderListText", ordersPanel.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -12f), "ACTIVE CUSTOMER ORDERS:\nWaiting for tickets...", 13, Color.white, TextAnchor.UpperLeft);

            // 3. Mobile D-Pad (Bottom Left)
            GameObject dpadRoot = new GameObject("MobileDPad");
            dpadRoot.transform.SetParent(root, false);
            var dpRect = dpadRoot.AddComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0f, 0f);
            dpRect.anchorMax = new Vector2(0f, 0f);
            dpRect.pivot = new Vector2(0f, 0f);
            dpRect.anchoredPosition = new Vector2(24f, 24f);
            dpRect.sizeDelta = new Vector2(140f, 140f);

            Color dpadCol = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            dpadUpButton = CreateButton("DpadUp", dpadRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(40f, 40f), "▲", dpadCol, out _);
            dpadDownButton = CreateButton("DpadDown", dpadRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(40f, 40f), "▼", dpadCol, out _);
            dpadLeftButton = CreateButton("DpadLeft", dpadRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-44f, 0f), new Vector2(40f, 40f), "◀", dpadCol, out _);
            dpadRightButton = CreateButton("DpadRight", dpadRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 0f), new Vector2(40f, 40f), "▶", dpadCol, out _);

            // 4. Action Buttons (Bottom Right)
            GameObject actionsRoot = new GameObject("MobileActions");
            actionsRoot.transform.SetParent(root, false);
            var actRect = actionsRoot.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(1f, 0f);
            actRect.anchorMax = new Vector2(1f, 0f);
            actRect.pivot = new Vector2(1f, 0f);
            actRect.anchoredPosition = new Vector2(-24f, 20f);
            actRect.sizeDelta = new Vector2(220f, 160f);

            primaryActionButton = CreateButton("PrimaryActionBtn", actionsRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 104f), new Vector2(210f, 48f), "INTERACT / PASS\n(E / Space)", new Color(0.18f, 0.52f, 0.65f, 0.9f), out _);
            secondaryActionButton = CreateButton("SecondaryActionBtn", actionsRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 52f), new Vector2(210f, 46f), "STRIKE / SCRUB / BOWL\n(F Key)", new Color(0.72f, 0.42f, 0.15f, 0.9f), out _);
            throwActionButton = CreateButton("ThrowActionBtn", actionsRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(210f, 40f), "THROW ITEM\n(Q / Right-Click)", new Color(0.58f, 0.22f, 0.55f, 0.9f), out _);

            // Desktop players use keyboard & mouse: hide touch overlay buttons to keep playfield uncluttered
            #if !UNITY_ANDROID && !UNITY_IOS
            dpadRoot.SetActive(false);
            actionsRoot.SetActive(false);
            #endif

            // 5. Jiwon Crew Bar (Bottom Center)
            jiwonCrewBar = new GameObject("JiwonCrewBar");
            jiwonCrewBar.transform.SetParent(root, false);
            var cbRect = jiwonCrewBar.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0f);
            cbRect.anchorMax = new Vector2(0.5f, 0f);
            cbRect.pivot = new Vector2(0.5f, 0f);
            cbRect.anchoredPosition = new Vector2(0f, 42f);
            cbRect.sizeDelta = new Vector2(440f, 36f);

            hirePrepButton = CreateButton("HirePrepBtn", jiwonCrewBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-105f, 0f), new Vector2(195f, 32f), "Hire Prep Assistant (20c)", new Color(0.24f, 0.44f, 0.38f, 0.85f), out hirePrepCooldownText);
            hireDishwasherButton = CreateButton("HireDishwasherBtn", jiwonCrewBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(105f, 0f), new Vector2(195f, 32f), "Hire Dishwasher (20c)", new Color(0.22f, 0.38f, 0.52f, 0.85f), out hireDishwasherCooldownText);

            // 6. Interaction Prompt (Bottom Center with dark backdrop)
            GameObject promptBar = new GameObject("PromptBar");
            promptBar.transform.SetParent(root, false);
            var pbRect = promptBar.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0f);
            pbRect.anchorMax = new Vector2(0.5f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.anchoredPosition = new Vector2(0f, 10f);
            pbRect.sizeDelta = new Vector2(640f, 28f);
            var pbImg = promptBar.AddComponent<Image>();
            pbImg.color = new Color(0.06f, 0.07f, 0.10f, 0.85f);

            promptText = CreateLabel("PromptText", promptBar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f), "Near: None", 13, new Color(0.95f, 0.95f, 0.95f), TextAnchor.MiddleCenter);

            // 7. Feedback Banner (Top Center)
            feedbackBannerText = CreateLabel("FeedbackBanner", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(500f, 32f), "", 16, Color.yellow, TextAnchor.MiddleCenter);
            feedbackBannerText.gameObject.SetActive(false);

            // 8. End Shift Modal
            endShiftModal = new GameObject("EndShiftModal");
            endShiftModal.transform.SetParent(root, false);
            var esmRect = endShiftModal.AddComponent<RectTransform>();
            esmRect.anchorMin = new Vector2(0.5f, 0.5f);
            esmRect.anchorMax = new Vector2(0.5f, 0.5f);
            esmRect.pivot = new Vector2(0.5f, 0.5f);
            esmRect.anchoredPosition = Vector2.zero;
            esmRect.sizeDelta = new Vector2(380f, 280f);
            var esmImg = endShiftModal.AddComponent<Image>();
            esmImg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            endShiftDetailsText = CreateLabel("EndShiftDetails", endShiftModal.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(-24f, -60f), "SHIFT SUMMARY", 14, Color.white, TextAnchor.MiddleCenter);
            retryShiftButton = CreateButton("RetryShiftBtn", endShiftModal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(160f, 36f), "START NEXT SHIFT", new Color(0.2f, 0.6f, 0.35f, 0.95f), out _);
            endShiftModal.SetActive(false);
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
            }

            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.OnStatusToast += ShowFeedback;
                KitchenController.Instance.OnPromptChanged += UpdatePrompt;
            }

            if (hirePrepButton != null)
                hirePrepButton.onClick.AddListener(() => OnHireWorker("prep_assistant"));

            if (hireDishwasherButton != null)
                hireDishwasherButton.onClick.AddListener(() => OnHireWorker("dishwasher"));

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnReturnedToMenu += OnReturnedToMenuHandler;
            }

            if (primaryActionButton != null)
                primaryActionButton.onClick.AddListener(() => {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.LocalPlayerInteractPrimary();
                });

            if (secondaryActionButton != null)
                secondaryActionButton.onClick.AddListener(() => {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.LocalPlayerInteractSecondary();
                });

            if (throwActionButton != null)
                throwActionButton.onClick.AddListener(() => {
                    if (KitchenController.Instance != null)
                        KitchenController.Instance.LocalPlayerThrow();
                });

            // Mobile D-Pad listeners
            SetupDpadButton(dpadUpButton, Vector2.up);
            SetupDpadButton(dpadDownButton, Vector2.down);
            SetupDpadButton(dpadLeftButton, Vector2.left);
            SetupDpadButton(dpadRightButton, Vector2.right);

            if (retryShiftButton != null)
            {
                retryShiftButton.onClick.AddListener(OnRetryClicked);
            }
        }

        private void SetupDpadButton(Button btn, Vector2 dir)
        {
            if (btn == null) return;
            var trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            entryDown.callback.AddListener((_) => SetJoystick(dir));
            trigger.triggers.Add(entryDown);

            var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
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

        private void Update()
        {
            if (ShiftManager.Instance == null || !ShiftManager.Instance.shiftActive) return;

            // Timer display
            if (shiftTimerText != null)
            {
                if (ShiftManager.Instance.currentMode == GameMode.CAMPAIGN)
                {
                    int m = Mathf.FloorToInt(ShiftManager.Instance.shiftTimeRemaining / 60.0f);
                    int s = Mathf.FloorToInt(ShiftManager.Instance.shiftTimeRemaining % 60.0f);
                    shiftTimerText.text = $"TIME: {m:D2}:{s:D2}";
                }
                else
                {
                    int m = Mathf.FloorToInt(ShiftManager.Instance.shiftElapsedSeconds / 60.0f);
                    int s = Mathf.FloorToInt(ShiftManager.Instance.shiftElapsedSeconds % 60.0f);
                    shiftTimerText.text = $"SURVIVED: {m:D2}:{s:D2} (Misses: {ShiftManager.Instance.ordersFailed}/3)";
                }
            }

            if (scoreText != null)
                scoreText.text = $"SCORE: {ShiftManager.Instance.totalScore}";

            if (cleanPlatesText != null)
                cleanPlatesText.text = $"CLEAN: {ShiftManager.Instance.cleanPlates}";

            if (dirtyPlatesText != null)
                dirtyPlatesText.text = $"DIRTY: {ShiftManager.Instance.dirtyPlatesAtReturn}";

            // Jiwon cooldown display
            if (KitchenController.Instance != null)
            {
                if (hirePrepCooldownText != null)
                {
                    float cd = KitchenController.Instance.workerCooldowns.TryGetValue("prep_assistant", out float val) ? val : 0.0f;
                    hirePrepCooldownText.text = cd > 0.0f ? $"{cd:F0}s" : "READY (20c)";
                }
                if (hireDishwasherCooldownText != null)
                {
                    float cd = KitchenController.Instance.workerCooldowns.TryGetValue("dishwasher", out float val) ? val : 0.0f;
                    hireDishwasherCooldownText.text = cd > 0.0f ? $"{cd:F0}s" : "READY (20c)";
                }
            }

            // Live order queue refresh
            RefreshOrders(ShiftManager.Instance.activeOrders);

            if (feedbackTimer > 0.0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0.0f && feedbackBannerText != null)
                {
                    feedbackBannerText.gameObject.SetActive(false);
                }
            }
        }

        private void OnFlowUpdated(float flow, bool rushActive, float rushRemaining)
        {
            if (flowSlider != null)
                flowSlider.value = flow / ShiftManager.MAX_FLOW;

            if (flowText != null)
            {
                flowText.text = rushActive ? $"★ RUSH RHYTHM! ({rushRemaining:F1}s) ★" : $"FLOW: {flow:F0}/100";
            }
        }

        private void OnRushRhythmChanged(bool isActive)
        {
            if (rushBannerObj != null)
                rushBannerObj.SetActive(isActive);
        }

        private void RefreshOrders(List<OrderTicket> orders)
        {
            if (orderListText == null || orders == null) return;

            if (orders.Count == 0)
            {
                orderListText.text = "ACTIVE ORDERS: None (Waiting for tickets...)";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ACTIVE ORDERS:");
            foreach (var o in orders)
            {
                string rName = o.recipeId;
                int score = 100;
                if (GameData.Instance != null && GameData.Instance.recipes.TryGetValue(o.recipeId, out var rSpec))
                {
                    rName = rSpec.displayName;
                    score = rSpec.baseScore;
                }
                string bonusTag = o.handoffBonusClaimed ? "" : " ★Flow+15";
                sb.AppendLine($"• #{o.ticketId} {rName} [{o.timeRemaining:F0}s] {score}pts{bonusTag}");
            }
            orderListText.text = sb.ToString();
        }

        private void ShowFeedback(string message, Color col)
        {
            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = message;
                feedbackBannerText.color = col;
                feedbackBannerText.gameObject.SetActive(true);
                feedbackTimer = 2.5f;
            }
        }

        private void ShowEndShiftModal(Dictionary<string, object> summary)
        {
            if (endShiftModal != null)
            {
                endShiftModal.SetActive(true);
                if (endShiftDetailsText != null && summary != null)
                {
                    int score = summary.ContainsKey("finalScore") ? System.Convert.ToInt32(summary["finalScore"]) : 0;
                    int starsCount = summary.ContainsKey("stars") ? System.Convert.ToInt32(summary["stars"]) : 0;
                    int coins = summary.ContainsKey("coinsEarned") ? System.Convert.ToInt32(summary["coinsEarned"]) : 0;
                    int served = summary.ContainsKey("ordersServed") ? System.Convert.ToInt32(summary["ordersServed"]) : 0;
                    int failed = summary.ContainsKey("ordersFailed") ? System.Convert.ToInt32(summary["ordersFailed"]) : 0;
                    float rush = summary.ContainsKey("rushSeconds") ? System.Convert.ToSingle(summary["rushSeconds"]) : 0.0f;

                    string stars = starsCount > 0 ? new string('★', starsCount) : "None";
                    endShiftDetailsText.text =
                        $"SHIFT COMPLETE!\n\n" +
                        $"Final Score: {score}\n" +
                        $"Stars Earned: {stars}\n" +
                        $"Coins Earned: +{coins}\n" +
                        $"Orders Served: {served}\n" +
                        $"Orders Failed: {failed}\n" +
                        $"Rush Multiplier Time: {rush:F1}s";
                }
            }
        }

        private void OnRetryClicked()
        {
            if (endShiftModal != null) endShiftModal.SetActive(false);
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.StartShift(ShiftManager.Instance.currentStageId, ShiftManager.Instance.currentMode);
            }
        }

        private void OnHireWorker(string roleId)
        {
            if (KitchenController.Instance != null)
            {
                int peerId = 2; // Jiwon's default peer
                var res = KitchenController.Instance.RequestHireWorker(peerId, roleId);
                if (!res.ok)
                {
                    ShowFeedback(res.reason, Color.red);
                }
            }
        }

        private void OnReturnToLobbyClicked()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.ReturnToMenu("Returned to Lobby.");
            }
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.EndShift();
            }
        }

        private void OnReturnedToMenuHandler(string reason)
        {
            if (ShiftManager.Instance != null && ShiftManager.Instance.shiftActive)
            {
                ShiftManager.Instance.EndShift();
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnReturnedToMenu -= OnReturnedToMenuHandler;
            }
        }
    }
}
