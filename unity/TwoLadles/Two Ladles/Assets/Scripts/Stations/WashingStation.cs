using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class WashingStation : StationBase
    {
        public const int REQUIRED_SCRUBS = 4;
        public int scrubStrokesCompleted = 0;

        private int GetEffectiveRequiredScrubs()
        {
            if (ProgressionManager.Instance != null && ProgressionManager.Instance.IsUpgradeOwned("high_pressure_sink"))
            {
                return 3;
            }
            return REQUIRED_SCRUBS;
        }

        public override void RefreshStationVisuals()
        {
            base.RefreshStationVisuals();
            int req = GetEffectiveRequiredScrubs();
            bool showBar = ShiftManager.Instance != null && ShiftManager.Instance.dirtyPlatesAtReturn > 0 && scrubStrokesCompleted > 0;
            if (showBar)
            {
                SetProgressBar(true, (float)scrubStrokesCompleted / req, new Color(0.35f, 0.85f, 1.0f, 1.0f));
            }
            else
            {
                SetProgressBar(false, 0f, Color.clear);
            }
        }

        public (bool ok, string reason) TakeCleanVessel(string vesselType = "plate")
        {
            if (ShiftManager.Instance == null) return (false, "Shift manager offline.");
            if (ShiftManager.Instance.TakeCleanPlate())
            {
                return (true, "Took clean vessel.");
            }
            return (false, "Wash dirty plates first!");
        }

        public (bool ok, string reason, bool plateCleaned) PerformScrubStroke()
        {
            if (ShiftManager.Instance == null)
                return (false, "Offline.", false);

            if (ShiftManager.Instance.dirtyPlatesAtReturn <= 0)
                return (false, "No dirty plates.", false);

            int req = GetEffectiveRequiredScrubs();
            scrubStrokesCompleted++;
            if (scrubStrokesCompleted >= req)
            {
                scrubStrokesCompleted = 0;
                ShiftManager.Instance.dirtyPlatesAtReturn--;
                ShiftManager.Instance.ReturnCleanPlate();
                RefreshStationVisuals();
                return (true, "Clean!", true);
            }

            RefreshStationVisuals();
            return (true, "", false);
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (playerItem != null)
                return (false, "Hands full.");

            if (ShiftManager.Instance == null)
                return (false, "Offline.");

            if (ShiftManager.Instance.TakeCleanPlate())
            {
                playerItem = new KitchenItem("clean_plate", "Clean Plate", "plate", QualityTier.STANDARD);
                return (true, "Clean Plate.");
            }

            return (false, "Wash dirty plates first!");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (playerItem == null && ShiftManager.Instance != null && ShiftManager.Instance.cleanPlates > 0 && ShiftManager.Instance.dirtyPlatesAtReturn == 0)
            {
                ShiftManager.Instance.TakeCleanPlate();
                playerItem = new KitchenItem("clean_bowl", "Clean Bowl", "bowl", QualityTier.STANDARD);
                return (true, "Clean Bowl.");
            }

            if (ShiftManager.Instance != null && ShiftManager.Instance.dirtyPlatesAtReturn > 0)
            {
                var res = PerformScrubStroke();
                return (res.ok, res.reason);
            }

            if (playerItem == null && ShiftManager.Instance != null && ShiftManager.Instance.cleanPlates > 0)
            {
                ShiftManager.Instance.TakeCleanPlate();
                playerItem = new KitchenItem("clean_bowl", "Clean Bowl", "bowl", QualityTier.STANDARD);
                return (true, "Clean Bowl.");
            }

            return (false, "No dirty plates.");
        }
    }
}
