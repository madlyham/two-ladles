using System;
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
        [Header("Language & Mode Selection")]
        public Button langEnButton;
        public Button langViButton;
        public Button langKoButton;
        public Text langEnText;
        public Text langViText;
        public Text langKoText;
        public Button modeToggleButton;
        public Text modeToggleText;
        public Button chefSelectButton;
        public Text chefSelectText;
        public bool isSoloMode = false;
        public int humanChefPeerId = 1; // 1 = Linh, 2 = Jiwon

        private Text titleLabel;
        private Text lanHeaderLabel;
        private Text hostButtonLabel;
        private Text joinButtonLabel;
        private Text levelSelectHeaderLabel;
        private Text playTutorialButtonLabel;
        private Text endlessModeButtonLabel;
        private Text openShopButtonLabel;
        public InputField ipInput;
        public InputField portInput;
        public InputField nameInput;
        public InputField roomNameInput;
        public Button hostButton;
        public Button joinButton;
        public Text statusText;
        private Text joinStatusText; // status mirrored inside Join popup (main-menu statusText is hidden there)
        private Text roomStatusText; // status mirrored inside Room panel
        private Text hostIpLabel;    // big "YOUR IP" readout for the host
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
        public Button twoBotTestButton;
        public Button playTutorialButton;
        public Button endlessModeButton;
        public Button openShopButton;
        public Button[] levelSelectButtons = new Button[5];
        public Text[] levelSelectLabels = new Text[5];
        public Image[] levelSelectBgs = new Image[5];
        public Text readyButtonText;
        public Text startShiftButtonText;
        public Text twoBotTestButtonText;
        public Text playersListText;
        public Text controlsGuideText;

        [Header("Progression & Shop")]
        public Text coinsText;
        public Text careerScoreText;
        public Button buyKnivesButton;
        public Button buyStockpotButton;

        [Header("Modal & Sub-Panels")]
        public GameObject modalPanel;
        public Button toggleMenuButton;
        public Text toggleMenuButtonText;

        public GameObject mainMenuPanel;
        public GameObject hostPopupPanel;
        public GameObject joinPopupPanel;
        public GameObject roomPanel;
        public GameObject settingsPopupPanel;
        public GameObject storyPopupPanel;

        public Button openHostPopupButton;
        public Button openJoinPopupButton;
        public Button openSettingsPopupButton;
        public Button openStoryPopupButton;
        public Text openStoryButtonLabel;
        public Button leaveRoomButton;

        public static LobbyUI Instance { get; private set; }

        private int selectedLevelId = 1;
        private bool isEndlessSelected = false;

        private void Awake()
        {
            Instance = this;
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
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(go.transform, false);
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.font = GetDefaultFont();
            txt.text = labelText;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);

            outLabel = txt;
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

            // 1. Floating Menu Toggle Button (Top-Right)
            toggleMenuButton = CreateButton("ToggleMenuBtn", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -44f), new Vector2(125f, 28f), "Hide Menu [ESC]", new Color(0.2f, 0.25f, 0.35f, 0.9f), out toggleMenuButtonText, 11);
            toggleMenuButton.onClick.AddListener(ToggleMenuVisibility);

            // 2. Master Card Container
            GameObject modal = new GameObject("LobbyModalPanel", typeof(RectTransform));
            modal.transform.SetParent(root, false);
            modalPanel = modal;

            var mRect = modal.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = new Vector2(0f, 10f);
            mRect.sizeDelta = new Vector2(740f, 480f);

            var mImg = modal.AddComponent<Image>();
            mImg.color = new Color(0.07f, 0.09f, 0.13f, 0.96f);
            Transform mRoot = modal.transform;

            // Global Header inside card
            titleLabel = CreateLabel("Title", mRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(680f, 26f), "TWO LADLES: VIETNAM & KOREA CO-OP", 16, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter);
            coinsText = CreateLabel("GoldText", mRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(180f, 22f), "GOLD: 150g", 14, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleLeft);
            careerScoreText = CreateLabel("CareerScoreText", mRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -16f), new Vector2(180f, 22f), "SCORE: 0", 13, Color.cyan, TextAnchor.MiddleRight);

            // -------------------------------------------------------------
            // SUB-PANEL A: MAIN MENU (Clean, non-intimidating welcoming screen)
            // -------------------------------------------------------------
            mainMenuPanel = new GameObject("MainMenuPanel", typeof(RectTransform));
            mainMenuPanel.transform.SetParent(mRoot, false);
            var mmRect = mainMenuPanel.GetComponent<RectTransform>();
            mmRect.anchorMin = Vector2.zero;
            mmRect.anchorMax = Vector2.one;
            mmRect.sizeDelta = Vector2.zero;

            Reg(CreateLabel("WelcomeSub", mainMenuPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(680f, 20f), "Authentic Street Food Cooperative Cooking", 12, new Color(0.75f, 0.82f, 0.92f), TextAnchor.MiddleCenter), "menu.welcome_sub");

            // Big Action Buttons for Non-Tech Players
            openHostPopupButton = CreateButton("OpenHostBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 85f), new Vector2(340f, 48f), "👨‍🍳  HOST A KITCHEN", new Color(0.18f, 0.62f, 0.35f, 1f), out var hostKitchenT, 14); Reg(hostKitchenT, "menu.host_kitchen");
            openHostPopupButton.onClick.AddListener(ShowHostPopup);

            openJoinPopupButton = CreateButton("OpenJoinBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(340f, 48f), "🍲  JOIN KITCHEN (LAN)", new Color(0.20f, 0.48f, 0.78f, 1f), out var joinKitchenT, 14); Reg(joinKitchenT, "menu.join_kitchen");
            openJoinPopupButton.onClick.AddListener(ShowJoinPopup);

            playTutorialButton = CreateButton("PlayTutorialBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90f, -38f), new Vector2(160f, 42f), "★ TUTORIAL ★", new Color(0.18f, 0.55f, 0.42f, 1f), out playTutorialButtonLabel, 12);
            playTutorialButton.onClick.AddListener(OnPlayTutorialClicked);

            openShopButton = CreateButton("OpenShopBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, -38f), new Vector2(160f, 42f), "★ UPGRADES ★", new Color(0.75f, 0.52f, 0.12f, 1f), out openShopButtonLabel, 12);
            openShopButton.onClick.AddListener(OnOpenShopClicked);

            openSettingsPopupButton = CreateButton("OpenSettingsBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90f, -88f), new Vector2(160f, 38f), "⚙️  SETTINGS", new Color(0.22f, 0.25f, 0.34f, 0.95f), out var settingsT, 12); Reg(settingsT, "menu.settings");
            openSettingsPopupButton.onClick.AddListener(ShowSettingsPopup);

            openStoryPopupButton = CreateButton("OpenStoryBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, -88f), new Vector2(160f, 38f), "📖  STORIES", new Color(0.42f, 0.25f, 0.52f, 1f), out openStoryButtonLabel, 12);
            openStoryPopupButton.onClick.AddListener(ShowStoryPopup);

            // Bottom bar on main menu
            statusText = CreateLabel("StatusText", mainMenuPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(680f, 20f), "Ready to play on Local Wi-Fi.", 11, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter);
            controlsGuideText = CreateLabel("ControlsGuide", mainMenuPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(680f, 20f), "[WASD] Walk  •  [E] Pick / Place  •  [F] Chop / Cook  •  [Q] Throw  •  [R] Recipes", 11, new Color(0.68f, 0.78f, 0.92f), TextAnchor.MiddleCenter);
            CreateLabel("VersionBadge", mainMenuPanel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-15f, 15f), new Vector2(120f, 18f), "v1.4.6", 11, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleRight);

            // -------------------------------------------------------------
            // SUB-PANEL B: HOST POPUP MODAL (Clean, asks for name & creates room)
            // -------------------------------------------------------------
            hostPopupPanel = new GameObject("HostPopupPanel", typeof(RectTransform));
            hostPopupPanel.transform.SetParent(mRoot, false);
            var hpRect = hostPopupPanel.GetComponent<RectTransform>();
            hpRect.anchorMin = Vector2.zero;
            hpRect.anchorMax = Vector2.one;
            hpRect.sizeDelta = Vector2.zero;

            var hpBg = hostPopupPanel.AddComponent<Image>();
            hpBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            Reg(CreateLabel("HostTitle", hostPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(600f, 26f), "HOST A NEW KITCHEN", 16, new Color(0.35f, 0.95f, 0.65f), TextAnchor.MiddleCenter), "menu.host_title");
            Reg(CreateLabel("HostDesc", hostPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 20f), "Choose your chef name and kitchen name. Your friend can join on the same Wi-Fi!", 11, new Color(0.75f, 0.82f, 0.90f), TextAnchor.MiddleCenter), "menu.host_desc");

            Reg(CreateLabel("NamePrompt", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 50f), new Vector2(220f, 20f), "Your Chef Name:", 12, Color.white, TextAnchor.MiddleRight), "menu.name_prompt");
            string savedHostName = PlayerPrefs.GetString("TwoLadles_PlayerName", "Chef Linh");
            nameInput = CreateInputField("NameInput", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 50f), new Vector2(200f, 32f), savedHostName, "Your Chef Name");

            Reg(CreateLabel("RoomPrompt", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 5f), new Vector2(220f, 20f), "Kitchen Room Name:", 12, Color.white, TextAnchor.MiddleRight), "menu.room_prompt");
            string savedRoomName = PlayerPrefs.GetString("TwoLadles_RoomName", "Two Ladles Bistro");
            roomNameInput = CreateInputField("RoomNameInput", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 5f), new Vector2(200f, 32f), savedRoomName, "Kitchen Name");

            hostButton = CreateButton("StartHostBtn", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(85f, -70f), new Vector2(170f, 44f), "START HOSTING", new Color(0.18f, 0.62f, 0.35f, 1f), out hostButtonLabel, 13);
            hostButton.onClick.AddListener(OnHostClicked);

            var cancelHostBtn = CreateButton("CancelHostBtn", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-85f, -70f), new Vector2(140f, 44f), "CANCEL", new Color(0.25f, 0.28f, 0.36f, 1f), out var cancelT, 12); Reg(cancelT, "menu.cancel");
            cancelHostBtn.onClick.AddListener(ShowMainMenu);

            hostPopupPanel.SetActive(false);

            // -------------------------------------------------------------
            // SUB-PANEL C: JOIN POPUP MODAL (Clean list of Wi-Fi rooms + direct IP)
            // -------------------------------------------------------------
            joinPopupPanel = new GameObject("JoinPopupPanel", typeof(RectTransform));
            joinPopupPanel.transform.SetParent(mRoot, false);
            var jpRect = joinPopupPanel.GetComponent<RectTransform>();
            jpRect.anchorMin = Vector2.zero;
            jpRect.anchorMax = Vector2.one;
            jpRect.sizeDelta = Vector2.zero;

            var jpBg = joinPopupPanel.AddComponent<Image>();
            jpBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            Reg(CreateLabel("JoinTitle", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(600f, 26f), "JOIN A KITCHEN", 16, new Color(0.35f, 0.75f, 0.98f), TextAnchor.MiddleCenter), "menu.join_title");

            // PRIMARY PATH: Automatic Wi-Fi kitchen scan
            lanHeaderLabel = CreateLabel("LanHeader", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(600f, 22f), "OPEN KITCHENS ON YOUR WI-FI (AUTO-SCAN):", 12, new Color(0.4f, 0.95f, 0.75f), TextAnchor.MiddleCenter);

            // Discovered LAN Room Slots (big, prominent, easy to tap)
            for (int i = 0; i < 2; i++)
            {
                int slotIdx = i;
                float yPos = -106f - (i * 48f);
                lanRoomButtons[i] = CreateButton($"LanRoomBtn_{i}", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(520f, 42f),
                    i == 0 ? Tr("menu.lan_scanning", "🔍 Scanning your Wi-Fi for open kitchens...") : Tr("menu.lan_refresh", "🔄 Tap to refresh Wi-Fi scan"),
                    new Color(0.14f, 0.18f, 0.24f, 0.95f), out lanRoomButtonLabels[i], 12);
                lanRoomButtons[i].onClick.AddListener(() => OnLanRoomSlotClicked(slotIdx));
            }

            joinStatusText = CreateLabel("JoinStatus", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -208f), new Vector2(680f, 24f), "", 11, new Color(0.85f, 0.95f, 1.0f), TextAnchor.MiddleCenter);

            // LAST RESORT: Manual IP entry
            Reg(CreateLabel("IpPrompt", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -242f), new Vector2(680f, 20f), "─── OR ENTER HOST IP MANUALLY (LAST RESORT) ───", 11, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleCenter), "menu.ip_prompt");

            string savedIp = PlayerPrefs.GetString("TwoLadles_LastJoinedIP", "");
            if (!NetworkManager.IsUsableLanAddress(savedIp))
            {
                savedIp = NetworkManager.GetSubnetPrefix(NetworkManager.Instance != null ? NetworkManager.Instance.LocalWifiIp : "");
            }
            ipInput = CreateInputField("IpInput", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-75f, -280f), new Vector2(260f, 38f), savedIp, "Host IP (e.g. 192.168.1.23)");
            ipInput.textComponent.fontSize = 18;
            ((Text)ipInput.placeholder).fontSize = 15;
            ipInput.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
            ipInput.characterLimit = 21;

            joinButton = CreateButton("DirectJoinBtn", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(140f, -280f), new Vector2(130f, 38f), "CONNECT", new Color(0.20f, 0.48f, 0.78f, 1f), out joinButtonLabel, 13);
            joinButton.onClick.AddListener(OnJoinClicked);

            var cancelJoinBtn = CreateButton("CancelJoinBtn", joinPopupPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(160f, 38f), "BACK", new Color(0.25f, 0.28f, 0.36f, 1f), out var backT, 12); Reg(backT, "menu.back");
            cancelJoinBtn.onClick.AddListener(ShowMainMenu);

            joinPopupPanel.SetActive(false);

            // -------------------------------------------------------------
            // SUB-PANEL D: ROOM WAITING & LEVEL SELECTION (When in room)
            // -------------------------------------------------------------
            roomPanel = new GameObject("RoomPanel", typeof(RectTransform));
            roomPanel.transform.SetParent(mRoot, false);
            var rpRect = roomPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = Vector2.zero;
            rpRect.anchorMax = Vector2.one;
            rpRect.sizeDelta = Vector2.zero;

            playersListText = CreateLabel("PlayersList", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(680f, 22f), "Room Chefs: Linh (West) & Jiwon (East)", 12, new Color(0.4f, 0.95f, 0.75f), TextAnchor.MiddleCenter);

            hostIpLabel = CreateLabel("HostIpLabel", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(700f, 28f), "", 18, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter);
            hostIpLabel.fontStyle = FontStyle.Bold;

            levelSelectHeaderLabel = CreateLabel("LevelSelectHeader", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(680f, 18f), "SELECT LEVEL TO PLAY:", 11, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleCenter);

            string[] lvlShortNames = new string[] { "Tteokbokki", "+ Bột Chiên", "+ K-BBQ Rice", "+ Broken Rice", "3m Banh Mi" };
            for (int i = 0; i < 5; i++)
            {
                int lvlId = i + 1;
                float xPos = -272f + (i * 136f);
                bool defaultUnlocked = (lvlId == 1);
                string defaultTag = defaultUnlocked ? "UNLOCKED" : "LOCKED";
                Color defaultCol = defaultUnlocked ? new Color(0.88f, 0.55f, 0.15f, 1f) : new Color(0.16f, 0.16f, 0.18f, 0.85f);
                levelSelectButtons[i] = CreateButton($"LevelBtn_{lvlId}", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(xPos, -146f), new Vector2(128f, 54f),
                    $"DAY {lvlId}\n{lvlShortNames[i]}\n[{defaultTag}]",
                    defaultCol, out levelSelectLabels[i], 11);
                levelSelectBgs[i] = levelSelectButtons[i].GetComponent<Image>();
                levelSelectButtons[i].onClick.AddListener(() => SelectCampaignLevel(lvlId));
            }

            endlessModeButton = CreateButton("EndlessBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(240f, 34f), "∞ ENDLESS SURVIVAL MODE", new Color(0.48f, 0.25f, 0.58f, 1f), out endlessModeButtonLabel, 11);
            endlessModeButton.onClick.AddListener(OnSelectEndlessClicked);

            // Chef switch: Linh -> Jiwon -> placeholder (???) -> Linh. Placeholder can never Ready.
            chefCycleButton = CreateButton("ChefCycleBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-250f, -210f), new Vector2(160f, 34f), "CHEF: LINH ⟳", new Color(0.22f, 0.5f, 0.45f, 1f), out chefCycleText, 11);
            chefCycleButton.onClick.AddListener(OnChefCycleClicked);

            // Room Action Row: Ready, Start Level, Leave
            readyButton = CreateButton("ReadyBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-210f, -252f), new Vector2(150f, 44f), "READY: NO", new Color(0.25f, 0.45f, 0.68f, 1f), out readyButtonText, 12);
            readyButton.onClick.AddListener(OnReadyClicked);

            startShiftButton = CreateButton("StartShiftBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(15f, -252f), new Vector2(260f, 44f), "▶ START LEVEL 1 (3 WAVES)", new Color(0.88f, 0.42f, 0.14f, 1f), out startShiftButtonText, 13);
            startShiftButton.onClick.AddListener(OnStartShiftClicked);

            leaveRoomButton = CreateButton("LeaveRoomBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(215f, -252f), new Vector2(110f, 44f), "LEAVE", new Color(0.68f, 0.22f, 0.25f, 1f), out var leaveT, 12); Reg(leaveT, "menu.leave");
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

            roomStatusText = CreateLabel("RoomStatus", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -312f), new Vector2(680f, 36f), "", 12, new Color(0.85f, 0.95f, 1.0f), TextAnchor.MiddleCenter);

            roomPanel.SetActive(false);

            // -------------------------------------------------------------
            // SUB-PANEL E: SETTINGS & EXTRAS MODAL
            // -------------------------------------------------------------
            settingsPopupPanel = new GameObject("SettingsPopupPanel", typeof(RectTransform));
            settingsPopupPanel.transform.SetParent(mRoot, false);
            var spRect = settingsPopupPanel.GetComponent<RectTransform>();
            spRect.anchorMin = Vector2.zero;
            spRect.anchorMax = Vector2.one;
            spRect.sizeDelta = Vector2.zero;

            var spBg = settingsPopupPanel.AddComponent<Image>();
            spBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            Reg(CreateLabel("SettingsTitle", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(600f, 26f), "SETTINGS & OPTIONS", 16, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleCenter), "menu.settings_title");

            // Language Selection
            Reg(CreateLabel("LangHeader", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(600f, 20f), "Display Language:", 12, Color.white, TextAnchor.MiddleCenter), "menu.lang_header");
            langEnButton = CreateButton("LangEnBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -135f), new Vector2(100f, 32f), "English", new Color(0.15f, 0.18f, 0.24f, 0.9f), out langEnText, 11);
            langEnButton.onClick.AddListener(() => SetAppLanguage(LocalizationManager.LANG_EN));

            langViButton = CreateButton("LangViBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(100f, 32f), "Tiếng Việt", new Color(0.15f, 0.18f, 0.24f, 0.9f), out langViText, 11);
            langViButton.onClick.AddListener(() => SetAppLanguage(LocalizationManager.LANG_VI));

            langKoButton = CreateButton("LangKoBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110f, -135f), new Vector2(100f, 32f), "한국어", new Color(0.15f, 0.18f, 0.24f, 0.9f), out langKoText, 11);
            langKoButton.onClick.AddListener(() => SetAppLanguage(LocalizationManager.LANG_KO));

            // Solo / AI Mode Toggle
            Reg(CreateLabel("ModeHeader", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(600f, 20f), "Cooperative or Solo Mode:", 12, Color.white, TextAnchor.MiddleCenter), "menu.mode_header");
            modeToggleButton = CreateButton("ModeToggleBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-95f, -225f), new Vector2(170f, 34f), "MODE: LAN CO-OP", new Color(0.18f, 0.35f, 0.55f, 0.95f), out modeToggleText, 11);
            modeToggleButton.onClick.AddListener(OnToggleModeClicked);

            chefSelectButton = CreateButton("ChefSelectBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(95f, -225f), new Vector2(180f, 34f), "YOU: LINH (AI: JIWON)", new Color(0.24f, 0.28f, 0.38f, 0.95f), out chefSelectText, 11);
            chefSelectButton.onClick.AddListener(OnToggleChefSelectClicked);
            chefSelectButton.gameObject.SetActive(isSoloMode);

            // UI / HUD size: [-] 100% [+]. Saved and applied to every canvas immediately.
            float uiRowY = -280f;
            uiSizeText = CreateLabel("UISizeLabel", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, uiRowY), new Vector2(200f, 24f), UISizeLabel(), 12, Color.white, TextAnchor.MiddleCenter);
            var uiMinusBtn = CreateButton("UISizeMinusBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-140f, uiRowY), new Vector2(50f, 32f), "−", new Color(0.15f, 0.18f, 0.24f, 0.9f), out _, 14);
            uiMinusBtn.onClick.AddListener(() => { UIScaleSettings.Step(-1); uiSizeText.text = UISizeLabel(); });
            var uiPlusBtn = CreateButton("UISizePlusBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(140f, uiRowY), new Vector2(50f, 32f), "+", new Color(0.15f, 0.18f, 0.24f, 0.9f), out _, 14);
            uiPlusBtn.onClick.AddListener(() => { UIScaleSettings.Step(1); uiSizeText.text = UISizeLabel(); });

            // 2-Bot QA Test Button: dev tool, lives in Settings (desktop and mobile; report shown on screen).
            twoBotTestButton = CreateButton("TwoBotTestBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -325f), new Vector2(220f, 36f), "🤖 2-BOT TEST RUN", new Color(0.18f, 0.50f, 0.64f, 0.75f), out twoBotTestButtonText, 11);
            twoBotTestButton.onClick.AddListener(OnTwoBotTestClicked);

            // QA options: which day(s) the bots play, and whether they get every upgrade (temporary, never saved).
            qaDayButton = CreateButton("QADayBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -370f), new Vector2(200f, 32f), QADayLabel(), new Color(0.15f, 0.18f, 0.24f, 0.9f), out qaDayText, 11);
            qaDayButton.onClick.AddListener(() => { qaDayOption = (qaDayOption + 1) % 6; qaDayText.text = QADayLabel(); });

            qaUpgradesButton = CreateButton("QAUpgradesBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110f, -370f), new Vector2(200f, 32f), QAUpgradesLabel(), new Color(0.15f, 0.18f, 0.24f, 0.9f), out qaUpgradesText, 11);
            qaUpgradesButton.onClick.AddListener(() => { qaAllUpgrades = !qaAllUpgrades; qaUpgradesText.text = QAUpgradesLabel(); });

            var closeSettingsBtn = CreateButton("CloseSettingsBtn", settingsPopupPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(150f, 36f), "CLOSE", new Color(0.25f, 0.28f, 0.36f, 1f), out var closeSettingsT, 12); Reg(closeSettingsT, "menu.close");
            closeSettingsBtn.onClick.AddListener(ShowMainMenu);

            settingsPopupPanel.SetActive(false);

            // -------------------------------------------------------------
            // SUB-PANEL F: STORY & CUTSCENES MODAL
            // -------------------------------------------------------------
            storyPopupPanel = new GameObject("StoryPopupPanel", typeof(RectTransform));
            storyPopupPanel.transform.SetParent(mRoot, false);
            var stRect = storyPopupPanel.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;

            var stBg = storyPopupPanel.AddComponent<Image>();
            stBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            Reg(CreateLabel("StoryTitle", storyPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(600f, 26f), "STORY & CUTSCENES", 16, new Color(0.95f, 0.75f, 0.35f), TextAnchor.MiddleCenter), "story.title");
            Reg(CreateLabel("StoryDesc", storyPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(600f, 20f), "Replay chef conversations, text messages, and recipe intros:", 11, new Color(0.75f, 0.82f, 0.90f), TextAnchor.MiddleCenter), "story.desc");

            var openingBtn = CreateButton("StoryOpeningBtn", storyPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(360f, 34f), "✨ OPENING: FIRST ENCOUNTER", new Color(0.72f, 0.28f, 0.45f, 1f), out var openingT, 11); Reg(openingT, "story.opening");
            openingBtn.onClick.AddListener(() =>
            {
                PlayerPrefs.DeleteKey(PhoneStoryUI.OPENING_SEEN_PREF);
                PlayStoryFromMenu(1);
            });

            string[] dayTitles = new string[] {
                "Day 1: Tteokbokki — First Shift",
                "Day 2: Bột Chiên — Saigon Heat",
                "Day 3: K-BBQ Rice — The Rush Begins",
                "Day 4: Cơm Tấm Broken Rice — Cross-Town Buzz",
                "Day 5: Bulgogi Bánh Mì — Street Fusion"
            };

            for (int i = 0; i < 5; i++)
            {
                int dayNum = i + 1;
                float yPos = -152f - (i * 38f);
                var dayBtn = CreateButton($"StoryDayBtn_{dayNum}", storyPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(360f, 32f), $"📖  {dayTitles[i]}", new Color(0.18f, 0.25f, 0.38f, 0.95f), out var dayT, 11); Reg(dayT, $"story.day{dayNum}", "📖  ");
                dayBtn.onClick.AddListener(() =>
                {
                    PlayStoryFromMenu(dayNum);
                });
            }

            var closeStoryBtn = CreateButton("CloseStoryBtn", storyPopupPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(150f, 36f), "CLOSE", new Color(0.25f, 0.28f, 0.36f, 1f), out var closeStoryT, 12); Reg(closeStoryT, "menu.close");
            closeStoryBtn.onClick.AddListener(ShowMainMenu);

            storyPopupPanel.SetActive(false);

            RefreshProgressionDisplay();
            RefreshLocalizedText();
        }

        // Sub-panel navigation helpers
        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
            if (storyPopupPanel != null) storyPopupPanel.SetActive(false);
        }

        public void ShowHostPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(true);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
            if (storyPopupPanel != null) storyPopupPanel.SetActive(false);
        }

        public void ShowJoinPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(true);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
            if (storyPopupPanel != null) storyPopupPanel.SetActive(false);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.ScanWifiNow();
                string myIp = NetworkManager.Instance.LocalWifiIp;
                if (NetworkManager.IsUsableLanAddress(myIp))
                    UpdateStatus($"Scanning your Wi-Fi ({NetworkManager.GetSubnetPrefix(myIp)}x) for kitchens...", false);
                else
                    UpdateStatus("Not connected to Wi-Fi. Join the same Wi-Fi as the host.", true);
            }
        }

        public void ShowRoomPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(true);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
            if (storyPopupPanel != null) storyPopupPanel.SetActive(false);

            RefreshProgressionDisplay();
            if (NetworkManager.Instance != null)
            {
                RefreshLobbyDisplay(NetworkManager.Instance.players);
            }
        }

        public void ShowSettingsPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(true);
            if (storyPopupPanel != null) storyPopupPanel.SetActive(false);
        }

        /// <summary>Plays a story replay over the menu, then brings the Stories list back when it closes.</summary>
        private void PlayStoryFromMenu(int day)
        {
            if (PhoneStoryUI.IsShowing) return;
            if (modalPanel != null) modalPanel.SetActive(false);
            PhoneStoryUI.Show(day, () =>
            {
                if (modalPanel != null) modalPanel.SetActive(true);
                ShowStoryPopup();
            }, false);
        }

        public void ShowStoryPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
            if (storyPopupPanel != null) storyPopupPanel.SetActive(true);
        }

        private void Update()
        {
            // Esc belongs to the story phone (skip) while it is open; don't also toggle the menu.
            if (!PhoneStoryUI.IsShowing && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M)))
            {
                ToggleMenuVisibility();
            }
        }

        // Static labels that only need a translation lookup when the language changes.
        private readonly List<(Text text, string key, string prefix)> staticLocTexts = new List<(Text, string, string)>();
        private Text Reg(Text t, string key, string prefix = "")
        {
            if (t != null) staticLocTexts.Add((t, key, prefix));
            return t;
        }

        private static string Tr(string key, string fallback)
        {
            var lm = LocalizationManager.Instance;
            return lm != null ? lm.Get(key, fallback) : fallback;
        }

        public void ToggleMenuVisibility()
        {
            if (modalPanel != null)
            {
                bool newState = !modalPanel.activeSelf;
                modalPanel.SetActive(newState);
                if (toggleMenuButtonText != null)
                {
                    toggleMenuButtonText.text = newState ? Tr("menu.hide_menu", "Hide Menu [ESC]") : Tr("menu.show_menu", "Show Menu [ESC]");
                }
                if (newState)
                {
                    RefreshProgressionDisplay();
                }
            }
        }

        private void Start()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectionStatusChanged += UpdateStatus;
                NetworkManager.Instance.OnLobbyUpdated += RefreshLobbyDisplay;
                NetworkManager.Instance.OnLanRoomsUpdated += RefreshLanRoomsDisplay;
                NetworkManager.Instance.OnShiftStartReceived += (stage, mode) => gameObject.SetActive(false);
                NetworkManager.Instance.OnReturnedToMenu += OnReturnedToMenuHandler;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += RefreshLocalizedText;
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
            UpdateStatus($"Selected Level {lvlId}. Click START to play!", false);
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
                // Close the menu window; the shop's close button brings the main menu back.
                if (modalPanel != null) modalPanel.SetActive(false);
                hud.OpenUpgradeShopModal();
            }
        }

        private void OnHostClicked()
        {
            int port = NetworkManager.DEFAULT_PORT;
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Linh";
            string rName = roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text) ? roomNameInput.text : $"{pName}'s Kitchen";

            PlayerPrefs.SetString("TwoLadles_PlayerName", pName);
            PlayerPrefs.SetString("TwoLadles_RoomName", rName);
            PlayerPrefs.Save();

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.HostGame(port, pName, rName);
            }
            ShowRoomPanel();
        }

        private void OnJoinClicked()
        {
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Jiwon";
            string typedIp = NetworkManager.NormalizeTypedIp(ipInput != null ? ipInput.text : "");
            string targetIp = typedIp;
            if (targetIp.Length == 0 && currentDiscoveredRooms.Count > 0)
            {
                targetIp = currentDiscoveredRooms[0].hostIp;
            }
            if (targetIp.Length == 0)
            {
                UpdateStatus("Type the full IP from the host's screen (4 numbers with dots, e.g. 192.168.1.23).", true);
                return;
            }
            if (ipInput != null && typedIp.Length > 0) ipInput.text = typedIp;

            PlayerPrefs.SetString("TwoLadles_PlayerName_Join", pName);
            PlayerPrefs.Save();

            if (NetworkManager.Instance == null) return;
            if (NetworkManager.Instance.JoinGame(targetIp, NetworkManager.DEFAULT_PORT, pName))
            {
                PlayerPrefs.SetString("TwoLadles_LastJoinedIP", targetIp);
                PlayerPrefs.Save();
                ShowRoomPanel();
            }
        }

        private void OnLanRoomSlotClicked(int slotIdx)
        {
            if (NetworkManager.Instance == null) return;
            string pName = nameInput != null && !string.IsNullOrEmpty(nameInput.text) ? nameInput.text : "Chef Jiwon";
            if (slotIdx >= 0 && slotIdx < currentDiscoveredRooms.Count)
            {
                NetworkManager.Instance.JoinDiscoveredRoom(currentDiscoveredRooms[slotIdx], pName);
                ShowRoomPanel();
            }
            else
            {
                NetworkManager.Instance.ScanWifiNow();
                UpdateStatus("Scanning your Wi-Fi for open kitchens...", false);
            }
        }

        private void OnLeaveRoomClicked()
        {
            if (NetworkManager.Instance != null)
            {
                // Tells partner we left (DISCONNECT packet), then closes socket.
                NetworkManager.Instance.ReturnToMenu("Partner left the room.");
            }
            ShowMainMenu();
            UpdateStatus("Returned to main menu.", false);
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
                    lanRoomButtonLabels[i].text = $"🍲 JOIN: {r.roomName} ({r.hostName}) [{r.playerCount}/2] — {r.hostIp}";
                    lanRoomButtons[i].GetComponent<Image>().color = new Color(0.16f, 0.62f, 0.35f, 1f);
                }
                else
                {
                    lanRoomButtonLabels[i].text = i == 0 ? Tr("menu.lan_scanning", "🔍 Scanning your Wi-Fi for open kitchens...") : Tr("menu.lan_refresh", "🔄 Tap to refresh Wi-Fi scan");
                    lanRoomButtons[i].GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.24f, 0.95f);
                }
            }

            if (currentDiscoveredRooms.Count > 0)
            {
                if (ipInput != null && !NetworkManager.IsUsableLanAddress((ipInput.text ?? "").Trim()))
                {
                    ipInput.text = currentDiscoveredRooms[0].hostIp;
                }
                if (joinStatusText != null)
                {
                    joinStatusText.text = $"Found kitchen: {currentDiscoveredRooms[0].roomName} ({currentDiscoveredRooms[0].hostIp}). Tap button above to join!";
                    joinStatusText.color = new Color(0.35f, 0.95f, 0.65f);
                }
            }
        }

        private void OnReadyClicked()
        {
            if (NetworkManager.Instance == null) return;
            int localId = NetworkManager.Instance.localPeerId;
            if (NetworkManager.Instance.players.TryGetValue(localId, out var p))
            {
                var res = NetworkManager.Instance.SetPlayerReady(localId, !p.ready);
                if (!res.ok) UpdateStatus(res.reason, true);
            }
        }

        private Button chefCycleButton;
        private Text chefCycleText;

        /// <summary>Cycles local chef Linh -> Jiwon -> placeholder, skipping a chef the partner already holds.</summary>
        private void OnChefCycleClicked()
        {
            var nm = NetworkManager.Instance;
            if (nm == null) return;
            int localId = nm.localPeerId;
            if (!nm.players.TryGetValue(localId, out var p)) return;

            string[] order = { "linh", "jiwon", NetworkManager.PLACEHOLDER_CHAR };
            int idx = System.Array.IndexOf(order, p.character);
            for (int step = 1; step <= order.Length; step++)
            {
                string next = order[(idx + step + order.Length) % order.Length];
                var res = nm.ApplyPlayerSelection(localId, next, p.specialization);
                if (res.ok) return;
            }
            UpdateStatus("Could not switch chef.", true);
        }

        private static string ChefDisplayName(string characterId)
        {
            var lm = LocalizationManager.Instance;
            if (characterId == NetworkManager.PLACEHOLDER_CHAR) return lm != null ? lm.Get("char.placeholder", "???") : "???";
            string key = "char.short_" + characterId;
            return lm != null ? lm.Get(key, characterId.ToUpper()) : characterId.ToUpper();
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

            // LAN co-op gate: 2 chefs in room, both READY, only host starts.
            if (!isSoloMode && NetworkManager.Instance != null && NetworkManager.Instance.isLanActive)
            {
                if (!NetworkManager.Instance.IsHost())
                {
                    UpdateStatus("Waiting for the host to start the level...", false);
                    return;
                }
                var gate = NetworkManager.Instance.CanStartShift(true);
                if (!gate.ok)
                {
                    UpdateStatus(NetworkManager.Instance.GetLobbyWaitReason(), true);
                    return;
                }
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
                ShiftManager.Instance.isSoloMode = isSoloMode;
                ShiftManager.Instance.humanChefPeerId = humanChefPeerId;
                ShiftManager.Instance.StartShift(stage, mode);
            }
            gameObject.SetActive(false);
        }

        // QA test options (Settings popup)
        private Button qaDayButton;
        private Text qaDayText;
        private Button qaUpgradesButton;
        private Text qaUpgradesText;
        private int qaDayOption = 0; // 0 = All days, 1-5 = single day
        private bool qaAllUpgrades = true;
        private string QADayLabel() => qaDayOption == 0 ? "TEST DAY: ALL (1-5)" : "TEST DAY: " + qaDayOption;
        private string QAUpgradesLabel() => qaAllUpgrades ? "ALL UPGRADES: ON" : "ALL UPGRADES: OFF";

        // UI / HUD size option (Settings popup)
        private Text uiSizeText;
        private static string UISizeLabel()
        {
            int pct = Mathf.RoundToInt(UIScaleSettings.Get() * 100f);
            var lm = LocalizationManager.Instance;
            string fmt = lm != null ? lm.Get("menu.ui_size", "UI SIZE: {0}%") : "UI SIZE: {0}%";
            try { return string.Format(fmt, pct); } catch (FormatException) { return $"UI SIZE: {pct}%"; }
        }

        private void OnTwoBotTestClicked()
        {
            if (TwoLadles.AI.TwoBotQAManager.Instance == null)
            {
                var qaObj = new GameObject("TwoBotQAManager");
                qaObj.AddComponent<TwoLadles.AI.TwoBotQAManager>();
            }

            var range = TwoLadles.AI.TwoBotQAManager.ResolveDayRange(qaDayOption);
            TwoLadles.AI.TwoBotQAManager.Instance.qaAllUpgrades = qaAllUpgrades;
            TwoLadles.AI.TwoBotQAManager.Instance.StartAutoplayRun(range.start, range.end, UnityEngine.Random.Range(10, 9999), 2.0f);
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
            Color c = isError ? new Color(1.0f, 0.4f, 0.4f) : new Color(0.85f, 0.95f, 1.0f);
            foreach (var t in new[] { statusText, joinStatusText, roomStatusText })
            {
                if (t == null) continue;
                t.text = message;
                t.color = c;
            }
        }

        private void RefreshLobbyDisplay(Dictionary<int, PlayerLobbyInfo> players)
        {
            var lm = LocalizationManager.Instance;
            string L(string key, string fb) => lm != null ? lm.Get(key, fb) : fb;

            if (playersListText != null)
            {
                if (players == null || players.Count == 0)
                {
                    playersListText.text = L("menu.room_chefs_default", "Room Chefs: Linh (West) & Jiwon (East)");
                }
                else
                {
                    var parts = new List<string>();
                    foreach (var p in players.Values)
                    {
                        string readyMark = p.ready ? L("menu.ready_mark", "READY ✓") : L("menu.waiting_mark", "WAITING...");
                        parts.Add($"{p.name} ({ChefDisplayName(p.character)}): [{readyMark}]");
                    }
                    playersListText.text = L("menu.chefs_in_kitchen", "Chefs in Kitchen: ") + string.Join("  •  ", parts);
                }
            }

            bool lanRoom = !isSoloMode && NetworkManager.Instance != null && NetworkManager.Instance.isLanActive;
            if (chefCycleButton != null) chefCycleButton.gameObject.SetActive(lanRoom);

            if (NetworkManager.Instance != null && NetworkManager.Instance.players.TryGetValue(NetworkManager.Instance.localPeerId, out var localP))
            {
                bool canReady = NetworkManager.CanReadyAs(localP.character);
                if (readyButton != null) readyButton.interactable = canReady;
                if (readyButtonText != null)
                {
                    if (!canReady) readyButtonText.text = L("menu.pick_chef", "PICK A CHEF");
                    else readyButtonText.text = localP.ready ? L("menu.ready_yes", "READY: YES") + " ✓" : L("menu.ready_no", "READY: NO");
                }
                if (chefCycleText != null)
                {
                    string name = ChefDisplayName(localP.character);
                    chefCycleText.text = lm != null ? lm.Format("menu.chef_cycle", name) : $"CHEF: {name} ⟳";
                }
            }

            var nm = NetworkManager.Instance;
            if (nm != null)
            {
                bool lanCoop = !isSoloMode && nm.isLanActive;
                if (startShiftButton != null)
                {
                    startShiftButton.interactable = !lanCoop || (nm.IsHost() && nm.CanStartShift(true).ok);
                }
                if (hostIpLabel != null)
                {
                    string ipText = "";
                    if (lanCoop && nm.IsHost())
                    {
                        var ips = NetworkManager.GetAllUsableLanIPv4();
                        if (ips.Count == 0) ipText = "No Wi-Fi IP found - connect this device to Wi-Fi";
                        else if (ips.Count == 1) ipText = $"YOUR IP: {ips[0]}   (partner types this in JOIN)";
                        else ipText = $"YOUR IP: {ips[0]}   (or {string.Join(" / ", ips.GetRange(1, Mathf.Min(2, ips.Count - 1)))})";
                    }
                    hostIpLabel.text = ipText;
                }
                if (lanCoop && players != null && players.Count > 0)
                {
                    string reason = nm.GetLobbyWaitReason();
                    if (!string.IsNullOrEmpty(reason)) UpdateStatus(reason, false);
                    else if (nm.IsHost()) UpdateStatus("Both chefs READY! Press START.", false);
                    else UpdateStatus("Both chefs READY! Waiting for host to start...", false);
                }
            }
        }

        public void SetAppLanguage(string langCode)
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(langCode);
            }
        }

        private void OnToggleModeClicked()
        {
            isSoloMode = !isSoloMode;
            if (chefSelectButton != null)
            {
                chefSelectButton.gameObject.SetActive(isSoloMode);
            }
            RefreshLocalizedText();
            if (NetworkManager.Instance != null) RefreshLobbyDisplay(NetworkManager.Instance.players);
        }

        private void OnToggleChefSelectClicked()
        {
            humanChefPeerId = (humanChefPeerId == 1) ? 2 : 1;
            RefreshLocalizedText();
        }

        public void RefreshLocalizedText()
        {
            if (LocalizationManager.Instance == null) return;
            var lm = LocalizationManager.Instance;

            if (titleLabel != null) titleLabel.text = lm.Get("menu.title");
            if (lanHeaderLabel != null) lanHeaderLabel.text = lm.Get("menu.lan_header", "OPEN KITCHENS ON YOUR WI-FI (AUTO-SCAN):");
            if (hostButtonLabel != null) hostButtonLabel.text = lm.Get("menu.start_hosting", "START HOSTING");
            if (joinButtonLabel != null) joinButtonLabel.text = lm.Get("menu.connect", "CONNECT");
            if (levelSelectHeaderLabel != null) levelSelectHeaderLabel.text = lm.Get("menu.level_select");
            if (playTutorialButtonLabel != null) playTutorialButtonLabel.text = lm.Get("menu.play_tutorial");
            if (endlessModeButtonLabel != null) endlessModeButtonLabel.text = lm.Get("menu.endless_mode");
            if (openShopButtonLabel != null) openShopButtonLabel.text = lm.Get("menu.upgrade_shop");
            if (openStoryButtonLabel != null) openStoryButtonLabel.text = lm.Get("menu.stories", "📖  STORIES");
            if (twoBotTestButtonText != null) twoBotTestButtonText.text = lm.Get("menu.two_bot_test");
            if (uiSizeText != null) uiSizeText.text = UISizeLabel();
            if (controlsGuideText != null) controlsGuideText.text = lm.Get("menu.controls_guide");
            if (toggleMenuButtonText != null)
            {
                bool isVisible = (modalPanel != null && modalPanel.activeSelf);
                toggleMenuButtonText.text = isVisible ? lm.Get("menu.hide_menu") : lm.Get("menu.show_menu");
            }

            if (modeToggleText != null)
            {
                modeToggleText.text = isSoloMode ? lm.Get("menu.mode_solo") : lm.Get("menu.mode_coop");
            }

            if (chefSelectText != null)
            {
                chefSelectText.text = (humanChefPeerId == 1) ? lm.Get("menu.human_linh") : lm.Get("menu.human_jiwon");
            }

            foreach (var e in staticLocTexts)
            {
                if (e.text != null) e.text.text = e.prefix + lm.Get(e.key, e.text.text);
            }
            if (playersListText != null && NetworkManager.Instance != null) RefreshLobbyDisplay(NetworkManager.Instance.players);

            string curLang = lm.CurrentLanguage;
            UpdateLangBtnHighlight(langEnButton, langEnText, curLang == LocalizationManager.LANG_EN);
            UpdateLangBtnHighlight(langViButton, langViText, curLang == LocalizationManager.LANG_VI);
            UpdateLangBtnHighlight(langKoButton, langKoText, curLang == LocalizationManager.LANG_KO);

            RefreshProgressionDisplay();
        }

        private void UpdateLangBtnHighlight(Button btn, Text txt, bool active)
        {
            if (btn == null || txt == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = active ? new Color(0.88f, 0.55f, 0.15f, 1f) : new Color(0.15f, 0.18f, 0.24f, 0.9f);
            }
            txt.color = active ? Color.white : new Color(0.7f, 0.75f, 0.85f, 1f);
            txt.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
        }

        public void RefreshProgressionDisplay()
        {
            if (ProgressionManager.Instance == null) return;
            var lm = LocalizationManager.Instance;

            if (coinsText != null)
                coinsText.text = lm != null ? lm.Format("menu.gold", ProgressionManager.Instance.gold) : $"GOLD: {ProgressionManager.Instance.gold}g";

            if (careerScoreText != null)
                careerScoreText.text = lm != null ? lm.Format("menu.score", ProgressionManager.Instance.totalCareerScore) : $"SCORE: {ProgressionManager.Instance.totalCareerScore}";

            string[] lvlNames = new string[] { "Tteokbokki", "+ Bột Chiên", "+ K-BBQ Rice", "+ Broken Rice", "3m Banh Mi" };
            for (int n = 0; n < lvlNames.Length; n++) lvlNames[n] = Tr($"menu.lvl{n + 1}", lvlNames[n]);
            for (int i = 0; i < 5; i++)
            {
                int lvlId = i + 1;
                if (levelSelectLabels[i] == null || levelSelectBgs[i] == null) continue;

                bool unlocked = ProgressionManager.Instance.IsLevelUnlocked(lvlId);
                bool completed = ProgressionManager.Instance.IsLevelCompleted(lvlId);
                bool isSelected = (!isEndlessSelected && selectedLevelId == lvlId);

                string statusTag = completed ? Tr("menu.completed", "★ COMPLETED ★") : (unlocked ? (lm != null ? lm.Get("menu.unlocked") : "UNLOCKED") : (lm != null ? lm.Get("menu.locked") : "LOCKED"));
                string dayLabel = lm != null ? lm.Format("menu.day_n", lvlId) : $"DAY {lvlId}";
                levelSelectLabels[i].text = $"{dayLabel}\n{lvlNames[i]}\n[{statusTag}]";

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
                    startShiftButtonText.text = lm != null ? lm.Get("menu.start_endless") : "▶ START ENDLESS RUSH";
                }
                else
                {
                    int waves = (selectedLevelId == 5) ? 1 : 3;
                    startShiftButtonText.text = lm != null ? lm.Format("menu.start_level", selectedLevelId, waves) : $"▶ START LEVEL {selectedLevelId} ({waves} WAVES)";
                }
            }
        }

        private void OnReturnedToMenuHandler(string reason)
        {
            gameObject.SetActive(true);
            if (modalPanel != null) modalPanel.SetActive(true);
            ShowMainMenu();
            RefreshProgressionDisplay();
            RefreshLocalizedText();
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
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshLocalizedText;
            }
        }
    }
}
