using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    /// <summary>
    /// An empty holding counter / self-assembly station.
    /// Allows chefs to place any carried ingredient or dish down, pick it back up,
    /// or press [E] / INTERACT while holding a compatible ingredient to automatically assemble!
    /// </summary>
    public class CounterStation : StationBase
    {
        private void Awake()
        {
            if (string.IsNullOrEmpty(stationId))
                stationId = "counter_station";
            if (string.IsNullOrEmpty(displayName))
                displayName = "HOLDING COUNTER";
            if (string.IsNullOrEmpty(zone))
                zone = "shared";
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            // Case 1: Player holding item, counter is empty -> place item on counter
            if (playerItem != null && heldItem == null)
            {
                heldItem = playerItem.Clone();
                playerItem = null;
                RefreshStationVisuals();
                return (true, $"Placed {heldItem.displayName} on counter.");
            }

            // Case 2: Counter has item, player hands empty -> pick up item
            if (playerItem == null && heldItem != null)
            {
                playerItem = heldItem.Clone();
                heldItem = null;
                RefreshStationVisuals();
                return (true, $"Picked up {playerItem.displayName} from counter.");
            }

            // Case 3: Both have items -> Self-Assemble if compatible, otherwise swap!
            if (playerItem != null && heldItem != null)
            {
                if (GameData.TryCombineItems(heldItem, playerItem, out var combined))
                {
                    heldItem = null;
                    playerItem = combined;
                    RefreshStationVisuals();
                    return (true, $"Assembled {combined.displayName}!");
                }

                // Identical items: swapping would change nothing and can trap bots in an endless loop
                if (heldItem.itemId == playerItem.itemId)
                {
                    return (false, $"Counter already holds {heldItem.displayName}.");
                }

                var temp = playerItem.Clone();
                playerItem = heldItem.Clone();
                heldItem = temp;
                RefreshStationVisuals();
                return (true, $"Swapped {heldItem.displayName} with {playerItem.displayName}.");
            }

            return (false, "Counter is empty.");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            // Secondary interaction behaves like Primary on simple holding counters
            return InteractPrimary(ref playerItem, peerId, characterId);
        }
    }
}
