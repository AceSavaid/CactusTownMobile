# Cactus Town Mobile

A 2D mobile game built in **Godot 4.4.1 (mono build)**, GDScript.
Target: Android (portrait, 1080×1920 design resolution). iOS export is not
possible from this Windows machine.

## Engine / tooling

- Godot editor: `C:\Users\alann\OneDrive\Desktop\Games\Sources\Godot_v4.4.1-stable_mono_win64\Godot_v4.4.1-stable_mono_win64.exe`
- Run headless / import from CLI with `--headless`, run a scene with `--path . scenes/Foo.tscn`.
- The project lives inside OneDrive. If the import cache corrupts or files lock
  mid-edit, pause OneDrive sync for this folder (or move it to `C:\dev\`).

## Language

- **GDScript** by default. C# is available (mono build) but avoid mixing unless
  a feature clearly needs it — flag it first.
- Use **typed GDScript**: `var speed: float = 0.0`, typed params and return types,
  `-> void` on functions that return nothing.
- `class_name` only when the type is referenced from other scripts or the editor.

## Conventions

- **Files**: scenes `PascalCase.tscn`, scripts `PascalCase.gd` matching their
  root node. Reusable non-node scripts `snake_case.gd`.
- **Nodes**: `PascalCase` in the scene tree.
- **Vars / funcs**: `snake_case`. **Constants / enums**: `CONSTANT_CASE`.
- **Signals**: named as past-tense events — `died`, `health_changed`,
  `wave_completed`. Connect in code, not the editor, unless it's purely visual.
- **Private** members prefixed `_`.
- One `_ready()` responsibility per node; push shared logic into autoloads or
  components.

## Project layout

```
scenes/    composed .tscn files (levels, entities, screens)
scripts/   .gd attached to scenes, plus pure logic modules
ui/        HUD, menus, overlays
assets/    sprites/  audio/  fonts/   (imported art — keep source-of-truth here)
design/    reference screenshots / mockups pasted into chat, saved for context
```

## Autoloads / globals

None yet. When added, document each here with its responsibility (e.g.
`GameState` — run progress & save data; `Audio` — bus + one-shot SFX).

## Mobile specifics

- Stretch mode `canvas_items`, aspect `expand`. Build UI with anchors/containers,
  never absolute positions.
- Input: touch is emulated from mouse in-editor. Use `_input` /
  `InputEventScreenTouch` / `InputEventScreenDrag`; register actions in the
  Input Map rather than hardcoding keys.
- Renderer is `mobile`. Avoid features that force the `forward_plus` backend.
- Keep textures power-of-two friendly; ETC2/ASTC compression is on.

## Git

- Default branch `main`. Feature branches `feature/<slug>`, `fix/<slug>`.
- `.godot/` and `export_presets.cfg` are gitignored — never force-add them.
- Commit messages: imperative summary, why in the body when non-obvious.

## Testing / running

- Ask before assuming the game runs — this machine can import/run via CLI but
  confirm the editor isn't holding a lock.
- No test framework yet. If one is added (GUT / GdUnit4), note the command here.
