using System;
using UnityEngine;

namespace TwoLadles.Data
{
    [Serializable]
    public class LinhTierSpec
    {
        public int tier;
        public string title;
        public string description;
        public float taskSpeedMultiplier;
        public string armStyle; // "none", "rustic_brass_steel", "precision_white_blue"
        public Color armColorPrimary;
        public Color armColorGlow;

        public LinhTierSpec(
            int pTier,
            string pTitle,
            string pDesc,
            float pSpeedMult,
            string pStyle,
            Color pCol,
            Color pGlow = default)
        {
            tier = pTier;
            title = pTitle;
            description = pDesc;
            taskSpeedMultiplier = pSpeedMult;
            armStyle = pStyle;
            armColorPrimary = pCol;
            armColorGlow = pGlow == default ? Color.clear : pGlow;
        }
    }

    [Serializable]
    public class JiwonTierSpec
    {
        public int tier;
        public string title;
        public string description;
        public int partTimerSlots; // 0, 1, or 2
        public string outfitName; // "casual_apron", "rolled_dress_shirt", "business_suit"
        public Color outfitColor;

        public JiwonTierSpec(
            int pTier,
            string pTitle,
            string pDesc,
            int pSlots,
            string pOutfit,
            Color pCol)
        {
            tier = pTier;
            title = pTitle;
            description = pDesc;
            partTimerSlots = pSlots;
            outfitName = pOutfit;
            outfitColor = pCol;
        }
    }

    [Serializable]
    public class WorkerSpec
    {
        public string roleId;
        public string displayName;
        public string targetStationType; // "prep" or "washing"
        public int hireCost;
        public float contractDuration;
        public float cooldownDuration;
        public float workInterval;
        public string description;
        public Color uniformColor;

        public WorkerSpec(
            string pRole,
            string pName,
            string pStationType,
            int pCost,
            float pDuration,
            float pCooldown,
            float pInterval,
            string pDesc,
            Color pUniformCol)
        {
            roleId = pRole;
            displayName = pName;
            targetStationType = pStationType;
            hireCost = pCost;
            contractDuration = pDuration;
            cooldownDuration = pCooldown;
            workInterval = pInterval;
            description = pDesc;
            uniformColor = pUniformCol;
        }
    }
}
