using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Stations;

namespace TwoLadles.AI
{
    public class NavNode
    {
        public string id;
        public Vector3 position;
        public List<string> neighborIds = new List<string>();

        public NavNode(string pId, Vector3 pPos, params string[] pNeighbors)
        {
            id = pId;
            position = pPos;
            if (pNeighbors != null)
            {
                neighborIds.AddRange(pNeighbors);
            }
        }
    }

    /// <summary>
    /// Deterministic 2.5D A* Waypoint Navigation Graph for Two Ladles Kitchen.
    /// Provides 100% collision-free paths along clear corridors, wide crosswalks, and station approach zones.
    /// Completely avoids all station BoxColliders.
    /// </summary>
    public static class KitchenNavGraph
    {
        private static Dictionary<string, NavNode> nodes = new Dictionary<string, NavNode>();
        private static bool initialized = false;

        public static void EnsureInitialized()
        {
            if (initialized && nodes.Count > 0) return;
            nodes.Clear();

            // 1. WEST ZONE (Linh's Walkways & Stations)
            // Outer West Column (X = -8.5f)
            AddNode("NW_Outer", new Vector3(-8.5f, 0f, 4.6f), "W_Stove1", "W_Fridge_App");
            AddNode("W_Stove1", new Vector3(-8.5f, 0f, 3.4f), "NW_Outer", "W_Stove2");
            AddNode("W_Stove2", new Vector3(-8.5f, 0f, 0.2f), "W_Stove1", "W_Mid_North_Outer", "W_Mid_South_Outer");
            AddNode("W_Mid_North_Outer", new Vector3(-8.5f, 0f, 2.2f), "W_Stove2", "W_Mid_North_Cross", "W_Mid_South_Outer");
            AddNode("W_Mid_South_Outer", new Vector3(-8.5f, 0f, -3.4f), "W_Stove2", "W_Mid_North_Outer", "W_Mid_South_Cross", "W_Shelf");
            AddNode("W_Shelf", new Vector3(-8.5f, 0f, -3.3f), "W_Mid_South_Outer", "SW_Outer");
            AddNode("SW_Outer", new Vector3(-8.5f, 0f, -4.8f), "W_Shelf");

            // North Wall Fridge West Approach (Z = 4.6f)
            AddNode("W_Fridge_App", new Vector3(-6.2f, 0f, 4.6f), "NW_Outer", "NW_Inner");

            // Inner West Column (X = -2.2f)
            AddNode("NW_Inner", new Vector3(-2.2f, 0f, 4.6f), "W_Fridge_App", "W_Prep_App", "W_Serving_App");
            AddNode("W_Serving_App", new Vector3(-1.4f, 0f, 4.2f), "NW_Inner", "W_Prep_App");
            AddNode("W_Prep_App", new Vector3(-2.2f, 0f, 3.6f), "NW_Inner", "W_Serving_App", "W_Counter1_App");
            AddNode("W_Counter1_App", new Vector3(-2.2f, 0f, 0.8f), "W_Prep_App", "W_Mid_North_Cross", "W_Pass_App", "W_Counter2_App");
            AddNode("W_Pass_App", new Vector3(-1.4f, 0f, 0.5f), "W_Counter1_App", "W_Counter2_App");
            AddNode("W_Counter2_App", new Vector3(-2.2f, 0f, -2.0f), "W_Counter1_App", "W_Pass_App", "SW_Inner");
            AddNode("SW_Inner", new Vector3(-2.2f, 0f, -3.4f), "W_Counter2_App", "W_Mid_South_Cross", "W_Counter3_App", "W_Trash_App");
            AddNode("W_Counter3_App", new Vector3(-2.2f, 0f, -5.0f), "SW_Inner", "W_Trash_App");
            AddNode("W_Trash_App", new Vector3(-1.4f, 0f, -5.5f), "SW_Inner", "W_Counter3_App");

            // West Clear Crosswalks (Zero BoxCollider Intersections)
            // Mid-North Crosswalk (Z = 2.2f, wide gap between prep_west and counter_west)
            AddNode("W_Mid_North_Cross", new Vector3(-5.5f, 0f, 2.2f), "W_Mid_North_Outer", "W_Counter1_App");
            // Mid-South Crosswalk (Z = -3.4f, wide gap between counter_west_2 and serving_station)
            AddNode("W_Mid_South_Cross", new Vector3(-5.5f, 0f, -3.4f), "W_Mid_South_Outer", "SW_Inner");

            // 2. CENTER DIVIDER (Chefs are 100% strictly isolated to their own half)
            // No C_North or C_South: physical stainless-steel counter column and static colliders block all cross-movement.

            // 3. EAST ZONE (Jiwon's Walkways & Stations)
            // Inner East Column (X = +2.2f)
            AddNode("NE_Inner", new Vector3(2.2f, 0f, 4.6f), "E_Fridge_App", "E_Counter1_App", "E_Serving_App");
            AddNode("E_Serving_App", new Vector3(1.4f, 0f, 4.2f), "NE_Inner", "E_Counter1_App");
            AddNode("E_Counter1_App", new Vector3(2.2f, 0f, 1.8f), "NE_Inner", "E_Serving_App", "E_Mid_North_Cross", "E_Pass_App", "E_Prep_App");
            AddNode("E_Pass_App", new Vector3(1.4f, 0f, 0.5f), "E_Counter1_App", "E_Mid_North_Cross", "E_Prep_App");
            AddNode("E_Prep_App", new Vector3(2.2f, 0f, -1.6f), "E_Counter1_App", "E_Pass_App", "SE_Inner");
            AddNode("SE_Inner", new Vector3(2.2f, 0f, -3.4f), "E_Prep_App", "E_Mid_South_Cross", "E_Counter2_App", "E_Trash_App");
            AddNode("E_Counter2_App", new Vector3(2.2f, 0f, -5.0f), "SE_Inner", "E_Trash_App");
            AddNode("E_Trash_App", new Vector3(1.4f, 0f, -5.5f), "SE_Inner", "E_Counter2_App");

            // North Wall Fridge East Approach (Z = 4.6f)
            AddNode("E_Fridge_App", new Vector3(6.2f, 0f, 4.6f), "NE_Inner", "NE_Outer");

            // East Outer Column (X = +8.5f)
            AddNode("NE_Outer", new Vector3(8.5f, 0f, 4.6f), "E_Fridge_App", "E_Grill");
            AddNode("E_Grill", new Vector3(8.5f, 0f, 3.4f), "NE_Outer", "E_Wok");
            AddNode("E_Wok", new Vector3(8.5f, 0f, 0.2f), "E_Grill", "E_Mid_North_Outer", "E_Mid_South_Outer");
            AddNode("E_Mid_North_Outer", new Vector3(8.5f, 0f, 0.1f), "E_Wok", "E_Mid_North_Cross", "E_Mid_South_Outer");
            AddNode("E_Mid_South_Outer", new Vector3(8.5f, 0f, -3.4f), "E_Wok", "E_Mid_North_Outer", "E_Mid_South_Cross", "E_Shelf");
            AddNode("E_Shelf", new Vector3(8.5f, 0f, -3.3f), "E_Mid_South_Outer", "SE_Outer");
            AddNode("SE_Outer", new Vector3(8.5f, 0f, -4.8f), "E_Shelf");

            // East Clear Crosswalks (Zero BoxCollider Intersections)
            // Mid-North Crosswalk (Z = 0.1f, wide gap directly connecting simmer_wok to Pass Counter)
            AddNode("E_Mid_North_Cross", new Vector3(5.5f, 0f, 0.1f), "E_Mid_North_Outer", "E_Counter1_App", "E_Pass_App");
            // Mid-South Crosswalk (Z = -3.4f, wide gap between prep_east and counter_east_2)
            AddNode("E_Mid_South_Cross", new Vector3(5.5f, 0f, -3.4f), "E_Mid_South_Outer", "SE_Inner");

            // Ensure all graph connections are 100% bidirectional
            foreach (var kvp in nodes)
            {
                foreach (var nId in new List<string>(kvp.Value.neighborIds))
                {
                    if (nodes.TryGetValue(nId, out var neighbor))
                    {
                        if (!neighbor.neighborIds.Contains(kvp.Key))
                        {
                            neighbor.neighborIds.Add(kvp.Key);
                        }
                    }
                }
            }

            initialized = true;
        }

