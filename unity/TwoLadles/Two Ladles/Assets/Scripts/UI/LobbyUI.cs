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

        public Button openHostPopupButton;
        public Button openJoinPopupButton;
        public Button openSettingsPopupButton;
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

            CreateLabel("WelcomeSub", mainMenuPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(680f, 20f), "Authentic Street Food Cooperative Cooking", 12, new Color(0.75f, 0.82f, 0.92f), TextAnchor.MiddleCenter);

            // Big Action Buttons for Non-Tech Players
            openHostPopupButton = CreateButton("OpenHostBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 85f), new Vector2(340f, 48f), "👨‍🍳  HOST A KITCHEN", new Color(0.18f, 0.62f, 0.35f, 1f), out _, 14);
            openHostPopupButton.onClick.AddListener(ShowHostPopup);

            openJoinPopupButton = CreateButton("OpenJoinBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(340f, 48f), "🍲  JOIN KITCHEN (LAN)", new Color(0.20f, 0.48f, 0.78f, 1f), out _, 14);
            openJoinPopupButton.onClick.AddListener(ShowJoinPopup);

            playTutorialButton = CreateButton("PlayTutorialBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90f, -38f), new Vector2(160f, 42f), "★ TUTORIAL ★", new Color(0.18f, 0.55f, 0.42f, 1f), out playTutorialButtonLabel, 12);
            playTutorialButton.onClick.AddListener(OnPlayTutorialClicked);

            openShopButton = CreateButton("OpenShopBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, -38f), new Vector2(160f, 42f), "★ UPGRADES ★", new Color(0.75f, 0.52f, 0.12f, 1f), out openShopButtonLabel, 12);
            openShopButton.onClick.AddListener(OnOpenShopClicked);

            openSettingsPopupButton = CreateButton("OpenSettingsBtn", mainMenuPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(340f, 38f), "⚙️  SETTINGS & OPTIONS", new Color(0.22f, 0.25f, 0.34f, 0.95f), out _, 12);
            openSettingsPopupButton.onClick.AddListener(ShowSettingsPopup);

            // Bottom bar on main menu
            statusText = CreateLabel("StatusText", mainMenuPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(680f, 20f), "Ready to play on Local Wi-Fi.", 11, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter);
            controlsGuideText = CreateLabel("ControlsGuide", mainMenuPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(680f, 20f), "[WASD] Walk  •  [E] Pick / Place  •  [F] Chop / Cook  •  [Q] Throw  •  [R] Recipes", 11, new Color(0.68f, 0.78f, 0.92f), TextAnchor.MiddleCenter);

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

            CreateLabel("HostTitle", hostPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(600f, 26f), "HOST A NEW KITCHEN", 16, new Color(0.35f, 0.95f, 0.65f), TextAnchor.MiddleCenter);
            CreateLabel("HostDesc", hostPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 20f), "Choose your chef name and kitchen name. Your friend can join on the same Wi-Fi!", 11, new Color(0.75f, 0.82f, 0.90f), TextAnchor.MiddleCenter);

            CreateLabel("NamePrompt", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 50f), new Vector2(220f, 20f), "Your Chef Name:", 12, Color.white, TextAnchor.MiddleRight);
            string savedHostName = PlayerPrefs.GetString("TwoLadles_PlayerName", "Chef Linh");
            nameInput = CreateInputField("NameInput", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 50f), new Vector2(200f, 32f), savedHostName, "Your Chef Name");

            CreateLabel("RoomPrompt", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 5f), new Vector2(220f, 20f), "Kitchen Room Name:", 12, Color.white, TextAnchor.MiddleRight);
            string savedRoomName = PlayerPrefs.GetString("TwoLadles_RoomName", "Two Ladles Bistro");
            roomNameInput = CreateInputField("RoomNameInput", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 5f), new Vector2(200f, 32f), savedRoomName, "Kitchen Name");

            hostButton = CreateButton("StartHostBtn", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(85f, -70f), new Vector2(170f, 44f), "START HOSTING", new Color(0.18f, 0.62f, 0.35f, 1f), out hostButtonLabel, 13);
            hostButton.onClick.AddListener(OnHostClicked);

            var cancelHostBtn = CreateButton("CancelHostBtn", hostPopupPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-85f, -70f), new Vector2(140f, 44f), "CANCEL", new Color(0.25f, 0.28f, 0.36f, 1f), out _, 12);
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

            CreateLabel("JoinTitle", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(600f, 26f), "JOIN A KITCHEN", 16, new Color(0.35f, 0.75f, 0.98f), TextAnchor.MiddleCenter);
            lanHeaderLabel = CreateLabel("LanHeader", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(600f, 20f), "Open kitchens on your local Wi-Fi network:", 11, new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleCenter);

            // Discovered LAN Room Slots
            for (int i = 0; i < 2; i++)
            {
                int slotIdx = i;
                float yPos = -115f - (i * 38f);
                lanRoomButtons[i] = CreateButton($"LanRoomBtn_{i}", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(480f, 32f),
                    i == 0 ? "[LAN] Scanning for open kitchens..." : "[LAN] Click to refresh scan",
                    new Color(0.14f, 0.18f, 0.24f, 0.95f), out lanRoomButtonLabels[i], 11);
                lanRoomButtons[i].onClick.AddListener(() => OnLanRoomSlotClicked(slotIdx));
            }

            // Direct IP Connect row for cross-network / hotspot
            CreateLabel("IpPrompt", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, -205f), new Vector2(240f, 22f), "Or Enter Host IP:", 11, new Color(0.7f, 0.75f, 0.85f), TextAnchor.MiddleRight);
            string savedIp = PlayerPrefs.GetString("TwoLadles_LastJoinedIP", "127.0.0.1");
            ipInput = CreateInputField("IpInput", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(50f, -205f), new Vector2(170f, 28f), savedIp, "192.168.1.xxx");

            joinButton = CreateButton("DirectJoinBtn", joinPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(185f, -205f), new Vector2(85f, 28f), "CONNECT", new Color(0.20f, 0.48f, 0.78f, 1f), out joinButtonLabel, 11);
            joinButton.onClick.AddListener(OnJoinClicked);

            var cancelJoinBtn = CreateButton("CancelJoinBtn", joinPopupPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(160f, 38f), "BACK", new Color(0.25f, 0.28f, 0.36f, 1f), out _, 12);
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

            playersListText = CreateLabel("PlayersList", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(680f, 22f), "Room Chefs: Linh (West) & Jiwon (East)", 12, new Color(0.4f, 0.95f, 0.75f), TextAnchor.MiddleCenter);

            levelSelectHeaderLabel = CreateLabel("LevelSelectHeader", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(680f, 18f), "SELECT LEVEL TO PLAY:", 11, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleCenter);

            string[] lvlShortNames = new string[] { "Tteokbokki", "+ Bột Chiên", "+ K-BBQ Rice", "+ Broken Rice", "3m Banh Mi" };
            for (int i = 0; i < 5; i++)
            {
                int lvlId = i + 1;
                float xPos = -272f + (i * 136f);
                bool defaultUnlocked = (lvlId == 1);
                string defaultTag = defaultUnlocked ? "UNLOCKED" : "LOCKED";
                Color defaultCol = defaultUnlocked ? new Color(0.88f, 0.55f, 0.15f, 1f) : new Color(0.16f, 0.16f, 0.18f, 0.85f);
                levelSelectButtons[i] = CreateButton($"LevelBtn_{lvlId}", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(xPos, -128f), new Vector2(128f, 54f),
                    $"DAY {lvlId}\n{lvlShortNames[i]}\n[{defaultTag}]",
                    defaultCol, out levelSelectLabels[i], 11);
                levelSelectBgs[i] = levelSelectButtons[i].GetComponent<Image>();
                levelSelectButtons[i].onClick.AddListener(() => SelectCampaignLevel(lvlId));
            }

            endlessModeButton = CreateButton("EndlessBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(240f, 34f), "∞ ENDLESS SURVIVAL MODE", new Color(0.48f, 0.25f, 0.58f, 1f), out endlessModeButtonLabel, 11);
            endlessModeButton.onClick.AddListener(OnSelectEndlessClicked);

            // Room Action Row: Ready, Start Level, Leave
            readyButton = CreateButton("ReadyBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-210f, -252f), new Vector2(150f, 44f), "READY: YES", new Color(0.25f, 0.45f, 0.68f, 1f), out readyButtonText, 12);
            readyButton.onClick.AddListener(OnReadyClicked);

            startShiftButton = CreateButton("StartShiftBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(15f, -252f), new Vector2(260f, 44f), "▶ START LEVEL 1 (3 WAVES)", new Color(0.88f, 0.42f, 0.14f, 1f), out startShiftButtonText, 13);
            startShiftButton.onClick.AddListener(OnStartShiftClicked);

            leaveRoomButton = CreateButton("LeaveRoomBtn", roomPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(215f, -252f), new Vector2(110f, 44f), "LEAVE", new Color(0.68f, 0.22f, 0.25f, 1f), out _, 12);
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

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

            CreateLabel("SettingsTitle", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(600f, 26f), "SETTINGS & OPTIONS", 16, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleCenter);

            // Language Selection
            CreateLabel("LangHeader", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(600f, 20f), "Display Language:", 12, Color.white, TextAnchor.MiddleCenter);
            langEnButton = CreateButton("LangEnBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -135f), new Vector2(100f, 32f), "English", new Color(0.15f, 0.18f, 0.24f, 0.9f), out langEnText, 11);
            langEnButton.onClick.AddListener(() => SetAppLanguage(LocalizationManager.LANG_EN));

            langViButton = CreateButton("LangViBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(100f, 32f), "Tiếng Việt", new Color(0.15f, 0.18f, 0.24f, 0.9f), out langViText, 11);
            langViButton.onClick.AddListener(() => SetAppLanguage(LocalizationManager.LANG_VI));

            langKoButton = CreateButton("LangKoBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110f, -135f), new Vector2(100f, 32f), "한국어", new Color(0.15f, 0.18f, 0.24f, 0.9f), out langKoText, 11);
            langKoButton.onClick.AddListener(() => SetAppLanguage(LocalizationManager.LANG_KO));

            // Solo / AI Mode Toggle
            CreateLabel("ModeHeader", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(600f, 20f), "Cooperative or Solo Mode:", 12, Color.white, TextAnchor.MiddleCenter);
            modeToggleButton = CreateButton("ModeToggleBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-95f, -225f), new Vector2(170f, 34f), "MODE: LAN CO-OP", new Color(0.18f, 0.35f, 0.55f, 0.95f), out modeToggleText, 11);
            modeToggleButton.onClick.AddListener(OnToggleModeClicked);

            chefSelectButton = CreateButton("ChefSelectBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(95f, -225f), new Vector2(180f, 34f), "YOU: LINH (AI: JIWON)", new Color(0.24f, 0.28f, 0.38f, 0.95f), out chefSelectText, 11);
            chefSelectButton.onClick.AddListener(OnToggleChefSelectClicked);
            chefSelectButton.gameObject.SetActive(isSoloMode);

            // 2-Bot QA Test Button (Hidden on Mobile)
            twoBotTestButton = CreateButton("TwoBotTestBtn", settingsPopupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(220f, 36f), "🤖 2-BOT TEST RUN", new Color(0.18f, 0.50f, 0.64f, 0.75f), out twoBotTestButtonText, 11);
            twoBotTestButton.onClick.AddListener(OnTwoBotTestClicked);
            twoBotTestButton.gameObject.SetActive(!Application.isMobilePlatform);

            var closeSettingsBtn = CreateButton("CloseSettingsBtn", settingsPopupPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(150f, 36f), "CLOSE", new Color(0.25f, 0.28f, 0.36f, 1f), out _, 12);
            closeSettingsBtn.onClick.AddListener(ShowMainMenu);

            settingsPopupPanel.SetActive(false);

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
        }

        public void ShowHostPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(true);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
        }

        public void ShowJoinPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(true);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendLanDiscoveryProbe();
            }
        }

        public void ShowRoomPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(true);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);

            RefreshProgressionDisplay();
        }

        public void ShowSettingsPopup()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
            if (joinPopupPanel != null) joinPopupPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
            if (settingsPopupPanel != null) settingsPopupPanel.SetActive(true);
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
            string targetIp = ipInput != null && !string.IsNullOrEmpty(ipInput.text) ? ipInput.text : "127.0.0.1";

            PlayerPrefs.SetString("TwoLadles_PlayerName_Join", pName);
            PlayerPrefs.SetString("TwoLadles_LastJoinedIP", targetIp);
            PlayerPrefs.Save();

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.JoinGame(targetIp, NetworkManager.DEFAULT_PORT, pName);
            }
            ShowRoomPanel();
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
                NetworkManager.Instance.SendLanDiscoveryProbe();
                UpdateStatus("Scanning LAN for open kitchens...", false);
            }
        }

        private void OnLeaveRoomClicked()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Disconnect();
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
                    lanRoomButtonLabels[i].text = $"▶ JOIN: {r.roomName} ({r.hostName}) [{r.playerCount}/2]";
                    lanRoomButtons[i].GetComponent<Image>().color = new Color(0.16f, 0.55f, 0.35f, 0.95f);
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
                ShiftManager.Instance.isSoloMode = isSoloMode;
                ShiftManager.Instance.humanChefPeerId = humanChefPeerId;
                ShiftManager.Instance.StartShift(stage, mode);
            }
            gameObject.SetActive(false);
        }

        private void OnTwoBotTestClicked()
        {
            if (TwoLadles.AI.TwoBotQAManager.Instance == null)
            {
                var qaObj = new GameObject("TwoBotQAManager");
                qaObj.AddComponent<TwoLadles.AI.TwoBotQAManager>();
            }

            int startLvl = isEndlessSelected ? 1 : Mathf.Clamp(selectedLevelId, 1, 5);
            int endLvl = isEndlessSelected ? 1 : 5;
            TwoLadles.AI.TwoBotQAManager.Instance.StartAutoplayRun(startLvl, endLvl, UnityEngine.Random.Range(10, 9999), 2.0f);
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
                    playersListText.text = "Room Chefs: Linh (West) & Jiwon (East)";
                }
                else
                {
                    var parts = new List<string>();
                    foreach (var p in players.Values)
                    {
                        string readyMark = p.ready ? "READY ✓" : "WAITING...";
                        parts.Add($"{p.name} ({p.character.ToUpper()}): [{readyMark}]");
                    }
                    playersListText.text = "Chefs in Kitchen: " + string.Join("  •  ", parts);
                }
            }

            if (NetworkManager.Instance != null && NetworkManager.Instance.players.TryGetValue(NetworkManager.Instance.localPeerId, out var localP))
            {
                if (readyButtonText != null)
                {
                    readyButtonText.text = localP.ready ? "READY: YES ✓" : "READY: NO";
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
            if (lanHeaderLabel != null) lanHeaderLabel.text = lm.Get("menu.lan_header");
            if (hostButtonLabel != null) hostButtonLabel.text = lm.Get("menu.host_room");
            if (joinButtonLabel != null) joinButtonLabel.text = lm.Get("menu.auto_join");
            if (levelSelectHeaderLabel != null) levelSelectHeaderLabel.text = lm.Get("menu.level_select");
            if (playTutorialButtonLabel != null) playTutorialButtonLabel.text = lm.Get("menu.play_tutorial");
            if (endlessModeButtonLabel != null) endlessModeButtonLabel.text = lm.Get("menu.endless_mode");
            if (openShopButtonLabel != null) openShopButtonLabel.text = lm.Get("menu.upgrade_shop");
            if (twoBotTestButtonText != null) twoBotTestButtonText.text = lm.Get("menu.two_bot_test");
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
            for (int i = 0; i < 5; i++)
            {
                int lvlId = i + 1;
                if (levelSelectLabels[i] == null || levelSelectBgs[i] == null) continue;

                bool unlocked = ProgressionManager.Instance.IsLevelUnlocked(lvlId);
                bool completed = ProgressionManager.Instance.IsLevelCompleted(lvlId);
                bool isSelected = (!isEndlessSelected && selectedLevelId == lvlId);

                string statusTag = completed ? "★ COMPLETED ★" : (unlocked ? (lm != null ? lm.Get("menu.unlocked") : "UNLOCKED") : (lm != null ? lm.Get("menu.locked") : "LOCKED"));
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
