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

        private void Awake()
        {
            if (hostButton == null)
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

        private InputField CreateInputField(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, string defaultText, string placeholderText)
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
            img.color = new Color(0.12f, 0.12f, 0.16f, 1f);

            var input = go.AddComponent<InputField>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-12f, -6f);
            var txt = textObj.AddComponent<Text>();
            txt.font = GetDefaultFont();
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(go.transform, false);
            var pRect = phObj.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.sizeDelta = new Vector2(-12f, -6f);
            var pTxt = phObj.AddComponent<Text>();
            pTxt.font = GetDefaultFont();
            pTxt.fontSize = 13;
            pTxt.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            pTxt.fontStyle = FontStyle.Italic;
            pTxt.text = placeholderText;
            pTxt.alignment = TextAnchor.MiddleLeft;

            input.textComponent = txt;
            input.placeholder = pTxt;
            input.text = defaultText;
            return input;
        }

        private void AutoBuildUI()
        {
            var root = transform;

            // Background Panel
            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            var bgImg = gameObject.GetComponent<Image>();
            if (bgImg == null) bgImg = gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.07f, 0.10f, 0.96f);

            // Title
            CreateLabel("Title", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(700f, 40f), "TWO LADLES: VIETNAM & KOREA CO-OP KITCHEN", 20, Color.yellow, TextAnchor.MiddleCenter);
            CreateLabel("SubTitle", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(700f, 24f), "Cross-Prep Culinary Rhythm Game | Korea Game Week 2026 Submission", 13, new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleCenter);

            // Network Panel
            CreateLabel("NetHeader", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(600f, 24f), "LAN UDP MULTIPLAYER (Port 24567)", 15, Color.white, TextAnchor.MiddleCenter);

            nameInput = CreateInputField("NameInput", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-220f, -135f), new Vector2(170f, 32f), "Chef Linh", "Player Name");
            ipInput = CreateInputField("IpInput", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -135f), new Vector2(160f, 32f), "127.0.0.1", "Host IPv4");
            portInput = CreateInputField("PortInput", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(100f, -135f), new Vector2(90f, 32f), "24567", "Port");

            hostButton = CreateButton("HostBtn", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(200f, -135f), new Vector2(90f, 32f), "HOST", new Color(0.2f, 0.6f, 0.35f, 1f), out _);
            joinButton = CreateButton("JoinBtn", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(295f, -135f), new Vector2(90f, 32f), "JOIN", new Color(0.25f, 0.5f, 0.75f, 1f), out _);

            statusText = CreateLabel("StatusText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(700f, 30f), "Ready to host or join LAN session.", 13, Color.white, TextAnchor.MiddleCenter);

            // Connected Players
            playersListText = CreateLabel("PlayersList", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(500f, 70f), "Connected Chefs:\n• Host (Linh) [READY]\n• Client (Jiwon) [NOT READY]", 13, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter);

            // Controls
            readyButton = CreateButton("ReadyBtn", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 100f), new Vector2(180f, 44f), "TOGGLE READY", new Color(0.3f, 0.5f, 0.7f, 1f), out readyButtonText);
            startShiftButton = CreateButton("StartShiftBtn", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120f, 100f), new Vector2(180f, 44f), "START SHIFT", new Color(0.85f, 0.45f, 0.15f, 1f), out _);

            // Career / Shop
            coinsText = CreateLabel("CoinsText", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 45f), new Vector2(200f, 26f), "Coins: 0", 14, Color.yellow, TextAnchor.MiddleCenter);
            careerScoreText = CreateLabel("CareerScoreText", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 45f), new Vector2(200f, 26f), "Career Score: 0", 14, Color.cyan, TextAnchor.MiddleCenter);
        }

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
                NetworkManager.Instance.OnShiftStartReceived += (stage, mode) => gameObject.SetActive(false);
                NetworkManager.Instance.OnReturnedToMenu += OnReturnedToMenuHandler;
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
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsHost())
            {
                NetworkManager.Instance.BroadcastShiftStart(stage, mode);
            }
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

        private void OnReturnedToMenuHandler(string reason)
        {
            gameObject.SetActive(true);
            UpdateStatus(reason, false);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionStatusChanged -= UpdateStatus;
                NetworkManager.Instance.OnLobbyUpdated -= RefreshLobbyDisplay;
                NetworkManager.Instance.OnReturnedToMenu -= OnReturnedToMenuHandler;
            }
        }
    }
}
