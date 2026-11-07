using System;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;

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

        public void InitializeWorker(string id, string role, StationBase station)
        {
            workerId = id;
            roleId = role;
            targetStation = station;
            currentState = WorkerState.WALKING_IN;
            transform.position = entrancePosition;

            if (GameData.Instance != null && GameData.Instance.workerCatalog.TryGetValue(roleId, out var spec))
            {
                contractRemainingSec = spec.contractDuration;
                workInterval = spec.workInterval;
            }
            else
            {
                contractRemainingSec = 20.0f;
                workInterval = 1.1f;
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
                    if (targetStation != null)
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
                    if (taskTimer >= workInterval)
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

            if (targetStation is PrepStation prep)
            {
                if (prep.heldItem != null && prep.strikesCompleted < PrepStation.REQUIRED_STRIKES)
                {
                    prep.PerformStrike(Time.time, "", "worker", 1.0f);
                }
            }
            else if (targetStation is WashingStation wash)
            {
                if (ShiftManager.Instance != null && ShiftManager.Instance.dirtyPlatesAtReturn > 0)
                {
                    wash.PerformScrubStroke();
                }
            }
        }
    }
}
