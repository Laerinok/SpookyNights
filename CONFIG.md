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

### Boss Spawning

*   `"Bosses"`
    Controls special spawning rules for creatures designated as bosses. This is an object where each key is the entity code of a boss (wildcards `*` are supported), and the value contains its specific spawning rules.

    *   **Example Key**: `"spookynights:spectralbear-giant-*"`
        This key targets the giant spectral bear. Inside this key, you define its specific rules:
        *   `"Enabled": true`: Enables (`true`) or disables (`false`) the special rules for this specific boss. If set to `false`, this boss will be prevented from spawning by this system.
        *   `"AllowedMoonPhases": [ "full" ]`: A list of moon phases during which this boss is allowed to spawn. If the list is empty, the moon phase check is ignored for this boss.
            *   Possible phases: `"new"`, `"waxingcrescent"`, `"firstquarter"`, `"waxinggibbous"`, `"full"`, `"waninggibbous"`, `"thirdquarter"`, `"waningcrescent"`.

#### Example Configuration

You can add multiple entries to the `"Bosses"` object to configure different bosses, each with their own set of rules.

```json
"Bosses": {
  "spookynights:spectralbear-giant-*": {
    "Enabled": true,
    "AllowedMoonPhases": [
      "full"
    ]
  },
  "spookynights:some-other-future-boss-*": {
    "Enabled": true,
    "AllowedMoonPhases": [
      "new",
      "waningcrescent"
    ]
  }
}
```


#### Spawn Multipliers (`SpawnMultipliers`)

This dictionary acts as a filter to approve or deny the game's natural spawn attempts for each creature type.

*   **Values from `1.0` and above**: Guarantees that any spawn attempt made by the game will be approved. A value of `2.0` behaves identically to `1.0`. This is the default behavior.
*   **Values between `0.0` and `1.0`**: Represents a percentage chance to approve a spawn attempt. For example, `0.5` means there is a 50% chance the creature will be allowed to spawn when the game tries.
*   **Values of `0.0` or less**: Completely disables this creature from spawning by denying all spawn attempts


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