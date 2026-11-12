using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Kitchen;
using TwoLadles.Player;
using TwoLadles.Networking;

namespace TwoLadles.UI
{
    public class QuickSelectWheelUI : MonoBehaviour
    {
        public static QuickSelectWheelUI Instance { get; private set; }

        public struct WheelItem
        {
            public string itemId;
            public string displayName;
            public string iconSpriteName;
            public Color itemColor;

            public WheelItem(string id, string name, string icon, Color col)
            {
                itemId = id;
                displayName = name;
                iconSpriteName = icon;
                itemColor = col;
            }
        }

        // Per-station storage: 2 distinct items per fridge/shelf, 8 total, no duplicates.
        // West (Linh) keeps the Day-1 tutorial items (Toboki in fridge, Gochujang on shelf).
        private static readonly WheelItem[] FRIDGE_WEST_ITEMS = new WheelItem[]
        {
            new WheelItem("tteok_portioned", "Toboki Portion", "tteok_portioned", new Color(0.96f, 0.96f, 0.94f)),
            new WheelItem("egg_raw", "Fresh Egg", "egg_raw", new Color(0.98f, 0.92f, 0.72f))
        };
        private static readonly WheelItem[] FRIDGE_EAST_ITEMS = new WheelItem[]
        {
            new WheelItem("unprocessed_raw_meat", "Raw Meat", "unprocessed_raw_meat", new Color(0.95f, 0.45f, 0.45f)),
            new WheelItem("radish_carrot", "Radish & Carrot", "radish_carrot", new Color(0.98f, 0.65f, 0.25f))
        };
        private static readonly WheelItem[] SHELF_WEST_ITEMS = new WheelItem[]
        {
            new WheelItem("gochujang", "Gochujang", "gochujang", new Color(0.92f, 0.25f, 0.20f)),
            new WheelItem("flour", "Flour", "flour", new Color(0.96f, 0.94f, 0.88f))
        };
        private static readonly WheelItem[] SHELF_EAST_ITEMS = new WheelItem[]
        {
            new WheelItem("rice_portioned", "Rice Portion", "rice_portioned", new Color(0.98f, 0.98f, 0.98f)),
            new WheelItem("baguette", "Baguette", "baguette", new Color(0.90f, 0.70f, 0.36f))
        };

        private static WheelItem[] Concat(WheelItem[] a, WheelItem[] b)
        {
            var r = new WheelItem[a.Length + b.Length];
            a.CopyTo(r, 0);
            b.CopyTo(r, a.Length);
            return r;
        }

        private static WheelItem[] GetStationCatalog(string stationId)
        {
            switch (stationId)
            {
                case "fridge_west": return FRIDGE_WEST_ITEMS;
                case "fridge_east": return FRIDGE_EAST_ITEMS;
                case "shelf_west": return SHELF_WEST_ITEMS;
                case "shelf_east": return SHELF_EAST_ITEMS;
                default:
                    bool fridge = stationId != null && stationId.Contains("fridge");
                    return fridge ? Concat(FRIDGE_WEST_ITEMS, FRIDGE_EAST_ITEMS) : Concat(SHELF_WEST_ITEMS, SHELF_EAST_ITEMS);
            }
        }

        private static string[] ToIds(WheelItem[] src)
        {
            var ids = new string[src.Length];
            for (int i = 0; i < src.Length; i++) ids[i] = src[i].itemId;
            return ids;
        }

        public static string[] GetStationCatalogIds(string stationId) => ToIds(GetStationCatalog(stationId));

        /// <summary>All fridge items (both fridges) or all shelf items (both shelves).</summary>
        public static string[] GetCatalogIds(bool isFridge) => ToIds(GetStationCatalog(isFridge ? "fridge" : "shelf"));

