using UnityEngine;
using TwoLadles.Data;

namespace TwoLadles.Stations
{
    /// <summary>
    /// An empty holding counter / prep table station.
    /// Allows chefs to place any carried ingredient, dish, plate, or bowl down
    /// to free their hands, and pick it back up or swap items.
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
                return (true, $"Placed {heldItem.displayName} on counter.");
            }

            // Case 2: Counter has item, player hands empty -> pick up item
            if (playerItem == null && heldItem != null)
            {
                playerItem = heldItem.Clone();
                heldItem = null;
                return (true, $"Picked up {playerItem.displayName} from counter.");
            }

            // Case 3: Both have items -> swap items!
            if (playerItem != null && heldItem != null)
            {
                var temp = playerItem.Clone();
                playerItem = heldItem.Clone();
                heldItem = temp;
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