        private static void AddNode(string id, Vector3 pos, params string[] neighbors)
        {
            if (nodes.ContainsKey(id)) return;
            nodes[id] = new NavNode(id, pos, neighbors);
        }

        public static Vector3 GetStationApproachPosition(StationBase station, int peerId)
        {
            EnsureInitialized();
            if (station == null) return Vector3.zero;

            string sId = station.stationId;
            if (sId == "pass_counter")
            {
                return (peerId == 1) ? nodes["W_Pass_App"].position : nodes["E_Pass_App"].position;
            }
            if (sId == "trash_bin")
            {
                return (peerId == 1) ? nodes["W_Trash_App"].position : nodes["E_Trash_App"].position;
            }
            if (sId == "stockpot") return nodes["W_Stove1"].position;
            if (sId == "noodle_blancher") return nodes["W_Stove2"].position;
            if (sId == "shelf_west") return nodes["W_Shelf"].position;
            if (sId == "fridge_west") return nodes["W_Fridge_App"].position;
            if (sId == "prep_west") return nodes["W_Prep_App"].position;
            if (sId == "counter_west") return nodes["W_Counter1_App"].position;
            if (sId == "counter_west_2" || sId == "assembly_west") return nodes["W_Counter2_App"].position;
            if (sId == "counter_west_3") return nodes["W_Counter3_App"].position;
            if (sId == "serving_station")
            {
                return (peerId == 1) ? nodes["W_Serving_App"].position : nodes["E_Serving_App"].position;
            }

            if (sId == "grill_pan") return nodes["E_Grill"].position;
            if (sId == "simmer_wok") return nodes["E_Wok"].position;
            if (sId == "shelf_east") return nodes["E_Shelf"].position;
            if (sId == "fridge_east") return nodes["E_Fridge_App"].position;
            if (sId == "prep_east" || sId == "assembly_east") return nodes["E_Prep_App"].position;
            if (sId == "counter_east") return nodes["E_Counter1_App"].position;
            if (sId == "counter_east_2") return nodes["E_Counter2_App"].position;

            // Fallback: 1.25m offset toward kitchen center
            Vector3 centerDir = (Vector3.zero - station.transform.position).normalized;
            centerDir.y = 0f;
            return station.transform.position + centerDir * 1.25f;
        }

