using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Kitchen;
using TwoLadles.Networking;

namespace TwoLadles.Player
{
    public class PlayerController : MonoBehaviour
    {
        public const float BASE_MOVE_SPEED = 6.2f;
        public const float KITCHEN_BOUNDS_X = 10.4f;
        public const float KITCHEN_BOUNDS_Z = 6.6f;

        [Header("Identity")]
        public int peerId = 1;
        public string characterId = "linh";
        public string specializationId = "prep_master";
        public int characterTier = 1;
        public bool isLocalPlayer = true;
        /// <summary>Partner-sent upgrade values (LAN). Null = use this device's global upgrades.</summary>
        public System.Collections.Generic.Dictionary<string, float> upgradeOverrides = null;

        [Header("Carried Item")]
        public KitchenItem carriedItem = null;

        [Header("Visual Components")]
        public GameObject thirdArmVisual;
        public GameObject thirdArmGlowVisual;
        public SpriteRenderer characterSpriteRenderer;
        public SpriteRenderer carriedItemSpriteRenderer;
        public float walkAnimTime = 0.0f;

        [Header("Physics & Collision")]
        public CharacterController characterController;

        [Header("Carried Item Hover & Tilt Animation")]
        /// <summary>Height of the carried item above the chef's feet (raised so the face stays visible).</summary>
        public const float CARRIED_ITEM_HEIGHT = 2.3f;
        public Vector3 carriedItemBaseOffset = new Vector3(0.0f, CARRIED_ITEM_HEIGHT, 0.0f);
        public float carriedHoverBobSpeed = 4.5f;
        public float carriedHoverBobHeight = 0.08f;
        public float carriedHoverTiltSpeed = 3.5f;
        public float carriedHoverTiltAngle = 10.0f;
        private float carriedAnimTime = 0.0f;

        [Header("Controls & Input")]
        public Vector2 virtualJoystickInput = Vector2.zero;
        public Vector2 facingDirection = Vector2.right;

        private void Start()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (NetworkManager.Instance != null)
            {
                isLocalPlayer = (peerId == NetworkManager.Instance.localPeerId);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (isLocalPlayer)
            {
                HandlePlayerInput(dt);
            }
            else if (NetworkManager.Instance != null && NetworkManager.Instance.isLanActive)
            {
                // LAN partner: animate flip + wobble from received positions
                UpdateRemoteWalkVisuals(dt);
            }

            UpdateCarriedItemHover(dt);

            // Dynamic 2.5D Depth Sorting: Automatically renders chefs behind north counters and in front of south counters
            if (characterSpriteRenderer != null)
            {
                int order = 500 - (int)(transform.position.z * 50);
                characterSpriteRenderer.sortingOrder = order;
                if (carriedItemSpriteRenderer != null)
                {
                    carriedItemSpriteRenderer.sortingOrder = 1300;
                }
            }
        }

        private void HandlePlayerInput(float delta)
        {
            // 1. Movement Input (Keyboard WASD / Arrows + Mobile Joystick)
            Vector2 inputDir = virtualJoystickInput;
            if (inputDir.sqrMagnitude < 0.01f)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                inputDir = new Vector2(h, v);
            }

            if (inputDir.sqrMagnitude > 0.01f)
            {
                facingDirection = inputDir.normalized;
            }

            ApplyMovement(inputDir, delta);

            // 2. Action Keys
            // Primary Interact: E or Space key
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                TryInteractPrimary();
            }

            // Secondary Action (Strike cadence / Scrub dish / Bowl vessel): F key
            if (Input.GetKeyDown(KeyCode.F))
            {
                TryInteractSecondary();
            }

            // Throw ingredient/dish: Q key or Right Mouse Button
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
            {
                ThrowItem(facingDirection);
            }