        /// <summary>Wheel closes once the chef walks a bit past interact range.</summary>
        public const float AUTO_CLOSE_DISTANCE = 3.0f;
        public static bool ShouldAutoClose(float distanceToStation) => distanceToStation > AUTO_CLOSE_DISTANCE;

        /// <summary>Bots hold their wheel open this long before taking the item.</summary>
        public const float BOT_WHEEL_DELAY = 0.5f;
        /// <summary>Wheel drawn at 75% size: readable, still leaves the kitchen visible.</summary>
        public const float WHEEL_SCALE = 0.75f;
        private const float DEAD_ZONE = 36f;

        // Shared wheel layout (unscaled pixels) for player and bot wheels.
        private const float WHEEL_SIZE = 420f;
        private const float SLICE_RADIUS = 145f;
        private const float SLICE_SIZE = 96f;

        /// <summary>Linh's wheel sits left of centre, Jiwon's right of centre (mirror-symmetric).</summary>
        public static float GetWheelAnchorX(string characterId) => characterId == "jiwon" ? 0.72f : 0.28f;

        /// <summary>Slice under a mouse offset from the wheel centre; -1 inside the dead zone.</summary>
        public static int SliceIndexFor(Vector2 offset, int count)
        {
            if (count <= 0 || offset.magnitude <= DEAD_ZONE * WHEEL_SCALE) return -1;
            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            return Mathf.RoundToInt(angle / (360f / count)) % count;
        }

        private RectTransform wheelRect;
        private readonly Dictionary<string, GameObject> botWheels = new Dictionary<string, GameObject>();

        private Transform openStation;
        private string openStationId;
        private WheelItem[] currentCatalog = FRIDGE_WEST_ITEMS;
        private GameObject wheelRoot;
        private Text storageHeaderText;
        private List<GameObject> sliceObjects = new List<GameObject>();
        private List<Button> sliceButtons = new List<Button>();
        private List<Image> sliceHighlights = new List<Image>();
        private Text centerTitleText;
        private Image centerIconImage;

        public bool IsOpen => wheelRoot != null && wheelRoot.activeSelf;
        private int highlightedIndex = -1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            // Fill the HUD so the left/right anchors map to the real screen edges.
            var selfRect = GetComponent<RectTransform>();
            if (selfRect != null)
            {
                selfRect.anchorMin = Vector2.zero;
                selfRect.anchorMax = Vector2.one;
                selfRect.offsetMin = Vector2.zero;
                selfRect.offsetMax = Vector2.zero;
            }
            BuildWheelUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool inActiveShift = ShiftManager.Instance != null && ShiftManager.Instance.shiftActive;
            if (!inActiveShift)
            {
                if (IsOpen) CloseWheel();
                HideAllBotPicks();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                CloseWheel();
                return;
            }

            // Auto-close when the local chef walks away from the opened fridge/shelf
            if (IsOpen && openStation != null && KitchenController.Instance != null)
            {
                int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
                if (KitchenController.Instance.playersByPeer.TryGetValue(localPeer, out var chef) && chef != null)
                {
                    Vector3 d = chef.transform.position - openStation.position;
                    d.y = 0f;
                    if (ShouldAutoClose(d.magnitude))
                    {
                        CloseWheel();
                        return;
                    }
                }
            }

            // Mouse direction highlight when wheel is open
            if (IsOpen && currentCatalog != null && currentCatalog.Length > 0)
            {
                Vector2 centre = wheelRect != null
                    ? RectTransformUtility.WorldToScreenPoint(null, wheelRect.position)
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                int idx = SliceIndexFor((Vector2)Input.mousePosition - centre, currentCatalog.Length);
                if (idx >= 0) HighlightSlice(idx);
            }
        }

        public void ToggleWheel()
        {
            if (IsOpen) CloseWheel();
            else OpenWheelForStorage(true);
        }

        public void OpenWheel()
        {
            OpenWheelForStorage(true);
        }

