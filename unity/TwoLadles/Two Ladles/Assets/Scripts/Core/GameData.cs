using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;

namespace TwoLadles.Core
{
    public class GameData : MonoBehaviour
    {
        public static GameData Instance { get; set; }

        public Dictionary<string, IngredientSpec> ingredients = new Dictionary<string, IngredientSpec>();
        public Dictionary<string, ApplianceSpec> appliances = new Dictionary<string, ApplianceSpec>();
        public Dictionary<string, RecipeSpec> recipes = new Dictionary<string, RecipeSpec>();
        public Dictionary<string, SpecializationSpec> specializations = new Dictionary<string, SpecializationSpec>();
        public Dictionary<int, CampaignLevelSpec> campaignLevels = new Dictionary<int, CampaignLevelSpec>();
        public Dictionary<int, LinhTierSpec> linhTiers = new Dictionary<int, LinhTierSpec>();
        public Dictionary<int, JiwonTierSpec> jiwonTiers = new Dictionary<int, JiwonTierSpec>();
        public Dictionary<string, WorkerSpec> workerCatalog = new Dictionary<string, WorkerSpec>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitData()
        {
            if (recipes.Count > 0) return;

            RegisterSpecializations();
            RegisterIngredients();
            RegisterAppliances();
            RegisterRecipes();
            RegisterCampaignLevels();
            RegisterCharacterTiers();
            RegisterWorkers();
        }

        private void RegisterSpecializations()
        {
            specializations["prep_master"] = new SpecializationSpec(
                "prep_master", "PREP MASTER", "PRECISION CHOPPER",
                "Widens Perfect cadence sweet-spot by +25% and adds +15 bonus points on Perfect-prepped dishes.",
                1.25f, 15, 1.0f, 1.0f, 1.0f
            );
            specializations["flame_keeper"] = new SpecializationSpec(
                "flame_keeper", "FLAME KEEPER", "HEAT MASTER",
                "Extends the Optimal cooking window by +35% across all appliances.",
                1.0f, 0, 1.35f, 1.0f, 1.0f
            );
            specializations["market_runner"] = new SpecializationSpec(
                "market_runner", "MARKET RUNNER", "SPEED LOGISTICS",
                "Increases chef movement speed by +20%.",
                1.0f, 0, 1.0f, 1.20f, 1.0f
            );
            specializations["fusion_artist"] = new SpecializationSpec(
                "fusion_artist", "FUSION ARTIST", "CROSS-CULTURE HARMONY",
                "Awards +20% score bonus and +10 extra Flow on original fusion dishes (Bulgogi Banh Mi).",
                1.0f, 0, 1.0f, 1.0f, 1.20f
            );
        }

