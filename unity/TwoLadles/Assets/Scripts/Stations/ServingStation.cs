using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class ServingStation : StationBase
    {
        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (playerItem == null)
                return (false, "Bring a completed dish to the serving window.");

            if (ShiftManager.Instance == null || !ShiftManager.Instance.shiftActive)
                return (false, "No active shift.");

            var res = ShiftManager.Instance.DeliverDish(playerItem, peerId);
            if (res.success)
            {
                playerItem = null;
                return (true, $"Dish Served! +{res.scoreAwarded} points.");
            }

            return (false, "Dish does not match any active customer order.");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return (false, "Secondary action not applicable to Serving Window.");
        }
    }
}
