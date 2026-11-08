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
        public Button playTutorialButton;
        public Text readyButtonText;
        public Text playersListText;
        public Text controlsGuideText;

        [Header("Progression & Shop")]
        public Text coinsText;
        public Text careerScoreText;
        public Button buyKnivesButton;
        public Button buyStockpotButton;

        [Header("Modal & Toggle")]
        public GameObject modalPanel;
        public Button toggleMenuButton;
        public Text toggleMenuButtonText;

        private void Awake()
        {
            if (hostButton == null)
            {
                AutoBuildUI();
            }
        }

        private static Font GetDefaultFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
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
                catch {}
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

        public void AutoBuildUI()
        {
            if (hostButton != null) return;

            // Transparent Root (allows 100% full view of 3D kitchen)
            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            var root = rootRect.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;

            var bgImg = gameObject.GetComponent<Image>();
            if (bgImg != null) Destroy(bgImg);

            // 1. Floating Menu Toggle Button (Pinned to Top-Right below TopBar)
            toggleMenuButton = CreateButton("ToggleMenuBtn", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -44f), new Vector2(140f, 32f), "Hide Menu [TAB]", new Color(0.2f, 0.25f, 0.35f, 0.9f), out toggleMenuButtonText);
            toggleMenuButton.onClick.AddListener(ToggleMenuVisibility);

            // 2. Compact Floating Modal Card (Pinned to Top-Center below TopBar)
            GameObject modal = new GameObject("LobbyModalPanel");
            modal.transform.SetParent(root, false);
            modalPanel = modal;

            var mRect = modal.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 1f);
            mRect.anchorMax = new Vector2(0.5f, 1f);
            mRect.pivot = new Vector2(0.5f, 1f);
            mRect.anchoredPosition = new Vector2(0f, -44f);
            mRect.sizeDelta = new Vector2(640f, 305f);

            var mImg = modal.AddComponent<Image>();
            mImg.color = new Color(0.08f, 0.10f, 0.14f, 0.90f); // Sleek dark card

            Transform mRoot = modal.transform;

            // Title
            CreateLabel("Title", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(600f, 26f), "TWO LADLES: VIETNAM & KOREA CO-OP", 16, Color.yellow, TextAnchor.MiddleCenter);
            CreateLabel("SubTitle", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 18f), "Korea Game Week 2026 | Press [TAB] to toggle menu", 11, new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleCenter);

            // Network Input Row
            nameInput = CreateInputField("NameInput", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-205f, -72f), new Vector2(150f, 28f), "Chef Linh", "Player Name");
            ipInput = CreateInputField("IpInput", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-55f, -72f), new Vector2(130f, 28f), "127.0.0.1", "Host IP");
            portInput = CreateInputField("PortInput", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(55f, -72f), new Vector2(70f, 28f), "24567", "Port");

            hostButton = CreateButton("HostBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(145f, -72f), new Vector2(85f, 28f), "HOST", new Color(0.2f, 0.6f, 0.35f, 1f), out _);
            joinButton = CreateButton("JoinBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(235f, -72f), new Vector2(75f, 28f), "JOIN", new Color(0.25f, 0.5f, 0.75f, 1f), out _);

            // Status & Connected Players
            statusText = CreateLabel("StatusText", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(600f, 22f), "Ready to host or join LAN session.", 12, Color.white, TextAnchor.MiddleCenter);
            playersListText = CreateLabel("PlayersList", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(600f, 32f), "Connected: Host (Linh) [READY]  •  Client (Jiwon) [READY]", 12, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter);

            // Primary Action Buttons: Ready, Play Tutorial, Start Shift
            readyButton = CreateButton("ReadyBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-208f, -178f), new Vector2(180f, 36f), "TOGGLE READY", new Color(0.3f, 0.5f, 0.7f, 1f), out readyButtonText);
            playTutorialButton = CreateButton("PlayTutorialBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(208f, 36f), "★ PLAY TUTORIAL ★", new Color(0.18f, 0.65f, 0.38f, 1f), out _);
            startShiftButton = CreateButton("StartShiftBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(208f, -178f), new Vector2(180f, 36f), "START SHIFT", new Color(0.85f, 0.45f, 0.15f, 1f), out _);

            // Controls Guide Quick Reference
            controlsGuideText = CreateLabel("ControlsGuide", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(620f, 22f), "CONTROLS: [WASD] Move  •  [TAB] Wheel  •  [E] Use  •  [F] Strike/Scrub  •  [C] Switch Chef  •  [Q] Throw", 11, new Color(0.75f, 0.88f, 0.98f), TextAnchor.MiddleCenter);

            // Career / Coins
            coinsText = CreateLabel("CoinsText", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-130f, -265f), new Vector2(220f, 22f), "Coins: 0", 13, Color.yellow, TextAnchor.MiddleCenter);
            careerScoreText = CreateLabel("CareerScoreText", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(130f, -265f), new Vector2(220f, 22f), "Career Score: 0", 13, Color.cyan, TextAnchor.MiddleCenter);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
            {
                ToggleMenuVisibility();
            }
        }

        public void ToggleMenuVisibility()
        {
            if (modalPanel != null)
            {
                bool newState = !modalPanel.activeSelf;
                modalPanel.SetActive(newState);
                if (toggleMenuButtonText != null)
                {
                    toggleMenuButtonText.text = newState ? "Hide Menu [TAB]" : "Show Menu [TAB]";
                }
            }
        }

        private void Start()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (playTutorialButton != null) playTutorialButton.onClick.AddListener(OnPlayTutorialClicked);
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

        private void OnPlayTutorialClicked()
        {
            if (NetworkManager.Instance != null && !NetworkManager.Instance.isLanActive && NetworkManager.Instance.players.Count == 0)
            {
                NetworkManager.Instance.HostGame(24567, "Chef Linh");
            }

            if (NetworkManager.Instance != null && NetworkManager.Instance.IsHost())
            {
                NetworkManager.Instance.BroadcastShiftStart(0, GameMode.CAMPAIGN);
            }
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.StartShift(0, GameMode.CAMPAIGN);
            }
            gameObject.SetActive(false);
        }

        private void OnStartShiftClicked()
        {
            if (NetworkManager.Instance != null && !NetworkManager.Instance.isLanActive && NetworkManager.Instance.players.Count == 0)
            {
                // Auto-host local session so clicking START SHIFT immediately enters kitchen
                NetworkManager.Instance.HostGame(24567, "Chef Linh");
            }

            (bool ok, string reason) res = NetworkManager.Instance != null 
                ? NetworkManager.Instance.CanStartShift(false) 
                : (ok: true, reason: "");
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
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.StartShift(stage, mode);
            }
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
