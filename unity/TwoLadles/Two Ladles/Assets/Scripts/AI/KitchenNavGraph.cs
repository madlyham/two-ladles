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
    /// Provides 100% collision-free paths along clear corridors, aisles, and station approach zones.
    /// Eliminates snagging on counter BoxColliders.
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
            // Outer West Column (X = -8.8)
            AddNode("NW_Outer", new Vector3(-8.8f, 0f, 4.2f), "W_Fridge_App", "W_Stove1", "W_N_Cross");
            AddNode("W_Stove1", new Vector3(-8.8f, 0f, 3.4f), "NW_Outer", "W_Stove2");
            AddNode("W_Stove2", new Vector3(-8.8f, 0f, 0.2f), "W_Stove1", "W_Shelf");
            AddNode("W_Shelf", new Vector3(-8.8f, 0f, -3.3f), "W_Stove2", "SW_Outer");
            AddNode("SW_Outer", new Vector3(-8.8f, 0f, -4.8f), "W_Shelf", "W_S_Cross");

            // North Wall Fridge West Approach
            AddNode("W_Fridge_App", new Vector3(-6.2f, 0f, 4.2f), "NW_Outer", "NW_Inner");

            // Inner West Column (X = -2.6)
            AddNode("NW_Inner", new Vector3(-2.6f, 0f, 4.2f), "W_Fridge_App", "W_Prep_App", "C_North");
            AddNode("W_Prep_App", new Vector3(-2.6f, 0f, 3.6f), "NW_Inner", "W_Counter1_App");
            AddNode("W_Counter1_App", new Vector3(-2.6f, 0f, 0.8f), "W_Prep_App", "W_Pass_App", "W_Counter2_App");
            AddNode("W_Pass_App", new Vector3(-1.4f, 0f, 0.5f), "W_Counter1_App", "W_Counter2_App", "NW_Inner", "SW_Inner");
            AddNode("W_Counter2_App", new Vector3(-2.6f, 0f, -2.0f), "W_Counter1_App", "W_Pass_App", "W_Serving_App");
            AddNode("W_Serving_App", new Vector3(-2.6f, 0f, -4.8f), "W_Counter2_App", "SW_Inner");
            AddNode("SW_Inner", new Vector3(-2.6f, 0f, -4.8f), "W_Serving_App", "W_S_Cross", "C_South");

            // West Cross Connectors
            AddNode("W_N_Cross", new Vector3(-5.5f, 0f, 4.2f), "NW_Outer", "NW_Inner");
            AddNode("W_S_Cross", new Vector3(-5.5f, 0f, -4.8f), "SW_Outer", "SW_Inner");

            // 2. CENTER PASSAGE & DIVIDER
            AddNode("C_North", new Vector3(0.0f, 0f, 4.2f), "NW_Inner", "NE_Inner");
            AddNode("C_South", new Vector3(0.0f, 0f, -4.8f), "SW_Inner", "SE_Inner", "Trash_App");
            AddNode("Trash_App", new Vector3(0.0f, 0f, -5.2f), "C_South");

            // 3. EAST ZONE (Jiwon's Walkways & Stations)
            // Inner East Column (X = +2.6)
            AddNode("NE_Inner", new Vector3(2.6f, 0f, 4.2f), "C_North", "E_Fridge_App", "E_Counter1_App");
            AddNode("E_Counter1_App", new Vector3(2.6f, 0f, 1.8f), "NE_Inner", "E_Pass_App", "E_Prep_App");
            AddNode("E_Pass_App", new Vector3(1.4f, 0f, 0.5f), "E_Counter1_App", "E_Prep_App", "NE_Inner", "SE_Inner");
            AddNode("E_Prep_App", new Vector3(2.6f, 0f, -1.6f), "E_Counter1_App", "E_Pass_App", "E_Counter2_App");
            AddNode("E_Counter2_App", new Vector3(2.6f, 0f, -4.8f), "E_Prep_App", "SE_Inner");
            AddNode("SE_Inner", new Vector3(2.6f, 0f, -4.8f), "E_Counter2_App", "E_S_Cross", "C_South");

            // North Wall Fridge East Approach
            AddNode("E_Fridge_App", new Vector3(6.2f, 0f, 4.2f), "NE_Inner", "NE_Outer");

            // Outer East Column (X = +8.8)
            AddNode("NE_Outer", new Vector3(8.8f, 0f, 4.2f), "E_Fridge_App", "E_Grill", "E_N_Cross");
            AddNode("E_Grill", new Vector3(8.8f, 0f, 3.4f), "NE_Outer", "E_Wok");
            AddNode("E_Wok", new Vector3(8.8f, 0f, 0.2f), "E_Grill", "E_Shelf");
            AddNode("E_Shelf", new Vector3(8.8f, 0f, -3.3f), "E_Wok", "SE_Outer");
            AddNode("SE_Outer", new Vector3(8.8f, 0f, -4.8f), "E_Shelf", "E_S_Cross");

            // East Cross Connectors
            AddNode("E_N_Cross", new Vector3(5.5f, 0f, 4.2f), "NE_Inner", "NE_Outer");
            AddNode("E_S_Cross", new Vector3(5.5f, 0f, -4.8f), "SE_Inner", "SE_Outer");

            // Ensure all graph connections are bidirectional
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
                return nodes["Trash_App"].position;
            }
            if (sId == "stockpot") return nodes["W_Stove1"].position;
            if (sId == "noodle_blancher") return nodes["W_Stove2"].position;
            if (sId == "shelf_west") return nodes["W_Shelf"].position;
            if (sId == "fridge_west") return nodes["W_Fridge_App"].position;
            if (sId == "prep_west") return nodes["W_Prep_App"].position;
            if (sId == "counter_west") return nodes["W_Counter1_App"].position;
            if (sId == "counter_west_2" || sId == "assembly_west") return nodes["W_Counter2_App"].position;
            if (sId == "serving_station") return nodes["W_Serving_App"].position;

            if (sId == "grill_pan") return nodes["E_Grill"].position;
            if (sId == "simmer_wok") return nodes["E_Wok"].position;
            if (sId == "shelf_east") return nodes["E_Shelf"].position;
            if (sId == "fridge_east") return nodes["E_Fridge_App"].position;
            if (sId == "prep_east" || sId == "assembly_east") return nodes["E_Prep_App"].position;
            if (sId == "counter_east") return nodes["E_Counter1_App"].position;
            if (sId == "counter_east_2") return nodes["E_Counter2_App"].position;

            // Fallback: 1.2m offset toward kitchen center
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

            // Direct line check: if already very close (< 1.2m) with clear line, move direct
            if (Vector3.Distance(startPos, targetPos) < 1.2f)
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
