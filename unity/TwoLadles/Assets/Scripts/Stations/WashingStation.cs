using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class WashingStation : StationBase
    {
        public const int REQUIRED_SCRUBS = 4;
        public int scrubStrokesCompleted = 0;

        public (bool ok, string reason, bool plateCleaned) PerformScrubStroke()
        {
            if (ShiftManager.Instance == null)
                return (false, "Shift manager offline.", false);

            if (ShiftManager.Instance.dirtyPlatesAtReturn <= 0)
                return (false, "No dirty plates to wash at return tray.", false);

            scrubStrokesCompleted++;
            if (scrubStrokesCompleted >= REQUIRED_SCRUBS)
            {
                scrubStrokesCompleted = 0;
                ShiftManager.Instance.dirtyPlatesAtReturn--;
                ShiftManager.Instance.ReturnCleanPlate();
                return (true, "Plate scrubbed clean and returned to stack!", true);
            }

            return (true, $"Scrub progress: {scrubStrokesCompleted}/{REQUIRED_SCRUBS}", false);
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (playerItem != null)
                return (false, "Hands full. Put down item before taking a clean plate.");

            if (ShiftManager.Instance == null)
                return (false, "Shift manager offline.");

            if (ShiftManager.Instance.TakeCleanPlate())
            {
                playerItem = new KitchenItem("clean_plate", "Clean Plate", "plate", QualityTier.STANDARD);
                return (true, "Took clean plate from stack.");
            }

            return (false, "All 4 vessels in use! Wash dirty plates at the sink.");
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            // If empty hands, take a clean bowl
            if (playerItem == null && ShiftManager.Instance != null && ShiftManager.Instance.cleanPlates > 0)
            {
                ShiftManager.Instance.TakeCleanPlate();
                playerItem = new KitchenItem("clean_bowl", "Clean Bowl", "bowl", QualityTier.STANDARD);
                return (true, "Took clean bowl from stack.");
            }

            // Scrub dirty dish
            var res = PerformScrubStroke();
            return (res.ok, res.reason);
        }
    }
}
