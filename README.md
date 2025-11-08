# Spooky Nights

*Halloween comes to your Vintage Story world!*

Get ready for a unique and thrilling gameplay experience with **Spooky Nights**, the mod that turns your world into a real Halloween playground! Specters are coming, and most importantly, monsters now hide delicious treats with surprising powers. Will you be brave enough to face the darkness and collect the sweet loot?

---

## Main Features

### New Spectral Creatures
The world of Vintage Story will never be the same at night! Spectral versions of familiar creatures appear, emitting a sinister, colorful glow in the dark. Thanks to the v0.2 overhaul, these are no longer simple variations, but **entirely new entities** with their own characteristics.
*   Spectral Drifters (Surface, Deep, Tainted, Corrupt, Nightmare, Double-headed)
*   Spectral Bow-Torns (Surface, Deep, Tainted, Corrupt, Nightmare, Gearfoot)
*   Spectral Shivers (Surface, Deep, Tainted, Corrupt, Nightmare, Bellhead, Deepsplit, Stilt)
*   Spectral Wolves and Bears (including a mini-boss, the Giant Bear)

### Halloween Candies with Unique Powers
The candy loot system is **disabled by default** now that Halloween is over, in order to be reworked and rebalanced. However, you can easily re-enable it via the `EnableCandyLoot` option in the mod's config file!
*   **Mummy (Common):** Restores **40 satiety**.
*   **Spider Gummy (Uncommon):** A very nourishing treat that restores **60 satiety**.
*   **Ghostly Caramel (Uncommon+):** Restores **40 satiety** and instantly heals **0.5 health**.
*   **Vampire Fangs (Rare):** Restores **40 satiety** and heals **1 health**.
*   **Shadow Cube (Very Rare):** No effect for now. Once the candy system is reworked, it will grant a random effect (buff or debuff).

### Full Customization via Configuration
The mod is highly customizable through the `spookynights-server.json` and `spookynights-client.json` files. You can now:

#### Time-Based Spawning (`UseTimeBasedSpawning`)
Control whether spectral creatures only spawn at certain times. You can restrict spawns to night, specific months, the last day of the week, or the last day of the month.

#### Spawn Multipliers (`SpawnMultipliers`)
This dictionary acts as a filter to approve or deny the game's natural spawn attempts for each creature type.
*   **Values from `1.0` and above**: Guarantees that any spawn attempt made by the game will be approved. A value of `2.0` behaves identically to `1.0`.
*   **Values between `0.0` and `1.0`**: Represents a percentage chance to approve a spawn attempt. For example, `0.5` means there is a 50% chance the creature will be allowed to spawn.
*   **Values of `0.0` or less**: Completely disables this creature from spawning.

#### Boss Spawning (`Bosses`)
This powerful system lets you define special spawning rules for "boss" creatures. A boss can be any creature you choose, even from the base game or other mods!
*   The key is the entity code (e.g., `spookynights:spectralbear-giant-*`).
*   `"Enabled": true`: Activates the special rules. If `false`, the creature is prevented from spawning via this system.
*   `"AllowedMoonPhases": [ "full" ]`: A list of moon phases during which the boss can spawn.

#### Client-Side Settings
*   **Jack O' Lantern Particles**: You can enable or disable the decorative smoke particles from Jack O' Lanterns in the `spookynights-client.json` file.

### Crafting and Decorations
To make your base as festive as the rest of the world!
*   **Jack O' Lantern:** The essential Halloween decoration! A carved pumpkin that emits light, craftable via a recipe.
*   **Spiderweb:** The game's spiderweb block is now craftable with 4 flax fibers and 1 flax twine.

---

## Advanced Usage: Create Your Own Bosses!

The "Bosses" configuration system is designed to be a powerful tool for server admins and modpack creators. Because it only handles **spawn conditions**, you can combine it with JSON patches to create fully custom boss encounters.

#### Step 1: Create a Stronger Creature with a Patch
Use a standard Vintage Story JSON patch to modify a creature's stats. For example, you could create a patch to give a Corrupt Drifter 500 health.

```json
[
  {
    "file": "game:entities/land/drifter.json",
    "op": "add",
    "path": "/server/behaviors/1/health/maxhealthByType/drifter-corrupt",
    "value": 500,
    "side": "Server"
  }
]
```

#### Step 2: Control Its Spawn with Spooky Nights
Now, go into `spookynights-server.json` and add an entry for that creature to make it only spawn during the new moon.

```json
"Bosses": {
  "game:drifter-corrupt": {
    "Enabled": true,
    "AllowedMoonPhases": [ "new" ]
  }
}
```
By combining these two steps, you can turn any creature from any mod into a unique boss with its own special appearance rules!

---

## Candy Hunting Guide
Candies aren't found just anywhere! Only **spectral creatures** drop these precious treats. The more dangerous the monster, the better your chances of getting rare candies!

| Entities & Difficulty Level | Mummy (Common) | Spider Gummy (Uncommon) | Ghostly Caramel (Uncommon+) | Vampire Fangs (Rare) | Shadow Cube (Very Rare) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Tier 1**<br>(Normal, Surface) | ~80% | ~20% | ~15% | ~8% | ~5% |
| **Tier 2**<br>(Deep, Tainted) | 100% (1-2) | ~50% | ~30% | ~15% | ~10% |
| **Tier 3**<br>(Corrupt, Nightmare)| 100% (2) | ~80% | ~60% | ~25% | ~30% |
| **Tier 4 (Elites)**<br>(Double-headed, etc.) | 100% (3-4) | 100% (2) | ~75% | ~50% | ~40% |

---

## Roadmap (Upcoming Updates)
The development of **Spooky Nights** continues! Here is a preview of what is planned for the next major updates (potentially v1.0.0):
*   **New Hunting Mechanic:** To make specter hunting more engaging, you will be able to craft **special weapons** that are much more effective against spectral creatures.
*   **Loot System Overhaul:** The current loot (mostly gears) will be completely rethought. The goal is to provide unique and useful rewards that will fully justify hunting spectral entities, beyond the Halloween season.

---

## Latest Updates

**v0.2.0 (2025-11-06)**
- Added: Configuration file for advanced mod customization.
- Added: Spectral creatures are now unique entities, not just patches on vanilla mobs.
- Added: Option to enable/disable pumpkin smoke via config.
- Added: Option to enable/disable the CandyLoot system via config.
- Changed: Overhauled the loot system.
- Changed: Overhauled the spawn system (now based on a defined period).
- Refactor: Generalized the boss spawning system to be data-driven, allowing for multiple, user-configurable bosses.
- Docs: Updated documentation to clarify the behavior of `SpawnMultipliers` and to explain the powerful new boss system.

**v0.2-dev1 (2025-10-31)**
- added: New Candy Bag Interaction: You now hold the right mouse button to open a candy bag.
- added: Patch for remapping candy and candybag items. All old candybags cannot be remapped to new candybag, so you SHOULD OPEN THEM before if you don't want to lose them.
- changed: Candy Bag Rework: All previous candy bag variants have been merged into a single, generic candybag item. Each bag now drops a random amount (1-3) of a random candy type upon being opened.