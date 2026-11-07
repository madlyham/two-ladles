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
            sb.AppendLine("ACTIVE CUSTOMER ORDERS:");
            foreach (var o in orders)
            {
                string rName = o.recipeId;
                int score = 100;
                if (GameData.Instance != null && GameData.Instance.recipes.TryGetValue(o.recipeId, out var rSpec))
                {
                    rName = rSpec.displayName;
                    score = rSpec.baseScore;
                }
                string bonusTag = o.handoffBonusClaimed ? "" : " [Pass Handoff +15 Flow Available]";
                sb.AppendLine($"#{o.ticketId} {rName} — {o.timeRemaining:F0}s left | {score} pts{bonusTag}");
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
    }
}
