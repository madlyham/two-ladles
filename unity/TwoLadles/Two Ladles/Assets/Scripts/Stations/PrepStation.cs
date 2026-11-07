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

        public bool PlaceRawItem(KitchenItem item)
        {
            if (heldItem != null) return false;
            heldItem = item;
            strikesCompleted = 0;
            lastStrikeTime = 0.0f;
            hasMissedCadence = false;
            perfectCadenceCount = 0;
            return true;
        }

        public (bool ok, string reason, CadenceGrade grade, QualityTier quality, bool finished, int scoreBonus) PerformStrike(
            float currentTime,
            string specializationId = "",
            string characterId = "linh",
            float customSpeedMultiplier = 1.0f)
        {
            if (heldItem == null)
                return (false, "No item on preparation counter.", CadenceGrade.MISS, QualityTier.STANDARD, false, 0);

            if (strikesCompleted >= REQUIRED_STRIKES)
                return (false, "Item preparation already completed.", CadenceGrade.MISS, QualityTier.STANDARD, true, 0);

            // Calculate speed multiplier
            float speedMult = customSpeedMultiplier;
            if (characterId == "linh" && ProgressionManager.Instance != null && GameData.Instance != null)
            {
                int tier = ProgressionManager.Instance.linhTier;
                if (GameData.Instance.linhTiers.TryGetValue(tier, out var spec))
                {
                    speedMult = spec.taskSpeedMultiplier;
                }
            }

            float idealInterval = IDEAL_CADENCE_SEC * speedMult;
            float perfectWindow = CADENCE_PERFECT_WINDOW * speedMult;
            float goodWindow = CADENCE_GOOD_WINDOW * speedMult;

            // Apply knife upgrade
            if (ProgressionManager.Instance != null)
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
                heldItem.itemId = outputId;
                heldItem.quality = quality;
            }

            int scoreBonus = (grade == CadenceGrade.PERFECT ? 10 : 0) + (specializationId == "prep_master" ? 15 : 0);
            return (true, "", grade, quality, finished, scoreBonus);
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (heldItem == null)
            {
                if (playerItem != null)
                {
                    heldItem = playerItem;
                    playerItem = null;
                    strikesCompleted = 0;
                    lastStrikeTime = 0.0f;
                    hasMissedCadence = false;
                    perfectCadenceCount = 0;
                    return (true, "Placed item on prep counter.");
                }
                return (false, "Nothing to place.");
            }
            else
            {
                if (playerItem == null)
                {
                    playerItem = heldItem;
                    heldItem = null;
                    return (true, "Picked up item from prep counter.");
                }
                return (false, "Hands full.");
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            var res = PerformStrike(Time.time, "", characterId);
            return (res.ok, res.reason);
        }
    }
}
