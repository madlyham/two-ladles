using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TwoLadles.Core;
using TwoLadles.Story;
using TwoLadles.Networking;

namespace TwoLadles.UI
{
    /// <summary>
    /// Start-of-day story phone. Built fully in code (no prefab).
    /// Flow: screen off -> wakes -> (first time only: swipe right on Jiwon -> opening chat)
    /// -> day chat -> dish card -> Start Shift. SKIP button / Esc always closes it.
    /// Uses unscaled time so it works while gameplay is paused.
    /// </summary>
    public class PhoneStoryUI : MonoBehaviour
    {
        public const string OPENING_SEEN_PREF = "TwoLadles_StoryOpeningSeen";

        private static PhoneStoryUI instance;
        public static bool IsShowing => instance != null;

        /// <summary>Story only plays on campaign days 1-5 (never tutorial, endless or QA autoplay).</summary>
        public static bool ShouldShowStory(int day, GameMode mode, bool isQA)
        {
            return mode == GameMode.CAMPAIGN && !isQA && day >= 1 && day <= 5;
        }

        public static void Show(int day, Action onDone)
        {
            if (instance != null) return;
            var go = new GameObject("PhoneStoryUI");
            var ui = go.AddComponent<PhoneStoryUI>();
            ui.day = day;
            ui.onDone = onDone;
        }

        /// <summary>Closes the phone without re-broadcasting (used by LAN STORY_DONE).</summary>
        public static void Close()
        {
            if (instance != null) instance.Finish(false);
        }

        // ---------------------------------------------------------------- labels
        private static readonly Dictionary<string, string[]> Labels = new Dictionary<string, string[]>
        {
            // key                 EN                         VI                          KO
            { "skip",      new[] { "SKIP >>",                 "BỎ QUA >>",                "건너뛰기 >>" } },
            { "start",     new[] { "Start Shift",             "Bắt đầu ca",               "영업 시작" } },
            { "day",       new[] { "Day {0}",                 "Ngày {0}",                 "{0}일차" } },
            { "newmsg",    new[] { "1 new message",           "1 tin nhắn mới",           "새 메시지 1개" } },
            { "swipe",     new[] { "Swipe right >>",          "Vuốt sang phải >>",        "오른쪽으로 밀기 >>" } },
            { "match",     new[] { "It's a match!",           "Ghép đôi rồi!",            "매칭 성공!" } },
            { "tap",       new[] { "Tap to speed up",         "Chạm để tua nhanh",        "탭하면 빨리 넘어가요" } },
            { "continue",  new[] { "Tap to continue >>",      "Chạm để tiếp tục >>",      "탭해서 계속 >>" } },
            { "today",     new[] { "Today's dish",            "Món hôm nay",              "오늘의 메뉴" } },
            { "by",        new[] { "Presented by {0}",        "Giới thiệu: {0}",          "소개: {0}" } },
            { "jiwon",     new[] { "Jiwon",                   "Jiwon",                    "지원" } },
            { "linh",      new[] { "Linh",                    "Linh",                     "린" } },
            { "both",      new[] { "Linh & Jiwon",            "Linh & Jiwon",             "린 & 지원" } },
            { "korea",     new[] { "Korea",                   "Hàn Quốc",                 "한국" } },
            { "vietnam",   new[] { "Vietnam",                 "Việt Nam",                 "베트남" } },
            { "fusion",    new[] { "Vietnam + Korea",         "Việt + Hàn",               "베트남 + 한국" } },
            { "online",    new[] { "online",                  "đang hoạt động",           "온라인" } },
        };

        private static string Lang => LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "en";

        private static string L(string key)
        {
            if (!Labels.TryGetValue(key, out var arr)) return key;
            string lang = Lang;
            return lang == "vi" ? arr[1] : (lang == "ko" ? arr[2] : arr[0]);
        }

        // ---------------------------------------------------------------- state
        private int day;
        private Action onDone;
        private bool finished;
        private bool openingShown;
        private bool tapRequested;
        private bool swiped;
        private bool continueHint;
        private Text tapHint;

        private readonly List<Action> refreshers = new List<Action>();
        private Font font;

