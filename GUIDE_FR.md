# 📖 Guide de Configuration - Spooky Nights

Le mod **Spooky Nights** est hautement configurable pour s'adapter aussi bien aux joueurs solo cherchant l'immersion qu'aux serveurs multijoueurs souhaitant créer des événements saisonniers.

Les fichiers de configuration se génèrent automatiquement au premier lancement du jeu dans le dossier :
`VintagestoryData/ModConfig/`

Il y a deux fichiers distincts :
1.  `spookynights-client.json` (Options visuelles personnelles)
2.  `spookynights-server.json` (Règles du monde, apparition des monstres, butin)

---

## 🖥️ Configuration Client (`spookynights-client.json`)

Ce fichier affecte uniquement ce que **vous** voyez. Chaque joueur peut avoir ses propres réglages.

| Option | Type | Description |
| :--- | :--- | :--- |
| **EnableJackOLanternParticles** | `true` / `false` | Active ou désactive les particules de flammes à l'intérieur des citrouilles sculptées (Jack o' Lanterns). Utile si vous avez des baisses de FPS près de nombreuses citrouilles. |

---

## 🌍 Configuration Serveur (`spookynights-server.json`)

Ce fichier contrôle les mécaniques de jeu. En multijoueur, seul le fichier du serveur est pris en compte.

### 🍬 Système de Bonbons (Trick or Treat)

| Option | Type | Description |
| :--- | :--- | :--- |
| **EnableCandyLoot** | `true` / `false` | Active l'obtention de sacs de bonbons sur les mobs spectraux. Mettez `false` pour désactiver complètement les bonbons. |
| **HalloweenEventOnly** | `true` / `false` | Si `true`, les bonbons ne tomberont que durant le mois d'**Octobre** (en jeu). Idéal pour les serveurs RP saisonniers. |

### 👻 Apparition des Créatures (Spawning)

Contrôlez quand et comment les monstres apparaissent.

| Option | Type | Description |
| :--- | :--- | :--- |
| **SpawnMultipliers** | `Liste` | Ajuste la fréquence d'apparition par type de monstre. <br>• `1.0` = Normal<br>• `0.5` = Deux fois moins souvent<br>• `2.0` = Deux fois plus souvent<br>• `0.0` = Désactivé (Le monstre n'apparaîtra jamais) |
| **SpawnOnlyAtNight** | `true` / `false` | Si `true`, les entités spectrales n'apparaissent que la nuit. Si `false`, elles peuvent apparaître le jour (dangereux !). |
| **LightLevelThreshold** | `0` à `32` | Niveau de lumière maximum pour qu'un monstre apparaisse. `7` est la pénombre standard. |

#### 🕒 Gestion de la Nuit
| Option | Type | Description |
| :--- | :--- | :--- |
| **NightTimeMode** | `"Auto"` | • `"Auto"` : Le mod détecte le coucher/lever du soleil selon la saison et la latitude.<br>• `"Manual"` : Utilise les heures fixes définies ci-dessous. |
| **NightStartHour** | `0.0` à `24.0` | Heure de début de la nuit (ex: `20.0` pour 20h00). Ignoré en mode Auto. |
| **NightEndHour** | `0.0` à `24.0` | Heure de fin de la nuit (ex: `6.0` pour 06h00). Ignoré en mode Auto. |

### 📅 Événements et Calendrier

Ces options permettent de transformer le mod en événement ponctuel.

| Option | Type | Description |
| :--- | :--- | :--- |
| **AllowedSpawnMonths** | `[Liste]` | Liste des mois autorisés (1 à 12). Exemple : `[10]` pour Octobre uniquement. Laissez vide `[]` pour autoriser toute l'année. |
| **SpawnOnlyOnLastDayOfMonth** | `true` / `false` | Les monstres n'apparaissent que le dernier jour de chaque mois. |
| **SpawnOnlyOnLastDayOfWeek** | `true` / `false` | Les monstres n'apparaissent que le dernier jour de la semaine (Dimanche ?). |
| **SpawnOnlyOnFullMoon** | `true` / `false` | Les monstres n'apparaissent que les soirs de **Pleine Lune**. |
| **FullMoonSpawnMultiplier** | `Float` | Multiplicateur bonus les soirs de pleine lune (ex: `2.0` = Double de monstres). |

### 👹 Les Boss

Configuration spécifique pour les entités majeures comme l'Ours Spectral Géant.

```json
"Bosses": {
  "spookynights:spectralbear-giant-*": {
    "Enabled": true,
    "AllowedMoonPhases": ["full"]
  }
}
```
*   **Enabled :** Active ou désactive ce Boss.
*   **AllowedMoonPhases :** Liste des phases de lune requises.
    *   Valeurs possibles : `"full"` (Pleine), `"waxing"` (Croissante), `"waning"` (Décroissante), `"new"` (Nouvelle), etc.

### 🛠️ Avancé

| Option | Type | Description |
| :--- | :--- | :--- |
| **EnableDebugLogging** | `true` / `false` | Affiche des messages techniques dans la console du serveur (`[SpookyNights] Checked spawn...`). À n'utiliser qu'en cas de problème pour ne pas spammer les logs. |

---

## 📝 Exemple de configuration "Halloween Hardcore"

Vous voulez que votre serveur soit terrifiant, mais **uniquement** pendant le mois d'octobre et les soirs de pleine lune ?

Copiez ceci dans votre `spookynights-server.json` :

```json
{
  "EnableCandyLoot": true,
  "HalloweenEventOnly": true,
  "SpawnMultipliers": {
    "spookynights:spectralwolf-*": 1.5,
    "spookynights:spectraldrifter-*": 2.0
  },
  "SpawnOnlyAtNight": true,
  "AllowedSpawnMonths": [ 10 ],
  "SpawnOnlyOnFullMoon": true,
  "FullMoonSpawnMultiplier": 3.0
}