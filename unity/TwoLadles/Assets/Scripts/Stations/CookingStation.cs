using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class CookingStation : StationBase
    {
        public string applianceId;
        public ApplianceSpec applianceSpec;
        public float cookingElapsedSeconds = 0.0f;
        public CookingState currentState = CookingState.RAW;

        public bool PlaceIngredient(KitchenItem item)
        {
            if (heldItem != null) return false;

            // Separation rule: dedicated stockpot rejects raw rice noodles
            if (applianceId == "stockpot" && item.itemId == "noodles_raw")
            {
                return false;
            }

            heldItem = item;
            cookingElapsedSeconds = 0.0f;
            currentState = CookingState.COOKING;
            return true;
        }

        public void TickCooking(float delta, string specializationId = "")
        {
            if (heldItem == null)
            {
                currentState = CookingState.RAW;
                cookingElapsedSeconds = 0.0f;
                return;
            }

            cookingElapsedSeconds += delta;

            float optStart = applianceSpec != null ? applianceSpec.optimalStartTime : 4.0f;
            float optEnd = applianceSpec != null ? applianceSpec.optimalEndTime : 8.0f;
            float burnTime = applianceSpec != null ? applianceSpec.burnTime : 12.0f;

            // Apply Flame Keeper specialization bonus
            if (specializationId == "flame_keeper" && GameData.Instance != null && GameData.Instance.specializations.TryGetValue("flame_keeper", out var specData))
            {
                float extraWindow = (optEnd - optStart) * (specData.cookingOptimalMultiplier - 1.0f);
                optEnd += extraWindow;
                burnTime += extraWindow;
            }

            if (cookingElapsedSeconds < optStart)
            {
                currentState = CookingState.COOKING;
            }
            else if (cookingElapsedSeconds <= optEnd)
            {
                currentState = CookingState.OPTIMAL;
            }
            else if (cookingElapsedSeconds < burnTime)
            {
                currentState = CookingState.OVERCOOKED;
            }
            else
            {
                currentState = CookingState.BURNT;
            }
        }

        public KitchenItem RetrieveCookedItem()
        {
            if (heldItem == null) return null;

            var item = heldItem;
            heldItem = null;

            if (currentState == CookingState.OPTIMAL)
            {
                item.quality = QualityTier.OPTIMAL;
                item.itemId = applianceSpec != null ? applianceSpec.outputItemId : item.itemId;
            }
            else if (currentState == CookingState.OVERCOOKED)
            {
                item.quality = QualityTier.OVERCOOKED;
                item.itemId = applianceSpec != null ? applianceSpec.outputItemId : item.itemId;
            }
            else if (currentState == CookingState.BURNT)
            {
                item.quality = QualityTier.BURNT_WASTE;
                item.itemId = "burnt_waste";
                item.displayName = "Burnt Food Waste";
            }
            else if (currentState == CookingState.COOKING)
            {
                item.quality = QualityTier.STANDARD;
            }

            cookingElapsedSeconds = 0.0f;
            currentState = CookingState.RAW;
            return item;
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (heldItem == null)
            {
                if (playerItem != null)
                {
                    if (PlaceIngredient(playerItem))
                    {
                        playerItem = null;
                        return (true, $"Cooking started in {displayName}.");
                    }
                    return (false, $"{displayName} cannot accept this ingredient.");
                }
                return (false, "Nothing to cook.");
            }
            else
            {
                if (playerItem == null)
                {
                    playerItem = RetrieveCookedItem();
                    return (true, $"Retrieved {playerItem.displayName} from {displayName}.");
                }
                return (false, "Hands full.");
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return (false, "Secondary action not applicable to cooking station.");
        }
    }
}
