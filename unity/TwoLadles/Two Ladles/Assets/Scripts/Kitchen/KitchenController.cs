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
        public event Action<int> OnChefSwitched;

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
            if (autoBuildKitchenOnStart && transform.childCount == 0)
            {
                BuildEnvironment();
                BuildPerimeterWalls();
                BuildStations();
                BuildDecorations();
                SpawnPlayers();
            }
            else
            {
                // Register stations and players already present in the pre-baked scene
                foreach (var st in GetComponentsInChildren<StationBase>())
                {
                    stations[st.stationId] = st;
                }
                foreach (var chef in FindObjectsOfType<Player.PlayerController>())
                {
                    playersByPeer[chef.peerId] = chef;
                }
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
                if (nearest is PrepStation)
                    prompt = $"{nearest.displayName}  •  [E] Place / Pick  |  [F] Strike";
                else if (nearest is CookingStation)
                    prompt = $"{nearest.displayName}  •  [E] Cook / Retrieve";
                else if (nearest is PassCounter)
                    prompt = $"Pass Counter  •  [E] Place / Retrieve (+15 Flow)";
                else if (nearest is CounterStation)
                    prompt = $"Holding Table  •  [E] Place / Pick Item";
                else if (nearest is AssemblyStation)
                    prompt = $"{nearest.displayName}  •  [E] Add Component / Plate";
                else if (nearest is ServingStation)
                    prompt = $"Serving Counter  •  [E] Deliver Completed Dish";
                else if (nearest is WashingStation)
                    prompt = $"Washing Sink  •  [E] Clean Plate  |  [F] Clean Bowl";
                else if (nearest is CrateStation crate)
                    prompt = $"{crate.displayName}  •  [E] Take";
                else if (nearest is TrashStation)
                    prompt = $"Trash Bin  •  [E] Dispose Item";
                else
                    prompt = $"{nearest.displayName}  •  [E] Interact";
            }
            else
            {
                prompt = "Move near a station to interact  [E: Use | F: Strike | Q: Throw]";
            }

            if (prompt != currentPrompt)
            {
                currentPrompt = prompt;
                OnPromptChanged?.Invoke(prompt);
            }
        }

        public void TriggerStatusToast(string message, Color color)
        {
            if (AudioManager.Instance != null)
            {
                if (color.r > 0.8f && color.g < 0.5f)
                    AudioManager.Instance.PlayError();
                else if (message != null && (message.Contains("Served") || message.Contains("Flow") || message.Contains("Academy")))
                    AudioManager.Instance.PlayBell();
                else if (message != null && (message.Contains("Chop") || message.Contains("Strike") || message.Contains("Prep")))
                    AudioManager.Instance.PlayChop();
                else if (message != null && (message.Contains("Cook") || message.Contains("Grill") || message.Contains("Simmer") || message.Contains("Blanch")))
                    AudioManager.Instance.PlaySizzle();
                else
                    AudioManager.Instance.PlayPickup();
            }
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

        public void SwitchLocalChef()
        {
            int currentLocal = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            int nextLocal = (currentLocal == 1) ? 2 : 1;
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.localPeerId = nextLocal;
            }
            foreach (var kvp in playersByPeer)
            {
                kvp.Value.isLocalPlayer = (kvp.Key == nextLocal);
            }
            UpdateLocalPrompt();
            string chefName = (nextLocal == 1) ? "Chef Linh (West Zone)" : "Chef Jiwon (East Zone)";
            TriggerStatusToast($"Active Chef: {chefName}", new Color(0.35f, 0.9f, 1.0f));
            OnChefSwitched?.Invoke(nextLocal);

            if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 5 && nextLocal == 2)
            {
                ShiftManager.Instance.AdvanceTutorialStep(6);
            }
        }

        public void BuildEnvironment()
        {
            // 1. Camera (Expanded 2.5D Orthographic FOV, pushed back to encompass all bounds without clipping)
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("KitchenCamera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 9.5f;
                camObj.transform.position = new Vector3(0.0f, 15.0f, -11.0f);
                camObj.transform.LookAt(new Vector3(0.0f, 0.0f, 0.6f), Vector3.up);
            }
            else
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 9.5f;
                Camera.main.transform.position = new Vector3(0.0f, 15.0f, -11.0f);
                Camera.main.transform.LookAt(new Vector3(0.0f, 0.0f, 0.6f), Vector3.up);
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
                lightObj.transform.rotation = Quaternion.Euler(51.0f, 28.0f, 0.0f);
            }

            // 3. Procedural Kitchen Floor Grid (Normal and Broken Tiles)
            BuildFloorTileGrid();
        }

        private void BuildFloorTileGrid()
        {
            GameObject floorRoot = new GameObject("FloorTileGrid");
            floorRoot.transform.SetParent(transform);
            floorRoot.transform.position = new Vector3(0.0f, -0.05f, 0.0f);
            floorRoot.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);

            // Deterministic seeded pseudo-random distribution for authentic worn/broken tiles
            System.Random rng = new System.Random(1337);

            Sprite cleanSprite = GetStationSprite("floor_tile_clean");
            Sprite brokenSprite = GetStationSprite("floor_tile_broken");

            // All floor tiles share the exact same Z-axis plane (local Z = 0.0f)
            // X from -8.5 to +8.5 (step 1.0m, 18 columns)
            // Y from -4.5 to +3.5 (step 1.0m, 9 rows meeting the base of the North wall)
            for (float x = -8.5f; x <= 8.5f; x += 1.0f)
            {
                for (float y = -4.5f; y <= 3.5f; y += 1.0f)
                {
                    GameObject tileObj = new GameObject($"Tile_{x:F1}_{y:F1}");
                    tileObj.transform.SetParent(floorRoot.transform, false);
                    tileObj.transform.localPosition = new Vector3(x, y, 0.0f);
                    tileObj.transform.localRotation = Quaternion.identity;
                    tileObj.transform.localScale = Vector3.one * 1.01f;

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    bool isBroken = rng.NextDouble() < 0.16;
                    sr.sprite = (isBroken && brokenSprite != null) ? brokenSprite : cleanSprite;
                    sr.sortingOrder = -50;

                    // Authentic subtle kitchen zone tinting
                    if (Mathf.Abs(x) <= 0.35f)
                    {
                        // Golden center boundary divider line
                        sr.color = new Color(0.92f, 0.82f, 0.45f);
                    }
                    else if (x < 0f)
                    {
                        // Linh's West zone: cool teal-grey tile
                        sr.color = isBroken ? new Color(0.85f, 0.88f, 0.86f) : new Color(0.92f, 0.96f, 0.94f);
                    }
                    else
                    {
                        // Jiwon's East zone: warm terracotta-tinted tile
                        sr.color = isBroken ? new Color(0.90f, 0.84f, 0.80f) : new Color(0.96f, 0.92f, 0.88f);
                    }
                }
            }
        }

        public void BuildPerimeterWalls()
        {
            // 1. Physical Boundaries (Invisible BoxColliders)
            CreateStaticWall("Wall_North", new Vector3(0.0f, 1.5f, 5.8f), new Vector3(19.2f, 3.0f, 0.8f));
            CreateStaticWall("Wall_South", new Vector3(0.0f, 1.5f, -5.8f), new Vector3(19.2f, 3.0f, 0.8f));
            CreateStaticWall("Wall_West", new Vector3(-9.4f, 1.5f, 0.0f), new Vector3(0.8f, 3.0f, 12.0f));
            CreateStaticWall("Wall_East", new Vector3(9.4f, 1.5f, 0.0f), new Vector3(0.8f, 3.0f, 12.0f));

            // Central Divider Barriers (Chefs stay in their assigned half, only interacting via Pass Counter!)
            CreateStaticWall("CenterDivider_North", new Vector3(0.0f, 0.75f, 3.6f), new Vector3(0.45f, 1.5f, 3.2f));
            CreateStaticWall("CenterDivider_South", new Vector3(0.0f, 0.75f, -2.1f), new Vector3(0.45f, 1.5f, 1.9f));
            CreateStaticWall("CenterDivider_Bottom", new Vector3(0.0f, 0.75f, -4.75f), new Vector3(0.45f, 1.5f, 1.3f));

            // 2. Visual Back North Wall (Deleted lowest 0.6f tier; reversed front/back order so front tier covers upper baseboards)
            GameObject wallRoot = new GameObject("VisualBackWall");
            wallRoot.transform.SetParent(transform);
            Sprite wallSprite = GetStationSprite("wall_south");

            if (wallSprite != null)
            {
                float[] wallYLevels = new float[] { 1.8f, 3.0f, 4.2f };
                for (int i = 0; i < wallYLevels.Length; i++)
                {
                    float y = wallYLevels[i];
                    // Reverse order: front/lower tier (i=0, y=1.8) has higher sortingOrder (-14)
                    // than back/upper tiers (-15, -16), hiding repeated baseboards on upper tiers.
                    int tierSortingOrder = -14 - i;

                    for (float x = -8.5f; x <= 8.5f; x += 1.0f)
                    {
                        // Door opening is 2.8m wide, 2.2m tall: skip tiles inside door frame
                        if (y < 2.2f && Mathf.Abs(x) < 1.4f) continue;

                        GameObject wObj = new GameObject($"WallTile_{x:F1}_{y:F1}");
                        wObj.transform.SetParent(wallRoot.transform);
                        wObj.transform.position = new Vector3(x, y, 5.2f);
                        wObj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                        wObj.transform.localScale = new Vector3(1.02f, 1.55f, 1.0f);

                        var sr = wObj.AddComponent<SpriteRenderer>();
                        sr.sprite = wallSprite;
                        sr.sortingOrder = tierSortingOrder;
                        sr.color = (x < 0f) ? new Color(0.88f, 0.92f, 0.90f) : new Color(0.92f, 0.88f, 0.85f);
                    }
                }
            }
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

        private GameObject CreateFloorMesh(string floorName, Vector3 pos, Vector2 size, Color col, float tileDensity = 0.55f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = floorName;
            obj.transform.SetParent(transform);
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            obj.transform.localScale = new Vector3(size.x, size.y, 1.0f);

            var c = obj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader s = Shader.Find("Sprites/Default");
                Material mat = s != null ? new Material(s) : mr.material;
                mat.color = col;
                Texture2D floorTex = Resources.Load<Texture2D>("Sprites/Kitchen/floor_tile_clean");
                if (floorTex != null)
                {
                    mat.mainTexture = floorTex;
                    mat.mainTextureScale = new Vector2(size.x * tileDensity, size.y * tileDensity);
                }
                mr.material = mat;
            }
            return obj;
        }

        public void BuildStations()
        {
            // Clear previous stations if rebuild
            stations.Clear();
            _kitchenSpritesCache = null;

            // --- CENTER: SHARED PASS COUNTER & TRASH ---
            var pass = CreateStation<PassCounter>(
                "pass_counter", "PASS COUNTER", "shared",
                new Vector3(0.0f, 0.0f, 0.4f), new Vector3(1.6f, 0.9f, 3.2f),
                new Color(0.78f, 0.80f, 0.84f), "table_double_vertical", "dish_plate"
            );

            var trash = CreateStation<TrashStation>(
                "trash_bin", "TRASH BIN", "shared",
                new Vector3(0.0f, 0.0f, -3.6f), new Vector3(1.1f, 0.8f, 1.1f),
                new Color(0.35f, 0.35f, 0.38f), "table_single_horizontal", "dish_brush"
            );

            // --- WEST ZONE (LINH'S SIDE): Preps, Holding, Stockpot, Blancher, Assembly, Serving, Crates ---
            var prepWest = CreateStation<PrepStation>(
                "prep_west", "RESPONSIVE PREP COUNTER", "linh_west",
                new Vector3(-3.2f, 0.0f, 2.6f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.72f, 0.75f, 0.78f), "prep_table_pot", "prep_counter_icon"
            );
            // Universal prep counter: only 1 prep table needed in kitchen
            stations["prep_east"] = prepWest;

            var counterWest = CreateStation<CounterStation>(
                "counter_west", "HOLDING TABLE (West)", "linh_west",
                new Vector3(-3.2f, 0.0f, 0.6f), new Vector3(2.0f, 0.9f, 1.1f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", "dish_plate"
            );

            var stockpot = CreateStation<CookingStation>(
                "stockpot", "Dedicated Phở Stockpot", "linh_west",
                new Vector3(-6.5f, 0.0f, 3.2f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.30f, 0.30f, 0.33f), "stove_south_pot", "broth_pot"
            );
            stockpot.applianceId = "stockpot";

            var blancher = CreateStation<CookingStation>(
                "noodle_blancher", "Noodle Blancher", "linh_west",
                new Vector3(-6.5f, 0.0f, 1.0f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.30f, 0.30f, 0.33f), "stove_south_on", "dish_bowl"
            );
            blancher.applianceId = "noodle_blancher";

            var assemblyWest = CreateStation<AssemblyStation>(
                "assembly_west", "ASSEMBLY WEST (Linh)", "linh_west",
                new Vector3(-3.2f, 0.0f, -1.4f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", "dish_bowl"
            );

            var servingStation = CreateStation<ServingStation>(
                "serving_station", "SERVING COUNTER", "shared",
                new Vector3(-3.2f, 0.0f, -4.0f), new Vector3(2.8f, 0.9f, 1.2f),
                new Color(0.75f, 0.78f, 0.82f), "table_double_horizontal", "baguette_bread"
            );

            // West Storage: North-West Corner Cooler & Exactly 1 West Shelf (Pantry)
            var fridgeWest = CreateCrateStation("fridge_west", "WEST COOLER (Meat / Cold)", "pork_raw", new Vector3(-7.6f, 0.0f, 4.2f), new Color(0.85f, 0.88f, 0.92f), "fridge_south", "pork_belly_raw");
            var shelfWest = CreateCrateStation("shelf_west", "WEST PANTRY (Bread / Dry)", "baguette", new Vector3(-7.8f, 0.0f, -2.0f), new Color(0.70f, 0.65f, 0.58f), "shelf_horizontal", "baguette_bread");
            stations["crate_pork"] = fridgeWest;
            stations["crate_beef"] = fridgeWest;
            stations["crate_bulgogi"] = fridgeWest;
            stations["crate_baguette"] = shelfWest;
            stations["crate_noodles"] = shelfWest;
            stations["crate_flour"] = shelfWest;

            // --- EAST ZONE (JIWON'S SIDE): Holding, Grill, Simmer Wok, Assembly, Washing, Crates ---
            var counterEast = CreateStation<CounterStation>(
                "counter_east", "HOLDING TABLE (East)", "jiwon_east",
                new Vector3(3.2f, 0.0f, 0.6f), new Vector3(2.0f, 0.9f, 1.1f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", "dish_plate"
            );

            var grill = CreateStation<CookingStation>(
                "grill_pan", "Grill & Sear Pan (Pork / Bulgogi)", "jiwon_east",
                new Vector3(6.5f, 0.0f, 3.0f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.30f, 0.30f, 0.33f), "grill_south", "cooking_pan_icon"
            );
            grill.applianceId = "grill_pan";

            var wok = CreateStation<CookingStation>(
                "simmer_wok", "Gochujang Simmer Wok (Tteokbokki)", "jiwon_east",
                new Vector3(6.5f, 0.0f, 0.8f), new Vector3(1.6f, 0.9f, 1.4f),
                new Color(0.30f, 0.30f, 0.33f), "stove_south_on", "cooking_pan_icon"
            );
            wok.applianceId = "simmer_wok";

            var assemblyEast = CreateStation<AssemblyStation>(
                "assembly_east", "ASSEMBLY EAST (Jiwon)", "jiwon_east",
                new Vector3(3.2f, 0.0f, -1.4f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", "dish_plate"
            );

            var washingStation = CreateStation<WashingStation>(
                "washing_station", "WASHING SINK & DISH RACK", "jiwon_east",
                new Vector3(3.2f, 0.0f, -4.0f), new Vector3(2.8f, 0.9f, 1.2f),
                new Color(0.40f, 0.70f, 0.85f), "sink_double_south", "dish_soap"
            );

            // East Storage: North-East Corner Cooler & Exactly 1 East Shelf (Grains / Sauces)
            var fridgeEast = CreateCrateStation("fridge_east", "EAST COOLER (Veg / Cold)", "kimchi_raw", new Vector3(7.6f, 0.0f, 4.2f), new Color(0.85f, 0.88f, 0.92f), "fridge_south", "kimchi_slaw");
            var shelfEast = CreateCrateStation("shelf_east", "EAST PANTRY (Grains / Sauces)", "tteok_raw", new Vector3(7.8f, 0.0f, -2.0f), new Color(0.70f, 0.65f, 0.58f), "shelf_horizontal", "tteok_portioned");
            stations["crate_kimchi"] = fridgeEast;
            stations["crate_daikon"] = fridgeEast;
            stations["crate_tteok"] = shelfEast;
            stations["crate_eomuk"] = shelfEast;
            stations["crate_gochujang"] = shelfEast;

            // Build Authentic Decorative Kitchen Props
            BuildDecorations();
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

            // Compact Physical BoxCollider: tight bounds allowing chefs to walk close to stations smoothly
            var col = stationObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, 0.45f, 0.0f);
            col.size = new Vector3(Mathf.Min(size.x * 0.75f, 1.4f), 0.9f, 0.65f);

            // Upright 2D Sliced Pixel-Art Appliance Sprite resting flush on the floor tile
            int stationBaseOrder = 500 - (int)(pos.z * 50);
            if (!string.IsNullOrEmpty(tileSpriteName))
            {
                GameObject sprObj = new GameObject("ApplianceSprite");
                sprObj.transform.SetParent(stationObj.transform);
                sprObj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f); // Match orthographic camera tilt
                var sr = sprObj.AddComponent<SpriteRenderer>();
                sr.sprite = GetStationSprite(tileSpriteName);
                sr.sortingOrder = stationBaseOrder;

                float bottomOffset = 0.0f;
                if (sr.sprite != null)
                {
                    bottomOffset = -sr.sprite.bounds.min.y;
                }
                float scale = 1.35f;
                sprObj.transform.localScale = Vector3.one * scale;
                sprObj.transform.localPosition = new Vector3(0.0f, bottomOffset * scale, 0.0f);
            }

            // Floating Station Icon
            if (!string.IsNullOrEmpty(iconSpriteName))
            {
                GameObject iconObj = new GameObject("StationIcon");
                iconObj.transform.SetParent(stationObj.transform);
                iconObj.transform.localPosition = new Vector3(0.0f, 1.35f, 0.0f);
                iconObj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                var isr = iconObj.AddComponent<SpriteRenderer>();
                isr.sprite = Resources.Load<Sprite>($"Sprites/Items/{iconSpriteName}");
                isr.sortingOrder = stationBaseOrder + 1;
                iconObj.transform.localScale = Vector3.one * 0.85f;
            }

            stations[stId] = comp;
            return comp;
        }

        private CrateStation CreateCrateStation(
            string stId, string title, string itemId,
            Vector3 pos, Color crateColor, string spriteName, string iconSpriteName)
        {
            Vector3 size = new Vector3(1.4f, 0.8f, 1.1f);
            var crate = CreateStation<CrateStation>(
                stId, title, "crate",
                pos, size, crateColor, spriteName, iconSpriteName
            );
            crate.dispensedItemId = itemId;
            return crate;
        }

        private CrateStation CreateCrateStation(
            string stId, string title, string itemId,
            Vector3 pos, Color crateColor, string iconSpriteName)
        {
            return CreateCrateStation(stId, title, itemId, pos, crateColor, "fridge_south", iconSpriteName);
        }

        public void AddDecorSprite(string spriteName, Vector3 pos, Vector3 scale, int sortingOrderOffset = 0)
        {
            GameObject obj = new GameObject($"Decor_{spriteName}");
            obj.transform.SetParent(transform);
            obj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = GetStationSprite(spriteName);
            int baseOrder = 500 - (int)(pos.z * 50);
            sr.sortingOrder = baseOrder + sortingOrderOffset;
            obj.transform.localScale = scale;

            float posY = pos.y;
            if (sr.sprite != null && pos.y == 0.0f)
            {
                posY = -sr.sprite.bounds.min.y * scale.y;
            }
            obj.transform.position = new Vector3(pos.x, posY, pos.z);
        }

        public void BuildDecorations()
        {
            // 1. Double entrance doors at North Wall center (flush at floor boundary)
            AddDecorSprite("door_double_closed", new Vector3(0.0f, 0.0f, 5.05f), Vector3.one * 1.5f, -10);

            // 2. Center Divider Counters along X=0 (Framing the Pass Counter to physically block phasing across)
            AddDecorSprite("table_single_vertical", new Vector3(0.0f, 0.0f, 2.6f), Vector3.one * 1.35f, 0);
            AddDecorSprite("table_single_vertical", new Vector3(0.0f, 0.0f, 4.0f), Vector3.one * 1.35f, 0);
            AddDecorSprite("table_single_vertical", new Vector3(0.0f, 0.0f, -2.1f), Vector3.one * 1.35f, 0);

            // 3. Wall Hung Utensils on North Wall (hung high up at Y = 2.8m)
            AddDecorSprite("utensils_wall_1", new Vector3(-3.4f, 2.8f, 5.15f), Vector3.one * 1.3f, -5);
            AddDecorSprite("utensils_wall_2", new Vector3(3.8f, 2.8f, 5.15f), Vector3.one * 1.3f, -5);
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

            // Upright Chef SpriteRenderer (Proportional ~1.45m chef height facing orthographic camera)
            GameObject sprChild = new GameObject("ChefSprite");
            sprChild.transform.SetParent(chefObj.transform);
            sprChild.transform.localPosition = new Vector3(0.0f, 0.72f, 0.0f);
            sprChild.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f); // Match camera tilt
            var sr = sprChild.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>($"Sprites/Characters/{charId}_tier{tier}");
            if (sr.sprite != null && sr.sprite.bounds.size.y > 0.01f)
            {
                float scale = 1.95f / sr.sprite.bounds.size.y;
                sprChild.transform.localScale = Vector3.one * scale;
            }
            else
            {
                sprChild.transform.localScale = Vector3.one * 0.1f;
            }
            playerComp.characterSpriteRenderer = sr;

            // Carried Item SpriteRenderer (hovering directly above chef's head with tilt & bobbing)
            GameObject itemChild = new GameObject("CarriedItemSprite");
            itemChild.transform.SetParent(chefObj.transform);
            itemChild.transform.localPosition = new Vector3(0.0f, 1.85f, 0.0f);
            itemChild.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            var itemSr = itemChild.AddComponent<SpriteRenderer>();
            itemChild.transform.localScale = Vector3.one * 0.55f;
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

        private static Dictionary<string, Sprite> _kitchenSpritesCache = null;

        public static void InvalidateSpriteCache()
        {
            _kitchenSpritesCache = null;
        }

        public Sprite GetStationSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_kitchenSpritesCache == null)
            {
                _kitchenSpritesCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

                // 1. Slices from master spritesheets in Resources
                string[] sheets = new string[] {
                    "Sprites/Kitchen/professional_kitchen",
                    "Sprites/Kitchen/professional_kitchen_withshadows",
                    "Sprites/Kitchen/professional_kitchen_room_door_tiles"
                };

                foreach (var sPath in sheets)
                {
                    Sprite[] subs = Resources.LoadAll<Sprite>(sPath);
                    if (subs != null)
                    {
                        foreach (var s in subs)
                        {
                            if (s != null && !_kitchenSpritesCache.ContainsKey(s.name))
                                _kitchenSpritesCache[s.name] = s;
                        }
                    }
                }

#if UNITY_EDITOR
                // In Editor, also load directly via AssetDatabase in case user sliced in Assets/Sprites/
                string[] editorPaths = new string[] {
                    "Assets/Resources/Sprites/Kitchen/professional_kitchen.png",
                    "Assets/Sprites/Kitchen/professional_kitchen.png",
                    "Assets/Resources/Sprites/Kitchen/professional_kitchen_withshadows.png",
                    "Assets/Sprites/Kitchen/professional_kitchen_withshadows.png"
                };
                foreach (var ep in editorPaths)
                {
                    var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(ep);
                    if (assets != null)
                    {
                        foreach (var a in assets)
                        {
                            if (a is Sprite sp && !_kitchenSpritesCache.ContainsKey(sp.name))
                            {
                                _kitchenSpritesCache[sp.name] = sp;
                            }
                        }
                    }
                }
#endif
            }

            // 1. Direct exact match
            if (_kitchenSpritesCache.TryGetValue(name, out Sprite exact))
            {
                return exact;
            }

            // 2. Built-in station alias lookup
            string alias = GetStationSpriteAlias(name);
            if (!string.IsNullOrEmpty(alias) && _kitchenSpritesCache.TryGetValue(alias, out Sprite aliased))
            {
                return aliased;
            }

            // 3. Keyword/substring match (e.g. user named slice "grill", station requests "grill_pan")
            foreach (var kvp in _kitchenSpritesCache)
            {
                string sliceName = kvp.Key;
                if (sliceName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf(sliceName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Value;
                }
            }

            // 4. Fallback to separate single-tile files
            Sprite fallback = Resources.Load<Sprite>($"Sprites/Kitchen/{name}");
            return fallback;
        }

        private string GetStationSpriteAlias(string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "stockpot": return "stove_south_pot";
                case "noodle_blancher": return "stove_south_on";
                case "grill_pan": return "grill_south";
                case "simmer_wok": return "stove_south_on";
                case "prep_west": return "prep_table_pot";
                case "prep_east": return "prep_table_dry";
                case "washing_station":
                case "washing_sink": return "sink_double_south";
                case "pass_counter": return "table_double_vertical";
                case "serving_station": return "table_double_horizontal";
                case "counter_west":
                case "counter_east":
                case "counter_stainless": return "table_double_horizontal";
                case "trash_bin": return "table_single_horizontal";
                case "crate_cooler": return "fridge_south";
                default: return null;
            }
        }
    }
}
