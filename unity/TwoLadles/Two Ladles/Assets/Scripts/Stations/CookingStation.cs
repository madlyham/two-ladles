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
        public bool flameKeeperEquipped = false;

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
            RefreshStationVisuals();
            return true;
        }

        public void TickCooking(float delta, string specializationId = "")
        {
            if (heldItem == null)
            {
                currentState = CookingState.RAW;
                cookingElapsedSeconds = 0.0f;
                SetProgressBar(false, 0f, Color.clear);
                return;
            }

            float speedMult = 1.0f;
            if (ProgressionManager.Instance != null)
            {
                speedMult = ProgressionManager.Instance.GetUpgradeValue("cook_speed", 1.0f);
            }

            cookingElapsedSeconds += delta * speedMult;

            float optStart = applianceSpec != null ? applianceSpec.optimalStartTime : 4.0f;
            float optEnd = applianceSpec != null ? applianceSpec.optimalEndTime : 8.0f;
            float burnTime = applianceSpec != null ? applianceSpec.burnTime : 12.0f;

            // Apply Flame Keeper specialization bonus
            if ((flameKeeperEquipped || specializationId == "flame_keeper") && GameData.Instance != null && GameData.Instance.specializations.TryGetValue("flame_keeper", out var specData))
            {
                float extraWindow = (optEnd - optStart) * (specData.cookingOptimalMultiplier - 1.0f);
                optEnd += extraWindow;
                burnTime += extraWindow;
            }

            // Apply Double-Walled Stockpot / Appliance Window upgrade
            if (ProgressionManager.Instance != null)
            {
                float winMult = ProgressionManager.Instance.GetUpgradeValue("stockpot_window", 1.0f);
                if (winMult > 1.0f)
                {
                    float extra = (optEnd - optStart) * (winMult - 1.0f);
                    optEnd += extra;
                    burnTime += extra;
                }
            }

            if (cookingElapsedSeconds < optStart)
            {
                currentState = CookingState.COOKING;
                float pct = optStart > 0.01f ? Mathf.Clamp01(cookingElapsedSeconds / optStart) : 1.0f;
                SetProgressBar(true, pct, new Color(0.95f, 0.80f, 0.20f, 1.0f));
            }
            else if (cookingElapsedSeconds <= optEnd)
            {
                currentState = CookingState.OPTIMAL;
                float pulse = Mathf.PingPong(Time.time * 4.0f, 1.0f);
                Color optCol = Color.Lerp(new Color(0.15f, 0.95f, 0.35f, 1.0f), new Color(0.55f, 1.0f, 0.65f, 1.0f), pulse);
                SetProgressBar(true, 1.0f, optCol);
            }
            else if (cookingElapsedSeconds < burnTime)
            {
                currentState = CookingState.OVERCOOKED;
                float pct = 1.0f - Mathf.Clamp01((cookingElapsedSeconds - optEnd) / Mathf.Max(0.1f, burnTime - optEnd));
                SetProgressBar(true, Mathf.Max(0.2f, pct), new Color(0.98f, 0.50f, 0.15f, 1.0f));
            }
            else
            {
                currentState = CookingState.BURNT;
                SetProgressBar(true, 1.0f, new Color(0.85f, 0.15f, 0.15f, 1.0f));
            }
        }

        public KitchenItem RetrieveCookedItem()
        {
            if (heldItem == null) return null;

            var item = heldItem;
            heldItem = null;

            if (currentState == CookingState.OPTIMAL || currentState == CookingState.OVERCOOKED)
            {
                item.quality = (currentState == CookingState.OPTIMAL) ? QualityTier.OPTIMAL : QualityTier.OVERCOOKED;
                if (GameData.Instance != null && GameData.Instance.ingredients.TryGetValue(item.itemId, out var ingSpec) && !string.IsNullOrEmpty(ingSpec.cookedId))
                {
                    item.itemId = ingSpec.cookedId;
                }
                else if (applianceSpec != null && !string.IsNullOrEmpty(applianceSpec.outputItemId))
                {
                    item.itemId = applianceSpec.outputItemId;
                }
                if (GameData.Instance != null && GameData.Instance.ingredients.TryGetValue(item.itemId, out var outSpec))
                {
                    item.displayName = outSpec.displayName;
                }
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
            SetProgressBar(false, 0f, Color.clear);
            RefreshStationVisuals();
            return item;
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (heldItem == null)
            {
                if (playerItem != null)
                {
                    string placedId = playerItem.itemId;
                    if (PlaceIngredient(playerItem))
                    {
                        playerItem = null;

                        if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial)
                        {
                            if (ShiftManager.Instance.tutorialStep == 6)
                                ShiftManager.Instance.AdvanceTutorialStep(7);
                            else if (ShiftManager.Instance.tutorialStep <= 4 && placedId == "tteokbokki_raw")
                                ShiftManager.Instance.AdvanceTutorialStep(5);
                            else if (ShiftManager.Instance.tutorialStep <= 3 && (placedId == "tteok_portioned" || placedId == "gochujang"))
                                ShiftManager.Instance.AdvanceTutorialStep(4);
                        }

                        return (true, $"Cooking in {displayName}.");
                    }
                    return (false, $"Wrong appliance.");
                }
                return (false, "Empty.");
            }
            else
            {
                if (playerItem == null)
                {
                    if (currentState == CookingState.COOKING || currentState == CookingState.RAW)
                    {
                        return (false, "Still cooking...");
                    }

                    playerItem = RetrieveCookedItem();

                    if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && playerItem != null)
                    {
                        if (ShiftManager.Instance.tutorialStep == 7 && playerItem.itemId.Contains("pork_grilled"))
                            ShiftManager.Instance.AdvanceTutorialStep(8);
                        else if (ShiftManager.Instance.tutorialStep <= 5 && playerItem.itemId == "tteokbokki")
                            ShiftManager.Instance.AdvanceTutorialStep(6);
                    }

                    return (true, $"Picked {playerItem.displayName}.");
                }
                else
                {
                    // Allow adding a 2nd ingredient directly into the pan or self-assembling with cooked item!
                    KitchenItem effectivePanItem = heldItem.Clone();
                    bool panFinished = (currentState == CookingState.OPTIMAL || currentState == CookingState.OVERCOOKED);
                    if (panFinished && GameData.Instance != null &&
                        GameData.Instance.ingredients.TryGetValue(effectivePanItem.itemId, out var ingSpec) &&
                        !string.IsNullOrEmpty(ingSpec.cookedId))
                    {
                        effectivePanItem.itemId = ingSpec.cookedId;
                        if (GameData.Instance.ingredients.TryGetValue(effectivePanItem.itemId, out var outSpec))
                            effectivePanItem.displayName = outSpec.displayName;
                    }

                    // Case 0: Safeguard for Bột Chiên — if cooking flour for Bột Chiên, do not allow adding egg while flour is raw!
                    if (playerItem.itemId == "egg_raw" && heldItem.itemId == "flour" && !panFinished)
                    {
                        int stage = ShiftManager.Instance != null ? ShiftManager.Instance.currentStageId : 1;
                        bool hasBaguette = GameData.Instance != null && GameData.Instance.GetDayAllowedRecipes(stage).Contains("bulgogi_banh_mi");
                        if (!hasBaguette)
                        {
                            return (false, "Flour is still frying! Wait for Flour Cake to finish cooking before adding Egg.");
                        }
                    }

                    if (GameData.TryCombineItems(effectivePanItem, playerItem, out var combined) ||
                        GameData.TryCombineItems(heldItem, playerItem, out combined))
                    {
                        // Case 1: Cooked Flour + Egg -> needs to cook again in pan into Bột Chiên!
                        if (combined.itemId == "bot_chien_batter")
                        {
                            heldItem = combined;
                            playerItem = null;
                            cookingElapsedSeconds = 0.0f;
                            currentState = CookingState.COOKING;
                            RefreshStationVisuals();
                            return (true, "Added Egg! Cooking Bột Chiên...");
                        }

                        // Case 2: Raw Flour + Egg or Raw Toboki + Gochujang while still cooking -> finishes cooking in pan
                        if ((combined.itemId == "baguette_dough" || combined.itemId == "tteokbokki_raw") && !panFinished)
                        {
                            heldItem = combined;
                            playerItem = null;
                            RefreshStationVisuals();
                            if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep <= 4)
                            {
                                ShiftManager.Instance.AdvanceTutorialStep(5);
                            }
                            return (true, $"Cooking {combined.displayName}...");
                        }

                        // Case 3: Toboki + Gochujang when pan is already cooked -> immediately yields Tteokbokki!
                        if (combined.itemId == "tteokbokki_raw" && panFinished)
                        {
                            combined.itemId = "tteokbokki";
                            combined.displayName = "Tteokbokki";
                        }

                        // Finished dish / self-assembly -> give directly to player and clear pan
                        heldItem = null;
                        cookingElapsedSeconds = 0.0f;
                        currentState = CookingState.RAW;
                        SetProgressBar(false, 0f, Color.clear);
                        playerItem = combined;
                        RefreshStationVisuals();

                        if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && playerItem.itemId == "tteokbokki")
                        {
                            ShiftManager.Instance.AdvanceTutorialStep(6);
                        }

                        return (true, $"Assembled {combined.displayName}!");
                    }
                }
                return (false, "Hands full.");
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return InteractPrimary(ref playerItem, peerId, characterId);
        }
    }
}
