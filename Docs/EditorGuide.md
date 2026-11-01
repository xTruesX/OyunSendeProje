# Börühan — Editor Guide

Everything in the game can be built and changed from the Unity Editor without writing code.
This guide explains the tools and how they fit together.

- [1. The big picture](#1-the-big-picture)
- [2. Börühan Stüdyo (the hub)](#2-börühan-stüdyo-the-hub)
- [3. Rooms and maps](#3-rooms-and-maps)
- [4. Events (Olaylar) — the RPG Maker part](#4-events-olaylar--the-rpg-maker-part)
- [5. Command reference](#5-command-reference)
- [6. Flags, variables and self flags](#6-flags-variables-and-self-flags)
- [7. Recipes](#7-recipes)
- [8. Dialogue and speakers](#8-dialogue-and-speakers)
- [9. Enemies and bosses](#9-enemies-and-bosses)
- [10. Items, powers and the player](#10-items-powers-and-the-player)
- [11. Music](#11-music)
- [12. Game settings and testing](#12-game-settings-and-testing)
- [13. Ready-made components](#13-ready-made-components)
- [14. Animations](#14-animations)
- [15. Replacing placeholder art and sound](#15-replacing-placeholder-art-and-sound)
- [16. Troubleshooting](#16-troubleshooting)
- [17. For programmers: extending the system](#17-for-programmers-extending-the-system)

---

## 1. The big picture

| Thing | What it is | Where it lives |
|---|---|---|
| **Room** | A scene: floor, walls, player, camera, HUD, music | `Assets/Scenes/` |
| **Map** | Tilemap layers you paint (floor, decoration, walls) | inside each room, under `Harita` |
| **Event (Olay)** | Anything that *does* something: NPCs, chests, signs, doors, cutscenes | objects in a room, usually under `Olaylar` |
| **Common event (Ortak Olay)** | A reusable list of commands any event can call | `Assets/Events/OrtakOlaylar/` |
| **Dialogue** | Lines of text with speakers | `Assets/Dialogue/` |
| **Speaker** | Name, color and portrait of a character | `Assets/Dialogue/Konusmacilar/` |
| **Enemy** | Stats + AI + moveset | `Assets/Combat/Dusmanlar/` |
| **Theme** | Floor/border tiles, light, camera zoom, music for new rooms | `Assets/Tiles/Temalar/` |
| **Music track** | One song + volume | `Assets/Audio/Parcalar/` |
| **Game settings** | Where a new game starts, starting flags | `Assets/Resources/OyunAyarlari` |
| **Flags & variables** | The story state, saved with the game | in the save file |

The game's story progress is just **flags** (on/off) and **variables** (numbers). Events read them to
decide what to do, and change them to move the story forward.

---

## 2. Börühan Stüdyo (the hub)

Open it with **Börühan → Stüdyo**. It has a search box and these tabs:

| Tab | What you can do |
|---|---|
| **Olaylar** | Add events from templates (NPC, Tabela, Sandık, Kapı, Tetikleyici Bölge, Otomatik Olay, Tüccar, Boş Olay); list and select every event in the open room; add spawn points and Kut Ocağı save shrines; open the Room Creator |
| **Odalar** | List all rooms in the game and open them; create a new room; open the Tile Palette; list/edit themes and music tracks |
| **Düşmanlar** | List all enemies, create a new one, edit it, place it in the open room |
| **Diyaloglar** | List/create dialogues and speakers |
| **Ortak Olaylar** | List/create common events |
| **Oyuncu** | Edit Börühan's settings (speed, health, attack damage, dodge, parry, upgrades…) — applies everywhere |
| **Ayarlar & Test** | Game settings (new-game start), test flags, "Test the open room", list of flags used in this room |

New things are placed at the **centre of the Scene view** and selected, so you can drag them where you want.

---

## 3. Rooms and maps

### Making a room
**Stüdyo → Odalar → Yeni Oda…** (or **Börühan → Oda Oluşturucu**):

1. **Sahne Dosyası** — file name, no spaces or Turkish letters (e.g. `Kat4_BuzMagarasi`).
2. **Yer Adı** — shown on the save slot (e.g. `Buz Mağarası`).
3. **Kat** — floor number (0 = above ground; hides "N. Kat" on the save slot).
4. **Boyut** — width × height in tiles.
5. **Tema** — floor, border, lighting and music (see below).
6. **Odayı Oluştur ve Aç**.

You get a playable room with the player, camera, pause menu, subtitles, music, a border and a spawn point
called `giris`. It is added to the game's build list automatically.

### Painting the map
Open **Window → 2D → Tile Palette** and pick a palette: `Bozkir`, `Magara`, `Gok`, `IcMekan`, `Col`,
`Demirhane`, `Bataklik`, `Obsidyen`. Every room has these layers under **Harita** — select the layer as the
"Active Tilemap" in the Tile Palette before painting:

| Layer | Use for | Collision |
|---|---|---|
| **Zemin** | Ground | none |
| **Dekor** | Paths, grass, cracks, details — also "fake walls" (wall-looking tiles you can walk through) | none |
| **Engeller** | Mountains, walls, rocks, trees, pillars | solid; sorted with the player so you can walk behind tall things |
| **Cukurlar** (some rooms) | Pits / lava | solid, but **Ayna Sıçraması** can leap over it |

Walls and rocks only block if they are on **Engeller**. If something you painted is walkable in game,
press **Çarpışmaları Yenile** in the Room Creator.

### Themes
**Assets → Create → Börühan → Oda Teması.** A theme sets the floor tile, the border tiles (fill + tall
edge tiles), border thickness, background color, light color/strength, camera zoom and music for rooms
made with it.

### Connecting rooms
- **Spawn point (Varış Noktası)** — where the player appears. Give it an id (e.g. `kapi_kuzey`).
- **Door** — use the **Kapı / Geçiş** event template and set its *Işınla* command to the target room and
  spawn id. (The older `SceneDoor` component also works; see §13.)
- The cyan circles and green boxes you see in the Scene view are spawn points and doors with their labels.

---

## 4. Events (Olaylar) — the RPG Maker part

An **event** is an object with one or more **pages**. Each page says *when* it is active, *what it looks
like*, *how it starts* and *what it does*.

### Pages
- Pages are tabs at the top of the event's Inspector: **Sayfa 1, Sayfa 2…** (`+` adds, `−` removes).
- **The last page whose conditions are all met is the active one.** Put the "default" page first and the
  "later in the story" pages after it.
- The page switches by itself the moment a flag/variable changes — no need to reload the room.
- If no page's conditions are met, the event is hidden and inactive.
- In Play mode the active page is marked with ●.

### Page settings
| Setting | Meaning |
|---|---|
| **Not** | A note for yourself |
| **Koşullar** | All must be true for this page to be active (see §6) |
| **Tetiklenme** | **Etkileşim** — player faces it and presses E. **Dokunma** — player walks into it. **Otomatik** — runs once when the page becomes active (cutscenes). **Paralel** — keeps repeating while the page is active (ambient effects, timers) |
| **Görsel** | Sprite (empty = invisible) |
| **Renk** | Tint |
| **Geçilmez** | On = solid (NPCs, chests). Off = can be walked over (doors, trigger zones) |
| **Boyut** | Size of its collision/trigger box (from the feet up) |
| **Etkileşim Yazısı** | Shown as `[E] …` |
| **Hareket** | Sabit (still), Rastgele dolaş (wander around its start point), Oyuncuya yaklaş (walk to the player when near) |
| **Oyuncuyu Durdur** | The player can't move while this page runs |
| **Komutlar** | What it does, in order (see §5) |

### Building the command list
- **+ Komut Ekle** opens a menu of commands grouped by category.
- Click a command's title to fold it open and edit it.
- ▲ ▼ reorder, ✕ deletes.
- Some commands contain their own lists — **Koşul (Eğer)** has *O zaman* and *Değilse*, **Seçenek Sun**
  has a list per option. These nest as deep as you like, which is how you build event trees.

### Templates (Stüdyo → Olaylar)
| Template | What you get |
|---|---|
| **NPC** | A wandering character; E → shows text |
| **Tabela** | A sign; E → shows text |
| **Sandık** | Page 1: closed chest, E → gives a Kut Parçası and sets `self:acildi`. Page 2 (when `self:acildi`): open chest, "Sandık boş." |
| **Kapı / Geçiş** | Invisible, walk into it → teleport |
| **Tetikleyici Bölge** | Invisible area, walk in → text once (`self:bitti`) |
| **Otomatik Olay** | Plays once when the room starts (`self:bitti`) |
| **Tüccar** | NPC with a Yes/No choice |
| **Boş Olay** | One empty page |
| **Kut Ocağı** (button) | A shrine that calls the `KutOcagi` common event: heal + save |

Templates are only a starting point — change anything.

---

## 5. Command reference

**Mesaj**
| Command | What it does |
|---|---|
| Metin Göster | Speaker (empty = narrator) + text. Each line of text is its own dialogue box. |
| Diyalog Oynat | Plays a Dialogue asset |
| Seçenek Sun | Asks a question; each option has its own command list. Players choose with W/S or arrows and E/Enter/Space, or 1–9 |

**Akış**
| Command | What it does |
|---|---|
| Koşul (Eğer) | If the conditions are met run *O zaman*, otherwise *Değilse*. "Require All" off = any one condition is enough |
| Bekle | Waits N seconds |
| Ortak Olay Çağır | Runs a common event's commands |
| Olayı Bitir | Stops this event here |

**Durum**
| Command | What it does |
|---|---|
| Bayrak Ayarla | Turns a flag on/off (`self:` flags allowed) |
| Değişken | Sets/adds/subtracts a number variable |
| Güç Ver | Gives a story power: Kılıç, Kut Kıvılcımı, Ayna Sıçraması, Demir Eldiven, Zehir Bağışıklığı |
| Oyunu Kaydet | Saves at the player's current position |

**Oyuncu**
| Command | What it does |
|---|---|
| Işınla | To another room + spawn id, or to a spawn id / coordinate in this room |
| İyileştir | Full heal or a fixed amount |
| Hasar Ver | Damages the player (traps) |
| Eşya Ver | Can Parçası (+max HP), Kut Parçası (+damage) or Şifa Otu (heal), with a message |

**Sahne**
| Command | What it does |
|---|---|
| Müzik Çal / Müziği Durdur | Crossfades the music |
| Ses Çal | Plays a one-shot sound |
| Ekranı Salla | Camera shake |
| Nesneyi Göster-Gizle | Turns any object in the room on/off (bridges, walls, decorations) |
| Düşman Çıkar | Spawns enemies next to the event; optionally waits until they are defeated (ambushes, arena waves) |

**Olay**
| Command | What it does |
|---|---|
| Olayı Hareket Ettir | Walks the event by an offset (simple cutscene movement) |
| Olayı Sil (Geçici) | Hides the event until the room is entered again |
| Animasyon Oynat | Plays an animation from a character's Animasyon Seti (this event, another object or Börühan); can wait for it to end |

---

## 6. Flags, variables and self flags

- **Flag** — an on/off switch with a name, e.g. `kopru_onarildi`. Turned on with *Bayrak Ayarla*, checked
  with conditions *Bayrak açık / Bayrak kapalı*. Saved with the game.
- **Variable** — a whole number, e.g. `toplanan_cicek`. Changed with *Değişken*, checked with
  *Değişken ≥ / < / =*. Starts at 0. Saved with the game.
- **Self flag** — a flag that belongs to one event only. Write `self:` in front: `self:acildi`. Two chests
  can both use `self:acildi` without affecting each other. (Internally it becomes `Room/EventId/acildi`.)
  If you duplicate an event, press **Yeni ID** in its Inspector so the copies don't share self flags —
  the button says "(kopya!)" when that's needed.
- Other conditions: **Can Parçası ≥ N**, **Kut Parçası ≥ N**, **Boss savaşı sürüyor**.

### Story flags used by the game
| Flag | Meaning |
|---|---|
| `kilic_alindi` | Has a sword (combat enabled) |
| `kut_kivilcimi` | Kayra Han's spark (red eyes and the blade's glow, combat enabled, +25% damage) |
| `ayna_sicramasi` | Ayna Sıçraması (dodge becomes a blink over pits; it has its own longer cooldown, and until it's back the dodge is a normal dash) |
| `demir_eldiven` | Demir Eldiven: unlocks the heavy attack (stuns, breaks guards and cracked walls) |
| `zehir_bagisikligi` | Poison immunity |
| `ayna_kopya_yenildi`, `temir_han_yenildi`, `calis_han_yenildi`, `erlik_han_yenildi` | Bosses defeated |
| `oyun_bitti` | The ending was seen |
| `can_parcasi_*`, `kut_parcasi_*` | One per collected shard (counted) |
| `yazit_*` | One per lore tablet/soul read (counted in the pause menu) |
| `tilsim_alindi`, `merdiven_belirdi`, `yarik_acildi`, … | Prologue steps (see `StoryFlags.cs`) |

**Stüdyo → Ayarlar & Test** lists every flag and variable used by events in the open room.

---

## 7. Recipes

### A chest that opens once
Use the **Sandık** template. Change the *Eşya Ver* item if you like.

### An NPC who says something different after a boss
1. Page 1: NPC sprite, Etkileşim, *Metin Göster* "Temir Han'dan korkuyorum…".
2. Page 2: same sprite, condition **Bayrak açık** `temir_han_yenildi`, *Metin Göster* "Teşekkürler, yiğit!".

### A bridge that appears when a lever is pulled
1. Make the bridge a separate object (a sprite, or a small tilemap) and turn it **off**.
2. Add a `FlagActivator` component to any always-on object: flag `kopru_acik`, target = the bridge.
   It keeps the bridge visible whenever the flag is on — also after reloading or loading a save.
3. Lever event, page 1: *Metin Göster* "Kolu çektin.", *Ekranı Salla*, *Bayrak Ayarla* `kopru_acik`.
4. Page 2 (condition **Bayrak açık** `kopru_acik`): lever looks pulled, *Metin Göster* "Kol zaten çekilmiş."

### A small quest with a counter
1. Each flower event: *Değişken* `cicek` + 1, *Olayı Sil*, and *Bayrak Ayarla* `self:toplandi` so it
   stays gone (add a page 2 with condition `self:toplandi` and no sprite).
2. Quest giver: *Koşul* `cicek ≥ 3` → *O zaman*: thanks + *Eşya Ver* Can Parçası + *Bayrak Ayarla*
   `cicek_gorevi_bitti`; *Değilse*: "Bana üç çiçek getir."
3. Give the quest giver a page 2 with condition `cicek_gorevi_bitti` for after.

### An ambush
Tetikleyici Bölge → *Metin Göster* "Pusu!", *Düşman Çıkar* (Hain Gölge ×2, wait until defeated),
*Eşya Ver* Kut Parçası, *Bayrak Ayarla* `self:bitti`.

### A cutscene when entering a room
Otomatik Olay with condition `self:bitti` **kapalı**: *Müzik Çal*, *Metin Göster*, *Olayı Hareket Ettir* on
an NPC event (or *Bekle*), *Ekranı Salla*, *Bayrak Ayarla* `self:bitti`.

### A choice that changes the story
*Seçenek Sun* "Ruhu serbest bırakacak mısın?" → **Evet**: *Bayrak Ayarla* `ruh_serbest`;
**Hayır**: *Bayrak Ayarla* `ruh_tutsak`. Other events anywhere can check these flags.

### A reusable "rest" or "shop"
Make a **common event** (Stüdyo → Ortak Olaylar → + Yeni), add its commands, and call it from any number
of events with *Ortak Olay Çağır*. The `KutOcagi` common event (heal + save) is created the first time
you add a Kut Ocağı.

---

## 8. Dialogue and speakers

- **Speaker** (`Assets/Dialogue/Konusmacilar/`): display name, portrait, name color. Change it once and
  every line updates.
- **Dialogue asset**: a list of lines (speaker, text, optional duration for cutscenes, optional flag set
  when the line appears).
- **Hızlı Yazım**: select a dialogue asset and type the whole conversation:

  ```
  Börühan: Babam beni bekliyor.
  Anlatıcı: Yer yarıldı! [bayrak: yarik_acildi]
  Kayra Han: Yolun açık olsun. [süre: 4]
  ```

  Press **Metni Uygula**. Unknown names can be turned into new speakers with one button.
- For short, one-off text, *Metin Göster* inside an event is quicker than a Dialogue asset.
- The subtitle box's font, colors and typing speed are on `Assets/Prefabs/DiyalogKutusu.prefab`.

### Voice acting (Seslendirme)

- Every line has a **Voice** clip that plays when the line appears. The next line cuts it off, and it
  pauses with the game. In cutscenes a subtitle stays up until its recording ends.
- Every line already has an empty (silent) file:
  `Assets/Audio/Seslendirme/<dialogue>/<no>_<speaker>.wav`, e.g. `00_GirisSinematigi/03_Anlatici.wav`.
  **Record over the file with the same name (.wav)** and that line plays it. Nothing else to hook up.
- `Assets/Audio/Seslendirme/Seslendirme.txt` lists every file with who says what. Read from it while recording.
- New lines: press **Seslendirme: … boş ses dosyası oluştur** on the dialogue asset, or
  **Börühan → Bakım → Seslendirme Dosyalarını Oluştur** for all dialogues (also refreshes the list).
- Rewriting a dialogue with Hızlı Yazım keeps each line's recording: lines with the same text keep theirs,
  otherwise the line in the same place does.
- *Metin Göster* in an event has a **Voices** list: one clip per text line, in order.
- Not voiced: the short system messages written in code (pickup messages like "Bir Can Parçası buldun!",
  the boss-fight save warning, choices).

---

## 9. Enemies and bosses

**Stüdyo → Düşmanlar → + Yeni**, then edit it in the Inspector:

- **Looks**: name, difficulty, sprite, tint, scale, foot collision size.
- **Stats**: max health, speed, contact damage, knockback resistance, **poise** (damage before it is
  staggered; 0 = every hit staggers), stagger time.
- **AI**: aggro range, preferred distance (small = melee, big = ranged), strafe.
- **Boss**: shows the big named bar at the top of the screen.
- **Hareket Seti (moveset)**: click a **Yetenek Ekle** button. Each ability has *when* (cooldown,
  min/max range, weight), *timing* (windup = warning time, recovery = punish window) and its own values.
  The AI picks a ready ability that fits the distance.

| Ability | Does |
|---|---|
| Yakın Saldırı | 1+ strikes in front, with a step |
| Atılma | Warning line, then a dash |
| Mermi | Single / fan / burst shots (the sword can cut them; parry reflects the stagger) |
| Işınlanma | Behind / near / away from the player, optional follow-up move |
| Yanılsama | Fragile see-through copies (can drop a Şifa Küresi, see below) |
| Yer Sarsma | Ground slam in growing waves |
| Kalkan | Guard; light hits barely hurt, heavy attack breaks it |
| Korku | Slows the player in range |
| Ayna | Reflects hits back for a moment |
| Çağırma | Summons other enemies (with a limit; can drop a Şifa Küresi, see below) |
| Zehir Bulutu | Lingering poison clouds (blocked by poison immunity) |

**Placing**: *Yerleştir* in the Stüdyo, *Sahneye Yerleştir* in the enemy Inspector, or *Düşman Çıkar* in
an event (spawns during play).

**A boss fight** like the existing ones: put the boss in a room and add a `BossEncounter` component
(boss, defeated flag, victory dialogue, **reward item**, optional next room).

- The **reward item** is an inactive `Pickup` of kind **Özel Eşya** placed in the room. When the boss dies
  it appears where the boss fell; the player has to walk over it. Picking it up sets its **Grant Flag**
  (e.g. `ayna_sicramasi`) and plays its **Pickup Dialogue**.
- Make the way onward depend on the reward flag (e.g. a `FlagActivator` on the exit rift), so the player
  can't continue without picking it up. If they leave or reload first, the item is still lying there.
- All boss rewards use the placeholder `Assets/Art/Placeholder/OzelEsya.png`; give each its own sprite
  by changing the Sprite Renderer on its `Odul_…` object.
- Or build it with events: Otomatik intro → *Düşman Çıkar* (wait until defeated) → reward.

---

## 10. Items, powers and the player

- **Can Parçası**: +20 max HP each (amount set on the player). **Kut Parçası**: +10% damage each.
  **Şifa Otu**: heals 40.
- **Şifa Küresi** (green orb): summons can drop one when the player kills them. Set *Health Orb Chance*
  (0–1) and *Health Orb* on a Yanılsama or Çağırma ability. Ayna Kopya's copies drop one 30% of the time
  and Temir Han's Kor Cini 40%. A copy that just fades away drops nothing. The orb heals 15, drifts to the
  player when close and disappears after 12 seconds. It stays put while the player is at full health.
  Heal amount, lifetime and sound are on `Assets/Prefabs/SifaKuresi.prefab`; the art is
  `Art/Placeholder/SifaKuresi.png` and the sound `Audio/Efektler/SifaKuresi.wav`.
- Give them with the *Eşya Ver* command, or place a `Pickup` object (walk over it; collected once).
- **Story items are pickups too** (`Pickup` kind **Özel Eşya**): the sword and every boss reward. Each
  sets a flag and plays a dialogue when picked up. Duplicate one to make your own. A pickup waits until
  no dialogue is on screen, so one that appears under the player mid-conversation is taken right after.
- **The sword comes from Kayra Han** (Gök Katı). His conversation is the event `KayraHan_Olay` — a good
  example of pages: page 1 talks and sets `kayra_kilic_birakti` (the sword falls in front of him),
  page 2 "Kılıcını al", page 3 (after `kut_kivilcimi`) a farewell. Picking up the sword continues his
  speech: the Kut spark lights the blade and he points to the cave.
- **Powers** come from *Güç Ver* or from flags (§6).
- **Aiming**: attacks go towards the mouse pointer and Börühan turns to face the swing. With a gamepad
  (or J/K with the mouse untouched since the gamepad was used) they go where he walks. The dash still goes
  where he walks.
- **The heavy attack is locked** until `demir_eldiven` (Temir Han, the 2nd boss). Change or clear
  *Heavy Attack Flag* on the player (Stüdyo → Oyuncu) to move or remove the lock.
- **Player settings** (Stüdyo → Oyuncu): walk speed, base health, health per shard, damage per shard,
  combo/heavy attack damage and timing, dodge, parry window, blink distance and cooldown, gauntlet stun/cooldown.

Health is saved and carried between rooms.

### Death screen
When Börühan dies the music fades and the **death screen** (*YENİLDİN*) fades in over the room:
- **Tekrar Dene** reloads the **last save** with full health. That's the last Kut Ocağı, pause-menu save or
  room change (every door saves on the way), whichever came last. Anything since (items, flags) is undone.
  In test mode (scene played directly, no save) the current room restarts instead.
- **Ana Menüye Dön** goes to the main menu without saving.
- The text under the title is picked at random from *Messages*. Those, the timing and the three sounds are
  on the `DeathScreen` component of `Assets/Prefabs/OyunArayuzu.prefab`; the screen itself is its child
  *OlumEkrani*. *Börühan → Bakım → Ölüm Ekranını Kur* adds it if it's missing.

### Save points and boss fights
A **boss fight** is in progress from the moment a boss (an enemy with **Boss** ticked) starts fighting
until it dies. During it:

- **Save points can't be used.** Any object with the `SavePoint` component (every Kut Ocağı has one)
  only shows "Kut Ocağı'nın ateşi titriyor... Savaş sürerken burada dinlenemezsin." — the text can be
  changed on the component. Add `SavePoint` to any event or object you want to behave the same way.
- The pause menu's **Kaydet** button and the *Oyunu Kaydet* command don't save.
- Events can check it with the condition **Boss savaşı sürüyor**.

---

## 11. Music

- Every song is a **Music Track** (`Assets/Audio/Parcalar/`): drag your audio file into its **Clip**
  field; every room using that track updates.
- A room's music is the **SahneMuzigi** object (`SceneMusic` component) — pick a track or leave it empty
  for silence. Rooms made with a theme get the theme's music.
- Events can change music during play (*Müzik Çal*, *Müziği Durdur*).
- Music crossfades between rooms and keeps playing if the next room uses the same track.

---

## 12. Game settings and testing

**Stüdyo → Ayarlar & Test**

- **New game start**: room, spawn id, location name, and flags that are already on (e.g. skip the
  prologue while testing by starting in `Kat1_Col` with `kut_kivilcimi`).
- **Test the open room**: open any room and press **▶ Açık Sahneyi Test Et**. The **Test Bayrakları**
  (comma-separated) are turned on first — buttons add common ones (sword, Kut spark, all powers).
  Saving is disabled in this test mode; start from the main menu to test saving.

---

## 13. Ready-made components

These older single-purpose components are still used by the existing story and work fine alongside
events:

| Component | Does |
|---|---|
| `Interactable` | E → dialogue, with lock/repeat options and an *On Finished* hook |
| `StoryTrigger` | Walk in / on start → dialogue once |
| `SceneDoor` | Walk in → another room (can lead elsewhere while locked) |
| `SpawnPoint` | Named arrival point |
| `FlagActivator` | Shows/hides objects by a flag |
| `Pickup` | Walk-over item |
| `HazardZone` | Damage/slow area (poison, bog) |
| `BreakableWall` | Breaks with a heavy attack + Demir Eldiven |
| `BossEncounter` | Boss victory: dialogue, flag, next room |
| `TalismanGuide` | The glowing talisman in the overworld |
| `RoomUtility` | Hooks for events: reload room, heal, rest at shrine |
| `SavePoint` | Marks a save point; it can't be used during a boss fight |

Anything those do can also be done with events — use whichever is quicker.

---

## 14. Animations

Every character has an **Animasyon Seti** (animation set): Börühan, every enemy, and the NPCs
(Kayra Han, the Ruh spirits, Ülgen Han, and NPCs made from the Stüdyo or Room Creator).
Right now they hold **placeholder animations** generated from each character's own sprite: a breathing
idle, a bobbing walk, a lunge for attacks, a flinch, a collapse on death. Replace them with real frames
whenever you like.

**Where:** `Assets/Art/Animasyonlar/<Karakter>/`. Each folder has one strip image per animation
(`Bekle.png`, `Yuru.png`, `Saldiri.png`…), with frames side by side from left to right, plus the set itself
(`<Karakter>_Animasyon.asset`).

### Replacing frames

- **Quickest:** overwrite a strip PNG with your own, using the same name and the same frame count.
  Open the set and drag the PNG onto its animation again (hold **Shift** to replace instead of append).
- **Any number of frames:** select the set, then drag sprites or a strip image onto an animation's box.
  If the image isn't cut into frames yet, it's sliced automatically. Set **Şerit Kare Sayısı** first, or
  leave it at 0 for square frames (width ÷ height). Frames get the same pixels-per-unit as the rest of
  the set and their pivot at the feet.
- **A sheet with one row per direction** (like Börühan's): open **Sprite Sayfası İçe Aktar** in the set,
  pick the image, the state, the number of columns (frames) and rows, which direction each row is, and
  the speed (1000 ÷ milliseconds per frame, e.g. 120 ms ≈ 8.3), then **İçe Aktar**. It is cut into a grid
  and replaces that state's old animations.
- The **Önizleme** at the top of the set plays any animation. **Sola** shows it mirrored.

### Animation states

| State | When it plays |
|---|---|
| Bekle | Standing still |
| Yürü | Moving (detected automatically, so knockback and cutscene movement count too) |
| Saldırı | Börühan's light attack; an enemy's moves (by default) |
| Ağır Saldırı | Börühan's heavy attack (falls back to Saldırı) |
| Kaçınma | Dodge / Ayna Sıçraması; enemy teleports |
| Savuşturma | Börühan's parry; enemy Kalkan and Ayna moves |
| Hasar | A short flinch when hit (doesn't cancel an attack) |
| Sersemleme | While an enemy is staggered |
| Ölüm | On death. Enemies finish it before fading out |
| Özel | A named animation, used by moves and the *Animasyon Oynat* command |
| Dönüş | Turning in place: frames go round the character from the front (front, front-right, right, back-right, back, back-left, left, front-left). When the character changes facing, the in-between frames play |

Missing animations fall back: Ağır Saldırı and Özel → Saldırı, Yürü → Bekle, anything else → the plain sprite.
A one-shot animation (Döngü off) stays on its last frame for as long as the action lasts.

**Directions:** an animation's **Yön** can be *Hepsi* (all directions), *Aşağı*, *Yukarı*, *Sol*, *Sağ* or
*Yan*. Sol and Sağ are used exactly as drawn. *Yan* is one side-view drawn facing right and mirrored for
left (when **Sola Bakınca Aynala** is on), handy when the left and right frames would be identical.
A missing direction falls back to Yan, then Hepsi.

**Börühan's looks.** He has three animation folders in `Assets/Art/Animasyonlar/`:

| Folder | Used |
|---|---|
| `Boruhan/` | From the start |
| `Boruhan_Tilsim/` | Once he takes the Tılsım (`tilsim_alindi`) |
| `Boruhan_Kilic/` | Once he takes the sword (`kilic_alindi`); wins over the Tılsım look |

Drop sprite sheets into a folder and run **Börühan → Bakım → Börühan Animasyonlarını İçe Aktar**. The file
name tells the importer what the sheet is, so name sheets like the ones already there:
*what* + *directions* + *frames* + *frame time*, e.g. `Yürüme • 4 yön × 4 kare • 120 ms.png`,
`Bekle_4yon_3kare_240ms.png` or `Dönüş • 8 yön • 100 ms.png`.

- What: Bekle(me), Yürü(me), Dönüş, Saldırı, Ağır Saldırı, Kaçınma, Savuşturma, Hasar, Sersemleme, Ölüm.
- 4 directions = rows front, back, right, left (top to bottom). 1 direction = used for every facing.
  Dönüş is a single row of facings going round him: front, front-right, right, back-right, back,
  back-left, left, front-left.
- Sheets without a frame count in the name (like the generated placeholders) are left alone.
- Missing combat animations are generated: for each look, the importer draws 4-direction placeholder
  sheets for Saldırı, Ağır Saldırı, Kaçınma, Savuşturma, Hasar and Ölüm from that look's own Bekle
  frames (so with the sword he attacks holding the sword), named like `Saldiri_4yon_3kare_71ms.png`.
  To replace one, delete it and drop in your own sheet (any layout), then run the import again.

Any character can have looks like these: in its **Character Animator**, add entries to **Variants**
(a flag and an Animasyon Seti). The last one whose flag is set is used.

### Giving animations to someone

| Who | How |
|---|---|
| An enemy | Its definition → **Animasyon Seti** field, or press **Animasyon Seti Oluştur** to generate placeholders from its sprite |
| An enemy move | Each ability → *Animasyon*: pick a state, or *Özel* plus a name to give that move its own clip (e.g. a wind-up for Yer Sarsma) |
| An event NPC | Each page → **Animasyon Seti** (pages can use different sets) |
| Anything else with a sprite | Add **Character Animator** next to its SpriteRenderer, then set a set or press *Bu sprite'tan yer tutucu animasyon seti oluştur* |
| Börühan | `Assets/Prefabs/Boruhan.prefab` → *Govde* → Character Animator |

New sets: **Assets → Create → Börühan → Animasyon Seti**.

**Cutscenes:** the event command **Olay → Animasyon Oynat** plays an animation on this event, on another
object or on Börühan, and can wait for it to finish (e.g. Kayra Han raising his hand with an Özel clip named `el_kaldir`).

**Börühan → Animasyonları Kur** creates any missing placeholder sets and connects them. It's safe to run
again: it never overwrites strips or sets that already exist, and skips characters that already have a set.

---

## 15. Replacing placeholder art and sound

All placeholders are plain files — overwrite them with the same name and everything using them updates:

| What | Where |
|---|---|
| Characters, props, items, portraits | `Assets/Art/Placeholder/` |
| Enemies | `Assets/Art/Placeholder/Dusmanlar/` |
| Animation frames | `Assets/Art/Animasyonlar/<Karakter>/` (see [Animations](#14-animations)) |
| Tiles | `Assets/Art/Tiles/<Set>/` |
| Music | `Assets/Audio/Muzik/` (or drag new clips into the tracks) |
| Attack sounds | `Assets/Audio/Efektler/` — `HafifSaldiri.wav` (light hit), `AgirSaldiri.wav` (heavy hit), `IskaSaldiri.wav` (swing that hits nothing). Or drag other clips into the player's *Ses Efektleri* fields (Stüdyo → Oyuncu) |
| Dash sounds | `Assets/Audio/Efektler/` — `Kacis.wav` (dash), `AynaSicramasi.wav` (teleport), `KacisHazirDegil.wav` (Kaçış pressed while not ready), `AynaSicramasiHazir.wav` (teleport ready again). Same *Ses Efektleri* fields |
| Intro video | `Assets/StreamingAssets/Videolar/giris_sinematigi.mp4` (use `.webm` if it won't play on Linux). Its own sound is never played; the narration comes from the voice files |
| Death screen sounds | `Assets/Audio/Efektler/` — `Olum.wav` (Börühan falls), `OlumEkrani.wav` (screen appears), `TekrarDene.wav` (Tekrar Dene pressed) |
| Voice lines | `Assets/Audio/Seslendirme/` — see *Voice acting* in section 8 |
| Main menu art | `Assets/Art/MainMenu/` |

Sprites are 16 pixels per unit. Tall things (trees, pillars, characters) have their pivot at the bottom.

---

## 16. Troubleshooting

| Problem | Fix |
|---|---|
| Painted walls are walkable | Paint on **Engeller**, then **Çarpışmaları Yenile** (Room Creator) |
| Two events share their `self:` state | Press **Yeni ID** on the copy |
| An event never appears | No page's conditions are met — check the conditions; in Play mode the active page shows ● |
| E does nothing on an event | Its active page is not *Etkileşim*, or it's still running |
| The player spawns in the wrong place | The door's spawn id doesn't match a `SpawnPoint` id in the target room |
| Keyboard ignored in Play mode | Click the Game view so it has focus |
| A scene I generated looks old | The builders (`Börühan → Prolog Sahnelerini Oluştur`, `Yeraltını Oluştur`) never overwrite existing scenes; delete the scene file to regenerate it |

---

## 17. For programmers: extending the system

- **New event command**: a `[Serializable]` class inheriting `EventCommand` with a
  `[CommandInfo("Kategori/Ad", "help")]` attribute and a `Run(EventContext ctx)` coroutine. Its public
  fields show in the Inspector; fields of type `List<EventCommand>` marked `[SerializeReference]` become
  nested command lists automatically. It appears in **+ Komut Ekle** without further changes.
- **New enemy ability**: inherit `EnemyAbility`, add `[CreateAssetMenu]`, write `Perform(EnemyController)`.
  Add it to the list in `EnemyDefinitionEditor` to get a button.
- **New condition**: add a value to `ConditionKind` and handle it in `EventCondition.IsMet`.
- Code lives in `Assets/Scripts/` (`Events/`, `Combat/`, `Story/`, `Dialogue/`, `Map/`, `Audio/`,
  editor tools in `Editor/`).