        public void OpenWheelForStorage(bool isFridge)
        {
            OpenWheelForStation(isFridge ? "fridge" : "shelf", null);
        }

        /// <summary>Opens the wheel with only the items stocked at this station; auto-closes when the chef walks away.</summary>
        public void OpenWheelForStation(string stationId, Transform station)
        {
            currentCatalog = GetStationCatalog(stationId);
            openStation = station;
            openStationId = stationId;
            bool isFridge = stationId != null && stationId.Contains("fridge");
            var names = new List<string>();
            foreach (var it in currentCatalog) names.Add(it.displayName.ToUpper());
            RebuildSlices((isFridge ? "FRIDGE (" : "SHELF (") + string.Join(" • ", names) + ")");
            if (wheelRect != null)
            {
                float ax = GetWheelAnchorX(GetLocalCharacterId());
                wheelRect.anchorMin = new Vector2(ax, 0.5f);
                wheelRect.anchorMax = new Vector2(ax, 0.5f);
            }
            if (wheelRoot != null)
            {
                wheelRoot.SetActive(true);
                HighlightSlice(0);
            }
        }

        private static string GetLocalCharacterId()
        {
            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (KitchenController.Instance != null && KitchenController.Instance.playersByPeer.TryGetValue(localPeer, out var chef) && chef != null)
                return chef.characterId;
            return "linh";
        }

