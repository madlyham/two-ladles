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
using TwoLadles.Stations;

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
        private float stationSyncTimer = 0.0f;
        private const float STATION_SYNC_INTERVAL = 0.1f; // 10Hz host -> client station items + chef hands

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
                    var job = mainThreadQueue.Dequeue();
                    try { job?.Invoke(); }
                    catch (Exception ex) { Debug.LogWarning($"[NetworkManager] Dropped bad packet: {ex.Message}"); }
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

            // 6. Host sends authoritative station items, cook/chop progress, pass slots and chef hands at 10Hz
            if (isLanActive && IsHost() && partnerEndPoint != null && Kitchen.KitchenController.Instance != null
                && ShiftManager.Instance != null && ShiftManager.Instance.shiftActive)
            {
                stationSyncTimer += Time.unscaledDeltaTime;
                if (stationSyncTimer >= STATION_SYNC_INTERVAL)
                {
                    stationSyncTimer = 0.0f;
                    var kc = Kitchen.KitchenController.Instance;
                    SendPacket(EncodeStationSnapshot(kc.stations.Values, kc.playersByPeer), partnerEndPoint);
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

        /// <summary>
        /// Cleans an IP the player typed: trims, commas -> dots, drops ":port". Returns "" unless it is a usable LAN IPv4.
        /// </summary>
        public static string NormalizeTypedIp(string typed)
        {
            if (string.IsNullOrWhiteSpace(typed)) return "";
            string t = typed.Trim().Replace(',', '.').Replace(" ", "");
            int colon = t.IndexOf(':');
            if (colon >= 0) t = t.Substring(0, colon);
            return IsUsableLanAddress(t) ? t : "";
        }

        /// <summary>Every usable IPv4 on this device (physical adapters first). Shown on the host screen.</summary>
        public static List<string> GetAllUsableLanIPv4()
        {
            var list = new List<string>();
            if (TryGetLocalWifiIPv4(out var best, out _) && IsUsableLanAddress(best)) list.Add(best);
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    string n = ((ni.Name ?? "") + " " + (ni.Description ?? "")).ToLowerInvariant();
                    if (n.Contains("virtual") || n.Contains("vethernet") || n.Contains("vmware") || n.Contains("hyper-v") || n.Contains("vpn") || n.Contains("virtualbox") || n.Contains("wsl") || n.Contains("bluetooth")) continue;
                    IPInterfaceProperties props;
                    try { props = ni.GetIPProperties(); } catch { continue; }
                    foreach (var ua in props.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        string c = ua.Address.ToString();
                        if (IsUsableLanAddress(c) && !list.Contains(c)) list.Add(c);
                    }
                }
            }
            catch { }
            return list;
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
                        SendLocalUpgrades(); // host simulates our chef with our own tier + upgrades
                    }
                    break;

                case "UPGRADES":
                    // Both sides: partner's chef uses partner's own tier + upgrades.
                    // Host simulates it; client only uses it for the tier sprite. Host replies once with its own.
                    if (Kitchen.KitchenController.Instance != null && DecodeUpgrades(msg, out int upTier, out var upVals))
                    {
                        foreach (var kv in Kitchen.KitchenController.Instance.playersByPeer)
                        {
                            if (kv.Key != localPeerId) { ApplyUpgradesToChef(kv.Value, upTier, upVals); break; }
                        }
                        if (IsHost()) SendLocalUpgrades();
                    }
                    break;

                case "WORKER":
                    if (!IsHost() && Kitchen.KitchenController.Instance != null)
                    {
                        ApplyWorkerPacket(msg, Kitchen.KitchenController.Instance);
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

                            remoteChef.transform.position = new Vector3(x, y, z);
                            remoteChef.facingDirection = new Vector2(fx, fy);
                            remoteChef.walkAnimTime = wAnim;

                            // Held items are host-authoritative and arrive in STATIONS packets,
                            // so the carried-item field (parts[8]) is informational only.
                        }
                    }
                    break;

                case "STATIONS":
                    if (!IsHost() && Kitchen.KitchenController.Instance != null)
                    {
                        var kcs = Kitchen.KitchenController.Instance;
                        ApplyStationSnapshot(msg, kcs.stations, kcs.playersByPeer);
                    }
                    break;

                case "HIRE":
                    if (IsHost() && parts.Length >= 2 && Kitchen.KitchenController.Instance != null
                        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hirePid) && hirePid != localPeerId)
                    {
                        Kitchen.KitchenController.Instance.RequestHireAvailableCrew(hirePid);
                    }
                    break;

                case "INTERACT":
                    if (IsHost() && parts.Length >= 3 && Kitchen.KitchenController.Instance != null
                        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iPid) && iPid != localPeerId)
                    {
                        var kci = Kitchen.KitchenController.Instance;
                        bool isSec = parts[2] == "1";
                        if (kci.playersByPeer.TryGetValue(iPid, out var chef))
                        {
                            // Client names the station it aimed at; host still checks the 2.6m reach.
                            StationBase aimed = null;
                            if (parts.Length >= 4 && !string.IsNullOrEmpty(parts[3])) kci.stations.TryGetValue(parts[3], out aimed);
                            if (isSec) chef.TryInteractSecondary(aimed);
                            else chef.TryInteractPrimary(aimed);
                        }
                    }
                    break;

                case "THROW":
                    if (IsHost() && parts.Length >= 4 && Kitchen.KitchenController.Instance != null
                        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tPid) && tPid != localPeerId
                        && Kitchen.KitchenController.Instance.playersByPeer.TryGetValue(tPid, out var thrower)
                        && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float ax)
                        && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float ay))
                    {
                        thrower.ThrowItem(new Vector2(ax, ay));
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

        public void SendClientInteract(bool isSecondary, string stationId = "")
        {
            if (!IsHost() && partnerEndPoint != null)
            {
                SendPacket($"INTERACT|{localPeerId}|{(isSecondary ? 1 : 0)}|{(stationId ?? "").Replace("|", "")}", partnerEndPoint);
            }
        }

        public void SendClientHire()
        {
            if (!IsHost() && partnerEndPoint != null)
            {
                SendPacket($"HIRE|{localPeerId}", partnerEndPoint);
            }
        }

        // ---------------- Per-chef upgrades (UPGRADES packet, client -> host at shift start) ----------------
        // UPGRADES|tier|move_speed=1.2,cook_speed=1.5,...   Values clamped 0.5..3.0, tier 1..3.

        public static readonly string[] UPGRADE_KEYS = { "move_speed", "cook_speed", "stockpot_window", "prep_window" };

        public static string EncodeUpgrades(int tier, Dictionary<string, float> upgrades)
        {
            var sb = new StringBuilder();
            sb.Append("UPGRADES|").Append(Mathf.Clamp(tier, 1, 3).ToString(CultureInfo.InvariantCulture)).Append('|');
            bool first = true;
            if (upgrades != null)
            {
                foreach (var key in UPGRADE_KEYS)
                {
                    if (!upgrades.TryGetValue(key, out float v)) continue;
                    if (!first) sb.Append(',');
                    sb.Append(key).Append('=').Append(v.ToString("0.###", CultureInfo.InvariantCulture));
                    first = false;
                }
            }
            return sb.ToString();
        }

        public static bool DecodeUpgrades(string msg, out int tier, out Dictionary<string, float> upgrades)
        {
            tier = 1;
            upgrades = new Dictionary<string, float>();
            try
            {
                if (string.IsNullOrEmpty(msg) || !msg.StartsWith("UPGRADES|")) return false;
                string[] parts = msg.Split('|');
                if (parts.Length < 3) return false;
                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t)) return false;
                tier = Mathf.Clamp(t, 1, 3);
                foreach (string entry in parts[2].Split(','))
                {
                    int eq = entry.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = entry.Substring(0, eq).Trim();
                    if (Array.IndexOf(UPGRADE_KEYS, key) < 0) continue;
                    if (!float.TryParse(entry.Substring(eq + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) continue;
                    if (float.IsNaN(v) || float.IsInfinity(v)) continue;
                    upgrades[key] = Mathf.Clamp(v, 0.5f, 3.0f);
                }
                return true;
            }
            catch
            {
                tier = 1;
                upgrades = new Dictionary<string, float>();
                return false;
            }
        }

        public static bool ApplyUpgradesToChef(Player.PlayerController chef, int tier, Dictionary<string, float> upgrades)
        {
            if (chef == null) return false;
            chef.characterTier = Mathf.Clamp(tier, 1, 3);
            chef.upgradeOverrides = upgrades != null ? new Dictionary<string, float>(upgrades) : new Dictionary<string, float>();
            chef.RefreshVisuals();
            return true;
        }

        /// <summary>Sends this device's own tier + upgrade values to the partner (client at shift start, host in reply).</summary>
        public void SendLocalUpgrades()
        {
            if (!isLanActive || partnerEndPoint == null) return;
            var pm = ProgressionManager.Instance;
            int tier = 1;
            var kc = Kitchen.KitchenController.Instance;
            if (pm != null && kc != null && kc.playersByPeer.TryGetValue(localPeerId, out var me))
                tier = me.characterId == "jiwon" ? pm.jiwonTier : pm.linhTier;
            var up = new Dictionary<string, float>();
            foreach (var key in UPGRADE_KEYS)
                up[key] = pm != null ? pm.GetUpgradeValue(key, 1.0f) : 1.0f;
            SendPacket(EncodeUpgrades(tier, up), partnerEndPoint);
        }

        // ---------------- Part-timer mirror (WORKER packet, host -> client) ----------------
        // WORKER|wid|roleId|stationId

        public static string EncodeWorker(string wid, string roleId, string stationId)
        {
            return $"WORKER|{CleanSyncField(wid)}|{CleanSyncField(roleId)}|{CleanSyncField(stationId)}";
        }

        public void BroadcastWorker(string wid, string roleId, string stationId)
        {
            if (!isLanActive || !IsHost() || partnerEndPoint == null) return;
            SendPacket(EncodeWorker(wid, roleId, stationId), partnerEndPoint);
        }

        public static bool ApplyWorkerPacket(string msg, Kitchen.KitchenController kc)
        {
            try
            {
                if (kc == null || string.IsNullOrEmpty(msg)) return false;
                string[] parts = msg.Split('|');
                if (parts.Length < 4 || parts[0] != "WORKER") return false;
                string wid = CleanSyncField(parts[1]);
                string role = CleanSyncField(parts[2]);
                string sid = CleanSyncField(parts[3]);
                if (wid.Length == 0 || role.Length == 0 || sid.Length == 0) return false;
                if (!kc.stations.TryGetValue(sid, out var station) || station == null) return false;
                if (kc.activeWorkers.ContainsKey(wid)) return true;

                int uid = 0;
                foreach (char c in wid) if (c >= '0' && c <= '9') uid = (uid * 10 + (c - '0')) % 100000;

                int tier = 2;
                foreach (var p in kc.playersByPeer.Values)
                    if (p != null && p.characterId == "jiwon") { tier = Mathf.Max(2, p.characterTier); break; }

                return kc.SpawnWorkerVisual(wid, role, station, uid, tier) != null;
            }
            catch
            {
                return false;
            }
        }

        // ---------------- Host-authoritative station snapshot (STATIONS packet) ----------------
        // STATIONS|S;id;item;quality;vessel;comps;cookState;cookElapsed;strikes;placedComps;passSlots|...|H;peer;item;quality
        // Empty fields are "-". Lists use ",". Pass slots are "item~peer".

        private static string CleanSyncField(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c != '|' && c != ';' && c != ',' && c != '~') sb.Append(c);
            }
            return sb.ToString();
        }

        private static string OrDash(string s) => string.IsNullOrEmpty(s) ? "-" : s;

        private static string JoinList(List<string> list)
        {
            if (list == null || list.Count == 0) return "-";
            var cleaned = new List<string>();
            foreach (var s in list)
            {
                string c = CleanSyncField(s);
                if (c.Length > 0) cleaned.Add(c);
            }
            return cleaned.Count == 0 ? "-" : string.Join(",", cleaned);
        }

        private static List<string> SplitList(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s) || s == "-") return list;
            foreach (var p in s.Split(',')) if (p.Length > 0) list.Add(p);
            return list;
        }

        public static string EncodeStationSnapshot(IEnumerable<StationBase> stations, Dictionary<int, Player.PlayerController> chefs)
        {
            var sb = new StringBuilder("STATIONS");
            var seen = new HashSet<string>();
            if (stations != null)
            {
                foreach (var st in stations)
                {
                    if (st == null) continue;
                    string id = CleanSyncField(st.stationId);
                    if (id.Length == 0 || !seen.Add(id)) continue;

                    var it = st.heldItem;
                    string cookState = "-", cookElapsed = "-", strikes = "-", placed = "-", slots = "-";
                    if (st is CookingStation cs)
                    {
                        cookState = ((int)cs.currentState).ToString(CultureInfo.InvariantCulture);
                        cookElapsed = cs.cookingElapsedSeconds.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    if (st is PrepStation ps) strikes = ps.strikesCompleted.ToString(CultureInfo.InvariantCulture);
                    if (st is AssemblyStation asm) placed = JoinList(asm.placedComponents);
                    if (st is PassCounter pc)
                    {
                        pc.InitSlots();
                        var parts = new List<string>();
                        foreach (var slot in pc.slots)
                        {
                            string sid = slot != null && slot.item != null ? OrDash(CleanSyncField(slot.item.itemId)) : "-";
                            int peer = slot != null && slot.item != null ? slot.placedByPeer : 0;
                            parts.Add(sid + "~" + peer.ToString(CultureInfo.InvariantCulture));
                        }
                        slots = string.Join(",", parts);
                    }

                    sb.Append("|S;").Append(id).Append(';')
                      .Append(it != null ? OrDash(CleanSyncField(it.itemId)) : "-").Append(';')
                      .Append(it != null ? ((int)it.quality).ToString(CultureInfo.InvariantCulture) : "-").Append(';')
                      .Append(it != null ? OrDash(CleanSyncField(it.vesselType)) : "-").Append(';')
                      .Append(it != null ? JoinList(it.assembledComponents) : "-").Append(';')
                      .Append(cookState).Append(';').Append(cookElapsed).Append(';')
                      .Append(strikes).Append(';').Append(placed).Append(';').Append(slots);
                }
            }
            if (chefs != null)
            {
                foreach (var kv in chefs)
                {
                    if (kv.Value == null) continue;
                    var ci = kv.Value.carriedItem;
                    sb.Append("|H;").Append(kv.Key.ToString(CultureInfo.InvariantCulture)).Append(';')
                      .Append(ci != null ? OrDash(CleanSyncField(ci.itemId)) : "-").Append(';')
                      .Append(ci != null ? ((int)ci.quality).ToString(CultureInfo.InvariantCulture) : "-").Append(';')
                      .Append(ci != null ? OrDash(CleanSyncField(ci.vesselType)) : "-").Append(';')
                      .Append(ci != null ? JoinList(ci.assembledComponents) : "-");
                }
            }
            return sb.ToString();
        }

        private static KitchenItem BuildSyncedItem(string id, string q, string vessel, string comps)
        {
            if (string.IsNullOrEmpty(id) || id == "-") return null;
            QualityTier qt = QualityTier.STANDARD;
            if (int.TryParse(q, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qi) && Enum.IsDefined(typeof(QualityTier), qi))
                qt = (QualityTier)qi;
            string v = string.IsNullOrEmpty(vessel) || vessel == "-" ? "none" : vessel;
            var item = new KitchenItem(id, Player.PlayerController.ResolveItemSpriteName(id), v, qt);
            item.assembledComponents.AddRange(SplitList(comps));
            return item;
        }

        private static bool SameItem(KitchenItem a, KitchenItem b)
        {
            if (a == null || b == null) return a == b;
            if (a.itemId != b.itemId || a.quality != b.quality || a.vesselType != b.vesselType) return false;
            int ac = a.assembledComponents?.Count ?? 0, bc = b.assembledComponents?.Count ?? 0;
            if (ac != bc) return false;
            for (int i = 0; i < ac; i++) if (a.assembledComponents[i] != b.assembledComponents[i]) return false;
            return true;
        }

        public static bool ApplyStationSnapshot(string pkt, Dictionary<string, StationBase> stations, Dictionary<int, Player.PlayerController> chefs)
        {
            if (string.IsNullOrEmpty(pkt) || !pkt.StartsWith("STATIONS|")) return false;
            bool applied = false;
            try
            {
                string[] entries = pkt.Split('|');
                for (int e = 1; e < entries.Length; e++)
                {
                    try
                    {
                        string[] f = entries[e].Split(';');
                        if (f.Length >= 11 && f[0] == "S" && stations != null)
                        {
                            if (!stations.TryGetValue(f[1], out var st) || st == null) continue;
                            st.heldItem = BuildSyncedItem(f[2], f[3], f[4], f[5]);

                            if (st is CookingStation cs)
                            {
                                if (int.TryParse(f[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cst) && Enum.IsDefined(typeof(CookingState), cst))
                                    cs.currentState = (CookingState)cst;
                                if (float.TryParse(f[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float el) && !float.IsNaN(el) && !float.IsInfinity(el))
                                    cs.cookingElapsedSeconds = Mathf.Max(0f, el);
                            }
                            if (st is AssemblyStation asm)
                            {
                                asm.placedComponents.Clear();
                                asm.placedComponents.AddRange(SplitList(f[9]));
                            }
                            if (st is PassCounter pc)
                            {
                                pc.InitSlots();
                                string[] sl = f[10] == "-" ? new string[0] : f[10].Split(',');
                                for (int i = 0; i < PassCounter.NUM_SLOTS; i++)
                                {
                                    string sid = "-"; int peer = 0;
                                    if (i < sl.Length)
                                    {
                                        string[] sp = sl[i].Split('~');
                                        sid = sp[0];
                                        if (sp.Length > 1) int.TryParse(sp[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out peer);
                                    }
                                    var newItem = BuildSyncedItem(sid, "-", "-", "-");
                                    if (newItem == null) { pc.slots[i].item = null; pc.slots[i].placedByPeer = 0; }
                                    else if (pc.slots[i].item == null || pc.slots[i].item.itemId != newItem.itemId)
                                    {
                                        pc.slots[i].item = newItem; pc.slots[i].placedByPeer = peer;
                                    }
                                    else pc.slots[i].placedByPeer = peer;
                                }
                            }
                            if (st is PrepStation ps)
                            {
                                if (int.TryParse(f[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sk)) ps.strikesCompleted = Mathf.Max(0, sk);
                                try { ps.UpdateVisuals(); } catch { }
                            }
                            else
                            {
                                try { st.RefreshStationVisuals(); } catch { }
                            }
                            applied = true;
                        }
                        else if (f.Length >= 3 && f[0] == "H" && chefs != null)
                        {
                            if (!int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int peerId)) continue;
                            if (!chefs.TryGetValue(peerId, out var chef) || chef == null) continue;
                            var item = BuildSyncedItem(f[2], f.Length > 3 ? f[3] : "-", f.Length > 4 ? f[4] : "-", f.Length > 5 ? f[5] : "-");
                            if (item == null)
                            {
                                if (chef.carriedItem != null) { try { chef.ClearCarriedItem(); } catch { } chef.carriedItem = null; }
                            }
                            else if (!SameItem(chef.carriedItem, item))
                            {
                                try { chef.SetCarriedItem(item); } catch { }
                                chef.carriedItem = item;
                            }
                            applied = true;
                        }
                    }
                    catch (Exception) { /* skip one bad entry */ }
                }
            }
            catch (Exception) { return false; }
            return applied;
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

        /// <summary>Lobby-only stand-in chef: lets a player step off Linh/Jiwon, but can never be Ready.</summary>
        public const string PLACEHOLDER_CHAR = "placeholder";

        /// <summary>True when a player using this character may mark themselves Ready.</summary>
        public static bool CanReadyAs(string characterId) => characterId != PLACEHOLDER_CHAR;

        public (bool ok, string reason) ApplyPlayerSelection(int peerId, string characterId, string specializationId)
        {
            if (!players.ContainsKey(peerId))
                return (false, "Unknown player peer ID.");

            if (characterId != "linh" && characterId != "jiwon" && characterId != PLACEHOLDER_CHAR)
                return (false, "Unknown character.");

            foreach (var kvp in players)
            {
                if (characterId != PLACEHOLDER_CHAR && kvp.Key != peerId && kvp.Value.character == characterId)
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

            if (isReady && !CanReadyAs(players[peerId].character))
                return (false, "Pick Linh or Jiwon before readying up (placeholder chef cannot Ready).");

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

                if (!CanReadyAs(p.character))
                    return (false, $"{p.name} is using the placeholder chef. Pick Linh or Jiwon.");

                if (seenChars.Contains(p.character))
                    return (false, "Linh and Jiwon must both be represented (no duplicate characters).");

                seenChars.Add(p.character);
            }

            return (true, "");
        }
    }
}
