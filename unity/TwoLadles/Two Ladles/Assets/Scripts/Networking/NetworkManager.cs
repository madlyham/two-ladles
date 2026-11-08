using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

    [Serializable]
    public class LanRoomInfo
    {
        public string roomKey;
        public string roomName;
        public string hostName;
        public string hostIp;
        public int port;
        public int playerCount;
        public float lastSeenRealtime;
    }

    /// <summary>
    /// Zero-dependency UDP network manager with Terraria-style LAN Room Auto-Discovery.
    /// Operates over UDP port 24567 (gameplay) and 24568 (LAN broadcast beacon).
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; set; }

        public const int DEFAULT_PORT = 24567;
        public const int DISCOVERY_PORT = 24568;
        public const int MAX_CLIENTS = 1; // 2 players total

        public event Action<string, bool> OnConnectionStatusChanged;
        public event Action<Dictionary<int, PlayerLobbyInfo>> OnLobbyUpdated;
        public event Action<string> OnReturnedToMenu;
        public event Action<int, GameMode> OnShiftStartReceived;
        public event Action<List<LanRoomInfo>> OnLanRoomsUpdated;

        public bool isLanActive = false;
        public int localPeerId = 1;
        public string currentRoomName = "Linh & Jiwon's Kitchen";
        public Dictionary<int, PlayerLobbyInfo> players = new Dictionary<int, PlayerLobbyInfo>();
        public Dictionary<string, LanRoomInfo> discoveredRooms = new Dictionary<string, LanRoomInfo>();

        private UdpClient udpSocket = null;
        private Thread receiveThread = null;
        private bool isRunning = false;
        private IPEndPoint partnerEndPoint = null;

        // LAN Discovery socket & thread
        private UdpClient discoverySocket = null;
        private Thread discoveryThread = null;
        private bool isScanningLan = false;
        private float beaconBroadcastTimer = 0.0f;
        private float scanPruneTimer = 0.0f;

        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
        private readonly object queueLock = new object();

        private float posSyncTimer = 0.0f;
        private const float POS_SYNC_INTERVAL = 0.033f; // 30Hz network sync

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

        private void Start()
        {
            StartLanDiscovery();
        }

        private void Update()
        {
            // 1. Process queued network events on main Unity thread
            lock (queueLock)
            {
                while (mainThreadQueue.Count > 0)
                {
                    mainThreadQueue.Dequeue()?.Invoke();
                }
            }

            // 2. Host broadcasts LAN Room Beacon every 1.2s; Scanner sends LAN_DISCOVER probe
            beaconBroadcastTimer += Time.unscaledDeltaTime;
            if (beaconBroadcastTimer >= 1.2f)
            {
                beaconBroadcastTimer = 0.0f;
                if (isLanActive && IsHost())
                {
                    BroadcastHostBeacon();
                }
                else if (isScanningLan && !isLanActive)
                {
                    SendLanDiscoveryProbe();
                }
            }

            // 3. Prune stale LAN rooms older than 5 seconds
            scanPruneTimer += Time.unscaledDeltaTime;
            if (scanPruneTimer >= 2.0f)
            {
                scanPruneTimer = 0.0f;
                PruneStaleLanRooms();
            }

            // 4. Transmit local player position at 30Hz
            if (isLanActive && partnerEndPoint != null && Kitchen.KitchenController.Instance != null)
            {
                posSyncTimer += Time.deltaTime;
                if (posSyncTimer >= POS_SYNC_INTERVAL)
                {
                    posSyncTimer = 0.0f;
                    BroadcastLocalPlayerTransform();
                }
            }
        }

        private void OnDestroy()
        {
            StopLanDiscovery();
            CloseConnection();
        }

        private void OnApplicationQuit()
        {
            StopLanDiscovery();
            CloseConnection();
        }

        public void StartLanDiscovery()
        {
            if (isScanningLan) return;
            try
            {
                discoverySocket = new UdpClient();
                discoverySocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                discoverySocket.EnableBroadcast = true;
                discoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));
                isScanningLan = true;
                discoveryThread = new Thread(DiscoveryReceiveLoop) { IsBackground = true };
                discoveryThread.Start();
                SendLanDiscoveryProbe();
            }
            catch
            {
                // Fallback if port 24568 is already bound on same machine: bind ephemeral port for active probing
                try
                {
                    discoverySocket = new UdpClient(0);
                    discoverySocket.EnableBroadcast = true;
                    isScanningLan = true;
                    discoveryThread = new Thread(DiscoveryReceiveLoop) { IsBackground = true };
                    discoveryThread.Start();
                    SendLanDiscoveryProbe();
                }
                catch { }
            }
        }

        public void StopLanDiscovery()
        {
            isScanningLan = false;
            if (discoverySocket != null)
            {
                try { discoverySocket.Close(); } catch { }
                discoverySocket = null;
            }
            if (discoveryThread != null)
            {
                try { discoveryThread.Abort(); } catch { }
                discoveryThread = null;
            }
        }

        private void DiscoveryReceiveLoop()
        {
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (isScanningLan && discoverySocket != null)
            {
                try
                {
                    byte[] data = discoverySocket.Receive(ref remoteEp);
                    if (data != null && data.Length > 0)
                    {
                        string msg = Encoding.UTF8.GetString(data);
                        string senderIp = remoteEp.Address.ToString();
                        lock (queueLock)
                        {
                            mainThreadQueue.Enqueue(() => ProcessDiscoveryPacket(msg, senderIp));
                        }
                    }
                }
                catch (SocketException)
                {
                    break;
                }
                catch
                {
                    break;
                }
            }
        }

        private void ProcessDiscoveryPacket(string msg, string senderIp)
        {
            if (string.IsNullOrEmpty(msg)) return;
            string[] parts = msg.Split('|');
            if (parts.Length >= 5 && parts[0] == "LAN_BEACON")
            {
                string rName = parts[1];
                string hName = parts[2];
                int port = int.TryParse(parts[3], out int p) ? p : DEFAULT_PORT;
                int pCount = int.TryParse(parts[4], out int c) ? c : 1;

                string key = $"{senderIp}:{port}";
                discoveredRooms[key] = new LanRoomInfo
                {
                    roomKey = key,
                    roomName = rName,
                    hostName = hName,
                    hostIp = senderIp,
                    port = port,
                    playerCount = pCount,
                    lastSeenRealtime = Time.realtimeSinceStartup
                };
                NotifyLanRoomsUpdated();
            }
        }

        private void PruneStaleLanRooms()
        {
            if (discoveredRooms.Count == 0) return;
            float now = Time.realtimeSinceStartup;
            List<string> staleKeys = null;
            foreach (var kvp in discoveredRooms)
            {
                if (now - kvp.Value.lastSeenRealtime > 5.0f)
                {
                    if (staleKeys == null) staleKeys = new List<string>();
                    staleKeys.Add(kvp.Key);
                }
            }
            if (staleKeys != null)
            {
                foreach (var k in staleKeys) discoveredRooms.Remove(k);
                NotifyLanRoomsUpdated();
            }
        }

        private void NotifyLanRoomsUpdated()
        {
            var list = new List<LanRoomInfo>(discoveredRooms.Values);
            OnLanRoomsUpdated?.Invoke(list);
        }

        public void SendLanDiscoveryProbe()
        {
            if (discoverySocket == null) return;
            try
            {
                byte[] probe = Encoding.UTF8.GetBytes("LAN_DISCOVER");
                discoverySocket.Send(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, DEFAULT_PORT));
                discoverySocket.Send(probe, probe.Length, new IPEndPoint(IPAddress.Loopback, DEFAULT_PORT));
            }
            catch { }
        }

        private void BroadcastHostBeacon()
        {
            if (udpSocket == null || !IsHost()) return;
            try
            {
                string hostName = players.TryGetValue(1, out var p) ? p.name : "Host Chef";
                string cleanRoom = string.IsNullOrEmpty(currentRoomName) ? $"{hostName}'s Kitchen" : currentRoomName.Replace("|", " ");
                string beacon = $"LAN_BEACON|{cleanRoom}|{hostName}|{DEFAULT_PORT}|{players.Count}";
                byte[] bytes = Encoding.UTF8.GetBytes(beacon);
                udpSocket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT));
                udpSocket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, DISCOVERY_PORT));
            }
            catch { }
        }

        public bool IsHost()
        {
            if (!isLanActive) return true;
            return localPeerId == 1;
        }

        public bool HostGame(int port = DEFAULT_PORT, string playerName = "Host Chef", string roomName = "Two Ladles Bistro")
        {
            CloseConnection();
            localPeerId = 1;
            isLanActive = true;
            currentRoomName = string.IsNullOrEmpty(roomName) ? $"{playerName}'s Kitchen" : roomName;

            players.Clear();
            players[1] = new PlayerLobbyInfo
            {
                peerId = 1,
                name = playerName,
                character = "linh",
                specialization = "prep_master",
                ready = true
            };

            try
            {
                udpSocket = new UdpClient(port);
                udpSocket.EnableBroadcast = true;
                isRunning = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                BroadcastHostBeacon();
                OnConnectionStatusChanged?.Invoke($"Hosting Room '{currentRoomName}' on LAN! Waiting for partner...", false);
                OnLobbyUpdated?.Invoke(players);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] HostGame failed: {ex.Message}");
                OnConnectionStatusChanged?.Invoke($"Room already active or port busy.", true);
                CloseConnection();
                return false;
            }
        }

        public bool JoinDiscoveredRoom(LanRoomInfo room, string playerName = "Partner Chef")
        {
            if (room == null) return false;
            return JoinGame(room.hostIp, room.port, playerName);
        }

        public bool AutoJoinLanRoom(string playerName = "Partner Chef")
        {
            foreach (var room in discoveredRooms.Values)
            {
                return JoinGame(room.hostIp, room.port, playerName);
            }
            return JoinGame("127.0.0.1", DEFAULT_PORT, playerName);
        }

        public bool JoinGame(string ipAddress, int port = DEFAULT_PORT, string playerName = "Partner Chef")
        {
            CloseConnection();
            string cleanIp = ipAddress.Trim();
            if (string.IsNullOrEmpty(cleanIp))
            {
                cleanIp = "127.0.0.1";
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
                ready = true
            };

            try
            {
                udpSocket = new UdpClient(0);
                udpSocket.EnableBroadcast = true;
                partnerEndPoint = new IPEndPoint(IPAddress.Parse(cleanIp), port);
                isRunning = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                SendPacket($"HELLO|2|{playerName}|jiwon|flame_keeper|1", partnerEndPoint);

                OnConnectionStatusChanged?.Invoke($"Joining Room at {cleanIp}...", false);
                OnLobbyUpdated?.Invoke(players);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] JoinGame failed: {ex.Message}");
                OnConnectionStatusChanged?.Invoke($"Connection failed: {ex.Message}", true);
                CloseConnection();
                return false;
            }
        }

        public void CloseConnection(string reason = null)
        {
            isRunning = false;
            if (udpSocket != null)
            {
                try { udpSocket.Close(); } catch { }
                udpSocket = null;
            }
            if (receiveThread != null)
            {
                try { receiveThread.Abort(); } catch { }
                receiveThread = null;
            }
            partnerEndPoint = null;
            isLanActive = false;
            players.Clear();
            OnLobbyUpdated?.Invoke(players);

            if (!string.IsNullOrEmpty(reason))
            {
                OnReturnedToMenu?.Invoke(reason);
            }
        }

        public void ReturnToMenu(string reason = "Returned to lobby.")
        {
            if (partnerEndPoint != null)
            {
                SendPacket($"DISCONNECT|{reason}", partnerEndPoint);
            }
            CloseConnection(reason);
        }

        private void SendPacket(string message, IPEndPoint target)
        {
            if (udpSocket == null || target == null) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                udpSocket.Send(data, data.Length, target);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkManager] SendPacket error: {ex.Message}");
            }
        }

        private void ReceiveLoop()
        {
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (isRunning && udpSocket != null)
            {
                try
                {
                    byte[] data = udpSocket.Receive(ref remoteEp);
                    if (data != null && data.Length > 0)
                    {
                        string msg = Encoding.UTF8.GetString(data);
                        IPEndPoint senderCopy = new IPEndPoint(remoteEp.Address, remoteEp.Port);
                        lock (queueLock)
                        {
                            mainThreadQueue.Enqueue(() => HandleIncomingMessage(msg, senderCopy));
                        }
                    }
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkManager] Receive error: {ex.Message}");
                    break;
                }
            }
        }

        private void HandleIncomingMessage(string msg, IPEndPoint senderEp)
        {
            string[] parts = msg.Split('|');
            if (parts.Length == 0) return;

            string cmd = parts[0];

            switch (cmd)
            {
                case "LAN_DISCOVER":
                    if (IsHost() && isLanActive)
                    {
                        string hostName = players.TryGetValue(1, out var p) ? p.name : "Host Chef";
                        string cleanRoom = string.IsNullOrEmpty(currentRoomName) ? $"{hostName}'s Kitchen" : currentRoomName.Replace("|", " ");
                        string beacon = $"LAN_BEACON|{cleanRoom}|{hostName}|{DEFAULT_PORT}|{players.Count}";
                        SendPacket(beacon, senderEp);
                        if (discoverySocket != null)
                        {
                            try
                            {
                                byte[] b = Encoding.UTF8.GetBytes(beacon);
                                discoverySocket.Send(b, b.Length, new IPEndPoint(senderEp.Address, DISCOVERY_PORT));
                            }
                            catch { }
                        }
                    }
                    break;

                case "LAN_BEACON":
                    ProcessDiscoveryPacket(msg, senderEp.Address.ToString());
                    break;

                case "DISCONNECT":
                    string disconnectReason = parts.Length > 1 ? parts[1] : "Partner left the game.";
                    OnConnectionStatusChanged?.Invoke(disconnectReason, true);
                    CloseConnection(disconnectReason);
                    break;

                case "HELLO":
                    if (IsHost() && parts.Length >= 6)
                    {
                        partnerEndPoint = senderEp;
                        int pId = int.Parse(parts[1]);
                        players[pId] = new PlayerLobbyInfo
                        {
                            peerId = pId,
                            name = parts[2],
                            character = parts[3],
                            specialization = parts[4],
                            ready = parts[5] == "1"
                        };
                        OnConnectionStatusChanged?.Invoke($"{parts[2]} joined '{currentRoomName}'!", false);
                        OnLobbyUpdated?.Invoke(players);
                        BroadcastLobbyState();
                    }
                    break;

                case "LOBBY_STATE":
                    if (!IsHost() && parts.Length >= 2)
                    {
                        partnerEndPoint = senderEp;
                        int count = int.Parse(parts[1]);
                        players.Clear();
                        for (int i = 0; i < count; i++)
                        {
                            if (parts.Length > 2 + i)
                            {
                                string[] sub = parts[2 + i].Split(';');
                                if (sub.Length >= 5)
                                {
                                    int pid = int.Parse(sub[0]);
                                    players[pid] = new PlayerLobbyInfo
                                    {
                                        peerId = pid,
                                        name = sub[1],
                                        character = sub[2],
                                        specialization = sub[3],
                                        ready = sub[4] == "1"
                                    };
                                }
                            }
                        }
                        OnConnectionStatusChanged?.Invoke("Connected to Room!", false);
                        OnLobbyUpdated?.Invoke(players);
                    }
                    break;

                case "LOBBY_SELECT":
                    if (IsHost() && parts.Length >= 4)
                    {
                        int pId = int.Parse(parts[1]);
                        ApplyPlayerSelection(pId, parts[2], parts[3]);
                        BroadcastLobbyState();
                    }
                    break;

                case "LOBBY_READY":
                    if (IsHost() && parts.Length >= 3)
                    {
                        int pId = int.Parse(parts[1]);
                        bool rdy = parts[2] == "1";
                        SetPlayerReady(pId, rdy);
                        BroadcastLobbyState();
                    }
                    break;

                case "START_SHIFT":
                    if (!IsHost() && parts.Length >= 3)
                    {
                        int stage = int.Parse(parts[1]);
                        GameMode mode = (GameMode)Enum.Parse(typeof(GameMode), parts[2]);
                        OnShiftStartReceived?.Invoke(stage, mode);
                        if (ShiftManager.Instance != null)
                        {
                            ShiftManager.Instance.StartShift(stage, mode);
                        }
                    }
                    break;

                case "POS":
                    if (parts.Length >= 9 && Kitchen.KitchenController.Instance != null)
                    {
                        int pId = int.Parse(parts[1]);
                        if (pId != localPeerId && Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(pId, out var remoteChef))
                        {
                            float x = float.Parse(parts[2]);
                            float y = float.Parse(parts[3]);
                            float z = float.Parse(parts[4]);
                            float fx = float.Parse(parts[5]);
                            float fy = float.Parse(parts[6]);
                            float wAnim = float.Parse(parts[7]);
                            string carried = parts[8];

                            remoteChef.transform.position = new Vector3(x, y, z);
                            remoteChef.facingDirection = new Vector2(fx, fy);
                            remoteChef.walkAnimTime = wAnim;

                            if (string.IsNullOrEmpty(carried) || carried == "none")
                            {
                                if (remoteChef.carriedItem != null) remoteChef.ClearCarriedItem();
                            }
                            else
                            {
                                if (remoteChef.carriedItem == null || remoteChef.carriedItem.itemId != carried)
                                {
                                    remoteChef.SetCarriedItem(new KitchenItem(carried, Player.PlayerController.ResolveItemSpriteName(carried)));
                                }
                            }
                        }
                    }
                    break;

                case "INTERACT":
                    if (IsHost() && parts.Length >= 3 && Kitchen.KitchenController.Instance != null)
                    {
                        int pId = int.Parse(parts[1]);
                        bool isSec = parts[2] == "1";
                        if (Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(pId, out var chef))
                        {
                            if (isSec) chef.TryInteractSecondary();
                            else chef.TryInteractPrimary();
                        }
                    }
                    break;

                case "THROW":
                    if (parts.Length >= 4 && Kitchen.KitchenController.Instance != null)
                    {
                        int pId = int.Parse(parts[1]);
                        if (pId != localPeerId && Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(pId, out var chef))
                        {
                            float ax = float.Parse(parts[2]);
                            float ay = float.Parse(parts[3]);
                            chef.ThrowItem(new Vector2(ax, ay));
                        }
                    }
                    break;
            }
        }

        private void BroadcastLocalPlayerTransform()
        {
            if (Kitchen.KitchenController.Instance == null || partnerEndPoint == null) return;

            if (Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(localPeerId, out var localChef))
            {
                Vector3 pos = localChef.transform.position;
                Vector2 f = localChef.facingDirection;
                string item = localChef.carriedItem != null ? localChef.carriedItem.itemId : "none";
                string pkt = $"POS|{localPeerId}|{pos.x:F3}|{pos.y:F3}|{pos.z:F3}|{f.x:F2}|{f.y:F2}|{localChef.walkAnimTime:F2}|{item}";
                SendPacket(pkt, partnerEndPoint);
            }
        }

        public void SendClientInteract(bool isSecondary)
        {
            if (!IsHost() && partnerEndPoint != null)
            {
                SendPacket($"INTERACT|{localPeerId}|{(isSecondary ? 1 : 0)}", partnerEndPoint);
            }
        }

        public void SendClientThrow(Vector2 aim)
        {
            if (partnerEndPoint != null)
            {
                SendPacket($"THROW|{localPeerId}|{aim.x:F2}|{aim.y:F2}", partnerEndPoint);
            }
        }

        public void BroadcastLobbyState()
        {
            if (!IsHost() || partnerEndPoint == null) return;

            var sb = new StringBuilder();
            sb.Append($"LOBBY_STATE|{players.Count}");
            foreach (var p in players.Values)
            {
                sb.Append($"|{p.peerId};{p.name};{p.character};{p.specialization};{(p.ready ? 1 : 0)}");
            }
            SendPacket(sb.ToString(), partnerEndPoint);
        }

        public void BroadcastShiftStart(int stage, GameMode mode)
        {
            if (IsHost() && partnerEndPoint != null)
            {
                SendPacket($"START_SHIFT|{stage}|{mode}", partnerEndPoint);
            }
        }

        public (bool ok, string reason) ApplyPlayerSelection(int peerId, string characterId, string specializationId)
        {
            if (!players.ContainsKey(peerId))
                return (false, "Unknown player peer ID.");

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

            if (changed)
            {
                p.ready = false;
            }

            if (!IsHost() && partnerEndPoint != null)
            {
                SendPacket($"LOBBY_SELECT|{peerId}|{characterId}|{specializationId}", partnerEndPoint);
            }
            else
            {
                BroadcastLobbyState();
            }

            OnLobbyUpdated?.Invoke(players);
            return (true, "");
        }

        public (bool ok, string reason) SetPlayerReady(int peerId, bool isReady)
        {
            if (!players.ContainsKey(peerId))
                return (false, "Unknown player peer ID.");

            players[peerId].ready = isReady;

            if (!IsHost() && partnerEndPoint != null)
            {
                SendPacket($"LOBBY_READY|{peerId}|{(isReady ? 1 : 0)}", partnerEndPoint);
            }
            else
            {
                BroadcastLobbyState();
            }

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
