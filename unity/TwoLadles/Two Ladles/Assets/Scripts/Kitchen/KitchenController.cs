using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Player;
using TwoLadles.Networking;
using TwoLadles.AI;

namespace TwoLadles.Kitchen
{
    public class KitchenController : MonoBehaviour
    {
        public static KitchenController Instance { get; set; }

        /// <summary>World height (units) of hired part-timer sprites. Chefs are ~1.9; part-timers slightly smaller but readable.</summary>
        public const float PART_TIMER_HEIGHT = 1.75f;

        // Sprites that show whatever item sits on a station/chef. Empty is normal, so they never trigger a rebuild.
        public static bool IsRuntimeItemRenderer(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            return objectName == "FrontHeldItemSprite" || objectName == "CarriedItemSprite" || objectName.StartsWith("PassSlotSprite_");
        }
        public const float CAMERA_ORTHO_SIZE = 11.2f;
        public const float MOBILE_CAMERA_ORTHO_SIZE = 9.6f; // phones: zoom in a bit more
        public static float CameraOrthoSize(bool mobile) => mobile ? MOBILE_CAMERA_ORTHO_SIZE : CAMERA_ORTHO_SIZE;

        // Yard decor on the grass outside the building (kept out of the walkable kitchen/dining box)
        public static readonly (string sprite, Vector3 pos)[] YARD_DECOR = new (string, Vector3)[] {
            ("yard_tree", new Vector3(-14.0f, 0f, 6.0f)),  ("yard_tree", new Vector3(14.0f, 0f, 6.0f)),
            ("yard_tree", new Vector3(-16.5f, 0f, -2.0f)), ("yard_tree", new Vector3(16.5f, 0f, -2.0f)),
            ("yard_tree", new Vector3(-13.5f, 0f, -10.0f)), ("yard_tree", new Vector3(13.5f, 0f, -10.0f)),
            ("yard_tree", new Vector3(-18.0f, 0f, 11.0f)), ("yard_tree", new Vector3(18.0f, 0f, 11.0f)),
            ("yard_tree", new Vector3(-21.0f, 0f, 3.0f)),  ("yard_tree", new Vector3(21.0f, 0f, -7.0f)),
            ("yard_bush", new Vector3(-12.6f, 0f, 2.0f)),  ("yard_bush", new Vector3(12.6f, 0f, 1.0f)),
            ("yard_bush", new Vector3(-6.0f, 0f, -9.5f)),  ("yard_bush", new Vector3(6.5f, 0f, -10.5f)),
            ("yard_bush", new Vector3(-17.5f, 0f, -12.0f)), ("yard_bush", new Vector3(19.0f, 0f, 7.5f)),
            ("yard_flowers", new Vector3(-2.5f, 0f, -11.0f)), ("yard_flowers", new Vector3(10.0f, 0f, -12.5f)),
            ("yard_flowers", new Vector3(-15.0f, 0f, 9.0f)), ("yard_flowers", new Vector3(15.5f, 0f, -5.0f)),
            ("yard_rock", new Vector3(-15.0f, 0f, -6.0f)), ("yard_rock", new Vector3(3.0f, 0f, -12.0f)),
            ("yard_rock", new Vector3(17.0f, 0f, 13.5f)),
        };
        /// <summary>Serving Counter sits south of the North double door so the doorway stays clear.</summary>
        public static readonly Vector3 SERVING_STATION_POS = new Vector3(0.0f, 0.0f, 3.2f);
        /// <summary>Decor divider tables along x=0 (none near the North double door).</summary>
        public static readonly float[] DIVIDER_DECOR_Z = new float[] { -1.45f, -2.50f, -3.55f, -4.55f };
        /// <summary>How long the North double door shows its open sprite after a dish is served.</summary>
        public const float DOOR_OPEN_SECONDS = 1.0f;
        private float _doorOpenTimer = 0f;
        private SpriteRenderer _doorRenderer;
        /// <summary>True while the double door is showing its open sprite.</summary>
        public bool DoorOpen => _doorOpenTimer > 0f;

        /// <summary>Swing the double door open for DOOR_OPEN_SECONDS (called when a dish is served).</summary>
        public void OpenDoorBriefly()
        {
            _doorOpenTimer = DOOR_OPEN_SECONDS;
            ApplyDoorSprite();
        }

        /// <summary>Count down the open-door timer and swap back to the closed sprite when it runs out.</summary>
        public void TickDoor(float dt)
        {
            if (_doorOpenTimer <= 0f) return;
            _doorOpenTimer = Mathf.Max(0f, _doorOpenTimer - dt);
            if (_doorOpenTimer <= 0f) ApplyDoorSprite();
        }

        private void ApplyDoorSprite()
        {
            if (_doorRenderer == null)
            {
                Transform t = transform.Find("Decor_door_double_closed");
                if (t != null) _doorRenderer = t.GetComponent<SpriteRenderer>();
            }
            if (_doorRenderer == null) return;
            Sprite s = GetStationSprite(DoorOpen ? "door_double_open" : "door_double_closed");
            if (s != null) _doorRenderer.sprite = s;
        }

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
        private GameObject _tutorialArrowObj;
        private static Sprite _tutorialArrowSprite;

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
                SpawnPlayers();
            }
            else if (transform.childCount > 0)
            {
                // Check if runtime sprites were cleared during Unity Edit->Play serialization or if layout was upgraded
                bool needsSpriteRebuild = false;
                Transform restCorner = transform.Find("FloorTileGrid/VisualBackWall/RestCorner_UpperLeft_v5");

                // Stale baked layout check: the current layout adds counter_west_3 and moves the
                // serving window to SERVING_STATION_POS. Old baked scenes lack counter_west_3, which leaves
                // the serving station where bots/nav can never reach it.
                bool layoutOutdated = true;
                foreach (var st in GetComponentsInChildren<StationBase>(true))
                {
                    if (st != null && st.stationId == "counter_west_3") { layoutOutdated = false; break; }
                }

                if (restCorner == null || layoutOutdated)
                {
                    needsSpriteRebuild = true;
                }
                else
                {
                    foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        // Held-item and pass-slot sprites are empty on purpose when nothing sits there.
                        if (sr != null && IsRuntimeItemRenderer(sr.gameObject.name)) continue;
                        if (sr != null && sr.sprite == null)
                        {
                            needsSpriteRebuild = true;
                            break;
                        }
                    }
                }

                if (needsSpriteRebuild && autoBuildKitchenOnStart)
                {
                    // Preserve any station positions that the user dragged/moved in the current layout
                    var customPositions = new Dictionary<string, Vector3>();
                    // Skip restore when the layout is outdated: old positions would undo the new layout.
                    if (restCorner != null && !layoutOutdated)
                    {
                        foreach (var st in GetComponentsInChildren<StationBase>(true))
                        {
                            if (st != null && !string.IsNullOrEmpty(st.stationId))
                                customPositions[st.stationId] = st.transform.position;
                        }
                    }

                    // Remove serialized Edit-Mode preview children & preview chefs
                    for (int i = transform.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(transform.GetChild(i).gameObject);
                    }
                    foreach (var oldChef in FindObjectsOfType<Player.PlayerController>())
                    {
                        if (oldChef != null)
                            DestroyImmediate(oldChef.gameObject);
                    }
                    stations.Clear();
                    playersByPeer.Clear();

                    // Rebuild fresh runtime sprites, colliders, and players
                    BuildEnvironment();
                    BuildPerimeterWalls();
                    BuildStations();
                    SpawnPlayers();

                    // Restore any custom station positions set by user in Scene View
                    foreach (var kvp in customPositions)
                    {
                        if (stations.TryGetValue(kvp.Key, out var rebuiltStation) && rebuiltStation != null)
                        {
                            rebuiltStation.transform.position = kvp.Value;
                            rebuiltStation.SyncSpriteAndColliderPosition();
                        }
                    }
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

            // 2. Tick cooking stations (LAN client: host owns the timers; delta 0 only refreshes the bar)
            bool lanClient = PlayerController.IsLanClient();
            foreach (var st in stations.Values)
            {
                if (st is CookingStation cs)
                {
                    cs.TickCooking(lanClient ? 0f : dt);
                }
            }

            // 3. Update interaction prompt for local player
            UpdateLocalPrompt();

            // 4. Update Tutorial 3D Target Arrow
            UpdateTutorialArrow();

            // 5. Close the double door again after a serve
            TickDoor(dt);
        }

