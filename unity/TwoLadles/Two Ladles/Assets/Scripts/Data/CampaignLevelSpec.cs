using System;
using System.Collections.Generic;

namespace TwoLadles.Data
{
    [Serializable]
    public class CampaignLevelSpec
    {
        public int levelId;
        public string title;
        public string subtitle;
        public string settingName;
        public float shiftDuration;
        public int targetScoreStar1;
        public int targetScoreStar2;
        public int targetScoreStar3;
        public List<string> allowedRecipes;
        public int maxConcurrentOrders;
        public float orderSpawnInterval;
        public string introObjective;

        public CampaignLevelSpec(
            int pId,
            string pTitle,
            string pSubtitle,
            string pSetting,
            float pDuration,
            int pStar1,
            int pStar2,
            int pStar3,
            List<string> pRecipes,
            int pConcurrent,
            float pInterval,
            string pIntro)
        {
            levelId = pId;
            title = pTitle;
            subtitle = pSubtitle;
            settingName = pSetting;
            shiftDuration = pDuration;
            targetScoreStar1 = pStar1;
            targetScoreStar2 = pStar2;
            targetScoreStar3 = pStar3;
            allowedRecipes = pRecipes;
            maxConcurrentOrders = pConcurrent;
            orderSpawnInterval = pInterval;
            introObjective = pIntro;
        }

        public int EvaluateStars(int finalScore)
        {
            if (finalScore >= targetScoreStar3) return 3;
            if (finalScore >= targetScoreStar2) return 2;
            if (finalScore >= targetScoreStar1) return 1;
            return 0;
        }
    }
}