        /// <summary>Display-only wheel showing a bot picking an item (no buttons, no input).</summary>
        public void ShowBotPick(string characterId, string stationId, string itemId)
        {
            string key = string.IsNullOrEmpty(characterId) ? "bot" : characterId;
            HideBotPick(key);
            var catalog = GetStationCatalog(stationId);
            if (catalog == null || catalog.Length == 0) return;

            var go = new GameObject($"BotWheel_{key}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<RectTransform>();
            float ax = GetWheelAnchorX(key);
            r.anchorMin = new Vector2(ax, 0.5f);
            r.anchorMax = new Vector2(ax, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(WHEEL_SIZE, WHEEL_SIZE);
            go.transform.localScale = Vector3.one * WHEEL_SCALE;

            MakeDisc(go.transform, "Backdrop", Vector2.zero, WHEEL_SIZE, new Color(0.06f, 0.08f, 0.12f, 0.85f));
            int chosen = Array.FindIndex(catalog, x => x.itemId == itemId);
            float step = (2f * Mathf.PI) / catalog.Length;
            for (int i = 0; i < catalog.Length; i++)
            {
                var pos = new Vector2(Mathf.Cos(i * step), Mathf.Sin(i * step)) * SLICE_RADIUS;
                var col = i == chosen ? new Color(0.95f, 0.75f, 0.2f, 0.90f) : new Color(0.18f, 0.22f, 0.28f, 0.78f);
                var slice = MakeDisc(go.transform, "Slice_" + catalog[i].itemId, pos, SLICE_SIZE, col);
                var icon = new GameObject("Icon", typeof(RectTransform));
                icon.transform.SetParent(slice.transform, false);
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(62f, 62f);
                var img = icon.AddComponent<Image>();
                img.raycastTarget = false;
                var spr = PlayerController.LoadItemSprite(catalog[i].iconSpriteName);
                if (spr != null) img.sprite = spr; else img.color = catalog[i].itemColor;
            }
            botWheels[key] = go;
        }

        public void HideBotPick(string characterId)
        {
            string key = string.IsNullOrEmpty(characterId) ? "bot" : characterId;
            if (botWheels.TryGetValue(key, out var go))
            {
                if (go != null) Destroy(go);
                botWheels.Remove(key);
            }
        }

        private void HideAllBotPicks()
        {
            if (botWheels.Count == 0) return;
            foreach (var go in botWheels.Values) if (go != null) Destroy(go);
            botWheels.Clear();
        }

        private static GameObject MakeDisc(Transform parent, string name, Vector2 pos, float size, Color col)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.sprite = KitchenHUD.GetCircleSprite();
            img.color = col;
            img.raycastTarget = false;
            return go;
        }

        public void CloseWheel()
        {
            if (wheelRoot != null)
            {
                wheelRoot.SetActive(false);
            }
            openStation = null;
            highlightedIndex = -1;
        }

        public void HighlightSlice(int index)
        {
            if (currentCatalog == null || index < 0 || index >= currentCatalog.Length) return;
            highlightedIndex = index;

            for (int i = 0; i < sliceHighlights.Count; i++)
            {
                if (sliceHighlights[i] != null)
                {
                    sliceHighlights[i].color = (i == index)
                        ? new Color(0.95f, 0.75f, 0.2f, 0.90f)
                        : new Color(0.18f, 0.22f, 0.28f, 0.78f);
                }
            }

            var item = currentCatalog[index];
            if (centerTitleText != null)
            {
                string locName = item.displayName;
                if (LocalizationManager.Instance != null)
                {
                    string loc = LocalizationManager.Instance.Get("item." + item.itemId, null);
                    if (!string.IsNullOrEmpty(loc)) locName = loc;
                }
                centerTitleText.text = locName.ToUpper();
                centerTitleText.color = item.itemColor;
            }
            if (centerIconImage != null)
            {
                Sprite spr = PlayerController.LoadItemSprite(item.iconSpriteName);
                if (spr != null)
                {
                    centerIconImage.sprite = spr;
                    centerIconImage.color = Color.white;
                }
            }
        }

        public void SelectItem(string itemId)
        {
            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (KitchenController.Instance == null || !KitchenController.Instance.playersByPeer.TryGetValue(localPeer, out var localChef) || localChef == null)
                return;

            if (localChef.carriedItem != null)
            {
                KitchenController.Instance.TriggerStatusToast("Hands full! Place or throw current item first.", new Color(1.0f, 0.45f, 0.45f));
                return;
            }

            // LAN guest: the host owns item state, so ask it to hand over this pick.
            if (NetworkManager.Instance != null && NetworkManager.Instance.isLanActive && !NetworkManager.Instance.IsHost())
            {
                NetworkManager.Instance.SendClientDispense(openStationId, itemId);
                CloseWheel();
                return;
            }

            WheelItem chosen = Array.Find(currentCatalog, x => x.itemId == itemId);
            string dName = !string.IsNullOrEmpty(chosen.displayName) ? chosen.displayName : itemId;
            string vesselType = "none";
            if (itemId == "clean_plate") vesselType = "plate";
            else if (itemId == "clean_bowl") vesselType = "bowl";

            localChef.carriedItem = new KitchenItem(itemId, dName, vesselType, QualityTier.STANDARD);
            localChef.RefreshVisuals();

            if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial)
            {
                if (ShiftManager.Instance.tutorialStep <= 2 && (itemId == "tteok_portioned" || itemId == "gochujang" || itemId == "pork_raw"))
                {
                    ShiftManager.Instance.AdvanceTutorialStep(3);
                }
                else if (ShiftManager.Instance.tutorialStep <= 4 && (itemId == "gochujang" || itemId == "tteok_portioned"))
                {
                    ShiftManager.Instance.AdvanceTutorialStep(4);
                }
            }

            KitchenController.Instance.TriggerStatusToast($"Picked {dName}!", new Color(0.35f, 1.0f, 0.6f));
        }

