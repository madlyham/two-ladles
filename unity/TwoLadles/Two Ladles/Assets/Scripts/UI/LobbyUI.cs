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
        [Header("Connection & LAN Discovery")]
        public InputField ipInput;
        public InputField portInput;
        public InputField nameInput;
        public InputField roomNameInput;
        public Button hostButton;
        public Button joinButton;
        public Text statusText;
        public Button[] lanRoomButtons = new Button[2];
        public Text[] lanRoomButtonLabels = new Text[2];
        private List<LanRoomInfo> currentDiscoveredRooms = new List<LanRoomInfo>();

        [Header("Lobby & Level Selection")]
        public Dropdown characterDropdown;
        public Dropdown specializationDropdown;
        public Dropdown stageDropdown;
        public Toggle endlessToggle;
        public Button readyButton;
        public Button startShiftButton;
        public Button playTutorialButton;
        public Button endlessModeButton;
        public Button openShopButton;
        public Button[] levelSelectButtons = new Button[5];
        public Text[] levelSelectLabels = new Text[5];
        public Image[] levelSelectBgs = new Image[5];
        public Text readyButtonText;
        public Text startShiftButtonText;
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

        private int selectedLevelId = 1;
        private bool isEndlessSelected = false;

        private void Awake()
        {
            if (hostButton == null)
            {
                AutoBuildUI();
            }
        }

        private static Font GetDefaultFont()
        {
            return KitchenHUD.GetDefaultFont();
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

            outLabel = CreateLabel(name + "_Label", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -4f), labelText, fontSize, Color.white, TextAnchor.MiddleCenter);
            return btn;
        }

        private InputField CreateInputField(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, string defaultText, string placeholderText)
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
            img.color = new Color(0.12f, 0.13f, 0.18f, 1f);

            var input = go.AddComponent<InputField>();

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(go.transform, false);
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-10f, -4f);
            var txt = textObj.AddComponent<Text>();
            txt.font = GetDefaultFont();
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform));
            phObj.transform.SetParent(go.transform, false);
            var pRect = phObj.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.sizeDelta = new Vector2(-10f, -4f);
            var pTxt = phObj.AddComponent<Text>();
            pTxt.font = GetDefaultFont();
            pTxt.fontSize = 12;
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

            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            var root = rootRect.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;

            var bgImg = gameObject.GetComponent<Image>();
            if (bgImg != null) Destroy(bgImg);

            // 1. Floating Menu Toggle Button (Top-Right below TopBar)
            toggleMenuButton = CreateButton("ToggleMenuBtn", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -44f), new Vector2(125f, 28f), "Hide Menu [ESC]", new Color(0.2f, 0.25f, 0.35f, 0.9f), out toggleMenuButtonText, 11);
            toggleMenuButton.onClick.AddListener(ToggleMenuVisibility);

            // 2. Main Lobby & Level Selection Modal Card
            GameObject modal = new GameObject("LobbyModalPanel", typeof(RectTransform));
            modal.transform.SetParent(root, false);
            modalPanel = modal;

            var mRect = modal.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = new Vector2(0f, 10f);
            mRect.sizeDelta = new Vector2(720f, 430f);

            var mImg = modal.AddComponent<Image>();
            mImg.color = new Color(0.07f, 0.09f, 0.13f, 0.94f);

            Transform mRoot = modal.transform;

            // Header
            CreateLabel("Title", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(680f, 24f), "TWO LADLES: VIETNAM & KOREA CO-OP", 16, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter);
            coinsText = CreateLabel("GoldText", mRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(180f, 22f), "GOLD: 150g", 14, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleLeft);
            careerScoreText = CreateLabel("CareerScoreText", mRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -14f), new Vector2(180f, 22f), "SCORE: 0", 13, Color.cyan, TextAnchor.MiddleRight);

            // --- SECTION A: TERRARIA-STYLE LAN ROOM HOST & AUTO-SCAN ---
            CreateLabel("LanHeader", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(680f, 18f), "LAN MULTIPLAYER — CREATE A ROOM OR CLICK A DISCOVERED ROOM TO JOIN", 11, new Color(0.65f, 0.82f, 0.95f), TextAnchor.MiddleCenter);

            nameInput = CreateInputField("NameInput", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-245f, -68f), new Vector2(150f, 28f), "Chef Linh", "Your Name");
            roomNameInput = CreateInputField("RoomNameInput", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, -68f), new Vector2(195f, 28f), "Linh & Jiwon Bistro", "Room Name");
            hostButton = CreateButton("HostBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(115f, -68f), new Vector2(130f, 28f), "HOST ROOM", new Color(0.18f, 0.62f, 0.35f, 1f), out _);
            joinButton = CreateButton("JoinBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(252f, -68f), new Vector2(125f, 28f), "AUTO-JOIN LAN", new Color(0.22f, 0.50f, 0.78f, 1f), out _);

            // Discovered LAN Room Slots
            for (int i = 0; i < 2; i++)
            {
                int slotIdx = i;
                float xPos = (i == 0) ? -170f : 170f;
                lanRoomButtons[i] = CreateButton($"LanRoomBtn_{i}", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(xPos, -102f), new Vector2(325f, 26f),
                    i == 0 ? "[LAN] Scanning for open kitchens..." : "[LAN] Click to refresh scan",
                    new Color(0.14f, 0.18f, 0.24f, 0.95f), out lanRoomButtonLabels[i], 11);
                lanRoomButtons[i].onClick.AddListener(() => OnLanRoomSlotClicked(slotIdx));
            }

            statusText = CreateLabel("StatusText", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(680f, 18f), "LAN Auto-Scanner Active.", 11, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter);

            // --- SECTION B: LEVEL SELECTION & REPLAY (LEVELS 1-5) ---
            CreateLabel("LevelSelectHeader", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -154f), new Vector2(680f, 18f), "SELECT LEVEL (REPLAY COMPLETED LEVELS ANYTIME TO EARN MORE GOLD)", 11, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleCenter);

            string[] lvlShortNames = new string[] { "Tteokbokki", "+ Bột Chiên", "+ K-BBQ Rice", "+ Broken Rice", "3m Banh Mi" };
            for (int i = 0; i < 5; i++)
            {
                int lvlId = i + 1;
                float xPos = -272f + (i * 136f);
                bool defaultUnlocked = (lvlId == 1);
                string defaultTag = defaultUnlocked ? "UNLOCKED" : "LOCKED";
                Color defaultCol = defaultUnlocked ? new Color(0.88f, 0.55f, 0.15f, 1f) : new Color(0.16f, 0.16f, 0.18f, 0.85f);
                levelSelectButtons[i] = CreateButton($"LevelBtn_{lvlId}", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(xPos, -198f), new Vector2(128f, 58f),
                    $"DAY {lvlId}\n{lvlShortNames[i]}\n[{defaultTag}]",
                    defaultCol, out levelSelectLabels[i], 11);
                levelSelectBgs[i] = levelSelectButtons[i].GetComponent<Image>();
                levelSelectButtons[i].onClick.AddListener(() => SelectCampaignLevel(lvlId));
            }

            // Mode Row: Tutorial, Endless Survival, Upgrade Shop
            playTutorialButton = CreateButton("PlayTutorialBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-225f, -264f), new Vector2(205f, 36f), "★ PLAY TUTORIAL ★", new Color(0.18f, 0.58f, 0.38f, 1f), out _, 12);
            endlessModeButton = CreateButton("EndlessBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -264f), new Vector2(205f, 36f), "∞ ENDLESS MODE", new Color(0.48f, 0.25f, 0.58f, 1f), out _, 12);
            openShopButton = CreateButton("OpenShopBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(225f, -264f), new Vector2(205f, 36f), "★ UPGRADE SHOP ★", new Color(0.75f, 0.52f, 0.12f, 1f), out _, 12);

            // Primary Launch Row: Ready & Start Selected Level
            readyButton = CreateButton("ReadyBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-155f, -318f), new Vector2(220f, 42f), "READY: YES", new Color(0.25f, 0.45f, 0.68f, 1f), out readyButtonText, 13);
            startShiftButton = CreateButton("StartShiftBtn", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(125f, -318f), new Vector2(310f, 42f), "▶ START LEVEL 1 (3 WAVES)", new Color(0.88f, 0.42f, 0.14f, 1f), out startShiftButtonText, 14);

            playersListText = CreateLabel("PlayersList", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -372f), new Vector2(680f, 20f), "Chefs: Linh (West) & Jiwon (East)", 11, new Color(0.8f, 0.85f, 0.9f), TextAnchor.MiddleCenter);
            controlsGuideText = CreateLabel("ControlsGuide", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -396f), new Vector2(680f, 20f), "[WASD] Move  •  [TAB] Ingredient Wheel  •  [E] Use  •  [F] Chop/Wash  •  [C] Switch Chef  •  [R] Recipe Book", 11, new Color(0.70f, 0.85f, 0.98f), TextAnchor.MiddleCenter);

            RefreshProgressionDisplay();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
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
                    toggleMenuButtonText.text = newState ? "Hide Menu [ESC]" : "Show Menu [ESC]";
                }
                if (newState)
                {
                    RefreshProgressionDisplay();
                }
            }
        }

        private void Start()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (playTutorialButton != null) playTutorialButton.onClick.AddListener(OnPlayTutorialClicked);
            if (endlessModeButton != null) endlessModeButton.onClick.AddListener(OnSelectEndlessClicked);
            if (openShopButton != null) openShopButton.onClick.AddListener(OnOpenShopClicked);
            if (startShiftButton != null) startShiftButton.onClick.AddListener(OnStartShiftClicked);
            if (buyKnivesButton != null) buyKnivesButton.onClick.AddListener(() => OnBuyUpgrade("sharp_santoku_knives"));
            if (buyStockpotButton != null) buyStockpotButton.onClick.AddListener(() => OnBuyUpgrade("insulated_broth_pot"));

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionStatusChanged += UpdateStatus;
                NetworkManager.Instance.OnLobbyUpdated += RefreshLobbyDisplay;
                NetworkManager.Instance.OnLanRoomsUpdated += RefreshLanRoomsDisplay;
                NetworkManager.Instance.OnShiftStartReceived += (stage, mode) => gameObject.SetActive(false);
                NetworkManager.Instance.OnReturnedToMenu += OnReturnedToMenuHandler;
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionUpdated += RefreshProgressionDisplay;
                selectedLevelId = Mathf.Clamp(ProgressionManager.Instance.highestUnlockedStage, 1, 5);
                RefreshProgressionDisplay();
            }
        }

        public void SelectCampaignLevel(int lvlId)
        {
            if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsLevelUnlocked(lvlId))
            {
                UpdateStatus($"Level {lvlId} is LOCKED! Complete Level {lvlId - 1} first.", true);
                return;
            }

            isEndlessSelected = false;
            selectedLevelId = lvlId;
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.selectedStage = lvlId;
            }
            RefreshProgressionDisplay();
            UpdateStatus($"Selected Level {lvlId}. Click START LEVEL to play (or replay for extra Gold)!", false);
        }

        private void OnSelectEndlessClicked()
        {
            isEndlessSelected = true;
            RefreshProgressionDisplay();
            UpdateStatus("Selected ENDLESS SURVIVAL MODE! Click START to begin.", false);
        }

        private void OnOpenShopClicked()
        {
            var hud = FindObjectOfType<KitchenHUD>(true);
            if (hud != null)
            {
                hud.OpenUpgradeShopModal();
            }
        }

        private void OnHostClicked()
        {
            int port = NetworkManager.DEFAULT_PORT;
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Linh";
            string rName = roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text) ? roomNameInput.text : $"{pName}'s Kitchen";

            NetworkManager.Instance.HostGame(port, pName, rName);
        }

        private void OnJoinClicked()
        {
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Jiwon";
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendLanDiscoveryProbe();
                NetworkManager.Instance.AutoJoinLanRoom(pName);
            }
        }

        private void OnLanRoomSlotClicked(int slotIdx)
        {
            if (NetworkManager.Instance == null) return;
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Jiwon";
            if (slotIdx >= 0 && slotIdx < currentDiscoveredRooms.Count)
            {
                NetworkManager.Instance.JoinDiscoveredRoom(currentDiscoveredRooms[slotIdx], pName);
            }
            else
            {
                NetworkManager.Instance.SendLanDiscoveryProbe();
                UpdateStatus("Scanning LAN for open rooms...", false);
            }
        }

        private void RefreshLanRoomsDisplay(List<LanRoomInfo> rooms)
        {
            currentDiscoveredRooms = rooms ?? new List<LanRoomInfo>();
            for (int i = 0; i < 2; i++)
            {
                if (lanRoomButtonLabels[i] == null) continue;
                if (i < currentDiscoveredRooms.Count)
                {
                    var r = currentDiscoveredRooms[i];
                    lanRoomButtonLabels[i].text = $"[JOIN] {r.roomName} ({r.hostName}) [{r.playerCount}/2]";
                    lanRoomButtons[i].GetComponent<Image>().color = new Color(0.16f, 0.45f, 0.30f, 0.95f);
                }
                else
                {
                    lanRoomButtonLabels[i].text = i == 0 ? "[LAN] Scanning for open kitchens..." : "[LAN] Click to refresh scan";
                    lanRoomButtons[i].GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.24f, 0.95f);
                }
            }
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
                NetworkManager.Instance.HostGame(24567, "Chef Linh", "Tutorial Kitchen");
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
                string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Linh";
                string rName = roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text) ? roomNameInput.text : "Two Ladles Bistro";
                NetworkManager.Instance.HostGame(24567, pName, rName);
            }

            int stage = selectedLevelId;
            if (!isEndlessSelected && ProgressionManager.Instance != null && !ProgressionManager.Instance.IsLevelUnlocked(stage))
            {
                UpdateStatus($"Level {stage} is locked! Complete previous levels first.", true);
                return;
            }

            GameMode mode = isEndlessSelected ? GameMode.ENDLESS_RUSH : GameMode.CAMPAIGN;
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
                statusText.color = isError ? new Color(1.0f, 0.4f, 0.4f) : new Color(0.85f, 0.95f, 1.0f);
            }
        }

        private void RefreshLobbyDisplay(Dictionary<int, PlayerLobbyInfo> players)
        {
            if (playersListText != null)
            {
                if (players == null || players.Count == 0)
                {
                    playersListText.text = "Chefs: Linh (West) & Jiwon (East)";
                }
                else
                {
                    var parts = new List<string>();
                    foreach (var p in players.Values)
                    {
                        string readyMark = p.ready ? "READY" : "WAIT";
                        parts.Add($"{p.name} ({p.character.ToUpper()}) [{readyMark}]");
                    }
                    playersListText.text = "Room Chefs: " + string.Join("  •  ", parts);
                }
            }

            if (NetworkManager.Instance != null && NetworkManager.Instance.players.TryGetValue(NetworkManager.Instance.localPeerId, out var localP))
            {
                if (readyButtonText != null)
                {
                    readyButtonText.text = localP.ready ? "READY: YES" : "READY: NO";
                }
            }
        }

        public void RefreshProgressionDisplay()
        {
            if (ProgressionManager.Instance == null) return;

            if (coinsText != null)
                coinsText.text = $"GOLD: {ProgressionManager.Instance.gold}g";

            if (careerScoreText != null)
                careerScoreText.text = $"SCORE: {ProgressionManager.Instance.totalCareerScore}";

            string[] lvlNames = new string[] { "Tteokbokki", "+ Bột Chiên", "+ K-BBQ Rice", "+ Broken Rice", "3m Banh Mi" };
            for (int i = 0; i < 5; i++)
            {
                int lvlId = i + 1;
                if (levelSelectLabels[i] == null || levelSelectBgs[i] == null) continue;

                bool unlocked = ProgressionManager.Instance.IsLevelUnlocked(lvlId);
                bool completed = ProgressionManager.Instance.IsLevelCompleted(lvlId);
                bool isSelected = (!isEndlessSelected && selectedLevelId == lvlId);

                string statusTag = completed ? "★ COMPLETED ★" : (unlocked ? "UNLOCKED" : "LOCKED");
                levelSelectLabels[i].text = $"DAY {lvlId}\n{lvlNames[i]}\n[{statusTag}]";

                if (!unlocked)
                {
                    levelSelectBgs[i].color = new Color(0.16f, 0.16f, 0.18f, 0.85f);
                }
                else if (isSelected)
                {
                    levelSelectBgs[i].color = new Color(0.88f, 0.55f, 0.15f, 1.0f);
                }
                else if (completed)
                {
                    levelSelectBgs[i].color = new Color(0.18f, 0.52f, 0.32f, 0.95f);
                }
                else
                {
                    levelSelectBgs[i].color = new Color(0.20f, 0.36f, 0.56f, 0.95f);
                }
            }

            if (startShiftButtonText != null)
            {
                if (isEndlessSelected)
                {
                    startShiftButtonText.text = "▶ START ENDLESS RUSH";
                }
                else
                {
                    bool done = ProgressionManager.Instance.IsLevelCompleted(selectedLevelId);
                    string actionVerb = done ? "REPLAY" : "START";
                    string suffix = (selectedLevelId == 5) ? "(3-MIN HIGH SCORE)" : "(3 WAVES)";
                    startShiftButtonText.text = $"▶ {actionVerb} DAY {selectedLevelId} {suffix}";
                }
            }
        }

        private void OnReturnedToMenuHandler(string reason)
        {
            gameObject.SetActive(true);
            if (modalPanel != null) modalPanel.SetActive(true);
            RefreshProgressionDisplay();
            UpdateStatus(reason, false);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionStatusChanged -= UpdateStatus;
                NetworkManager.Instance.OnLobbyUpdated -= RefreshLobbyDisplay;
                NetworkManager.Instance.OnLanRoomsUpdated -= RefreshLanRoomsDisplay;
                NetworkManager.Instance.OnReturnedToMenu -= OnReturnedToMenuHandler;
            }
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionUpdated -= RefreshProgressionDisplay;
            }
        }
    }
}
