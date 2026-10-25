## Handles all networking communication with the server
## Uses WebSocket over SignalR for real-time updates
extends Node

class_name NetworkManager

signal connected_to_server
signal disconnected_from_server
signal lobby_updated
signal arena_state_received
signal hit_validation_received
signal player_died
signal match_started
signal match_ended
signal error_occurred

var _websocket: WebSocketPeer
var _server_url: String = "ws://localhost:5000/hubs"
var _player_id: int = 0
var _session_token: String = ""
var _current_arena_id: String = ""
var _is_connected: bool = false

# Reconnect settings
var _reconnect_attempts: int = 0
var _max_reconnect_attempts: int = 5
var _reconnect_interval: float = 2.0
var _reconnect_timer: float = 0.0

func _ready() -> void:
	set_process(true)

func _process(delta: float) -> void:
	if not _is_connected:
		_reconnect_timer += delta
		if _reconnect_timer >= _reconnect_interval and _reconnect_attempts < _max_reconnect_attempts:
			_reconnect_timer = 0.0
			connect_to_server()

	if _websocket and _websocket.get_ready_state() == WebSocketPeer.STATE_OPEN:
		_websocket.poll()
		while _websocket.get_available_packet_count() > 0:
			_handle_message(_websocket.get_message().get_string_from_utf8())

func connect_to_server(player_id: int = 0, token: String = "") -> void:
	_player_id = player_id
	_session_token = token
	_reconnect_attempts = 0
	
	if _websocket:
		_websocket.close()
	
	_websocket = WebSocketPeer.new()
	var error = _websocket.connect_to_url(_server_url)
	
	if error != OK:
		error_occurred.emit("Failed to connect to server: %s" % error)
		return
	
	print("Connecting to server: %s" % _server_url)

func disconnect_from_server() -> void:
	if _websocket:
		_websocket.close()
	_is_connected = false
	disconnected_from_server.emit()

func join_lobby(player_name: String) -> void:
	if not _is_connected:
		error_occurred.emit("Not connected to server")
		return
	
	_send_message("JoinLobby", {
		"playerId": _player_id,
		"playerName": player_name
	})

func ready_up() -> void:
	_send_message("ReadyUp", {"playerId": _player_id})

func not_ready() -> void:
	_send_message("NotReady", {"playerId": _player_id})

func join_arena(arena_id: String) -> void:
	_current_arena_id = arena_id
	_send_message("JoinArena", {
		"arenaId": arena_id,
		"playerId": _player_id
	})

func send_player_input(input: PlayerInput) -> void:
	_send_message("SendPlayerInput", {
		"arenaId": _current_arena_id,
		"playerId": _player_id,
		"input": {
			"moveX": input.move_x,
			"moveZ": input.move_z,
			"yaw": input.yaw,
			"pitch": input.pitch,
			"isJumping": input.is_jumping,
			"isSprinting": input.is_sprinting,
			"timestamp": Time.get_ticks_msec()
		}
	})

func send_combat_input(target_id: int, distance: float) -> void:
	_send_message("SendCombatInput", {
		"arenaId": _current_arena_id,
		"playerId": _player_id,
		"combatInput": {
			"targetPlayerId": target_id,
			"actionType": "attack",
			"timestamp": Time.get_ticks_msec()
		},
		"clientDistance": distance
	})

func request_reconnect() -> void:
	_send_message("Reconnect", {
		"arenaId": _current_arena_id,
		"playerId": _player_id
	})

## Private methods

func _send_message(method: String, data: Dictionary) -> void:
	if not _is_connected:
		return
	
	var message = {
		"method": method,
		"data": data
	}
	
	var json_string = JSON.stringify(message)
	var error = _websocket.send_text(json_string)
	
	if error != OK:
		error_occurred.emit("Failed to send message: %s" % error)

func _handle_message(message: String) -> void:
	var json = JSON.parse_string(message)
	if not json:
		return
	
	var method = json.get("method", "")
	var data = json.get("data", {})
	
	match method:
		"Connected":
			_on_connected()
		"Disconnected":
			_on_disconnected()
		"LobbyStatus":
			lobby_updated.emit(data)
		"PlayerJoinedLobby":
			lobby_updated.emit(data)
		"PlayerLeftLobby":
			lobby_updated.emit(data)
		"PlayerReadyStatusChanged":
			lobby_updated.emit(data)
		"MatchCanStart":
			print("Match can start!")
		"ArenaState":
			arena_state_received.emit(data)
		"ArenaStateUpdate":
			arena_state_received.emit(data)
		"HitValidation":
			hit_validation_received.emit(data)
		"PlayerDied":
			player_died.emit(data)
		"PlayerJoinedArena":
			arena_state_received.emit(data)
		"PlayerLeftArena":
			arena_state_received.emit(data)
		"MatchStarted":
			match_started.emit(data)
		"MatchEnded":
			match_ended.emit(data)
		"ReconnectSuccess":
			print("Reconnect successful")
		"Error":
			error_occurred.emit(data)

func _on_connected() -> void:
	_is_connected = true
	_reconnect_attempts = 0
	connected_to_server.emit()
	print("Connected to server!")

func _on_disconnected() -> void:
	_is_connected = false
	_reconnect_attempts += 1
	disconnected_from_server.emit()
	print("Disconnected from server. Attempting reconnect %d/%d" % [_reconnect_attempts, _max_reconnect_attempts])
