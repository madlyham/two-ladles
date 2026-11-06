using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Kitchen;

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
        public Transform orderQueueContainer;
        public GameObject orderTicketPrefab;

        [Header("Plate Stack")]
        public Text cleanPlatesText;
        public Text dirtyPlatesText;

        [Header("Jiwon Crew Bar")]
        public GameObject jiwonCrewBar;
        public Button hirePrepButton;
        public Button hireDishwasherButton;
        public Text hirePrepCooldownText;
        public Text hireDishwasherCooldownText;

        [Header("Mobile Action Buttons")]
        public Button primaryActionButton;
        public Button secondaryActionButton;

        [Header("Feedback Banner")]
        public Text feedbackBannerText;
        private float feedbackTimer = 0.0f;

        private void Start()
        {
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.OnFlowUpdated += OnFlowUpdated;
                ShiftManager.Instance.OnRushRhythmChanged += OnRushRhythmChanged;
                ShiftManager.Instance.OnOrderQueueUpdated += RefreshOrders;
                ShiftManager.Instance.OnFeedbackBanner += ShowFeedback;
            }

            if (hirePrepButton != null)
                hirePrepButton.onClick.AddListener(() => OnHireWorker("prep_assistant"));

            if (hireDishwasherButton != null)
                hireDishwasherButton.onClick.AddListener(() => OnHireWorker("dishwasher"));
        }

        private void Update()
        {
            if (ShiftManager.Instance == null || !ShiftManager.Instance.shiftActive) return;

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
            // Update order display widgets
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
