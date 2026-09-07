# Cactus Town Mobile

A 2D mobile game made with Godot 4.4.1.

## Requirements

- Godot 4.4.x **mono** build
- (Later, for Android export) Android SDK + build tools, a debug keystore, and
  Godot's export templates for 4.4.1

## Running

Open `project.godot` in the Godot editor, or from the CLI:

```bash
"C:\Users\alann\OneDrive\Desktop\Games\Sources\Godot_v4.4.1-stable_mono_win64\Godot_v4.4.1-stable_mono_win64.exe" --path .
```

## Layout

| Folder     | Contents                                        |
|------------|-------------------------------------------------|
| `scenes/`  | Composed scenes (levels, entities, screens)     |
| `scripts/` | GDScript attached to scenes + logic modules     |
| `ui/`      | HUD, menus, overlays                            |
| `assets/`  | Sprites, audio, fonts                           |
| `design/`  | Reference mockups                               |

See [CLAUDE.md](CLAUDE.md) for coding conventions.
