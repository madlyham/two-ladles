using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Networking;

namespace TwoLadles.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Connection Panel")]
        public InputField ipInput;
        public InputField portInput;
        public InputField nameInput;
        public Button hostButton;
        public Button joinButton;
        public Text statusText;

        [Header("Lobby Controls")]
        public Dropdown characterDropdown;
        public Dropdown specializationDropdown;
        public Dropdown stageDropdown;
        public Toggle endlessToggle;
        public Button readyButton;
        public Button startShiftButton;
        public Text readyButtonText;
        public Text playersListText;

        [Header("Progression & Shop")]
        public Text coinsText;
        public Text careerScoreText;
        public Button buyKnivesButton;
        public Button buyStockpotButton;

        private void Start()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (startShiftButton != null) startShiftButton.onClick.AddListener(OnStartShiftClicked);
            if (buyKnivesButton != null) buyKnivesButton.onClick.AddListener(() => OnBuyUpgrade("sharp_santoku_knives"));
            if (buyStockpotButton != null) buyStockpotButton.onClick.AddListener(() => OnBuyUpgrade("insulated_broth_pot"));

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionStatusChanged += UpdateStatus;
                NetworkManager.Instance.OnLobbyUpdated += RefreshLobbyDisplay;
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionUpdated += RefreshProgressionDisplay;
                RefreshProgressionDisplay();
            }
        }

        private void OnHostClicked()
        {
            int port = 24567;
            if (portInput != null && int.TryParse(portInput.text, out int p)) port = p;
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Host Chef";

            NetworkManager.Instance.HostGame(port, pName);
        }

        private void OnJoinClicked()
        {
            string ip = ipInput != null ? ipInput.text : "127.0.0.1";
            int port = 24567;
            if (portInput != null && int.TryParse(portInput.text, out int p)) port = p;
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Client Chef";

            NetworkManager.Instance.JoinGame(ip, port, pName);
        }

        private void OnReadyClicked()
        {
            if (NetworkManager.Instance == null) return;
            int localId = NetworkManager.Instance.localPeerId;
            if (NetworkManager.Instance.players.TryGetValue(localId, out var p))
            {
                NetworkManager.Instance.SetPlayerReady(localId, !p.ready);
            }
        }

        private void OnStartShiftClicked()
        {
            var res = NetworkManager.Instance.CanStartShift(false);
            if (!res.ok)
            {
                UpdateStatus(res.reason, true);
                return;
            }

            int stage = stageDropdown != null ? stageDropdown.value + 1 : 1;
            GameMode mode = (endlessToggle != null && endlessToggle.isOn) ? GameMode.ENDLESS_RUSH : GameMode.CAMPAIGN;
            ShiftManager.Instance.StartShift(stage, mode);
            gameObject.SetActive(false);
        }

        private void OnBuyUpgrade(string upgradeId)
        {
            if (ProgressionManager.Instance != null)
            {
                var res = ProgressionManager.Instance.PurchaseUpgrade(upgradeId);
                if (!res.ok) UpdateStatus(res.reason, true);
            }
        }

        private void UpdateStatus(string message, bool isError)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = isError ? new Color(1.0f, 0.4f, 0.4f) : Color.white;
            }
        }

        private void RefreshLobbyDisplay(Dictionary<int, PlayerLobbyInfo> players)
        {
            if (playersListText != null)
            {
                string s = "Connected Chefs:\n";
                foreach (var p in players.Values)
                {
                    string readyMark = p.ready ? "[READY]" : "[NOT READY]";
                    s += $"• Peer {p.peerId}: {p.name} ({p.character.ToUpper()}) - {readyMark}\n";
                }
                playersListText.text = s;
            }

            if (NetworkManager.Instance != null && NetworkManager.Instance.players.TryGetValue(NetworkManager.Instance.localPeerId, out var localP))
            {
                if (readyButtonText != null)
                {
                    readyButtonText.text = localP.ready ? "CANCEL READY" : "TOGGLE READY";
                }
            }
        }

        private void RefreshProgressionDisplay()
        {
            if (ProgressionManager.Instance == null) return;

            if (coinsText != null)
                coinsText.text = $"Coins: {ProgressionManager.Instance.coins}";

            if (careerScoreText != null)
                careerScoreText.text = $"Career Score: {ProgressionManager.Instance.totalCareerScore}";
        }
    }
}
