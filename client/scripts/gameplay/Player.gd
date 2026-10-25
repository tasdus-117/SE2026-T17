## Server-synchronized player controller for arena gameplay
extends CharacterBody3D

class_name Player

@export var move_speed: float = 10.0
@export var sprint_speed: float = 15.0
@export var jump_force: float = 5.0
@export var gravity: float = 9.81
@export var attack_range: float = 5.0
@export var attack_damage: float = 20.0

var player_id: int = 0
var player_name: String = ""
var health: float = 100.0
var max_health: float = 100.0
var is_alive: bool = true
var kills: int = 0
var deaths: int = 0

var network_manager: NetworkManager
var camera: Camera3D
var animation_player: AnimationPlayer

var _velocity: Vector3 = Vector3.ZERO
var _current_input: PlayerInput = PlayerInput.new()
var _last_attack_time: float = 0.0
var _attack_cooldown: float = 0.5

func _ready() -> void:
	camera = $Camera3D
	animation_player = $AnimationPlayer
	network_manager = get_tree().root.get_child(0).get_node("NetworkManager")
	
	# Connect to network events
	network_manager.arena_state_received.connect(_on_arena_state_received)
	network_manager.hit_validation_received.connect(_on_hit_validation)
	network_manager.player_died.connect(_on_player_died)

func _process(delta: float) -> void:
	if not is_alive:
		return
	
	# Collect input
	_current_input = _collect_input()
	
	# Send input to server
	if network_manager and network_manager._is_connected:
		network_manager.send_player_input(_current_input)

func _collect_input() -> PlayerInput:
	var input = PlayerInput.new()
	
	# Movement
	var move_dir = Vector3.ZERO
	if Input.is_action_pressed("ui_up"):
		move_dir.z -= 1
	if Input.is_action_pressed("ui_down"):
		move_dir.z += 1
	if Input.is_action_pressed("ui_left"):
		move_dir.x -= 1
	if Input.is_action_pressed("ui_right"):
		move_dir.x += 1
	
	if move_dir.length() > 0:
		move_dir = move_dir.normalized()
		input.move_x = move_dir.x
		input.move_z = move_dir.z
	
	# Sprinting
	input.is_sprinting = Input.is_action_pressed("sprint")
	
	# Jumping
	input.is_jumping = Input.is_action_just_pressed("ui_accept")
	
	# Camera look (yaw/pitch)
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		var mouse_delta = get_viewport().get_mouse_velocity()
		input.yaw = rotation.y - (mouse_delta.x * 0.005)
		input.pitch = rotation.x - (mouse_delta.y * 0.005)
	else:
		input.yaw = rotation.y
		input.pitch = rotation.x
	
	return input

func apply_server_state(state: Dictionary) -> void:
	"""Update player state from server"""
	if state.get("playerId") == player_id:
		position = Vector3(state.get("positionX"), state.get("positionY"), state.get("positionZ"))
		rotation.y = state.get("yaw")
		rotation.x = state.get("pitch")
		health = state.get("health")
		is_alive = state.get("isAlive")
		kills = state.get("kills")
		deaths = state.get("deaths")

func perform_attack(target: Player) -> void:
	"""Request attack on target (server validates)"""
	var now = Time.get_ticks_msec() / 1000.0
	if now - _last_attack_time < _attack_cooldown:
		return
	
	if not is_alive or not target.is_alive:
		return
	
	var distance = global_position.distance_to(target.global_position)
	if distance > attack_range:
		return
	
	_last_attack_time = now
	network_manager.send_combat_input(target.player_id, distance)
	_play_attack_animation()

func take_damage(damage: float) -> void:
	"""Called by server to apply damage"""
	health -= damage
	if health <= 0:
		health = 0
		is_alive = false
		_play_death_animation()
	else:
		_play_hit_animation()

func respawn() -> void:
	"""Respawn player"""
	health = max_health
	is_alive = true
	_play_spawn_animation()

func _play_attack_animation() -> void:
	if animation_player:
		animation_player.play("attack")

func _play_hit_animation() -> void:
	if animation_player:
		animation_player.play("hit")

func _play_death_animation() -> void:
	if animation_player:
		animation_player.play("death")

func _play_spawn_animation() -> void:
	if animation_player:
		animation_player.play("spawn")

func _on_arena_state_received(state: Dictionary) -> void:
	"""Handle arena state updates from server"""
	if not is_alive:
		return
	
	var players = state.get("players", [])
	for player_state in players:
		if player_state.get("playerId") == player_id:
			apply_server_state(player_state)

func _on_hit_validation(validation: Dictionary) -> void:
	"""Handle hit validation from server"""
	var is_valid = validation.get("isValid")
	var is_attacker = validation.get("attackerId") == player_id
	var is_target = validation.get("targetId") == player_id
	
	if is_valid:
		if is_target:
			var damage = validation.get("damage")
			take_damage(damage)

func _on_player_died(data: Dictionary) -> void:
	"""Handle player death"""
	var dead_player_id = data.get("playerId")
	var killer_id = data.get("killerId")
	
	if dead_player_id == player_id:
		deaths += 1
		is_alive = false
		_play_death_animation()
