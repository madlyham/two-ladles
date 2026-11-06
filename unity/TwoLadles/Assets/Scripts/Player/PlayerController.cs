using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

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
            Vector3 newPos = transform.position + move;

            newPos.x = Mathf.Clamp(newPos.x, -KITCHEN_BOUNDS_X, KITCHEN_BOUNDS_X);
            newPos.z = Mathf.Clamp(newPos.z, -KITCHEN_BOUNDS_Z, KITCHEN_BOUNDS_Z);
            newPos.y = 0.0f;
            transform.position = newPos;
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
        }
    }
}
