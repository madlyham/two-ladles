using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Player;
using TwoLadles.Networking;

namespace TwoLadles.Kitchen
{
    public class KitchenController : MonoBehaviour
    {
        public static KitchenController Instance { get; set; }

        public event Action<string, Color> OnStatusToast;
        public event Action<string> OnPromptChanged;

        public Dictionary<int, PlayerController> playersByPeer = new Dictionary<int, PlayerController>();
        public Dictionary<string, StationBase> stations = new Dictionary<string, StationBase>();
        public Dictionary<string, PartTimeWorker> activeWorkers = new Dictionary<string, PartTimeWorker>();
        public Dictionary<string, float> workerCooldowns = new Dictionary<string, float>();

        public bool autoBuildKitchenOnStart = true;
        private int workerUidCounter = 1;
        private string currentPrompt = "";

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (autoBuildKitchenOnStart)
            {
                BuildEnvironment();
                BuildPerimeterWalls();
                BuildStations();
                SpawnPlayers();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Tick worker cooldowns
            var keys = new List<string>(workerCooldowns.Keys);
            foreach (var k in keys)
            {
                if (workerCooldowns[k] > 0.0f)
                {
                    workerCooldowns[k] = Mathf.Max(0.0f, workerCooldowns[k] - dt);
                }
            }

            // 2. Tick cooking stations
            foreach (var st in stations.Values)
            {
                if (st is CookingStation cs)
                {
                    cs.TickCooking(dt);
                }
            }

            // 3. Update interaction prompt for local player
            UpdateLocalPrompt();
        }

        private void UpdateLocalPrompt()
        {
            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (!playersByPeer.TryGetValue(localPeer, out var localChef) || localChef == null)
                return;

            StationBase nearest = FindNearestStation(localChef.transform.position);
            string prompt = "";

            if (nearest != null)
            {
                string sName = nearest.displayName;
                if (nearest is PrepStation)
                    prompt = $"Near: {sName} [E/Space: Place/Pick | F: Strike Cadence]";
                else if (nearest is CookingStation)
                    prompt = $"Near: {sName} [E/Space: Place Raw / Take Cooked]";
                else if (nearest is PassCounter)
                    prompt = $"Near: {sName} [E/Space: Place / Retrieve Dish (+15 Flow)]";
                else if (nearest is CounterStation)
                    prompt = $"Near: {sName} [E/Space: Place / Pick / Swap Item]";
                else if (nearest is AssemblyStation)
                    prompt = $"Near: {sName} [E/Space: Place Vessel / Add Component]";
                else if (nearest is ServingStation)
                    prompt = $"Near: {sName} [E/Space: Deliver Order]";
                else if (nearest is WashingStation)
                    prompt = $"Near: {sName} [E/Space: Clean Plate | F: Scrub / Clean Bowl]";
                else if (nearest is CrateStation)
                    prompt = $"Near: {sName} [E/Space: Grab Ingredient]";
                else if (nearest is TrashStation)
                    prompt = $"Near: {sName} [E/Space: Dispose Item]";
                else
                    prompt = $"Near: {sName} [E/Space: Interact]";
            }

            if (prompt != currentPrompt)
            {
                currentPrompt = prompt;
                OnPromptChanged?.Invoke(prompt);
            }
        }

        public void TriggerStatusToast(string message, Color color)
        {
            OnStatusToast?.Invoke(message, color);
        }

        public void LocalPlayerInteractPrimary()
        {
            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (playersByPeer.TryGetValue(localPeer, out var localChef))
            {
                localChef.TryInteractPrimary();
            }
        }

        public void LocalPlayerInteractSecondary()
        {
            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (playersByPeer.TryGetValue(localPeer, out var localChef))
            {
                localChef.TryInteractSecondary();
            }
        }

        public void LocalPlayerThrow()
        {
            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            if (playersByPeer.TryGetValue(localPeer, out var localChef))
            {
                localChef.ThrowItem(localChef.facingDirection);
            }
        }

