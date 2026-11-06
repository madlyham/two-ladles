using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Player;

namespace TwoLadles.Kitchen
{
    public class KitchenController : MonoBehaviour
    {
        public static KitchenController Instance { get; private set; }

        public event Action<string, Color> OnStatusToast;

        public Dictionary<int, PlayerController> playersByPeer = new Dictionary<int, PlayerController>();
        public Dictionary<string, StationBase> stations = new Dictionary<string, StationBase>();
        public Dictionary<string, PartTimeWorker> activeWorkers = new Dictionary<string, PartTimeWorker>();
        public Dictionary<string, float> workerCooldowns = new Dictionary<string, float>();

        private int workerUidCounter = 1;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Tick worker cooldowns
            var keys = new List<string>(workerCooldowns.Keys);
            foreach (var k in keys)
            {
                if (workerCooldowns[k] > 0.0f)
                {
                    workerCooldowns[k] = Mathf.Max(0.0f, workerCooldowns[k] - dt);
                }
            }

            // Tick cooking stations
            foreach (var st in stations.Values)
            {
                if (st is CookingStation cs)
                {
                    cs.TickCooking(dt);
                }
            }
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
    }
}
