using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Player;

namespace TwoLadles.Stations
{
    public class PassSlot
    {
        public KitchenItem item;
        public int placedByPeer;
        public float placedAtTime;
    }

    public class PassCounter : StationBase
    {
        public const int NUM_SLOTS = 3;
        public const float HANDOFF_WINDOW_SECONDS = 4.0f;

        public PassSlot[] slots = new PassSlot[NUM_SLOTS]
        {
            new PassSlot(),
            new PassSlot(),
            new PassSlot()
        };

        private SpriteRenderer[] _slotRenderers = new SpriteRenderer[NUM_SLOTS];

        private void Awake()
        {
            InitSlots();
        }

        public void InitSlots()
        {
            for (int i = 0; i < NUM_SLOTS; i++)
            {
                if (slots[i] == null)
                    slots[i] = new PassSlot();
            }
        }

        public override void RefreshStationVisuals()
        {
            InitSlots();
            float[] zOffsets = new float[] { -0.55f, 0.0f, 0.55f };
            for (int i = 0; i < NUM_SLOTS; i++)
            {
                if (_slotRenderers[i] == null)
                {
                    Transform existing = transform.Find($"PassSlotSprite_{i}");
                    if (existing != null)
                    {
                        _slotRenderers[i] = existing.GetComponent<SpriteRenderer>();
                    }
                    else
                    {
                        GameObject go = new GameObject($"PassSlotSprite_{i}");
                        go.transform.SetParent(transform, false);
                        go.transform.localPosition = new Vector3(0.0f, 0.94f, zOffsets[i] - 0.18f);
                        go.transform.localRotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                        go.transform.localScale = Vector3.one * 0.68f;
                        _slotRenderers[i] = go.AddComponent<SpriteRenderer>();
                        _slotRenderers[i].sortingOrder = 1200 + i;
                    }
                }

                var sr = _slotRenderers[i];
                if (sr != null)
                {
                    if (slots[i] != null && slots[i].item != null)
                    {
                        sr.gameObject.SetActive(true);
                        Sprite spr = PlayerController.LoadItemSprite(slots[i].item.itemId);
                        if (spr != null) sr.sprite = spr;
                        sr.sortingOrder = 1200 + i;
                    }
                    else
                    {
                        sr.gameObject.SetActive(false);
                    }
                }
            }
        }

        public bool PlaceItem(int slotIndex, KitchenItem item, int peerId, float timestamp)
        {
            InitSlots();
            if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return false;
            if (slots[slotIndex] == null) slots[slotIndex] = new PassSlot();
            if (slots[slotIndex].item != null) return false;

            slots[slotIndex].item = item;
            slots[slotIndex].placedByPeer = peerId;
            slots[slotIndex].placedAtTime = timestamp;
            RefreshStationVisuals();
            return true;
        }

        public bool HasEmptySlot()
        {
            InitSlots();
            for (int i = 0; i < NUM_SLOTS; i++)
            {
                if (slots[i] == null || slots[i].item == null) return true;
            }
            return false;
        }

        public (KitchenItem item, bool handoffAwarded) RetrieveItem(int slotIndex, int retrieverPeer, float currentTime)
        {
            InitSlots();
            if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return (null, false);
            var slot = slots[slotIndex];
            if (slot == null || slot.item == null) return (null, false);

            var item = slot.item;
            int placerPeer = slot.placedByPeer;
            float placedTime = slot.placedAtTime;

            slot.item = null;
            slot.placedByPeer = 0;
            slot.placedAtTime = 0.0f;

            bool handoffAwarded = false;
            float elapsed = currentTime - placedTime;

            // Anti-exploit check
            if (placerPeer != retrieverPeer && elapsed <= HANDOFF_WINDOW_SECONDS)
            {
                if (ShiftManager.Instance != null)
                {
                    handoffAwarded = ShiftManager.Instance.RecordHandoff(item.itemId, placerPeer, retrieverPeer, elapsed);
                }
            }

            RefreshStationVisuals();
            return (item, handoffAwarded);
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            float now = Time.time;

            if (playerItem != null)
            {
                // First check if carried item can self-assemble with an item waiting on the Pass Counter!
                for (int i = 0; i < NUM_SLOTS; i++)
                {
                    if (slots[i] != null && slots[i].item != null &&
                        GameData.TryCombineItems(slots[i].item, playerItem, out var combined))
                    {
                        var res = RetrieveItem(i, peerId, now);
                        playerItem = combined;
                        string bonusMsg = res.handoffAwarded ? " (+15 Flow!)" : "";
                        return (true, $"Assembled {combined.displayName}!{bonusMsg}");
                    }
                }

                for (int i = 0; i < NUM_SLOTS; i++)
                {
                    if (slots[i].item == null)
                    {
                        PlaceItem(i, playerItem, peerId, now);
                        playerItem = null;

                        if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 4)
                        {
                            ShiftManager.Instance.AdvanceTutorialStep(5);
                        }

                        return (true, $"Passed!");
                    }
                }
                return (false, "Pass Counter full.");
            }
            else
            {
                // First pass: retrieve an item placed by partner chef (immediate handoff)
                // Intelligent selection:
                // Linh prioritizes completed dishes or needed meats (bbq_beef, bulgogi)
                // Jiwon prioritizes raw prep ingredients
                int chosenSlot = -1;
                for (int i = 0; i < NUM_SLOTS; i++)
                {
                    if (slots[i].item != null && slots[i].placedByPeer != peerId)
                    {
                        if (characterId == "linh" && (GameData.IsCompletedDish(slots[i].item.itemId) || slots[i].item.itemId == "bbq_beef" || slots[i].item.itemId == "bulgogi"))
                        {
                            chosenSlot = i;
                            break;
                        }
                        if (characterId == "jiwon" && !GameData.IsCompletedDish(slots[i].item.itemId))
                        {
                            chosenSlot = i;
                            break;
                        }
                    }
                }

                if (chosenSlot < 0)
                {
                    for (int i = 0; i < NUM_SLOTS; i++)
                    {
                        if (slots[i].item != null && slots[i].placedByPeer != peerId)
                        {
                            chosenSlot = i;
                            break;
                        }
                    }
                }

                if (chosenSlot >= 0)
                {
                    var res = RetrieveItem(chosenSlot, peerId, now);
                    playerItem = res.item;

                    if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 5)
                    {
                        ShiftManager.Instance.AdvanceTutorialStep(6);
                    }

                    string msg = res.handoffAwarded ? "Handoff! +15 Flow!" : "Retrieved.";
                    return (true, msg);
                }

                // Second pass: if only items placed by self exist, only retrieve after 5.0 seconds (prevents ping-pong loop)
                for (int i = 0; i < NUM_SLOTS; i++)
                {
                    if (slots[i].item != null && slots[i].placedByPeer == peerId)
                    {
                        if ((now - slots[i].placedAtTime) >= 5.0f)
                        {
                            var res = RetrieveItem(i, peerId, now);
                            playerItem = res.item;
                            return (true, "Retrieved.");
                        }
                        else
                        {
                            return (false, "Item waiting for partner chef.");
                        }
                    }
                }
                return (false, "Empty.");
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return InteractPrimary(ref playerItem, peerId, characterId);
        }
    }
}
