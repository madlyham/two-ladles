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

        // Fridge holds: Raw Meat, Radish & Carrot, Fresh Egg, Toboki Portion, and Baguette
        private static readonly WheelItem[] FRIDGE_ITEMS = new WheelItem[]
        {
            new WheelItem("unprocessed_raw_meat", "Raw Meat", "unprocessed_raw_meat", new Color(0.95f, 0.45f, 0.45f)),
            new WheelItem("radish_carrot", "Radish & Carrot", "radish_carrot", new Color(0.98f, 0.65f, 0.25f)),
            new WheelItem("egg_raw", "Fresh Egg", "egg_raw", new Color(0.98f, 0.92f, 0.72f)),
            new WheelItem("tteok_portioned", "Toboki Portion", "tteok_portioned", new Color(0.96f, 0.96f, 0.94f)),
            new WheelItem("baguette", "Baguette", "baguette", new Color(0.90f, 0.70f, 0.36f))
        };

        // Shelf holds: Gochujang, Flour, Rice Portion, and Toboki Portion (No bowls or plates!)
        private static readonly WheelItem[] SHELF_ITEMS = new WheelItem[]
        {
            new WheelItem("gochujang", "Gochujang", "gochujang", new Color(0.92f, 0.25f, 0.20f)),
            new WheelItem("flour", "Flour", "flour", new Color(0.96f, 0.94f, 0.88f)),
            new WheelItem("rice_portioned", "Rice Portion", "rice_portioned", new Color(0.98f, 0.98f, 0.98f)),
            new WheelItem("tteok_portioned", "Toboki Portion", "tteok_portioned", new Color(0.96f, 0.96f, 0.94f))
        };

        private WheelItem[] currentCatalog = FRIDGE_ITEMS;
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
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                CloseWheel();
                return;
            }

            // Mouse direction highlight when wheel is open
            if (IsOpen && currentCatalog != null && currentCatalog.Length > 0)
            {
                Vector2 mouseOffset = (Vector2)Input.mousePosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (mouseOffset.magnitude > 36f)
                {
                    float angle = Mathf.Atan2(mouseOffset.y, mouseOffset.x) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;

                    float sliceAngle = 360f / currentCatalog.Length;
                    int idx = Mathf.RoundToInt(angle / sliceAngle) % currentCatalog.Length;
                    HighlightSlice(idx);
                }
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
            currentCatalog = isFridge ? FRIDGE_ITEMS : SHELF_ITEMS;
            RebuildSlices(isFridge ? "FRIDGE (MEAT • VEG • EGG • TOBOKI • BAGUETTE)" : "SHELF (GOCHUJANG • FLOUR • RICE • TOBOKI)");
            if (wheelRoot != null)
            {
                wheelRoot.SetActive(true);
                HighlightSlice(0);
            }
        }

        public void CloseWheel()
        {
            if (wheelRoot != null)
            {
                wheelRoot.SetActive(false);
            }
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
                centerTitleText.text = item.displayName.ToUpper();
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
            var wrRect = wheelRoot.AddComponent<RectTransform>();
            wrRect.anchorMin = new Vector2(0.5f, 0.5f);
            wrRect.anchorMax = new Vector2(0.5f, 0.5f);
            wrRect.pivot = new Vector2(0.5f, 0.5f);
            wrRect.anchoredPosition = Vector2.zero;
            wrRect.sizeDelta = new Vector2(420f, 420f);

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
            hdrRect.anchoredPosition = new Vector2(0f, 22f);
            hdrRect.sizeDelta = new Vector2(420f, 28f);
            storageHeaderText = hdrObj.AddComponent<Text>();
            storageHeaderText.font = KitchenHUD.GetDefaultFont();
            storageHeaderText.text = "FRIDGE STORAGE";
            storageHeaderText.fontSize = 14;
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
            hRect.sizeDelta = new Vector2(114f, 114f);
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
            ciRect.anchoredPosition = new Vector2(0f, 12f);
            ciRect.sizeDelta = new Vector2(48f, 48f);
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
            centerTitleText.fontSize = 11;
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

            float radius = 142f;
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
                sRect.sizeDelta = new Vector2(74f, 74f);

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
                iRect.anchoredPosition = new Vector2(0f, 8f);
                iRect.sizeDelta = new Vector2(36f, 36f);
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
                lText.fontSize = 10;
                lText.alignment = TextAnchor.MiddleCenter;
                lText.color = item.itemColor;
            }
        }
    }
}