        private void BuildWheelUI()
        {
            Transform root = transform;

            // Circular Radial Modal Wheel Container (Opened when interacting with Fridge or Shelf!)
            wheelRoot = new GameObject("RadialWheelContainer");
            wheelRoot.transform.SetParent(root, false);
            wheelRect = wheelRoot.AddComponent<RectTransform>();
            float defaultAx = GetWheelAnchorX("linh");
            wheelRect.anchorMin = new Vector2(defaultAx, 0.5f);
            wheelRect.anchorMax = new Vector2(defaultAx, 0.5f);
            wheelRect.pivot = new Vector2(0.5f, 0.5f);
            wheelRect.anchoredPosition = Vector2.zero;
            wheelRect.sizeDelta = new Vector2(WHEEL_SIZE, WHEEL_SIZE);
            wheelRect.localScale = Vector3.one * WHEEL_SCALE;

            // Dark radial backdrop circle
            GameObject bgCircle = new GameObject("BackdropCircle");
            bgCircle.transform.SetParent(wheelRoot.transform, false);
            var bgRect = bgCircle.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgCircle.AddComponent<Image>();
            bgImg.sprite = KitchenHUD.GetCircleSprite();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            // Storage Header Label at Top of Wheel
            GameObject hdrObj = new GameObject("StorageHeader");
            hdrObj.transform.SetParent(wheelRoot.transform, false);
            var hdrRect = hdrObj.AddComponent<RectTransform>();
            hdrRect.anchorMin = new Vector2(0.5f, 1f);
            hdrRect.anchorMax = new Vector2(0.5f, 1f);
            hdrRect.pivot = new Vector2(0.5f, 0.5f);
            hdrRect.anchoredPosition = new Vector2(0f, 26f);
            hdrRect.sizeDelta = new Vector2(WHEEL_SIZE, 36f);
            storageHeaderText = hdrObj.AddComponent<Text>();
            storageHeaderText.font = KitchenHUD.GetDefaultFont();
            storageHeaderText.text = "FRIDGE STORAGE";
            storageHeaderText.fontSize = 22;
            storageHeaderText.fontStyle = FontStyle.Bold;
            storageHeaderText.alignment = TextAnchor.MiddleCenter;
            storageHeaderText.color = new Color(1.0f, 0.88f, 0.25f);

            // Center Hub (Icon & Title of selected item + Close on click)
            GameObject hubObj = new GameObject("CenterHub");
            hubObj.transform.SetParent(wheelRoot.transform, false);
            var hRect = hubObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0.5f);
            hRect.anchorMax = new Vector2(0.5f, 0.5f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(132f, 132f);
            var hImg = hubObj.AddComponent<Image>();
            hImg.sprite = KitchenHUD.GetCircleSprite();
            hImg.color = new Color(0.12f, 0.15f, 0.20f, 0.98f);
            var hBtn = hubObj.AddComponent<Button>();
            hBtn.targetGraphic = hImg;
            hBtn.onClick.AddListener(CloseWheel);

            // Center Icon
            GameObject ciObj = new GameObject("CenterIcon");
            ciObj.transform.SetParent(hubObj.transform, false);
            var ciRect = ciObj.AddComponent<RectTransform>();
            ciRect.anchorMin = new Vector2(0.5f, 0.5f);
            ciRect.anchorMax = new Vector2(0.5f, 0.5f);
            ciRect.pivot = new Vector2(0.5f, 0.5f);
            ciRect.anchoredPosition = new Vector2(0f, 16f);
            ciRect.sizeDelta = new Vector2(66f, 66f);
            centerIconImage = ciObj.AddComponent<Image>();

            // Center Label
            GameObject clObj = new GameObject("CenterLabel");
            clObj.transform.SetParent(hubObj.transform, false);
            var clRect = clObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0f, 0f);
            clRect.anchorMax = new Vector2(1f, 0.38f);
            clRect.pivot = new Vector2(0.5f, 0f);
            clRect.anchoredPosition = Vector2.zero;
            clRect.sizeDelta = Vector2.zero;
            centerTitleText = clObj.AddComponent<Text>();
            centerTitleText.font = KitchenHUD.GetDefaultFont();
            centerTitleText.text = "SELECT";
            centerTitleText.fontSize = 17;
            centerTitleText.resizeTextForBestFit = true;
            centerTitleText.resizeTextMinSize = 10;
            centerTitleText.resizeTextMaxSize = 17;
            centerTitleText.alignment = TextAnchor.MiddleCenter;
            centerTitleText.color = Color.white;