        /// <summary>
        /// Finds the closest NavNode to a world coordinate.
        /// </summary>
        public static string FindClosestNodeId(Vector3 pos, int peerId = 0)
        {
            EnsureInitialized();
            string bestId = null;
            float bestDistSq = float.MaxValue;

            foreach (var kvp in nodes)
            {
                // Prefer same-side nodes for initial chef location
                if (peerId == 1 && kvp.Value.position.x > 0.5f) continue;
                if (peerId == 2 && kvp.Value.position.x < -0.5f) continue;

                float dSq = (kvp.Value.position - pos).sqrMagnitude;
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestId = kvp.Key;
                }
            }

            if (bestId == null)
            {
                foreach (var kvp in nodes)
                {
                    float dSq = (kvp.Value.position - pos).sqrMagnitude;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestId = kvp.Key;
                    }
                }
            }

            return bestId;
        }

        /// <summary>
        /// A* Pathfinding between current world position and target world position.
        /// Returns an ordered list of waypoints ending at targetPos.
        /// </summary>
        public static List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos, int peerId = 0)
        {
            EnsureInitialized();
            List<Vector3> path = new List<Vector3>();

            // Direct line check: if already very close (< 1.1m), move direct
            if (Vector3.Distance(startPos, targetPos) < 1.1f)
            {
                path.Add(targetPos);
                return path;
            }

            string startNodeId = FindClosestNodeId(startPos, peerId);
            string endNodeId = FindClosestNodeId(targetPos, peerId);

            if (string.IsNullOrEmpty(startNodeId) || string.IsNullOrEmpty(endNodeId))
            {
                path.Add(targetPos);
                return path;
            }

            if (startNodeId == endNodeId)
            {
                path.Add(nodes[startNodeId].position);
                path.Add(targetPos);
                return path;
            }

            // A* Algorithm
            var openSet = new List<string> { startNodeId };
            var cameFrom = new Dictionary<string, string>();
            var gScore = new Dictionary<string, float> { [startNodeId] = 0f };
            var fScore = new Dictionary<string, float> { [startNodeId] = Vector3.Distance(nodes[startNodeId].position, nodes[endNodeId].position) };

            while (openSet.Count > 0)
            {
                // Find node in openSet with lowest fScore
                string current = openSet[0];
                float lowestF = fScore.TryGetValue(current, out float fVal) ? fVal : float.MaxValue;
                for (int i = 1; i < openSet.Count; i++)
                {
                    float f = fScore.TryGetValue(openSet[i], out float val) ? val : float.MaxValue;
                    if (f < lowestF)
                    {
                        current = openSet[i];
                        lowestF = f;
                    }
                }

                if (current == endNodeId)
                {
                    // Reconstruct path
                    var nodePath = new List<string> { current };
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        nodePath.Insert(0, current);
                    }

                    foreach (var nId in nodePath)
                    {
                        path.Add(nodes[nId].position);
                    }
                    path.Add(targetPos);
                    return path;
                }

                openSet.Remove(current);

                var currentNode = nodes[current];
                float currentG = gScore.TryGetValue(current, out float cg) ? cg : float.MaxValue;

                foreach (var neighborId in currentNode.neighborIds)
                {
                    if (!nodes.ContainsKey(neighborId)) continue;

                    float tentativeG = currentG + Vector3.Distance(currentNode.position, nodes[neighborId].position);
                    float neighborG = gScore.TryGetValue(neighborId, out float ng) ? ng : float.MaxValue;

                    if (tentativeG < neighborG)
                    {
                        cameFrom[neighborId] = current;
                        gScore[neighborId] = tentativeG;
                        fScore[neighborId] = tentativeG + Vector3.Distance(nodes[neighborId].position, nodes[endNodeId].position);

                        if (!openSet.Contains(neighborId))
                        {
                            openSet.Add(neighborId);
                        }
                    }
                }
            }

            // Fallback direct
            path.Add(nodes[startNodeId].position);
            path.Add(nodes[endNodeId].position);
            path.Add(targetPos);
            return path;
        }
    }
}
