# Networking Architecture — Two Ladles

> [!IMPORTANT]
> Networking is implemented in Unity C# at `unity/TwoLadles/Two Ladles/Assets/Scripts/Networking/NetworkManager.cs`. The retired Godot prototype (ENet) is no longer part of the repository.

## 1. Transport & Topology

- **Sockets:** raw `System.Net.Sockets.UdpClient` — no third-party networking library.
- **Ports:**
  - `24567` — gameplay traffic (`NetworkManager.DEFAULT_PORT`).
  - `24568` — LAN room discovery broadcast (`DISCOVERY_PORT`).
- **Topology:** 1 authoritative host (peer `1`) + 1 client (peer `2`); `MAX_CLIENTS = 1`.
- **Threading:** a background thread receives packets and queues them; packets are processed on Unity's main thread.
- **Packet format:** UTF-8 text, fields separated by `|` (e.g. `POS|2|1.25|0|-3.5|...`).
- **Cross-locale safety:** all decimal numbers in `POS` and `THROW` are written and parsed with `CultureInfo.InvariantCulture`, so a Vietnamese/Korean-locale phone and an English-locale laptop agree on `1.25` vs `1,25`.

## 2. LAN Room Discovery (Wi-Fi scan)

The game finds the device's real **Wi-Fi IPv4 address** (`NetworkManager.TryGetLocalWifiIPv4`; skips virtual adapters such as Hyper-V, VMware, VirtualBox, WSL, VPN, Bluetooth). It never defaults to `127.0.0.1`.

Discovery uses three layers, so it still works when one is blocked:

1. **Host beacon:** the host sends `LAN_BEACON|room|hostName|port|playerCount` every 1.2 s on UDP `24568` to the **subnet broadcast** (e.g. `192.168.1.255`), `255.255.255.255` and loopback.
2. **Join probe:** opening the **Join** screen calls `ScanWifiNow()`, which sends `LAN_DISCOVER` to the subnet broadcast, `255.255.255.255` and loopback.
3. **Wi-Fi sweep:** for 60 s after the Join screen opens, the joiner also sends `LAN_DISCOVER` directly (unicast) to every address in its /24 Wi-Fi subnet (`x.x.x.1`–`x.x.x.254`, port `24567`) every 3 s. This works on routers or phones that drop broadcast packets.

The host answers every `LAN_DISCOVER` with a unicast `LAN_BEACON` to the sender. Found kitchens appear as tappable slots; the IP box auto-fills with the first found host. Rooms not heard from for 5 s are removed.

**Manual fallback:** the host's lobby screen shows `Your Wi-Fi IP: x.x.x.x`. The joiner can type it in the IP box (pre-filled with the Wi-Fi prefix, e.g. `192.168.1.`; last good IP remembered in `PlayerPrefs` key `TwoLadles_LastJoinedIP`). Loopback/invalid addresses are rejected.

## 2b. Join Handshake & Lobby Rules

1. Client sends `HELLO|2|name|jiwon|flame_keeper|0`, re-sent every 0.5 s up to 10 times until the host replies with `LOBBY_STATE`. If no reply: *"Host did not answer. Same Wi-Fi? Host allowed the game in Windows Firewall?"*
2. Host accepts only one partner. A repeated `HELLO` from the same device does not reset it; a third device is ignored.
3. Both chefs start **not ready**.
4. **START** works only when: 2 chefs are in the room, both pressed **READY**, and the **host** presses it (`CanStartShift(true)`). The client's START button is disabled and shows *"Waiting for the host…"*.
5. Solo mode and the tutorial are not gated.

Text fields (names, room names) are cleaned before sending: `|` and `;` removed, max 24 characters.

## 3. Packet Reference

