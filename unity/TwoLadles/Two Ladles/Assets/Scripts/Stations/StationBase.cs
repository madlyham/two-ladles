using UnityEngine;
using TwoLadles.Data;

namespace TwoLadles.Stations
{
    public abstract class StationBase : MonoBehaviour
    {
        public const float MAX_INTERACT_DISTANCE = 2.6f;

        [Header("Station Identity")]
        public string stationId;
        public string displayName;
        public string zone; // "linh_west", "jiwon_east", or "shared"

        [Header("Current Item State")]
        public KitchenItem heldItem = null;

        public virtual bool CanInteract(Vector3 playerPosition)
        {
            float dist = Vector3.Distance(transform.position, playerPosition);
            return dist <= MAX_INTERACT_DISTANCE;
        }

        public abstract (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId);
        public abstract (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId);
    }
}
