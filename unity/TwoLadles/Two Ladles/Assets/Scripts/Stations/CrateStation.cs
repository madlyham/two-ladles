using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Kitchen;
using TwoLadles.Networking;
using TwoLadles.UI;

namespace TwoLadles.Stations
{
    public class CrateStation : StationBase
    {
        public string dispensedItemId;
        public bool isFridge = false;
        private SpriteRenderer _applianceSr;
        private Sprite _closedFridgeSprite;
        private Sprite _openFridgeSprite;
        private bool _isDoorOpen = false;

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!isFridge && (stationId == null || !stationId.Contains("fridge")))
                return;

            if (_applianceSr == null)
            {
                Transform sprTr = transform.Find("ApplianceSprite");
                if (sprTr != null)
                    _applianceSr = sprTr.GetComponent<SpriteRenderer>();
            }
            if (_applianceSr == null || KitchenController.Instance == null)
                return;

            if (_closedFridgeSprite == null)
                _closedFridgeSprite = KitchenController.Instance.GetStationSprite("fridge_south");
            if (_openFridgeSprite == null)
                _openFridgeSprite = KitchenController.Instance.GetStationSprite("fridge_south_open");

            bool chefNearby = false;
            float openRadius = 2.8f;
            foreach (var chef in KitchenController.Instance.playersByPeer.Values)
            {
                if (chef != null && Vector3.Distance(chef.transform.position, transform.position) <= openRadius)
                {
                    chefNearby = true;
                    break;
                }
            }

            if (chefNearby != _isDoorOpen)
            {
                _isDoorOpen = chefNearby;
                if (_isDoorOpen && _openFridgeSprite != null)
                    _applianceSr.sprite = _openFridgeSprite;
                else if (!_isDoorOpen && _closedFridgeSprite != null)
                    _applianceSr.sprite = _closedFridgeSprite;
            }
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            bool isFridgeStation = isFridge || (stationId != null && stationId.Contains("fridge"));

            int localPeer = NetworkManager.Instance != null ? NetworkManager.Instance.localPeerId : 1;
            bool isHuman = (peerId == localPeer);

            // In interactive gameplay with HUD active, human chef interacting with Fridge or Shelf opens the Select Wheel!
            if (isHuman && QuickSelectWheelUI.Instance != null)
            {
                if (playerItem != null)
                {
                    return (false, "Hands full.");
                }
                QuickSelectWheelUI.Instance.OpenWheelForStation(stationId, transform);
                return (true, isFridgeStation ? "Opened Fridge." : "Opened Shelf.");
            }

            if (playerItem == null)
            {
                if (string.IsNullOrEmpty(dispensedItemId))
                    return (false, "Crate is empty.");

                string dName = dispensedItemId;
                string vType = "none";
                if (GameData.Instance != null && GameData.Instance.ingredients.TryGetValue(dispensedItemId, out var ing))
                {
                    dName = ing.displayName;
                }
                playerItem = new KitchenItem(dispensedItemId, dName, vType, QualityTier.STANDARD);
                return (true, $"Grabbed {playerItem.displayName} from crate.");
            }

            if (playerItem.itemId == dispensedItemId)
            {
                return (false, "Already holding this ingredient.");
            }

            return (false, "Hands full.");
        }

        public (bool ok, string reason) DispenseItemDirect(ref KitchenItem playerItem, string requestedItemId)
        {
            if (playerItem != null) return (false, "Hands full.");
            if (string.IsNullOrEmpty(requestedItemId)) return (false, "Invalid item requested.");

            string dName = requestedItemId;
            if (GameData.Instance != null && GameData.Instance.ingredients.TryGetValue(requestedItemId, out var ing))
            {
                dName = ing.displayName;
            }
            playerItem = new KitchenItem(requestedItemId, dName, "none", QualityTier.STANDARD);
            return (true, $"Grabbed {playerItem.displayName}.");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return InteractPrimary(ref playerItem, peerId, characterId);
        }
    }
}
