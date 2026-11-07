using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Kitchen;

namespace TwoLadles.Player
{
    public class PlayerController : MonoBehaviour
    {
        public const float BASE_MOVE_SPEED = 6.2f;
        public const float KITCHEN_BOUNDS_X = 8.8f;
        public const float KITCHEN_BOUNDS_Z = 5.2f;

        [Header("Identity")]
        public int peerId = 1;
        public string characterId = "linh";
        public string specializationId = "prep_master";
        public int characterTier = 1;

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
        public Vector3 carriedItemBaseOffset = new Vector3(0.0f, 1.85f, 0.0f);
        public float carriedHoverBobSpeed = 4.5f;
        public float carriedHoverBobHeight = 0.08f;
        public float carriedHoverTiltSpeed = 3.5f;
        public float carriedHoverTiltAngle = 10.0f;
        private float carriedAnimTime = 0.0f;

        private void Start()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            UpdateCarriedItemHover(Time.deltaTime);
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
            carriedItemSpriteRenderer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, tiltZ);
        }

        public void ConfigureChef(int pPeerId, string pCharId, string pSpecId, int pTier = 1)
        {
            peerId = pPeerId;
            characterId = pCharId;
            specializationId = pSpecId;
            characterTier = pTier;
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
            return BASE_MOVE_SPEED * mult;
        }

        public void ApplyMovement(Vector2 inputDir, float delta)
        {
            Vector2 clamped = inputDir;
            if (clamped.magnitude > 1.0f)
                clamped.Normalize();

            float speed = GetEffectiveMoveSpeed();
            Vector3 move = new Vector3(clamped.x * speed, 0.0f, clamped.y * speed) * delta;

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

            // 30% Tilt movement animation (waddle while walking)
            if (clamped.magnitude > 0.08f)
            {
                walkAnimTime += delta * 14.0f;
                float tiltDegrees = Mathf.Sin(walkAnimTime) * 17.5f; // ~30% movement tilt arc
                if (characterSpriteRenderer != null)
                {
                    characterSpriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees);
                    if (clamped.x < -0.05f) characterSpriteRenderer.flipX = true;
                    else if (clamped.x > 0.05f) characterSpriteRenderer.flipX = false;
                }
            }
            else
            {
                walkAnimTime = 0.0f;
                if (characterSpriteRenderer != null)
                {
                    characterSpriteRenderer.transform.localRotation = Quaternion.Lerp(
                        characterSpriteRenderer.transform.localRotation,
                        Quaternion.identity,
                        delta * 14.0f
                    );
                }
            }
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
                dir = (characterSpriteRenderer != null && characterSpriteRenderer.flipX) ? Vector2.left : Vector2.right;
            }
            dir.Normalize();

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
                            return true;
                        }
                    }
                }

                // 2. Nearest station target (Holding counter, Pass Counter, Trash bin)
                StationBase nearest = KitchenController.Instance.FindNearestStation(landingTarget);
                if (nearest is CounterStation counter && counter.heldItem == null)
                {
                    counter.heldItem = itemToThrow;
                    return true;
                }

                if (nearest is PassCounter pass)
                {
                    for (int i = 0; i < PassCounter.NUM_SLOTS; i++)
                    {
                        if (pass.slots[i] == null || pass.slots[i].item == null)
                        {
                            pass.PlaceItem(i, itemToThrow, peerId, Time.time);
                            return true;
                        }
                    }
                }

                if (nearest != null && nearest.stationId == "trash_bin")
                {
                    // Disposed in trash
                    return true;
                }
            }

            return true;
        }

        public void RefreshVisuals()
        {
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
                    carriedItemSpriteRenderer.transform.localPosition = carriedItemBaseOffset;
                    carriedItemSpriteRenderer.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
}
