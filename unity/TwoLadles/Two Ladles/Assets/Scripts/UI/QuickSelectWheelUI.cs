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

        private static readonly WheelItem[] AVAILABLE_ITEMS = new WheelItem[]
        {
            new WheelItem("pork_raw", "Pork Belly", "pork_belly_raw", new Color(0.92f, 0.45f, 0.45f)),
            new WheelItem("beef_raw", "Beef Shank", "beef_raw", new Color(0.78f, 0.22f, 0.22f)),
            new WheelItem("tteok_raw", "Rice Cakes", "tteok_portioned", new Color(0.95f, 0.95f, 0.92f)),
            new WheelItem("noodles_raw", "Rice Noodles", "noodles_raw", new Color(0.92f, 0.90f, 0.82f)),
            new WheelItem("baguette", "Baguette", "baguette_bread", new Color(0.86f, 0.65f, 0.35f)),
            new WheelItem("daikon_carrot_raw", "Daikon & Carrot", "pickled_veg", new Color(0.95f, 0.62f, 0.22f)),
            new WheelItem("kimchi_raw", "Aged Kimchi", "kimchi_slaw", new Color(0.88f, 0.30f, 0.18f)),
            new WheelItem("eomuk_raw", "Fish Cakes", "fish_cake_raw", new Color(0.85f, 0.75f, 0.55f))
        };

        private GameObject wheelRoot;
        private List<Button> sliceButtons = new List<Button>();
        private List<Image> sliceHighlights = new List<Image>();
        private Text centerTitleText;
        private Image centerIconImage;
        private Button mobileToggleBtn;

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
            // Desktop Key Trigger: Hold TAB to keep wheel open, release to select!
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                OpenWheel();
            }
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                if (IsOpen)
                {
                    if (highlightedIndex >= 0 && highlightedIndex < AVAILABLE_ITEMS.Length)
                    {
                        SelectItem(AVAILABLE_ITEMS[highlightedIndex].itemId);
                    }
                    CloseWheel();
                }
            }

            // Keyboard / Mouse direction highlight when wheel is open
            if (IsOpen)
            {
                Vector2 mouseOffset = (Vector2)Input.mousePosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (mouseOffset.magnitude > 40f)
                {
                    float angle = Mathf.Atan2(mouseOffset.y, mouseOffset.x) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;

                    // 8 slices: 45 degrees per slice, centered on 0, 45, 90, etc.
                    float sliceAngle = 360f / AVAILABLE_ITEMS.Length;
                    int idx = Mathf.RoundToInt(angle / sliceAngle) % AVAILABLE_ITEMS.Length;
                    HighlightSlice(idx);
                }
            }
        }

        public void ToggleWheel()
        {
            if (IsOpen) CloseWheel();
            else OpenWheel();
        }

        public void OpenWheel()
        {
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
            if (index < 0 || index >= AVAILABLE_ITEMS.Length) return;
            highlightedIndex = index;

            for (int i = 0; i < sliceHighlights.Count; i++)
            {
                if (sliceHighlights[i] != null)
                {
                    sliceHighlights[i].color = (i == index)
                        ? new Color(0.95f, 0.75f, 0.2f, 0.85f)
                        : new Color(0.2f, 0.22f, 0.28f, 0.65f);
                }
            }

            var item = AVAILABLE_ITEMS[index];
            if (centerTitleText != null)
            {
                centerTitleText.text = item.displayName.ToUpper();
                centerTitleText.color = item.itemColor;
            }
            if (centerIconImage != null)
            {
                Sprite spr = Resources.Load<Sprite>($"Sprites/Items/{item.iconSpriteName}");
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

            WheelItem chosen = Array.Find(AVAILABLE_ITEMS, x => x.itemId == itemId);
            string dName = !string.IsNullOrEmpty(chosen.displayName) ? chosen.displayName : itemId;
            localChef.carriedItem = new KitchenItem(itemId, dName, "none", QualityTier.STANDARD);
            localChef.RefreshVisuals();

            if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 2 && itemId == "pork_raw")
            {
                ShiftManager.Instance.AdvanceTutorialStep(3);
            }

            KitchenController.Instance.TriggerStatusToast($"Picked {dName}!", new Color(0.35f, 1.0f, 0.6f));
        }

        private void BuildWheelUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            Transform root = transform;

            // 1. Mobile Floating Quick-Select Button (Bottom Left, right of D-pad)
            GameObject mobBtnObj = new GameObject("MobileWheelButton");
            mobBtnObj.transform.SetParent(root, false);
            var mbRect = mobBtnObj.AddComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(0f, 0f);
            mbRect.anchorMax = new Vector2(0f, 0f);
            mbRect.pivot = new Vector2(0.5f, 0.5f);
            mbRect.anchoredPosition = new Vector2(178f, 94f);
            mbRect.sizeDelta = new Vector2(74f, 74f);

            var mbImg = mobBtnObj.AddComponent<Image>();
            mbImg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
            mobileToggleBtn = mobBtnObj.AddComponent<Button>();
            mobileToggleBtn.targetGraphic = mbImg;
            mobileToggleBtn.onClick.AddListener(ToggleWheel);

            // Button label
            GameObject mbTextObj = new GameObject("Text");
            mbTextObj.transform.SetParent(mobBtnObj.transform, false);
            var mtRect = mbTextObj.AddComponent<RectTransform>();
            mtRect.anchorMin = Vector2.zero;
            mtRect.anchorMax = Vector2.one;
            mtRect.sizeDelta = Vector2.zero;
            var mtText = mbTextObj.AddComponent<Text>();
            mtText.font = KitchenHUD.GetDefaultFont();
            mtText.text = "INGREDIENT\nWHEEL\n[TAB]";
            mtText.fontSize = 11;
            mtText.fontStyle = FontStyle.Bold;
            mtText.alignment = TextAnchor.MiddleCenter;
            mtText.color = new Color(0.95f, 0.82f, 0.40f);

            // 2. Circular Radial Modal Wheel Container
            wheelRoot = new GameObject("RadialWheelContainer");
            wheelRoot.transform.SetParent(root, false);
            var wrRect = wheelRoot.AddComponent<RectTransform>();
            wrRect.anchorMin = new Vector2(0.5f, 0.5f);
            wrRect.anchorMax = new Vector2(0.5f, 0.5f);
            wrRect.pivot = new Vector2(0.5f, 0.5f);
            wrRect.anchoredPosition = Vector2.zero;
            wrRect.sizeDelta = new Vector2(400f, 400f);

            // Dark radial backdrop circle
            GameObject bgCircle = new GameObject("BackdropCircle");
            bgCircle.transform.SetParent(wheelRoot.transform, false);
            var bgRect = bgCircle.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgCircle.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.07f, 0.10f, 0.88f);

            // 3. Center Hub (Icon & Title of selected item)
            GameObject hubObj = new GameObject("CenterHub");
            hubObj.transform.SetParent(wheelRoot.transform, false);
            var hRect = hubObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0.5f);
            hRect.anchorMax = new Vector2(0.5f, 0.5f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(104f, 104f);
            var hImg = hubObj.AddComponent<Image>();
            hImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            // Center Icon
            GameObject ciObj = new GameObject("CenterIcon");
            ciObj.transform.SetParent(hubObj.transform, false);
            var ciRect = ciObj.AddComponent<RectTransform>();
            ciRect.anchorMin = new Vector2(0.5f, 0.5f);
            ciRect.anchorMax = new Vector2(0.5f, 0.5f);
            ciRect.pivot = new Vector2(0.5f, 0.5f);
            ciRect.anchoredPosition = new Vector2(0f, 10f);
            ciRect.sizeDelta = new Vector2(48f, 48f);
            centerIconImage = ciObj.AddComponent<Image>();

            // Center Label
            GameObject clObj = new GameObject("CenterLabel");
            clObj.transform.SetParent(hubObj.transform, false);
            var clRect = clObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0f, 0f);
            clRect.anchorMax = new Vector2(1f, 0.35f);
            clRect.pivot = new Vector2(0.5f, 0f);
            clRect.anchoredPosition = Vector2.zero;
            clRect.sizeDelta = Vector2.zero;
            centerTitleText = clObj.AddComponent<Text>();
            centerTitleText.font = KitchenHUD.GetDefaultFont();
            centerTitleText.text = "SELECT";
            centerTitleText.fontSize = 11;
            centerTitleText.fontStyle = FontStyle.Bold;
            centerTitleText.alignment = TextAnchor.MiddleCenter;
            centerTitleText.color = Color.white;

            // 4. Slices arranged radially
            float radius = 138f;
            float stepAngle = (2f * Mathf.PI) / AVAILABLE_ITEMS.Length;

            for (int i = 0; i < AVAILABLE_ITEMS.Length; i++)
            {
                int index = i;
                var item = AVAILABLE_ITEMS[i];
                float angle = i * stepAngle; // Starting from 0° (right) counter-clockwise
                float posX = Mathf.Cos(angle) * radius;
                float posY = Mathf.Sin(angle) * radius;

                GameObject sliceObj = new GameObject($"Slice_{item.itemId}");
                sliceObj.transform.SetParent(wheelRoot.transform, false);
                var sRect = sliceObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.5f);
                sRect.anchorMax = new Vector2(0.5f, 0.5f);
                sRect.pivot = new Vector2(0.5f, 0.5f);
                sRect.anchoredPosition = new Vector2(posX, posY);
                sRect.sizeDelta = new Vector2(66f, 66f);

                var sImg = sliceObj.AddComponent<Image>();
                sImg.color = new Color(0.2f, 0.22f, 0.28f, 0.65f);
                sliceHighlights.Add(sImg);

                var sBtn = sliceObj.AddComponent<Button>();
                sBtn.targetGraphic = sImg;
                sBtn.onClick.AddListener(() =>
                {
                    SelectItem(item.itemId);
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
                iRect.anchoredPosition = new Vector2(0f, 6f);
                iRect.sizeDelta = new Vector2(34f, 34f);
                var iImg = icoObj.AddComponent<Image>();
                Sprite itemSpr = Resources.Load<Sprite>($"Sprites/Items/{item.iconSpriteName}");
                if (itemSpr != null) iImg.sprite = itemSpr;

                // Label inside slice
                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(sliceObj.transform, false);
                var lRect = lblObj.AddComponent<RectTransform>();
                lRect.anchorMin = new Vector2(0f, 0f);
                lRect.anchorMax = new Vector2(1f, 0.35f);
                lRect.pivot = new Vector2(0.5f, 0f);
                lRect.anchoredPosition = Vector2.zero;
                lRect.sizeDelta = Vector2.zero;
                var lText = lblObj.AddComponent<Text>();
                lText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                lText.text = item.displayName.Split(' ')[0]; // Short 1-word title
                lText.fontSize = 10;
                lText.fontStyle = FontStyle.Bold;
                lText.alignment = TextAnchor.MiddleCenter;
                lText.color = item.itemColor;
            }

            wheelRoot.SetActive(false);
        }
    }
}