        private static readonly Color PhoneBody = new Color32(0x2B, 0x2D, 0x42, 0xFF);
        private static readonly Color PhoneEdge = new Color32(0x10, 0x10, 0x18, 0xFF);
        private static readonly Color ScreenBg = new Color32(0xFF, 0xF4, 0xE6, 0xFF);
        private static readonly Color HeaderBg = new Color32(0xF2, 0x8C, 0x6B, 0xFF);
        private static readonly Color JiwonBubble = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color LinhBubble = new Color32(0xFF, 0xD1, 0x8A, 0xFF);
        private static readonly Color Ink = new Color32(0x2A, 0x22, 0x1E, 0xFF);
        private static readonly Color HeartRed = new Color32(0xE8, 0x3F, 0x5B, 0xFF);

        private Sprite boxSprite, heartSprite, jiwonHead, linhHead;

        private RectTransform screen;
        private CanvasGroup blackout;       // "screen off" layer
        private GameObject lockPanel, matchPanel, chatPanel, cardPanel;
        private RectTransform chatContent;
        private ScrollRect chatScroll;
        private RectTransform swipeCard;

        // ---------------------------------------------------------------- lifecycle
        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            font = KitchenHUD.GetDefaultFont();
            openingShown = day == 1 && PlayerPrefs.GetInt(OPENING_SEEN_PREF, 0) == 0;
            BuildSprites();
            BuildUI();
            if (LocalizationManager.Instance != null) LocalizationManager.Instance.OnLanguageChanged += Refresh;
            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null) LocalizationManager.Instance.OnLanguageChanged -= Refresh;
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Finish(true);
        }

        private void Refresh()
        {
            foreach (var r in refreshers) r();
        }

        private void Finish(bool broadcast)
        {
            if (finished) return;
            finished = true;
            if (openingShown)
            {
                PlayerPrefs.SetInt(OPENING_SEEN_PREF, 1);
                PlayerPrefs.Save();
            }
            if (broadcast && NetworkManager.Instance != null) NetworkManager.Instance.BroadcastStoryDone();
            var cb = onDone;
            onDone = null;
            cb?.Invoke();
            Destroy(gameObject);
        }

        // ---------------------------------------------------------------- flow
        private IEnumerator Run()
        {
            // 1) Screen off, then wake up.
            yield return Hold(0.7f);
            yield return Fade(blackout, 1f, 0f, 0.5f);
            blackout.blocksRaycasts = false;

            // 2) Lock screen with clock + notification.
            yield return Wait(1.4f);
            lockPanel.SetActive(false);

            // 3) First time only: swipe right on Jiwon.
            if (openingShown)
            {
                matchPanel.SetActive(true);
                float t = 0f;
                while (!swiped && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
                yield return FlyCardRight();
                yield return Wait(1.0f);
                matchPanel.SetActive(false);
            }

            // 4) Today's chat. Day 1 always replays the opening chat (it is Day 1's story),
            //    so replays no longer jump straight to the dish card.
            var story = StoryScript.GetDay(day);
            StoryLine[] lines = day == 1 ? StoryScript.Opening : story?.chat;
            if (lines != null && lines.Length > 0)
            {
                ClearChat();
                chatPanel.SetActive(true);
                yield return PlayChat(lines);
                // Last message stays on screen until the player taps (no auto-jump to the dish card).
                SetContinueHint(true);
                yield return WaitForTap();
                SetContinueHint(false);
            }

            // 5) Dish card + Start Shift.
            chatPanel.SetActive(false);
            BuildCard(story);
            cardPanel.SetActive(true);
            cardPanel.transform.localScale = Vector3.one * 0.8f;
            yield return Pop(cardPanel.transform, 0.25f);
        }

        private IEnumerator PlayChat(StoryLine[] lines)
        {
            foreach (var line in lines)
            {
                bool isLinh = line.speaker == "linh";
                var typing = AddBubble(isLinh, () => ".", true);
                var typingText = typing.GetComponentInChildren<Text>();
                float typeTime = 0.45f + Mathf.Min(line.Get(Lang).Length * 0.015f, 0.9f);
                float t = 0f;
                while (t < typeTime && !tapRequested)
                {
                    t += Time.unscaledDeltaTime;
                    int dots = 1 + (int)(t * 4f) % 3;
                    typingText.text = dots == 1 ? "." : (dots == 2 ? ". ." : ". . .");
                    yield return null;
                }
                tapRequested = false;
                Destroy(typing.transform.parent.gameObject);

                StoryLine captured = line;
                var bubble = AddBubble(isLinh, () => captured.Get(Lang), false);
                bubble.transform.localScale = Vector3.one * 0.6f;
                yield return Pop(bubble.transform, 0.18f);
                yield return Wait(ReadTime(captured.Get(Lang)));
            }
        }

        /// <summary>Seconds to leave a message on screen before the next one: ~0.05s per character, 2-6s.</summary>
        public static float ReadTime(string text)
        {
            int len = text != null ? text.Length : 0;
            return Mathf.Clamp(1.2f + len * 0.05f, 2f, 6f);
        }

        // Waits for a tap only (no timeout). SKIP / Esc still close the phone.
        private IEnumerator WaitForTap()
        {
            tapRequested = false;
            while (!tapRequested) yield return null;
            tapRequested = false;
        }

        private void SetContinueHint(bool on)
        {
            continueHint = on;
            if (tapHint != null) tapHint.text = L(on ? "continue" : "tap");
        }

        private IEnumerator FlyCardRight()
        {
            Vector2 start = swipeCard.anchoredPosition;
            Vector2 end = new Vector2(420f, start.y + 40f);
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                swipeCard.anchoredPosition = Vector2.Lerp(start, end, k * k);
                swipeCard.localRotation = Quaternion.Euler(0, 0, -20f * k);
                yield return null;
            }
            swipeCard.gameObject.SetActive(false);
            matchPanel.transform.Find("MatchText").gameObject.SetActive(true);
            yield return Pop(matchPanel.transform.Find("MatchText"), 0.25f);
        }

        // Wait that the player can shorten by tapping.
        private IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds && !tapRequested) { t += Time.unscaledDeltaTime; yield return null; }
            tapRequested = false;
        }

        // Wait that cannot be shortened.
        private IEnumerator Hold(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private static IEnumerator Fade(CanvasGroup g, float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / time);
                yield return null;
            }
            g.alpha = to;
        }

        private static IEnumerator Pop(Transform tr, float time)
        {
            Vector3 from = tr.localScale.x < 0.99f ? tr.localScale : Vector3.one * 0.6f;
            float t = 0f;
            while (t < time && tr != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / time);
                float overshoot = 1f + 0.12f * Mathf.Sin(k * Mathf.PI);
                tr.localScale = Vector3.Lerp(from, Vector3.one, k) * overshoot;
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        // ---------------------------------------------------------------- sprites
        private void BuildSprites()
        {
            // 8x8 pixel box with 2px border and clipped corners, sliced.
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    bool corner = (x == 0 || x == 7) && (y == 0 || y == 7);
                    bool edge = x == 0 || x == 7 || y == 0 || y == 7;
                    tex.SetPixel(x, y, corner ? Color.clear : (edge ? new Color(0, 0, 0, 0.85f) : Color.white));
                }
            tex.Apply();
            boxSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));

            // Pixel heart.
            string[] rows =
            {
                ".XX...XX.",
                "XXXX.XXXX",
                "XXXXXXXXX",
                "XXXXXXXXX",
                ".XXXXXXX.",
                "..XXXXX..",
                "...XXX...",
                "....X....",
            };
            var h = new Texture2D(9, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int r = 0; r < rows.Length; r++)
                for (int x = 0; x < 9; x++)
                    h.SetPixel(x, 7 - r, rows[r][x] == 'X' ? Color.white : Color.clear);
            h.Apply();
            heartSprite = Sprite.Create(h, new Rect(0, 0, 9, 8), new Vector2(0.5f, 0.5f), 100f);

            jiwonHead = HeadCrop(Resources.Load<Sprite>("Sprites/Characters/jiwon_tier1"));
            linhHead = HeadCrop(Resources.Load<Sprite>("Sprites/Characters/linh_tier1"));
        }

        // Top square of a full-body sprite = head and shoulders.
        private static Sprite HeadCrop(Sprite full)
        {
            if (full == null) return null;
            Rect r = full.rect;
            float side = Mathf.Min(r.width, r.height);
            var crop = new Rect(r.x, r.y + r.height - side, side, side);
            return Sprite.Create(full.texture, crop, new Vector2(0.5f, 0.5f), full.pixelsPerUnit);
        }

        // ---------------------------------------------------------------- UI building
        private void BuildUI()
        {
            if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Dim background (also blocks clicks to the HUD behind).
            var dim = Panel(transform, "Dim", new Color(0, 0, 0, 0.78f));
            Stretch(dim.rectTransform);

            // Phone body.
            var phone = Box(transform, "Phone", PhoneBody, 0.25f);
            phone.rectTransform.sizeDelta = new Vector2(380, 680);
            phone.rectTransform.anchoredPosition = Vector2.zero;
            var edge = phone.gameObject.AddComponent<Outline>();
            edge.effectColor = PhoneEdge;
            edge.effectDistance = new Vector2(4, -4);

            // Speaker slit.
            var slit = Panel(phone.transform, "Speaker", PhoneEdge);
            slit.rectTransform.sizeDelta = new Vector2(70, 8);
            slit.rectTransform.anchorMin = slit.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            slit.rectTransform.anchoredPosition = new Vector2(0, -12);

            // Screen.
            var scr = Panel(phone.transform, "Screen", ScreenBg);
            screen = scr.rectTransform;
            Stretch(screen, 16, 26, 16, 26);
            scr.gameObject.AddComponent<RectMask2D>();

            // Tap anywhere on the screen to speed up.
            var tapBtn = scr.gameObject.AddComponent<Button>();
            tapBtn.transition = Selectable.Transition.None;
            tapBtn.onClick.AddListener(() => tapRequested = true);

            BuildStatusBar();
            BuildLockPanel();
            BuildMatchPanel();
            BuildChatPanel();
            cardPanel = new GameObject("Card", typeof(RectTransform)).gameObject;
            cardPanel.transform.SetParent(screen, false);
            Stretch((RectTransform)cardPanel.transform, 0, 24, 0, 0);
            cardPanel.SetActive(false);

            // Blackout (screen off) on top of everything inside the screen.
            var black = Panel(screen, "Blackout", Color.black);
            Stretch(black.rectTransform);
            blackout = black.gameObject.AddComponent<CanvasGroup>();
            blackout.alpha = 1f;

            // SKIP - always visible, outside the phone.
            var skip = MakeButton(transform, () => L("skip"), new Color32(0x44, 0x44, 0x55, 0xFF), () => Finish(true));
            var srt = (RectTransform)skip.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(1, 0);
            srt.sizeDelta = new Vector2(180, 50);
            srt.anchoredPosition = new Vector2(-30, 30);
        }

        private void BuildStatusBar()
        {
            var bar = Panel(screen, "Status", new Color(0, 0, 0, 0.08f));
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, 24); rt.anchoredPosition = Vector2.zero;
            var time = MakeText(bar.transform, () => "07:00", 14, Ink, TextAnchor.MiddleLeft);
            Stretch(time.rectTransform, 10, 0, 10, 0);
            var batt = MakeText(bar.transform, () => "Wi-Fi  87%", 14, Ink, TextAnchor.MiddleRight);
            Stretch(batt.rectTransform, 10, 0, 10, 0);
        }

        private void BuildLockPanel()
        {
            var bg = Panel(screen, "Lock", new Color32(0x3A, 0x2F, 0x5B, 0xFF));
            Stretch(bg.rectTransform, 0, 24, 0, 0);
            lockPanel = bg.gameObject;

            var clock = MakeText(bg.transform, () => "07:00", 64, Color.white, TextAnchor.MiddleCenter);
            Place(clock.rectTransform, 0, -110, 300, 80);
            var dayT = MakeText(bg.transform, () => string.Format(L("day"), day), 24, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
            Place(dayT.rectTransform, 0, -170, 300, 36);

            var note = Box(bg.transform, "Notification", new Color(1, 1, 1, 0.92f), 0.5f);
            Place(note.rectTransform, 0, -270, 300, 64);
            var av = Avatar(note.transform, jiwonHead, 44);
            av.rectTransform.anchorMin = av.rectTransform.anchorMax = new Vector2(0, 0.5f);
            av.rectTransform.anchoredPosition = new Vector2(34, 0);
            var nt = MakeText(note.transform, () => L("jiwon") + "\n" + L("newmsg"), 16, Ink, TextAnchor.MiddleLeft);
            Stretch(nt.rectTransform, 66, 4, 10, 4);
        }

        private void BuildMatchPanel()
        {
            var bg = Panel(screen, "Match", new Color32(0xFF, 0xE3, 0xEA, 0xFF));
            Stretch(bg.rectTransform, 0, 24, 0, 0);
            matchPanel = bg.gameObject;
            matchPanel.SetActive(false);

            // Swipeable Jiwon card.
            var card = Box(bg.transform, "SwipeCard", Color.white, 0.5f);
            swipeCard = card.rectTransform;
            Place(swipeCard, 0, -60, 260, 380);
            // Head-and-shoulders crop: a masked frame shows only the top of the full-body sprite (legs cut off).
            var frame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(RectMask2D));
            frame.transform.SetParent(card.transform, false);
            Stretch((RectTransform)frame.transform, 20, 20, 20, 70);
            var pic = new GameObject("Portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            pic.transform.SetParent(frame.transform, false);
            pic.sprite = Resources.Load<Sprite>("Sprites/Characters/jiwon_tier1");
            pic.preserveAspect = true;
            pic.raycastTarget = false;
            if (pic.sprite == null) pic.color = Color.clear;
            const float portraitW = 270f; // wider than the frame = zoomed in
            float portraitH = pic.sprite != null ? portraitW * pic.sprite.rect.height / pic.sprite.rect.width : 600f;
            Place(pic.rectTransform, 0, -24, portraitW, portraitH, true); // -24 = sits a bit lower in the card
            var name = MakeText(card.transform, () => L("jiwon"), 26, Ink, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.anchorMin = new Vector2(0, 0); name.rectTransform.anchorMax = new Vector2(1, 0);
            name.rectTransform.pivot = new Vector2(0.5f, 0); name.rectTransform.sizeDelta = new Vector2(0, 50);
            name.rectTransform.anchoredPosition = new Vector2(0, 12);
            var swipe = card.gameObject.AddComponent<SwipeCard>();
            swipe.owner = this;

            var hint = MakeText(bg.transform, () => L("swipe"), 18, Ink, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, 0, -470, 300, 30);

            var go = MakeButton(bg.transform, () => ">>", HeartRed, () => swiped = true);
            Place((RectTransform)go.transform, 0, -530, 90, 50);

            // Shown after swipe.
            var match = new GameObject("MatchText", typeof(RectTransform));
            match.transform.SetParent(bg.transform, false);
            Place((RectTransform)match.transform, 0, -230, 300, 200);
            var heart = new GameObject("Heart", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            heart.transform.SetParent(match.transform, false);
            heart.sprite = heartSprite; heart.color = HeartRed;
            Place(heart.rectTransform, 0, 0, 108, 96);
            var mt = MakeText(match.transform, () => L("match"), 30, HeartRed, TextAnchor.MiddleCenter);
            mt.fontStyle = FontStyle.Bold;
            Place(mt.rectTransform, 0, -110, 300, 50);
            match.SetActive(false);
        }

        private void BuildChatPanel()
        {
            var root = new GameObject("Chat", typeof(RectTransform));
            root.transform.SetParent(screen, false);
            Stretch((RectTransform)root.transform, 0, 24, 0, 0);
            chatPanel = root;

            // Header: Jiwon's contact.
            var header = Panel(root.transform, "Header", HeaderBg);
            var hrt = header.rectTransform;
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1); hrt.sizeDelta = new Vector2(0, 56); hrt.anchoredPosition = Vector2.zero;
            var av = Avatar(header.transform, jiwonHead, 40);
            av.rectTransform.anchorMin = av.rectTransform.anchorMax = new Vector2(0, 0.5f);
            av.rectTransform.anchoredPosition = new Vector2(32, 0);
            var hn = MakeText(header.transform, () => L("jiwon") + "\n<size=12>" + L("online") + "</size>", 18, Color.white, TextAnchor.MiddleLeft);
            hn.supportRichText = true;
            Stretch(hn.rectTransform, 62, 0, 10, 0);
            var heart = new GameObject("Heart", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            heart.transform.SetParent(header.transform, false);
            heart.sprite = heartSprite; heart.color = Color.white;
            heart.rectTransform.anchorMin = heart.rectTransform.anchorMax = new Vector2(1, 0.5f);
            heart.rectTransform.sizeDelta = new Vector2(27, 24);
            heart.rectTransform.anchoredPosition = new Vector2(-24, 0);

            // Scrolling message list.
            var view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            view.transform.SetParent(root.transform, false);
            var vrt = (RectTransform)view.transform;
            Stretch(vrt, 6, 60, 6, 34);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(view.transform, false);
            chatContent = (RectTransform)content.transform;
            chatContent.anchorMin = new Vector2(0, 1); chatContent.anchorMax = new Vector2(1, 1);
            chatContent.pivot = new Vector2(0.5f, 1); chatContent.sizeDelta = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.padding = new RectOffset(4, 4, 8, 8);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            chatScroll = view.AddComponent<ScrollRect>();
            chatScroll.content = chatContent;
            chatScroll.viewport = vrt;
            chatScroll.horizontal = false;
            chatScroll.movementType = ScrollRect.MovementType.Clamped;

            var tap = MakeText(root.transform, () => L(continueHint ? "continue" : "tap"), 13, new Color(0, 0, 0, 0.45f), TextAnchor.MiddleCenter);
            tapHint = tap;
            tap.rectTransform.anchorMin = new Vector2(0, 0); tap.rectTransform.anchorMax = new Vector2(1, 0);
            tap.rectTransform.pivot = new Vector2(0.5f, 0); tap.rectTransform.sizeDelta = new Vector2(0, 28);
            tap.rectTransform.anchoredPosition = new Vector2(0, 4);

            chatPanel.SetActive(false);
        }

        private void ClearChat()
        {
            for (int i = chatContent.childCount - 1; i >= 0; i--) Destroy(chatContent.GetChild(i).gameObject);
        }

        /// <summary>Adds one chat row. Returns the bubble (child of the row).</summary>
        private GameObject AddBubble(bool isLinh, Func<string> getter, bool isTyping)
        {
            var row = new GameObject(isLinh ? "LinhRow" : "JiwonRow", typeof(RectTransform));
            row.transform.SetParent(chatContent, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = isLinh ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            Image avatar = null;
            if (!isLinh) avatar = Avatar(row.transform, jiwonHead, 34);

            var bubble = Box(row.transform, "Bubble", isLinh ? LinhBubble : JiwonBubble, 0.5f);
            var bvl = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            bvl.padding = new RectOffset(10, 10, 7, 7);
            bvl.childControlWidth = true; bvl.childControlHeight = true;
            bvl.childForceExpandWidth = true; bvl.childForceExpandHeight = false;
            var ble = bubble.gameObject.AddComponent<LayoutElement>();

            var text = MakeText(bubble.transform, getter, 16, Ink, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (isLinh) avatar = Avatar(row.transform, linhHead, 34);
            var ale = avatar.gameObject.AddComponent<LayoutElement>();
            ale.preferredWidth = 34; ale.preferredHeight = 34; ale.minWidth = 34;

            // Bubble width hugs short messages, wraps long ones.
            Action size = () =>
            {
                if (text == null) return;
                float w = text.cachedTextGeneratorForLayout.GetPreferredWidth(text.text, text.GetGenerationSettings(Vector2.zero)) / text.pixelsPerUnit;
                ble.preferredWidth = Mathf.Min(w + 22f, 230f);
            };
            size();
            if (!isTyping) refreshers.Add(size);

            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
            StartCoroutine(ScrollToBottomNextFrame());
            return bubble.gameObject;
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            if (chatScroll != null) chatScroll.verticalNormalizedPosition = 0f;
        }

        private void BuildCard(DayStory story)
        {
            var bg = Panel(cardPanel.transform, "CardBg", new Color32(0xFF, 0xEC, 0xD2, 0xFF));
            Stretch(bg.rectTransform);

            var title = MakeText(bg.transform, () => string.Format(L("day"), day) + "  -  " + L("today"), 16, new Color(0, 0, 0, 0.55f), TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0, -26, 320, 28);

            var frame = Box(bg.transform, "DishFrame", Color.white, 0.5f);
            Place(frame.rectTransform, 0, -110, 120, 120);
            var dish = new GameObject("Dish", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            dish.transform.SetParent(frame.transform, false);
            Stretch(dish.rectTransform, 12, 12, 12, 12);
            dish.preserveAspect = true;
            if (story != null) dish.sprite = Resources.Load<Sprite>("Sprites/Items/" + story.dishId);
            if (dish.sprite == null) { dish.sprite = heartSprite; dish.color = HeartRed; }

            var name = MakeText(bg.transform, () => story != null ? story.GetName(Lang) : "", 26, Ink, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold;
            Place(name.rectTransform, 0, -200, 320, 40);

            var meta = MakeText(bg.transform, () => story == null ? "" :
                string.Format(L("by"), L(story.presenter)) + "  |  " + L(story.origin), 15, HeaderBg, TextAnchor.MiddleCenter);
            Place(meta.rectTransform, 0, -236, 320, 26);

            var descBox = Box(bg.transform, "DescBox", Color.white, 0.5f);
            Place(descBox.rectTransform, 0, -262, 316, 230, true);
            var desc = MakeText(descBox.transform, () => story != null ? story.GetDesc(Lang) : "", 16, Ink, TextAnchor.UpperLeft);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.resizeTextForBestFit = true; desc.resizeTextMinSize = 11; desc.resizeTextMaxSize = 16;
            Stretch(desc.rectTransform, 12, 10, 12, 10);

            var start = MakeButton(bg.transform, () => L("start"), new Color32(0x3C, 0xA5, 0x5C, 0xFF), () => Finish(true));
            var srt = (RectTransform)start.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0);
            srt.pivot = new Vector2(0.5f, 0);
            srt.sizeDelta = new Vector2(220, 52);
            srt.anchoredPosition = new Vector2(0, 22);
        }

        // ---------------------------------------------------------------- helpers
        private Image Panel(Transform parent, string name, Color color)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.color = color;
            return img;
        }

        private Image Box(Transform parent, string name, Color color, float pixelScale)
        {
            var img = Panel(parent, name, color);
            img.sprite = boxSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = pixelScale; // smaller = chunkier pixels
            return img;
        }

        private Image Avatar(Transform parent, Sprite sprite, float size)
        {
            var img = Box(parent, "Avatar", new Color32(0xFF, 0xE9, 0xC9, 0xFF), 0.5f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            if (sprite != null)
            {
                var face = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                face.transform.SetParent(img.transform, false);
                face.sprite = sprite;
                face.preserveAspect = true;
                face.raycastTarget = false;
                Stretch(face.rectTransform, 3, 3, 3, 3);
            }
            return img;
        }

        private Text MakeText(Transform parent, Func<string> getter, int size, Color color, TextAnchor align)
        {
            var t = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.text = getter();
            refreshers.Add(() => { if (t != null) t.text = getter(); });
            return t;
        }

        private GameObject MakeButton(Transform parent, Func<string> label, Color color, Action onClick)
        {
            var img = Box(parent, "Button", color, 0.5f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            var t = MakeText(img.transform, label, 20, Color.white, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            Stretch(t.rectTransform);
            return img.gameObject;
        }

        private static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>Top-center anchored placement. y is the offset from the top (negative = down).</summary>
        private static void Place(RectTransform rt, float x, float y, float w, float h, bool topPivot = false)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, topPivot ? 1f : 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ---------------------------------------------------------------- swipe
        private class SwipeCard : MonoBehaviour, IDragHandler, IEndDragHandler
        {
            public PhoneStoryUI owner;
            private float dragX;

            public void OnDrag(PointerEventData e)
            {
                if (owner == null || owner.swiped) return;
                var canvas = GetComponentInParent<Canvas>();
                float scale = canvas != null ? canvas.scaleFactor : 1f;
                dragX = Mathf.Max(0f, dragX + e.delta.x / scale);
                var rt = (RectTransform)transform;
                rt.anchoredPosition = new Vector2(dragX, -60f);
                rt.localRotation = Quaternion.Euler(0, 0, -dragX * 0.08f);
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (owner == null || owner.swiped) return;
                if (dragX > 70f) owner.swiped = true;
                else
                {
                    dragX = 0f;
                    var rt = (RectTransform)transform;
                    rt.anchoredPosition = new Vector2(0f, -60f);
                    rt.localRotation = Quaternion.identity;
                }
            }
        }
    }
}
