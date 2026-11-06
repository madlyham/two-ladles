# Testing Guide — Two Ladles (`docs/TESTING.md`)

This guide explains how to test **Two Ladles** at every verification level without writing any code.

> **Important Distinction:**
> - **Level 1 (Headless Automated Tests):** Proves game rules, recipes, station state machines, and anti-exploit math.
> - **Level 2 (Same-Computer 2-Window Test):** Proves two live game instances communicate over UDP `127.0.0.1` and synchronize gameplay.
> - **Level 3 (Cross-Device Wi-Fi LAN Test):** Proves real local Wi-Fi multiplayer between a Windows PC and an Android phone (or two Android phones).

---

## Level 1: Headless Automated Logic Tests

### What It Tests
Verifies all **114 automated assertions across 13 comprehensive test suites**:
1. Starter recipes & cross-prep interdependence (*Phở Bò*, *Bánh Mì Thịt*, *Tteokbokki*, *Bulgogi Bánh Mì*).
2. Preparation Counter 4-strike cadence & non-blocking misses.
3. Cooking state machine (`RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`) and Phở blanching separation.
4. Recipe assembly validation and broth-last sequence.
5. Pass Counter handoff bonuses (+15 Flow within 4s) and anti-exploit guards.
6. Kitchen Flow (0–100) and Rush Rhythm (15s, 1.5x score, paused Flow).
7. Washing station and finite 4-plate lifecycle.
8. Multiplayer lobby character exclusivity and readiness reset.
9. Persistent progression, stage unlocks, and kitchen upgrades.
10. Linh Execution Specialist: Tiers 1–3 speed multipliers (1.0x, 0.92x, 0.85x) and visual third arm evolution.
11. Jiwon Multitasking & Delegation: Part-time worker hiring ("Call in the Crew"), coin balance deduction, cooldowns, and worker state machine.
12. 5 Handcrafted Campaign Levels & Milestone 3 Character Tier Unlocks.
13. Endless Rush Mode: Dynamic pressure scaling and personal best tracking.

### Steps to Run
Open **PowerShell** in `C:\Users\madly\OneDrive\Desktop\Projects\two-ladles` and run (read-only verification):
```powershell
& "$env:LOCALAPPDATA\Programs\two-ladles-tools\godot\Godot_v4.7.2-stable_win64_console.exe" --headless -- --run-tests
```
**Expected Output:** Ends with `RESULTS: 114 / 114 Passed | 0 Failed` and `ALL AUTOMATED TESTS PASSED CLEANLY.`

---

## Level 2: Same-Computer Two-Instance Multiplayer Test (`127.0.0.1`)

### What It Tests
Launches two independent game windows on your Windows PC that connect over UDP port `24567` using `127.0.0.1` (localhost).

### Automated 2-Process UDP Handshake Smoke Test
Run this command in PowerShell to automatically launch a Host process and Client process, verify they connect over UDP, ready up as Linh & Jiwon, and start a synchronized shift:
```powershell
$godot = "$env:LOCALAPPDATA\Programs\two-ladles-tools\godot\Godot_v4.7.2-stable_win64_console.exe"
$h = Start-Process $godot -ArgumentList "--headless","--","--lan-smoke-host" -PassThru -NoNewWindow
$c = Start-Process $godot -ArgumentList "--headless","--","--lan-smoke-client" -PassThru -NoNewWindow
$h.WaitForExit(10000); $c.WaitForExit(10000)
```

### Interactive Side-by-Side 2-Window Desktop Playtest
1. Open **PowerShell** in `C:\Users\madly\OneDrive\Desktop\Projects\two-ladles`.
2. Launch Window 1 (Host - defaults to **Linh**):
   ```powershell
   Start-Process "build\windows\two-ladles.exe" -ArgumentList "--", "--auto-host"
   ```
3. Launch Window 2 (Client - defaults to **Jiwon**):
   ```powershell
   Start-Process "build\windows\two-ladles.exe" -ArgumentList "--", "--auto-join=127.0.0.1"
   ```
4. In **both** windows, click **TOGGLE READY**.
5. In **Window 1 (Host)**, click **START SHIFT**.
6. **Controls inside the Kitchen:**
   - **Move:** `WASD` or Arrow Keys (or on-screen `▲ ▼ ◀ ▶` touch buttons).
   - **Primary Interact / Pick Up / Place / Pass / Serve:** `E` or `Space` (or on-screen `INTERACT / PASS` button).
   - **Secondary Strike / Scrub / Take Clean Bowl:** `F` (or on-screen `STRIKE / SCRUB / BOWL` button).

---

## Level 3: Cross-Device Wi-Fi LAN Testing (Desktop + Android or Dual Android)

### Procedure B: Windows Desktop (Host) + Android Phone (Client)

#### Step 1: Find Your Windows PC's Local Wi-Fi IPv4 Address
Open PowerShell on your Windows PC and run (read-only):
```powershell
Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -match "Wi-Fi|Ethernet" -and $_.IPAddress -notmatch "^169\.254" } | Select-Object InterfaceAlias, IPAddress
```
Note the `IPAddress` (for example, `192.168.1.42`). Both your PC and Android phone must be connected to the same Wi-Fi network.

#### Step 2: Allow UDP Port `24567` in Windows Firewall
If your Android phone cannot connect to your Windows PC host, Windows Firewall is likely blocking incoming UDP traffic on port `24567`. Run **PowerShell as Administrator** once to allow it:
```powershell
New-NetFirewallRule -DisplayName "Two Ladles LAN UDP 24567" -Direction Inbound -Protocol UDP -LocalPort 24567 -Action Allow
```
*(To undo/remove this rule later: `Remove-NetFirewallRule -DisplayName "Two Ladles LAN UDP 24567"`)*

#### Step 3: Connect & Play
1. On your **Windows PC**, launch `build\windows\two-ladles.exe` and click **HOST GAME (UDP)**.
2. Install `two-ladles-debug.apk` (from GitHub Actions Artifacts) onto your **Android phone**.
3. On your **Android phone**, type your PC's IPv4 address (e.g., `192.168.1.42`) into the IP box and tap **JOIN GAME**.
4. Select characters (**Linh** & **Jiwon**) and specializations, tap **TOGGLE READY** on both devices, and click **START SHIFT** on the Host.

---

### Procedure C: Two Physical Android Devices over Wi-Fi

1. Connect both Android phones/tablets to the same Wi-Fi router (ensure the router does not have "AP Isolation / Guest Client Isolation" enabled).
2. On **Phone 1 (Host)**: Open Wi-Fi Settings → Network Details to view Phone 1's local IPv4 address (e.g., `192.168.1.55`), then open **Two Ladles** and tap **HOST GAME (UDP)**.
3. On **Phone 2 (Client)**: Open **Two Ladles**, enter Phone 1's IPv4 address (`192.168.1.55`), and tap **JOIN GAME**.
4. Both players tap **TOGGLE READY**, and Phone 1 taps **START SHIFT**.

---

## Level 4: Optional VirtualBox Android VM Notes

Oracle VirtualBox is installed on this PC (`C:\Program Files\Oracle\VirtualBox\VBoxManage.exe`).
- **When Useful:** Optional UI layout inspection of an x86_64 Android image (`export_presets.cfg` includes `architectures/x86_64=true` alongside `arm64-v8a=true`).
- **Networking Caveat:** VirtualBox NAT mode isolates UDP broadcasts/incoming sockets. If testing LAN connection inside VirtualBox, set the VM Network Adapter to **Bridged Adapter** bound to your Wi-Fi card. However, real physical Android hardware over Wi-Fi remains the authoritative Level 3 test.
