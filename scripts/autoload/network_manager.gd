extends Node
## Authoritative LAN Multiplayer Manager using ENetMultiplayerPeer (UDP default port 24567).
## Enforces 2-player max, character mutual exclusivity (Linh vs Jiwon), specialization selection,
## readiness reset on selection change, and safe disconnection recovery.

signal lobby_updated(players_dict: Dictionary)
signal connection_status_changed(status_text: String, is_error: bool)
signal shift_start_requested()
signal returned_to_menu(reason: String)

const DEFAULT_PORT: int = 24567
const MAX_CLIENTS: int = 1 # 1 Host + 1 Client = 2 Total Players

var peer: ENetMultiplayerPeer = null
var players: Dictionary = {} # peer_id (int) -> Dictionary
var is_lan_active: bool = false

func _ready() -> void:
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)

func host_game(port: int = DEFAULT_PORT, player_name: String = "Player 1") -> Error:
	close_connection()
	peer = ENetMultiplayerPeer.new()
	var err: Error = peer.create_server(port, MAX_CLIENTS)
	if err != OK:
		connection_status_changed.emit("Failed to host on UDP port %d (Error %d)." % [port, err], true)
		return err
	multiplayer.multiplayer_peer = peer
	is_lan_active = true
	players.clear()
	players[1] = {
		"peer_id": 1,
		"name": player_name,
		"character": "linh",
		"specialization": "prep_master",
		"ready": false,
	}
	connection_status_changed.emit("Hosting on UDP port %d. Waiting for partner..." % port, false)
	lobby_updated.emit(players)
	return OK

func join_game(ip_address: String, port: int = DEFAULT_PORT, player_name: String = "Player 2") -> Error:
	close_connection()
	var clean_ip: String = ip_address.strip_edges()
	if clean_ip.is_empty():
		connection_status_changed.emit("Please enter a valid host IPv4 address.", true)
		return ERR_INVALID_PARAMETER
	peer = ENetMultiplayerPeer.new()
	var err: Error = peer.create_client(clean_ip, port)
	if err != OK:
		connection_status_changed.emit("Failed to create UDP client for %s:%d." % [clean_ip, port], true)
		return err
	multiplayer.multiplayer_peer = peer
	is_lan_active = true
	set_meta("pending_name", player_name)
	connection_status_changed.emit("Connecting to %s:%d..." % [clean_ip, port], false)
	return OK

func close_connection() -> void:
	if multiplayer.multiplayer_peer != null:
		multiplayer.multiplayer_peer.close()
		multiplayer.multiplayer_peer = null
	peer = null
	is_lan_active = false
	players.clear()
	lobby_updated.emit(players)

func is_host() -> bool:
	if not is_lan_active or multiplayer.multiplayer_peer == null:
		return true
	return multiplayer.is_server()

## Authoritative validation of character selection (Linh and Jiwon cannot be selected by both players).
## Changing character or specialization automatically resets that player's ready state to false.
func apply_player_selection(peer_id: int, character_id: String, specialization_id: String) -> Dictionary:
	if not players.has(peer_id):
		return {"ok": false, "reason": "Unknown peer ID."}
	if not GameData.characters.has(character_id):
		return {"ok": false, "reason": "Invalid character ID."}
	if not GameData.specializations.has(specialization_id):
		return {"ok": false, "reason": "Invalid specialization ID."}

	# Check mutual exclusivity for character selection
	for other_id: int in players.keys():
		if other_id != peer_id and players[other_id].get("character", "") == character_id:
			return {
				"ok": false,
				"reason": "%s is already selected by your partner! Choose the other chef." % GameData.characters[character_id]["display_name"]
			}

	var p: Dictionary = players[peer_id]
	var changed: bool = (p.get("character", "") != character_id) or (p.get("specialization", "") != specialization_id)
	p["character"] = character_id
	p["specialization"] = specialization_id
	if changed:
		p["ready"] = false
	players[peer_id] = p
	_broadcast_lobby_state()
	return {"ok": true, "reason": ""}

func set_player_ready(peer_id: int, is_ready: bool) -> Dictionary:
	if not players.has(peer_id):
		return {"ok": false, "reason": "Unknown peer ID."}
	players[peer_id]["ready"] = is_ready
	_broadcast_lobby_state()
	return {"ok": true}

