using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Stations
{
    public class AssemblyStation : StationBase
    {
        public List<string> placedComponents = new List<string>();

        public KitchenItem currentVessel { get => heldItem; set => heldItem = value; }
        public List<string> assembledIngredients => placedComponents;

        public (bool ok, string reason) PlaceVessel(KitchenItem vesselItem)
        {
            if (heldItem != null)
                return (false, "Assembly station already contains a vessel or dish.");

            if (vesselItem.vesselType != "plate" && vesselItem.vesselType != "bowl")
                return (false, "Assembly requires a clean Plate or Bowl placed first.");

            heldItem = vesselItem;
            placedComponents.Clear();
            return (true, $"Placed clean {vesselItem.vesselType} on assembly counter.");
        }

        public (bool ok, string reason, bool recipeCompleted, string completedRecipeId) AddComponent(KitchenItem ingredient)
        {
            if (heldItem == null)
                return (false, "Assembly rejects ingredients before a clean Plate or Bowl is placed.", false, "");

            if (heldItem.vesselType == "none")
                return (false, "Must place a Plate or Bowl before adding ingredients.", false, "");

            // Tteokbokki rejection on flat plate
            if (ingredient.itemId == "tteok_cooked" && heldItem.vesselType == "plate")
            {
                return (false, "Assembly rejects Tteokbokki on a flat Plate", false, "");
            }

            // Pho Bo specific rule: rejects hot broth before blanched noodles and beef are placed!
            if (ingredient.itemId == "broth_hot")
            {
                if (!placedComponents.Contains("noodles_blanched") || !placedComponents.Contains("beef_sliced"))
                {
                    return (false, "Pho Bo rejects pouring hot broth before noodles and sliced beef are in the bowl.", false, "");
                }
            }

            placedComponents.Add(ingredient.itemId);
            heldItem.assembledComponents.Add(ingredient.itemId);

            // Check if matches any recipe
            string matchId = CheckRecipeMatch(heldItem.vesselType, placedComponents);
            if (!string.IsNullOrEmpty(matchId))
            {
                heldItem.itemId = matchId;
                var spec = GameData.Instance != null && GameData.Instance.recipes.TryGetValue(matchId, out var r) ? r : null;
                heldItem.displayName = spec != null ? spec.displayName : matchId;

                if (ShiftManager.Instance != null && ShiftManager.Instance.isTutorial && ShiftManager.Instance.tutorialStep == 8)
                {
                    ShiftManager.Instance.AdvanceTutorialStep(9);
                }

                return (true, $"Completed dish: {heldItem.displayName}!", true, matchId);
            }

            return (true, $"Added {ingredient.displayName} to {heldItem.vesselType}.", false, "");
        }

        public string CheckRecipeMatch(string vessel, List<string> components)
        {
            if (GameData.Instance == null) return "";

            foreach (var kvp in GameData.Instance.recipes)
            {
                var spec = kvp.Value;
                if (spec.requiredVessel != vessel) continue;

                bool allFound = true;
                foreach (var req in spec.requiredIngredients)
                {
                    if (!components.Contains(req))
                    {
                        allFound = false;
                        break;
                    }
                }

                if (allFound && components.Count == spec.requiredIngredients.Count)
                {
                    return spec.id;
                }
            }

            return "";
        }

        public override (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            if (heldItem == null)
            {
                if (playerItem != null)
                {
                    if (playerItem.vesselType == "plate" || playerItem.vesselType == "bowl")
                    {
                        var res = PlaceVessel(playerItem);
                        if (res.ok) playerItem = null;
                        return res;
                    }
                    return (false, "Assembly rejects ingredients before a clean Plate or Bowl is placed.");
                }
                return (false, "Assembly counter is empty.");
            }
            else
            {
                if (playerItem != null)
                {
                    var res = AddComponent(playerItem);
                    if (res.ok) playerItem = null;
                    return (res.ok, res.reason);
                }
                else
                {
                    playerItem = heldItem;
                    heldItem = null;
                    placedComponents.Clear();
                    return (true, $"Picked up {playerItem.displayName} from assembly counter.");
                }
            }
        }

        public override (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId)
        {
            return (false, "Secondary action not applicable to Assembly Station.");
        }
    }
}
