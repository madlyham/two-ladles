using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwoLadles.AI
{
    [Serializable]
    public class ChefIntent
    {
        public int peerId;
        public string characterId;
        public string currentTask;
        public string targetStationId;
        public string targetRecipeId;
        public float updatedAt;
    }

    [Serializable]
    public class HandoffNotification
    {
        public int slotIndex;
        public string itemId;
        public int senderPeer;
        public int targetPeer;
        public string recipeId;
        public float postedTime;
        public bool claimed;
    }

    /// <summary>
    /// Inter-Bot Communication Hub & Shared Blackboard.
    /// Allows AI Linh and AI Jiwon to coordinate 2-sided recipes deterministically,
    /// announce ingredient handoffs across the Pass Counter, avoid redundant tasks,
    /// and claim the +15 Flow handoff bonuses within the 4000ms window.
    /// </summary>
    public static class BotCommunicationHub
    {
        private static Dictionary<int, ChefIntent> intents = new Dictionary<int, ChefIntent>();
        private static List<HandoffNotification> activeHandoffs = new List<HandoffNotification>();

        public static void Reset()
        {
            intents.Clear();
            activeHandoffs.Clear();
        }

        public static void BroadcastIntent(int peerId, string charId, string task, string stationId, string recipeId)
        {
            intents[peerId] = new ChefIntent
            {
                peerId = peerId,
                characterId = charId,
                currentTask = task,
                targetStationId = stationId,
                targetRecipeId = recipeId,
                updatedAt = Time.time
            };
        }

        public static ChefIntent GetPartnerIntent(int myPeerId)
        {
            int partnerPeer = (myPeerId == 1) ? 2 : 1;
            intents.TryGetValue(partnerPeer, out var intent);
            return intent;
        }

        public static void PostHandoff(int slotIndex, string itemId, int senderPeer, string recipeId)
        {
            int targetPeer = (senderPeer == 1) ? 2 : 1;
            // Remove previous handoff in same slot
            activeHandoffs.RemoveAll(x => x.slotIndex == slotIndex);

            activeHandoffs.Add(new HandoffNotification
            {
                slotIndex = slotIndex,
                itemId = itemId,
                senderPeer = senderPeer,
                targetPeer = targetPeer,
                recipeId = recipeId,
                postedTime = Time.time,
                claimed = false
            });

            Debug.Log($"[BotComms] Chef Peer {senderPeer} posted '{itemId}' on Pass Slot {slotIndex} for Peer {targetPeer} (Recipe: {recipeId})");
        }

        public static HandoffNotification GetPendingHandoffFor(int receiverPeer)
        {
            for (int i = 0; i < activeHandoffs.Count; i++)
            {
                var h = activeHandoffs[i];
                if (!h.claimed && h.targetPeer == receiverPeer)
                {
                    return h;
                }
            }
            return null;
        }

        public static void ClaimHandoff(int slotIndex, int receiverPeer)
        {
            for (int i = 0; i < activeHandoffs.Count; i++)
            {
                var h = activeHandoffs[i];
                if (h.slotIndex == slotIndex && h.targetPeer == receiverPeer)
                {
                    h.claimed = true;
                    Debug.Log($"[BotComms] Chef Peer {receiverPeer} claimed '{h.itemId}' from Pass Slot {slotIndex}!");
                    break;
                }
            }
            activeHandoffs.RemoveAll(x => x.claimed);
        }
    }
}
