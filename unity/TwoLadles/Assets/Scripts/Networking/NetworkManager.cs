using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;

namespace TwoLadles.Networking
{
    [Serializable]
    public class PlayerLobbyInfo
    {
        public int peerId;
        public string name;
        public string character; // "linh" or "jiwon"
        public string specialization; // "prep_master", "flame_keeper", etc.
        public bool ready;
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; set; }

        public const int DEFAULT_PORT = 24567;
        public const int MAX_CLIENTS = 1; // Exactly 2 players total

        public event Action<string, bool> OnConnectionStatusChanged;
        public event Action<Dictionary<int, PlayerLobbyInfo>> OnLobbyUpdated;
        public event Action<string> OnReturnedToMenu;

        public bool isLanActive = false;
        public int localPeerId = 1;
        public Dictionary<int, PlayerLobbyInfo> players = new Dictionary<int, PlayerLobbyInfo>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool IsHost()
        {
            if (!isLanActive) return true;
            return localPeerId == 1;
        }

        public bool HostGame(int port = DEFAULT_PORT, string playerName = "Host Chef")
        {
            CloseConnection();
            localPeerId = 1;
            isLanActive = true;

            players.Clear();
            players[1] = new PlayerLobbyInfo
            {
                peerId = 1,
                name = playerName,
                character = "linh",
                specialization = "prep_master",
                ready = false
            };

            OnConnectionStatusChanged?.Invoke($"Hosting on UDP port {port}. Waiting for partner...", false);
            OnLobbyUpdated?.Invoke(players);
            return true;
        }

        public bool JoinGame(string ipAddress, int port = DEFAULT_PORT, string playerName = "Partner Chef")
        {
            CloseConnection();
            string cleanIp = ipAddress.Trim();
            if (string.IsNullOrEmpty(cleanIp))
            {
                OnConnectionStatusChanged?.Invoke("Please enter a valid host IPv4 address.", true);
                return false;
            }

            localPeerId = 2;
            isLanActive = true;

            players.Clear();
            players[2] = new PlayerLobbyInfo
            {
                peerId = 2,
                name = playerName,
                character = "jiwon",
                specialization = "flame_keeper",
                ready = false
            };

            OnConnectionStatusChanged?.Invoke($"Connecting to {cleanIp}:{port}...", false);
            OnLobbyUpdated?.Invoke(players);
            return true;
        }

        public void CloseConnection()
        {
            isLanActive = false;
            players.Clear();
            OnLobbyUpdated?.Invoke(players);
        }

        public (bool ok, string reason) ApplyPlayerSelection(int peerId, string characterId, string specializationId)
        {
            if (!players.ContainsKey(peerId))
                return (false, "Unknown player peer ID.");

            // Exclusivity check: partner cannot pick the same character
            foreach (var kvp in players)
            {
                if (kvp.Key != peerId && kvp.Value.character == characterId)
                {
                    return (false, $"{characterId.ToUpper()} is already selected by your partner! Choose the other chef.");
                }
            }

            var p = players[peerId];
            bool changed = (p.character != characterId) || (p.specialization != specializationId);
            p.character = characterId;
            p.specialization = specializationId;

            // Invariant: Changing character or perks resets Ready state
            if (changed)
            {
                p.ready = false;
            }

            OnLobbyUpdated?.Invoke(players);
            return (true, "");
        }

        public (bool ok, string reason) SetPlayerReady(int peerId, bool isReady)
        {
            if (!players.ContainsKey(peerId))
                return (false, "Unknown player peer ID.");

            players[peerId].ready = isReady;
            OnLobbyUpdated?.Invoke(players);
            return (true, "");
        }

        public (bool ok, string reason) CanStartShift(bool requireTwoPlayers = true)
        {
            if (requireTwoPlayers && players.Count < 2)
                return (false, "Two connected human players are required to start a shift.");

            var seenChars = new HashSet<string>();
            foreach (var p in players.Values)
            {
                if (!p.ready)
                    return (false, $"{p.name} is not Ready yet.");

                if (seenChars.Contains(p.character))
                    return (false, "Linh and Jiwon must both be represented (no duplicate characters).");

                seenChars.Add(p.character);
            }

            return (true, "");
        }
    }
}
