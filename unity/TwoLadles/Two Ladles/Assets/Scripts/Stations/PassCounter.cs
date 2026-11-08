using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

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

        public bool PlaceItem(int slotIndex, KitchenItem item, int peerId, float timestamp)
        {
            InitSlots();
            if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return false;
            if (slots[slotIndex] == null) slots[slotIndex] = new PassSlot();
            if (slots[slotIndex].item != null) return false;

            slots[slotIndex].item = item;
            slots[slotIndex].placedByPeer = peerId;
            slots[slotIndex].placedAtTime = timestamp;
            return true;
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

            return (item, handoffAwarded);
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            float now = Time.time;

            if (playerItem != null)
            {
                // Place into first open slot
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

                        return (true, $"Passed item to slot {i + 1}.");
                    }
                }
                return (false, "Pass Counter is full (all 3 slots occupied).");
            }
            else
            {
                // Retrieve from first occupied slot
                for (int i = 0; i < NUM_SLOTS; i++)
                {
                    if (slots[i].item != null)
                    {
                        var res = RetrieveItem(i, peerId, now);
                        playerItem = res.item;

                        if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 5)
                        {
                            ShiftManager.Instance.AdvanceTutorialStep(6);
                        }

                        string msg = res.handoffAwarded ? "Coordinated Handoff! +15 Kitchen Flow!" : "Retrieved item from Pass Counter.";
                        return (true, msg);
                    }
                }
                return (false, "Pass Counter is empty.");
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return (false, "Secondary action not applicable to Pass Counter.");
        }
    }
}
