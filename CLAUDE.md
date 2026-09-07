# Cactus Town Mobile

A 2D mobile game built in **Godot 4.4.1 (mono build)**, **C#**.
Target: Android, **landscape**, 1920×1080 design resolution. iOS export is not
possible from this Windows machine.

## Engine / tooling

- Godot editor: `C:\Users\alann\OneDrive\Desktop\Games\Sources\Godot_v4.4.1-stable_mono_win64\Godot_v4.4.1-stable_mono_win64.exe`
- .NET SDK 8/9/10 installed (`C:\Program Files\dotnet\dotnet.exe`). Godot targets **net8.0**.
- Build C# before running: `dotnet build CactusTownMobile.csproj` (or let the editor build).
  `CactusTownMobile.csproj` is engine-managed — Godot rewrites it; don't hand-edit beyond PropertyGroup basics.
- The project lives inside OneDrive. If the import cache corrupts or files lock
  mid-edit, pause OneDrive sync for this folder (or move it to `C:\dev\`).

## Language — C#

- All gameplay code is C#. Namespace `CactusTown`. `<Nullable>enable</Nullable>` —
  respect it (`null!` for node fields set in `_Ready`, `?` where genuinely nullable).
- One `.cs` file per class, file name = class name.
- Godot idioms:
  - `[Export] public float Foo = 1f;` for inspector fields.
  - `[Signal] public delegate void ThingHappenedEventHandler(int value);`
    emit with `EmitSignal(SignalName.ThingHappened, value)`, subscribe with the
    generated C# event: `node.ThingHappened += Handler;` (unsubscribe in `_ExitTree`).
  - `GetNode<T>("%UniqueName")` / `GetNode<T>("Path")`.
  - Autoloads accessed via their static `Instance` (`GameState.Instance`, `Router.Instance`).
- Connect signals in code, not the editor.

## Conventions

- **Files**: scenes `PascalCase.tscn`; scripts `PascalCase.cs` matching the class /
  scene root node.
- **Nodes**: `PascalCase` in the scene tree.
- **C#**: `PascalCase` members/methods, `_camelCase` private fields, `const`/`static readonly` for constants.
- **Signals**: past-tense events — `Died`, `HealthChanged`, `PlayerEntered`.
- One `_Ready()` responsibility per node; shared logic → autoloads or components.

## Project layout

```
scenes/    composed .tscn files (levels, entities, screens); scenes/dev/ is dev-only
scripts/   .cs attached to scenes; scripts/globals/ autoloads; scripts/entities/; scripts/dev/
ui/        HUD, menus, overlays (.tscn + .cs)
assets/    sprites/  audio/  fonts/   (imported art — keep source-of-truth here)
design/    DESIGN.md + reference mockups pasted into chat, saved for context
```

## Autoloads / globals

- `GameState` (`scripts/globals/GameState.cs`) — wallet, materials, plant, town
  object repair-state; single JSON save slot at `user://cactus_town_save.json`
  with forward-migration. Signals: `CoinsChanged`, `MaterialsChanged`, `PlantChanged`.
- `Router` (`scripts/globals/Router.cs`) — `GotoScene(path)` fade transition,
  `Toast(msg)`. `ProcessMode = Always` so it works while the tree is paused.

## Mobile specifics

- Landscape. Stretch mode `canvas_items`, aspect `expand`. Build UI with
  anchors/containers, never absolute positions.
- Input: touch is emulated from mouse in-editor. Use `_Input` /
  `InputEventScreenTouch` / `InputEventScreenDrag`; register actions in the
  Input Map rather than hardcoding keys.
- Renderer is `mobile`. Avoid features that force the `forward_plus` backend.
- Modal dialogues pause the tree (`GetTree().Paused = true`); give always-on
  nodes `ProcessMode = WhenPaused`/`Always` as needed.

## Git

- Default branch `main`. Feature branches `feature/<slug>`, `fix/<slug>`.
- `.godot/`, `bin/`, `obj/`, `export_presets.cfg` are gitignored — never force-add.
- `CactusTownMobile.csproj` and any generated `.sln` ARE committed.
- Commit messages: imperative summary, why in the body when non-obvious.

## Testing / running

- Close the Godot editor before running CLI commands (OneDrive + a held import
  lock is the one thing that bites here).
- Build + import + error check:
  `dotnet build CactusTownMobile.csproj`
  `godot --headless --import --path .`
  `godot --headless --path . scenes/<Scene>.tscn --quit-after 3`
- **Screenshots** (windowed render, autoloads + C# active) — dev-only:
  `godot --path . scenes/dev/Screenshot.tscn --resolution 1920x1080 -- res://scenes/Town.tscn <out.png> [frames] [demo_talk]`
- No unit-test framework yet. If one is added (GUT / GdUnit4 / xUnit), note the command here.
