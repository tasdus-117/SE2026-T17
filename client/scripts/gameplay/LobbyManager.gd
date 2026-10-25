## Manages lobby UI and interactions
extends Control

class_name LobbyManager

var network_manager: NetworkManager
var _waiting_players: Array[Dictionary] = []
var _player_ready: bool = false

@onready var player_list = $VBoxContainer/PlayerList
@onready var ready_button = $VBoxContainer/ReadyButton
@onready var player_count_label = $VBoxContainer/PlayerCountLabel

func _ready() -> void:
	network_manager = get_tree().root.get_child(0).get_node("NetworkManager")
	network_manager.lobby_updated.connect(_on_lobby_updated)
	
	ready_button.pressed.connect(_on_ready_toggled)

func _on_lobby_updated(data: Dictionary) -> void:
	_waiting_players = data.get("players", [])
	_update_player_list()
	
	var total = data.get("totalPlayers", 0)
	var ready = data.get("readyPlayers", 0)
	player_count_label.text = "Players: %d/4 (Ready: %d)" % [total, ready]
	
	if data.get("canStart"):
		print("Match can start!")

func _on_ready_toggled() -> void:
	_player_ready = not _player_ready
	
	if _player_ready:
		network_manager.ready_up()
		ready_button.text = "Not Ready"
		ready_button.modulate = Color.GREEN
	else:
		network_manager.not_ready()
		ready_button.text = "Ready"
		ready_button.modulate = Color.WHITE

func _update_player_list() -> void:
	# Clear existing items
	for child in player_list.get_children():
		child.queue_free()
	
	# Add players
	for player in _waiting_players:
		var item = Label.new()
		var name_str = player.get("playerName", "Unknown")
		var ready_str = "✓ Ready" if player.get("isReady") else "✗ Not Ready"
		item.text = "%s - %s" % [name_str, ready_str]
		player_list.add_child(item)
