# Cactus Town Mobile — Design

Casual game about caring for a plant and restoring a town.
**Landscape**, C#, Godot 4.4.1. Every character in the game is a cactus in a pot
(the player and all NPCs).

## Core loop

1. **House (hub / main menu)** — customise your plant, play mini-games, view plant stats.
2. **Town** — top-down isometric 2D. Player moves around, talks to NPCs.
3. NPCs give **requests**: fix broken / worn-down objects in the town.
4. Completing a request → **coins** + the object becomes fixed.
5. A fixed object can be **customised** (street lamp colour, fountain design, …).
6. Coins spend at **shops** on plant + town customisations.
7. Fixed objects have a chance to **decay** back to a worn state → follow-up repair request → more coins.
8. **Regions** (reached from the town) — gather the **materials** needed to complete repairs.

## Subsections

Six, reached from the town map, gated by an unlock chain (complete one for the
first time to open the next):

- **Main Square** (open) → Garden, Shopping District
- **Garden** → Park
- **Shopping District** → Housing → Business Quarter

Each has 4–6 worn objects; a cactus by each gives the repair task. A section is
**complete** when every object is fixed → its objects become customisable, its
first completion unlocks the next section(s), and daily decay begins.

### Decay
Reference clock: **US Eastern (fixed UTC-5)**. For each completed section, on a
new EST day: 1 object breaks, 25% chance 2, 5% chance 3. Broken objects reopen
their repair task. (One catch-up event per return, not per day missed.)

### Object customisation
After a section is complete, interact with a fixed object → styling picker.
Index 0 is the original (free); further styles cost coins (50, 100, … —
extensible per object) and swap the object's sprite when selected.

## Entities & state

### Plant
- Cosmetic customisation (pot, species look, accessories).
- Stats shown in the house. *(Open: are stats a live care mechanic with decay over
  time, or cosmetic/progress readouts? — TBD)*

### Repairable object
- `id`, `area_id`, `type` (lamp, fountain, bench, …)
- `state`: `broken` | `fixed`
- `required_materials`: { material_id: count }
- `customisation`: chosen variant once fixed (colour / design)
- `decay_chance`: per-visit roll to revert `fixed` → `broken`

### Town area / subsection
- `id`, `display_name`, `unlocked`
- `objects`: list of repairable object ids
- `complete` = every object `fixed`

### Regions (4)
Reached from the town. Each has its own gatherable materials.

| Region | Materials | Gathering |
|---|---|---|
| **Forest** | wood (from trees), sticks (from the ground) | chop trees / pick up |
| **Flower Field** | flowers (several kinds) | pick |
| **River** | water (buckets, from the river), stones (from the ground) | scoop / pick up |
| **Cave** | ores — iron, gold (more later) | mine rocks |

- `id`, `display_name`, `unlocked`
- Contains resource nodes; each node yields a material, may have a respawn timer.
- Exact gathering feel (tap, hold, timed minigame, energy?) — TBD.

### Player wallet & inventory
- `coins`
- `materials`: { material_id: count }
- `owned_customisations`: purchased variants for plant + town objects

## Screen flow

```
House ──▶ Town ──▶ Town Subsection (repair requests, customise)
  │         │
  │         └─▶ Region (gather materials)
  │
  ├─ Customise Plant
  ├─ Mini-games
  └─ Plant Stats
```

## Decisions made

- Town render: top-down free 2D movement with iso-styled art + Y-sort (not a real iso TileMap).
- Plant stats: cosmetic / progress readout, no live-decay care mechanic.
- Orientation: landscape. Language: C#.
- Gathering: a **mini-game per region** (`ResourceNode` launches it, `MiniGame` base).
  Forest = timing bar, Flower Field = button mash, River = hold-and-release,
  Cave = tap-the-target. No hard fail — leaving early pays out proportionally.
- Requests **consume materials**: `RequestPanel` shows a checklist, "Fix it" locked
  until the player has them, consumed on completion.

## Open questions

- Save: single slot, autosave on every mutation (current behaviour) — confirm that's fine.
- Region unlocking — all open from the start now; should some be gated behind town progress?
- Energy / stamina limit on gathering, or unlimited?
- Mini-games (the House pillar) — separate from gathering games; how many, what kind?
