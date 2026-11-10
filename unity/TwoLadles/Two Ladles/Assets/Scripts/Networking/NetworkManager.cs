using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
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
        private float sweepTimer = 0.0f;
        private float sweepUntilRealtime = 0.0f; // sweep only runs for 60s after the Join screen opens
        private float helloRetryTimer = 0.0f;
        private int helloRetriesLeft = 0; // client resends HELLO every 0.5s until the host answers
        private const float SWEEP_INTERVAL = 3.0f; // unicast Wi-Fi sweep (works even when router drops broadcasts)

        // Cached Wi-Fi address of this device (never 127.0.0.1)
        private string localWifiIp = "";
        private string localWifiMask = "255.255.255.0";
        private float localIpRefreshTimer = 999f;
        public string LocalWifiIp { get { RefreshLocalWifiIp(false); return localWifiIp; } }

        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
        private readonly object queueLock = new object();

        private float posSyncTimer = 0.0f;
        private const float POS_SYNC_INTERVAL = 0.033f; // 30Hz network sync
        private float orderSyncTimer = 0.0f;
        private const float ORDER_SYNC_INTERVAL = 0.25f; // 4Hz host -> client order/score sync

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

            // 2b. Joiner sweeps every address on the Wi-Fi subnet (unicast) every 3s
            if (isScanningLan && !isLanActive && Time.realtimeSinceStartup < sweepUntilRealtime)
            {
                sweepTimer += Time.unscaledDeltaTime;
                if (sweepTimer >= SWEEP_INTERVAL)
                {
                    sweepTimer = 0.0f;
                    SendSubnetSweep();
                }
            }

            // 2c. Client resends HELLO until the host replies (UDP may drop the first one)
            if (helloRetriesLeft > 0 && isLanActive && !IsHost() && partnerEndPoint != null)
            {
                helloRetryTimer += Time.unscaledDeltaTime;
                if (helloRetryTimer >= 0.5f)
                {
                    helloRetryTimer = 0f;
                    helloRetriesLeft--;
                    if (players.TryGetValue(2, out var me)) SendPacket(BuildHelloPacket(me.name), partnerEndPoint);
                    if (helloRetriesLeft == 0)
                    {
                        CloseConnection();
                        OnConnectionStatusChanged?.Invoke("Host did not answer. Same Wi-Fi? Host allowed the game in Windows Firewall?", true);
                    }
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

            // 5. Host sends authoritative orders / score / flow to the client at 4Hz
            if (isLanActive && IsHost() && partnerEndPoint != null && ShiftManager.Instance != null && ShiftManager.Instance.shiftActive)
            {
                orderSyncTimer += Time.unscaledDeltaTime;
                if (orderSyncTimer >= ORDER_SYNC_INTERVAL)
                {
                    orderSyncTimer = 0.0f;
                    SendPacket(ShiftManager.EncodeNetworkState(ShiftManager.Instance.CaptureNetworkState()), partnerEndPoint);
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
            RefreshLocalWifiIp(false);
            byte[] probe = Encoding.UTF8.GetBytes("LAN_DISCOVER");
            foreach (var target in GetBroadcastTargets())
            {
                try { discoverySocket.Send(probe, probe.Length, new IPEndPoint(target, DEFAULT_PORT)); } catch { }
            }
        }

        /// <summary>
        /// Opens the Join screen: refresh this device's Wi-Fi address and probe the whole subnet right now.
        /// </summary>
        public void ScanWifiNow()
        {
            if (!isScanningLan) StartLanDiscovery();
            RefreshLocalWifiIp(true);
            SendLanDiscoveryProbe();
            SendSubnetSweep();
            sweepTimer = 0.0f;
            sweepUntilRealtime = Time.realtimeSinceStartup + 60f;
        }

        /// <summary>
        /// Unicast LAN_DISCOVER to every address on the /24 Wi-Fi subnet. Works on routers that block broadcast.
        /// 253 tiny packets (~3 KB) every 3 seconds while the Join screen is open.
        /// </summary>
        private void SendSubnetSweep()
        {
            if (discoverySocket == null) return;
            RefreshLocalWifiIp(false);
            var targets = BuildSubnetSweepTargets(localWifiIp);
            if (targets.Count == 0) return;
            byte[] probe = Encoding.UTF8.GetBytes("LAN_DISCOVER");
            foreach (var ip in targets)
            {
                try { discoverySocket.Send(probe, probe.Length, new IPEndPoint(IPAddress.Parse(ip), DEFAULT_PORT)); } catch { }
            }
        }

        private List<IPAddress> GetBroadcastTargets()
        {
            var list = new List<IPAddress> { IPAddress.Broadcast, IPAddress.Loopback };
            string subnetBc = GetSubnetBroadcast(localWifiIp, localWifiMask);
            if (!string.IsNullOrEmpty(subnetBc) && IPAddress.TryParse(subnetBc, out var bc)) list.Insert(0, bc);
            return list;
        }

        private void BroadcastHostBeacon()
        {
            if (udpSocket == null || !IsHost()) return;
            RefreshLocalWifiIp(false);
            byte[] bytes = Encoding.UTF8.GetBytes(BuildBeacon());
            foreach (var target in GetBroadcastTargets())
            {
                try { udpSocket.Send(bytes, bytes.Length, new IPEndPoint(target, DISCOVERY_PORT)); } catch { }
            }
        }

        private string BuildBeacon()
        {
            string hostName = players.TryGetValue(1, out var p) ? p.name : "Host Chef";
            string cleanRoom = string.IsNullOrEmpty(currentRoomName) ? $"{hostName}'s Kitchen" : currentRoomName;
            return $"LAN_BEACON|{CleanField(cleanRoom)}|{CleanField(hostName)}|{DEFAULT_PORT}|{players.Count}";
        }

        // ---------- Wi-Fi address helpers (pure functions are unit tested in Suite 19) ----------

        /// <summary>Strips protocol separators from player-typed text.</summary>
        public static string CleanField(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Chef";
            string c = s.Replace("|", " ").Replace(";", " ").Trim();
            if (c.Length > 24) c = c.Substring(0, 24);
            return c.Length == 0 ? "Chef" : c;
        }

        /// <summary>True for a real IPv4 Wi-Fi/LAN address (not localhost, not link-local 169.254.x.x).</summary>
        public static bool IsUsableLanAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            string[] parts = ip.Trim().Split('.');
            if (parts.Length != 4) return false;
            var b = new int[4];
            for (int i = 0; i < 4; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out b[i]) || b[i] < 0 || b[i] > 255) return false;
            }
            if (b[0] == 127 || b[0] == 0 || b[0] >= 224) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            return true;
        }

        /// <summary>192.168.1.37 + 255.255.255.0 -> 192.168.1.255. Returns null on bad input.</summary>
        public static string GetSubnetBroadcast(string ip, string mask)
        {
            if (!IsUsableLanAddress(ip) || string.IsNullOrEmpty(mask)) return null;
            if (!IPAddress.TryParse(mask, out var m) || m.AddressFamily != AddressFamily.InterNetwork) return null;
            byte[] a = IPAddress.Parse(ip.Trim()).GetAddressBytes();
            byte[] mb = m.GetAddressBytes();
            var r = new byte[4];
            for (int i = 0; i < 4; i++) r[i] = (byte)(a[i] | (~mb[i] & 0xFF));
            return new IPAddress(r).ToString();
        }

        /// <summary>"192.168.1.37" -> "192.168.1." ; unusable address -> "".</summary>
        public static string GetSubnetPrefix(string ip)
        {
            if (!IsUsableLanAddress(ip)) return "";
            string t = ip.Trim();
            return t.Substring(0, t.LastIndexOf('.') + 1);
        }

        /// <summary>Every host .1-.254 on the /24 subnet of localIp, excluding localIp itself.</summary>
        public static List<string> BuildSubnetSweepTargets(string localIp)
        {
            var list = new List<string>();
            string prefix = GetSubnetPrefix(localIp);
            if (prefix.Length == 0) return list;
            string self = localIp.Trim();
            for (int i = 1; i <= 254; i++)
            {
                string ip = prefix + i.ToString(CultureInfo.InvariantCulture);
                if (ip != self) list.Add(ip);
            }
            return list;
        }

        /// <summary>
        /// Finds this device's Wi-Fi (or Ethernet) IPv4 address + mask. Skips VPN / virtual adapters.
        /// </summary>
        public static bool TryGetLocalWifiIPv4(out string ip, out string mask)
        {
            ip = ""; mask = "255.255.255.0";
            int bestScore = -1;
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    string n = ((ni.Name ?? "") + " " + (ni.Description ?? "")).ToLowerInvariant();
                    if (n.Contains("virtual") || n.Contains("vethernet") || n.Contains("vmware") || n.Contains("hyper-v") || n.Contains("vpn") || n.Contains("virtualbox") || n.Contains("tap-windows") || n.Contains("wsl") || n.Contains("bluetooth")) continue;

                    int score = ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || n.Contains("wlan") || n.Contains("wi-fi") || n.Contains("wifi") ? 3
                              : ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 2 : 1;
                    if (score <= bestScore) continue;

                    IPInterfaceProperties props;
                    try { props = ni.GetIPProperties(); } catch { continue; }
                    foreach (var ua in props.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        string candidate = ua.Address.ToString();
                        if (!IsUsableLanAddress(candidate)) continue;
                        string m = "255.255.255.0";
                        try { if (ua.IPv4Mask != null && ua.IPv4Mask.ToString() != "0.0.0.0") m = ua.IPv4Mask.ToString(); } catch { }
                        ip = candidate; mask = m; bestScore = score;
                        break;
                    }
                }
            }
            catch { }

            if (bestScore < 0)
            {
                // Fallback: ask the OS which local address would route outward (no packet is actually sent).
                try
                {
                    using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        s.Connect("8.8.8.8", 65530);
                        string candidate = ((IPEndPoint)s.LocalEndPoint).Address.ToString();
                        if (IsUsableLanAddress(candidate)) { ip = candidate; bestScore = 0; }
                    }
                }
                catch { }
            }
            return bestScore >= 0;
        }

        private void RefreshLocalWifiIp(bool force)
        {
            if (!force && Time.realtimeSinceStartup - localIpRefreshTimer < 10f && localIpRefreshTimer < 900f) return;
            localIpRefreshTimer = Time.realtimeSinceStartup;
            if (TryGetLocalWifiIPv4(out var ip, out var mask))
            {
                localWifiIp = ip;
                localWifiMask = mask;
            }
            else
            {
                localWifiIp = "";
            }
        }

        // ---------- Lobby helpers ----------

        public static PlayerLobbyInfo CreateHostPlayer(string playerName)
        {
            return new PlayerLobbyInfo { peerId = 1, name = CleanField(playerName), character = "linh", specialization = "prep_master", ready = false };
        }

        public static PlayerLobbyInfo CreateClientPlayer(string playerName)
        {
            return new PlayerLobbyInfo { peerId = 2, name = CleanField(playerName), character = "jiwon", specialization = "flame_keeper", ready = false };
        }

        public static string BuildHelloPacket(string playerName)
        {
            return $"HELLO|2|{CleanField(playerName)}|jiwon|flame_keeper|0";
        }

        /// <summary>Lobby hint shown to both chefs. Empty when the host can press Start.</summary>
        public string GetLobbyWaitReason()
        {
            if (players.Count < 2) return $"Waiting for partner to join (1/2)...";
            var check = CanStartShift(true);
            return check.ok ? "" : check.reason;
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
            players[1] = CreateHostPlayer(playerName);

            try
            {
                udpSocket = new UdpClient(port);
                udpSocket.EnableBroadcast = true;
                isRunning = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                RefreshLocalWifiIp(true);
                BroadcastHostBeacon();
                string where = string.IsNullOrEmpty(localWifiIp) ? "(no Wi-Fi found - connect to Wi-Fi!)" : $"on Wi-Fi {localWifiIp}";
                OnConnectionStatusChanged?.Invoke($"Hosting '{currentRoomName}' {where}. Waiting for partner (1/2)...", string.IsNullOrEmpty(localWifiIp));
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
            OnConnectionStatusChanged?.Invoke("No kitchen found on your Wi-Fi yet. Ask your partner to Host first, then try again.", true);
            return false;
        }

        public bool JoinGame(string ipAddress, int port = DEFAULT_PORT, string playerName = "Partner Chef")
        {
            string cleanIp = (ipAddress ?? "").Trim();
            if (!IsUsableLanAddress(cleanIp))
            {
                OnConnectionStatusChanged?.Invoke("Enter the host's Wi-Fi IP (shown on the host's screen, e.g. 192.168.1.23).", true);
                return false;
            }
            CloseConnection();

            localPeerId = 2;
            isLanActive = true;

            players.Clear();
            players[2] = CreateClientPlayer(playerName);

            try
            {
                udpSocket = new UdpClient(0);
                udpSocket.EnableBroadcast = true;
                partnerEndPoint = new IPEndPoint(IPAddress.Parse(cleanIp), port);
                isRunning = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                SendPacket(BuildHelloPacket(playerName), partnerEndPoint);
                helloRetryTimer = 0f;
                helloRetriesLeft = 10;

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
            if (ShiftManager.Instance != null) ShiftManager.Instance.isNetworkClient = false;
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
                        string beacon = BuildBeacon();
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
                    if (IsHost() && isLanActive && parts.Length >= 6)
                    {
                        // Room holds exactly 2 chefs: ignore a third device once a partner is connected.
                        if (partnerEndPoint != null && !partnerEndPoint.Equals(senderEp) && players.ContainsKey(2)) break;
                        bool isRetry = players.ContainsKey(2) && partnerEndPoint != null && partnerEndPoint.Equals(senderEp);
                        partnerEndPoint = senderEp;
                        if (!isRetry)
                        {
                            players[2] = CreateClientPlayer(parts[2]);
                            OnConnectionStatusChanged?.Invoke($"{players[2].name} joined '{currentRoomName}'! Both chefs press READY.", false);
                        }
                        OnLobbyUpdated?.Invoke(players);
                        BroadcastLobbyState();
                    }
                    break;

                case "LOBBY_STATE":
                    if (!IsHost() && parts.Length >= 2)
                    {
                        helloRetriesLeft = 0;
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
                            ShiftManager.Instance.isNetworkClient = true; // host owns orders/score from here on
                        }
                    }
                    break;

                case ShiftManager.ORDERS_PACKET:
                    if (!IsHost() && ShiftManager.Instance != null && ShiftManager.Instance.isNetworkClient)
                    {
                        var state = ShiftManager.DecodeNetworkState(msg);
                        if (state != null) ShiftManager.Instance.ApplyNetworkState(state);
                    }
                    break;

                case "POS":
                    if (parts.Length >= 9 && Kitchen.KitchenController.Instance != null)
                    {
                        int pId = int.Parse(parts[1], CultureInfo.InvariantCulture);
                        if (pId != localPeerId && Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(pId, out var remoteChef))
                        {
                            float x = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            float y = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            float z = float.Parse(parts[4], CultureInfo.InvariantCulture);
                            float fx = float.Parse(parts[5], CultureInfo.InvariantCulture);
                            float fy = float.Parse(parts[6], CultureInfo.InvariantCulture);
                            float wAnim = float.Parse(parts[7], CultureInfo.InvariantCulture);
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
                        int pId = int.Parse(parts[1], CultureInfo.InvariantCulture);
                        if (pId != localPeerId && Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(pId, out var chef))
                        {
                            float ax = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            float ay = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            chef.ThrowItem(new Vector2(ax, ay));
                        }
                    }
                    break;

                case "STORY_DONE":
                    // Host finished/skipped the start-of-day story phone; close ours too.
                    if (!IsHost())
                    {
                        UI.PhoneStoryUI.Close();
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
                string pkt = string.Format(CultureInfo.InvariantCulture, "POS|{0}|{1:F3}|{2:F3}|{3:F3}|{4:F2}|{5:F2}|{6:F2}|{7}",
                    localPeerId, pos.x, pos.y, pos.z, f.x, f.y, localChef.walkAnimTime, item);
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
                string pkt = string.Format(CultureInfo.InvariantCulture, "THROW|{0}|{1:F2}|{2:F2}", localPeerId, aim.x, aim.y);
                SendPacket(pkt, partnerEndPoint);
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

        public void BroadcastStoryDone()
        {
            if (IsHost() && partnerEndPoint != null)
            {
                SendPacket("STORY_DONE", partnerEndPoint);
            }
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
