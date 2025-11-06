### Spooky Nights Mod Configuration Guide

Welcome! This guide explains how to customize your Spooky Nights experience using its configuration files.

To provide maximum flexibility, the mod uses two separate configuration files:
*   `spookynights-server.json`: For gameplay settings (loot, monster spawning, etc.). These rules apply to **all players** on a server.
*   `spookynights-client.json`: For personal cosmetic settings (visual effects, etc.). These choices only affect **your game**.

#### Where to Find the Configuration Files

The configuration files are created automatically the first time you launch the game with the mod. You can find them in the `ModConfig` folder of your Vintage Story installation.

The typical path is: `C:\Users\YourName\AppData\Roaming\VintagoryData\ModConfig`

#### Golden Rules for Editing JSON Files

Before you change anything, here are two very important rules to keep the files valid:
1.  **No Comments**: The JSON format does not allow comments. Do not add lines starting with `//` or `#`.
2.  **Watch Your Commas**: Every line must end with a comma, **except for the very last line in a block**. A missing or extra comma is the most common error.

*Example:*
```json
{
  "option_1": true,   <-- Comma here
  "option_2": false   <-- NO comma here because it's the last line
}
```

---

### Server File: `spookynights-server.json`

This file controls the game's logic and rules. In single-player mode, you are your own server.

#### General Options

*   `"EnableCandyLoot": true`
    Enables (`true`) or disables (`false`) the chance for spectral creatures to drop candy bags upon death.

*   `"UseTimeBasedSpawning": true`
    This is the master switch for all time-based spawning rules. If set to `false`, the options below (`SpawnOnlyAtNight`, `AllowedSpawnMonths`, etc.) will be ignored.

*   `"SpawnOnlyAtNight": true`
    If `true`, spectral creatures will only spawn at night (between 8 PM and 6 AM). If `false`, they can spawn at any time. This also enables the "daylight burning" feature for spectral creatures.

*   `"AllowedSpawnMonths": []`
    A list of months during which spectral creatures can spawn. Months are numbered 1 (January) through 12 (December). By default, this list is empty, which allows creatures to spawn **all year round**. To restrict spawning to specific months, add their numbers to the list, separated by commas.
    *   *Example: To allow spawns only in October, November, and December, use `[10, 11, 12]`.*

*   `"SpawnOnlyOnLastDayOfMonth": false`
    If `true`, creatures will only spawn on the last day of the current month.

*   `"SpawnOnlyOnLastDayOfWeek": false`
    If `true`, creatures will only spawn on the last day of the week (which is 7 days long in-game).

#### Bear (Boss) Spawning

*   `"BearSpawnConfig"`
    Controls the spawning of spectral Giant Bear.
    *   `"Enabled": true`: Enables (`true`) or disables (`false`) the special rules for bears.
    *   `"AllowedMoonPhases": [ "full" ]`: A list of moon phases during which spectral bears are allowed to spawn.
        *   Possible phases: `"new"`, `"waxingcrescent"`, `"firstquarter"`, `"waxinggibbous"`, `"full"`, `"waninggibbous"`, `"thirdquarter"`, `"waningcrescent"`.

#### Spawn Multipliers (`SpawnMultipliers`)

This dictionary controls the spawn frequency of each creature type.
*   `1.0`: Normal frequency (100% of base chance).
*   `0.5`: Half frequency (50% chance).
*   `2.0`: Double frequency (200% chance).
*   `0.0`: Completely disables this creature from spawning.

*Example:*
```json
"SpawnMultipliers": {
  "spookynights:spectralwolf-*": 0.5,  // Wolves are twice as rare
  "spookynights:spectralbear-*": 0.0   // Bears will never spawn
}
```

#### Loot Table (`CandyLootTable`)

This controls the drop chance and quantity of candy bags for each creature.
The format is `"CHANCE@MIN-MAX"`.
*   **CHANCE**: A probability from `0.0` (0%) to `1.0` (100%).
*   **MIN**: The minimum quantity of bags to drop.
*   **MAX**: The maximum quantity of bags. If MAX is not provided, the quantity will always be equal to MIN.

*Example:*
```json
"CandyLootTable": {
  "spookynights:spectraldrifter-normal": "0.2@1",       // 20% chance to drop 1 bag.
  "spookynights:spectraldrifter-nightmare": "0.6@3-5"   // 60% chance to drop between 3 and 5 bags.
}
```

---

### Client File: `spookynights-client.json`

This file controls visual options that only affect your game. It does not change anything for other players.

*   `"EnableJackOLanternParticles": true`
    Controls the smoke and ember particle effects coming from Jack o'Lanterns. Set this to `false` if you want to improve performance or simply don't like the effect.

---

**Important:** After editing and saving a configuration file, you must **restart your game (or server)** for the changes to take effect.