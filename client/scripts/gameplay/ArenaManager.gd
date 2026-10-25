## Manages arena gameplay, player spawning, and match state
extends Node3D

class_name ArenaManager

var network_manager: NetworkManager
var player_scene: PackedScene = preload("res://scenes/player.tscn")

var players: Dictionary[int, Player] = {}
var local_player: Player
var local_player_id: int = 0

var match_state: String = "waiting"
var time_remaining: float = 600.0

@onready var hud = $HUD
@onready var score_board = $HUD/ScoreBoardPanel

func _ready() -> void:
	network_manager = get_tree().root.get_child(0).get_node("NetworkManager")
	network_manager.arena_state_received.connect(_on_arena_state_received)
	network_manager.player_died.connect(_on_player_died)
	network_manager.match_ended.connect(_on_match_ended)

func _process(delta: float) -> void:
	time_remaining -= delta
	if time_remaining <= 0:
		end_match()

func spawn_player(player_id: int, player_name: String, spawn_pos: Vector3) -> Player:
	var player = player_scene.instantiate()
	player.global_position = spawn_pos
	player.player_id = player_id
	player.player_name = player_name
	add_child(player)
	players[player_id] = player
	return player

func _on_arena_state_received(state: Dictionary) -> void:
	match_state = state.get("state", "waiting")
	time_remaining = state.get("timeRemaining", 600.0)
	
	var players_data = state.get("players", [])
	
	# Spawn missing players
	for player_data in players_data:
		var pid = player_data.get("playerId")
		if pid not in players:
			var pos = Vector3(
				player_data.get("positionX"),
				player_data.get("positionY"),
				player_data.get("positionZ")
			)
			spawn_player(pid, "Player_%d" % pid, pos)
		
		# Update existing player
		var player = players.get(pid)
		if player:
			player.apply_server_state(player_data)

func _on_player_died(data: Dictionary) -> void:
	var dead_id = data.get("playerId")
	var killer_id = data.get("killerId")
	
	if dead_id in players:
		players[dead_id].is_alive = false

func end_match() -> void:
	match_state = "ended"
	print("Match ended!")
	# TODO: Show match results
	pass

func _on_match_ended(results: Dictionary) -> void:
	match_state = "ended"
	print("Match ended: %s" % results)
