using System;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class PrepStation : StationBase
    {
        public const int REQUIRED_STRIKES = 4;
        public const float IDEAL_CADENCE_SEC = 0.45f;
        public const float CADENCE_PERFECT_WINDOW = 0.12f;
        public const float CADENCE_GOOD_WINDOW = 0.28f;

        public int strikesCompleted = 0;
        public float lastStrikeTime = 0.0f;
        public bool hasMissedCadence = false;
        public int perfectCadenceCount = 0;

        public void UpdateVisuals()
        {
            RefreshStationVisuals();
            if (heldItem == null)
            {
                SetProgressBar(false, 0f, Color.clear);
            }
            else
            {
                float pct = Mathf.Clamp01((float)strikesCompleted / REQUIRED_STRIKES);
                Color barCol = (strikesCompleted >= REQUIRED_STRIKES)
                    ? new Color(0.20f, 0.95f, 0.40f, 1.0f)
                    : new Color(0.95f, 0.80f, 0.20f, 1.0f);
                SetProgressBar(strikesCompleted > 0 || heldItem != null, Mathf.Max(0.08f, pct), barCol);
            }
        }

        public bool PlaceRawItem(KitchenItem item)
        {
            if (heldItem != null) return false;
            if (!CanChop(item)) return false;
            heldItem = item;
            strikesCompleted = 0;
            lastStrikeTime = 0.0f;
            hasMissedCadence = false;
            perfectCadenceCount = 0;
            UpdateVisuals();
            return true;
        }

        // Only raw food that actually turns into something on the board may be placed here.
        public static bool CanChop(KitchenItem item)
        {
            if (item == null) return false;
            if (GameData.Instance == null) return true;
            return !string.IsNullOrEmpty(GameData.Instance.GetPrepOutput(item.itemId));
        }

        public (bool ok, string reason, CadenceGrade grade, QualityTier quality, bool finished, int scoreBonus) PerformStrike(
            float currentTime,
            string specializationId = "",
            string characterId = "linh",
            float customSpeedMultiplier = 1.0f,
            int chefTier = -1,
            float prepWindowOverride = -1f)
        {
            if (heldItem == null)
                return (false, "Empty.", CadenceGrade.MISS, QualityTier.STANDARD, false, 0);

            if (strikesCompleted >= REQUIRED_STRIKES)
            {
                // Allow a second cut stage if the current item has a valid next prep output (e.g., korean_bbq_raw -> bulgogi_portioned)
                string nextOut = GameData.Instance != null ? GameData.Instance.GetPrepOutput(heldItem.itemId) : "";
                if (!string.IsNullOrEmpty(nextOut) && nextOut != heldItem.itemId)
                {
                    strikesCompleted = 0;
                    lastStrikeTime = 0.0f;
                    hasMissedCadence = false;
                    perfectCadenceCount = 0;
                }
                else
                {
                    return (false, "Ready!", CadenceGrade.MISS, QualityTier.STANDARD, true, 0);
                }
            }

            float speedMult = customSpeedMultiplier;
            if (characterId == "linh" && ProgressionManager.Instance != null && GameData.Instance != null)
            {
                int tier = chefTier > 0 ? chefTier : ProgressionManager.Instance.linhTier;
                if (GameData.Instance.linhTiers.TryGetValue(tier, out var spec))
                {
                    speedMult = spec.taskSpeedMultiplier;
                }
            }

            float idealInterval = IDEAL_CADENCE_SEC * speedMult;
            // Faster chefs get a faster beat, but the same-size Perfect/Good windows.
            float perfectWindow = CADENCE_PERFECT_WINDOW;
            float goodWindow = CADENCE_GOOD_WINDOW;

            // Apply knife upgrade (striking chef's own value in LAN, else this device's upgrade)
            if (prepWindowOverride > 0f)
            {
                perfectWindow *= prepWindowOverride;
                goodWindow *= prepWindowOverride;
            }
            else if (ProgressionManager.Instance != null)
            {
                float upVal = ProgressionManager.Instance.GetUpgradeValue("prep_window", 1.0f);
                perfectWindow *= upVal;
                goodWindow *= upVal;
            }

            // Apply specialization perk
            if (specializationId == "prep_master" && GameData.Instance != null && GameData.Instance.specializations.TryGetValue("prep_master", out var specData))
            {
                perfectWindow *= specData.cadenceToleranceMultiplier;
                goodWindow *= specData.cadenceToleranceMultiplier;
            }

            CadenceGrade grade = CadenceGrade.GOOD;
            if (strikesCompleted == 0)
            {
                grade = CadenceGrade.PERFECT;
                perfectCadenceCount++;
            }
            else
            {
                float interval = currentTime - lastStrikeTime;
                float delta = Math.Abs(interval - idealInterval);

                if (delta <= perfectWindow)
                {
                    grade = CadenceGrade.PERFECT;
                    perfectCadenceCount++;
                }
                else if (delta <= goodWindow)
                {
                    grade = CadenceGrade.GOOD;
                }
                else
                {
                    grade = CadenceGrade.MISS;
                    hasMissedCadence = true;
                }
            }

            lastStrikeTime = currentTime;
            strikesCompleted++;

            bool finished = strikesCompleted >= REQUIRED_STRIKES;
            QualityTier quality = QualityTier.STANDARD;

            if (finished)
            {
                quality = (!hasMissedCadence && perfectCadenceCount == REQUIRED_STRIKES) ? QualityTier.OPTIMAL : QualityTier.STANDARD;
                string outputId = GameData.Instance != null ? GameData.Instance.GetPrepOutput(heldItem.itemId) : heldItem.itemId + "_portioned";
                if (!string.IsNullOrEmpty(outputId))
                {
                    heldItem.itemId = outputId;
                    if (GameData.Instance != null && GameData.Instance.ingredients.TryGetValue(outputId, out var outSpec))
                    {
                        heldItem.displayName = outSpec.displayName;
                    }
                    heldItem.quality = quality;
                }

                if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 3)
                {
                    ShiftManager.Instance.AdvanceTutorialStep(4);
                }
            }

            UpdateVisuals();
            int scoreBonus = (grade == CadenceGrade.PERFECT ? 10 : 0) + (specializationId == "prep_master" ? 15 : 0);
            return (true, "", grade, quality, finished, scoreBonus);
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (heldItem == null)
            {
                if (playerItem != null)
                {
                    if (!CanChop(playerItem)) return (false, "Can't chop that.");
                    heldItem = playerItem;
                    playerItem = null;
                    strikesCompleted = 0;
                    lastStrikeTime = 0.0f;
                    hasMissedCadence = false;
                    perfectCadenceCount = 0;
                    UpdateVisuals();
                    return (true, "Placed.");
                }
                return (false, "Empty.");
            }
            else
            {
                if (playerItem == null)
                {
                    playerItem = heldItem;
                    heldItem = null;
                    UpdateVisuals();
                    return (true, "Picked up.");
                }
                else if (GameData.TryCombineItems(heldItem, playerItem, out var combined))
                {
                    heldItem = null;
                    strikesCompleted = 0;
                    playerItem = combined;
                    UpdateVisuals();
                    return (true, $"Assembled {combined.displayName}!");
                }
                return (false, "Hands full.");
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            // LAN: use the striking chef's own tier + knife upgrade (partner may have different upgrades).
            int chefTier = -1;
            float chefPrepWindow = -1f;
            if (TwoLadles.Kitchen.KitchenController.Instance != null &&
                TwoLadles.Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(peerId, out var chef) &&
                chef != null && chef.upgradeOverrides != null)
            {
                chefTier = chef.characterTier;
                chefPrepWindow = chef.GetChefUpgradeValue("prep_window", 1.0f);
            }
            var res = PerformStrike(Time.time, "", characterId, 1.0f, chefTier, chefPrepWindow);
            return (res.ok, res.reason);
        }
    }
}