        public void BuildEnvironment()
        {
            // 1. Camera
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("KitchenCamera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 7.0f;
                camObj.transform.position = new Vector3(0.0f, 11.5f, 9.5f);
                camObj.transform.LookAt(new Vector3(0.0f, 0.0f, 0.2f), Vector3.up);

            }

            // 2. Warm Sun Light
            Light[] existingLights = FindObjectsOfType<Light>();
            bool hasSun = false;
            foreach (var l in existingLights)
            {
                if (l.type == LightType.Directional) { hasSun = true; break; }
            }
            if (!hasSun)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1.0f, 0.94f, 0.84f);
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                lightObj.transform.rotation = Quaternion.Euler(52.0f, -28.0f, 0.0f);
            }

            // 3. Kitchen Floors (West = Linh Sage-Teal tile, East = Jiwon Terracotta tile, Center Divider)
            CreateFloorMesh("FloorWest", new Vector3(-4.6f, -0.1f, 0.0f), new Vector3(9.2f, 0.2f, 11.5f), new Color(0.24f, 0.28f, 0.27f));
            CreateFloorMesh("FloorEast", new Vector3(4.6f, -0.1f, 0.0f), new Vector3(9.2f, 0.2f, 11.5f), new Color(0.30f, 0.24f, 0.22f));
            CreateFloorMesh("CenterDividerLine", new Vector3(0.0f, -0.08f, 0.0f), new Vector3(0.35f, 0.21f, 11.5f), new Color(0.85f, 0.72f, 0.35f));
        }

        public void BuildPerimeterWalls()
        {
            CreateStaticWall("Wall_North", new Vector3(0.0f, 1.5f, -5.8f), new Vector3(19.2f, 3.0f, 0.8f));
            CreateStaticWall("Wall_South", new Vector3(0.0f, 1.5f, 5.8f), new Vector3(19.2f, 3.0f, 0.8f));
            CreateStaticWall("Wall_West", new Vector3(-9.4f, 1.5f, 0.0f), new Vector3(0.8f, 3.0f, 12.0f));
            CreateStaticWall("Wall_East", new Vector3(9.4f, 1.5f, 0.0f), new Vector3(0.8f, 3.0f, 12.0f));
        }

        private GameObject CreateStaticWall(string wallName, Vector3 pos, Vector3 size)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(transform);
            wall.transform.position = pos;
            var col = wall.AddComponent<BoxCollider>();
            col.size = size;
            return wall;
        }

        private GameObject CreateFloorMesh(string floorName, Vector3 pos, Vector3 size, Color col)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = floorName;
            obj.transform.SetParent(transform);
            obj.transform.position = pos;
            obj.transform.localScale = size;

            // Remove collider from floor so player character controller uses ground plane y=0
            var c = obj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = col;
                mat.SetFloat("_Glossiness", 0.4f);
                mat.SetFloat("_Metallic", 0.2f);
                mr.material = mat;
            }
            return obj;
        }

        public void BuildStations()
        {
            // Clear previous stations if rebuild
            stations.Clear();

            // --- CENTER: SHARED PASS COUNTER & TRASH ---
            var pass = CreateStation<PassCounter>(
                "pass_counter", "PASS COUNTER", "shared",
                new Vector3(0.0f, 0.0f, -0.4f), new Vector3(1.6f, 0.9f, 3.2f),
                new Color(0.92f, 0.76f, 0.32f), "pass_counter", "dish_plate"
            );

            var trash = CreateStation<TrashStation>(
                "trash_bin", "TRASH BIN", "shared",
                new Vector3(0.0f, 0.0f, 3.6f), new Vector3(1.1f, 0.8f, 1.1f),
                new Color(0.35f, 0.35f, 0.38f), "trash_bin", "dish_brush"
            );

            // --- WEST ZONE (LINH'S SIDE): Preps, Holding, Stockpot, Blancher, Assembly, Serving, Crates ---
            var prepWest = CreateStation<PrepStation>(
                "prep_west", "LINH PREP (West)", "linh_west",
                new Vector3(-3.2f, 0.0f, -2.6f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.25f, 0.68f, 0.58f), "prep_west", "prep_counter_icon"
            );

            var counterWest = CreateStation<CounterStation>(
                "counter_west", "HOLDING TABLE (West)", "linh_west",
                new Vector3(-3.2f, 0.0f, -0.6f), new Vector3(2.0f, 0.9f, 1.1f),
                new Color(0.65f, 0.70f, 0.74f), "serving_station", "dish_plate"
            );

            var stockpot = CreateStation<CookingStation>(
                "stockpot", "Dedicated Phở Stockpot", "linh_west",
                new Vector3(-6.5f, 0.0f, -3.2f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.82f, 0.58f, 0.22f), "stockpot", "broth_pot"
            );
            stockpot.applianceId = "stockpot";

            var blancher = CreateStation<CookingStation>(
                "noodle_blancher", "Noodle Blancher", "linh_west",
                new Vector3(-6.5f, 0.0f, -1.0f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.75f, 0.78f, 0.85f), "noodle_blancher", "dish_bowl"
            );
            blancher.applianceId = "noodle_blancher";

            var assemblyWest = CreateStation<AssemblyStation>(
                "assembly_west", "ASSEMBLY WEST (Linh)", "linh_west",
                new Vector3(-3.2f, 0.0f, 1.4f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.38f, 0.58f, 0.78f), "serving_station", "dish_bowl"
            );

            var servingStation = CreateStation<ServingStation>(
                "serving_station", "SERVING COUNTER", "shared",
                new Vector3(-3.2f, 0.0f, 4.0f), new Vector3(2.8f, 0.9f, 1.2f),
                new Color(0.95f, 0.75f, 0.22f), "serving_station", "baguette_bread"
            );

            // West Crates
            CreateCrateStation("crate_pork", "CRATE: Raw Pork Belly", "pork_raw", new Vector3(-7.6f, 0.0f, 1.2f), new Color(0.88f, 0.48f, 0.45f), "pork_belly_raw");
            CreateCrateStation("crate_bulgogi", "CRATE: Raw Bulgogi", "bulgogi_raw", new Vector3(-7.6f, 0.0f, 2.6f), new Color(0.68f, 0.22f, 0.20f), "beef_bulgogi_raw");
            CreateCrateStation("crate_tteok", "CRATE: Rice Cakes", "tteok_raw", new Vector3(-7.6f, 0.0f, 4.0f), new Color(0.95f, 0.93f, 0.90f), "tteok_portioned");
            CreateCrateStation("crate_beef", "CRATE: Raw Beef", "beef_raw", new Vector3(-5.4f, 0.0f, -4.4f), new Color(0.78f, 0.25f, 0.25f), "beef_bulgogi_raw");
            CreateCrateStation("crate_noodles", "CRATE: Dry Rice Noodles", "noodles_raw", new Vector3(-3.4f, 0.0f, -4.4f), new Color(0.95f, 0.92f, 0.80f), "dish_bowl");
            CreateCrateStation("crate_baguette", "CRATE: Baguettes", "baguette", new Vector3(-1.4f, 0.0f, -4.4f), new Color(0.86f, 0.66f, 0.36f), "baguette_bread");

            // --- EAST ZONE (JIWON'S SIDE): Preps, Holding, Grill, Simmer Wok, Assembly, Washing, Crates ---
            var prepEast = CreateStation<PrepStation>(
                "prep_east", "JIWON PREP (East)", "jiwon_east",
                new Vector3(3.2f, 0.0f, -2.6f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.88f, 0.45f, 0.25f), "prep_east", "prep_counter_icon"
            );

            var counterEast = CreateStation<CounterStation>(
                "counter_east", "HOLDING TABLE (East)", "jiwon_east",
                new Vector3(3.2f, 0.0f, -0.6f), new Vector3(2.0f, 0.9f, 1.1f),
                new Color(0.65f, 0.70f, 0.74f), "serving_station", "dish_plate"
            );

            var grill = CreateStation<CookingStation>(
                "grill_pan", "Grill & Sear Pan (Pork / Bulgogi)", "jiwon_east",
                new Vector3(6.5f, 0.0f, -3.0f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.85f, 0.35f, 0.22f), "grill_pan", "cooking_pan_icon"
            );
            grill.applianceId = "grill_pan";

            var wok = CreateStation<CookingStation>(
                "simmer_wok", "Gochujang Simmer Wok (Tteokbokki)", "jiwon_east",
                new Vector3(6.5f, 0.0f, -0.8f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.78f, 0.18f, 0.16f), "simmer_wok", "cooking_pan_icon"
            );
            wok.applianceId = "simmer_wok";

            var assemblyEast = CreateStation<AssemblyStation>(
                "assembly_east", "ASSEMBLY EAST (Jiwon)", "jiwon_east",
                new Vector3(3.2f, 0.0f, 1.4f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.38f, 0.58f, 0.78f), "prep_east", "dish_plate"
            );

            var washingStation = CreateStation<WashingStation>(
                "washing_station", "WASHING SINK & DISH RACK", "jiwon_east",
                new Vector3(3.2f, 0.0f, 4.0f), new Vector3(2.8f, 0.9f, 1.2f),
                new Color(0.32f, 0.75f, 0.92f), "washing_sink", "dish_soap"
            );

            // East Crates
            CreateCrateStation("crate_daikon", "CRATE: Daikon & Carrot", "daikon_carrot_raw", new Vector3(1.6f, 0.0f, -4.4f), new Color(0.95f, 0.62f, 0.24f), "pickled_veg");
            CreateCrateStation("crate_kimchi", "CRATE: Aged Kimchi", "kimchi_raw", new Vector3(3.8f, 0.0f, -4.4f), new Color(0.88f, 0.26f, 0.18f), "kimchi_slaw");
            CreateCrateStation("crate_eomuk", "CRATE: Fish Cakes", "eomuk_raw", new Vector3(6.0f, 0.0f, -4.4f), new Color(0.88f, 0.76f, 0.54f), "fish_cake_raw");
            CreateCrateStation("crate_gochujang", "CRATE: Gochujang Sauce", "gochujang_sauce", new Vector3(7.6f, 0.0f, 1.4f), new Color(0.76f, 0.12f, 0.10f), "korean_sauce");
        }

        private T CreateStation<T>(
            string stId, string title, string z,
            Vector3 pos, Vector3 size, Color counterColor,
            string tileSpriteName, string iconSpriteName) where T : StationBase
        {
            GameObject stationObj = new GameObject(stId);
            stationObj.transform.SetParent(transform);
            stationObj.transform.position = pos;

            T comp = stationObj.AddComponent<T>();
            comp.stationId = stId;
            comp.displayName = title;
            comp.zone = z;

            // Physical BoxCollider (ensures characters cannot phase through appliances!)
            var col = stationObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, size.y * 0.5f, 0.0f);
            col.size = size;

            // 3D Counter Base Mesh
            GameObject counterMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counterMesh.name = $"{stId}_CounterMesh";
            counterMesh.transform.SetParent(stationObj.transform);
            counterMesh.transform.localPosition = new Vector3(0.0f, size.y * 0.5f, 0.0f);
            counterMesh.transform.localScale = size;
            var subCol = counterMesh.GetComponent<Collider>();
            if (subCol != null) Destroy(subCol);

            var mr = counterMesh.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = counterColor;
                mat.SetFloat("_Glossiness", 0.45f);
                mat.SetFloat("_Metallic", 0.4f);
                mr.material = mat;
            }

            // Upright 2D Sliced Pixel-Art Appliance Sprite
            if (!string.IsNullOrEmpty(tileSpriteName))
            {
                GameObject sprObj = new GameObject("ApplianceSprite");
                sprObj.transform.SetParent(stationObj.transform);
                sprObj.transform.localPosition = new Vector3(0.0f, size.y + 0.42f, 0.05f);
                sprObj.transform.rotation = Quaternion.Euler(52.0f, 0.0f, 0.0f); // Match orthographic camera tilt
                var sr = sprObj.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>($"Sprites/Kitchen/Tiles/{tileSpriteName}");
                sprObj.transform.localScale = Vector3.one * 1.6f;
            }

            // Floating Station Icon
            if (!string.IsNullOrEmpty(iconSpriteName))
            {
                GameObject iconObj = new GameObject("StationIcon");
                iconObj.transform.SetParent(stationObj.transform);
                iconObj.transform.localPosition = new Vector3(0.0f, size.y + 1.1f, 0.0f);
                iconObj.transform.rotation = Quaternion.Euler(52.0f, 0.0f, 0.0f);
                var isr = iconObj.AddComponent<SpriteRenderer>();
                isr.sprite = Resources.Load<Sprite>($"Sprites/Items/{iconSpriteName}");
                iconObj.transform.localScale = Vector3.one * 1.2f;
            }

            stations[stId] = comp;
            return comp;
        }

        private CrateStation CreateCrateStation(
            string stId, string title, string itemId,
            Vector3 pos, Color crateColor, string iconSpriteName)
        {
            Vector3 size = new Vector3(1.4f, 0.8f, 1.1f);
            var crate = CreateStation<CrateStation>(
                stId, title, "crate",
                pos, size, crateColor, "crate_cooler", iconSpriteName
            );
            crate.dispensedItemId = itemId;
            return crate;
        }

        public void SpawnPlayers()
        {
            // Clear previous player tracking if rebuild
            playersByPeer.Clear();

            // Spawn Linh (Player 1) at (-2.0, 0, 0)
            SpawnChef(1, "linh", "prep_master", 1, new Vector3(-2.0f, 0.0f, 0.0f));

            // Spawn Jiwon (Player 2) at (2.0, 0, 0)
            SpawnChef(2, "jiwon", "flame_keeper", 1, new Vector3(2.0f, 0.0f, 0.0f));
        }

        private PlayerController SpawnChef(int peerId, string charId, string specId, int tier, Vector3 spawnPos)
        {
            GameObject chefObj = new GameObject($"Chef_{charId.ToUpper()}_Peer{peerId}");
            chefObj.transform.SetParent(transform);
            chefObj.transform.position = spawnPos;

            // Character Controller for physics movement & non-phasing collisions
            var cc = chefObj.AddComponent<CharacterController>();
            cc.radius = 0.45f;
            cc.height = 1.3f;
            cc.center = new Vector3(0.0f, 0.65f, 0.0f);
            cc.stepOffset = 0.3f;

            var playerComp = chefObj.AddComponent<PlayerController>();

            // Upright Chef SpriteRenderer
            GameObject sprChild = new GameObject("ChefSprite");
            sprChild.transform.SetParent(chefObj.transform);
            sprChild.transform.localPosition = new Vector3(0.0f, 0.7f, 0.0f);
            sprChild.transform.rotation = Quaternion.Euler(52.0f, 0.0f, 0.0f); // Match camera tilt
            var sr = sprChild.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>($"Sprites/Characters/{charId}_tier{tier}");
            sprChild.transform.localScale = Vector3.one * 1.8f;
            playerComp.characterSpriteRenderer = sr;

            // Carried Item SpriteRenderer (hovering over head with tilt & bobbing)
            GameObject itemChild = new GameObject("CarriedItemSprite");
            itemChild.transform.SetParent(chefObj.transform);
            itemChild.transform.localPosition = new Vector3(0.0f, 1.85f, 0.0f);
            itemChild.transform.rotation = Quaternion.Euler(52.0f, 0.0f, 0.0f);
            var itemSr = itemChild.AddComponent<SpriteRenderer>();
            itemChild.transform.localScale = Vector3.one * 1.3f;
            itemChild.SetActive(false);
            playerComp.carriedItemSpriteRenderer = itemSr;

            playerComp.ConfigureChef(peerId, charId, specId, tier);
            playersByPeer[peerId] = playerComp;
            return playerComp;
        }

        public (bool ok, string reason, string workerId) RequestHireWorker(int peerId, string roleId)
        {
            if (!playersByPeer.TryGetValue(peerId, out var p))
                return (false, "Unknown chef.", "");

            if (p.characterId != "jiwon")
                return (false, "Only Jiwon has the delegation authority to hire part-time crew!", "");

            int tierSlots = 0;
            if (GameData.Instance != null && GameData.Instance.jiwonTiers.TryGetValue(p.characterTier, out var jTier))
            {
                tierSlots = jTier.partTimerSlots;
            }

            if (tierSlots <= 0)
                return (false, "Jiwon is Tier 1 (Burnt-out Executive). Complete Level 3 to unlock Part-Timer hiring!", "");

            if (activeWorkers.Count >= tierSlots)
                return (false, $"Maximum part-timer capacity reached ({activeWorkers.Count}/{tierSlots} slots active)!", "");

            if (GameData.Instance == null || !GameData.Instance.workerCatalog.TryGetValue(roleId, out var spec))
                return (false, $"Unknown worker role: {roleId}", "");

            if (workerCooldowns.TryGetValue(roleId, out float cd) && cd > 0.0f)
                return (false, $"{spec.displayName} contract agency is on cooldown ({cd:F0}s left)!", "");

            if (ProgressionManager.Instance == null || ProgressionManager.Instance.coins < spec.hireCost)
                return (false, $"Not enough restaurant coins! Need {spec.hireCost} coins.", "");

            StationBase targetStation = null;
            if (roleId == "prep_assistant")
            {
                stations.TryGetValue("prep_east", out targetStation);
                if (targetStation == null) stations.TryGetValue("prep_west", out targetStation);
            }
            else if (roleId == "dishwasher")
            {
                stations.TryGetValue("washing_station", out targetStation);
            }

            if (targetStation == null)
                return (false, $"No station available for {spec.displayName}.", "");

            // Deduct coins & apply cooldown
            ProgressionManager.Instance.coins -= spec.hireCost;
            workerCooldowns[roleId] = spec.cooldownDuration;
            ProgressionManager.Instance.SaveProgression();

            workerUidCounter++;
            string wid = $"worker_{workerUidCounter}";

            GameObject workerObj = new GameObject(wid);
            workerObj.transform.SetParent(transform);
            var workerComp = workerObj.AddComponent<PartTimeWorker>();
            workerComp.InitializeWorker(wid, roleId, targetStation);

            workerComp.OnWorkerExpired += (expId) =>
            {
                if (activeWorkers.ContainsKey(expId))
                    activeWorkers.Remove(expId);
            };

            activeWorkers[wid] = workerComp;

            OnStatusToast?.Invoke($"Hired {spec.displayName} for {spec.contractDuration:F0}s! (-{spec.hireCost} coins)", new Color(0.35f, 1.0f, 0.6f));
            return (true, "", wid);
        }

        public StationBase FindNearestStation(Vector3 pos)
        {
            StationBase nearest = null;
            float minDst = StationBase.MAX_INTERACT_DISTANCE;

            foreach (var st in stations.Values)
            {
                float d = Vector3.Distance(st.transform.position, pos);
                if (d <= minDst)
                {
                    minDst = d;
                    nearest = st;
                }
            }

            return nearest;
        }
    }
}
