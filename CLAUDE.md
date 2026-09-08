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

## Town subsections

- `scenes/TownMap.tscn` (`TownMap.cs`) is the hub — House → here. Buttons built
  from `TownSections.All`; locked until their `UnlockedBy` section is completed once.
- Section scenes `scenes/town/*.tscn` use `TownSection.cs` (same shape as `Region`):
  `%Player`, `%VirtualJoystick`, `%ActionButton`, `%BackButton`, `%RepairPanel`,
  `%CustomizePanel`, `MaterialsHud`, `CoinHud`, and a Y-sorted `%World` of
  `RepairableObject` instances.
- `RepairableObject` (`scenes/entities/RepairableObject.tscn`) — data-driven:
  `ObjectId`, `DisplayName`, `Variants` (Array[Texture2D]; [0] free, rest cost
  `VariantCosts`), `RequiredMaterials`, `RepairReward`, request/thanks text,
  `SpriteOffset`. Worn = default sprite tinted grimy + a cactus NPC beside it.
- **Adding a section**: entry in `TownSections.All` (id, name, `UnlockedBy`,
  `SceneFile`, `ObjectIds`) + a scene from the pattern above. `ObjectIds` must
  match the scene's `RepairableObject.ObjectId`s (decay reads the config).
- **Decay**: `GameState.RunTownDecay()` (called from `GameState._Ready`, `TownMap`,
  and each section `_Ready`). For every section completed once, on a new EST day
  (`EstToday()`, fixed UTC-5) one fixed object breaks — 25% two, 5% three. One
  catch-up event no matter how many days passed.

## Plant customisation & stats

- `PlantCatalog` (`scripts/globals/PlantCatalog.cs`) — every pot / plant / accessory
  item (id, slot, name, texture, cost, and for accessories an `Anchor`). Add an
  entry + a sprite in `assets/sprites/plant/` to add an option.
- `ui/PlantView.tscn` (`PlantView.cs`) — composites pot + plant + accessory from
  `GameState`; self-updates on `PlantChanged`. Accessory anchors ("hat", "face",
  "neck", "pot", "aura") are pixel positions in `PlantView.AnchorCentre` — tuned
  for the default cactus, approximate for other species.
- `scenes/PlantCustomize.tscn` — Pot / Plant / Accessory tabs, buy + equip cards.
- `scenes/PlantStats.tscn` — rename field + a code-built stat list (section
  progress, coins, days played, materials, customisations unlocked).
- `GameState`: `PlantName/PlantPot/PlantSpecies/PlantAccessory`, `EquipPlantItem`,
  `OwnsPlantItem/BuyPlantItem`, `SetPlantName`, `DaysOnApp`, `SectionProgress`,
  `TownCustomizationsUnlocked`, `PlantCustomizationsUnlocked`. `first_day` (EST)
  is stamped in `DefaultData`. Plant data shape is now
  `plant: {name, pot, plant, accessory, owned[]}`.

## House arcade mini-games

- `scenes/ArcadeGallery.tscn` (`ArcadeGallery.cs`) — House → Mini-Games. A
  scrolling card grid built from `ArcadeCatalog.All`; pick a card → difficulty
  panel (Easy/Medium/Hard = 1/2/3 coin reward) → the game loads into `%GameHost`.
- Games extend `ArcadeGame` (`scripts/arcade/`): `Configure(difficulty)` before
  entering the tree; call `ReportResult(1|0|-1)` when a round ends (a win pays
  `Difficulty` coins there) and `Close()` when the player leaves.
- End-of-round overlay: instance `ui/ArcadeResult.tscn`, wire its `PlayAgain` /
  `Leave` signals, call `ShowResult(result, Difficulty)`.
- **Adding a game**: entry in `ArcadeCatalog.All` (id, name, `SceneFile`,
  `IconFile`) + a scene extending `ArcadeGame` + a ~200px icon at
  `assets/sprites/arcade/<IconFile>.svg`. The grid grows on its own; unbuilt
  entries show "(soon)".
- Built: Tic-Tac-Toe (minimax AI), Pong (speed-scaled AI paddle), Match the
  Pairs (AI with difficulty-capped memory), Minesweeper (solo; size/mines scale).
- Games run standalone for dev, so give board-size fields real default values
  (Configure overrides them).

## Regions & gathering

- `scenes/regions/*.tscn` each use `Region.cs`: a `World` (Y-sorted) with a
  `Player` instance + `ResourceNode` instances, and a `UI` CanvasLayer
  (`%VirtualJoystick`, `%BackButton`, `%GatherButton`, `MaterialsHud`).
- `ResourceNode` (`scenes/entities/ResourceNode.tscn`) is fully data-driven via
  exports — set `MaterialId`, `YieldAmount`, `Difficulty`, `HarvestsUntilDepleted`,
  `RespawnSeconds`, `NodeTexture`, `SpriteOffset`, `MiniGameScene`.
- Mini-games extend `MiniGame` (pauses the tree; root `process_mode = 3`), get
  `Configure(title, reward, difficulty)` before entering the tree, and call
  `Finish(success, amount)`. To add one: new `.cs` + `.tscn`, point a
  `ResourceNode.MiniGameScene` at it.
- To add a region: new scene from the pattern above + wire its button in `RegionSelect.cs`.

## Autoloads / globals

- `GameState` (`scripts/globals/GameState.cs`) — wallet, materials, plant, town
  object repair-state; single JSON save slot at `user://cactus_town_save.json`
  with forward-migration. Signals: `CoinsChanged`, `MaterialsChanged`, `PlantChanged`.
- `Router` (`scripts/globals/Router.cs`) — `GotoScene(path)` fade transition,
  `Toast(msg)`. `ProcessMode = Always` so it works while the tree is paused.
- `TownSections` (`scripts/globals/TownSections.cs`) — static config of the six
  subsections (name, unlock chain, object ids, scene).
- `Materials` (`scripts/globals/Materials.cs`) — material id constants + names.

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
