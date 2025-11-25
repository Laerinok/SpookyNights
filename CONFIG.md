# ⚙️ Configuration Guide

Spooky Nights is highly configurable to suit both immersive survival experiences and seasonal server events.

Configuration files are generated automatically after the first launch in:
`VintagestoryData/ModConfig/`

There are two files:
1.  `spookynights-client.json` (Visual settings, per player)
2.  `spookynights-server.json` (Game rules, spawning, loot)

---

## 🖥️ Client Configuration (`spookynights-client.json`)
These settings only affect **you**.

| Option | Type | Description |
| :--- | :--- | :--- |
| **EnableJackOLanternParticles** | `true` / `false` | Toggles the flame particles inside Jack o' Lanterns. Disable this if you experience FPS drops near large pumpkin decorations. |

---

## 🌍 Server Configuration (`spookynights-server.json`)
These settings affect gameplay and mechanics.

### 🍬 Candy System (Trick or Treat)
Control when players can find candy bags on spectral mobs.

| Option | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| **EnableCandyLoot** | `bool` | `true` | **Master Switch.** If `false`, candy will never drop. |
| **AllowedCandyMonths** | `list` | `[10]` | List of months (1-12) when candy drops are active.<br>• `[]` (Empty): Drops all year round.<br>• `[10]`: October only.<br>• `[4, 10]`: April and October. |
| **CandyOnlyOnFullMoon** | `bool` | `false` | If `true`, candy bags will **only** drop during Full Moon nights, regardless of the month. |

### 👻 Spawning Rules
Control the density and conditions for spectral creatures.

| Option | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| **SpawnMultipliers** | `dict` | `1.0` | Adjust spawn rate per entity code.<br>• `1.0`: Normal<br>• `0.5`: Half as often<br>• `2.0`: Twice as often<br>• `0.0`: Disabled |
| **SpawnOnlyAtNight** | `bool` | `true` | If `true`, spectral mobs vanish/die when the sun rises. |
| **LightLevelThreshold** | `int` | `7` | Maximum light level for a mob to spawn. |

#### 🕒 Day/Night Cycle
| Option | Type | Description |
| :--- | :--- | :--- |
| **NightTimeMode** | `"Auto"` | • `"Auto"`: Detects sunset/sunrise based on latitude/season.<br>• `"Manual"`: Uses fixed hours defined below. |
| **NightStartHour** | `float` | Start of danger time (e.g., `20.0` for 8 PM). Ignored in Auto mode. |
| **NightEndHour** | `float` | End of danger time (e.g., `6.0` for 6 AM). Ignored in Auto mode. |

### 📅 Seasonal Events & Calendar
Turn the mod into a rare event.

| Option | Type | Description |
| :--- | :--- | :--- |
| **AllowedSpawnMonths** | `[ ]` | List of months (1-12) where mobs can spawn. Leave empty `[]` for all year. |
| **SpawnOnlyOnLastDayOfMonth** | `bool` | Mobs only appear on the very last day of the month. |
| **SpawnOnlyOnLastDayOfWeek** | `bool` | Mobs only appear on the last day of the week. |
| **SpawnOnlyOnFullMoon** | `bool` | Mobs only appear during Full Moon nights. |
| **FullMoonSpawnMultiplier** | `float` | Spawn rate multiplier during Full Moons (e.g., `2.0` = Double trouble). |

### 👹 Boss Configuration
Specific settings for major entities (e.g., Giant Spectral Bear).

```json
"Bosses": {
  "spookynights:spectralbear-giant-*": {
    "Enabled": true,
    "AllowedMoonPhases": ["full"]
  }
}
```
*   **Enabled:** Toggle the boss ON/OFF.
*   **AllowedMoonPhases:** List of required phases (e.g.,   "Empty", "Grow1", "Grow2", "Grow3", "Full", "Shrink1", "Shrink2", "Shrink3")..

---

## 📝 Configuration Examples

### 1. The "Halloween Only" Server (Default)
Candy and Mobs only appear in October.
```json
"AllowedCandyMonths": [ 10 ],
"AllowedSpawnMonths": [ 10 ]
```

### 2. The "Full Moon Ritual"
Mobs and Candy appear all year round, but **ONLY** during Full Moons.
```json
"SpawnOnlyOnFullMoon": true,
"CandyOnlyOnFullMoon": true,
"AllowedCandyMonths": [],
"AllowedSpawnMonths": []
```

### 3. The "Hardcore Night"
Mobs appear every night all year, but Candy is a rare Full Moon reward.
```json
"SpawnOnlyAtNight": true,
"AllowedSpawnMonths": [],
"AllowedCandyMonths": [],
"CandyOnlyOnFullMoon": true
```