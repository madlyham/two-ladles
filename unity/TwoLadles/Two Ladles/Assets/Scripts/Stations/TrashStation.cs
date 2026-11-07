using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class TrashStation : StationBase
    {
        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (playerItem != null)
            {
                string discarded = playerItem.displayName;
                playerItem = null;
                return (true, $"Discarded {discarded} into trash.");
            }
            return (false, "Hands are empty.");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return InteractPrimary(ref playerItem, peerId, characterId);
        }
    }
}
