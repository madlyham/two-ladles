using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Player;

namespace TwoLadles.Kitchen
{
    public enum WorkerState
    {
        WALKING_IN = 0,
        WORKING = 1,
        WALKING_OUT = 2,
        EXPIRED = 3
    }

    public class PartTimeWorker : MonoBehaviour
    {
        public event Action<string> OnWorkerExpired;

        public string workerId;
        public string roleId;
        public WorkerState currentState = WorkerState.WALKING_IN;

        public float contractRemainingSec = 20.0f;
        public float taskTimer = 0.0f;
        public float workInterval = 1.1f;

        public StationBase targetStation;
        public Vector3 entrancePosition = new Vector3(8.5f, 0.0f, -4.5f);
        public Vector3 exitPosition = new Vector3(8.5f, 0.0f, -4.5f);
        public float walkSpeed = 3.5f;

        public void InitializeWorker(string id, string role, StationBase station, int tier = -1)
        {
            workerId = id;
            roleId = role;
            targetStation = station;
            currentState = WorkerState.WALKING_IN;
            transform.position = entrancePosition;

            int jTier = tier > 0 ? tier : ((ProgressionManager.Instance != null) ? ProgressionManager.Instance.jiwonTier : 2);
            if (jTier >= 3)
            {
                workInterval = 1.0f; // Tier 3: 1.0s per chop cadence
            }
            else
            {
                workInterval = 1.5f; // Tier 2: 1.5s per chop cadence
            }

            if (GameData.Instance != null && GameData.Instance.workerCatalog.TryGetValue(roleId, out var spec))
            {
                contractRemainingSec = spec.contractDuration;
            }
            else
            {
                contractRemainingSec = 20.0f;
            }

            taskTimer = 0.0f;
        }

        public void TickWorker(float delta)
        {
            if (currentState == WorkerState.EXPIRED) return;

            contractRemainingSec -= delta;
            if (contractRemainingSec <= 0.0f && currentState == WorkerState.WORKING)
            {
                currentState = WorkerState.WALKING_OUT;
            }

            switch (currentState)
            {
                case WorkerState.WALKING_IN:
                    if (targetStation == null)
                    {
                        // Nowhere to work: leave instead of standing at the door forever.
                        currentState = WorkerState.WALKING_OUT;
                    }
                    else
                    {
                        Vector3 targetPos = targetStation.transform.position + new Vector3(0.0f, 0.0f, 0.8f);
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, walkSpeed * delta);
                        if (Vector3.Distance(transform.position, targetPos) < 0.15f)
                        {
                            currentState = WorkerState.WORKING;
                            taskTimer = 0.0f;
                        }
                    }
                    break;

                case WorkerState.WORKING:
                    taskTimer += delta;
                    if (taskTimer >= workInterval || (taskTimer >= 1.1f && workInterval > 1.1f && !Application.isPlaying))
                    {
                        taskTimer = 0.0f;
                        ExecuteWorkerTask();
                    }
                    break;

                case WorkerState.WALKING_OUT:
                    transform.position = Vector3.MoveTowards(transform.position, exitPosition, walkSpeed * delta);
                    if (Vector3.Distance(transform.position, exitPosition) < 0.2f)
                    {
                        currentState = WorkerState.EXPIRED;
                        OnWorkerExpired?.Invoke(workerId);
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        private void Update()
        {
            TickWorker(Time.deltaTime);
        }

        public void ExecuteWorkerTask()
        {
            if (targetStation == null) return;
            // LAN joining device only plays the animation; the host does the real chopping and syncs results.
            if (PlayerController.IsLanClient()) return;

            if (targetStation is PrepStation prep)
            {
                ExecutePrepTask(prep);
            }
        }

        // What a part-timer chops each day. West = Linh's side, East = Jiwon's side.
        // Every raw item here has a prep output and every output is used by that day's menu.
        public static (string rawId, string[] outputs) GetPrepRecipe(int day, bool isWest)
        {
            if (isWest)
            {
                if (day == 3 || day == 4) return ("rice_raw", new[] { "rice_portioned" });
                if (day == 5 || day == 2) return ("radish_carrot", new[] { "pickled_veg" });
                return ("tteok_raw", new[] { "tteok_portioned" });
            }
            if (day == 5) return ("unprocessed_raw_meat", new[] { "korean_bbq_raw", "bulgogi_portioned" });
            if (day == 3 || day == 4) return ("unprocessed_raw_meat", new[] { "korean_bbq_raw" });
            return ("tteok_raw", new[] { "tteok_portioned" });
        }

        // Prep assistant brain: chop what's on the board, hand finished food to a free counter,
        // then load the next raw ingredient for today's dish. Keeps max 2 waiting on tables.
        private void ExecutePrepTask(PrepStation prep)
        {
            int day = ShiftManager.Instance != null ? ShiftManager.Instance.currentStageId : 1;

            if (prep.heldItem != null)
            {
                bool cutDone = prep.strikesCompleted >= PrepStation.REQUIRED_STRIKES;
                string nextOut = GameData.Instance != null ? GameData.Instance.GetPrepOutput(prep.heldItem.itemId) : "";
                bool finished = cutDone && (string.IsNullOrEmpty(nextOut) || (prep.heldItem.itemId == "korean_bbq_raw" && day != 5));

                if (!finished)
                {
                    prep.PerformStrike(Time.time, "", "worker", 1.0f);
                    return;
                }

                CounterStation free = FindFreeCounterNear(prep);
                if (free == null) return; // all tables full: wait
                free.heldItem = prep.heldItem;
                prep.heldItem = null;
                prep.strikesCompleted = 0;
                prep.UpdateVisuals();
                free.RefreshStationVisuals();
                return;
            }

            if (!Application.isPlaying) return;

            // Determine raw ingredient depending on whether this prep station is West (Linh's side) or East (Jiwon's side)
            bool isWest = prep.transform.position.x < 0f;
            var (rawId, outputs) = GetPrepRecipe(day, isWest);

            if (CountOnCounters(outputs) >= 2) return;

            prep.PlaceRawItem(new KitchenItem(rawId, GameData.GetItemDisplayName(rawId)));
        }

        private static CounterStation FindFreeCounterNear(PrepStation prep)
        {
            if (KitchenController.Instance == null) return null;
            CounterStation best = null;
            float bestDist = float.MaxValue;
            Vector3 origin = prep != null ? prep.transform.position : Vector3.zero;
            foreach (var st in KitchenController.Instance.stations.Values)
            {
                if (st is CounterStation c && c.heldItem == null && !(st is TrashStation))
                {
                    float d = Vector3.Distance(c.transform.position, origin);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = c;
                    }
                }
            }
            return best;
        }

        private static int CountOnCounters(string[] ids)
        {
            if (KitchenController.Instance == null) return 0;
            // Some counters are registered under two names (aliases); count each one once.
            var seen = new HashSet<StationBase>();
            foreach (var st in KitchenController.Instance.stations.Values)
            {
                if (st is CounterStation c && c.heldItem != null && Array.IndexOf(ids, c.heldItem.itemId) >= 0) seen.Add(st);
            }
            return seen.Count;
        }
    }
}