        private static Sprite GetTutorialArrowSprite()
        {
            if (_tutorialArrowSprite == null)
            {
                int w = 24, h = 24;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color clear = new Color(0, 0, 0, 0);
                Color gold = new Color(1.0f, 0.88f, 0.15f, 1.0f);
                Color outline = new Color(0.12f, 0.10f, 0.05f, 1.0f);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        tex.SetPixel(x, y, clear);
                        // Downward pointing arrow: shaft at top (y=10..21, x=8..15), triangle head at bottom (y=2..11)
                        bool inShaft = (y >= 11 && y <= 21 && x >= 9 && x <= 14);
                        int headHalfWidth = y - 2;
                        bool inHead = (y >= 2 && y <= 11 && Mathf.Abs(x - 11.5f) <= headHalfWidth);
                        if (inShaft || inHead)
                        {
                            bool isEdge = (y == 21 || y == 2 || (inShaft && (x == 9 || x == 14)) || (inHead && Mathf.Abs(Mathf.Abs(x - 11.5f) - headHalfWidth) < 1.2f));
                            tex.SetPixel(x, y, isEdge ? outline : gold);
                        }
                    }
                }
                tex.Apply();
                _tutorialArrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.0f), 24f);
            }
            return _tutorialArrowSprite;
        }

        private void UpdateTutorialArrow()
        {
            bool showArrow = ShiftManager.Instance != null && ShiftManager.Instance.shiftActive && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep < 10;
            if (!showArrow)
            {
                if (_tutorialArrowObj != null && _tutorialArrowObj.activeSelf)
                    _tutorialArrowObj.SetActive(false);
                return;
            }

            if (_tutorialArrowObj == null)
            {
                _tutorialArrowObj = new GameObject("TutorialTargetArrow");
                _tutorialArrowObj.transform.SetParent(transform);
                _tutorialArrowObj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                _tutorialArrowObj.transform.localScale = Vector3.one * 1.15f;
                var sr = _tutorialArrowObj.AddComponent<SpriteRenderer>();
                sr.sprite = GetTutorialArrowSprite();
                sr.sortingOrder = 1450;
            }

            if (!_tutorialArrowObj.activeSelf)
                _tutorialArrowObj.SetActive(true);

            Vector3 targetPos = new Vector3(-2.0f, 0.0f, 0.0f);
            int step = ShiftManager.Instance.tutorialStep;

            switch (step)
            {
                case 1:
                case 2:
                    if (stations.TryGetValue("fridge_west", out var fridge) && fridge != null)
                        targetPos = fridge.transform.position;
                    else if (playersByPeer.TryGetValue(1, out var linh) && linh != null)
                        targetPos = linh.transform.position + new Vector3(0f, 0.4f, 0f);
                    break;
                case 3:
                    if (stations.TryGetValue("shelf_west", out var shelf) && shelf != null)
                        targetPos = shelf.transform.position;
                    break;
                case 4:
                case 5:
                    if (stations.TryGetValue("noodle_blancher", out var stove) && stove != null)
                        targetPos = stove.transform.position;
                    else if (stations.TryGetValue("stockpot", out var pot) && pot != null)
                        targetPos = pot.transform.position;
                    break;
                default:
                    if (stations.TryGetValue("serving_station", out var srv) && srv != null)
                        targetPos = srv.transform.position;
                    break;
            }

            float bob = Mathf.Sin(Time.time * 6.5f) * 0.22f;
            _tutorialArrowObj.transform.position = targetPos + new Vector3(0.0f, 2.15f + bob, -0.35f);
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
                    prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("hud.prep_station") : "[E] Place/Pick/Assemble  •  [F] Chop";
                else if (nearest is CookingStation)
                    prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("hud.cooking_station") : "[E] Cook / Add Ingredient / Pick Up";
                else if (nearest is PassCounter)
                    prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("hud.pass_counter") : "[E] Pass / Take / Assemble (+15 Flow)";
                else if (nearest is CounterStation)
                    prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("hud.holding_table") : "[E] Place / Pick / Self-Assemble";
                else if (nearest is ServingStation)
                    prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("hud.prompt_serve") : "[E] Serve Dish";
                else if (nearest is CrateStation crate)
                    prompt = $"[E] {crate.displayName}";
                else if (nearest is TrashStation)
                    prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("hud.trash_station") : "[E] Trash";
                else
                    prompt = "[E] Use";
            }
            else
            {
                prompt = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("menu.controls_guide") : "[WASD] Move  •  [E] Interact / Assemble  •  [F] Chop  •  [R] Recipes";
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

        public void BuildEnvironment()
        {
            // 1. Camera (Expanded 2.5D Orthographic FOV shifted +2 tiles up to frame expanded kitchen & restaurant patio)
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("KitchenCamera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = CameraOrthoSize(Application.isMobilePlatform);
                camObj.transform.position = new Vector3(0.0f, 15.0f, -9.0f);
                camObj.transform.LookAt(new Vector3(0.0f, 0.0f, 2.8f), Vector3.up);
            }
            else
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = CameraOrthoSize(Application.isMobilePlatform);
                Camera.main.transform.position = new Vector3(0.0f, 15.0f, -9.0f);
                Camera.main.transform.LookAt(new Vector3(0.0f, 0.0f, 2.8f), Vector3.up);
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
            Sprite grass1Sprite = GetStationSprite("grass_tile_1");
            Sprite grass2Sprite = GetStationSprite("grass_tile_2");
            Sprite woodFloorSprite = GetStationSprite("wood_floor_tile");

            // All floor & exterior tiles share the exact same Z-axis plane (local Z = 0.0f)
            // Expanded grid [-26.5..26.5, -14.5..16.5] covers ultrawide mobile & tablet aspect ratios with zero void:
            // - Spacious Kitchen Interior: x in [-10.5..10.5] (22 tiles wide), y in [-5.5..4.5] (11 tiles deep)
            // - Lived-In Restaurant Dining Room outside North Double Doors: x in [-10.5..10.5], y in [5.5..16.5] (runs past the top of the view)
            // - Wide Lush Grass Surround on all outer sides
            for (float x = -26.5f; x <= 26.5f; x += 1.0f)
            {
                for (float y = -14.5f; y <= 16.5f; y += 1.0f)
                {
                    bool isInsideKitchen = (x >= -10.5f && x <= 10.5f && y >= -5.5f && y <= 4.5f);
                    bool isRestaurantDining = (!isInsideKitchen && y >= 5.5f && y <= 16.5f && Mathf.Abs(x) <= 10.5f);

                    GameObject tileObj = new GameObject($"Tile_{x:F1}_{y:F1}");
                    tileObj.transform.SetParent(floorRoot.transform, false);
                    tileObj.transform.localPosition = new Vector3(x, y, 0.0f);
                    tileObj.transform.localRotation = Quaternion.identity;
                    tileObj.transform.localScale = Vector3.one * 1.015f;

                    var sr = tileObj.AddComponent<SpriteRenderer>();

                    if (isInsideKitchen)
                    {
                        bool isBroken = rng.NextDouble() < 0.16;
                        sr.sprite = (isBroken && brokenSprite != null) ? brokenSprite : cleanSprite;
                        sr.sortingOrder = -50;

                        if (Mathf.Abs(x) <= 0.35f)
                        {
                            sr.color = new Color(0.92f, 0.82f, 0.45f);
                        }
                        else if (x < 0f)
                        {
                            sr.color = isBroken ? new Color(0.85f, 0.88f, 0.86f) : new Color(0.92f, 0.96f, 0.94f);
                        }
                        else
                        {
                            sr.color = isBroken ? new Color(0.90f, 0.84f, 0.80f) : new Color(0.96f, 0.92f, 0.88f);
                        }
                    }
                    else if (isRestaurantDining)
                    {
                        sr.sprite = woodFloorSprite != null ? woodFloorSprite : cleanSprite;
                        sr.sortingOrder = -52;
                        // Warm red/gold bistro runner carpet directly outside the double doors
                        if (Mathf.Abs(x) <= 1.5f && y <= 8.5f)
                        {
                            sr.color = new Color(0.88f, 0.55f, 0.48f);
                        }
                        else
                        {
                            sr.color = new Color(0.96f, 0.88f, 0.78f);
                        }
                    }
                    else
                    {
                        bool useFlowers = rng.NextDouble() < 0.25;
                        sr.sprite = (useFlowers && grass2Sprite != null) ? grass2Sprite : (grass1Sprite != null ? grass1Sprite : cleanSprite);
                        sr.sortingOrder = -55;
                        sr.color = (grass1Sprite != null) ? Color.white : new Color(0.38f, 0.64f, 0.32f);
                    }
                }
            }
        }

        public void BuildPerimeterWalls()
        {
            // 1. Physical Boundaries (Invisible BoxColliders matching expanded 22x11 kitchen)
            CreateStaticWall("Wall_North", new Vector3(0.0f, 1.5f, 6.8f), new Vector3(23.2f, 3.0f, 0.8f));
            CreateStaticWall("Wall_South", new Vector3(0.0f, 1.5f, -7.2f), new Vector3(23.2f, 3.0f, 0.8f));
            CreateStaticWall("Wall_West", new Vector3(-11.4f, 1.5f, 0.0f), new Vector3(0.8f, 3.0f, 14.5f));
            CreateStaticWall("Wall_East", new Vector3(11.4f, 1.5f, 0.0f), new Vector3(0.8f, 3.0f, 14.5f));

            // Central Divider Barriers (Chefs stay in their assigned half, only interacting via Pass Counter & Serving Counter!)
            // One invisible full-length wall at x = 0 (north wall to south wall). Collider only, no renderer.
            // Pass Counter / Serving window sit on the line and stay reachable from both sides (2.6 interact range).
            CreateStaticWall("CenterDivider_Full", new Vector3(0.0f, 1.5f, -0.2f), new Vector3(0.7f, 3.0f, 14.0f));

            // 2. Visual Room Walls & Labeled Room Border Tiles (Parented on FloorTileGrid plane for 100% seamless tile alignment!)
            Transform floorRoot = transform.Find("FloorTileGrid");
            GameObject wallRoot = new GameObject("VisualBackWall");
            if (floorRoot != null)
            {
                wallRoot.transform.SetParent(floorRoot, false);
                wallRoot.transform.localPosition = Vector3.zero;
                wallRoot.transform.localRotation = Quaternion.identity;
            }
            else
            {
                wallRoot.transform.SetParent(transform);
                wallRoot.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            }

            Sprite wallSprite = GetStationSprite("wall_south");
            if (wallSprite != null)
            {
                // Full North Tiled Wall including right behind the North double doors (no gap behind doors!)
                float[] localYLevels = new float[] { 5.45f, 6.35f };
                for (int i = 0; i < localYLevels.Length; i++)
                {
                    float ly = localYLevels[i];
                    int tierSortingOrder = -14 - i;

                    for (float x = -10.5f; x <= 10.5f; x += 1.0f)
                    {
                        GameObject wObj = new GameObject($"WallTile_{x:F1}_{ly:F1}");
                        wObj.transform.SetParent(wallRoot.transform, false);
                        wObj.transform.localPosition = new Vector3(x, ly, 0.0f);
                        wObj.transform.localRotation = Quaternion.identity;
                        wObj.transform.localScale = new Vector3(1.02f, 1.02f, 1.0f);

                        var sr = wObj.AddComponent<SpriteRenderer>();
                        sr.sprite = wallSprite;
                        sr.sortingOrder = tierSortingOrder;
                        sr.color = (x < 0f) ? new Color(0.88f, 0.92f, 0.90f) : new Color(0.92f, 0.88f, 0.85f);
                    }
                }
            }

            // 3. Enclosed Room Border Cap Frame from professional_kitchen_room_door_tiles.png
            Sprite ulCorner = GetStationSprite("upper left corner wall");
            Sprite urCorner = GetStationSprite("upper right corner wall");
            Sprite llCorner = GetStationSprite("lower left corner wall");
            Sprite lrCorner = GetStationSprite("lower right corner wall");
            Sprite hWall = GetStationSprite("horizontal wall");
            Sprite vWall = GetStationSprite("vertical wall");
            Sprite tNorth = GetStationSprite("wall_t_north");
            Sprite tSouth = GetStationSprite("wall_t_south");

            // Top-Left & Top-Right Corners
            CreateRoomBorderPiece(wallRoot.transform, "RoomCorner_UpperLeft", ulCorner, new Vector3(-10.1f, 6.85f, 0.0f), Vector3.one, 0.0f, -9);
            CreateRoomBorderPiece(wallRoot.transform, "RoomCorner_UpperRight", urCorner, new Vector3(10.1f, 6.85f, 0.0f), Vector3.one, 0.0f, -9);

            // Bottom-Left & Bottom-Right Corners
            CreateRoomBorderPiece(wallRoot.transform, "RoomCorner_LowerLeft", llCorner, new Vector3(-10.1f, -5.55f, 0.0f), Vector3.one, 0.0f, -9);
            CreateRoomBorderPiece(wallRoot.transform, "RoomCorner_LowerRight", lrCorner, new Vector3(10.1f, -5.55f, 0.0f), Vector3.one, 0.0f, -9);

            // Top & Bottom Horizontal Wall Borders: tiled edge-to-edge between the corner pieces
            // (corners are 2.0 units wide centered at x = +/-10.1, so their inner edges sit at +/-9.1).
            if (hWall != null)
            {
                TileBorderRun(wallRoot.transform, "RoomWall_Top", hWall, true, -9.1f, 9.1f, 7.3f, 0.0f, -9);
                TileBorderRun(wallRoot.transform, "RoomWall_Bottom", hWall, true, -9.1f, 9.1f, -6.0f, 0.0f, -9);
            }

            // West & East Vertical Side Wall Borders: run from the lower corner's top edge up into the upper corner (no gaps)
            Sprite sideWallSprite = vWall != null ? vWall : hWall;
            float sideWallRot = vWall != null ? 0.0f : 90.0f;
            if (sideWallSprite != null)
            {
                TileBorderRun(wallRoot.transform, "RoomWall_West", sideWallSprite, false, -5.05f, 6.4f, -11.0f, sideWallRot, -9);
                TileBorderRun(wallRoot.transform, "RoomWall_East", sideWallSprite, false, -5.05f, 6.4f, 11.0f, sideWallRot, -9);
            }

            // North & South T-Shaped Center Divider Wall Caps
            CreateRoomBorderPiece(wallRoot.transform, "RoomWall_T_North", tNorth, new Vector3(0.0f, 5.85f, 0.0f), Vector3.one, 0.0f, -10);
            CreateRoomBorderPiece(wallRoot.transform, "RoomWall_T_South", tSouth, new Vector3(0.0f, -5.48f, 0.0f), Vector3.one, 0.0f, -8);

            // 4. Restaurant Dining Room Walls (wood floor y in [5.5..16.5], x in [-10.5..10.5], same width as the kitchen).
            // The camera's top edge sits near local y ~13.4, so the far (south-facing) back wall at y ~16.6+ stays off-screen;
            // the player only sees the dining room continuing past the top of the view.
            if (wallSprite != null)
            {
                float[] restWallY = new float[] { 16.65f, 17.45f };
                for (int i = 0; i < restWallY.Length; i++)
                {
                    float ry = restWallY[i];
                    int rSort = -48 - i;
                    for (float x = -10.5f; x <= 10.5f; x += 1.0f)
                    {
                        GameObject rwObj = new GameObject($"RestaurantWallTile_{x:F1}_{ry:F1}");
                        rwObj.transform.SetParent(wallRoot.transform, false);
                        rwObj.transform.localPosition = new Vector3(x, ry, 0.0f);
                        rwObj.transform.localRotation = Quaternion.identity;
                        rwObj.transform.localScale = new Vector3(1.02f, 1.02f, 1.0f);
                        var rSr = rwObj.AddComponent<SpriteRenderer>();
                        rSr.sprite = wallSprite;
                        rSr.sortingOrder = rSort;
                        rSr.color = new Color(0.92f, 0.85f, 0.78f);
                    }
                }
            }

            // Restaurant corners/top run share the kitchen's x = +/-10.1 corner columns so every run lines up.
            // NOTE: "RestCorner_UpperLeft_v5" is also the stale-scene rebuild marker checked in Start().
            CreateRoomBorderPiece(wallRoot.transform, "RestCorner_UpperLeft_v5", ulCorner, new Vector3(-10.1f, 17.85f, 0.0f), Vector3.one, 0.0f, -45);
            CreateRoomBorderPiece(wallRoot.transform, "RestCorner_UpperRight_v4", urCorner, new Vector3(10.1f, 17.85f, 0.0f), Vector3.one, 0.0f, -45);
            if (hWall != null)
            {
                TileBorderRun(wallRoot.transform, "RestWall_Top", hWall, true, -9.1f, 9.1f, 18.3f, 0.0f, -45);
            }
            if (sideWallSprite != null)
            {
                // Restaurant side walls continue straight up from the kitchen's side walls (same x = +/-11.0 line, no gaps)
                TileBorderRun(wallRoot.transform, "RestWall_West", sideWallSprite, false, 6.4f, 17.4f, -11.0f, sideWallRot, -45);
                TileBorderRun(wallRoot.transform, "RestWall_East", sideWallSprite, false, 6.4f, 17.4f, 11.0f, sideWallRot, -45);
            }
        }

        /// <summary>
        /// Lays border sprites edge-to-edge from 'start' to 'end' along one axis so the run has no gaps.
        /// The piece count is ceil(span / spriteLength); pieces are spaced evenly (slight overlap, never a gap).
        /// </summary>
        private void TileBorderRun(Transform parent, string prefix, Sprite sp, bool horizontal, float start, float end, float fixedCoord, float rotZ, int sortOrder)
        {
            if (sp == null) return;
            bool rotated = Mathf.Abs(Mathf.DeltaAngle(rotZ, 90.0f)) < 1.0f;
            Vector2 size = sp.bounds.size;
            float len = horizontal ? (rotated ? size.y : size.x) : (rotated ? size.x : size.y);
            if (len <= 0.01f) len = 1.0f;
            float span = end - start;
            int count = Mathf.Max(1, Mathf.CeilToInt(span / len - 0.001f));
            float step = span / count;
            for (int i = 0; i < count; i++)
            {
                float c = start + step * (i + 0.5f);
                Vector3 pos = horizontal ? new Vector3(c, fixedCoord, 0.0f) : new Vector3(fixedCoord, c, 0.0f);
                // Stretch each piece just enough to cover its slot.
                float stretch = step / len + 0.02f;
                Vector3 scale = (horizontal != rotated) ? new Vector3(stretch, 1.0f, 1.0f) : new Vector3(1.0f, stretch, 1.0f);
                CreateRoomBorderPiece(parent, $"{prefix}_{c:F1}", sp, pos, scale, rotZ, sortOrder);
            }
        }

        private void CreateRoomBorderPiece(Transform parent, string name, Sprite sp, Vector3 localPos, Vector3 scale, float rotZ, int sortOrder)
        {
            if (sp == null) return;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotZ);
            obj.transform.localScale = scale;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortOrder;
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

            // --- CENTER: SHARED PASS COUNTER & TRASH CAN ---
            var pass = CreateStation<PassCounter>(
                "pass_counter", "PASS COUNTER", "shared",
                new Vector3(0.0f, 0.0f, 0.5f), new Vector3(1.6f, 0.9f, 3.2f),
                new Color(0.78f, 0.80f, 0.84f), "table_double_vertical", null
            );

            var trash = CreateStation<TrashStation>(
                "trash_bin", "TRASH CAN", "shared",
                new Vector3(0.0f, 0.0f, -6.0f), new Vector3(1.1f, 0.8f, 1.1f),
                new Color(0.35f, 0.35f, 0.38f), "trash_can", null
            );

            // --- WEST ZONE (LINH'S SIDE): Spaced out inner counter column & West wall appliance column ---
            var prepWest = CreateStation<PrepStation>(
                "prep_west", "WET PREP TABLE (Cut Radish / Veg)", "linh_west",
                new Vector3(-4.4f, 0.0f, 3.6f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.72f, 0.75f, 0.78f), "prep_table_pot", null
            );

            var counterWest = CreateStation<CounterStation>(
                "counter_west", "SELF-ASSEMBLY / HOLDING TABLE (West 1)", "linh_west",
                new Vector3(-4.4f, 0.0f, 0.8f), new Vector3(2.0f, 0.9f, 1.1f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", null
            );

            // Ditched dedicated Assembly Table -> Self-Assembly Holding Counter [E]
            var counterWest2 = CreateStation<CounterStation>(
                "counter_west_2", "SELF-ASSEMBLY / HOLDING TABLE (West 2)", "linh_west",
                new Vector3(-4.4f, 0.0f, -2.0f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", null
            );
            stations["assembly_west"] = counterWest2;

            var counterWest3 = CreateStation<CounterStation>(
                "counter_west_3", "SELF-ASSEMBLY / HOLDING TABLE (West 3)", "linh_west",
                new Vector3(-4.4f, 0.0f, -5.0f), new Vector3(2.8f, 0.9f, 1.2f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", null
            );
            stations["counter_west_3"] = counterWest3;

            // Send-Out Table next to the North Door for BOTH Linh and Jiwon to submit food
            var servingStation = CreateStation<ServingStation>(
                "serving_station", "SERVING COUNTER (Send-Out Window)", "shared",
                SERVING_STATION_POS, new Vector3(1.8f, 0.9f, 2.2f),
                new Color(0.75f, 0.78f, 0.82f), "table_double_vertical", null
            );

            // West Wall Column (X = -10.35f): Flush against West Wall!
            var stockpot = CreateStation<CookingStation>(
                "stockpot", "West Stove 1 (Cook / Simmer)", "linh_west",
                new Vector3(-10.35f, 0.0f, 3.4f), new Vector3(1.3f, 0.9f, 1.5f),
                new Color(0.30f, 0.30f, 0.33f), "stove_side_pot", null
            );
            stockpot.applianceId = "stockpot";
            if (GameData.Instance != null && GameData.Instance.appliances.TryGetValue("stockpot", out var sSpec))
                stockpot.applianceSpec = sSpec;

            var blancher = CreateStation<CookingStation>(
                "noodle_blancher", "West Stove 2 (Cook Flour / Rice / Toboki)", "linh_west",
                new Vector3(-10.35f, 0.0f, 0.2f), new Vector3(1.3f, 0.9f, 1.5f),
                new Color(0.30f, 0.30f, 0.33f), "stove_side_on", null
            );
            blancher.applianceId = "noodle_blancher";
            if (GameData.Instance != null && GameData.Instance.appliances.TryGetValue("noodle_blancher", out var bSpec))
                blancher.applianceSpec = bSpec;

            // West Storage: Fridge flush on North wall baseboard (-6.2, 5.5) & Vertical Shelf flush on West wall (-10.35, -3.3)
            var fridgeWest = CreateCrateStation("fridge_west", "FRIDGE (Toboki / Egg)", "tteok_portioned", new Vector3(-6.2f, 0.0f, 5.5f), new Color(0.85f, 0.88f, 0.92f), "fridge_south", null);
            fridgeWest.isFridge = true;
            var shelfWest = CreateCrateStation("shelf_west", "SHELF (Gochujang / Flour)", "gochujang", new Vector3(-10.35f, 0.0f, -3.3f), new Color(0.70f, 0.65f, 0.58f), "shelf_vertical", null);
            stations["crate_pork"] = fridgeWest;
            stations["crate_beef"] = fridgeWest;
            stations["crate_bulgogi"] = fridgeWest;
            stations["crate_baguette"] = shelfWest;
            stations["crate_noodles"] = shelfWest;
            stations["crate_flour"] = shelfWest;

            // --- EAST ZONE (JIWON'S SIDE): Spaced out inner counter column & East wall appliance column ---
            var counterEast = CreateStation<CounterStation>(
                "counter_east", "SELF-ASSEMBLY / HOLDING TABLE (East 1)", "jiwon_east",
                new Vector3(4.4f, 0.0f, 1.8f), new Vector3(2.0f, 0.9f, 1.1f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", null
            );

            // East Prep Table on Jiwon's side for cutting Raw Meat once (Korean BBQ Meat) or twice (Bulgogi Portion)!
            var prepEast = CreateStation<PrepStation>(
                "prep_east", "EAST PREP TABLE (Cut Meat 1x / 2x)", "jiwon_east",
                new Vector3(4.4f, 0.0f, -1.6f), new Vector3(2.2f, 0.9f, 1.3f),
                new Color(0.72f, 0.75f, 0.78f), "prep_table_dry", null
            );
            stations["assembly_east"] = prepEast;

            // Ditched Dish Washing Sink -> Self-Assembly Holding Counter [E]
            var counterEast2 = CreateStation<CounterStation>(
                "counter_east_2", "SELF-ASSEMBLY / HOLDING TABLE (East 2)", "jiwon_east",
                new Vector3(4.4f, 0.0f, -5.0f), new Vector3(2.8f, 0.9f, 1.2f),
                new Color(0.72f, 0.75f, 0.78f), "table_double_horizontal", null
            );

            // East Wall Column (X = +10.35f): Flush against East Wall!
            var grill = CreateStation<CookingStation>(
                "grill_pan", "East Grill (Cook BBQ Beef / Bulgogi)", "jiwon_east",
                new Vector3(10.35f, 0.0f, 3.4f), new Vector3(1.3f, 0.9f, 1.6f),
                new Color(0.30f, 0.30f, 0.33f), "grill_side", null
            );
            grill.applianceId = "grill_pan";
            if (GameData.Instance != null && GameData.Instance.appliances.TryGetValue("grill_pan", out var gSpec))
                grill.applianceSpec = gSpec;

            var wok = CreateStation<CookingStation>(
                "simmer_wok", "East Wok (Cook Tteokbokki / Rice / Egg)", "jiwon_east",
                new Vector3(10.35f, 0.0f, 0.2f), new Vector3(1.3f, 0.9f, 1.5f),
                new Color(0.30f, 0.30f, 0.33f), "stove_side_on", null
            );
            wok.applianceId = "simmer_wok";
            if (GameData.Instance != null && GameData.Instance.appliances.TryGetValue("simmer_wok", out var wSpec))
                wok.applianceSpec = wSpec;

            // East Storage: Fridge flush on North wall baseboard (6.2, 5.5) & Vertical Shelf flush on East wall (10.35, -3.3)
            var fridgeEast = CreateCrateStation("fridge_east", "FRIDGE (Raw Meat / Radish)", "unprocessed_raw_meat", new Vector3(6.2f, 0.0f, 5.5f), new Color(0.85f, 0.88f, 0.92f), "fridge_south", null);
            fridgeEast.isFridge = true;
            var shelfEast = CreateCrateStation("shelf_east", "SHELF (Rice / Baguette)", "rice_portioned", new Vector3(10.35f, 0.0f, -3.3f), new Color(0.70f, 0.65f, 0.58f), "shelf_vertical", null);
            stations["crate_kimchi"] = fridgeEast;
            stations["crate_daikon"] = fridgeEast;
            stations["crate_tteok"] = shelfEast;
            stations["crate_eomuk"] = shelfEast;
            stations["crate_gochujang"] = shelfEast;

            // Build Authentic Decorative Kitchen & Lived-In Restaurant Props
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

        public void AddDecorSprite(string spriteName, Vector3 pos, Vector3 scale, int sortingOrderOffset = 0, int? exactSortingOrder = null)
        {
            GameObject obj = new GameObject($"Decor_{spriteName}");
            obj.transform.SetParent(transform);
            obj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = GetStationSprite(spriteName);
            int baseOrder = 500 - (int)(pos.z * 50);
            sr.sortingOrder = exactSortingOrder.HasValue ? exactSortingOrder.Value : (baseOrder + sortingOrderOffset);
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
            // 1. Double entrance doors moved down flush onto the North Wall baseboard line (sortingOrder = -6)
            AddDecorSprite("door_double_closed", new Vector3(0.0f, 0.0f, 5.55f), Vector3.one * 1.45f, 0, -6);

            // 2. Gap-Free Center Divider Counters along X=0 (Continuous stainless-steel counter column splitting West & East, stopping above Trash Can at -6.0f)
            float[] dividerZ = DIVIDER_DECOR_Z; // tables in front of the double door removed
            foreach (float dz in dividerZ)
            {
                AddDecorSprite("table_single_vertical", new Vector3(0.0f, 0.0f, dz), Vector3.one * 1.35f, 0);
            }

            // 3. Wall Hung Utensils & Chalkboard Menu Boards on North Tiled Wall
            AddDecorSprite("utensils_wall_1", new Vector3(-3.2f, 2.9f, 6.1f), Vector3.one * 1.3f, 0, -5);
            AddDecorSprite("utensils_wall_2", new Vector3(3.4f, 2.9f, 6.1f), Vector3.one * 1.3f, 0, -5);
            AddDecorSprite("wall_menu_board", new Vector3(-8.8f, 3.0f, 6.1f), Vector3.one * 1.25f, 0, -5);
            AddDecorSprite("wall_menu_board", new Vector3(8.8f, 3.0f, 6.1f), Vector3.one * 1.25f, 0, -5);

            // 4. Lived-In Restaurant Dining Room Outside the North Double Doors
            // Z-Hierarchy: Wood Floor (-52) < Restaurant Walls (-48..-45) < Potted Trees (-38) < Tables/Chairs (-34) < Customers (-26) < Kitchen North Wall (-15..-8)
            AddDecorSprite("bistro_table_set", new Vector3(-5.0f, 0.0f, 9.2f), Vector3.one * 1.45f, 0, -34);
            AddDecorSprite("bistro_table_set", new Vector3(5.0f, 0.0f, 9.2f), Vector3.one * 1.45f, 0, -34);
            AddDecorSprite("dining_table_1", new Vector3(-2.6f, 0.0f, 10.6f), Vector3.one * 1.35f, 0, -34);
            AddDecorSprite("dining_table_2", new Vector3(2.6f, 0.0f, 10.6f), Vector3.one * 1.35f, 0, -34);

            // Back row of the expanded dining room (fills the floor that used to sit behind the old restaurant wall)
            AddDecorSprite("dining_table_1", new Vector3(-7.4f, 0.0f, 13.4f), Vector3.one * 1.35f, 0, -36);
            AddDecorSprite("bistro_table_set", new Vector3(0.0f, 0.0f, 13.6f), Vector3.one * 1.45f, 0, -36);
            AddDecorSprite("dining_table_2", new Vector3(7.4f, 0.0f, 13.4f), Vector3.one * 1.35f, 0, -36);
            AddDecorSprite("bistro_plant", new Vector3(-10.0f, 0.0f, 11.6f), Vector3.one * 1.35f, 0, -38);
            AddDecorSprite("bistro_plant", new Vector3(10.0f, 0.0f, 11.6f), Vector3.one * 1.35f, 0, -38);

            // Potted Plants strictly behind the kitchen walls (-38 < -15) yet in front of the wooden floor (-38 > -52)!
            AddDecorSprite("bistro_plant", new Vector3(-2.1f, 0.0f, 8.2f), Vector3.one * 1.3f, 0, -38);
            AddDecorSprite("bistro_plant", new Vector3(2.1f, 0.0f, 8.2f), Vector3.one * 1.3f, 0, -38);
            AddDecorSprite("bistro_plant", new Vector3(-8.0f, 0.0f, 8.4f), Vector3.one * 1.35f, 0, -38);
            AddDecorSprite("bistro_plant", new Vector3(8.0f, 0.0f, 8.4f), Vector3.one * 1.35f, 0, -38);

            // Lively Restaurant Customers seated ON TOP of the chairs (sortingOrder = -26 > chairs -34)!
            // Horizontally flipped as requested
            SpawnRestaurantCustomer("Customer_HanoiDiner", "customer_1", new Vector3(-6.1f, 0.0f, 9.35f), true);
            SpawnRestaurantCustomer("Customer_SeoulFoodie", "customer_2", new Vector3(6.1f, 0.0f, 9.35f), false);
            SpawnRestaurantCustomer("Customer_RegularGuest", "customer_3", new Vector3(-3.6f, 0.0f, 10.7f), true);

            // 5. Yard trees, bushes, flowers and rocks so the grass is not bland (above grass -55, below building walls)
            foreach (var d in YARD_DECOR)
            {
                AddDecorSprite(d.sprite, d.pos, Vector3.one * 1.35f, 0, -40);
            }
        }

        private void SpawnRestaurantCustomer(string objName, string charSpriteName, Vector3 pos, bool flipX)
        {
            GameObject custObj = new GameObject(objName);
            custObj.transform.SetParent(transform);
            custObj.transform.position = pos;
            custObj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);

            var sr = custObj.AddComponent<SpriteRenderer>();
            Sprite sp = Resources.Load<Sprite>($"Sprites/Characters/{charSpriteName}");
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>($"Sprites/Characters/{charSpriteName}");
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.0f), 32f);
                }
            }
            sr.sprite = sp;
            sr.flipX = flipX;
            // Render strictly in front of / on top of the restaurant chairs (-34), while staying behind the kitchen North wall (-15)
            sr.sortingOrder = -26;

            if (sp != null && sp.bounds.size.y > 0.01f)
            {
                float scale = 1.85f / sp.bounds.size.y;
                custObj.transform.localScale = Vector3.one * scale;
            }
            else
            {
                custObj.transform.localScale = Vector3.one * 0.1f;
            }
        }

        public void SpawnPlayers()
        {
            // Clear previous player tracking if rebuild
            playersByPeer.Clear();

            int lTier = (ProgressionManager.Instance != null) ? ProgressionManager.Instance.linhTier : 1;
            int jTier = (ProgressionManager.Instance != null) ? ProgressionManager.Instance.jiwonTier : 1;

            var nm = NetworkManager.Instance;
            if (nm != null && nm.isLanActive && nm.players.Count >= 2)
            {
                // LAN: honour the lobby pick (host may play Jiwon, guest Linh). Linh always spawns West.
                string c1 = NetworkManager.ResolveChefCharacter(1, nm.players);
                string c2 = c1 == "linh" ? "jiwon" : "linh";
                foreach (var (peer, ch) in new[] { (1, c1), (2, c2) })
                {
                    nm.players.TryGetValue(peer, out var info);
                    SpawnChef(peer, ch, NetworkManager.ResolveChefSpec(ch, info?.specialization),
                        ch == "linh" ? lTier : jTier, new Vector3(ch == "linh" ? -2.2f : 2.2f, 0.0f, 0.5f));
                }
            }
            else
            {
                // Spawn Linh (Player 1) at (-2.2, 0, 0.5) in spacious West walkway
                SpawnChef(1, "linh", "prep_master", lTier, new Vector3(-2.2f, 0.0f, 0.5f));

                // Spawn Jiwon (Player 2) at (2.2, 0, 0.5) in spacious East walkway
                SpawnChef(2, "jiwon", "flame_keeper", jTier, new Vector3(2.2f, 0.0f, 0.5f));
            }
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
            itemChild.transform.localPosition = new Vector3(0.0f, PlayerController.CARRIED_ITEM_HEIGHT, 0.0f);
            itemChild.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            var itemSr = itemChild.AddComponent<SpriteRenderer>();
            itemChild.transform.localScale = Vector3.one * 0.55f;
            itemChild.SetActive(false);
            playerComp.carriedItemSpriteRenderer = itemSr;

            playerComp.ConfigureChef(peerId, charId, specId, tier);
            playersByPeer[peerId] = playerComp;
            return playerComp;
        }

        public void ConfigureTwoBotsQA()
        {
            foreach (var kvp in playersByPeer)
            {
                int peer = kvp.Key;
                var chef = kvp.Value;
                if (chef == null) continue;

                chef.isLocalPlayer = false;
                var existingBot = chef.GetComponent<AI.BotController>();
                if (existingBot == null)
                {
                    existingBot = chef.gameObject.AddComponent<AI.BotController>();
                }
                existingBot.botActive = true;
                existingBot.peerId = peer;
                existingBot.characterId = chef.characterId;
                existingBot.playerController = chef;
                existingBot.ConfigureDifficulty(BotDifficulty.Assistive);
            }
        }

        public void ConfigureSoloAndAIMode(bool isSolo, int humanPeer, BotDifficulty diff)
        {
            if (ShiftManager.Instance != null && ShiftManager.Instance.isAutoplayQAMode)
            {
                ConfigureTwoBotsQA();
                return;
            }

            // Solo with no partner in the room: an auto-hosted empty LAN room would pin
            // the local peer to 1 (Linh), leaving a human Jiwon frozen. Solo is offline.
            if (isSolo && NetworkManager.Instance != null && NetworkManager.Instance.isLanActive && NetworkManager.Instance.players.Count <= 1)
            {
                NetworkManager.Instance.CloseConnection();
            }

            foreach (var kvp in playersByPeer)
            {
                int peer = kvp.Key;
                var chef = kvp.Value;
                if (chef == null) continue;

                var existingBot = chef.GetComponent<AI.BotController>();
                if (isSolo && peer != humanPeer)
                {
                    chef.isLocalPlayer = false;
                    if (existingBot == null)
                    {
                        existingBot = chef.gameObject.AddComponent<AI.BotController>();
                    }
                    existingBot.botActive = true;
                    existingBot.peerId = peer;
                    existingBot.characterId = chef.characterId;
                    existingBot.playerController = chef;
                    existingBot.ConfigureDifficulty(diff);
                }
                else
                {
                    bool lan = NetworkManager.Instance != null && NetworkManager.Instance.isLanActive;
                    chef.isLocalPlayer = lan ? (peer == NetworkManager.Instance.localPeerId) : (peer == humanPeer || !isSolo);
                    if (existingBot != null)
                    {
                        existingBot.botActive = false;
                    }
                }
            }

            // Offline: the human-controlled chef must be the local peer, otherwise input,
            // prompts, fridge/shelf wheel and mobile buttons all stay bound to Linh.
            if (NetworkManager.Instance != null && !NetworkManager.Instance.isLanActive)
            {
                NetworkManager.Instance.localPeerId = isSolo ? humanPeer : 1;
                UpdateLocalPrompt();
                OnChefSwitched?.Invoke(NetworkManager.Instance.localPeerId);
            }
        }

        public bool IsStationOccupiedByWorker(StationBase st)
        {
            if (st == null) return false;
            foreach (var w in activeWorkers.Values)
            {
                if (w != null && w.targetStation == st && w.currentState != WorkerState.WALKING_OUT && w.currentState != WorkerState.EXPIRED)
                    return true;
            }
            return false;
        }

        public (bool ok, string reason, int hiredCount) RequestHireAvailableCrew(int peerId)
        {
            if (!playersByPeer.TryGetValue(peerId, out var p))
                return (false, "Unknown chef.", 0);

            if (p.characterId != "jiwon")
                return (false, "Only Jiwon has the delegation authority to hire part-time crew!", 0);

            // LAN client: host owns the crew; send the request.
            if (PlayerController.IsLanClient())
            {
                NetworkManager.Instance.SendClientHire();
                return (true, "", 0);
            }

            int tierSlots = 0;
            if (GameData.Instance != null && GameData.Instance.jiwonTiers.TryGetValue(p.characterTier, out var jTier))
            {
                tierSlots = jTier.partTimerSlots;
            }

            if (tierSlots <= 0)
                return (false, "Jiwon is Tier 1 (Burnt-out Executive). Complete Level 3 to unlock Part-Timer hiring!", 0);

            int freeSlots = tierSlots - activeWorkers.Count;
            if (freeSlots <= 0)
                return (false, $"Maximum part-timer capacity reached ({activeWorkers.Count}/{tierSlots} slots active)!", 0);

            if (workerCooldowns.TryGetValue("prep_assistant", out float cd) && cd > 0.0f)
                return (false, $"Part-timer agency is on cooldown ({cd:F0}s left)!", 0);

            int hired = 0;
            for (int i = 0; i < freeSlots; i++)
            {
                var hireRes = RequestHireWorker(peerId, "prep_assistant", false);
                if (hireRes.ok)
                {
                    hired++;
                }
                else
                {
                    if (hired == 0) return (false, hireRes.reason, 0);
                    break;
                }
            }

            if (hired > 0)
            {
                float cooldown = 45.0f;
                if (GameData.Instance != null && GameData.Instance.workerCatalog.TryGetValue("prep_assistant", out var spec))
                    cooldown = spec.cooldownDuration;
                workerCooldowns["prep_assistant"] = cooldown;
            }

            return (hired > 0, hired > 0 ? "" : "Failed to hire crew.", hired);
        }

        public (bool ok, string reason, string workerId) RequestHireWorker(int peerId, string roleId, bool applyCooldown = true)
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

            if (applyCooldown && workerCooldowns.TryGetValue(roleId, out float cd) && cd > 0.0f)
                return (false, $"{spec.displayName} contract agency is on cooldown ({cd:F0}s left)!", "");

            if (ShiftManager.Instance == null || ShiftManager.Instance.totalScore < spec.hireCost)
                return (false, $"Not enough score! Need {spec.hireCost} score to hire.", "");

            StationBase targetStation = null;
            if (roleId == "prep_assistant")
            {
                stations.TryGetValue("prep_east", out var east);
                stations.TryGetValue("prep_west", out var west);

                bool eastOccupied = IsStationOccupiedByWorker(east);
                bool westOccupied = IsStationOccupiedByWorker(west);

                if (east != null && !eastOccupied)
                {
                    targetStation = east;
                }
                else if (west != null && !westOccupied)
                {
                    targetStation = west;
                }
                else
                {
                    targetStation = east ?? west;
                }
            }
            else if (roleId == "dishwasher")
            {
                stations.TryGetValue("washing_station", out targetStation);
                if (targetStation == null)
                {
                    stations.TryGetValue("prep_west", out var west);
                    stations.TryGetValue("prep_east", out var east);
                    targetStation = (!IsStationOccupiedByWorker(west) ? west : east) ?? west ?? east;
                }
            }

            if (targetStation == null)
                return (false, $"No station available for {spec.displayName}.", "");

            // Deduct shift score (not coins) & apply cooldown
            ShiftManager.Instance.totalScore -= spec.hireCost;
            if (applyCooldown)
            {
                workerCooldowns[roleId] = spec.cooldownDuration;
            }

            workerUidCounter++;
            string wid = $"worker_{workerUidCounter}";

            SpawnWorkerVisual(wid, roleId, targetStation, workerUidCounter, p.characterTier);

            // LAN: tell the client which station this worker walks to, so both screens show it
            NetworkManager.Instance?.BroadcastWorker(wid, roleId, targetStation.stationId);

            OnStatusToast?.Invoke($"Hired {spec.displayName} for {spec.contractDuration:F0}s! (-{spec.hireCost} score)", new Color(0.35f, 1.0f, 0.6f));
            return (true, "", wid);
        }

        /// <summary>
        /// Creates the part-timer sprite + behaviour walking to the target station.
        /// Used by host hiring and by LAN clients mirroring the host's WORKER packet.
        /// </summary>
        public PartTimeWorker SpawnWorkerVisual(string wid, string roleId, StationBase targetStation, int uid, int tier)
        {
            GameObject workerObj = new GameObject(wid);
            workerObj.transform.SetParent(transform);

            // Cute mini part-timer chef: smaller than Jiwon (~1.35m height) with customer sprite styling
            workerObj.transform.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            var sr = workerObj.AddComponent<SpriteRenderer>();
            string sprName = (roleId == "dishwasher" || uid % 2 == 0) ? "customer_3" : "customer_2";
            Sprite sp = Resources.Load<Sprite>($"Sprites/Characters/{sprName}");
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>($"Sprites/Characters/{sprName}");
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.0f), 32f);
                }
            }
            if (sp == null) sp = Resources.Load<Sprite>("Sprites/Characters/jiwon");
            sr.sprite = sp;
            sr.sortingOrder = 15;

            if (sp != null && sp.bounds.size.y > 0.01f)
            {
                float scale = PART_TIMER_HEIGHT / sp.bounds.size.y;
                workerObj.transform.localScale = Vector3.one * scale;
            }
            else
            {
                workerObj.transform.localScale = Vector3.one * 0.58f;
            }

            var workerComp = workerObj.AddComponent<PartTimeWorker>();
            workerComp.InitializeWorker(wid, roleId, targetStation, tier);

            workerComp.OnWorkerExpired += (expId) =>
            {
                if (activeWorkers.ContainsKey(expId))
                    activeWorkers.Remove(expId);
            };

            activeWorkers[wid] = workerComp;
            return workerComp;
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
                    "Assets/Sprites/Kitchen/professional_kitchen_withshadows.png",
                    "Assets/Resources/Sprites/Kitchen/professional_kitchen_room_door_tiles.png",
                    "Assets/Sprites/Kitchen/professional_kitchen_room_door_tiles.png"
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

            // 3. Direct slice fallback for room walls, side grill, and bistro tables before fuzzy matching
            Sprite directSlice = TryCreateDirectSlice(name);
            if (directSlice != null)
            {
                _kitchenSpritesCache[name] = directSlice;
                return directSlice;
            }

            // 4. Separate single-tile files in Resources/Sprites/Kitchen/
            Sprite fallback = Resources.Load<Sprite>($"Sprites/Kitchen/{name}");