func can_start_shift(require_two_players: bool = true) -> Dictionary:
	if require_two_players and players.size() < 2:
		return {"ok": false, "reason": "Two connected human players are required to start a shift."}
	var chars_seen: Dictionary = {}
	for pid: int in players.keys():
		var p: Dictionary = players[pid]
		if not p.get("ready", false):
			return {"ok": false, "reason": "%s is not Ready yet." % p.get("name", "Player")}
		var c: String = p.get("character", "")
		if chars_seen.has(c):
			return {"ok": false, "reason": "Linh and Jiwon must both be represented (no duplicate characters)."}
		chars_seen[c] = true
	return {"ok": true, "reason": ""}

@rpc("any_peer", "call_local", "reliable")
func request_selection(character_id: String, specialization_id: String) -> void:
	if not is_host():
		return
	var sender_id: int = multiplayer.get_remote_sender_id()
	if sender_id == 0:
		sender_id = 1
	var res: Dictionary = apply_player_selection(sender_id, character_id, specialization_id)
	if not res.get("ok", false) and sender_id != 1:
		_notify_client_error.rpc_id(sender_id, res.get("reason", "Selection rejected."))
	elif not res.get("ok", false) and sender_id == 1:
		connection_status_changed.emit(res.get("reason", "Selection rejected."), true)

@rpc("any_peer", "call_local", "reliable")
func request_ready_toggle(is_ready: bool) -> void:
	if not is_host():
		return
	var sender_id: int = multiplayer.get_remote_sender_id()
	if sender_id == 0:
		sender_id = 1
	set_player_ready(sender_id, is_ready)

@rpc("authority", "call_local", "reliable")
func request_start_shift(require_two_players: bool = true) -> Dictionary:
	if not is_host():
		return {"ok": false, "reason": "Only the host can start the shift."}
	var check: Dictionary = can_start_shift(require_two_players)
	if not check.get("ok", false):
		connection_status_changed.emit(check.get("reason", "Cannot start shift."), true)
		return check
	_rpc_begin_shift.rpc(players)
	return {"ok": true, "reason": ""}

@rpc("authority", "call_local", "reliable")
func _rpc_begin_shift(synced_players: Dictionary) -> void:
	players = synced_players.duplicate(true)
	shift_start_requested.emit()

func _broadcast_lobby_state() -> void:
	if is_lan_active and multiplayer.multiplayer_peer != null and is_host():
		_sync_lobby_players.rpc(players)
	else:
		lobby_updated.emit(players)

@rpc("authority", "call_local", "reliable")
func _sync_lobby_players(synced_players: Dictionary) -> void:
	players = synced_players.duplicate(true)
	lobby_updated.emit(players)

@rpc("authority", "call_local", "reliable")
func _notify_client_error(message: String) -> void:
	connection_status_changed.emit(message, true)

@rpc("any_peer", "reliable")
func _register_client_name(client_name: String) -> void:
	if not is_host():
		return
	var sender_id: int = multiplayer.get_remote_sender_id()
	if players.has(sender_id):
		players[sender_id]["name"] = client_name
		_broadcast_lobby_state()

func _on_peer_connected(id: int) -> void:
	if not is_host():
		return
	if players.size() >= 2:
		# Reject duplicate/third join attempt safely
		peer.disconnect_peer(id)
		return
	var host_char: String = players.get(1, {}).get("character", "linh")
	var default_client_char: String = "jiwon" if host_char == "linh" else "linh"
	players[id] = {
		"peer_id": id,
		"name": "Player 2",
		"character": default_client_char,
		"specialization": "flame_keeper",
		"ready": false,
	}
	connection_status_changed.emit("Partner connected (Peer %d)!" % id, false)
	_broadcast_lobby_state()

func _on_peer_disconnected(id: int) -> void:
	if players.has(id):
		players.erase(id)
	# Ignore clean quit when shift is not running or during shutdown
	if not is_lan_active:
		return
	# Reset host readiness when partner disconnects
	if players.has(1):
		players[1]["ready"] = false
	connection_status_changed.emit("Partner disconnected. Returning to lobby cleanly.", true)
	_broadcast_lobby_state()
	returned_to_menu.emit("Partner disconnected from LAN session.")

func _on_connected_to_server() -> void:
	var my_name: String = get_meta("pending_name", "Player 2")
	connection_status_changed.emit("Connected to Host! Syncing lobby...", false)
	_register_client_name.rpc_id(1, my_name)

func _on_connection_failed() -> void:
	close_connection()
	connection_status_changed.emit("Connection timed out or refused. Check Host IP and UDP Firewall port 24567.", true)
	returned_to_menu.emit("Connection failed.")

func _on_server_disconnected() -> void:
	close_connection()
	connection_status_changed.emit("Host closed the session. Returned to Main Menu.", true)
	returned_to_menu.emit("Host left the game.")
