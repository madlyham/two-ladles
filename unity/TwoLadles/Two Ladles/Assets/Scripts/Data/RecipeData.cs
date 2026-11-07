using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwoLadles.Data
{
    public enum CookingState
    {
        RAW = 0,
        COOKING = 1,
        OPTIMAL = 2,
        OVERCOOKED = 3,
        BURNT = 4
    }

    public enum CadenceGrade
    {
        PERFECT = 0,
        GOOD = 1,
        MISS = 2
    }

    public enum QualityTier
    {
        BURNT_WASTE = 0,
        OVERCOOKED = 1,
        STANDARD = 2,
        OPTIMAL = 3
    }

    [Serializable]
    public class IngredientSpec
    {
        public string id;
        public string displayName;
        public string originZone; // "linh_west" or "jiwon_east" or "shared"
        public string partnerTargetZone; // "jiwon_east" or "linh_west" or "shared"
        public string rawId;
        public string preparedId;
        public string cookedId;
        public int requiredPrepStrikes = 4;
        public string requiredAppliance = "";
        public Color color = Color.white;

        public IngredientSpec(
            string pId,
            string pDisplayName,
            string pOriginZone,
            string pPartnerTargetZone,
            string pRawId = "",
            string pPreparedId = "",
            string pCookedId = "",
            string pRequiredAppliance = "",
            Color pColor = default)
        {
            id = pId;
            displayName = pDisplayName;
            originZone = pOriginZone;
            partnerTargetZone = pPartnerTargetZone;
            rawId = pRawId;
            preparedId = pPreparedId;
            cookedId = pCookedId;
            requiredAppliance = pRequiredAppliance;
            color = pColor == default ? Color.white : pColor;
        }
    }

    [Serializable]
    public class ApplianceSpec
    {
        public string id;
        public string displayName;
        public string zone;
        public float optimalStartTime;
        public float optimalEndTime;
        public float burnTime;
        public string inputItemId;
        public string outputItemId;
        public bool requiresActiveMonitoring;

        public ApplianceSpec(
            string pId,
            string pDisplayName,
            string pZone,
            float pOptimalStart,
            float pOptimalEnd,
            float pBurnTime,
            string pInputItem,
            string pOutputItem,
            bool pMonitor = false)
        {
            id = pId;
            displayName = pDisplayName;
            zone = pZone;
            optimalStartTime = pOptimalStart;
            optimalEndTime = pOptimalEnd;
            burnTime = pBurnTime;
            inputItemId = pInputItem;
            outputItemId = pOutputItem;
            requiresActiveMonitoring = pMonitor;
        }

        public CookingState EvaluateCookingState(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0.0f) return CookingState.RAW;
            if (elapsedSeconds < optimalStartTime) return CookingState.COOKING;
            if (elapsedSeconds <= optimalEndTime) return CookingState.OPTIMAL;
            if (elapsedSeconds < burnTime) return CookingState.OVERCOOKED;
            return CookingState.BURNT;
        }
    }

    [Serializable]
    public class RecipeSpec
    {
        public string id;
        public string displayName;
        public string culturalOrigin;
        public bool isFusionSpecialty;
        public string requiredVessel; // "plate" or "bowl"
        public List<string> requiredIngredients;
        public string brothRequiredLast; // e.g. "broth_hot" for Pho Bo
        public int baseScore;
        public float targetPatienceSeconds;
        public int difficultyTier;
        public string prepSequenceHint;

        public RecipeSpec(
            string pId,
            string pDisplayName,
            string pOrigin,
            bool pFusion,
            string pVessel,
            List<string> pIngredients,
            string pBrothLast = "",
            int pBaseScore = 100,
            float pPatience = 60.0f,
            int pDifficulty = 1,
            string pHint = "")
        {
            id = pId;
            displayName = pDisplayName;
            culturalOrigin = pOrigin;
            isFusionSpecialty = pFusion;
            requiredVessel = pVessel;
            requiredIngredients = pIngredients;
            brothRequiredLast = pBrothLast;
            baseScore = pBaseScore;
            targetPatienceSeconds = pPatience;
            difficultyTier = pDifficulty;
            prepSequenceHint = pHint;
        }
    }

    [Serializable]
    public class SpecializationSpec
    {
        public string id;
        public string displayName;
        public string tag;
        public string description;
        public float cadenceToleranceMultiplier = 1.0f;
        public int cadenceBonusPoints = 0;
        public float cookingOptimalMultiplier = 1.0f;
        public float moveSpeedMultiplier = 1.0f;
        public float fusionBonusMultiplier = 1.0f;

        public SpecializationSpec(
            string pId,
            string pDisplayName,
            string pTag,
            string pDesc,
            float pCadenceTol = 1.0f,
            int pCadenceBonus = 0,
            float pCookMult = 1.0f,
            float pMoveMult = 1.0f,
            float pFusionMult = 1.0f)
        {
            id = pId;
            displayName = pDisplayName;
            tag = pTag;
            description = pDesc;
            cadenceToleranceMultiplier = pCadenceTol;
            cadenceBonusPoints = pCadenceBonus;
            cookingOptimalMultiplier = pCookMult;
            moveSpeedMultiplier = pMoveMult;
            fusionBonusMultiplier = pFusionMult;
        }
    }

    [Serializable]
    public class KitchenItem
    {
        public string itemId;
        public string displayName;
        public string vesselType; // "none", "plate", "bowl"
        public QualityTier quality;
        public List<string> assembledComponents;
        public int placedByPeer;
        public float placedAtTime;

        public KitchenItem(
            string pItemId = "",
            string pDisplayName = "",
            string pVessel = "none",
            QualityTier pQuality = QualityTier.STANDARD)
        {
            itemId = pItemId;
            displayName = pDisplayName;
            vesselType = pVessel;
            quality = pQuality;
            assembledComponents = new List<string>();
            placedByPeer = 0;
            placedAtTime = 0.0f;
        }

        public KitchenItem Clone()
        {
            var clone = new KitchenItem(itemId, displayName, vesselType, quality)
            {
                placedByPeer = placedByPeer,
                placedAtTime = placedAtTime,
                assembledComponents = new List<string>(assembledComponents)
            };
            return clone;
        }
    }
}