            RebuildSlices("FRIDGE STORAGE");
            wheelRoot.SetActive(false);
        }

        private void RebuildSlices(string headerTitle)
        {
            if (storageHeaderText != null)
                storageHeaderText.text = headerTitle;

            foreach (var go in sliceObjects)
            {
                if (go != null) Destroy(go);
            }
            sliceObjects.Clear();
            sliceButtons.Clear();
            sliceHighlights.Clear();

            if (wheelRoot == null || currentCatalog == null) return;

            float radius = SLICE_RADIUS;
            float stepAngle = (2f * Mathf.PI) / currentCatalog.Length;

            for (int i = 0; i < currentCatalog.Length; i++)
            {
                var item = currentCatalog[i];
                float angle = i * stepAngle;
                float posX = Mathf.Cos(angle) * radius;
                float posY = Mathf.Sin(angle) * radius;

                GameObject sliceObj = new GameObject($"Slice_{item.itemId}");
                sliceObj.transform.SetParent(wheelRoot.transform, false);
                sliceObjects.Add(sliceObj);
                var sRect = sliceObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.5f);
                sRect.anchorMax = new Vector2(0.5f, 0.5f);
                sRect.pivot = new Vector2(0.5f, 0.5f);
                sRect.anchoredPosition = new Vector2(posX, posY);
                sRect.sizeDelta = new Vector2(SLICE_SIZE, SLICE_SIZE);

                var sImg = sliceObj.AddComponent<Image>();
                sImg.sprite = KitchenHUD.GetCircleSprite();
                sImg.color = new Color(0.18f, 0.22f, 0.28f, 0.78f);
                sliceHighlights.Add(sImg);

                var sBtn = sliceObj.AddComponent<Button>();
                sBtn.targetGraphic = sImg;
                string capturedId = item.itemId;
                sBtn.onClick.AddListener(() =>
                {
                    SelectItem(capturedId);
                    CloseWheel();
                });
                sliceButtons.Add(sBtn);

                // Icon inside slice
                GameObject icoObj = new GameObject("Icon");
                icoObj.transform.SetParent(sliceObj.transform, false);
                var iRect = icoObj.AddComponent<RectTransform>();
                iRect.anchorMin = new Vector2(0.5f, 0.5f);
                iRect.anchorMax = new Vector2(0.5f, 0.5f);
                iRect.pivot = new Vector2(0.5f, 0.5f);
                iRect.anchoredPosition = new Vector2(0f, 11f);
                iRect.sizeDelta = new Vector2(54f, 54f);
                var iImg = icoObj.AddComponent<Image>();
                Sprite itemSpr = PlayerController.LoadItemSprite(item.iconSpriteName);
                if (itemSpr != null) iImg.sprite = itemSpr;

                // Label inside slice
                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(sliceObj.transform, false);
                var lRect = lblObj.AddComponent<RectTransform>();
                lRect.anchorMin = new Vector2(0f, 0f);
                lRect.anchorMax = new Vector2(1f, 0.36f);
                lRect.pivot = new Vector2(0.5f, 0f);
                lRect.anchoredPosition = Vector2.zero;
                lRect.sizeDelta = Vector2.zero;
                var lText = lblObj.AddComponent<Text>();
                lText.font = KitchenHUD.GetDefaultFont();
                lText.text = item.displayName.Split(' ')[0].ToUpper();
                lText.fontSize = 16;
                lText.fontStyle = FontStyle.Bold;
                lText.resizeTextForBestFit = true;
                lText.resizeTextMinSize = 10;
                lText.resizeTextMaxSize = 16;
                lText.alignment = TextAnchor.MiddleCenter;
                lText.color = item.itemColor;
            }
        }
    }
}
