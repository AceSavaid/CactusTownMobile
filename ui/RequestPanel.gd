extends Control

## Modal request dialogue. Feed it an Npc; it shows the greeting + request,
## lets the player help out (which completes the request and pays coins),
## then shows the thank-you line.

signal closed

var _npc: Npc = null

@onready var _name: Label = %NpcName
@onready var _body: Label = %Body
@onready var _accept: Button = %AcceptButton
@onready var _dismiss: Button = %DismissButton


func _ready() -> void:
	hide()
	_accept.pressed.connect(_on_accept)
	_dismiss.pressed.connect(close)


func open(npc: Npc) -> void:
	_npc = npc
	_name.text = npc.npc_name
	if npc.is_done():
		_show_thanks()
	else:
		_body.text = "%s\n\n%s" % [npc.greeting, npc.request_text]
		_accept.text = "Help out  (+%d coins)" % npc.reward_coins
		_accept.show()
		_dismiss.text = "Not now"
	show()
	get_tree().paused = true


func _on_accept() -> void:
	if _npc == null:
		return
	_npc.complete_request()
	Router.toast("Request complete!  +%d coins" % _npc.reward_coins)
	_show_thanks()


func _show_thanks() -> void:
	_body.text = _npc.thanks_text
	_accept.hide()
	_dismiss.text = "Close"


func close() -> void:
	hide()
	_npc = null
	get_tree().paused = false
	closed.emit()
