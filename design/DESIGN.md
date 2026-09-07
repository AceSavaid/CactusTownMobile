# Cactus Town Mobile — Design

Casual game about caring for a plant and restoring a town.

## Core loop

1. **House (hub / main menu)** — customise your plant, play mini-games, view plant stats.
2. **Town** — top-down isometric 2D. Player moves around, talks to NPCs.
3. NPCs give **requests**: fix broken / worn-down objects in the town.
4. Completing a request → **coins** + the object becomes fixed.
5. A fixed object can be **customised** (street lamp colour, fountain design, …).
6. Coins spend at **shops** on plant + town customisations.
7. Fixed objects have a chance to **decay** back to a worn state → follow-up repair request → more coins.
8. **Regions** (reached from the town) — gather the **materials** needed to complete repairs.

## Areas

- The town has several **subsections**, each travelled to from the town.
- Each subsection has its own set of **repairable objects**.
- An area is **complete** when all its objects are fixed → unlocks customisation for that area's objects.

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

### Region
- `id`, `display_name`, `unlocked`
- yields a set of materials (gathering minigame / node harvesting — TBD)

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

## Open questions

- Isometric: true isometric TileMap (diamond) vs 2.5D top-down with iso-styled art on a square grid?
- Plant stats: live care mechanic (water/light decay in real time) or cosmetic?
- Save: single slot, autosave on scene change (assumed) — confirm.
- Regions: how is gathering done — timed minigame, tap-to-harvest nodes, energy system?
- Mini-games: how many at launch, what kind?