#if UNITY_EDITOR
            if (fallback == null)
            {
                fallback = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/Kitchen/{name}.png");
            }
#endif
            if (fallback == null)
            {
                Texture2D rawTex = Resources.Load<Texture2D>($"Sprites/Kitchen/{name}");
#if UNITY_EDITOR
                if (rawTex == null)
                {
                    rawTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Sprites/Kitchen/{name}.png");
                }
#endif
                if (rawTex != null)
                {
                    rawTex.filterMode = FilterMode.Point;
                    Vector2 piv = (name == "bistro_table_set" || name == "bistro_plant" || name == "trash_can") ? new Vector2(0.5f, 0.0f) : new Vector2(0.5f, 0.5f);
                    fallback = Sprite.Create(rawTex, new Rect(0, 0, rawTex.width, rawTex.height), piv, 32f);
                }
            }
            if (fallback != null)
            {
                _kitchenSpritesCache[name] = fallback;
                return fallback;
            }

            // 5. Keyword/substring match
            foreach (var kvp in _kitchenSpritesCache)
            {
                string sliceName = kvp.Key;
                if (sliceName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf(sliceName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private Sprite TryCreateDirectSlice(string name)
        {
            if (name == "dining_table_1" || name == "dining_table_2" || name == "grill_side")
            {
                Texture2D pkTex = Resources.Load<Texture2D>("Sprites/Kitchen/professional_kitchen");
                if (pkTex != null)
                {
                    pkTex.filterMode = FilterMode.Point;
                    if (name == "dining_table_1")
                        return Sprite.Create(pkTex, new Rect(0f, 0f, 32f, 42f), new Vector2(0.5f, 0.0f), 32f);
                    if (name == "dining_table_2")
                        return Sprite.Create(pkTex, new Rect(32f, 0f, 32f, 42f), new Vector2(0.5f, 0.0f), 32f);
                    if (name == "grill_side")
                        return Sprite.Create(pkTex, new Rect(422f, 64f, 25f, 61f), new Vector2(0.5f, 0.0f), 32f);
                }
            }

            if (name == "upper left corner wall" || name == "upper right corner wall" ||
                name == "lower left corner wall" || name == "lower right corner wall" ||
                name == "horizontal wall" || name == "vertical wall")
            {
                Texture2D roomTex = Resources.Load<Texture2D>("Sprites/Kitchen/professional_kitchen_room_door_tiles");
                if (roomTex != null)
                {
                    roomTex.filterMode = FilterMode.Point;
                    switch (name)
                    {
                        case "upper left corner wall":
                            return Sprite.Create(roomTex, new Rect(477f, 128f, 64f, 35f), new Vector2(0.5f, 0.5f), 32f);
                        case "upper right corner wall":
                            return Sprite.Create(roomTex, new Rect(483f, 128f, 64f, 35f), new Vector2(0.5f, 0.5f), 32f);
                        case "lower left corner wall":
                            return Sprite.Create(roomTex, new Rect(478f, 189f, 64f, 35f), new Vector2(0.5f, 0.5f), 32f);
                        case "lower right corner wall":
                            return Sprite.Create(roomTex, new Rect(484f, 189f, 64f, 35f), new Vector2(0.5f, 0.5f), 32f);
                        case "horizontal wall":
                            return Sprite.Create(roomTex, new Rect(484f, 189f, 58f, 6f), new Vector2(0.5f, 0.5f), 32f);
                        case "vertical wall":
                            return Sprite.Create(roomTex, new Rect(160f, 288f, 6f, 32f), new Vector2(0.5f, 0.5f), 32f);
                    }
                }
            }
            return null;
        }

        private string GetStationSpriteAlias(string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "stockpot": return "stove_side_pot";
                case "noodle_blancher": return "stove_side_on";
                case "grill_pan": return "grill_side";
                case "simmer_wok": return "stove_side_on";
                case "prep_west": return "prep_table_pot";
                case "prep_east":
                case "assembly_west":
                case "assembly_east": return "prep_table_dry";
                case "washing_station":
                case "washing_sink": return "sink_double_south";
                case "pass_counter": return "table_double_vertical";
                case "serving_station": return "table_double_horizontal";
                case "counter_west":
                case "counter_east":
                case "counter_stainless": return "table_double_horizontal";
                case "trash_bin": return "trash_can";
                case "crate_cooler": return "fridge_south";
                default: return null;
            }
        }

        /// <summary>
        /// Clears all leftover food, items, and ingredients across all stations, counters,
        /// appliances, prep boards, pass counters, and chef hands daily for a spotless new day.
        /// </summary>
        public void ClearKitchenDaily()
        {
            // 1. Clear all Stations
            if (stations != null)
            {
                foreach (var st in stations.Values)
                {
                    if (st == null) continue;

                    if (st is CounterStation counter)
                    {
                        counter.heldItem = null;
                        counter.RefreshStationVisuals();
                    }
                    else if (st is CookingStation cooking)
                    {
                        cooking.heldItem = null;
                        cooking.cookingElapsedSeconds = 0.0f;
                        cooking.currentState = CookingState.RAW;
                        cooking.RefreshStationVisuals();
                    }
                    else if (st is PrepStation prep)
                    {
                        prep.heldItem = null;
                        prep.strikesCompleted = 0;
                        prep.UpdateVisuals();
                    }
                    else if (st is PassCounter pass)
                    {
                        pass.InitSlots();
                        for (int s = 0; s < pass.slots.Length; s++)
                        {
                            if (pass.slots[s] != null)
                            {
                                pass.slots[s].item = null;
                                pass.slots[s].placedByPeer = 0;
                                pass.slots[s].placedAtTime = 0.0f;
                            }
                        }
                        pass.RefreshStationVisuals();
                    }
                }
            }

            // 1b. Dismiss part-timers so they don't keep working into the results screen / next day
            if (activeWorkers != null) { foreach (var w in new List<PartTimeWorker>(activeWorkers.Values)) if (w != null) Destroy(w.gameObject); activeWorkers.Clear(); }
            if (workerCooldowns != null) workerCooldowns.Clear();

            // 2. Clear Chef Hands and Reset Spawn Positions
            if (playersByPeer != null)
            {
                foreach (var kvp in playersByPeer)
                {
                    var chef = kvp.Value;
                    if (chef == null) continue;

                    chef.carriedItem = null;
                    chef.RefreshVisuals();

                    // Halt any active bot controller
                    var bot = chef.GetComponent<BotController>();
                    if (bot != null)
                    {
                        bot.HaltBot();
                    }

                    // Reset position
                    Vector3 homePos = (chef.characterId == "linh" || kvp.Key == 1) 
                        ? new Vector3(-4.5f, 0.0f, 0.0f) 
                        : new Vector3(4.5f, 0.0f, 0.0f);
                    
                    if (chef.characterController != null && chef.characterController.enabled)
                    {
                        chef.characterController.enabled = false;
                        chef.transform.position = homePos;
                        chef.characterController.enabled = true;
                    }
                    else
                    {
                        chef.transform.position = homePos;
                    }
                }
            }

            // 3. Reset Communication Hub
            BotCommunicationHub.Reset();
        }
    }
}