| Packet | Direction | Purpose |
| :--- | :--- | :--- |
| `LAN_DISCOVER` | Joiner → broadcast | Ask hosts on the LAN to announce themselves. |
| `LAN_BEACON\|room\|host\|port\|count` | Host → broadcast | Advertise a room. |
| `HELLO\|2\|name\|character\|spec\|ready` | Client → Host | Join request; host registers player 2. |
| `LOBBY_STATE\|count\|id;name;char;spec;ready…` | Host → Client | Full lobby snapshot. |
| `LOBBY_SELECT\|peer\|char\|spec` | Client → Host | Chef / specialization change (host enforces Linh/Jiwon exclusivity and resets ready). |
| `LOBBY_READY\|peer\|0/1` | Client → Host | Ready toggle. |
| `START_SHIFT\|stage\|mode` | Host → Client | Start the selected day / mode on both machines. |
| `POS\|peer\|x\|y\|z\|fx\|fy\|walkAnim\|carried` | Both, ~30 Hz | Chef position, facing, walk animation, carried item id. |
| `INTERACT\|peer\|0/1` | Client → Host | Primary / secondary interaction; host runs it on the client's chef (range-checked, `MAX_INTERACT_DISTANCE = 2.6f`). |
| `THROW\|peer\|ax\|ay` | Both | Throw carried item in a direction. |
| `STORY_DONE` | Host → Client | Host closed/skipped the day-start story phone; client closes its phone. |
| `ORDERS\|score\|rep\|flow\|rush\|rushLeft\|waveDone\|waveNeed\|n\|ticket;items;vessel;time;…` | Host → Client, 4 Hz | Authoritative order queue, score, reputation, Kitchen Flow, Rush Rhythm and wave progress (`ShiftManager.EncodeNetworkState` / `DecodeNetworkState`). Malformed packets are ignored. |
| `DISCONNECT\|reason` | Both | Graceful leave. |

## 4. Authority

- The host executes all station interactions (including the client's, via `INTERACT`), owns order spawning, cooking timers, Pass Counter handoff checks (`placed_by != retrieved_by`, ≤ `4000 ms`, once per order), Kitchen Flow, Rush Rhythm, reputation and scoring.
- The host enforces lobby rules (character exclusivity, ready reset on loadout change) and decides when the shift starts.
- **Order sync:** after `START_SHIFT` the client sets `ShiftManager.isNetworkClient = true`. Its own order spawning and expiry are switched off, and it shows the orders, score, reputation and Flow that the host sends in `ORDERS` packets every 0.25 s. The flag clears when the connection closes.

> [!NOTE]
> Station contents (what is in each pan or on each counter) are not streamed. They change only on the host, through `INTERACT`. During the LAN playtest, check that pans and counters look the same on both screens.

## 5. Disconnection

- **Leaving:** the lobby **LEAVE** button and in-game exits call `NetworkManager.Instance.ReturnToMenu(reason)`, which sends `DISCONNECT|reason` to the partner and closes the socket.
- **Partner left:** on receiving `DISCONNECT`, the game shows the reason and closes the connection (returns to menu).
- **Room full:** `MAX_CLIENTS = 1`, so only one client is accepted.

## 6. LAN Crossplay Test Checklist

Use this for a laptop (Windows) + phone (Android) test.

1. **Same network:** laptop and phone on the same Wi-Fi (not a guest network that isolates devices; mobile hotspot from a third device is fine). On the phone, turn **mobile data off** so it doesn't route through 4G/5G.
2. **Windows firewall:** the first time `TwoLadles.exe` runs, Windows may ask to allow network access — tick **Private networks** and allow. Make sure Windows treats the Wi-Fi as **Private**, not Public. If you missed it, open PowerShell **as Administrator** and run (this *changes* firewall settings):
   ```powershell
   New-NetFirewallRule -DisplayName "Two Ladles LAN UDP" -Direction Inbound -Protocol UDP -LocalPort 24567,24568 -Action Allow
   ```
   To undo later:
   ```powershell
   Remove-NetFirewallRule -DisplayName "Two Ladles LAN UDP"
   ```
3. **Laptop hosts first:** open `TwoLadles.exe` → **HOST A KITCHEN** → enter names → confirm. The room screen shows `Your Wi-Fi IP: x.x.x.x` and *"Waiting for partner to join (1/2)"*. START is greyed out.
4. **Phone joins:** open the app → **JOIN KITCHEN (LAN)**. Status says *"Scanning your Wi-Fi (192.168.x.x)…"*. Within ~5 s the kitchen slot should turn green → tap it. If no slot appears, type the IP shown on the laptop's room screen → **CONNECT**.
5. **Room:** both screens list 2 chefs. Host picks a day, **both** press **READY** (button shows `READY: YES ✓`), then the host presses **START**.
6. **Check:** both chefs move on both screens; the phone chef can pick up and use stations; **orders, score and timers match on both screens**; the story phone closes on both when the host skips it; **LEAVE** on either side returns both to the menu.
7. **If it fails:** note which step failed and whether the phone sees the room slot (discovery, port 24568) vs. cannot connect by IP (gameplay, port 24567).
