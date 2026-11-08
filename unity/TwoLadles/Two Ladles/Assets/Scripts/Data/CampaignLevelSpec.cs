using System;
using System.Collections.Generic;

namespace TwoLadles.Data
{
    [Serializable]
    public class WaveSpec
    {
        public int waveNumber;
        public int totalOrders;
        public float orderSpawnInterval;
        public float customerPatienceSeconds;
        public int maxConcurrentOrders;
        public List<string> allowedRecipes;

        public WaveSpec(
            int pWaveNumber,
            int pTotalOrders,
            float pSpawnInterval,
            float pPatienceSeconds,
            int pMaxConcurrent,
            List<string> pRecipes)
        {
            waveNumber = pWaveNumber;
            totalOrders = pTotalOrders;
            orderSpawnInterval = pSpawnInterval;
            customerPatienceSeconds = pPatienceSeconds;
            maxConcurrentOrders = pMaxConcurrent;
            allowedRecipes = pRecipes != null ? new List<string>(pRecipes) : new List<string>();
        }
    }

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
        public List<WaveSpec> waves = new List<WaveSpec>();

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
            string pIntro,
            List<WaveSpec> pWaves = null)
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

            if (pWaves != null && pWaves.Count > 0)
            {
                waves = pWaves;
            }
            else
            {
                waves = BuildDefaultWaves(pId, pRecipes, pConcurrent, pInterval);
            }
        }

        private static List<WaveSpec> BuildDefaultWaves(int lvlId, List<string> baseRecipes, int baseConcurrent, float baseInterval)
        {
            var list = new List<WaveSpec>();
            if (lvlId == 0)
            {
                // Tutorial has 1 gentle training wave
                list.Add(new WaveSpec(1, 1, 60.0f, 600.0f, 1, baseRecipes));
                return list;
            }

            // 3 escalating waves per level:
            // Wave 1: Manageable warm-up workload
            int w1Orders = 2 + (lvlId > 3 ? 1 : 0);
            int w1Concurrent = Math.Max(1, baseConcurrent - 1);
            float w1Interval = baseInterval + 2.5f;
            float w1Patience = 75.0f - (lvlId * 2.0f);
            var w1Recipes = baseRecipes.Count > 1 ? baseRecipes.GetRange(0, Math.Max(1, baseRecipes.Count - 1)) : baseRecipes;
            list.Add(new WaveSpec(1, w1Orders, w1Interval, w1Patience, w1Concurrent, w1Recipes));

            // Wave 2: Standard pace — increases simultaneous order cap & introduces full level recipe pool
            int w2Orders = w1Orders + 1;
            int w2Concurrent = baseConcurrent;
            float w2Interval = baseInterval;
            float w2Patience = 70.0f - (lvlId * 2.5f);
            list.Add(new WaveSpec(2, w2Orders, w2Interval, w2Patience, w2Concurrent, baseRecipes));

            // Wave 3: Peak service wave — faster order frequency
            int w3Orders = w2Orders + 1;
            int w3Concurrent = baseConcurrent;
            float w3Interval = Math.Max(6.5f, baseInterval - 2.0f);
            float w3Patience = 65.0f - (lvlId * 2.5f);
            list.Add(new WaveSpec(3, w3Orders, w3Interval, w3Patience, w3Concurrent, baseRecipes));

            return list;
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