        private void RegisterIngredients()
        {
            // WEST ZONE (Linh's side: Meats & Starches)
            ingredients["pork_raw"] = new IngredientSpec("pork_raw", "Raw Pork Belly", "linh_west", "jiwon_east", "pork_raw", "pork_portioned", "pork_grilled", "grill_pan", new Color(0.92f, 0.45f, 0.45f));
            ingredients["pork_portioned"] = new IngredientSpec("pork_portioned", "Portioned Pork Belly", "linh_west", "jiwon_east", "pork_raw", "pork_portioned", "pork_grilled", "grill_pan", new Color(0.95f, 0.55f, 0.55f));
            ingredients["pork_grilled"] = new IngredientSpec("pork_grilled", "Grilled Pork Belly", "jiwon_east", "linh_west", "pork_raw", "pork_portioned", "pork_grilled", "grill_pan", new Color(0.65f, 0.32f, 0.15f));

            ingredients["bulgogi_raw"] = new IngredientSpec("bulgogi_raw", "Raw Marinated Bulgogi", "linh_west", "jiwon_east", "bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "grill_pan", new Color(0.68f, 0.28f, 0.25f));
            ingredients["bulgogi_portioned"] = new IngredientSpec("bulgogi_portioned", "Portioned Bulgogi Beef", "linh_west", "jiwon_east", "bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "grill_pan", new Color(0.75f, 0.35f, 0.30f));
            ingredients["bulgogi_seared"] = new IngredientSpec("bulgogi_seared", "Seared Bulgogi Beef", "jiwon_east", "linh_west", "bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "grill_pan", new Color(0.48f, 0.22f, 0.15f));

            ingredients["tteok_raw"] = new IngredientSpec("tteok_raw", "Raw Rice Cakes", "linh_west", "jiwon_east", "tteok_raw", "tteok_portioned", "tteokbokki_simmered", "simmer_wok", new Color(0.95f, 0.95f, 0.92f));
            ingredients["tteok_portioned"] = new IngredientSpec("tteok_portioned", "Portioned Rice Cakes", "linh_west", "jiwon_east", "tteok_raw", "tteok_portioned", "tteokbokki_simmered", "simmer_wok", new Color(0.98f, 0.98f, 0.95f));

            ingredients["beef_raw"] = new IngredientSpec("beef_raw", "Raw Beef Shank", "linh_west", "linh_west", "beef_raw", "beef_sliced", "beef_sliced", "", new Color(0.78f, 0.22f, 0.22f));
            ingredients["beef_sliced"] = new IngredientSpec("beef_sliced", "Thinly Sliced Beef", "linh_west", "linh_west", "beef_raw", "beef_sliced", "beef_sliced", "", new Color(0.85f, 0.32f, 0.32f));

            ingredients["noodles_raw"] = new IngredientSpec("noodles_raw", "Dry Rice Noodles", "linh_west", "linh_west", "noodles_raw", "noodles_blanched", "noodles_blanched", "noodle_blancher", new Color(0.92f, 0.90f, 0.82f));
            ingredients["noodles_blanched"] = new IngredientSpec("noodles_blanched", "Blanched Rice Noodles", "linh_west", "linh_west", "noodles_raw", "noodles_blanched", "noodles_blanched", "noodle_blancher", new Color(0.98f, 0.98f, 0.96f));

            ingredients["baguette"] = new IngredientSpec("baguette", "Crisp Vietnamese Baguette", "linh_west", "linh_west", "baguette", "baguette", "baguette", "", new Color(0.86f, 0.65f, 0.35f));
            ingredients["flour"] = new IngredientSpec("flour", "All-Purpose Flour", "linh_west", "linh_west", "flour", "flour", "flour", "", new Color(0.96f, 0.94f, 0.88f));
            ingredients["rice_raw"] = new IngredientSpec("rice_raw", "Raw Short-Grain Rice", "linh_west", "jiwon_east", "rice_raw", "rice_portioned", "kimchi_bokkeumbap_simmered", "simmer_wok", new Color(0.96f, 0.96f, 0.94f));
            ingredients["rice_portioned"] = new IngredientSpec("rice_portioned", "Steamed White Rice", "linh_west", "jiwon_east", "rice_raw", "rice_portioned", "kimchi_bokkeumbap_simmered", "simmer_wok", new Color(0.98f, 0.98f, 0.98f));

            // EAST ZONE (Jiwon's side: Vegetables, Sauces, Clean Dishes)
            ingredients["daikon_carrot_raw"] = new IngredientSpec("daikon_carrot_raw", "Raw Daikon & Carrot", "jiwon_east", "linh_west", "daikon_carrot_raw", "pickled_veg", "pickled_veg", "", new Color(0.95f, 0.62f, 0.22f));
            ingredients["pickled_veg"] = new IngredientSpec("pickled_veg", "Pickled Daikon & Carrot", "jiwon_east", "linh_west", "daikon_carrot_raw", "pickled_veg", "pickled_veg", "", new Color(1.0f, 0.72f, 0.35f));

            ingredients["kimchi_raw"] = new IngredientSpec("kimchi_raw", "Aged Baechu Kimchi", "jiwon_east", "linh_west", "kimchi_raw", "kimchi_slaw", "kimchi_slaw", "", new Color(0.88f, 0.30f, 0.18f));
            ingredients["kimchi_slaw"] = new IngredientSpec("kimchi_slaw", "Seasoned Kimchi Slaw", "jiwon_east", "linh_west", "kimchi_raw", "kimchi_slaw", "kimchi_slaw", "", new Color(0.92f, 0.38f, 0.22f));

            ingredients["eomuk_raw"] = new IngredientSpec("eomuk_raw", "Raw Fish Cakes", "jiwon_east", "jiwon_east", "eomuk_raw", "eomuk_prepared", "tteokbokki_simmered", "simmer_wok", new Color(0.85f, 0.75f, 0.55f));
            ingredients["eomuk_prepared"] = new IngredientSpec("eomuk_prepared", "Sliced Fish Cakes", "jiwon_east", "jiwon_east", "eomuk_raw", "eomuk_prepared", "tteokbokki_simmered", "simmer_wok", new Color(0.90f, 0.80f, 0.60f));

            ingredients["broth_hot"] = new IngredientSpec("broth_hot", "Simmering Pho Broth", "linh_west", "linh_west", "broth_hot", "broth_hot", "broth_hot", "stockpot", new Color(0.75f, 0.58f, 0.30f));
            ingredients["tteokbokki_simmered"] = new IngredientSpec("tteokbokki_simmered", "Simmered Tteokbokki & Eomuk", "jiwon_east", "shared", "tteok_portioned", "tteokbokki_simmered", "tteokbokki_simmered", "simmer_wok", new Color(0.85f, 0.20f, 0.12f));
            ingredients["kimchi_bokkeumbap_simmered"] = new IngredientSpec("kimchi_bokkeumbap_simmered", "Sizzling Kimchi Bokkeumbap", "jiwon_east", "shared", "rice_portioned", "kimchi_bokkeumbap_simmered", "kimchi_bokkeumbap_simmered", "simmer_wok", new Color(0.92f, 0.35f, 0.15f));
        }

        private void RegisterAppliances()
        {
            appliances["stockpot"] = new ApplianceSpec("stockpot", "Dedicated Pho Stockpot", "linh_west", 0.0f, 180.0f, 240.0f, "broth_hot", "broth_hot", false);
            appliances["noodle_blancher"] = new ApplianceSpec("noodle_blancher", "Noodle Blanching Basket", "linh_west", 3.0f, 6.0f, 10.0f, "noodles_raw", "noodles_blanched", true);
            appliances["grill_pan"] = new ApplianceSpec("grill_pan", "Cast-Iron Grill / Sear Pan", "jiwon_east", 4.0f, 9.0f, 13.0f, "pork_portioned", "pork_grilled", true);
            appliances["simmer_wok"] = new ApplianceSpec("simmer_wok", "Gochujang Simmer Wok", "jiwon_east", 5.0f, 10.0f, 15.0f, "tteok_portioned", "tteokbokki_simmered", false);
        }

        private void RegisterRecipes()
        {
            // Recipe 1: PHO BO (Traditional Vietnamese)
            recipes["pho_bo"] = new RecipeSpec(
                "pho_bo", "Phở Bò", "Traditional Vietnamese", false, "bowl",
                new List<string> { "noodles_blanched", "beef_sliced", "broth_hot" },
                "broth_hot", 120, 65.0f, 2,
                "Jiwon washes/passes Bowl & preps Beef -> Linh blanches Noodles & pours Stockpot Broth last."
            );

            // Recipe 2: BANH MI THIT (Traditional Vietnamese)
            recipes["banh_mi_thit"] = new RecipeSpec(
                "banh_mi_thit", "Bánh Mì Thịt", "Traditional Vietnamese", false, "plate",
                new List<string> { "baguette", "pork_grilled", "pickled_veg" },
                "", 110, 60.0f, 1,
                "Linh portions Pork -> passes to Jiwon to Grill; Jiwon preps Pickled Daikon/Carrot -> passes to Linh to assemble on Baguette."
            );

            // Recipe 3: TTEOKBOKKI (Traditional Korean)
            recipes["tteokbokki"] = new RecipeSpec(
                "tteokbokki", "Tteokbokki", "Traditional Korean", false, "bowl",
                new List<string> { "tteokbokki_simmered" },
                "", 115, 60.0f, 2,
                "Linh portions Rice Cakes -> passes to Jiwon; Jiwon preps Fish Cakes & simmers both with Gochujang in Wok -> plates in Bowl."
            );

            // Recipe 4: BULGOGI BANH MI (Original Vietnam-Korea Fusion Specialty)
            recipes["bulgogi_banh_mi"] = new RecipeSpec(
                "bulgogi_banh_mi", "Bulgogi Bánh Mì", "Original Vietnam–Korea Fusion Specialty", true, "plate",
                new List<string> { "baguette", "bulgogi_seared", "kimchi_slaw" },
                "", 150, 70.0f, 3,
                "Linh portions Marinated Bulgogi -> passes to Jiwon to Sear; Jiwon preps Kimchi Slaw -> passes to Linh to assemble on Baguette."
            );

            // Recipe 5: KIMCHI BOKKEUMBAP (Traditional Korean)
            // Sequence: Linh portions rice -> passes to Jiwon; Jiwon preps Kimchi & cooks in Wok with pork -> plates in clean Bowl.
            recipes["kimchi_bokkeumbap"] = new RecipeSpec(
                "kimchi_bokkeumbap", "Kimchi Bokkeumbap", "Traditional Korean", false, "bowl",
                new List<string> { "kimchi_bokkeumbap_simmered" },
                "", 125, 65.0f, 2,
                "Linh portions Rice -> passes to Jiwon; Jiwon preps Kimchi & simmers in Wok -> plates in Bowl."
            );
        }

        private void RegisterCampaignLevels()
        {
            // Level 1: 1 Recipe only (Bánh Mì Thịt) - Tutorial pace, master basic prep, grill & assembly
            campaignLevels[1] = new CampaignLevelSpec(
                1, "First Customer", "Rural Roadside Food Stand", "Rural Vietnam",
                180.0f, 150, 300, 500,
                new List<string> { "banh_mi_thit" },
                2, 16.0f,
                "Open the family food stall! Master basic movement, cross-prep, and initial service with Bánh Mì Thịt."
            );
            // Level 2: 2 Recipes (Bánh Mì Thịt + Phở Bò) - Introduces noodle blanching & hot stock timing
            campaignLevels[2] = new CampaignLevelSpec(
                2, "Lunch Rush", "Village Market Restaurant", "Village Market",
                180.0f, 220, 450, 650,
                new List<string> { "banh_mi_thit", "pho_bo" },
                3, 13.0f,
                "Multiple customer orders arrive together! Manage finite plates and scrub dirty dishes at the sink."
            );
            // Level 3: 3 Recipes (Bánh Mì Thịt + Phở Bò + Tteokbokki) - Introduces Korean Wok & Pass Counter handoffs
            campaignLevels[3] = new CampaignLevelSpec(
                3, "Night Market", "Night Market Stall", "Night Market",
                180.0f, 300, 600, 850,
                new List<string> { "banh_mi_thit", "pho_bo", "tteokbokki" },
                3, 11.0f,
                "Korean Tteokbokki added! Simmer rice cakes and fish cakes in the Wok. Unlocks Character Tier 2!"
            );
            // Level 4: 4 Recipes (+ Kimchi Bokkeumbap) - High volume cross-prep rush
            campaignLevels[4] = new CampaignLevelSpec(
                4, "City Bistro", "Street Food Market Hub", "City Food Market",
                180.0f, 400, 750, 1100,
                new List<string> { "banh_mi_thit", "pho_bo", "tteokbokki", "kimchi_bokkeumbap" },
                4, 10.0f,
                "Traditional Kimchi Bokkeumbap unlocked! High volume cross-prep and quick dish turnaround."
            );
            // Level 5: 5 Recipes (+ Bulgogi Bánh Mì Fusion) - Grand Opening climax with all dishes
            campaignLevels[5] = new CampaignLevelSpec(
                5, "Grand Opening", "Modern Fusion Restaurant", "Metropolitan Bistro",
                180.0f, 550, 950, 1400,
                new List<string> { "banh_mi_thit", "pho_bo", "tteokbokki", "kimchi_bokkeumbap", "bulgogi_banh_mi" },
                4, 8.5f,
                "Grand Opening! All 5 authentic dishes in rapid succession. Trigger Rush Rhythm and unlock Character Tier 3!"
            );
        }

        private void RegisterCharacterTiers()
        {
            linhTiers[1] = new LinhTierSpec(
                1, "Ordinary Chef",
                "Standard tech burnout chef. No mechanical arm equipment.",
                1.0f, "none", Color.white, Color.clear
            );
            linhTiers[2] = new LinhTierSpec(
                2, "Workshop Engineer",
                "One mechanical third arm made of brass and scrap metal (~8% faster actions).",
                0.92f, "rustic_brass_steel", new Color(0.72f, 0.58f, 0.35f), Color.clear
            );
            linhTiers[3] = new LinhTierSpec(
                3, "Precision Engineer",
                "One mechanical third arm with clean white exterior and glowing blue lines (~15% faster actions).",
                0.85f, "precision_white_blue", new Color(0.92f, 0.95f, 1.0f), new Color(0.2f, 0.6f, 1.0f)
            );

            jiwonTiers[1] = new JiwonTierSpec(
                1, "Burnt-out Executive",
                "Practical casual clothes, kitchen apron, former corporate executive. 0 part-timer slots.",
                0, "casual_apron", new Color(0.88f, 0.42f, 0.24f)
            );
            jiwonTiers[2] = new JiwonTierSpec(
                2, "Restaurant Manager",
                "Crisp dress shirt with rolled sleeves, no suit/tie. 1 part-timer slot.",
                1, "rolled_dress_shirt", new Color(0.92f, 0.55f, 0.32f)
            );
            jiwonTiers[3] = new JiwonTierSpec(
                3, "Business Operator",
                "Formal executive business suit with tie and briefcase. 2 simultaneous part-timer slots.",
                2, "business_suit", new Color(0.25f, 0.35f, 0.55f)
            );
        }

        private void RegisterWorkers()
        {
            workerCatalog["prep_assistant"] = new WorkerSpec(
                "prep_assistant", "Prep Assistant", "prep",
                20, 20.0f, 45.0f, 1.1f,
                "Walks into kitchen and automates chopping strikes on East preparation counter.",
                new Color(0.35f, 0.75f, 0.45f)
            );
            workerCatalog["dishwasher"] = new WorkerSpec(
                "dishwasher", "Kitchen Dishwasher", "washing",
                20, 20.0f, 45.0f, 1.2f,
                "Walks into kitchen and scrubs dirty plates at the washing sink.",
                new Color(0.35f, 0.55f, 0.85f)
            );
        }

        public string GetPrepOutput(string rawItemId)
        {
            if (ingredients.TryGetValue(rawItemId, out var spec))
            {
                if (!string.IsNullOrEmpty(spec.preparedId) && spec.preparedId != rawItemId)
                    return spec.preparedId;
            }
            return "";
        }

        public string GetGrillOutput(string preparedItemId)
        {
            if (preparedItemId == "pork_portioned") return "pork_grilled";
            if (preparedItemId == "bulgogi_portioned") return "bulgogi_seared";
            return "";
        }

        public bool IsCrossPartnerHandoff(string itemId, string placerChar, string retrieverChar)
        {
            if (string.IsNullOrEmpty(placerChar) || string.IsNullOrEmpty(retrieverChar)) return false;
            if (placerChar == retrieverChar) return false;
            return ingredients.ContainsKey(itemId);
        }
    }
}
