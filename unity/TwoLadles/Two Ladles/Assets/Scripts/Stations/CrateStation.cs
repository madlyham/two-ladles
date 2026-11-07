using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class CrateStation : StationBase
    {
        public string dispensedItemId;

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (playerItem == null)
            {
                if (string.IsNullOrEmpty(dispensedItemId))
                    return (false, "Crate is empty.");

                string dName = dispensedItemId;
                string vType = "none";
                if (GameData.Instance != null && GameData.Instance.ingredients.TryGetValue(dispensedItemId, out var ing))
                {
                    dName = ing.displayName;
                }
                playerItem = new KitchenItem(dispensedItemId, dName, vType, QualityTier.STANDARD);
                return (true, $"Grabbed {playerItem.displayName} from crate.");
            }

            if (playerItem.itemId == dispensedItemId)
            {
                return (false, "Already holding this ingredient.");
            }

            return (false, "Hands full.");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return InteractPrimary(ref playerItem, peerId, characterId);
        }
    }
}