            // Jiwon Part-Timer Crew Skill: H key
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (characterId == "jiwon" && KitchenController.Instance != null)
                {
                    KitchenController.Instance.RequestHireAvailableCrew(peerId);
                }
            }
        }

        public (bool ok, string reason) TryInteractPrimary(StationBase specificStation = null)
        {
            if (KitchenController.Instance == null) return (false, "Kitchen unavailable.");

            StationBase target = specificStation;
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist > StationBase.BOT_REACH_DISTANCE)
                {
                    return (false, "Target station out of reach.");
                }
            }
            else
            {
                target = KitchenController.Instance.FindNearestStation(transform.position);
            }

            if (target == null) return (false, "No station within reach.");

            // LAN client: never change station state locally. Send the intent; host decides and
            // the result comes back in the next STATIONS snapshot (~0.1s).
            if (IsLanClient())
            {
                if (target is CrateStation && carriedItem == null && TwoLadles.UI.QuickSelectWheelUI.Instance != null)
                {
                    // Guest picks on its own wheel; the pick goes to the host as DISPENSE.
                    TwoLadles.UI.QuickSelectWheelUI.Instance.OpenWheelForStation(target.stationId, target.transform);
                    return (true, "");
                }
                NetworkManager.Instance.SendClientInteract(false, target.stationId);
                return (true, "");
            }

            var res = target.InteractPrimary(ref carriedItem, peerId, characterId);
            RefreshVisuals();

            if (!string.IsNullOrEmpty(res.reason))
            {
                Color toastCol = res.ok ? new Color(0.35f, 1.0f, 0.6f) : new Color(1.0f, 0.45f, 0.45f);
                KitchenController.Instance.TriggerStatusToast(res.reason, toastCol);
            }

            return res;
        }

        public (bool ok, string reason) TryInteractSecondary(StationBase specificStation = null)
        {
            if (KitchenController.Instance == null) return (false, "Kitchen unavailable.");

            StationBase target = specificStation;
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist > StationBase.BOT_REACH_DISTANCE)
                {
                    return (false, "Target station out of reach.");
                }
            }
            else
            {
                target = KitchenController.Instance.FindNearestStation(transform.position);
            }

            if (target == null) return (false, "No station within reach.");

            if (IsLanClient())
            {
                if (target is CrateStation && carriedItem == null && TwoLadles.UI.QuickSelectWheelUI.Instance != null)
                {
                    // Guest picks on its own wheel; the pick goes to the host as DISPENSE.
                    TwoLadles.UI.QuickSelectWheelUI.Instance.OpenWheelForStation(target.stationId, target.transform);
                    return (true, "");
                }
                NetworkManager.Instance.SendClientInteract(true, target.stationId, target is PrepStation ? Time.time : -1f);
                return (true, "");
            }

            var res = target.InteractSecondary(ref carriedItem, peerId, characterId);
            RefreshVisuals();

            if (!string.IsNullOrEmpty(res.reason))
            {
                Color toastCol = res.ok ? new Color(0.35f, 1.0f, 0.6f) : new Color(1.0f, 0.45f, 0.45f);
                KitchenController.Instance.TriggerStatusToast(res.reason, toastCol);
            }

            return res;
        }

        /// <summary>
        /// Animates the carried item hovering above chef's head with subtle side-to-side tilt
        /// and gentle floating vertical bobbing.
        /// </summary>
        public void UpdateCarriedItemHover(float delta)
        {
            if (carriedItem == null || carriedItemSpriteRenderer == null)
                return;

            carriedAnimTime += delta;

            // Subtle vertical floating bob (sin wave)
            float bobOffset = Mathf.Sin(carriedAnimTime * carriedHoverBobSpeed) * carriedHoverBobHeight;

            // Subtle side-to-side tilt variation
            float tiltZ = Mathf.Sin(carriedAnimTime * carriedHoverTiltSpeed) * carriedHoverTiltAngle;

            // If chef is walking, synchronize dynamic tilt with movement stride
            if (walkAnimTime > 0.0f)
            {
                bobOffset += Mathf.Sin(walkAnimTime * 1.5f) * 0.04f;
                tiltZ += Mathf.Sin(walkAnimTime) * 6.0f;
            }

            carriedItemSpriteRenderer.transform.localPosition = carriedItemBaseOffset + new Vector3(0.0f, bobOffset, 0.0f);
            carriedItemSpriteRenderer.transform.localRotation = Quaternion.Euler(51.0f, 0.0f, tiltZ);
        }

        public void ConfigureChef(int pPeerId, string pCharId, string pSpecId, int pTier = 1)
        {
            peerId = pPeerId;
            characterId = pCharId;
            specializationId = pSpecId;
            characterTier = pTier;
            if (NetworkManager.Instance != null)
            {
                isLocalPlayer = (peerId == NetworkManager.Instance.localPeerId);
            }
            RefreshVisuals();
        }

        public float GetTaskSpeedMultiplier()
        {
            if (characterId == "linh" && GameData.Instance != null && GameData.Instance.linhTiers.TryGetValue(characterTier, out var spec))
            {
                return spec.taskSpeedMultiplier;
            }
            return 1.0f;
        }

        public float GetEffectiveMoveSpeed()
        {
            float mult = 1.0f;
            if (specializationId == "market_runner" && GameData.Instance != null && GameData.Instance.specializations.TryGetValue("market_runner", out var spec))
            {
                mult = spec.moveSpeedMultiplier;
            }
            mult *= GetChefUpgradeValue("move_speed", 1.0f);
            return BASE_MOVE_SPEED * mult;
        }

        /// <summary>
        /// Per-chef upgrade lookup. In LAN games the partner's own upgrades (sent at shift start)
        /// replace this device's global upgrade values for that chef.
        /// </summary>
        public float GetChefUpgradeValue(string effect, float defaultValue)
        {
            if (upgradeOverrides != null && upgradeOverrides.TryGetValue(effect, out float v))
            {
                return v;
            }
            if (ProgressionManager.Instance != null)
            {
                return ProgressionManager.Instance.GetUpgradeValue(effect, defaultValue);
            }
            return defaultValue;
        }

        public void ApplyMovement(Vector2 inputDir, float delta)
        {
            Vector2 clamped = inputDir;
            if (clamped.magnitude > 1.0f)
                clamped.Normalize();

            float speed = GetEffectiveMoveSpeed();
            // Balanced 2.5D Movement: 1.25x vertical scaling compensates for 51° camera foreshortening
            Vector3 move = new Vector3(clamped.x * speed, 0.0f, clamped.y * speed * 1.25f) * delta;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(move);
                Vector3 clampedPos = transform.position;
                clampedPos.x = Mathf.Clamp(clampedPos.x, -KITCHEN_BOUNDS_X, KITCHEN_BOUNDS_X);
                clampedPos.z = Mathf.Clamp(clampedPos.z, -KITCHEN_BOUNDS_Z, KITCHEN_BOUNDS_Z);
                clampedPos.y = 0.0f;
                transform.position = clampedPos;
            }
            else
            {
                Vector3 newPos = transform.position + move;
                newPos.x = Mathf.Clamp(newPos.x, -KITCHEN_BOUNDS_X, KITCHEN_BOUNDS_X);
                newPos.z = Mathf.Clamp(newPos.z, -KITCHEN_BOUNDS_Z, KITCHEN_BOUNDS_Z);
                newPos.y = 0.0f;
                transform.position = newPos;
            }

            // 15° Total Sway movement animation (gentle waddle while walking)
            if (clamped.magnitude > 0.08f)
            {
                walkAnimTime += delta * 14.0f;
                float tiltDegrees = ComputeWalkSway(walkAnimTime); // 15° total sway arc (-7.5° to +7.5°)
                if (characterSpriteRenderer != null)
                {
                    characterSpriteRenderer.transform.localRotation = Quaternion.Euler(51.0f, 0f, tiltDegrees);
                    // Base character PNG textures face left naturally.
                    // Moving right -> flipX = true (faces right). Moving left -> flipX = false (natural left).
                    characterSpriteRenderer.flipX = ComputeFlipX(clamped.x, characterSpriteRenderer.flipX);
                }
            }
            else
            {
                walkAnimTime = 0.0f;
                if (characterSpriteRenderer != null)
                {
                    characterSpriteRenderer.transform.localRotation = Quaternion.Lerp(
                        characterSpriteRenderer.transform.localRotation,
                        Quaternion.Euler(51.0f, 0f, 0f),
                        delta * 14.0f
                    );
                }
            }
        }

        /// <summary>Sprite faces right when moving right, left when moving left, otherwise keeps facing.</summary>
        public static bool ComputeFlipX(float dx, bool current)
        {
            if (dx > 0.05f) return true;
            if (dx < -0.05f) return false;
            return current;
        }

        /// <summary>Walk wobble tilt in degrees (-7.5 to +7.5).</summary>
        public static float ComputeWalkSway(float t) => Mathf.Sin(t) * 7.5f;

        private Vector3 lastRemotePos;
        private bool hasLastRemotePos = false;
        private float remoteMoveGrace = 0.0f;
        private float remoteAnimTime = 0.0f;

        /// <summary>
        /// LAN partner chef: derives flip + wobble from network position updates
        /// (partner input is not simulated locally). Short grace keeps wobble smooth
        /// between 30Hz packets.
        /// </summary>
        public void UpdateRemoteWalkVisuals(float dt)
        {
            Vector3 pos = transform.position;
            if (!hasLastRemotePos)
            {
                lastRemotePos = pos;
                hasLastRemotePos = true;
                return;
            }

            Vector3 d = pos - lastRemotePos;
            d.y = 0.0f;
            lastRemotePos = pos;

            if (d.magnitude > 0.001f)
            {
                remoteMoveGrace = 0.15f;
                if (characterSpriteRenderer != null)
                    characterSpriteRenderer.flipX = ComputeFlipX(d.x / Mathf.Max(d.magnitude, 0.0001f), characterSpriteRenderer.flipX);
            }

            if (remoteMoveGrace > 0.0f)
            {
                remoteMoveGrace -= dt;
                remoteAnimTime += dt * 14.0f;
                if (characterSpriteRenderer != null)
                    characterSpriteRenderer.transform.localRotation = Quaternion.Euler(51.0f, 0f, ComputeWalkSway(remoteAnimTime));
            }
            else
            {
                remoteAnimTime = 0.0f;
                if (characterSpriteRenderer != null)
                {
                    characterSpriteRenderer.transform.localRotation = Quaternion.Lerp(
                        characterSpriteRenderer.transform.localRotation,
                        Quaternion.Euler(51.0f, 0f, 0f),
                        Mathf.Clamp01(dt * 14.0f));
                }
            }
        }

        /// <summary>True on the joining device during a LAN game (host is authoritative).</summary>
        public static bool IsLanClient()
        {
            return NetworkManager.Instance != null && NetworkManager.Instance.isLanActive && !NetworkManager.Instance.IsHost();
        }

        public void SetCarriedItem(KitchenItem item)
        {
            carriedItem = item?.Clone();
            RefreshVisuals();
        }

        public KitchenItem ClearCarriedItem()
        {
            var prev = carriedItem?.Clone();
            carriedItem = null;
            RefreshVisuals();
            return prev;
        }

        /// <summary>
        /// Throws the carried ingredient or dish in the chef's facing direction.
        /// Can be caught by the other chef, land on an empty counter, enter the pass counter,
        /// or drop into the trash bin.
        /// </summary>
        public bool ThrowItem(Vector2 aimDir)
        {
            if (carriedItem == null)
                return false;

            Vector2 dir = aimDir;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = (characterSpriteRenderer != null && characterSpriteRenderer.flipX) ? Vector2.right : Vector2.left;
            }
            dir.Normalize();

            // LAN client: ask host to throw; the item leaves our hands when the host snapshot says so.
            if (IsLanClient())
            {
                NetworkManager.Instance.SendClientThrow(dir);
                return true;
            }

            var itemToThrow = ClearCarriedItem();
            if (itemToThrow == null)
                return false;

            Vector3 landingTarget = transform.position + new Vector3(dir.x * 4.5f, 0.0f, dir.y * 4.5f);
            landingTarget.x = Mathf.Clamp(landingTarget.x, -KITCHEN_BOUNDS_X, KITCHEN_BOUNDS_X);
            landingTarget.z = Mathf.Clamp(landingTarget.z, -KITCHEN_BOUNDS_Z, KITCHEN_BOUNDS_Z);

            if (KitchenController.Instance != null)
            {
                // 1. Partner catch detection
                foreach (var otherChef in KitchenController.Instance.playersByPeer.Values)
                {
                    if (otherChef != this && otherChef.carriedItem == null)
                    {
                        float dist = Vector3.Distance(otherChef.transform.position, landingTarget);
                        if (dist <= 2.2f)
                        {
                            otherChef.SetCarriedItem(itemToThrow);
                            KitchenController.Instance.TriggerStatusToast($"Pass Caught by {otherChef.characterId.ToUpper()}!", new Color(0.35f, 1.0f, 0.6f));
                            return true;
                        }
                    }
                }

                // 2. First free table along the throw path (tight snap radius, never far-side tables)
                StationBase nearest = FindThrowLandingStation(transform.position, dir);
                if (nearest is CounterStation counter && counter.heldItem == null)
                {
                    counter.heldItem = itemToThrow;
                    KitchenController.Instance.TriggerStatusToast($"Item Landed on {counter.displayName}!", new Color(0.9f, 0.9f, 0.5f));
                    return true;
                }

                if (nearest is PassCounter pass)
                {
                    for (int i = 0; i < PassCounter.NUM_SLOTS; i++)
                    {
                        if (pass.slots[i] == null || pass.slots[i].item == null)
                        {
                            pass.PlaceItem(i, itemToThrow, peerId, Time.time);
                            KitchenController.Instance.TriggerStatusToast("Thrown into Pass Counter slot!", new Color(0.95f, 0.8f, 0.3f));
                            return true;
                        }
                    }
                }

                if (nearest is TrashStation)
                {
                    KitchenController.Instance.TriggerStatusToast("Thrown cleanly into Trash!", new Color(0.6f, 0.6f, 0.6f));
                    return true;
                }

                KitchenController.Instance.TriggerStatusToast("Nowhere to land!", new Color(1.0f, 0.55f, 0.4f));
            }

            // Nothing caught it: item stays in hands instead of vanishing.
            SetCarriedItem(itemToThrow);
            return false;
        }

        /// <summary>Throw lands only on a table within this distance of the flight path.</summary>
        public const float THROW_SNAP_RADIUS = 1.4f;
        public const float THROW_RANGE = 4.5f;

        /// <summary>
        /// Walks the throw path from near the chef outward and returns the first table that can
        /// take the item (free counter, pass counter with a free slot, or trash).
        /// Pass Counter uses its long rectangle footprint so the middle exchange table is easy to hit.
        /// </summary>
        public static StationBase FindThrowLandingStation(Vector3 origin, Vector2 dir)
        {
            if (KitchenController.Instance == null) return null;
            for (float d = 1.0f; d <= THROW_RANGE + 0.001f; d += 0.25f)
            {
                Vector3 p = origin + new Vector3(dir.x * d, 0.0f, dir.y * d);
                StationBase best = null;
                float bestDist = THROW_SNAP_RADIUS;
                foreach (var st in KitchenController.Instance.stations.Values)
                {
                    if (st == null || !CanTakeThrownItem(st)) continue;
                    float dist = ThrowDistanceTo(st, p);
                    if (dist <= bestDist)
                    {
                        bestDist = dist;
                        best = st;
                    }
                }
                if (best != null) return best;
            }
            return null;
        }

        private static bool CanTakeThrownItem(StationBase st)
        {
            if (st is CounterStation c) return c.heldItem == null;
            if (st is PassCounter pc)
            {
                if (pc.slots == null) return false;
                for (int i = 0; i < PassCounter.NUM_SLOTS; i++)
                    if (pc.slots[i] == null || pc.slots[i].item == null) return true;
                return false;
            }
            return st is TrashStation;
        }

        private static float ThrowDistanceTo(StationBase st, Vector3 p)
        {
            Vector3 c = st.transform.position;
            float dx = Mathf.Abs(p.x - c.x);
            float dz = Mathf.Abs(p.z - c.z);
            if (st is PassCounter)
            {
                dx = Mathf.Max(0.0f, dx - 0.8f);
                dz = Mathf.Max(0.0f, dz - 1.6f);
            }
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static System.Collections.Generic.Dictionary<string, Sprite> _itemSpriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
        private static readonly System.Collections.Generic.HashSet<string> _missingSprites = new System.Collections.Generic.HashSet<string>();

        public static Sprite LoadItemSprite(string itemIdOrSpriteName)
        {
            if (string.IsNullOrEmpty(itemIdOrSpriteName)) return null;
            string sprName = ResolveItemSpriteName(itemIdOrSpriteName);
            if (_itemSpriteCache.TryGetValue(sprName, out Sprite cached) && cached != null)
                return cached;
            if (_missingSprites.Contains(sprName)) return null;

            Sprite spr = Resources.Load<Sprite>($"Sprites/Items/{sprName}");
            if (spr == null)
            {
                Texture2D tex = Resources.Load<Texture2D>($"Sprites/Items/{sprName}");
#if UNITY_EDITOR
                if (tex == null)
                {
                    tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Sprites/Items/{sprName}.png");
                    if (tex == null)
                        tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Sprites/Items/{sprName}.png");
                }
#endif
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    float ppu = Mathf.Max(32f, tex.width);
                    spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
                }
            }
            if (spr != null)
                _itemSpriteCache[sprName] = spr;
            else
                _missingSprites.Add(sprName);
            return spr;
        }

        public static string ResolveItemSpriteName(string itemId)
        {
            switch (itemId)
            {
                case "unprocessed_raw_meat": case "pork_raw": case "beef_raw": return "unprocessed_raw_meat";
                case "korean_bbq_raw": case "pork_portioned": return "korean_bbq_raw";
                case "bulgogi_raw": case "bulgogi_portioned": case "beef_sliced": return "bulgogi_portioned";
                case "bbq_beef": case "bulgogi_seared": case "bulgogi_cooked": case "pork_grilled": case "pork_cooked": case "bbq_with_egg": return "bulgogi";
                case "tteok_raw": case "tteok_portioned": case "tteok_cooked": return "tteok_portioned";
                case "tteokbokki_raw": case "tteokbokki_simmered": case "tteokbokki": return "tteokbokki";
                case "gochujang": case "gochujang_sauce": case "korean_sauce": return "gochujang";
                case "flour": case "flour_sack": case "baguette_dough": return "flour";
                case "cooked_flour": case "bot_chien_batter": return "cooked_flour";
                case "bot_chien": return "bot_chien";
                case "rice_raw": case "rice_portioned": return "rice_portioned";
                case "cooked_rice": case "rice_with_egg": return "cooked_rice";
                case "korean_bbq_rice": case "kimchi_bokkeumbap": case "kimchi_bokkeumbap_simmered": return "korean_bbq_rice";
                case "viet_broken_rice": return "viet_broken_rice";
                case "egg_raw": case "egg_cooked": return "egg_raw";
                case "baguette": case "banh_mi_partial_bp": case "banh_mi_partial_bb": return "baguette";
                case "radish_carrot": case "daikon_carrot_raw": return "radish_carrot";
                case "pickled_veg": case "kimchi_raw": case "kimchi_slaw": case "banh_mi_partial_pb": return "pickled_veg";
                case "banh_mi_thit": case "bulgogi_banh_mi": return "bulgogi_banh_mi";
                case "noodles_raw": case "noodles_blanched": case "pho_bo": return "tteokbokki";
                case "broth_hot": return "gochujang";
                case "clean_plate": case "dish_plate": case "clean_bowl": case "dish_bowl": return "dish_bowl";
                case "burnt_waste": return "beef_raw";
                default: return itemId;
            }
        }

        public void RefreshVisuals()
        {
            if (characterSpriteRenderer != null)
            {
                string charSprName = $"{characterId}_tier{characterTier}";
                Sprite spr = Resources.Load<Sprite>($"Sprites/Characters/{charSprName}");
                if (spr != null)
                {
                    characterSpriteRenderer.sprite = spr;
                    if (spr.bounds.size.y > 0.01f)
                    {
                        float scale = 1.95f / spr.bounds.size.y;
                        characterSpriteRenderer.transform.localScale = Vector3.one * scale;
                    }
                }
            }

            if (thirdArmVisual != null)
            {
                bool armVisible = (characterId == "linh" && characterTier >= 2);
                thirdArmVisual.SetActive(armVisible);
            }
            if (thirdArmGlowVisual != null)
            {
                bool glowVisible = (characterId == "linh" && characterTier >= 3);
                thirdArmGlowVisual.SetActive(glowVisible);
            }

            if (carriedItemSpriteRenderer != null)
            {
                carriedItemSpriteRenderer.gameObject.SetActive(carriedItem != null);
                if (carriedItem != null)
                {
                    Sprite itemSpr = LoadItemSprite(carriedItem.itemId);
                    if (itemSpr != null)
                    {
                        carriedItemSpriteRenderer.sprite = itemSpr;
                    }
                    carriedItemSpriteRenderer.transform.localPosition = carriedItemBaseOffset;
                    carriedItemSpriteRenderer.transform.localRotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                }
            }
        }
    }
}
