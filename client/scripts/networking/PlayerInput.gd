## Represents player input data
class_name PlayerInput

var move_x: float = 0.0
var move_z: float = 0.0
var yaw: float = 0.0
var pitch: float = 0.0
var is_jumping: bool = false
var is_sprinting: bool = false

func _init(p_move_x: float = 0.0, p_move_z: float = 0.0, p_yaw: float = 0.0, p_pitch: float = 0.0, p_jumping: bool = false, p_sprinting: bool = false) -> void:
	move_x = p_move_x
	move_z = p_move_z
	yaw = p_yaw
	pitch = p_pitch
	is_jumping = p_jumping
	is_sprinting = p_sprinting
